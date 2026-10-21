using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public sealed class CatalogScreenController : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    [SerializeField] private GameObject curtainPrefab;
    [SerializeField] private Material defaultFabric;
    [SerializeField] private Material defaultRod;

    private readonly List<CurtainMaterialCatalog.Entry> materials = new List<CurtainMaterialCatalog.Entry>();
    private readonly List<CurtainMaterialCatalog.Entry> filteredMaterials = new List<CurtainMaterialCatalog.Entry>();
    private readonly List<Button> materialButtons = new List<Button>();
    private CurtainViewSettings working;
    private CurtainMaterialCatalog.Entry selectedMaterial;
    private Material selectedRod;
    private GameObject previewRoot;
    private GameObject previewObject;
    private Camera previewCamera;
    private RenderTexture previewTexture;
    private VisualElement root;
    private ScrollView scroll;
    private Image previewImage;
    private TextField widthField;
    private TextField heightField;
    private TextField materialSearch;
    private VisualElement materialGrid;
    private Label materialPage;
    private Label selectionSummary;
    private Label statusLabel;
    private VisualElement customCard;
    private readonly Label[] sliderLabels = new Label[5];
    private readonly Slider[] sliders = new Slider[5];
    private Button singlePanel;
    private Button doublePanel;
    private readonly Button[] styleButtons = new Button[4];
    private Button previousMaterials;
    private Button nextMaterials;
    private int materialPageIndex;
    private const int MaterialPageSize = 9;

    private void Start()
    {
        if (document == null) document = GetComponent<UIDocument>();
        root = document.rootVisualElement;
        root.RegisterCallback<GeometryChangedEvent>(_ => ApplySafeAreaInsets());
        CacheElements();
        LoadMaterials();
        working = CatalogSelectionSession.CurrentConfiguration != null
            ? CurtainViewSettingsStore.Clone(CatalogSelectionSession.CurrentConfiguration)
            : CurtainViewSettingsStore.LoadLast();

        VisitDraft draft = VisitDraftSession.Current;
        // A draft contains the dimensions of the current visit/record. When this
        // catalog is opened from AR, however, the last-view settings are the
        // source of truth; the host draft may still contain dimensions from an
        // older record (for example 15 x 20 cm).
        if (CatalogSelectionSession.CurrentConfiguration == null &&
            draft != null && draft.widthCentimeters > 0f && draft.heightCentimeters > 0f)
        {
            working.widthCentimeters = draft.widthCentimeters;
            working.heightCentimeters = draft.heightCentimeters;
        }

        ConfigureSelectionFromWorking();
        CreatePreview();
        BindEvents();
        RefreshControls();
        RefreshMaterials();
        RefreshPreview();
    }

    private void ApplySafeAreaInsets()
    {
        if (root == null || Screen.height <= 0 || root.layout.height <= 0f) return;
        Rect safeArea = Screen.safeArea;
        float pointsPerPixel = root.layout.height / Screen.height;
        root.style.paddingTop = Mathf.Max(0f, Screen.height - safeArea.yMax) * pointsPerPixel;
        root.style.paddingBottom = Mathf.Max(0f, safeArea.yMin) * pointsPerPixel;
    }

    private void CacheElements()
    {
        scroll = root.Q<ScrollView>("configurationScroll");
        previewImage = root.Q<Image>("previewImage");
        widthField = root.Q<TextField>("widthField");
        heightField = root.Q<TextField>("heightField");
        materialSearch = root.Q<TextField>("materialSearch");
        materialGrid = root.Q<VisualElement>("materialGrid");
        materialPage = root.Q<Label>("materialPage");
        selectionSummary = root.Q<Label>("selectionSummary");
        statusLabel = root.Q<Label>("statusLabel");
        customCard = root.Q<VisualElement>("customCard");
        singlePanel = root.Q<Button>("singlePanel");
        doublePanel = root.Q<Button>("doublePanel");
        previousMaterials = root.Q<Button>("previousMaterials");
        nextMaterials = root.Q<Button>("nextMaterials");
        styleButtons[0] = root.Q<Button>("styleSmooth");
        styleButtons[1] = root.Q<Button>("styleNatural");
        styleButtons[2] = root.Q<Button>("styleWave");
        styleButtons[3] = root.Q<Button>("styleCustom");
        string[] sliderNames = { "folds", "depth", "profile", "naturalness", "opening" };
        for (int i = 0; i < sliderNames.Length; i++)
        {
            sliders[i] = root.Q<Slider>(sliderNames[i] + "Slider");
            sliderLabels[i] = root.Q<Label>(sliderNames[i] + "Value");
        }
    }

    private void LoadMaterials()
    {
        materials.Clear();
        CurtainMaterialCatalog catalog = Resources.Load<CurtainMaterialCatalog>("CurtainMaterials");
        if (catalog != null && catalog.entries != null)
            foreach (CurtainMaterialCatalog.Entry entry in catalog.entries)
                if (entry != null && entry.material != null) materials.Add(entry);
        materials.Sort((a, b) => string.Compare(a.displayName, b.displayName, System.StringComparison.OrdinalIgnoreCase));
    }

    private void ConfigureSelectionFromWorking()
    {
        selectedMaterial = FindMaterial(working.fabricMaterialPath, working.fabricMaterialName);
        selectedRod = defaultRod;
        if (selectedMaterial == null && materials.Count > 0) selectedMaterial = materials[0];
        if (selectedMaterial != null)
        {
            working.fabricMaterialPath = selectedMaterial.assetPath;
            working.fabricMaterialName = selectedMaterial.displayName;
        }
        if (string.IsNullOrEmpty(working.rodMaterialName)) working.rodMaterialName = defaultRod != null ? defaultRod.name : "RodMaterial";
        if (string.IsNullOrEmpty(working.rodMaterialPath) && defaultRod != null) working.rodMaterialPath = "Assets/Materials/RodMaterial.mat";
    }

    private void BindEvents()
    {
        root.Q<Button>("backButton").clicked += Back;
        root.Q<Button>("applyButton").clicked += ApplySelection;
        singlePanel.clicked += () => SetPanelType("SinglePanel");
        doublePanel.clicked += () => SetPanelType("DoublePanel");
        for (int i = 0; i < styleButtons.Length; i++)
        {
            int styleIndex = i;
            styleButtons[i].clicked += () => SetStyle(styleIndex);
        }
        widthField.isDelayed = true;
        heightField.isDelayed = true;
        widthField.RegisterValueChangedCallback(_ => ReadDimensions());
        heightField.RegisterValueChangedCallback(_ => ReadDimensions());
        materialSearch.RegisterValueChangedCallback(_ => { materialPageIndex = 0; RefreshMaterials(); });
        previousMaterials.clicked += () => ChangeMaterialPage(-1);
        nextMaterials.clicked += () => ChangeMaterialPage(1);
        for (int i = 0; i < sliders.Length; i++)
        {
            int index = i;
            sliders[i].RegisterValueChangedCallback(evt => UpdateCustomValue(index, evt.newValue));
        }
    }

    private void CreatePreview()
    {
        if (curtainPrefab == null) { SetStatus("Não foi possível carregar o modelo da cortina."); return; }
        const int previewLayer = 2;
        previewRoot = new GameObject("CurtainCatalogPreview") { hideFlags = HideFlags.DontSave };
        previewRoot.layer = previewLayer;
        previewObject = Instantiate(curtainPrefab, Vector3.zero, Quaternion.identity);
        previewObject.name = "CurtainPreviewModel";
        previewObject.hideFlags = HideFlags.DontSave;
        SetLayerRecursively(previewObject.transform, previewLayer);

        GameObject cameraObject = new GameObject("CurtainCatalogPreviewCamera") { hideFlags = HideFlags.DontSave };
        cameraObject.transform.SetParent(previewRoot.transform, false);
        cameraObject.transform.position = new Vector3(1.2f, -1.05f, -6.3f);
        cameraObject.transform.LookAt(new Vector3(0f, -1.05f, 0f));
        previewCamera = cameraObject.AddComponent<Camera>();
        previewCamera.enabled = true;
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0.91f, 0.93f, 0.95f, 1f);
        previewCamera.fieldOfView = 26f;
        previewCamera.nearClipPlane = 0.05f;
        previewCamera.farClipPlane = 100f;
        previewCamera.cullingMask = 1 << previewLayer;
        previewCamera.allowHDR = false;
        previewCamera.allowMSAA = true;

        CreatePreviewLight("CurtainCatalogKeyLight", previewRoot.transform, previewLayer,
            Quaternion.Euler(35f, -25f, 0f), 1.35f);
        CreatePreviewLight("CurtainCatalogFillLight", previewRoot.transform, previewLayer,
            Quaternion.Euler(18f, 145f, 0f), 0.55f);
        previewTexture = new RenderTexture(768, 480, 16, RenderTextureFormat.ARGB32)
        {
            name = "Curtain Catalog Preview", hideFlags = HideFlags.DontSave, filterMode = FilterMode.Bilinear
        };
        previewTexture.Create();
        previewCamera.targetTexture = previewTexture;
        previewImage.image = previewTexture;
    }

    private static void CreatePreviewLight(string lightName, Transform parent, int layer, Quaternion rotation, float intensity)
    {
        GameObject lightObject = new GameObject(lightName, typeof(Light)) { hideFlags = HideFlags.DontSave };
        lightObject.transform.SetParent(parent, false);
        lightObject.layer = layer;
        lightObject.transform.rotation = rotation;
        Light light = lightObject.GetComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = intensity;
        light.cullingMask = 1 << layer;
    }

    private static void SetLayerRecursively(Transform node, int layer)
    {
        node.gameObject.layer = layer;
        for (int i = 0; i < node.childCount; i++) SetLayerRecursively(node.GetChild(i), layer);
    }

    private void ReadDimensions()
    {
        if (!TryPositive(widthField.value, out float width) || !TryPositive(heightField.value, out float height))
        {
            SetStatus("Informe largura e altura maiores que zero.");
            return;
        }
        working.widthCentimeters = Mathf.Clamp(width, 10f, 10000f);
        working.heightCentimeters = Mathf.Clamp(height, 10f, 10000f);
        SetStatus(string.Empty);
        RefreshPreview();
        PersistWorking();
    }

    private static bool TryPositive(string value, out float result) =>
        float.TryParse((value ?? string.Empty).Trim().Replace(',', '.'), NumberStyles.Float,
            CultureInfo.InvariantCulture, out result) && result > 0f;

    private void SetPanelType(string panelType)
    {
        working.panelType = panelType;
        RefreshControls();
        RefreshPreview();
        PersistWorking();
    }

    private void SetStyle(int index)
    {
        if (index == 3) working.fabricStyle = "Personalizado";
        else
        {
            working.fabricStyle = index == 0 ? "Liso" : index == 1 ? "Natural" : "Wave";
            working.foldsPerMeter = index == 0 ? 3f : index == 1 ? 5f : 6f;
            working.foldDepthCentimeters = index == 0 ? 1.8f : index == 1 ? 6f : 9.5f;
            working.foldProfile = index == 0 ? 1f : index == 1 ? 1.15f : 0.8f;
            working.naturalness = index == 2 ? 0.8f : 1f;
        }
        RefreshControls();
        RefreshPreview();
        PersistWorking();
    }

    private void UpdateCustomValue(int index, float value)
    {
        switch (index)
        {
            case 0: working.foldsPerMeter = value; break;
            case 1: working.foldDepthCentimeters = value; break;
            case 2: working.foldProfile = value; break;
            case 3: working.naturalness = value; break;
            case 4: working.opening = value; break;
        }
        working.fabricStyle = "Personalizado";
        RefreshControls();
        RefreshPreview();
        PersistWorking();
    }

    private void SelectMaterial(CurtainMaterialCatalog.Entry entry)
    {
        selectedMaterial = entry;
        working.fabricMaterialPath = entry.assetPath;
        working.fabricMaterialName = entry.displayName;
        RefreshMaterials();
        RefreshPreview();
        PersistWorking();
    }

    private void ChangeMaterialPage(int direction)
    {
        int pageCount = Mathf.Max(1, Mathf.CeilToInt(filteredMaterials.Count / (float)MaterialPageSize));
        materialPageIndex = (materialPageIndex + direction + pageCount) % pageCount;
        RefreshMaterials();
    }

    private void RefreshMaterials()
    {
        string query = materialSearch.value?.Trim() ?? string.Empty;
        filteredMaterials.Clear();
        foreach (CurtainMaterialCatalog.Entry entry in materials)
            if (string.IsNullOrEmpty(query) || entry.displayName.IndexOf(query, System.StringComparison.OrdinalIgnoreCase) >= 0)
                filteredMaterials.Add(entry);
        int pageCount = Mathf.Max(1, Mathf.CeilToInt(filteredMaterials.Count / (float)MaterialPageSize));
        materialPageIndex = Mathf.Clamp(materialPageIndex, 0, pageCount - 1);
        materialGrid.Clear();
        materialButtons.Clear();
        int first = materialPageIndex * MaterialPageSize;
        for (int i = first; i < Mathf.Min(first + MaterialPageSize, filteredMaterials.Count); i++)
        {
            CurtainMaterialCatalog.Entry entry = filteredMaterials[i];
            Button choice = new Button { name = "Material_" + i, text = string.Empty };
            choice.AddToClassList("material-choice");
            if (entry == selectedMaterial) choice.AddToClassList("selected");
            var swatch = new Image { name = "Swatch" };
            swatch.AddToClassList("material-swatch");
            Texture2D texture = GetMaterialTexture(entry.material);
            if (texture != null) swatch.image = texture;
            else swatch.style.backgroundColor = GetMaterialColor(entry.material);
            choice.Add(swatch);
            choice.Add(new Label(entry.displayName));
            choice.clicked += () => SelectMaterial(entry);
            materialGrid.Add(choice);
            materialButtons.Add(choice);
        }
        materialPage.text = filteredMaterials.Count == 0
            ? "Nenhum tecido encontrado"
            : $"{materialPageIndex + 1} / {pageCount} · {filteredMaterials.Count} tecidos";
    }

    private static Texture2D GetMaterialTexture(Material material)
    {
        if (material == null) return null;
        return material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap") as Texture2D : material.mainTexture as Texture2D;
    }

    private static Color GetMaterialColor(Material material)
    {
        if (material == null) return Color.white;
        return material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material.color;
    }

    private void RefreshControls()
    {
        widthField.SetValueWithoutNotify(working.widthCentimeters.ToString("0.#", CultureInfo.InvariantCulture));
        heightField.SetValueWithoutNotify(working.heightCentimeters.ToString("0.#", CultureInfo.InvariantCulture));
        float[] values = { working.foldsPerMeter, working.foldDepthCentimeters, working.foldProfile, working.naturalness, working.opening };
        for (int i = 0; i < sliders.Length; i++) sliders[i].SetValueWithoutNotify(values[i]);
        singlePanel.EnableInClassList("selected", working.panelType == "SinglePanel");
        doublePanel.EnableInClassList("selected", working.panelType != "SinglePanel");
        string[] styles = { "Liso", "Natural", "Wave", "Personalizado" };
        for (int i = 0; i < styleButtons.Length; i++) styleButtons[i].EnableInClassList("selected", working.fabricStyle == styles[i]);
        customCard.style.display = working.fabricStyle == "Personalizado" ? DisplayStyle.Flex : DisplayStyle.None;
        sliderLabels[0].text = $"Densidade · {working.foldsPerMeter:0.#} dobras/m";
        sliderLabels[1].text = $"Profundidade · {working.foldDepthCentimeters:0.#} cm";
        sliderLabels[2].text = $"Perfil da prega · {working.foldProfile:0.##}";
        sliderLabels[3].text = $"Irregularidade natural · {working.naturalness:P0}";
        sliderLabels[4].text = $"Abertura · {working.opening:P0}";
        RefreshSummary();
    }

    private void RefreshSummary()
    {
        selectionSummary.text = $"{working.widthCentimeters:0.#} × {working.heightCentimeters:0.#} cm · " +
            $"{(working.panelType == "SinglePanel" ? "Painel único" : "Painel duplo")} · " +
            $"{working.fabricStyle} · {working.fabricMaterialName}";
    }

    private void RefreshPreview()
    {
        CurtainController curtain = previewObject != null ? previewObject.GetComponent<CurtainController>() : null;
        if (curtain != null) curtain.ApplySettings(working);
        if (previewObject != null)
        {
            Material fabric = selectedMaterial != null ? selectedMaterial.material : defaultFabric;
            Material rod = selectedRod != null ? selectedRod : defaultRod;
            foreach (MeshRenderer renderer in previewObject.GetComponentsInChildren<MeshRenderer>(true))
            {
                bool isRod = renderer.gameObject.name.Contains("RodBody") || renderer.gameObject.name.Contains("Finial");
                renderer.sharedMaterial = isRod ? rod : fabric;
            }
            FitPreviewToModel();
        }
        RefreshSummary();
        SetStatus(string.Empty);
    }

    private void FitPreviewToModel()
    {
        if (previewCamera == null || previewObject == null) return;
        MeshRenderer[] renderers = previewObject.GetComponentsInChildren<MeshRenderer>(true);
        bool hasBounds = false;
        Bounds bounds = default;
        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled) continue;
            if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        if (!hasBounds) return;

        float aspect = previewTexture != null ? previewTexture.width / (float)previewTexture.height : 1.6f;
        float halfVerticalFov = previewCamera.fieldOfView * Mathf.Deg2Rad * 0.5f;
        float fitHeight = bounds.extents.y / Mathf.Tan(halfVerticalFov);
        float fitWidth = bounds.extents.x / (Mathf.Tan(halfVerticalFov) * aspect);
        float distance = Mathf.Max(fitHeight, fitWidth) * 1.25f;
        Vector3 viewDirection = new Vector3(0.08f, 0.04f, -1f).normalized;
        previewCamera.transform.position = bounds.center - viewDirection * Mathf.Max(distance, 0.25f);
        previewCamera.transform.LookAt(bounds.center);
    }

    private void PersistWorking() => CurtainViewSettingsStore.SaveLast(working);
    private void SetStatus(string message) { if (statusLabel != null) statusLabel.text = message; }

    public void ApplySelection()
    {
        ReadDimensions();
        if (!TryPositive(widthField.value, out _) || !TryPositive(heightField.value, out _)) return;
        PersistWorking();
        CatalogSelectionSession.CurrentConfiguration = CurtainViewSettingsStore.Clone(working);
        CatalogSelectionSession.SelectedCurtainPrefab = curtainPrefab;
        CatalogSelectionSession.SelectedFabricMaterial = selectedMaterial != null ? selectedMaterial.material : defaultFabric;
        CatalogSelectionSession.SelectedRodMaterial = selectedRod != null ? selectedRod : defaultRod;
        CatalogSelectionSession.HasConfiguredCurtain = true;
        CurtainViewSettingsStore.ApplyToDraft(working, VisitDraftSession.Current);
        if (CatalogSelectionSession.ReturnToAR)
        {
            CatalogSelectionSession.ReturnToAR = false;
            CatalogSelectionSession.ReturnToVisit = false;
            SceneManager.LoadScene("AR_Test");
        }
        else
        {
            CatalogSelectionSession.ReturnToVisit = true;
            CatalogSelectionSession.ResumeStepIndex = 3;
            SceneManager.LoadScene("NewVisit");
        }
    }

    public void Back()
    {
        PersistWorking();
        if (CatalogSelectionSession.ReturnToAR)
        {
            CatalogSelectionSession.ReturnToAR = false;
            CatalogSelectionSession.CurrentConfiguration = CurtainViewSettingsStore.LoadLast();
            SceneManager.LoadScene("AR_Test");
            return;
        }
        CatalogSelectionSession.ReturnToVisit = true;
        SceneManager.LoadScene("NewVisit");
    }

    private CurtainMaterialCatalog.Entry FindMaterial(string path, string displayName)
    {
        foreach (CurtainMaterialCatalog.Entry entry in materials)
            if ((!string.IsNullOrEmpty(path) && entry.assetPath == path) ||
                (!string.IsNullOrEmpty(displayName) && entry.displayName == displayName)) return entry;
        return null;
    }

    private void OnDestroy()
    {
        if (working != null) PersistWorking();
        if (previewObject != null) Destroy(previewObject);
        if (previewRoot != null) Destroy(previewRoot);
        if (previewCamera != null && previewCamera.targetTexture != null) previewCamera.targetTexture = null;
        if (previewTexture != null) { previewTexture.Release(); Destroy(previewTexture); }
    }
}
