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
public static class ClientScenesBuilder
{
    private const string ClientsPath = "Assets/Scenes/Clients.unity";
    private const string NewClientPath = "Assets/Scenes/NewClient.unity";
    private const string ProfilePath = "Assets/Scenes/ClientProfile.unity";
    private const string NewRoomPath = "Assets/Scenes/NewRoom.unity";
    private const string RoomDetailPath = "Assets/Scenes/RoomDetail.unity";
    private const string FontPath = "Assets/MobileARTemplateAssets/UI/Fonts/Inter-Regular_SDF.asset";
    private static readonly Color Blue = Hex("#286892"), Navy = Hex("#172033"), Muted = Hex("#687386"),
        Background = Hex("#F4F6F8"), Border = Hex("#D8DEE6"), Pale = Hex("#EAF3F8"), Error = Hex("#B42318");
    private static TMP_FontAsset font;
    private static Sprite rounded;

    static ClientScenesBuilder() => EditorApplication.delayCall += BuildIfMissing;
    [MenuItem("DecorIt/Build Client Screens")] public static void BuildFromMenu() => Build();
    private static void BuildIfMissing()
    { if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ClientsPath) == null) Build(); }

    private static void Build()
    {
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath) ?? TMP_Settings.defaultFontAsset;
        rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        Scene previous = SceneManager.GetActiveScene();
        try { BuildClients(); BuildNewClient(); BuildNewRoom(); BuildProfile(); BuildRoomDetail(); AddBuildScenes(); }
        finally { if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous); }
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
    }

    private static void BuildClients()
    {
        Scene scene = NewScene();
        GameObject canvas = BaseCanvas("ClientsCanvas", out Transform safe);
        ClientsScreenController controller = canvas.AddComponent<ClientsScreenController>();
        Header(safe, "Clientes", "Cadastros e ambientes", controller.BackHome);

        GameObject list = CreateObject("ClientList", safe);
        RectTransform listRect = list.GetComponent<RectTransform>();
        listRect.anchorMin = Vector2.zero; listRect.anchorMax = Vector2.one;
        listRect.offsetMin = new Vector2(44f, 190f); listRect.offsetMax = new Vector2(-44f, -330f);
        Transform clientItems = ScrollContent(list.transform, "ClientItems");
        GameObject template = ClientCard(clientItems); template.SetActive(false);
        controller.Configure(clientItems, template);
        UnityEngine.UI.Button add = Button("AddClient", safe, "+  Novo cliente", Color.white, Blue, 30f);
        SetBottom(add.GetComponent<RectTransform>(), 44f, 44f, 34f, 154f);
        UnityEventTools.AddPersistentListener(add.onClick, controller.OpenNewClient);
        SaveAndClose(scene, ClientsPath);
    }

    private static GameObject ClientCard(Transform parent)
    {
        GameObject card = Image("ClientCardTemplate", parent, Color.white); Height(card, 170f); Outline(card, Border);
        var button = card.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = card.GetComponent<UnityEngine.UI.Image>();
        TMP_Text name = Text("Name", card.transform, "Nome", 30f, Navy, FontStyles.Bold); Place(name.rectTransform, 26f, 18f, 80f, 58f);
        TMP_Text phone = Text("Phone", card.transform, "Telefone", 23f, Muted); Place(phone.rectTransform, 26f, 63f, 80f, 98f);
        TMP_Text rooms = Text("Rooms", card.transform, "Cômodos", 22f, Blue); Place(rooms.rectTransform, 26f, 108f, 80f, 144f);
        TMP_Text arrow = Text("Arrow", card.transform, ">", 36f, Muted, FontStyles.Bold); Place(arrow.rectTransform, 0f, 54f, 22f, 116f);
        arrow.alignment = TextAlignmentOptions.Center;
        return card;
    }

    private static void BuildNewClient()
    {
        Scene scene = NewScene();
        GameObject canvas = BaseCanvas("NewClientCanvas", out Transform safe);
        NewClientController controller = canvas.AddComponent<NewClientController>();
        Header(safe, "Novo cliente", "Dados para futuros atendimentos", controller.Back);
        GameObject form = CreateObject("Form", safe); Stretch(form.GetComponent<RectTransform>(), 44f, 44f, 210f, 340f);
        TMP_InputField name = Field(form.transform, "Name", "Nome completo *", "Ex.: Maria Oliveira", 20f);
        TMP_InputField phone = Field(form.transform, "Phone", "Telefone (opcional)", "(00) 00000-0000", 225f);
        phone.contentType = TMP_InputField.ContentType.Custom; phone.keyboardType = TouchScreenKeyboardType.PhonePad;
        TMP_InputField address = Field(form.transform, "Address", "Endereço", "Rua, número e bairro", 430f);
        TMP_Text error = Text("Error", safe, string.Empty, 24f, Error, FontStyles.Bold); SetBottom(error.rectTransform, 44f, 44f, 174f, 224f); error.alignment = TextAlignmentOptions.Center;
        UnityEngine.UI.Button save = Button("Save", safe, "Salvar cliente", Color.white, Blue, 30f); SetBottom(save.GetComponent<RectTransform>(), 44f, 44f, 34f, 154f);
        controller.Configure(name, phone, address, error); UnityEventTools.AddPersistentListener(save.onClick, controller.Save);
        SaveAndClose(scene, NewClientPath);
    }

    private static void BuildProfile()
    {
        Scene scene = NewScene();
        GameObject canvas = BaseCanvas("ClientProfileCanvas", out Transform safe);
        ClientProfileController controller = canvas.AddComponent<ClientProfileController>();
        Header(safe, "Perfil do cliente", "Dados, cômodos e imagens", controller.Back);

        TMP_Text name = Text("ClientName", safe, "Cliente", 36f, Navy, FontStyles.Bold); Place(name.rectTransform, 44f, 330f, 44f, 385f);
        GameObject contact = Image("ContactCard", safe, Color.white); Place(contact.GetComponent<RectTransform>(), 44f, 400f, 44f, 495f); Outline(contact, Border);
        TMP_Text details = Text("Contact", contact.transform, string.Empty, 24f, Muted); Place(details.rectTransform, 28f, 20f, 28f, 78f); details.textWrappingMode = TextWrappingModes.Normal;
        UnityEngine.UI.Button roomsTab = Button("RoomsTab", safe, "Cômodos", Color.white, Blue, 25f); Place(roomsTab.GetComponent<RectTransform>(), 44f, 525f, 544f, 610f);
        UnityEngine.UI.Button photosTab = Button("PhotosTab", safe, "Fotos", Navy, Color.white, 25f); Place(photosTab.GetComponent<RectTransform>(), 544f, 525f, 44f, 610f); Outline(photosTab.gameObject, Border);
        GameObject roomsPane = CreateObject("RoomsPanel", safe); Place(roomsPane.GetComponent<RectTransform>(), 44f, 640f, 44f, 1510f);
        GameObject photosPane = CreateObject("PhotosPanel", safe); Place(photosPane.GetComponent<RectTransform>(), 44f, 640f, 44f, 1510f);
        Transform content = ScrollContent(roomsPane.transform, "Rooms");
        GameObject template = Image("RoomTemplate", content, Color.white); Height(template, 150f); Outline(template, Border); template.AddComponent<UnityEngine.UI.Button>().targetGraphic = template.GetComponent<UnityEngine.UI.Image>(); template.SetActive(false);
        TMP_Text roomName = Text("RoomName", template.transform, "Cômodo", 29f, Navy, FontStyles.Bold); Place(roomName.rectTransform, 24f, 22f, 24f, 66f);
        TMP_Text image = Text("ImageStatus", template.transform, "Sem imagem", 23f, Muted); Place(image.rectTransform, 24f, 78f, 24f, 120f);
        Transform photos = ScrollContent(photosPane.transform, "Photos");
        GameObject photoTemplate = Image("PhotoTemplate", photos, Hex("#E7ECF1")); Height(photoTemplate, 220f); Outline(photoTemplate, Border); photoTemplate.SetActive(false);
        TMP_Text photoLabel = Text("PhotoLabel", photoTemplate.transform, "Cômodo", 22f, Navy, FontStyles.Bold); Place(photoLabel.rectTransform, 15f, 15f, 15f, 52f);
        controller.Configure(name, details, content, template, roomsPane, photosPane, photos, photoTemplate);
        UnityEventTools.AddPersistentListener(roomsTab.onClick, controller.ShowRooms);
        UnityEventTools.AddPersistentListener(photosTab.onClick, controller.ShowPhotos);
        UnityEngine.UI.Button visit = Button("NewRoom", safe, "+  Novo cômodo", Color.white, Blue, 30f); SetBottom(visit.GetComponent<RectTransform>(), 44f, 44f, 34f, 154f);
        UnityEventTools.AddPersistentListener(visit.onClick, controller.NewRoom);
        SaveAndClose(scene, ProfilePath);
    }

    private static void BuildNewRoom()
    {
        Scene scene = NewScene();
        GameObject canvas = BaseCanvas("NewRoomCanvas", out Transform safe);
        NewRoomController controller = canvas.AddComponent<NewRoomController>();
        Header(safe, "Novo cômodo", "Organize os registros por ambiente", controller.Back);
        GameObject form = CreateObject("Form", safe); Stretch(form.GetComponent<RectTransform>(), 44f, 44f, 210f, 340f);
        TMP_InputField room = Field(form.transform, "RoomName", "Nome do cômodo *", "Ex.: Sala de estar", 20f);
        TMP_Text error = Text("Error", safe, string.Empty, 24f, Error, FontStyles.Bold); SetBottom(error.rectTransform, 44f, 44f, 174f, 224f); error.alignment = TextAlignmentOptions.Center;
        UnityEngine.UI.Button save = Button("Save", safe, "Salvar cômodo", Color.white, Blue, 30f); SetBottom(save.GetComponent<RectTransform>(), 44f, 44f, 34f, 154f);
        controller.Configure(room, error); UnityEventTools.AddPersistentListener(save.onClick, controller.Save);
        SaveAndClose(scene, NewRoomPath);
    }

    private static void BuildRoomDetail()
    {
        Scene scene = NewScene();
        GameObject canvas = BaseCanvas("RoomDetailCanvas", out Transform safe);
        RoomDetailController controller = canvas.AddComponent<RoomDetailController>();
        Header(safe, "Cômodo", "Registros e fotos deste ambiente", controller.Back);
        TMP_Text roomName = Text("RoomName", safe, "Cômodo", 34f, Navy, FontStyles.Bold); Place(roomName.rectTransform, 44f, 330f, 44f, 390f);
        TMP_Text recordsTitle = Text("RecordsTitle", safe, "Registros", 27f, Navy, FontStyles.Bold); Place(recordsTitle.rectTransform, 44f, 420f, 44f, 466f);
        GameObject recordsView = CreateObject("RecordsView", safe); Place(recordsView.GetComponent<RectTransform>(), 44f, 480f, 44f, 940f);
        Transform records = ScrollContent(recordsView.transform, "Records");
        GameObject recordTemplate = Image("RecordTemplate", records, Color.white); Height(recordTemplate, 150f); Outline(recordTemplate, Border); recordTemplate.SetActive(false);
        TMP_Text recordName = Text("RecordName", recordTemplate.transform, "Registro", 25f, Navy, FontStyles.Bold); Place(recordName.rectTransform, 20f, 18f, 20f, 57f);
        TMP_Text recordDetails = Text("RecordDetails", recordTemplate.transform, "Detalhes", 21f, Muted); Place(recordDetails.rectTransform, 20f, 68f, 20f, 130f); recordDetails.textWrappingMode = TextWrappingModes.Normal;
        TMP_Text photosTitle = Text("PhotosTitle", safe, "Fotos do cômodo", 27f, Navy, FontStyles.Bold); Place(photosTitle.rectTransform, 44f, 975f, 44f, 1021f);
        GameObject photosView = CreateObject("PhotosView", safe); Place(photosView.GetComponent<RectTransform>(), 44f, 1035f, 44f, 1510f);
        Transform photos = ScrollContent(photosView.transform, "Photos");
        GameObject photoTemplate = Image("PhotoTemplate", photos, Hex("#E7ECF1")); Height(photoTemplate, 200f); Outline(photoTemplate, Border); photoTemplate.SetActive(false);
        TMP_Text photoLabel = Text("PhotoLabel", photoTemplate.transform, "Foto", 22f, Navy, FontStyles.Bold); Place(photoLabel.rectTransform, 15f, 15f, 15f, 52f);
        controller.Configure(roomName, records, recordTemplate, photos, photoTemplate);
        UnityEngine.UI.Button newRecord = Button("NewRecord", safe, "+  Novo registro", Color.white, Blue, 30f); SetBottom(newRecord.GetComponent<RectTransform>(), 44f, 44f, 34f, 154f);
        UnityEventTools.AddPersistentListener(newRecord.onClick, controller.NewRecord);
        SaveAndClose(scene, RoomDetailPath);
    }

    private static Scene NewScene() { Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive); SceneManager.SetActiveScene(scene); return scene; }
    private static void SaveAndClose(Scene scene, string path) { EditorSceneManager.SaveScene(scene, path); EditorSceneManager.CloseScene(scene, true); }
    private static GameObject BaseCanvas(string name, out Transform safe)
    {
        GameObject canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1080,1920); scaler.matchWidthOrHeight=.5f;
        Stretch(Image("Background", canvasObject.transform, Background).GetComponent<RectTransform>());
        GameObject safeObject = CreateObject("SafeArea", canvasObject.transform); Stretch(safeObject.GetComponent<RectTransform>()); safeObject.AddComponent<SafeAreaFitter>(); safe = safeObject.transform;
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera)); cameraObject.tag="MainCamera"; Camera camera=cameraObject.GetComponent<Camera>(); camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Background; camera.orthographic=true; canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=1;
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); return canvasObject;
    }
    private static Transform ScrollContent(Transform parent, string name)
    {
        GameObject scrollObject = CreateObject(name + "Scroll", parent); Stretch(scrollObject.GetComponent<RectTransform>());
        UnityEngine.UI.ScrollRect scroll = scrollObject.AddComponent<UnityEngine.UI.ScrollRect>();
        scroll.horizontal = false; scroll.vertical = true; scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 60f;
        GameObject viewport = Image("Viewport", scrollObject.transform, new Color(1f, 1f, 1f, .01f)); Stretch(viewport.GetComponent<RectTransform>());
        UnityEngine.UI.Mask mask = viewport.AddComponent<UnityEngine.UI.Mask>(); mask.showMaskGraphic = false;
        scroll.viewport = viewport.GetComponent<RectTransform>();
        GameObject content = CreateObject("Content", viewport.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>(); contentRect.anchorMin = new Vector2(0f, 1f); contentRect.anchorMax = Vector2.one; contentRect.pivot = new Vector2(.5f, 1f); contentRect.offsetMin = Vector2.zero; contentRect.offsetMax = Vector2.zero;
        var layout = content.AddComponent<UnityEngine.UI.VerticalLayoutGroup>(); layout.spacing = 18f; layout.childControlHeight = true; layout.childControlWidth = true; layout.childForceExpandHeight = false; layout.childForceExpandWidth = true;
        var fitter = content.AddComponent<UnityEngine.UI.ContentSizeFitter>(); fitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained; fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = contentRect;
        return content.transform;
    }
    private static void Header(Transform parent, string title, string subtitle, UnityEngine.Events.UnityAction backAction)
    {
        GameObject header=Image("Header",parent,Blue); Place(header.GetComponent<RectTransform>(),0,0,0,300);
        UnityEngine.UI.Button back=Button("Back",header.transform,"‹",Color.white,Blue,48); Place(back.GetComponent<RectTransform>(),24,45,960,135); UnityEventTools.AddPersistentListener(back.onClick,backAction);
        TMP_Text t=Text("Title",header.transform,title,50,Color.white,FontStyles.Bold); Place(t.rectTransform,44,145,44,210);
        TMP_Text s=Text("Subtitle",header.transform,subtitle,27,new Color(1,1,1,.82f)); Place(s.rectTransform,44,220,44,265);
    }
    private static TMP_InputField Field(Transform parent,string name,string label,string placeholder,float top)
    {
        TMP_Text l=Text(name+"Label",parent,label,25,Navy,FontStyles.Bold); Place(l.rectTransform,0,top,0,top+42);
        GameObject obj=Image(name,parent,Color.white); Place(obj.GetComponent<RectTransform>(),0,top+55,0,top+165); Outline(obj,Border); var image=obj.GetComponent<UnityEngine.UI.Image>(); image.raycastTarget=true;
        TMP_InputField field=obj.AddComponent<TMP_InputField>(); field.targetGraphic=image;
        TMP_Text hint=Text("Placeholder",obj.transform,placeholder,25,Hex("#9AA3B2")); Stretch(hint.rectTransform,24,24,10,10);
        TMP_Text value=Text("Text",obj.transform,string.Empty,27,Navy); Stretch(value.rectTransform,24,24,10,10); field.placeholder=hint; field.textComponent=value; field.textViewport=obj.GetComponent<RectTransform>(); return field;
    }
    private static UnityEngine.UI.Button Button(string name,Transform parent,string label,Color foreground,Color background,float size)
    { GameObject obj=Image(name,parent,background); var image=obj.GetComponent<UnityEngine.UI.Image>(); image.raycastTarget=true; var button=obj.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic=image; TMP_Text text=Text("Label",obj.transform,label,size,foreground,FontStyles.Bold); Stretch(text.rectTransform,10,10,8,8); text.alignment=TextAlignmentOptions.Center; return button; }
    private static GameObject CreateObject(string name,Transform parent){GameObject obj=new GameObject(name,typeof(RectTransform));obj.transform.SetParent(parent,false);return obj;}
    private static GameObject Image(string name,Transform parent,Color color){GameObject obj=CreateObject(name,parent);var image=obj.AddComponent<UnityEngine.UI.Image>();image.color=color;image.raycastTarget=false;if(rounded!=null){image.sprite=rounded;image.type=UnityEngine.UI.Image.Type.Sliced;}return obj;}
    private static TMP_Text Text(string name,Transform parent,string value,float size,Color color,FontStyles style=FontStyles.Normal){var obj=CreateObject(name,parent);var text=obj.AddComponent<TextMeshProUGUI>();text.text=value;text.font=font;text.fontSize=size;text.color=color;text.fontStyle=style;text.textWrappingMode=TextWrappingModes.NoWrap;text.overflowMode=TextOverflowModes.Ellipsis;text.raycastTarget=false;return text;}
    private static void Outline(GameObject obj,Color color){var o=obj.AddComponent<UnityEngine.UI.Outline>();o.effectColor=color;o.effectDistance=new Vector2(1,-1);}
    private static void Height(GameObject obj,float height){var e=obj.AddComponent<UnityEngine.UI.LayoutElement>();e.preferredHeight=height;e.minHeight=height;e.flexibleHeight=0;e.layoutPriority=100;}
    private static void Stretch(RectTransform rect,float left=0,float right=0,float bottom=0,float top=0){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(left,bottom);rect.offsetMax=new Vector2(-right,-top);}
    private static void Place(RectTransform rect,float left,float top,float right,float bottom){rect.anchorMin=new Vector2(0,1);rect.anchorMax=new Vector2(1,1);rect.pivot=new Vector2(0,1);rect.offsetMin=new Vector2(left,-bottom);rect.offsetMax=new Vector2(-right,-top);}
    private static void SetBottom(RectTransform rect,float left,float right,float bottom,float top){rect.anchorMin=Vector2.zero;rect.anchorMax=new Vector2(1,0);rect.offsetMin=new Vector2(left,bottom);rect.offsetMax=new Vector2(-right,top);}
    private static Color Hex(string value)=>ColorUtility.TryParseHtmlString(value,out Color c)?c:Color.magenta;
    private static void AddBuildScenes()
    {
        string[] order={"Assets/Scenes/Home.unity",ClientsPath,NewClientPath,ProfilePath,NewRoomPath,RoomDetailPath,"Assets/Scenes/NewVisit.unity","Assets/Scenes/Catalog.unity","Assets/Scenes/AR_Test.unity"};
        List<EditorBuildSettingsScene> rest=EditorBuildSettings.scenes.Where(s=>!order.Contains(s.path)).ToList();
        var result=new List<EditorBuildSettingsScene>(); foreach(string path in order) if(AssetDatabase.LoadAssetAtPath<SceneAsset>(path)!=null) result.Add(new EditorBuildSettingsScene(path,true)); result.AddRange(rest); EditorBuildSettings.scenes=result.ToArray();
    }
}
