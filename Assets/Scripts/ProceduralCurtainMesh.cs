using UnityEngine;

/// <summary>A hanging fabric surface, in metres, with its origin at the top centre.</summary>
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralCurtainMesh : MonoBehaviour
{
    [Header("Dimensions (meters)")]
    [Min(0.005f)] [SerializeField] private float width = 1.8f;
    [Min(0.1f)] [SerializeField] private float height = 2.2f;

    [Header("Folds")]
    [Range(1, 128)] [SerializeField] private int foldCount = 8;
    [Min(0f)] [SerializeField] private float foldDepth = 0.06f;
    [Range(0.5f, 2f)] [SerializeField] private float foldProfile = 1f;

    [Header("Fabric drape")]
    [Tooltip("Variacao suave entre as pregas. Zero deixa todas iguais.")]
    [Range(0f, 0.5f)] [SerializeField] private float foldVariation = 0.22f;
    [Tooltip("Soltura abaixo da fixacao: curva as pregas e suaviza a parte inferior.")]
    [Range(0f, 1f)] [SerializeField] private float drape = 0.78f;
    [Tooltip("Pequenas rugas que nascem perto do topo e se dissipam ao longo do tecido.")]
    [Range(0f, 1f)] [SerializeField] private float wrinkleStrength = 0.35f;
    [Tooltip("Intensidade geral das irregularidades. Zero produz ondas regulares.")]
    [Range(0f, 1f)] [SerializeField] private float naturalness = 1f;
    [Tooltip("Mude para obter outro caimento. O mesmo valor produz a mesma forma.")]
    [SerializeField] private int seed = 17;

    [Header("Edge finishing")]
    [Range(0f, 0.25f)] [SerializeField] private float edgeFade = 0.08f;
    [Tooltip("Altura da bainha em metros. Reduz as rugas junto a barra.")]
    [Range(0f, 0.2f)] [SerializeField] private float hemWidth = 0.06f;
    [Tooltip("Irregularidade da barra em metros, sem ultrapassar a altura definida.")]
    [Range(0f, 0.03f)] [SerializeField] private float hemLift = 0.008f;

    [Header("Texture mapping")]
    [Tooltip("Mantem a escala da trama ao redimensionar. Desative para UVs de 0 a 1.")]
    [SerializeField] private bool useMetricUVs = true;
    [Min(0.01f)] [SerializeField] private float textureRepeatsPerMeter = 1f;

    [Header("Mesh resolution")]
    [Range(8, 32)] [SerializeField] private int segmentsPerFold = 16;
    [Range(4, 128)] [SerializeField] private int verticalSegments = 20;
    [Tooltip("Resolucao vertical por metro, respeitando o limite de vertices para celular.")]
    [Range(4, 32)] [SerializeField] private int segmentsPerMeter = 16;

    // Closed span: gathering must not shrink the texture along with the panel.
    [SerializeField, HideInInspector] private float referenceWidth;
    [SerializeField, HideInInspector] private int panelSeedOffset;

    private const int MaxVertices = 60000;
    private const float TwoPi = Mathf.PI * 2f;
    private Mesh generatedMesh;
    private MeshFilter meshFilter;
    private Vector3[] vertices;
    private Vector2[] uvs;
    private Vector3[] previousRestRow;
    private float[] verticalLengths;
    private int[] triangles;
    private int cachedColumns = -1;
    private int cachedRows = -1;
    private bool rebuildRequested;

    private void OnEnable()
    {
        rebuildRequested = true;
    }

    private void Update()
    {
        if (rebuildRequested)
            GenerateMesh();
    }

    private void OnValidate()
    {
        // May run off the main thread. Mesh creation/assignment belongs in Update.
        rebuildRequested = true;
    }

    public void Configure(float newWidth, float newHeight, int newFoldCount,
        float newFoldDepth, float newFoldProfile)
    {
        Configure(newWidth, newHeight, newFoldCount, newFoldDepth,
            newFoldProfile, newWidth, 0, naturalness);
    }

    public void Configure(float newWidth, float newHeight, int newFoldCount,
        float newFoldDepth, float newFoldProfile, float closedWidth,
        int seedOffset, float fabricNaturalness)
    {
        width = newWidth;
        height = newHeight;
        foldCount = newFoldCount;
        foldDepth = newFoldDepth;
        foldProfile = newFoldProfile;
        referenceWidth = closedWidth;
        panelSeedOffset = seedOffset;
        naturalness = fabricNaturalness;
        GenerateMesh();
    }

    [ContextMenu("Regenerar tecido")]
    public void GenerateMesh()
    {
        rebuildRequested = false;
        ClampSettings();
        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>();

        int columns = Mathf.Clamp(foldCount * segmentsPerFold, 8, 1024);
        int rows = Mathf.Clamp(Mathf.Max(verticalSegments,
            Mathf.CeilToInt(height * segmentsPerMeter)), 4, 128);
        rows = Mathf.Min(rows, MaxVertices / (columns + 1) - 1);
        bool topologyChanged = EnsureMesh(columns, rows);

        float restWidth = Mathf.Max(width, referenceWidth);
        float pitch = width / foldCount;
        float restPitch = restWidth / foldCount;
        // Approximation of packing more fabric into a smaller span, not cloth physics.
        float gatheredDepth = Mathf.Sqrt(foldDepth * foldDepth +
            Mathf.Max(0f, restPitch * restPitch - pitch * pitch) / 16f);
        float seedPhase = ((uint)unchecked(seed + panelSeedOffset) % 10007u) * 0.173f;
        float minHem = float.MaxValue;
        for (int x = 0; x <= columns; x++)
            minHem = Mathf.Min(minHem, HemOffset((float)x / columns, seedPhase));

        System.Array.Clear(verticalLengths, 0, verticalLengths.Length);
        for (int y = 0; y <= rows; y++)
        {
            float v = RowFraction(y, rows);
            Vector3 previousRest = Vector3.zero;
            float horizontalLength = 0f;
            for (int x = 0; x <= columns; x++)
            {
                float u = (float)x / columns;
                int index = y * (columns + 1) + x;
                Vector3 position = EvaluatePosition(u, v, width, gatheredDepth, seedPhase, minHem);
                Vector3 restPosition = restWidth == width ? position :
                    EvaluatePosition(u, v, restWidth, foldDepth, seedPhase, minHem);
                vertices[index] = position;

                if (x > 0)
                    horizontalLength += Vector3.Distance(previousRest, restPosition);
                if (y > 0)
                    verticalLengths[x] += Vector3.Distance(previousRestRow[x], restPosition);

                uvs[index] = useMetricUVs
                    ? new Vector2(horizontalLength, verticalLengths[x]) * textureRepeatsPerMeter
                    : new Vector2(u, v);
                previousRest = restPosition;
                previousRestRow[x] = restPosition;
            }
        }

        generatedMesh.vertices = vertices;
        generatedMesh.uv = uvs;
        if (topologyChanged)
            generatedMesh.triangles = triangles;
        generatedMesh.RecalculateNormals();
        generatedMesh.RecalculateTangents();
        generatedMesh.RecalculateBounds();
        if (meshFilter.sharedMesh != generatedMesh)
            meshFilter.sharedMesh = generatedMesh;
    }

    private Vector3 EvaluatePosition(float u, float v, float span, float depth,
        float seedPhase, float minHem)
    {
        float foldCoordinate = u * foldCount;
        float phase = foldCoordinate * TwoPi;
        float release = Mathf.SmoothStep(0f, 1f, v);
        float variation = foldVariation * naturalness;
        float looseness = drape * naturalness;
        float spacingVariation = Mathf.Sin(foldCoordinate * 1.71f + seedPhase) * variation;
        float wandering = Mathf.Sin(foldCoordinate * 0.83f + v * 2.4f + seedPhase);
        phase += spacingVariation + wandering * release * looseness * 1.5f;

        float sine = Mathf.Sin(phase);
        // Softened power profile avoids a cusp/normal seam at sine == 0.
        float fold = sine * Mathf.Pow((sine * sine + 0.04f) / 1.04f, (foldProfile - 1f) * 0.5f);
        float amplitude = 1f + Mathf.Sin(foldCoordinate * 1.13f + seedPhase + 1.2f) * variation;
        amplitude *= 1f - looseness * 0.22f * release;
        amplitude *= 1f + Mathf.Sin(foldCoordinate * 1.9f + v * 4.3f + seedPhase) *
            Mathf.Sin(Mathf.PI * v) * looseness * 0.18f;

        float edgeFactor = 1f;
        if (edgeFade > 0f)
        {
            float distanceToEdge = Mathf.Min(u, 1f - u);
            // Keep a little body at the side hem instead of flattening it completely.
            edgeFactor = Mathf.Lerp(0.35f, 1f,
                Mathf.SmoothStep(0f, 1f, distanceToEdge / edgeFade));
        }

        float hemStart = 1f - Mathf.Min(hemWidth, height * 0.25f) / height;
        float hemStiffness = hemWidth > 0f
            ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(hemStart, 1f, v)) : 0f;
        float wrinkleEnvelope = Mathf.Sin(Mathf.PI * v) * Mathf.Exp(-1.4f * v);
        float wrinkles = Mathf.Sin(phase * 2f + v * 5f + seedPhase) * 0.65f +
                         Mathf.Sin(phase * 3f - v * 7f + seedPhase * 0.7f) * 0.35f;
        float wrinkleDepth = Mathf.Min(0.025f, depth * 0.35f) * wrinkleStrength * naturalness;
        float billow = Mathf.Sin(u * Mathf.PI) * Mathf.Sin(u * TwoPi + seedPhase + v * 1.8f);
        float z = depth * fold * amplitude * edgeFactor;
        z += wrinkles * wrinkleDepth * wrinkleEnvelope * (1f - 0.8f * hemStiffness) * edgeFactor;
        z += billow * Mathf.Min(0.025f, depth * 0.35f) * looseness * release;
        if (hemWidth > 0f)
        {
            float hemRoll = Mathf.Sin(Mathf.PI * Mathf.InverseLerp(hemStart, 1f, v));
            z -= hemRoll * Mathf.Min(0.002f, depth * 0.05f) * naturalness;
        }

        float lift = (HemOffset(u, seedPhase) - minHem) * Mathf.SmoothStep(0f, 1f, v);
        // Exact width; highest point is the attachment and lowest point is -height.
        return new Vector3((u - 0.5f) * span, -v * height + lift, z);
    }

    private float HemOffset(float u, float seedPhase)
    {
        float wave = 0.5f + 0.5f * Mathf.Sin(u * foldCount * TwoPi + seedPhase);
        return wave * Mathf.Min(hemLift, height * 0.03f) * naturalness;
    }

    private float RowFraction(int row, int rows)
    {
        // Reserve a few rows for the narrow hem; a uniform grid can skip it entirely.
        if (hemWidth <= 0f || rows < 8)
            return (float)row / rows;
        int bodyRows = rows - 3;
        float hemStart = 1f - Mathf.Min(hemWidth, height * 0.25f) / height;
        return row <= bodyRows
            ? (float)row / bodyRows * hemStart
            : Mathf.Lerp(hemStart, 1f, (float)(row - bodyRows) / 3f);
    }

    private bool EnsureMesh(int columns, int rows)
    {
        bool created = generatedMesh == null;
        if (created)
        {
            generatedMesh = new Mesh
            {
                name = "Procedural Curtain Mesh",
                hideFlags = HideFlags.HideAndDontSave
            };
            generatedMesh.MarkDynamic();
        }
        if (!created && cachedColumns == columns && cachedRows == rows)
            return false;

        generatedMesh.Clear();
        cachedColumns = columns;
        cachedRows = rows;
        vertices = new Vector3[(columns + 1) * (rows + 1)];
        uvs = new Vector2[vertices.Length];
        previousRestRow = new Vector3[columns + 1];
        verticalLengths = new float[columns + 1];
        triangles = new int[columns * rows * 6];
        int triangle = 0;
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                int index = y * (columns + 1) + x;
                triangles[triangle++] = index;
                triangles[triangle++] = index + 1;
                triangles[triangle++] = index + columns + 1;
                triangles[triangle++] = index + 1;
                triangles[triangle++] = index + columns + 2;
                triangles[triangle++] = index + columns + 1;
            }
        }
        return true;
    }

    private void ClampSettings()
    {
        width = Mathf.Clamp(width, 0.005f, 100f);
        height = Mathf.Clamp(height, 0.1f, 100f);
        foldCount = Mathf.Clamp(foldCount, 1, 128);
        foldDepth = Mathf.Clamp(foldDepth, 0f, 1f);
        foldProfile = Mathf.Clamp(foldProfile, 0.5f, 2f);
        foldVariation = Mathf.Clamp(foldVariation, 0f, 0.5f);
        drape = Mathf.Clamp01(drape);
        wrinkleStrength = Mathf.Clamp01(wrinkleStrength);
        naturalness = Mathf.Clamp01(naturalness);
        edgeFade = Mathf.Clamp(edgeFade, 0f, 0.25f);
        hemWidth = Mathf.Clamp(hemWidth, 0f, 0.2f);
        hemLift = Mathf.Clamp(hemLift, 0f, 0.03f);
        referenceWidth = Mathf.Clamp(referenceWidth, 0f, 100f);
        textureRepeatsPerMeter = Mathf.Max(0.01f, textureRepeatsPerMeter);
        segmentsPerFold = Mathf.Clamp(segmentsPerFold, 8, 32);
        verticalSegments = Mathf.Clamp(verticalSegments, 4, 128);
        segmentsPerMeter = Mathf.Clamp(segmentsPerMeter, 4, 32);
    }

    private void OnDisable()
    {
        ReleaseMesh();
    }

    private void OnDestroy()
    {
        ReleaseMesh();
    }

    private void ReleaseMesh()
    {
        if (generatedMesh == null)
            return;
        if (meshFilter != null && meshFilter.sharedMesh == generatedMesh)
            meshFilter.sharedMesh = null;
        if (Application.isPlaying)
            Destroy(generatedMesh);
        else
            DestroyImmediate(generatedMesh);
        generatedMesh = null;
    }
}
