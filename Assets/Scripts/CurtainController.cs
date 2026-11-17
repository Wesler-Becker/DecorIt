using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class CurtainController : MonoBehaviour
{
    public enum CurtainType
    {
        SinglePanel,
        DoublePanel
    }

    public enum FabricStyle
    {
        Personalizado,
        Liso,
        Natural,
        Wave
    }

    [Header("Fabric style")]
    [Tooltip("Personalizado usa Fold Settings. Os outros estilos definem a densidade e profundidade das ondas.")]
    [SerializeField] private FabricStyle fabricStyle = FabricStyle.Personalizado;
    [Tooltip("Intensidade do caimento natural, das rugas e da variacao da barra.")]
    [Range(0f, 1f)] [SerializeField] private float fabricNaturalness = 1f;
    [SerializeField] private int fabricSeed = 17;

    [Header("Curtain Type")]
    [SerializeField] private CurtainType curtainType = CurtainType.DoublePanel;

    [Header("Total Dimensions (cm)")]
    [Min(10f)] [SerializeField] private float widthCm = 240f;
    [Min(10f)] [SerializeField] private float heightCm = 220f;

    [Header("Fold Settings")]
    [Min(1f)] [SerializeField] private float foldsPerMeter = 5f;
    [Min(0f)] [SerializeField] private float foldDepth = 0.06f;
    [Range(0.5f, 2f)] [SerializeField] private float foldProfile = 1f;

    [Header("Opening")]
    [Range(0f, 1f)] [SerializeField] private float opening = 0f;

    [Header("Curtain Rod")]
    [SerializeField] private Transform rodBody;
    [SerializeField] private Transform leftFinial;
    [SerializeField] private Transform rightFinial;

    [Header("Rod Settings (meters)")]
    [SerializeField] private float rodExtraWidth = 0.10f;
    [SerializeField] private float rodDiameter = 0.025f;
    [SerializeField] private float finialSize = 0.05f;
    [SerializeField] private float rodHeightOffset = 0.06f;

    [Header("Panels")]
    [SerializeField] private ProceduralCurtainMesh singlePanel;
    [SerializeField] private ProceduralCurtainMesh leftPanel;
    [SerializeField] private ProceduralCurtainMesh rightPanel;

    [Header("Layer spacing")]
    [Min(0f)] [SerializeField] private float layerSpacingCm = 6f;
    private string supportType = "Rod";
    private bool doubleSupport;
    private float secondaryOpening;
    private Mesh rodMesh;
    private Mesh railMesh;
    private Transform secondRod;
    private Transform secondLeftFinial;
    private Transform secondRightFinial;
    private readonly List<Transform> railHooks = new List<Transform>();
    private int usedRailHooks;
    public const float MinimumLayerClearanceMillimeters = 2f;

    private sealed class PanelGroup
    {
        public ProceduralCurtainMesh single;
        public ProceduralCurtainMesh left;
        public ProceduralCurtainMesh right;
        public float minDepth;
        public float maxDepth;
        public float offset;
        public float clearanceMillimeters;
        public bool generated;

        public PanelGroup(ProceduralCurtainMesh singlePanel, ProceduralCurtainMesh leftPanel,
            ProceduralCurtainMesh rightPanel, bool isGenerated)
        {
            single = singlePanel;
            left = leftPanel;
            right = rightPanel;
            generated = isGenerated;
        }
    }

    private readonly List<PanelGroup> panelGroups = new List<PanelGroup>();
    private readonly List<CurtainLayerViewSettings> layerSettings = new List<CurtainLayerViewSettings>();
    private bool updateRequested;

    private void OnEnable()
    {
        updateRequested = true;
    }

    private void Update()
    {
        if (updateRequested) UpdateCurtain();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        updateRequested = true;
    }
#endif

    [ContextMenu("Atualizar cortina")]
    public void UpdateCurtain()
    {
        updateRequested = false;
        EnsureBaseLayerSettings();
        EnsurePanelGroups(layerSettings.Count);
        if (panelGroups.Count == 0 ||
            (curtainType == CurtainType.SinglePanel && panelGroups[0].single == null) ||
            (curtainType == CurtainType.DoublePanel &&
             (panelGroups[0].left == null || panelGroups[0].right == null)))
        {
            Debug.LogWarning("CurtainController: panel references are missing.");
            return;
        }

        opening = Mathf.Clamp01(opening);

        for (int i = 0; i < panelGroups.Count; i++)
        {
            PanelGroup group = panelGroups[i];
            bool isUsed = i < layerSettings.Count;
            bool single = isUsed && layerSettings[i].panelType == "SinglePanel";
            if (group.single != null) group.single.gameObject.SetActive(isUsed && single);
            if (group.left != null) group.left.gameObject.SetActive(isUsed && !single);
            if (group.right != null) group.right.gameObject.SetActive(isUsed && !single);
            if (!isUsed) continue;

            CurtainLayerViewSettings layer = layerSettings[i];
            float layerOpening = doubleSupport && layer.supportIndex == 1 ? secondaryOpening : opening;
            ConfigureLayer(group, layer.widthCentimeters / 100f, layer.heightCentimeters / 100f,
                layer, layerOpening, i * 101);
            Bounds bounds = single ? group.single.LocalMeshBounds : group.left.LocalMeshBounds;
            if (!single) bounds.Encapsulate(group.right.LocalMeshBounds);
            group.minDepth = bounds.min.z;
            group.maxDepth = bounds.max.z;
        }

        // Spacing is measured between layer center planes. Each neighboring mesh
        // receives half the available depth, leaving a small clearance at contact.
        PanelGroup previous = null;
        var orderedGroups = new List<PanelGroup>();
        for (int support = 0; support < (doubleSupport ? 2 : 1); support++)
        {
            for (int i = 0; i < layerSettings.Count; i++)
            {
                CurtainLayerViewSettings layer = layerSettings[i];
                if ((doubleSupport ? layer.supportIndex : 0) != support) continue;
                PanelGroup group = panelGroups[i];
                group.clearanceMillimeters = previous == null ? 0f :
                    Mathf.Max(MinimumLayerClearanceMillimeters, layer.spacingMillimeters);
                group.offset = previous == null ? 0f : previous.offset - group.clearanceMillimeters / 1000f;
                SetLayerDepth(group, group.offset);
                orderedGroups.Add(group);
                previous = group;
            }
        }
        for (int i = 0; i < orderedGroups.Count; i++)
        {
            float available = float.PositiveInfinity;
            if (i > 0) available = Mathf.Min(available, orderedGroups[i].clearanceMillimeters);
            if (i + 1 < orderedGroups.Count) available = Mathf.Min(available, orderedGroups[i + 1].clearanceMillimeters);
            if (float.IsPositiveInfinity(available)) continue;
            float limit = Mathf.Max(0f, (available - 0.1f) / 2000f);
            PanelGroup group = orderedGroups[i];
            if (group.single != null && group.single.gameObject.activeSelf) group.single.LimitDepth(limit);
            if (group.left != null && group.left.gameObject.activeSelf) group.left.LimitDepth(limit);
            if (group.right != null && group.right.gameObject.activeSelf) group.right.LimitDepth(limit);
        }

        ApplyFirstLayerToLegacyFields();
        UpdateSupports();
        UpdateRailHooks();
    }

    public float GetLayerClearanceMillimeters(int index) =>
        index >= 0 && index < panelGroups.Count ? panelGroups[index].clearanceMillimeters : 0f;

    private void ConfigureLayer(PanelGroup group, float width, float height,
        CurtainLayerViewSettings layer, float layerOpening, int seedOffset)
    {
        float density;
        float depth;
        float profile;
        float naturalness;
        GetStyleValues(layer, out density, out depth, out profile, out naturalness);

        if (layer.panelType == "SinglePanel")
        {
            float visibleWidth = width * Mathf.Lerp(1f, 0.15f, layerOpening);
            group.single.transform.localPosition = new Vector3((visibleWidth - width) / 2f, 0f, 0f);
            ConfigurePanel(group.single, visibleWidth, height, width, density, depth,
                profile, naturalness, layer.seed + seedOffset);
            return;
        }

        float originalPanelWidth = width / 2f;
        float visibleWidthFactor = Mathf.Lerp(1f, 0.15f, layerOpening);
        float visiblePanelWidth = originalPanelWidth * visibleWidthFactor;
        float leftPosition = -width / 2f + visiblePanelWidth / 2f;
        float rightPosition = width / 2f - visiblePanelWidth / 2f;
        group.left.transform.localPosition = new Vector3(leftPosition, 0f, 0f);
        group.right.transform.localPosition = new Vector3(rightPosition, 0f, 0f);

        ConfigurePanel(group.left, visiblePanelWidth, height, originalPanelWidth,
            density, depth, profile, naturalness, layer.seed + seedOffset);
        ConfigurePanel(group.right, visiblePanelWidth, height, originalPanelWidth,
            density, depth, profile, naturalness, layer.seed + seedOffset + 101);
    }

    private static void GetStyleValues(CurtainLayerViewSettings layer, out float density,
        out float depth, out float profile, out float naturalness)
    {
        density = Mathf.Clamp(layer.foldsPerMeter, 1f, 32f);
        depth = Mathf.Clamp(layer.foldDepthCentimeters / 100f, 0f, 1f);
        profile = Mathf.Clamp(layer.foldProfile, 0.5f, 2f);
        naturalness = Mathf.Clamp01(layer.naturalness);
        switch (layer.fabricStyle)
        {
            case "Liso":
                density = 3f;
                depth = 0.018f;
                profile = 1f;
                naturalness *= 0.65f;
                break;
            case "Natural":
                density = 5f;
                depth = 0.06f;
                profile = 1.15f;
                break;
            case "Wave":
                density = 6f;
                depth = 0.095f;
                profile = 0.8f;
                naturalness *= 0.45f;
                break;
        }
    }

    private static void ConfigurePanel(ProceduralCurtainMesh panel, float visibleWidth,
        float panelHeight, float closedWidth, float density, float depth, float profile,
        float naturalness, int seedOffset)
    {
        if (panel == null) return;
        int count = Mathf.Clamp(Mathf.RoundToInt(closedWidth * density), 1, 128);
        panel.Configure(visibleWidth, panelHeight, count, depth, profile, closedWidth,
            seedOffset, naturalness);

    }

    private void SetLayerDepth(PanelGroup group, float depth)
    {
        SetPanelDepth(group.single, depth);
        SetPanelDepth(group.left, depth);
        SetPanelDepth(group.right, depth);
    }

    private static void SetPanelDepth(ProceduralCurtainMesh panel, float depth)
    {
        if (panel == null) return;
        Vector3 position = panel.transform.localPosition;
        position.z = depth;
        panel.transform.localPosition = position;
    }

    private void EnsureBaseLayerSettings()
    {
        if (layerSettings.Count > 0) return;
        layerSettings.Add(new CurtainLayerViewSettings
        {
            widthCentimeters = widthCm,
            heightCentimeters = heightCm,
            panelType = curtainType.ToString(),
            spacingMillimeters = layerSpacingCm * 10f,
            fabricStyle = fabricStyle.ToString(),
            foldsPerMeter = foldsPerMeter,
            foldDepthCentimeters = foldDepth * 100f,
            foldProfile = foldProfile,
            naturalness = fabricNaturalness,
            seed = fabricSeed
        });
    }

    private void EnsurePanelGroups(int count)
    {
        if (panelGroups.Count == 0)
            panelGroups.Add(new PanelGroup(singlePanel, leftPanel, rightPanel, false));

        while (panelGroups.Count < count)
        {
            int layerIndex = panelGroups.Count;
            PanelGroup group = new PanelGroup(
                ClonePanel(singlePanel, "SinglePanel_Layer" + (layerIndex + 1)),
                ClonePanel(leftPanel, "LeftPanel_Layer" + (layerIndex + 1)),
                ClonePanel(rightPanel, "RightPanel_Layer" + (layerIndex + 1)), true);
            panelGroups.Add(group);
        }
    }

    private ProceduralCurtainMesh ClonePanel(ProceduralCurtainMesh source, string cloneName)
    {
        if (source == null) return null;
        GameObject clone = Instantiate(source.gameObject, source.transform.parent);
        clone.name = cloneName;
        if (!Application.isPlaying) clone.hideFlags = HideFlags.HideAndDontSave;
        return clone.GetComponent<ProceduralCurtainMesh>();
    }

    private void ApplyFirstLayerToLegacyFields()
    {
        if (layerSettings.Count == 0) return;
        CurtainLayerViewSettings front = layerSettings[0];
        widthCm = front.widthCentimeters;
        heightCm = front.heightCentimeters;
        if (System.Enum.TryParse(front.panelType, out CurtainType type)) curtainType = type;
        if (System.Enum.TryParse(front.fabricStyle, out FabricStyle style)) fabricStyle = style;
        fabricNaturalness = front.naturalness;
        fabricSeed = front.seed;
        foldsPerMeter = front.foldsPerMeter;
        foldDepth = front.foldDepthCentimeters / 100f;
        foldProfile = front.foldProfile;
    }

    public void SetLayerMaterial(int layerIndex, Material material)
    {
        if (material == null || layerIndex < 0) return;
        EnsureBaseLayerSettings();
        EnsurePanelGroups(layerIndex + 1);
        PanelGroup group = panelGroups[layerIndex];
        ApplyMaterial(group.single, material);
        ApplyMaterial(group.left, material);
        ApplyMaterial(group.right, material);
    }

    private static void ApplyMaterial(ProceduralCurtainMesh panel, Material material)
    {
        if (panel == null) return;
        MeshRenderer renderer = panel.GetComponent<MeshRenderer>();
        if (renderer != null) renderer.sharedMaterial = material;
    }

    public void SetFabricStyle(FabricStyle style)
    {
        EnsureBaseLayerSettings();
        layerSettings[0].fabricStyle = style.ToString();
        ApplyFirstLayerToLegacyFields();
        UpdateCurtain();
    }

    public void ApplySettings(CurtainViewSettings settings)
    {
        if (settings == null) return;
        settings.EnsureLayers();
        widthCm = Mathf.Clamp(settings.widthCentimeters, 10f, 10000f);
        heightCm = Mathf.Clamp(settings.heightCentimeters, 10f, 10000f);
        if (System.Enum.TryParse(settings.panelType, out CurtainType savedType)) curtainType = savedType;
        opening = Mathf.Clamp01(settings.opening);
        secondaryOpening = settings.secondaryOpening;
        supportType = settings.supportType;
        doubleSupport = settings.doubleSupport;
        layerSettings.Clear();
        foreach (CurtainLayerViewSettings layer in settings.layers)
            layerSettings.Add(layer.Clone());
        ApplyFirstLayerToLegacyFields();
        UpdateCurtain();

        for (int i = 0; i < layerSettings.Count; i++)
        {
            CurtainLayerViewSettings layer = layerSettings[i];
            Material material = CurtainViewSettingsStore.ResolveFabric(layer.fabricMaterialPath, layer.fabricMaterialName);
            if (material != null) SetLayerMaterial(i, material);
        }
    }

    public void SetDimensions(float newWidthCm, float newHeightCm)
    {
        EnsureBaseLayerSettings();
        layerSettings[0].widthCentimeters = Mathf.Clamp(newWidthCm, 10f, 10000f);
        layerSettings[0].heightCentimeters = Mathf.Clamp(newHeightCm, 10f, 10000f);
        UpdateCurtain();
    }

    public void SetCurtainType(CurtainType newType)
    {
        EnsureBaseLayerSettings();
        layerSettings[0].panelType = newType.ToString();
        UpdateCurtain();
    }

    public void SetFoldSettings(float newFoldsPerMeter, float newFoldDepth, float newFoldProfile)
    {
        EnsureBaseLayerSettings();
        layerSettings[0].fabricStyle = FabricStyle.Personalizado.ToString();
        layerSettings[0].foldsPerMeter = Mathf.Max(1f, newFoldsPerMeter);
        layerSettings[0].foldDepthCentimeters = Mathf.Max(0f, newFoldDepth) * 100f;
        layerSettings[0].foldProfile = Mathf.Clamp(newFoldProfile, 0.5f, 2f);
        ApplyFirstLayerToLegacyFields();
        UpdateCurtain();
    }

    public void SetOpening(float value)
    {
        opening = Mathf.Clamp01(value);
        UpdateCurtain();
    }

    private void UpdateSupports()
    {
        if (rodBody == null) return;
        if (rodMesh == null) rodMesh = rodBody.GetComponent<MeshFilter>().sharedMesh;
        if (supportType == "Rail" && railMesh == null)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.SetActive(false);
            railMesh = cube.GetComponent<MeshFilter>().sharedMesh;
            if (Application.isPlaying) Destroy(cube); else DestroyImmediate(cube);
        }
        if (doubleSupport && secondRod == null)
        {
            secondRod = CloneSupport(rodBody, "RodBody_2");
            secondLeftFinial = CloneSupport(leftFinial, "LeftFinial_2");
            secondRightFinial = CloneSupport(rightFinial, "RightFinial_2");
        }
        float previousSupportDepth = 0f;
        for (int support = 0; support < 2; support++)
        {
            Transform body = support == 0 ? rodBody : secondRod;
            Transform left = support == 0 ? leftFinial : secondLeftFinial;
            Transform right = support == 0 ? rightFinial : secondRightFinial;
            if (body == null) continue;
            bool enabled = support == 0 || doubleSupport;
            body.gameObject.SetActive(enabled);
            if (left != null) left.gameObject.SetActive(enabled && supportType == "Rod");
            if (right != null) right.gameObject.SetActive(enabled && supportType == "Rod");
            if (!enabled) continue;
            float width = 0f;
            float depth = 0f;
            int count = 0;
            for (int i = 0; i < layerSettings.Count; i++)
            {
                if ((doubleSupport ? layerSettings[i].supportIndex : 0) != support) continue;
                width = Mathf.Max(width, layerSettings[i].widthCentimeters / 100f);
                depth += panelGroups[i].offset;
                count++;
            }
            depth = count > 0 ? depth / count : support == 0 ? 0.12f : previousSupportDepth - 0.12f;
            UpdateRod(body, left, right, width > 0f ? width : widthCm / 100f, depth);
            previousSupportDepth = depth;
        }
    }

    private Transform CloneSupport(Transform source, string partName)
    {
        if (source == null) return null;
        Transform clone = Instantiate(source, source.parent);
        clone.name = partName;
        if (!Application.isPlaying) clone.gameObject.hideFlags = HideFlags.HideAndDontSave;
        return clone;
    }

    private void UpdateRailHooks()
    {
        usedRailHooks = 0;
        if (supportType == "Rail" && rodBody != null && rodMesh != null)
        {
            for (int i = 0; i < layerSettings.Count; i++)
            {
                CurtainLayerViewSettings layer = layerSettings[i];
                Transform rail = doubleSupport && layer.supportIndex == 1 ? secondRod : rodBody;
                if (rail == null) continue;
                PanelGroup group = panelGroups[i];
                float closedWidth = layer.widthCentimeters / 100f;
                AddPanelRailHooks(group.single, rail, layer.foldsPerMeter, closedWidth);
                AddPanelRailHooks(group.left, rail, layer.foldsPerMeter, closedWidth / 2f);
                AddPanelRailHooks(group.right, rail, layer.foldsPerMeter, closedWidth / 2f);
            }
        }
        for (int i = usedRailHooks; i < railHooks.Count; i++)
            if (railHooks[i] != null) railHooks[i].gameObject.SetActive(false);
    }

    private void AddPanelRailHooks(ProceduralCurtainMesh panel, Transform rail, float density, float closedWidth)
    {
        if (panel == null || !panel.gameObject.activeSelf) return;
        int count = Mathf.Clamp(Mathf.CeilToInt(closedWidth * density) + 1, 2, 32);
        for (int i = 0; i < count; i++)
        {
            Transform hook;
            if (usedRailHooks < railHooks.Count) hook = railHooks[usedRailHooks];
            else
            {
                var item = new GameObject("RodBody_RailHook", typeof(MeshFilter), typeof(MeshRenderer));
                item.layer = gameObject.layer;
                if (!Application.isPlaying) item.hideFlags = HideFlags.HideAndDontSave;
                item.GetComponent<MeshFilter>().sharedMesh = rodMesh;
                hook = item.transform;
                railHooks.Add(hook);
            }
            usedRailHooks++;
            hook.SetParent(panel.transform, false);
            hook.gameObject.SetActive(true);
            hook.gameObject.layer = panel.gameObject.layer;
            hook.GetComponent<MeshRenderer>().sharedMaterial = rail.GetComponent<MeshRenderer>().sharedMaterial;
            Vector3 start = panel.GetTopPoint((float)i / (count - 1));
            Vector3 railPoint = panel.transform.InverseTransformPoint(rail.TransformPoint(new Vector3(
                0f, -0.5f, 0f)));
            Vector3 end = new Vector3(start.x, railPoint.y, railPoint.z);
            Vector3 delta = end - start;
            hook.localPosition = (start + end) * 0.5f;
            hook.localRotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
            hook.localScale = new Vector3(0.006f, delta.magnitude * 0.5f, 0.006f);
        }
    }

    private void UpdateRod(Transform body, Transform left, Transform right, float curtainWidth, float depth)
    {
        float rodLength = curtainWidth + rodExtraWidth;
        bool rail = supportType == "Rail";
        body.GetComponent<MeshFilter>().sharedMesh = rail ? railMesh : rodMesh;
        body.localScale = rail ? new Vector3(rodLength, rodDiameter, rodDiameter) :
            new Vector3(rodDiameter, rodLength / 2f, rodDiameter);
        body.localRotation = rail ? Quaternion.identity : Quaternion.Euler(0f, 0f, 90f);
        body.localPosition = new Vector3(0f, rodHeightOffset, depth);
        float finialOffset = rodLength / 2f + finialSize / 2f;
        if (left == null || right == null) return;
        left.localPosition = new Vector3(-finialOffset, rodHeightOffset, depth);
        right.localPosition = new Vector3(finialOffset, rodHeightOffset, depth);
        Vector3 finialScale = new Vector3(finialSize, finialSize, finialSize);
        left.localScale = finialScale;
        right.localScale = finialScale;
    }

    private void OnDestroy()
    {
        for (int i = panelGroups.Count - 1; i > 0; i--)
        {
            PanelGroup group = panelGroups[i];
            DestroyGeneratedPanel(group.single);
            DestroyGeneratedPanel(group.left);
            DestroyGeneratedPanel(group.right);
        }
        panelGroups.Clear();
        DestroyGeneratedSupport(secondRod);
        DestroyGeneratedSupport(secondLeftFinial);
        DestroyGeneratedSupport(secondRightFinial);
        foreach (Transform hook in railHooks) DestroyGeneratedSupport(hook);
        railHooks.Clear();
    }

    private static void DestroyGeneratedSupport(Transform part)
    {
        if (part == null) return;
        if (Application.isPlaying) Destroy(part.gameObject); else DestroyImmediate(part.gameObject);
    }

    private static void DestroyGeneratedPanel(ProceduralCurtainMesh panel)
    {
        if (panel == null) return;
        if (Application.isPlaying) Destroy(panel.gameObject);
        else DestroyImmediate(panel.gameObject);
    }
}
