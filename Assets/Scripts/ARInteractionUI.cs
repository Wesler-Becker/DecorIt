using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ARInteractionUI : MonoBehaviour
{
    [Header("Controles existentes no Canvas")]
    [SerializeField] private Button btnPosicionar;
    [SerializeField] private Button btnAjustar;
    [SerializeField] private Button btnModoGesto;
    [SerializeField] private TMP_Text textoModoGesto;

    private static readonly Color Ink = new Color(0.06f, 0.09f, 0.12f, 0.94f);
    private static readonly Color Panel = new Color(0.035f, 0.055f, 0.075f, 0.91f);
    private static readonly Color SoftWhite = new Color(0.96f, 0.97f, 0.98f, 0.96f);
    private static readonly Color Accent = new Color(0.29f, 0.72f, 0.94f, 1f);
    private static readonly Color[] FabricColors =
    {
        new Color(0.98f, 0.86f, 0.69f),
        new Color(0.91f, 0.90f, 0.86f),
        new Color(0.58f, 0.59f, 0.60f),
        new Color(0.27f, 0.37f, 0.49f),
        new Color(0.48f, 0.55f, 0.47f),
    };
    private static readonly string[] FabricNames = { "Areia", "Gelo", "Cinza", "Azul", "Sálvia" };
    private static readonly Color[] RodColors =
    {
        new Color(0.73f, 0.73f, 0.73f),
        new Color(0.12f, 0.14f, 0.17f),
        new Color(0.70f, 0.49f, 0.23f),
    };
    private static readonly string[] RodNames = { "Alumínio", "Preto", "Dourado" };

    private readonly List<Material> runtimeMaterials = new List<Material>();
    private readonly List<Sprite> runtimeSwatchSprites = new List<Sprite>();
    private readonly List<string> fabricNames = new List<string>();
    private readonly List<string> fabricPaths = new List<string>();
    private readonly List<Image> fabricSwatches = new List<Image>();
    private readonly List<Image> rodSwatches = new List<Image>();
    private readonly List<Material> fabrics = new List<Material>();
    private readonly List<Material> rods = new List<Material>();
    private ARPlacementController placementController;
    private ARSurfaceUI surfaceUI;
    private GameObject menuPanel;
    private GameObject fabricTray;
    private GameObject rodTray;
    private GameObject modelTray;
    private GameObject activeQuickSelector;
    private Button menuButton;
    private Transform overlayRoot;
    private Button addButton;
    private GameObject addOptionsPanel;
    private Button editButton;
    private Button zoomButton;
    private Button rotateButton;
    private GameObject editControls;
    private readonly List<Button> modelOptionButtons = new List<Button>();
    private readonly List<GameObject> modelOptions = new List<GameObject>();
    private TMP_Text planesButtonText;
    private int selectedFabricIndex;
    private int selectedRodIndex;
    private bool menuVisible;
    private bool modoPosicionar;
    private bool modoAjustar;
    private bool modoRotacao;

    private void Start()
    {
        placementController = FindFirstObjectByType<ARPlacementController>();
        if (placementController != null) placementController.InitializeCurtainSettings();
        surfaceUI = GetComponent<ARSurfaceUI>();
        if (btnPosicionar == null || btnAjustar == null || btnModoGesto == null)
            Debug.LogError("ARInteractionUI: os botões originais não foram ligados no Canvas.");

        btnPosicionar.onClick.AddListener(AtivarPosicionamento);
        btnAjustar.onClick.AddListener(AtivarAjuste);
        btnModoGesto.onClick.AddListener(AlternarModoGesto);
        HideLegacyControls();
        BuildCameraOverlay();

        if (!UnityARHostBridge.IsPhotoOnly || UnityARHostBridge.IsQuickAR)
        {
            CreateMaterialPresets();
            BuildFabricTray();
            BuildRodTray();
            BuildModelTray();
            BuildCameraMenu();
            BuildEditingControls();
            UpdateControlState();
            UpdateSelection();
        }
    }

    private void OnDestroy()
    {
        if (btnPosicionar != null) btnPosicionar.onClick.RemoveListener(AtivarPosicionamento);
        if (btnAjustar != null) btnAjustar.onClick.RemoveListener(AtivarAjuste);
        if (btnModoGesto != null) btnModoGesto.onClick.RemoveListener(AlternarModoGesto);
        foreach (Material material in runtimeMaterials)
            if (material != null) Destroy(material);
        runtimeMaterials.Clear();
        foreach (Sprite sprite in runtimeSwatchSprites)
            if (sprite != null) Destroy(sprite);
        runtimeSwatchSprites.Clear();
    }

    private void HideLegacyControls()
    {
        if (btnPosicionar != null) btnPosicionar.gameObject.SetActive(false);
        if (btnAjustar != null) btnAjustar.gameObject.SetActive(false);
        if (btnModoGesto != null) btnModoGesto.gameObject.SetActive(false);
        if (surfaceUI != null) surfaceUI.HideLegacyButton();
    }

    private void BuildCameraOverlay()
    {
        overlayRoot = ARUIIconFactory.GetCameraControlsRoot(GetComponentInParent<Canvas>());
        menuButton = CreateButton("CameraMenuButton", overlayRoot, Vector2.one, new Vector2(-24f, -24f), new Vector2(88f, 88f), Color.clear, ToggleMenu);
        ARUIIconFactory.ConfigureCameraButton(menuButton, "Menu", 88f, Color.clear, SoftWhite);
        menuButton.gameObject.SetActive(!UnityARHostBridge.IsPhotoOnly || UnityARHostBridge.IsQuickAR);
    }

    private void BuildEditingControls()
    {
        addButton = CreateButton("AddCurtainButton", overlayRoot, Vector2.zero, new Vector2(24f, 36f), new Vector2(96f, 96f), Ink, ToggleAddOptions);
        ARUIIconFactory.ConfigureCameraButton(addButton, "Add", 96f, Ink, SoftWhite);
        BuildAddOptions();
        editControls = CreatePanel("CameraEditControls", overlayRoot, new Vector2(1f, 0f), new Vector2(-24f, 36f), new Vector2(252f, 96f), Color.clear);
        editControls.GetComponent<Image>().raycastTarget = false;
        zoomButton = CreateGestureButton("ZoomGestureButton", "Zoom", 42f, () => SelecionarGesto(false));
        editButton = CreateGestureButton("EditObjectButton", "Edit", 126f, AtivarAjuste);
        rotateButton = CreateGestureButton("RotateGestureButton", "Rotate", 210f, () => SelecionarGesto(true));
    }

    private void BuildAddOptions()
    {
        addOptionsPanel = CreatePanel("AddCurtainOptions", overlayRoot, Vector2.zero,
            new Vector2(24f, 144f), new Vector2(420f, 170f), Panel);
        Button addLast = CreateButton("AddLastCurtainButton", addOptionsPanel.transform,
            new Vector2(0f, 1f), new Vector2(12f, -10f), new Vector2(396f, 68f),
            new Color(0.12f, 0.17f, 0.21f, 0.96f), AddLastCurtain);
        StyleMenuButton(addLast, "Adicionar última configuração", "Add");
        Button search = CreateButton("SearchCurtainButton", addOptionsPanel.transform,
            new Vector2(0f, 1f), new Vector2(12f, -90f), new Vector2(396f, 68f),
            new Color(0.12f, 0.17f, 0.21f, 0.96f), OpenCurtainCatalog);
        StyleMenuButton(search, "Buscar cortina no catálogo", "Search");
        addOptionsPanel.SetActive(false);
    }

    private void ToggleAddOptions()
    {
        if (addOptionsPanel != null) addOptionsPanel.SetActive(!addOptionsPanel.activeSelf);
    }

    private void AddLastCurtain()
    {
        if (addOptionsPanel != null) addOptionsPanel.SetActive(false);
        if (placementController != null) placementController.InitializeCurtainSettings();
        modoPosicionar = true;
        modoAjustar = false;
        UpdateControlState();
    }

    private void OpenCurtainCatalog()
    {
        if (addOptionsPanel != null) addOptionsPanel.SetActive(false);
        CatalogSelectionSession.ReturnToAR = true;
        CatalogSelectionSession.CurrentConfiguration = CurtainViewSettingsStore.LoadLast();
        SceneManager.LoadScene("Catalog");
    }

    private Button CreateGestureButton(string name, string icon, float x, System.Action action)
    {
        Button button = CreateButton(name, editControls.transform, new Vector2(0f, 0.5f), new Vector2(x, 0f), new Vector2(80f, 80f), Ink, action);
        (button.transform as RectTransform).pivot = new Vector2(0.5f, 0.5f);
        ARUIIconFactory.ConfigureCameraButton(button, icon, 80f, Ink, SoftWhite);
        return button;
    }

    private void SelecionarGesto(bool rotacao)
    {
        modoRotacao = rotacao;
        UpdateControlState();
    }

    private void BuildCameraMenu()
    {
        menuPanel = CreatePanel("CameraOptionsPanel", overlayRoot, new Vector2(1f, 1f), new Vector2(-24f, -132f), new Vector2(432f, 680f), Panel);
        menuPanel.SetActive(false);
        TMP_Text title = CreateText("MenuTitle", menuPanel.transform, "AJUSTES DA CENA", 20f, SoftWhite, TextAlignmentOptions.Left);
        PlaceTopLeft(title.rectTransform, new Vector2(24f, 20f), new Vector2(380f, 38f));
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 1.1f;

        Button planesButton = CreateMenuRow("PlanesMenuButton", "Planes", "Planos: visíveis", 78f, TogglePlanes);
        planesButtonText = planesButton.GetComponentInChildren<TMP_Text>();

        AddSectionLabel(menuPanel.transform, "MODELO", 152f);
        Button singlePanel = CreateMenuRow("SinglePanelButton", "", "Painel único", 188f, () => SetPanelType(CurtainController.CurtainType.SinglePanel));
        Button doublePanel = CreateMenuRow("DoublePanelButton", "", "Painel duplo", 252f, () => SetPanelType(CurtainController.CurtainType.DoublePanel));
        singlePanel.image.color = new Color(0.12f, 0.17f, 0.21f, 0.96f);
        doublePanel.image.color = new Color(0.12f, 0.17f, 0.21f, 0.96f);

        AddSectionLabel(menuPanel.transform, "MODELO DISPONÍVEL", 328f);
        CreateMenuRow("ModelSelectorButton", "", "Escolher Modelo", 364f, ToggleModelTray);

        AddSectionLabel(menuPanel.transform, "ACABAMENTO DO VARÃO", 438f);
        CreateMenuRow("RodSelectorButton", "", "Escolher Acabamento", 474f, ToggleRodTray);

        AddSectionLabel(menuPanel.transform, "TECIDO", 548f);
        Button fabricButton = CreateMenuRow("FabricTrayButton", "", "Escolher Tecido", 582f, ToggleFabricTray);
        fabricButton.image.color = new Color(0.12f, 0.17f, 0.21f, 0.96f);
    }

    private float quickSelectorWidth;

    private GameObject CreateQuickSelectorTray(string name, string title)
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
        float canvasWidth = canvasRect != null ? canvasRect.rect.width : Screen.width;
        quickSelectorWidth = Mathf.Clamp(canvasWidth - 500f, 220f, 580f);
        GameObject tray = CreatePanel(name, overlayRoot, Vector2.zero, new Vector2(20f, 170f), new Vector2(quickSelectorWidth, 140f), Panel);
        TMP_Text heading = CreateText(name + "Title", tray.transform, title, 16f, new Color(0.8f, 0.85f, 0.89f), TextAlignmentOptions.Left);
        PlaceTopLeft(heading.rectTransform, new Vector2(20f, 8f), new Vector2(quickSelectorWidth - 40f, 24f));
        heading.fontStyle = FontStyles.Bold;
        tray.SetActive(false);
        return tray;
    }

    private void BuildFabricTray()
    {
        fabricTray = CreateQuickSelectorTray("FabricQuickSelector", "TECIDO  ·  TOQUE PARA TESTAR");
        BuildHorizontalSelector(fabricTray.transform, "FabricSwatches", fabrics, fabricNames, fabricSwatches, -34f, SelectFabric,
            new Vector2(quickSelectorWidth - 24f, 94f), new Vector2(0f, 1f));
    }

    private void BuildRodTray()
    {
        rodTray = CreateQuickSelectorTray("RodQuickSelector", "ACABAMENTO  ·  TOQUE PARA APLICAR");
        BuildHorizontalSelector(rodTray.transform, "RodSwatches", rods, RodNames, rodSwatches, -34f, SelectRod,
            new Vector2(quickSelectorWidth - 24f, 94f), new Vector2(0f, 1f));
    }

    private void BuildModelTray()
    {
        modelTray = CreateQuickSelectorTray("ModelQuickSelector", "MODELO  ·  TOQUE PARA SELECIONAR");
        modelOptions.Clear();
        modelOptionButtons.Clear();
        if (placementController != null)
            foreach (GameObject model in placementController.AvailableModels)
                if (model != null) modelOptions.Add(model);

        GameObject scrollObject = new GameObject("ModelOptionsScroll", typeof(RectTransform), typeof(ScrollRect));
        scrollObject.transform.SetParent(modelTray.transform, false);
        RectTransform scrollRect = scrollObject.GetComponent<RectTransform>();
        scrollRect.anchorMin = scrollRect.anchorMax = new Vector2(0f, 1f);
        scrollRect.pivot = new Vector2(0f, 1f);
        scrollRect.anchoredPosition = new Vector2(12f, -34f);
        scrollRect.sizeDelta = new Vector2(quickSelectorWidth - 24f, 94f);

        GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewportObject.transform.SetParent(scrollObject.transform, false);
        Stretch(viewportObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        viewportObject.GetComponent<Image>().color = Color.white;
        viewportObject.GetComponent<Mask>().showMaskGraphic = false;

        GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        contentObject.transform.SetParent(viewportObject.transform, false);
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 0.5f);
        contentRect.anchorMax = new Vector2(0f, 0.5f);
        contentRect.pivot = new Vector2(0f, 0.5f);
        contentRect.sizeDelta = new Vector2(0f, 94f);
        HorizontalLayoutGroup layout = contentObject.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(4, 4, 5, 5);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = layout.childControlHeight = false;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        contentObject.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
        scroll.viewport = viewportObject.GetComponent<RectTransform>();
        scroll.content = contentRect;
        scroll.horizontal = true;
        scroll.vertical = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        foreach (GameObject model in modelOptions)
        {
            GameObject option = new GameObject(model.name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            option.transform.SetParent(contentObject.transform, false);
            option.GetComponent<LayoutElement>().preferredWidth = 176f;
            option.GetComponent<LayoutElement>().preferredHeight = 76f;
            Image background = option.GetComponent<Image>();
            background.sprite = ARUIIconFactory.RoundedSprite;
            background.type = Image.Type.Sliced;
            Button button = option.GetComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() =>
            {
                if (placementController != null) placementController.SelectModel(model);
                UpdateSelection();
            });
            TMP_Text label = CreateText("Label", option.transform, model.name, 17f, SoftWhite, TextAlignmentOptions.Center);
            Stretch(label.rectTransform, new Vector2(8f, 6f), new Vector2(-8f, -6f));
            modelOptionButtons.Add(button);
        }

        if (modelOptions.Count == 0)
        {
            TMP_Text empty = CreateText("NoModelsAvailable", modelTray.transform, "Nenhum modelo disponível", 15f, SoftWhite, TextAlignmentOptions.Center);
            PlaceTopLeft(empty.rectTransform, new Vector2(16f, 49f), new Vector2(quickSelectorWidth - 32f, 62f));
        }
    }

    private void CreateMaterialPresets()
    {
        Material baseFabric = placementController != null ? placementController.GetBaseFabricMaterial(false) : null;
        Material baseRod = placementController != null ? placementController.GetBaseFabricMaterial(true) : null;
        if (baseFabric == null) baseFabric = CreateFallbackMaterial("Tecido base", FabricColors[0]);
        if (baseRod == null) baseRod = CreateFallbackMaterial("Varão base", RodColors[0]);

        for (int i = 0; i < FabricNames.Length; i++)
        {
            Color color = i == 0 && baseFabric != null ? baseFabric.color : FabricColors[i];
            fabrics.Add(CreateTintedMaterial(baseFabric, "Tecido " + FabricNames[i], color));
            fabricNames.Add(FabricNames[i]);
            fabricPaths.Add(string.Empty);
        }
        CurtainMaterialCatalog catalog = Resources.Load<CurtainMaterialCatalog>("CurtainMaterials");
        if (catalog != null)
            foreach (CurtainMaterialCatalog.Entry entry in catalog.entries)
            {
                if (entry.material == null) continue;
                fabrics.Add(entry.material);
                fabricNames.Add(entry.displayName);
                fabricPaths.Add(entry.assetPath);
            }
        for (int i = 0; i < RodNames.Length; i++)
        {
            Color color = i == 0 && baseRod != null ? baseRod.color : RodColors[i];
            rods.Add(CreateTintedMaterial(baseRod, "Varão " + RodNames[i], color));
        }

        selectedFabricIndex = FindPresetIndex(FabricNames, VisitDraftSession.Current.fabricMaterialName);
        int importedIndex = fabricPaths.FindIndex(path => !string.IsNullOrEmpty(path) && path == VisitDraftSession.Current.fabricMaterialPath);
        if (importedIndex < 0) importedIndex = fabricNames.FindIndex(name => name == VisitDraftSession.Current.fabricMaterialName);
        if (importedIndex >= 0) selectedFabricIndex = importedIndex;
        selectedRodIndex = FindPresetIndex(RodNames, VisitDraftSession.Current.rodMaterialName);
        if (CatalogSelectionSession.SelectedFabricMaterial != null)
            fabrics[selectedFabricIndex] = CatalogSelectionSession.SelectedFabricMaterial;
        if (CatalogSelectionSession.SelectedRodMaterial != null)
            rods[selectedRodIndex] = CatalogSelectionSession.SelectedRodMaterial;
        CatalogSelectionSession.SelectedFabricMaterial = fabrics[selectedFabricIndex];
        CatalogSelectionSession.SelectedRodMaterial = rods[selectedRodIndex];
    }

    private Material CreateFallbackMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material material = new Material(shader) { name = name, color = color };
        runtimeMaterials.Add(material);
        return material;
    }

    private Material CreateTintedMaterial(Material source, string name, Color color)
    {
        Material material = new Material(source) { name = name, color = color };
        runtimeMaterials.Add(material);
        return material;
    }

    private static int FindPresetIndex(string[] names, string materialName)
    {
        if (string.IsNullOrWhiteSpace(materialName)) return 0;
        for (int i = 0; i < names.Length; i++)
            if (materialName.IndexOf(names[i], System.StringComparison.OrdinalIgnoreCase) >= 0) return i;
        return 0;
    }

    private void ConfigureMenuRow(Button button, string icon, string label, float top)
    {
        button.transform.SetParent(menuPanel.transform, false);
        RectTransform rect = button.transform as RectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(20f, -top);
        rect.sizeDelta = new Vector2(390f, 62f);
        button.gameObject.SetActive(true);
        StyleMenuButton(button, label, icon);
    }

    private Button CreateMenuRow(string name, string icon, string label, float top, System.Action action)
    {
        Button button = CreateButton(name, menuPanel.transform, new Vector2(0f, 1f), new Vector2(20f, -top), new Vector2(390f, 62f), new Color(0.12f, 0.17f, 0.21f, 0.96f), action);
        StyleMenuButton(button, label, icon);
        return button;
    }

    private void StyleMenuButton(Button button, string label, string icon)
    {
        Image background = button.GetComponent<Image>();
        background.sprite = ARUIIconFactory.RoundedSprite;
        background.type = Image.Type.Sliced;
        background.color = new Color(0.12f, 0.17f, 0.21f, 0.96f);
        button.targetGraphic = background;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.20f, 0.28f, 0.34f, 1f);
        colors.pressedColor = new Color(0.28f, 0.41f, 0.49f, 1f);
        button.colors = colors;

        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text == null)
        {
            text = CreateText("Label", button.transform, label, 17f, SoftWhite, TextAlignmentOptions.Left);
        }
        text.gameObject.SetActive(true);
        text.text = label;
        text.fontSize = 17f;
        text.fontStyle = FontStyles.Bold;
        text.color = SoftWhite;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.raycastTarget = false;
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = new Vector2(string.IsNullOrEmpty(icon) ? 18f : 62f, 3f);
        text.rectTransform.offsetMax = new Vector2(-12f, -3f);

        if (!string.IsNullOrEmpty(icon))
        {
            Transform iconTransform = button.transform.Find("ARIcon");
            if (iconTransform == null)
            {
                GameObject iconObject = new GameObject("ARIcon", typeof(RectTransform), typeof(Image));
                iconObject.transform.SetParent(button.transform, false);
                iconTransform = iconObject.transform;
            }
            RectTransform iconRect = iconTransform as RectTransform;
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(32f, 0f);
            iconRect.sizeDelta = new Vector2(52f, 52f);
            Image iconImage = iconTransform.GetComponent<Image>();
            ARUIIconFactory.SetIcon(iconImage, icon);
            iconImage.color = Accent;
        }
    }

    private void AddSectionLabel(Transform parent, string label, float top)
    {
        TMP_Text text = CreateText(label, parent, label, 14f, new Color(0.66f, 0.74f, 0.80f), TextAlignmentOptions.Left);
        PlaceTopLeft(text.rectTransform, new Vector2(24f, top), new Vector2(370f, 25f));
        text.fontStyle = FontStyles.Bold;
        text.characterSpacing = 1.2f;
    }

    private void BuildHorizontalSelector(Transform parent, string name, List<Material> materials, IReadOnlyList<string> labels, List<Image> swatches, float topOffset, System.Action<int> onSelected, Vector2 size, Vector2 anchor)
    {
        GameObject scrollObject = new GameObject(name, typeof(RectTransform), typeof(ScrollRect));
        scrollObject.transform.SetParent(parent, false);
        RectTransform scrollRect = scrollObject.GetComponent<RectTransform>();
        scrollRect.anchorMin = scrollRect.anchorMax = anchor;
        scrollRect.pivot = new Vector2(0f, 1f);
        scrollRect.anchoredPosition = new Vector2(0f, topOffset);
        scrollRect.sizeDelta = size;

        GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewportObject.transform.SetParent(scrollObject.transform, false);
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        Stretch(viewportRect, Vector2.zero, Vector2.zero);
        Image viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = Color.white;
        viewportImage.raycastTarget = true;
        viewportObject.GetComponent<Mask>().showMaskGraphic = false;

        GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        contentObject.transform.SetParent(viewportObject.transform, false);
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 0.5f);
        contentRect.anchorMax = new Vector2(0f, 0.5f);
        contentRect.pivot = new Vector2(0f, 0.5f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, size.y);
        HorizontalLayoutGroup layout = contentObject.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 7f;
        layout.padding = new RectOffset(8, 8, 3, 3);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = layout.childControlHeight = false;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = contentRect;
        scroll.horizontal = true;
        scroll.vertical = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = true;
        scroll.scrollSensitivity = 28f;

        for (int i = 0; i < materials.Count; i++)
        {
            int index = i;
            GameObject item = new GameObject(labels[i], typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            item.transform.SetParent(contentObject.transform, false);
            LayoutElement layoutElement = item.GetComponent<LayoutElement>();
            float itemWidth = name == "FabricSwatches" ? 160f : 82f;
            layoutElement.preferredWidth = itemWidth;
            layoutElement.preferredHeight = size.y - 4f;
            item.GetComponent<RectTransform>().sizeDelta = new Vector2(itemWidth, size.y - 4f);
            Image itemBackground = item.GetComponent<Image>();
            itemBackground.color = new Color(0f, 0f, 0f, 0f);
            Button itemButton = item.GetComponent<Button>();
            itemButton.targetGraphic = itemBackground;
            itemButton.onClick.AddListener(() => onSelected(index));

            GameObject swatchObject = new GameObject("Swatch", typeof(RectTransform), typeof(Image), typeof(Outline));
            swatchObject.transform.SetParent(item.transform, false);
            RectTransform swatchRect = swatchObject.GetComponent<RectTransform>();
            swatchRect.anchorMin = swatchRect.anchorMax = new Vector2(0.5f, 1f);
            swatchRect.pivot = new Vector2(0.5f, 1f);
            swatchRect.anchoredPosition = new Vector2(0f, -4f);
            swatchRect.sizeDelta = new Vector2(46f, 46f);
            Image swatch = swatchObject.GetComponent<Image>();
            swatch.sprite = ARUIIconFactory.CircleSprite;
            Material material = materials[i];
            swatch.color = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material.color;
            Texture2D texture = material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap") as Texture2D : material.mainTexture as Texture2D;
            if (texture != null)
            {
                Sprite thumbnail = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                runtimeSwatchSprites.Add(thumbnail);
                swatch.sprite = thumbnail;
                swatch.preserveAspect = true;
            }
            swatch.raycastTarget = false;
            Outline outline = swatchObject.GetComponent<Outline>();
            outline.effectColor = Accent;
            outline.effectDistance = new Vector2(3f, -3f);
            outline.useGraphicAlpha = false;
            outline.enabled = false;
            swatches.Add(swatch);

            TMP_Text label = CreateText("Label", item.transform, labels[i], 13f, SoftWhite, TextAlignmentOptions.Center);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, 1f);
            labelRect.sizeDelta = new Vector2(itemWidth, 38f);
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.maxVisibleLines = 2;
            label.raycastTarget = false;
        }
    }

    private Button CreateButton(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color color, System.Action callback)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = buttonObject.GetComponent<Image>();
        image.sprite = ARUIIconFactory.RoundedSprite;
        image.type = Image.Type.Sliced;
        image.color = color;
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        if (callback != null) button.onClick.AddListener(() => callback());
        return button;
    }

    private GameObject CreatePanel(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = panel.GetComponent<Image>();
        image.sprite = ARUIIconFactory.RoundedSprite;
        image.type = Image.Type.Sliced;
        image.color = color;
        return panel;
    }

    private TMP_Text CreateText(string name, Transform parent, string value, float size, Color color, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.font = TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.enableAutoSizing = false;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void PlaceTopLeft(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(position.x, -position.y);
        rect.sizeDelta = size;
    }

    private void SetMenuVisible(bool visible)
    {
        menuVisible = visible;
        if (menuPanel != null) menuPanel.SetActive(visible);
    }

    private void ToggleMenu() => SetMenuVisible(!menuVisible);

    private void ToggleFabricTray() => ToggleQuickSelector(fabricTray);
    private void ToggleRodTray() => ToggleQuickSelector(rodTray);
    private void ToggleModelTray() => ToggleQuickSelector(modelTray);

    private void ToggleQuickSelector(GameObject selector)
    {
        if (selector == null) return;
        bool show = activeQuickSelector != selector || !selector.activeSelf;
        if (fabricTray != null) fabricTray.SetActive(false);
        if (rodTray != null) rodTray.SetActive(false);
        if (modelTray != null) modelTray.SetActive(false);
        activeQuickSelector = show ? selector : null;
        if (show) selector.SetActive(true);
    }

    private void TogglePlanes()
    {
        if (surfaceUI != null) surfaceUI.ToggleFromCameraMenu();
        if (planesButtonText != null) planesButtonText.text = surfaceUI != null && surfaceUI.PlanesVisible ? "Planos: visíveis" : "Planos: ocultos";
    }

    private void SetPanelType(CurtainController.CurtainType type)
    {
        if (placementController != null) placementController.SetPanelType(type);
        VisitDraftSession.Current.panelType = type.ToString();
        UnityARHostBridge.SaveLastViewConfig();
        CurtainViewSettingsStore.SaveLast(CurtainViewSettingsStore.FromDraft(VisitDraftSession.Current));
        UpdateSelection();
    }

    private void SelectFabric(int index)
    {
        selectedFabricIndex = index;
        CatalogSelectionSession.SelectedFabricMaterial = fabrics[index];
        VisitDraftSession.Current.fabricMaterialName = fabricNames[index];
        VisitDraftSession.Current.fabricMaterialPath = fabricPaths[index];
        CurtainViewSettingsStore.SaveLast(CurtainViewSettingsStore.FromDraft(VisitDraftSession.Current));
        if (placementController != null) placementController.ApplyFabricMaterial(fabrics[index], fabricNames[index], fabricPaths[index]);
        UpdateSelection();
    }

    private void SelectRod(int index)
    {
        selectedRodIndex = index;
        CatalogSelectionSession.SelectedRodMaterial = rods[index];
        VisitDraftSession.Current.rodMaterialName = RodNames[index];
        VisitDraftSession.Current.rodMaterialPath = string.Empty;
        CurtainViewSettingsStore.SaveLast(CurtainViewSettingsStore.FromDraft(VisitDraftSession.Current));
        if (placementController != null) placementController.ApplyRodMaterial(rods[index], RodNames[index]);
        UpdateSelection();
    }

    private void UpdateSelection()
    {
        for (int i = 0; i < fabricSwatches.Count; i++)
        {
            Outline outline = fabricSwatches[i].GetComponent<Outline>();
            outline.enabled = i == selectedFabricIndex;
            fabricSwatches[i].rectTransform.localScale = i == selectedFabricIndex ? Vector3.one * 1.08f : Vector3.one;
        }
        for (int i = 0; i < rodSwatches.Count; i++)
        {
            Outline outline = rodSwatches[i].GetComponent<Outline>();
            outline.enabled = i == selectedRodIndex;
            rodSwatches[i].rectTransform.localScale = i == selectedRodIndex ? Vector3.one * 1.08f : Vector3.one;
        }
        if (placementController != null)
        {
            string activeModelName = placementController.ActiveModelName;
            for (int i = 0; i < modelOptionButtons.Count && i < modelOptions.Count; i++)
            {
                bool selected = modelOptions[i].name == activeModelName;
                modelOptionButtons[i].image.color = selected
                    ? new Color(0.16f, 0.41f, 0.57f, 1f)
                    : new Color(0.12f, 0.17f, 0.21f, 0.96f);
            }
        }
        if (planesButtonText != null && surfaceUI != null)
            planesButtonText.text = surfaceUI.PlanesVisible ? "Planos: visíveis" : "Planos: ocultos";
        if (textoModoGesto != null) textoModoGesto.text = modoRotacao ? "Gestos: girar" : "Gestos: aproximar";
    }

    private void AtivarPosicionamento()
    {
        modoPosicionar = !modoPosicionar;
        if (modoPosicionar) modoAjustar = false;
        UpdateControlState();
    }

    private void AtivarAjuste()
    {
        modoAjustar = !modoAjustar;
        if (modoAjustar) modoPosicionar = false;
        UpdateControlState();
    }

    private void AlternarModoGesto()
    {
        modoRotacao = !modoRotacao;
        UpdateControlState();
    }

    private void UpdateControlState()
    {
        ARUIIconFactory.SetSelected(btnPosicionar, modoPosicionar);
        ARUIIconFactory.SetSelected(btnAjustar, modoAjustar);
        ARUIIconFactory.SetSelected(addButton, modoPosicionar);
        ARUIIconFactory.SetSelected(editButton, modoAjustar);
        ARUIIconFactory.SetSelected(zoomButton, !modoRotacao);
        ARUIIconFactory.SetSelected(rotateButton, modoRotacao);
        if (zoomButton != null) zoomButton.gameObject.SetActive(modoAjustar);
        if (rotateButton != null) rotateButton.gameObject.SetActive(modoAjustar);
        if (editControls != null) editControls.GetComponent<Image>().color = modoAjustar ? Ink : Color.clear;
        if (textoModoGesto != null) textoModoGesto.text = modoRotacao ? "Gestos: girar" : "Gestos: aproximar";
    }

    public bool PosicionamentoAtivo() => modoPosicionar;
    public bool AjusteAtivo() => modoAjustar;
    public bool RotacaoAtiva() => modoRotacao;
}
