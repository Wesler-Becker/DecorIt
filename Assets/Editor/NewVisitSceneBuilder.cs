using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class NewVisitSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/NewVisit.unity";
    private const string HomePath = "Assets/Scenes/Home.unity";
    private const string ARPath = "Assets/Scenes/AR_Test.unity";
    private const string CatalogPath = "Assets/Scenes/Catalog.unity";
    private const string FontPath = "Assets/MobileARTemplateAssets/UI/Fonts/Inter-Regular_SDF.asset";

    private static readonly Color Navy = Hex("#172033");
    private static readonly Color Muted = Hex("#687386");
    private static readonly Color Blue = Hex("#286892");
    private static readonly Color Background = Hex("#F4F6F8");
    private static readonly Color Border = Hex("#D8DEE6");
    private static readonly Color Error = Hex("#B42318");
    private static TMP_FontAsset font;
    private static Sprite roundedSprite;

    static NewVisitSceneBuilder() => EditorApplication.delayCall += BuildIfMissing;

    [MenuItem("DecorIt/Build New Visit Flow")]
    public static void BuildFromMenu() => Build(true);

    private static void BuildIfMissing()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) Build(false);
    }

    private static void Build(bool replaceExisting)
    {
        if (!replaceExisting && AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) return;
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath) ?? TMP_Settings.defaultFontAsset;
        roundedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        Scene previous = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            BuildHierarchy();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddBuildScenes();
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("DecorIt: fluxo Novo atendimento criado em " + ScenePath);
    }

    private static void BuildHierarchy()
    {
        GameObject canvasObject = new GameObject("NewVisitCanvas", typeof(RectTransform), typeof(Canvas),
            typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Stretch(CreateImage("Background", canvasObject.transform, Background).GetComponent<RectTransform>());
        GameObject safeArea = CreateObject("SafeArea", canvasObject.transform);
        Stretch(safeArea.GetComponent<RectTransform>());
        safeArea.AddComponent<SafeAreaFitter>();

        NewVisitFlowController controller = canvasObject.AddComponent<NewVisitFlowController>();
        TMP_Text stepLabel;
        TMP_Text titleLabel;
        BuildHeader(safeArea.transform, controller, out stepLabel, out titleLabel);

        GameObject body = CreateObject("Body", safeArea.transform);
        RectTransform bodyRect = body.GetComponent<RectTransform>();
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = Vector2.one;
        bodyRect.offsetMin = new Vector2(44f, 44f);
        bodyRect.offsetMax = new Vector2(-44f, -330f);

        TMP_InputField clientName, phone, address, visitNotes, roomName, openingName, width, height, roomNotes;
        GameObject clientStep = BuildClientStep(body.transform, out clientName, out phone, out address);
        GameObject visitStep = BuildVisitStep(body.transform, out visitNotes);
        GameObject roomStep = BuildRoomStep(body.transform, out roomName, out openingName, out width, out height, out roomNotes);
        GameObject modelStep = BuildModelStep(body.transform, controller, out TMP_Text modelSelection, out UnityEngine.UI.Button editCatalog);
        GameObject reviewStep = BuildReviewStep(body.transform, controller, out TMP_Text summary);
        GameObject[] steps = { clientStep, visitStep, roomStep, modelStep, reviewStep };
        for (int i = 1; i < steps.Length; i++) steps[i].SetActive(false);

        TMP_Text error = CreateText("ValidationMessage", safeArea.transform, string.Empty, 24f, Error, FontStyles.Bold);
        RectTransform errorRect = error.rectTransform;
        errorRect.anchorMin = new Vector2(0f, 0f);
        errorRect.anchorMax = new Vector2(1f, 0f);
        errorRect.offsetMin = new Vector2(44f, 202f);
        errorRect.offsetMax = new Vector2(-44f, 254f);
        error.alignment = TextAlignmentOptions.Center;

        GameObject nextButton = BuildFooter(safeArea.transform, controller);
        controller.Configure(steps, stepLabel, titleLabel, error, summary, nextButton, modelSelection, editCatalog, clientName, phone, address,
            visitNotes, roomName, openingName, width, height, roomNotes);

        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera));
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Background;
        camera.orthographic = true;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1f;
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    private static GameObject BuildModelStep(Transform parent, NewVisitFlowController controller, out TMP_Text selection, out UnityEngine.UI.Button edit)
    {
        GameObject panel = StepPanel("ModelStep", parent, "Modelo e material", "Escolha o produto e confira a combinação selecionada.");
        GameObject card = CreateImage("SelectedProductCard", panel.transform, Color.white);
        Place(card.GetComponent<RectTransform>(), new Vector2(0f, -205f), new Vector2(0f, -520f));
        AddOutline(card, Border);
        selection = CreateText("Selection", card.transform, string.Empty, 29f, Navy);
        Stretch(selection.rectTransform, 28f, 28f, 24f, 24f);
        selection.textWrappingMode = TextWrappingModes.Normal;
        edit = CreateButton("EditCatalogButton", panel.transform, "Abrir catálogo", Blue, Color.white, 30f);
        Place(edit.GetComponent<RectTransform>(), new Vector2(0f, -560f), new Vector2(0f, -690f));
        UnityEventTools.AddPersistentListener(edit.onClick, controller.EditCatalog);
        return panel;
    }

    private static void BuildHeader(Transform parent, NewVisitFlowController controller,
        out TMP_Text stepLabel, out TMP_Text titleLabel)
    {
        GameObject header = CreateImage("Header", parent, Blue);
        RectTransform rect = header.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = Vector2.one; rect.pivot = new Vector2(.5f, 1f);
        rect.offsetMin = new Vector2(0f, -300f); rect.offsetMax = Vector2.zero;

        UnityEngine.UI.Button back = CreateButton("BackButton", header.transform, "‹", Color.white, Blue, 48f);
        RectTransform backRect = back.GetComponent<RectTransform>();
        backRect.anchorMin = backRect.anchorMax = new Vector2(0f, 1f); backRect.pivot = new Vector2(0f, 1f);
        backRect.anchoredPosition = new Vector2(32f, -56f); backRect.sizeDelta = new Vector2(88f, 88f);
        UnityEventTools.AddPersistentListener(back.onClick, controller.Back);

        stepLabel = CreateText("Step", header.transform, "ETAPA 1 DE 4", 22f, new Color(1f,1f,1f,.78f), FontStyles.Bold);
        Place(stepLabel.rectTransform, new Vector2(138f, -57f), new Vector2(-44f, -94f));
        titleLabel = CreateText("Title", header.transform, "Cliente", 52f, Color.white, FontStyles.Bold);
        Place(titleLabel.rectTransform, new Vector2(44f, -142f), new Vector2(-44f, -215f));
        TMP_Text subtitle = CreateText("Subtitle", header.transform, "Novo registro de atendimento", 27f, new Color(1f,1f,1f,.82f));
        Place(subtitle.rectTransform, new Vector2(44f, -220f), new Vector2(-44f, -265f));
    }

    private static GameObject BuildClientStep(Transform parent, out TMP_InputField name, out TMP_InputField phone, out TMP_InputField address)
    {
        GameObject panel = StepPanel("ClientStep", parent, "Quem será atendido?", "Cadastre os dados principais do cliente.");
        name = CreateField(panel.transform, "ClientName", "Nome completo *", "Ex.: Maria Oliveira", 210f);
        phone = CreateField(panel.transform, "Phone", "Telefone (opcional)", "(00) 00000-0000", 410f);
        phone.contentType = TMP_InputField.ContentType.Custom;
        phone.keyboardType = TouchScreenKeyboardType.PhonePad;
        address = CreateField(panel.transform, "Address", "Endereço", "Rua, número e bairro", 610f);
        return panel;
    }

    private static GameObject BuildVisitStep(Transform parent, out TMP_InputField notes)
    {
        GameObject panel = StepPanel("VisitStep", parent, "Dados da visita", "Registre o contexto antes de medir o ambiente.");
        GameObject dateCard = CreateImage("DateCard", panel.transform, Color.white);
        Place(dateCard.GetComponent<RectTransform>(), new Vector2(0f, -205f), new Vector2(0f, -335f));
        AddOutline(dateCard, Border);
        TMP_Text date = CreateText("Date", dateCard.transform, System.DateTime.Now.ToString("dd/MM/yyyy"), 32f, Navy, FontStyles.Bold);
        Stretch(date.rectTransform, 28f, 28f, 10f, 10f); date.alignment = TextAlignmentOptions.MidlineLeft;
        notes = CreateField(panel.transform, "VisitNotes", "Observações da visita", "Preferências, necessidades e horários", 400f, 250f, true);
        return panel;
    }

    private static GameObject BuildRoomStep(Transform parent, out TMP_InputField room, out TMP_InputField opening,
        out TMP_InputField width, out TMP_InputField height, out TMP_InputField notes)
    {
        GameObject panel = StepPanel("RoomStep", parent, "Medidas do ambiente", "Use centímetros. As medidas poderão orientar o modelo em RA.");
        room = CreateField(panel.transform, "RoomName", "Cômodo *", "Ex.: Sala de estar", 185f);
        opening = CreateField(panel.transform, "OpeningName", "Abertura", "Ex.: Janela principal", 365f);
        width = CreateField(panel.transform, "Width", "Largura (cm) *", "Ex.: 220", 545f, 145f);
        height = CreateField(panel.transform, "Height", "Altura (cm) *", "Ex.: 180", 710f, 145f);
        width.contentType = height.contentType = TMP_InputField.ContentType.DecimalNumber;
        notes = CreateField(panel.transform, "RoomNotes", "Observações", "Detalhes de instalação", 875f, 180f, true);
        return panel;
    }

    private static GameObject BuildReviewStep(Transform parent, NewVisitFlowController controller, out TMP_Text summary)
    {
        GameObject panel = StepPanel("ReviewStep", parent, "Revise o registro", "Confira os dados antes de salvar ou iniciar a visualização.");
        GameObject card = CreateImage("SummaryCard", panel.transform, Color.white);
        Place(card.GetComponent<RectTransform>(), new Vector2(0f, -190f), new Vector2(0f, -865f));
        AddOutline(card, Border);
        summary = CreateText("Summary", card.transform, string.Empty, 27f, Navy);
        Stretch(summary.rectTransform, 30f, 30f, 26f, 26f);
        summary.textWrappingMode = TextWrappingModes.Normal;
        summary.overflowMode = TextOverflowModes.Truncate;

        UnityEngine.UI.Button ar = CreateButton("OpenARButton", panel.transform, "Visualizar em RA", Color.white, Blue, 30f);
        Place(ar.GetComponent<RectTransform>(), new Vector2(0f, -905f), new Vector2(0f, -1035f));
        UnityEventTools.AddPersistentListener(ar.onClick, controller.OpenAR);
        UnityEngine.UI.Button save = CreateButton("SaveButton", panel.transform, "Salvar registro", Blue, Color.white, 28f);
        Place(save.GetComponent<RectTransform>(), new Vector2(0f, -1055f), new Vector2(0f, -1175f));
        AddOutline(save.gameObject, Blue);
        UnityEventTools.AddPersistentListener(save.onClick, controller.SaveAndReturnHome);
        return panel;
    }

    private static GameObject StepPanel(string name, Transform parent, string heading, string intro)
    {
        GameObject panel = CreateObject(name, parent); Stretch(panel.GetComponent<RectTransform>());
        TMP_Text title = CreateText("Heading", panel.transform, heading, 38f, Navy, FontStyles.Bold);
        Place(title.rectTransform, new Vector2(0f, -18f), new Vector2(0f, -72f));
        TMP_Text subtitle = CreateText("Intro", panel.transform, intro, 25f, Muted);
        Place(subtitle.rectTransform, new Vector2(0f, -82f), new Vector2(0f, -155f));
        subtitle.textWrappingMode = TextWrappingModes.Normal;
        return panel;
    }

    private static TMP_InputField CreateField(Transform parent, string name, string label, string placeholder,
        float top, float height = 150f, bool multiline = false)
    {
        TMP_Text labelText = CreateText(name + "Label", parent, label, 25f, Navy, FontStyles.Bold);
        Place(labelText.rectTransform, new Vector2(0f, -top), new Vector2(0f, -(top + 40f)));
        GameObject fieldObject = CreateImage(name, parent, Color.white);
        Place(fieldObject.GetComponent<RectTransform>(), new Vector2(0f, -(top + 50f)), new Vector2(0f, -(top + height)));
        AddOutline(fieldObject, Border);
        UnityEngine.UI.Image image = fieldObject.GetComponent<UnityEngine.UI.Image>(); image.raycastTarget = true;
        TMP_InputField field = fieldObject.AddComponent<TMP_InputField>();
        field.targetGraphic = image;
        field.lineType = multiline ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine;

        TMP_Text hint = CreateText("Placeholder", fieldObject.transform, placeholder, 25f, Hex("#9AA3B2"));
        Stretch(hint.rectTransform, 25f, 25f, 12f, 12f); hint.textWrappingMode = TextWrappingModes.Normal;
        TMP_Text text = CreateText("Text", fieldObject.transform, string.Empty, 27f, Navy);
        Stretch(text.rectTransform, 25f, 25f, 12f, 12f); text.textWrappingMode = TextWrappingModes.Normal;
        field.placeholder = hint;
        field.textComponent = text;
        field.textViewport = fieldObject.GetComponent<RectTransform>();
        return field;
    }

    private static GameObject BuildFooter(Transform parent, NewVisitFlowController controller)
    {
        GameObject footer = CreateImage("Footer", parent, Color.white);
        RectTransform rect = footer.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(.5f, 0f); rect.offsetMin = Vector2.zero; rect.offsetMax = new Vector2(0f, 185f);
        AddOutline(footer, Border);
        UnityEngine.UI.Button next = CreateButton("NextButton", footer.transform, "Continuar", Color.white, Blue, 30f);
        Stretch(next.GetComponent<RectTransform>(), 44f, 44f, 30f, 30f);
        UnityEventTools.AddPersistentListener(next.onClick, controller.Next);
        return next.gameObject;
    }

    private static UnityEngine.UI.Button CreateButton(string name, Transform parent, string label, Color textColor, Color background, float size)
    {
        GameObject obj = CreateImage(name, parent, background);
        UnityEngine.UI.Image image = obj.GetComponent<UnityEngine.UI.Image>(); image.raycastTarget = true;
        UnityEngine.UI.Button button = obj.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
        TMP_Text text = CreateText("Label", obj.transform, label, size, textColor, FontStyles.Bold);
        Stretch(text.rectTransform, 12f, 12f, 8f, 8f); text.alignment = TextAlignmentOptions.Center;
        return button;
    }

    private static GameObject CreateObject(string name, Transform parent)
    { GameObject obj = new GameObject(name, typeof(RectTransform)); obj.transform.SetParent(parent, false); return obj; }
    private static GameObject CreateImage(string name, Transform parent, Color color)
    { GameObject obj = CreateObject(name, parent); var image = obj.AddComponent<UnityEngine.UI.Image>(); image.color = color; image.raycastTarget = false; if (roundedSprite != null) { image.sprite = roundedSprite; image.type = UnityEngine.UI.Image.Type.Sliced; } return obj; }
    private static TMP_Text CreateText(string name, Transform parent, string value, float size, Color color, FontStyles style = FontStyles.Normal)
    { var obj = CreateObject(name, parent); var text = obj.AddComponent<TextMeshProUGUI>(); text.text = value; text.font = font; text.fontSize = size; text.color = color; text.fontStyle = style; text.textWrappingMode = TextWrappingModes.NoWrap; text.overflowMode = TextOverflowModes.Truncate; text.raycastTarget = false; return text; }
    private static void AddOutline(GameObject obj, Color color)
    { var outline = obj.AddComponent<UnityEngine.UI.Outline>(); outline.effectColor = color; outline.effectDistance = new Vector2(1f, -1f); }
    private static void Stretch(RectTransform rect, float left = 0f, float right = 0f, float bottom = 0f, float top = 0f)
    { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = new Vector2(left, bottom); rect.offsetMax = new Vector2(-right, -top); }
    private static void Place(RectTransform rect, Vector2 topLeft, Vector2 bottomRight)
    { rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = new Vector2(1f, 1f); rect.pivot = new Vector2(0f, 1f); rect.offsetMin = new Vector2(topLeft.x, bottomRight.y); rect.offsetMax = new Vector2(bottomRight.x, topLeft.y); }
    private static Color Hex(string value) => ColorUtility.TryParseHtmlString(value, out Color color) ? color : Color.magenta;

    private static void AddBuildScenes()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.Where(s => s.path != HomePath && s.path != ScenePath && s.path != CatalogPath && s.path != ARPath).ToList();
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(HomePath) != null) scenes.Insert(0, new EditorBuildSettingsScene(HomePath, true));
        scenes.Insert(1, new EditorBuildSettingsScene(ScenePath, true));
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(CatalogPath) != null) scenes.Insert(2, new EditorBuildSettingsScene(CatalogPath, true));
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ARPath) != null) scenes.Insert(AssetDatabase.LoadAssetAtPath<SceneAsset>(CatalogPath) != null ? 3 : 2, new EditorBuildSettingsScene(ARPath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
