using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class HomeScreenBuilder
{
    private const string ScenePath = "Assets/Scenes/Home.unity";
    private const string ARScenePath = "Assets/Scenes/AR_Test.unity";
    private const string FontPath =
        "Assets/MobileARTemplateAssets/UI/Fonts/Inter-Regular_SDF.asset";

    private static readonly Color Navy = Hex("#172033");
    private static readonly Color Muted = Hex("#687386");
    private static readonly Color Blue = Hex("#286892");
    private static readonly Color PaleBlue = Hex("#EAF3F8");
    private static readonly Color Background = Hex("#F4F6F8");
    private static readonly Color Border = Hex("#D8DEE6");
    private static readonly Color Green = Hex("#278C5C");
    private static readonly Color Amber = Hex("#B67818");

    private static TMP_FontAsset font;
    private static Sprite roundedSprite;

    static HomeScreenBuilder()
    {
        EditorApplication.delayCall += BuildIfMissing;
    }

    [MenuItem("DecorIt/Build Home Screen")]
    public static void BuildFromMenu()
    {
        Build(replaceExisting: true);
    }

    private static void BuildIfMissing()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            Build(replaceExisting: false);
    }

    private static void Build(bool replaceExisting)
    {
        if (!replaceExisting && AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            return;

        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath) ??
               TMP_Settings.defaultFontAsset;
        roundedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        Scene previousScene = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);

        try
        {
            BuildHierarchy();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddScenesToBuildSettings();
            Debug.Log("DecorIt: tela Início criada em " + ScenePath);
        }
        finally
        {
            if (previousScene.IsValid() && previousScene.isLoaded)
                SceneManager.SetActiveScene(previousScene);

            EditorSceneManager.CloseScene(scene, true);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void BuildHierarchy()
    {
        GameObject canvasObject = new GameObject(
            "HomeCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(UnityEngine.UI.CanvasScaler),
            typeof(UnityEngine.UI.GraphicRaycaster));

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        UnityEngine.UI.CanvasScaler scaler =
            canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject background = CreateImage("Background", canvasObject.transform, Background);
        Stretch(background.GetComponent<RectTransform>());

        GameObject safeArea = CreateObject("SafeArea", canvasObject.transform);
        Stretch(safeArea.GetComponent<RectTransform>());
        safeArea.AddComponent<SafeAreaFitter>();

        BuildHeader(safeArea.transform);

        GameObject content = CreateObject("Content", safeArea.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(44f, 158f);
        contentRect.offsetMax = new Vector2(-44f, -330f);

        UnityEngine.UI.VerticalLayoutGroup contentLayout =
            content.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(0, 0, 0, 0);
        contentLayout.spacing = 20f;
        contentLayout.childAlignment = TextAnchor.UpperCenter;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        CreateDemoBadge(content.transform);
        CreateStatsRow(content.transform);
        UnityEngine.UI.Button newVisitButton = CreateNewVisitButton(content.transform);
        CreateSectionTitle(content.transform, "Registros recentes");
        CreateVisitCard(content.transform, "Maria Oliveira", "Sala de estar • 2 cômodos", "Pendente de sincronização", Amber);
        CreateVisitCard(content.transform, "Carlos Mendes", "Quarto casal • 1 cômodo", "Sincronizado", Green);
        CreateVisitCard(content.transform, "Ana Pereira", "Escritório • 1 cômodo", "Sincronizado", Green);

        BuildBottomNavigation(
            safeArea.transform,
            out UnityEngine.UI.Button clientsButton,
            out UnityEngine.UI.Button arButton,
            out UnityEngine.UI.Button syncButton);

        BuildNotice(safeArea.transform, out GameObject noticePanel, out TMP_Text noticeText);

        HomeScreenController controller = canvasObject.AddComponent<HomeScreenController>();
        controller.Configure(
            newVisitButton,
            clientsButton,
            arButton,
            syncButton,
            noticePanel,
            noticeText);

        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera));
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Background;
        camera.orthographic = true;
        camera.cullingMask = ~0;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1f;

        GameObject eventSystem = new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule));
        eventSystem.transform.SetParent(null);
    }

    private static void BuildHeader(Transform parent)
    {
        GameObject header = CreateImage("Header", parent, Blue);
        RectTransform rect = header.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(0f, -310f);
        rect.offsetMax = Vector2.zero;

        TMP_Text title = CreateText("Title", header.transform, "Início", 54f, Color.white, FontStyles.Bold);
        Place(title.rectTransform, new Vector2(44f, -72f), new Vector2(-260f, -142f), true);

        TMP_Text subtitle = CreateText(
            "Subtitle",
            header.transform,
            "Visitas e registros recentes",
            28f,
            new Color(1f, 1f, 1f, 0.82f));
        Place(subtitle.rectTransform, new Vector2(44f, -148f), new Vector2(-260f, -205f), true);

        GameObject offline = CreateImage("OfflineBadge", header.transform, Color.white);
        RectTransform offlineRect = offline.GetComponent<RectTransform>();
        offlineRect.anchorMin = new Vector2(1f, 1f);
        offlineRect.anchorMax = new Vector2(1f, 1f);
        offlineRect.pivot = new Vector2(1f, 1f);
        offlineRect.anchoredPosition = new Vector2(-44f, -74f);
        offlineRect.sizeDelta = new Vector2(190f, 66f);

        TMP_Text offlineText = CreateText("Label", offline.transform, "OFFLINE", 24f, Blue, FontStyles.Bold);
        Stretch(offlineText.rectTransform);
        offlineText.alignment = TextAlignmentOptions.Center;
    }

    private static void CreateDemoBadge(Transform parent)
    {
        GameObject badge = CreateImage("DemoDataBadge", parent, Hex("#FFF4D8"));
        SetLayoutHeight(badge, 58f);

        TMP_Text label = CreateText(
            "Label",
            badge.transform,
            "DADOS DEMONSTRATIVOS",
            22f,
            Hex("#8B5C13"),
            FontStyles.Bold);
        Stretch(label.rectTransform, 18f, 18f, 0f, 0f);
        label.alignment = TextAlignmentOptions.MidlineLeft;
    }

    private static void CreateStatsRow(Transform parent)
    {
        GameObject row = CreateObject("StatsRow", parent);
        SetLayoutHeight(row, 156f);

        UnityEngine.UI.HorizontalLayoutGroup layout =
            row.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        layout.spacing = 22f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = true;
        layout.childForceExpandWidth = true;

        CreateStatCard(row.transform, "Clientes", "18");
        CreateStatCard(row.transform, "Pendentes", "3");
    }

    private static void CreateStatCard(Transform parent, string label, string value)
    {
        GameObject card = CreateImage(label + "Card", parent, Color.white);
        card.AddComponent<UnityEngine.UI.LayoutElement>().flexibleWidth = 1f;
        AddOutline(card, Border);

        TMP_Text labelText = CreateText("Label", card.transform, label, 25f, Muted);
        Place(labelText.rectTransform, new Vector2(28f, -25f), new Vector2(-20f, -70f), true);

        TMP_Text valueText = CreateText("Value", card.transform, value, 52f, Blue, FontStyles.Bold);
        Place(valueText.rectTransform, new Vector2(28f, -72f), new Vector2(-20f, -138f), true);
    }

    private static UnityEngine.UI.Button CreateNewVisitButton(Transform parent)
    {
        GameObject buttonObject = CreateImage("NewVisitButton", parent, Blue);
        SetLayoutHeight(buttonObject, 130f);
        UnityEngine.UI.Button button = buttonObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = buttonObject.GetComponent<UnityEngine.UI.Image>();
        button.targetGraphic.raycastTarget = true;
        ConfigureButtonColors(button, Color.white, Hex("#D9EAF3"));

        TMP_Text title = CreateText(
            "Title",
            buttonObject.transform,
            "Novo registro de atendimento",
            31f,
            Color.white,
            FontStyles.Bold);
        Place(title.rectTransform, new Vector2(28f, -24f), new Vector2(-28f, -72f), true);

        TMP_Text subtitle = CreateText(
            "Subtitle",
            buttonObject.transform,
            "Cadastrar cliente, cômodo e medidas",
            24f,
            new Color(1f, 1f, 1f, 0.82f));
        Place(subtitle.rectTransform, new Vector2(28f, -74f), new Vector2(-28f, -112f), true);
        return button;
    }

    private static void CreateSectionTitle(Transform parent, string text)
    {
        TMP_Text label = CreateText("RecentSectionTitle", parent, text, 34f, Navy, FontStyles.Bold);
        SetLayoutHeight(label.gameObject, 48f);
        label.alignment = TextAlignmentOptions.MidlineLeft;
    }

    private static void CreateVisitCard(
        Transform parent,
        string client,
        string room,
        string status,
        Color statusColor)
    {
        GameObject card = CreateImage(client.Replace(" ", string.Empty) + "Card", parent, Color.white);
        SetLayoutHeight(card, 142f);
        AddOutline(card, Border);

        TMP_Text clientText = CreateText("Client", card.transform, client, 30f, Navy, FontStyles.Bold);
        Place(clientText.rectTransform, new Vector2(26f, -19f), new Vector2(-70f, -61f), true);

        TMP_Text roomText = CreateText("Room", card.transform, room, 23f, Muted);
        Place(roomText.rectTransform, new Vector2(26f, -61f), new Vector2(-70f, -96f), true);

        TMP_Text statusText = CreateText("Status", card.transform, status, 21f, statusColor);
        Place(statusText.rectTransform, new Vector2(26f, -98f), new Vector2(-70f, -129f), true);

        TMP_Text arrow = CreateText("Arrow", card.transform, ">", 36f, Muted, FontStyles.Bold);
        RectTransform arrowRect = arrow.rectTransform;
        arrowRect.anchorMin = new Vector2(1f, 0.5f);
        arrowRect.anchorMax = new Vector2(1f, 0.5f);
        arrowRect.pivot = new Vector2(1f, 0.5f);
        arrowRect.anchoredPosition = new Vector2(-24f, 0f);
        arrowRect.sizeDelta = new Vector2(36f, 54f);
        arrow.alignment = TextAlignmentOptions.Center;
    }

    private static void BuildBottomNavigation(
        Transform parent,
        out UnityEngine.UI.Button clientsButton,
        out UnityEngine.UI.Button arButton,
        out UnityEngine.UI.Button syncButton)
    {
        GameObject nav = CreateImage("BottomNavigation", parent, Color.white);
        RectTransform rect = nav.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = new Vector2(0f, 140f);
        AddOutline(nav, Border);

        UnityEngine.UI.HorizontalLayoutGroup layout =
            nav.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 8, 8);
        layout.spacing = 8f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = true;
        layout.childForceExpandWidth = true;

        CreateNavButton(nav.transform, "HomeButton", "I", "Início", Blue, out _);
        CreateNavButton(nav.transform, "ClientsButton", "C", "Clientes", Muted, out clientsButton);
        CreateNavButton(nav.transform, "ARButton", "RA", "RA", Muted, out arButton);
        CreateNavButton(nav.transform, "SyncButton", "S", "Sync", Muted, out syncButton);
    }

    private static void CreateNavButton(
        Transform parent,
        string name,
        string icon,
        string label,
        Color color,
        out UnityEngine.UI.Button button)
    {
        GameObject buttonObject = CreateObject(name, parent);
        UnityEngine.UI.Image hitArea = buttonObject.AddComponent<UnityEngine.UI.Image>();
        hitArea.color = new Color(1f, 1f, 1f, 0.001f);
        button = buttonObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = hitArea;

        TMP_Text iconText = CreateText("Icon", buttonObject.transform, icon, 29f, color, FontStyles.Bold);
        Place(iconText.rectTransform, new Vector2(0f, -10f), new Vector2(0f, -67f), true);
        iconText.alignment = TextAlignmentOptions.Center;

        TMP_Text labelText = CreateText("Label", buttonObject.transform, label, 21f, color);
        Place(labelText.rectTransform, new Vector2(0f, -70f), new Vector2(0f, -116f), true);
        labelText.alignment = TextAlignmentOptions.Center;
    }

    private static void BuildNotice(Transform parent, out GameObject panel, out TMP_Text label)
    {
        panel = CreateImage("NoticePanel", parent, Navy);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 158f);
        rect.sizeDelta = new Vector2(880f, 92f);

        label = CreateText("Message", panel.transform, string.Empty, 25f, Color.white);
        Stretch(label.rectTransform, 28f, 28f, 8f, 8f);
        label.alignment = TextAlignmentOptions.Center;
        panel.SetActive(false);
    }

    private static GameObject CreateObject(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static GameObject CreateImage(string name, Transform parent, Color color)
    {
        GameObject gameObject = CreateObject(name, parent);
        UnityEngine.UI.Image image = gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color = color;
        image.raycastTarget = false;

        if (roundedSprite != null)
        {
            image.sprite = roundedSprite;
            image.type = UnityEngine.UI.Image.Type.Sliced;
        }

        return gameObject;
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        string value,
        float size,
        Color color,
        FontStyles style = FontStyles.Normal)
    {
        GameObject gameObject = CreateObject(name, parent);
        TextMeshProUGUI text = gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.fontStyle = style;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }

    private static void ConfigureButtonColors(
        UnityEngine.UI.Button button,
        Color normal,
        Color pressed)
    {
        UnityEngine.UI.ColorBlock colors = button.colors;
        colors.normalColor = normal;
        colors.highlightedColor = normal;
        colors.selectedColor = normal;
        colors.pressedColor = pressed;
        colors.disabledColor = new Color(normal.r, normal.g, normal.b, 0.45f);
        colors.colorMultiplier = 1f;
        button.colors = colors;
    }

    private static void AddOutline(GameObject gameObject, Color color)
    {
        UnityEngine.UI.Outline outline = gameObject.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = true;
    }

    private static void SetLayoutHeight(GameObject gameObject, float height)
    {
        UnityEngine.UI.LayoutElement element =
            gameObject.GetComponent<UnityEngine.UI.LayoutElement>() ??
            gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        element.preferredHeight = height;
        element.minHeight = height;
        element.flexibleHeight = 0f;
        element.layoutPriority = 100;
    }

    private static void Stretch(
        RectTransform rect,
        float left = 0f,
        float right = 0f,
        float bottom = 0f,
        float top = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void Place(
        RectTransform rect,
        Vector2 topLeft,
        Vector2 bottomRight,
        bool stretchWidth)
    {
        rect.anchorMin = stretchWidth ? new Vector2(0f, 1f) : new Vector2(0f, 1f);
        rect.anchorMax = stretchWidth ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.offsetMin = new Vector2(topLeft.x, bottomRight.y);
        rect.offsetMax = new Vector2(bottomRight.x, topLeft.y);
    }

    private static void AddScenesToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes =
            EditorBuildSettings.scenes.Where(scene =>
                scene.path != ScenePath && scene.path != ARScenePath).ToList();

        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ARScenePath) != null)
            scenes.Insert(1, new EditorBuildSettingsScene(ARScenePath, true));

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static Color Hex(string value)
    {
        return ColorUtility.TryParseHtmlString(value, out Color color)
            ? color
            : Color.magenta;
    }
}
