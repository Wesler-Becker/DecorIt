
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralCurtainMesh : MonoBehaviour
{
    [Header("Dimensions (meters)")]
    [Min(0.1f)]
    [SerializeField] private float width = 1.8f;

    [Min(0.1f)]
    [SerializeField] private float height = 2.2f;

    [Header("Folds")]
    [Min(1)]
    [SerializeField] private int foldCount = 8;

    [Min(0f)]
    [SerializeField] private float foldDepth = 0.06f;

    [Range(0.5f, 2f)]
    [SerializeField] private float foldProfile = 1f;

    [Header("Edge finishing")]
    [Range(0f, 0.25f)]
    [SerializeField] private float edgeFade = 0.08f;

    [Header("Mesh resolution")]
    [Min(4)]
    [SerializeField] private int segmentsPerFold = 16;

    [Min(1)]
    [SerializeField] private int verticalSegments = 20;

    private Mesh generatedMesh;

    private void Start()
    {
        GenerateMesh();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Não reconstruir malhas enquanto o projeto está fora do Play.
        if (!Application.isPlaying)
            return;

        GenerateMesh();
    }
#endif

    public void Configure(
        float newWidth,
        float newHeight,
        int newFoldCount,
        float newFoldDepth,
        float newFoldProfile)
    {
        width = Mathf.Max(0.1f, newWidth);
        height = Mathf.Max(0.1f, newHeight);

        foldCount = Mathf.Max(1, newFoldCount);
        foldDepth = Mathf.Max(0f, newFoldDepth);
        foldProfile = Mathf.Clamp(newFoldProfile, 0.5f, 2f);

        GenerateMesh();
    }

    public void GenerateMesh()
    {
        int columns = Mathf.Max(4, foldCount * segmentsPerFold);
        int rows = Mathf.Max(1, verticalSegments);

        Vector3[] vertices =
            new Vector3[(columns + 1) * (rows + 1)];

        Vector2[] uvs =
            new Vector2[vertices.Length];

        int[] triangles =
            new int[columns * rows * 6];

        // 1. Gera os vértices do tecido.
        for (int y = 0; y <= rows; y++)
        {
            float v = (float)y / rows;

            for (int x = 0; x <= columns; x++)
            {
                float u = (float)x / columns;

                // Origem no centro superior do painel.
                float positionX = (u - 0.5f) * width;
                float positionY = -v * height;

                // Gera ondulações no eixo Z.
                float phase =
                    u * foldCount * Mathf.PI * 2f;

                float sine = Mathf.Sin(phase);

                float fold =
                    Mathf.Sign(sine) *
                    Mathf.Pow(Mathf.Abs(sine), foldProfile);

                
                float edgeFactor = 1f;

                if (edgeFade > 0f)
                {
                    // Suaviza as ondas nas duas extremidades.
                    float leftFade = Mathf.SmoothStep(
                        0f,
                        1f,
                        Mathf.Clamp01(u / edgeFade)
                    );

                    float rightFade = Mathf.SmoothStep(
                        0f,
                        1f,
                        Mathf.Clamp01((1f - u) / edgeFade)
                    );

                    edgeFactor = Mathf.Min(leftFade, rightFade);
                }

                float positionZ = fold * foldDepth * edgeFactor;

                int index = y * (columns + 1) + x;

                vertices[index] = new Vector3(
                    positionX,
                    positionY,
                    positionZ
                );

                uvs[index] = new Vector2(u, v);
            }
        }

        // 2. Conecta os vértices em triângulos.
        int triangleIndex = 0;

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                int index = y * (columns + 1) + x;

                triangles[triangleIndex++] = index;
                triangles[triangleIndex++] = index + 1;
                triangles[triangleIndex++] = index + columns + 1;

                triangles[triangleIndex++] = index + 1;
                triangles[triangleIndex++] = index + columns + 2;
                triangles[triangleIndex++] = index + columns + 1;
            }
        }

        // 3. Descarta a malha anterior.
        if (generatedMesh != null)
        {
            // Libera a malha anterior ao reconstruir o tecido.
            Destroy(generatedMesh);
            generatedMesh = null;
        }

        // 4. Cria e configura a nova malha.
        generatedMesh = new Mesh();
        generatedMesh.name = "Procedural Curtain Mesh";

        generatedMesh.vertices = vertices;
        generatedMesh.uv = uvs;
        generatedMesh.triangles = triangles;

        generatedMesh.RecalculateNormals();
        generatedMesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = generatedMesh;
    }

    private void OnDestroy()
    {
        if (generatedMesh != null)
        {
            Destroy(generatedMesh);
        }
    }
}