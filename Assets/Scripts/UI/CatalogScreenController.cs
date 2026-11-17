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
    private CurtainViewSettings working;
    private CurtainMaterialCatalog.Entry selectedMaterial;
    private Material selectedRod;
    private GameObject previewRoot;
    private GameObject previewObject;
    private Camera previewCamera;
    private RenderTexture previewTexture;
    private VisualElement root;
    private VisualElement screen;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;
    private ScrollView scroll;
    private ScrollView layerTabsScroll;
    private VisualElement layerTabs;
    private Image previewImage;
    private TextField widthField;
    private TextField heightField;
    private TextField layerSpacingField;
    private TextField materialSearch;
    private VisualElement materialGrid;
    private ScrollView materialScroll;
    private VisualElement materialPicker;
    private Button selectedFabricButton;
    private Image selectedFabricSwatch;
    private Label selectedFabricName;
    private Button addLayerButton;
    private Label selectionSummary;
    private Label statusLabel;
    private VisualElement customCard;
    private readonly Label[] sliderLabels = new Label[5];
    private readonly Slider[] sliders = new Slider[5];
    private Button singlePanel;
    private Button doublePanel;
    private readonly Button[] styleButtons = new Button[4];
    private int activeLayerIndex;
    private bool previewOnly;
    private Button supportRod;
    private Button supportRail;
    private Button supportSingle;
    private Button supportDouble;
    private Button groupFront;
    private Button groupBack;

    private void Start()
    {
        if (document == null) document = GetComponent<UIDocument>();
        previewOnly = UnityARHostBridge.IsCatalogPreview;
        root = document.rootVisualElement;
        screen = root.Q<VisualElement>("root");
        screen.EnableInClassList("dark", UnityARHostBridge.IsDarkTheme);
        root.style.backgroundColor = UnityARHostBridge.IsDarkTheme
            ? new Color(13f / 255f, 22f / 255f, 28f / 255f, 1f)
            : new Color(245f / 255f, 247f / 255f, 248f / 255f, 1f);
        root.RegisterCallback<GeometryChangedEvent>(_ => ApplySafeAreaInsets());
        CacheElements();
        LoadMaterials();
        working = CatalogSelectionSession.CurrentConfiguration != null
            ? CurtainViewSettingsStore.Clone(CatalogSelectionSession.CurrentConfiguration)
            : CurtainViewSettingsStore.LoadLast();
        working.EnsureLayers();

        VisitDraft draft = VisitDraftSession.Current;
        // A draft contains the dimensions of the current visit/record. When this
        // catalog is opened from AR, however, the last-view settings are the
        // source of truth; the host draft may still contain dimensions from an
        // older record (for example 15 x 20 cm).
        if (CatalogSelectionSession.CurrentConfiguration == null &&
            draft != null && draft.widthCentimeters > 0f && draft.heightCentimeters > 0f)
        {
            working.GetLayer(0).widthCentimeters = draft.widthCentimeters;
            working.GetLayer(0).heightCentimeters = draft.heightCentimeters;
            working.SyncLegacyFromFrontLayer();
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
        if (root == null || screen == null || Screen.height <= 0 || root.layout.height <= 0f) return;
        Rect safeArea = Screen.safeArea;
        float pointsPerPixel = root.layout.height / Screen.height;
        screen.style.paddingTop = Mathf.Max(0f, Screen.height - safeArea.yMax) * pointsPerPixel;
        screen.style.paddingBottom = Mathf.Max(0f, safeArea.yMin) * pointsPerPixel;
        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
    }

    private void Update()
    {
        if (Screen.safeArea != lastSafeArea || Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y)
            ApplySafeAreaInsets();
    }

    private void CacheElements()
    {
        scroll = root.Q<ScrollView>("configurationScroll");
        scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        layerTabsScroll = root.Q<ScrollView>("layerTabsScroll");
        layerTabsScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        layerTabs = root.Q<VisualElement>("layerTabs");
        previewImage = root.Q<Image>("previewImage");
        widthField = root.Q<TextField>("widthField");
        heightField = root.Q<TextField>("heightField");
        layerSpacingField = root.Q<TextField>("layerSpacingField");
        materialSearch = root.Q<TextField>("materialSearch");
        materialGrid = root.Q<VisualElement>("materialGrid");
        materialScroll = root.Q<ScrollView>("materialScroll");
        materialScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        materialPicker = root.Q<VisualElement>("materialPicker");
        selectedFabricButton = root.Q<Button>("selectedFabricButton");
        selectedFabricSwatch = root.Q<Image>("selectedFabricSwatch");
        selectedFabricName = root.Q<Label>("selectedFabricName");
        addLayerButton = root.Q<Button>("addLayerButton");
        selectionSummary = root.Q<Label>("selectionSummary");
        statusLabel = root.Q<Label>("statusLabel");
        customCard = root.Q<VisualElement>("customCard");
        singlePanel = root.Q<Button>("singlePanel");
        doublePanel = root.Q<Button>("doublePanel");
        supportRod = root.Q<Button>("supportRod");
        supportRail = root.Q<Button>("supportRail");
        supportSingle = root.Q<Button>("supportSingle");
        supportDouble = root.Q<Button>("supportDouble");
        groupFront = root.Q<Button>("groupFront");
        groupBack = root.Q<Button>("groupBack");
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
        CurtainLayerViewSettings frontLayer = working.GetLayer(0);
        selectedMaterial = FindMaterial(frontLayer.fabricMaterialPath, frontLayer.fabricMaterialName);
        selectedRod = defaultRod;
        if (selectedMaterial == null && materials.Count > 0) selectedMaterial = materials[0];
        if (selectedMaterial != null)
        {
            frontLayer.fabricMaterialPath = selectedMaterial.assetPath;
            frontLayer.fabricMaterialName = selectedMaterial.displayName;
            working.SyncLegacyFromFrontLayer();
        }
        if (string.IsNullOrEmpty(working.rodMaterialName)) working.rodMaterialName = defaultRod != null ? defaultRod.name : "RodMaterial";
        if (string.IsNullOrEmpty(working.rodMaterialPath) && defaultRod != null) working.rodMaterialPath = "Assets/Materials/RodMaterial.mat";
    }

    private void BindEvents()
    {
        root.Q<Button>("backButton").clicked += Back;
        Button togglePreview = root.Q<Button>("togglePreviewButton");
        togglePreview.clicked += () =>
        {
            bool collapsed = !previewImage.ClassListContains("hidden");
            previewImage.EnableInClassList("hidden", collapsed);
            togglePreview.text = collapsed ? "Mostrar prévia" : "Recolher";
        };
        Button applyButton = root.Q<Button>("applyButton");
        if (previewOnly)
        {
            applyButton.style.display = DisplayStyle.None;
            root.Q<Label>(className: "header-title").text = "Prévia do catálogo";
            root.Q<Label>(className: "header-subtitle").text = "Explore os materiais e confira o resultado";
        }
        else
        {
            applyButton.clicked += ApplySelection;
        }
        singlePanel.clicked += () => SetPanelType("SinglePanel");
        doublePanel.clicked += () => SetPanelType("DoublePanel");
        supportRod.clicked += () => SetSupport("Rod", working.doubleSupport);
        supportRail.clicked += () => SetSupport("Rail", working.doubleSupport);
        supportSingle.clicked += () => SetSupport(working.supportType, false);
        supportDouble.clicked += () => SetSupport(working.supportType, true);
        groupFront.clicked += () => AssignSupport(0);
        groupBack.clicked += () => AssignSupport(1);
        root.Q<Button>("varyDrapeButton").clicked += () =>
        {
            CurrentLayer.seed = unchecked(CurrentLayer.seed + 37);
            RefreshPreview();
            PersistWorking();
        };
        for (int i = 0; i < styleButtons.Length; i++)
        {
            int styleIndex = i;
            styleButtons[i].clicked += () => SetStyle(styleIndex);
        }
        widthField.isDelayed = true;
        heightField.isDelayed = true;
        layerSpacingField.isDelayed = true;
        widthField.RegisterValueChangedCallback(_ => ReadDimensions());
        heightField.RegisterValueChangedCallback(_ => ReadDimensions());
        layerSpacingField.RegisterValueChangedCallback(_ => ReadLayerSpacing());
        materialSearch.RegisterValueChangedCallback(_ => RefreshMaterials());
        addLayerButton.clicked += AddLayer;
        selectedFabricButton.clicked += OpenMaterialPicker;
        root.Q<Button>("closeMaterialPicker").clicked += CloseMaterialPicker;
        materialPicker.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target == materialPicker) CloseMaterialPicker();
        });
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
        SceneManager.MoveGameObjectToScene(previewRoot, gameObject.scene);
        previewObject = Instantiate(curtainPrefab, Vector3.zero, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(previewObject, gameObject.scene);
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
        previewTexture = new RenderTexture(840, 600, 16, RenderTextureFormat.ARGB32)
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
        CurrentLayer.widthCentimeters = Mathf.Clamp(width, 10f, 10000f);
        CurrentLayer.heightCentimeters = Mathf.Clamp(height, 10f, 10000f);
        working.SyncLegacyFromFrontLayer();
        RefreshControls();
        SetStatus(string.Empty);
        RefreshPreview();
        PersistWorking();
    }

    private static bool TryPositive(string value, out float result) =>
        float.TryParse((value ?? string.Empty).Trim().Replace(',', '.'), NumberStyles.Float,
            CultureInfo.InvariantCulture, out result) && result > 0f && !float.IsInfinity(result);

    private CurtainLayerViewSettings CurrentLayer => working.GetLayer(activeLayerIndex);

    private void ReadLayerSpacing()
    {
        string raw = (layerSpacingField.value ?? string.Empty).Trim().Replace(',', '.');
        if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float spacing) ||
            float.IsNaN(spacing) || float.IsInfinity(spacing) || spacing < 0f)
        {
            SetStatus("Informe um afastamento igual ou maior que zero.");
            return;
        }

        CurrentLayer.spacingMillimeters = Mathf.Clamp(spacing, 0f, 1000f);
        RefreshControls();
        SetStatus(string.Empty);
        RefreshPreview();
        PersistWorking();
    }

    private void SetActiveLayer(int index)
    {
        if (index < 0 || index >= working.layers.Count || index == activeLayerIndex) return;
        activeLayerIndex = index;
        CurtainLayerViewSettings layer = CurrentLayer;
        selectedMaterial = FindMaterial(layer.fabricMaterialPath, layer.fabricMaterialName);
        if (selectedMaterial == null && materials.Count > 0)
        {
            selectedMaterial = materials[0];
            layer.fabricMaterialPath = selectedMaterial.assetPath;
            layer.fabricMaterialName = selectedMaterial.displayName;
            working.SyncLegacyFromFrontLayer();
        }
        RefreshControls();
        RefreshMaterials();
        RefreshPreview();
        PersistWorking();
    }

    private void AddLayer()
    {
        if (!working.AddLayerFrom(activeLayerIndex)) return;
        activeLayerIndex = working.layers.Count - 1;
        CurtainLayerViewSettings layer = CurrentLayer;
        selectedMaterial = FindMaterial(layer.fabricMaterialPath, layer.fabricMaterialName);
        RefreshControls();
        RefreshMaterials();
        RefreshPreview();
        PersistWorking();
    }

    private void RemoveLayer(int index)
    {
        if (!working.RemoveLayer(index)) return;
        if (activeLayerIndex == index) activeLayerIndex = Mathf.Min(index, working.layers.Count - 1);
        else if (activeLayerIndex > index) activeLayerIndex--;
        CurtainLayerViewSettings layer = CurrentLayer;
        selectedMaterial = FindMaterial(layer.fabricMaterialPath, layer.fabricMaterialName);
        RefreshControls();
        RefreshMaterials();
        RefreshPreview();
        PersistWorking();
    }

    private void RefreshLayerTabs()
    {
        layerTabs.Clear();
        for (int i = 0; i < working.layers.Count; i++)
        {
            int layerIndex = i;
            VisualElement tabItem = new VisualElement();
            tabItem.AddToClassList("layer-tab-item");
            Button tab = new Button { name = "layerTab" + (i + 1), text = "Camada " + (i + 1) };
            tab.AddToClassList("layer-tab");
            tab.EnableInClassList("selected", i == activeLayerIndex);
            tab.clicked += () => SetActiveLayer(layerIndex);
            Button remove = new Button { name = "removeLayer" + (i + 1), text = "−" };
            remove.AddToClassList("layer-remove-button");
            remove.SetEnabled(working.layers.Count > 1);
            remove.clicked += () => RemoveLayer(layerIndex);
            tabItem.Add(tab);
            tabItem.Add(remove);
            layerTabs.Add(tabItem);
        }
        addLayerButton.SetEnabled(working.layers.Count < CurtainViewSettings.MaximumLayerCount);
    }

    private void SetPanelType(string panelType)
    {
        CurrentLayer.panelType = panelType;
        working.SyncLegacyFromFrontLayer();
        RefreshControls();
        RefreshPreview();
        PersistWorking();
    }

    private void SetSupport(string type, bool isDouble)
    {
        working.supportType = type;
        working.doubleSupport = isDouble;
        RefreshControls();
        RefreshPreview();
        PersistWorking();
    }

    private void AssignSupport(int index)
    {
        CurrentLayer.supportIndex = index;
        RefreshControls();
        RefreshPreview();
        PersistWorking();
    }

    private void SetStyle(int index)
    {
        CurtainLayerViewSettings layer = CurrentLayer;
        if (index == 3) layer.fabricStyle = "Personalizado";
        else
        {
            layer.fabricStyle = index == 0 ? "Liso" : index == 1 ? "Natural" : "Wave";
            layer.foldsPerMeter = index == 0 ? 3f : index == 1 ? 5f : 6f;
            layer.foldDepthCentimeters = index == 0 ? 1.8f : index == 1 ? 6f : 9.5f;
            layer.foldProfile = index == 0 ? 1f : index == 1 ? 1.15f : 0.8f;
            layer.naturalness = index == 2 ? 0.8f : 1f;
        }
        working.SyncLegacyFromFrontLayer();
        RefreshControls();
        RefreshPreview();
        PersistWorking();
    }

    private void UpdateCustomValue(int index, float value)
    {
        CurtainLayerViewSettings layer = CurrentLayer;
        switch (index)
        {
            case 0: layer.foldsPerMeter = value; break;
            case 1: layer.foldDepthCentimeters = value; break;
            case 2: layer.foldProfile = value; break;
            case 3: layer.naturalness = value; break;
            case 4: working.SetOpening(activeLayerIndex, value); break;
        }
        if (index != 4) layer.fabricStyle = "Personalizado";
        working.SyncLegacyFromFrontLayer();
        RefreshControls();
        RefreshPreview();
        PersistWorking();
    }

    private void SelectMaterial(CurtainMaterialCatalog.Entry entry)
    {
        selectedMaterial = entry;
        CurrentLayer.fabricMaterialPath = entry.assetPath;
        CurrentLayer.fabricMaterialName = entry.displayName;
        working.SyncLegacyFromFrontLayer();
        CloseMaterialPicker();
        RefreshMaterials();
        RefreshPreview();
        PersistWorking();
    }

    private void RefreshMaterials()
    {
        string query = materialSearch.value?.Trim() ?? string.Empty;
        CurtainLayerViewSettings layer = CurrentLayer;
        selectedMaterial = FindMaterial(layer.fabricMaterialPath, layer.fabricMaterialName);
        RefreshSelectedFabric();
        filteredMaterials.Clear();
        foreach (CurtainMaterialCatalog.Entry entry in materials)
            if (CurtainMaterialCatalog.MatchesSearch(entry.displayName, entry.assetPath, query))
                filteredMaterials.Add(entry);
        materialGrid.Clear();
        foreach (CurtainMaterialCatalog.Entry entry in filteredMaterials)
        {
            Button choice = new Button { name = "Material_" + entry.displayName, text = string.Empty };
            choice.AddToClassList("material-choice");
            if (entry == selectedMaterial) choice.AddToClassList("selected");
            var swatch = new Image { name = "Swatch" };
            swatch.AddToClassList("material-swatch");
            swatch.scaleMode = ScaleMode.ScaleAndCrop;
            Texture2D texture = GetMaterialTexture(entry.material);
            if (texture != null) swatch.image = texture;
            else swatch.style.backgroundColor = GetMaterialColor(entry.material);
            choice.Add(swatch);
            Label label = new Label(entry.displayName);
            label.AddToClassList("material-name");
            choice.Add(label);
            choice.clicked += () => SelectMaterial(entry);
            materialGrid.Add(choice);
        }
        if (filteredMaterials.Count == 0)
        {
            Label empty = new Label("Nenhum tecido encontrado");
            empty.AddToClassList("material-empty");
            materialGrid.Add(empty);
        }
    }

    private void RefreshSelectedFabric()
    {
        Material material = selectedMaterial != null ? selectedMaterial.material : defaultFabric;
        selectedFabricName.text = selectedMaterial != null
            ? selectedMaterial.displayName
            : !string.IsNullOrEmpty(CurrentLayer.fabricMaterialName) ? CurrentLayer.fabricMaterialName : "Nenhum tecido selecionado";
        selectedFabricSwatch.scaleMode = ScaleMode.ScaleAndCrop;
        Texture2D texture = GetMaterialTexture(material);
        selectedFabricSwatch.image = texture;
        selectedFabricSwatch.style.backgroundColor = texture == null ? GetMaterialColor(material) : Color.clear;
    }

    private void OpenMaterialPicker()
    {
        materialPicker.AddToClassList("open");
        materialSearch.SetValueWithoutNotify(string.Empty);
        RefreshMaterials();
        materialSearch.Focus();
    }

    private void CloseMaterialPicker()
    {
        materialPicker.RemoveFromClassList("open");
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
        CurtainLayerViewSettings layer = CurrentLayer;
        widthField.SetValueWithoutNotify(layer.widthCentimeters.ToString("0.#", CultureInfo.InvariantCulture));
        heightField.SetValueWithoutNotify(layer.heightCentimeters.ToString("0.#", CultureInfo.InvariantCulture));
        layerSpacingField.SetValueWithoutNotify(layer.spacingMillimeters.ToString("0.#", CultureInfo.InvariantCulture));
        float[] values = { layer.foldsPerMeter, layer.foldDepthCentimeters, layer.foldProfile, layer.naturalness, working.GetOpening(activeLayerIndex) };
        for (int i = 0; i < sliders.Length; i++) sliders[i].SetValueWithoutNotify(values[i]);
        singlePanel.EnableInClassList("selected", layer.panelType == "SinglePanel");
        doublePanel.EnableInClassList("selected", layer.panelType != "SinglePanel");
        supportRod.EnableInClassList("selected", working.supportType == "Rod");
        supportRail.EnableInClassList("selected", working.supportType == "Rail");
        supportSingle.EnableInClassList("selected", !working.doubleSupport);
        supportDouble.EnableInClassList("selected", working.doubleSupport);
        root.Q<VisualElement>("layerGroup").EnableInClassList("hidden", !working.doubleSupport);
        groupFront.EnableInClassList("selected", layer.supportIndex == 0);
        groupBack.EnableInClassList("selected", layer.supportIndex == 1);
        string support = working.supportType == "Rail" ? "Trilho" : "Varão";
        groupFront.text = support + " 1 · frente";
        groupBack.text = support + " 2 · trás";
        root.Q<Label>("activeLayerLabel").text = "Editando camada " + (activeLayerIndex + 1);
        root.Q<Label>("supportHint").text = working.doubleSupport
            ? "Escolha o suporte de cada camada. Camadas no mesmo suporte abrem juntas."
            : "Todas as camadas abrem juntas. O suporte duplo permite separar em dois grupos.";
        string[] styles = { "Liso", "Natural", "Wave", "Personalizado" };
        for (int i = 0; i < styleButtons.Length; i++) styleButtons[i].EnableInClassList("selected", layer.fabricStyle == styles[i]);
        customCard.EnableInClassList("hidden", layer.fabricStyle != "Personalizado");
        sliderLabels[0].text = $"Densidade · {layer.foldsPerMeter:0.#} dobras/m";
        sliderLabels[1].text = $"Profundidade · {layer.foldDepthCentimeters:0.#} cm";
        sliderLabels[2].text = $"Perfil da prega · {layer.foldProfile:0.##}";
        sliderLabels[3].text = $"Irregularidade natural · {layer.naturalness:P0}";
        int group = working.GetSupportIndex(activeLayerIndex);
        sliderLabels[4].text = $"Abertura · {support.ToLower()} {group + 1} · {working.GetOpening(activeLayerIndex):P0}";
        var groupLayers = new List<string>();
        for (int i = 0; i < working.layers.Count; i++)
            if (working.GetSupportIndex(i) == group) groupLayers.Add((i + 1).ToString());
        root.Q<Label>("openingHint").text = "Move as camadas " + string.Join(", ", groupLayers) + " deste suporte.";
        RefreshLayerTabs();
        RefreshSelectedFabric();
        RefreshSummary();
    }

    private void RefreshSummary()
    {
        CurtainLayerViewSettings layer = CurrentLayer;
        selectionSummary.text = $"Camada {activeLayerIndex + 1} · {layer.widthCentimeters:0.#} × {layer.heightCentimeters:0.#} cm · " +
            $"{working.layers.Count} camada{(working.layers.Count == 1 ? string.Empty : "s")} · " +
            $"{(working.supportType == "Rail" ? "Trilho" : "Varão")} {(working.doubleSupport ? "duplo" : "simples")}";
    }

    private void RefreshPreview()
    {
        CurtainController curtain = previewObject != null ? previewObject.GetComponent<CurtainController>() : null;
        working.SyncLegacyFromFrontLayer();
        if (curtain != null)
        {
            curtain.ApplySettings(working);
            float gap = curtain.GetLayerClearanceMillimeters(activeLayerIndex);
            layerSpacingField.SetEnabled(gap > 0f);
            root.Q<Label>("spacingHint").text = gap <= 0f
                ? "Esta é a camada da frente. As demais reservam espaço atrás dela."
                : $"Distância entre os centros: {gap:0.#} mm (mínimo de 2 mm). As pregas são reduzidas quando necessário para evitar sobreposição.";
            for (int i = 0; i < working.layers.Count; i++)
            {
                CurtainLayerViewSettings layer = working.layers[i];
                CurtainMaterialCatalog.Entry entry = FindMaterial(layer.fabricMaterialPath, layer.fabricMaterialName);
                Material material = entry != null ? entry.material : defaultFabric;
                if (i == activeLayerIndex && selectedMaterial != null) material = selectedMaterial.material;
                curtain.SetLayerMaterial(i, material);
            }
        }
        if (previewObject != null)
        {
            Material rod = selectedRod != null ? selectedRod : defaultRod;
            foreach (MeshRenderer renderer in previewObject.GetComponentsInChildren<MeshRenderer>(true))
            {
                bool isRod = renderer.gameObject.name.Contains("RodBody") || renderer.gameObject.name.Contains("Finial");
                if (isRod) renderer.sharedMaterial = rod;
                else if (curtain == null && selectedMaterial != null) renderer.sharedMaterial = selectedMaterial.material;
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
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
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

    private void PersistWorking()
    {
        if (previewOnly) return;
        CurtainViewSettingsStore.SaveLast(working);
        CurtainViewSettingsStore.ApplyToDraft(working, VisitDraftSession.Current);
        UnityARHostBridge.SaveLastViewConfig();
    }
    private void SetStatus(string message) { if (statusLabel != null) statusLabel.text = message; }

    public void ApplySelection()
    {
        if (previewOnly) return;
        ReadDimensions();
        if (!TryPositive(widthField.value, out _) || !TryPositive(heightField.value, out _)) return;
        working.SyncLegacyFromFrontLayer();
        PersistWorking();
        CatalogSelectionSession.CurrentConfiguration = CurtainViewSettingsStore.Clone(working);
        CatalogSelectionSession.SelectedCurtainPrefab = curtainPrefab;
        CurtainLayerViewSettings frontLayer = working.GetLayer(0);
        CurtainMaterialCatalog.Entry frontMaterial = FindMaterial(frontLayer.fabricMaterialPath, frontLayer.fabricMaterialName);
        CatalogSelectionSession.SelectedFabricMaterial = frontMaterial != null ? frontMaterial.material : defaultFabric;
        CatalogSelectionSession.SelectedRodMaterial = selectedRod != null ? selectedRod : defaultRod;
        CatalogSelectionSession.HasConfiguredCurtain = true;
        CurtainViewSettingsStore.ApplyToDraft(working, VisitDraftSession.Current);
        UnityARHostBridge.SaveLastViewConfig();
        if (CatalogSelectionSession.ReturnToAR)
        {
            if (ARCatalogSession.ReturnToAR()) return;
            CatalogSelectionSession.ReturnToAR = false;
            CatalogSelectionSession.ReturnToVisit = false;
            SceneManager.LoadScene("AR_Test");
        }
        else
        {
            CatalogSelectionSession.ReturnToVisit = false;
            SceneManager.LoadScene("AR_Test");
        }
    }

    public void Back()
    {
        if (previewOnly)
        {
            UnityARHostBridge.ReturnToReactHost();
            return;
        }
        PersistWorking();
        if (CatalogSelectionSession.ReturnToAR)
        {
            CatalogSelectionSession.CurrentConfiguration = CurtainViewSettingsStore.Clone(working);
            if (ARCatalogSession.ReturnToAR()) return;
            CatalogSelectionSession.ReturnToAR = false;
            CatalogSelectionSession.CurrentConfiguration = CurtainViewSettingsStore.LoadLast();
            SceneManager.LoadScene("AR_Test");
            return;
        }
        CatalogSelectionSession.ReturnToVisit = false;
        SceneManager.LoadScene("AR_Test");
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
        if (working != null && !previewOnly) PersistWorking();
        if (previewObject != null) Destroy(previewObject);
        if (previewRoot != null) Destroy(previewRoot);
        if (previewCamera != null && previewCamera.targetTexture != null) previewCamera.targetTexture = null;
        if (previewTexture != null) { previewTexture.Release(); Destroy(previewTexture); }
    }
}
