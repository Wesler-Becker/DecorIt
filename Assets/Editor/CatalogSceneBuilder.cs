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
public static class CatalogSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Catalog.unity";
    private const string FontPath = "Assets/MobileARTemplateAssets/UI/Fonts/Inter-Regular_SDF.asset";
    private static readonly Color Blue = Hex("#286892"), Navy = Hex("#172033"), Muted = Hex("#687386"), Background = Hex("#F4F6F8"), Border = Hex("#D8DEE6"), PaleBlue = Hex("#EAF3F8");
    private static TMP_FontAsset font;
    private static Sprite rounded;

    static CatalogSceneBuilder() => EditorApplication.delayCall += BuildIfMissing;
    [MenuItem("DecorIt/Build Catalog Screen")] public static void BuildFromMenu() => Build(true);
    private static void BuildIfMissing() { if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) Build(false); }

    private static void Build(bool replaceExisting)
    {
        if (!replaceExisting && AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) return;
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath) ?? TMP_Settings.defaultFontAsset;
        rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        GameObject curtain = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CurtainPrefab.prefab");
        Material fabric = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/CurtainFabric.mat");
        Material rod = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/RodMaterial.mat");
        Scene previous = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try { BuildHierarchy(curtain, fabric, rod); EditorSceneManager.SaveScene(scene, ScenePath); AddBuildScenes(); }
        finally { if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene, true); }
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
    }

    private static void BuildHierarchy(GameObject curtain, Material fabric, Material rod)
    {
        GameObject canvasObject = new GameObject("CatalogCanvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        Canvas canvas=canvasObject.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceCamera;
        var scaler=canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1080,1920); scaler.matchWidthOrHeight=.5f;
        Stretch(Image("Background",canvasObject.transform,Background).GetComponent<RectTransform>());
        GameObject safe=Create("SafeArea",canvasObject.transform); Stretch(safe.GetComponent<RectTransform>()); safe.AddComponent<SafeAreaFitter>();

        GameObject header=Image("Header",safe.transform,Blue); Top(header.GetComponent<RectTransform>(),0,0,0,300);
        UnityEngine.UI.Button back=Button("Back","‹",Color.white,Blue,header.transform,48); Top(back.GetComponent<RectTransform>(),28,42,960,132);
        TMP_Text title=Text("Title",header.transform,"Catálogo",52,Color.white,FontStyles.Bold); Top(title.rectTransform,138,60,44,128);
        TMP_Text subtitle=Text("Subtitle",header.transform,"Modelos, tecidos e acabamentos",27,new Color(1,1,1,.82f)); Top(subtitle.rectTransform,44,205,44,258);

        UnityEngine.UI.Button curtains=Button("CurtainsTab","Cortinas",Color.white,Blue,safe.transform,24); Top(curtains.GetComponent<RectTransform>(),44,330,700,414);
        UnityEngine.UI.Button blinds=Button("BlindsTab","Persianas",Muted,Color.white,safe.transform,24); Top(blinds.GetComponent<RectTransform>(),370,330,44,414); Outline(blinds.gameObject,Border);
        UnityEngine.UI.Button accessories=Button("AccessoriesTab","Acessórios",Muted,Color.white,safe.transform,24); Top(accessories.GetComponent<RectTransform>(),44,430,44,514); Outline(accessories.gameObject,Border);

        TMP_Text category=Text("CategoryDescription",safe.transform,"Modelo disponível",25,Blue,FontStyles.Bold); Top(category.rectTransform,44,535,44,578);
        GameObject modelCard=Image("ModelCard",safe.transform,Color.white); Top(modelCard.GetComponent<RectTransform>(),44,590,44,850); Outline(modelCard,Border);
        TMP_Text modelTitle=Text("ModelTitle",modelCard.transform,"Cortina sob medida",32,Navy,FontStyles.Bold); Top(modelTitle.rectTransform,24,24,24,73);
        TMP_Text model=Text("ModelDescription",modelCard.transform,string.Empty,24,Muted); Top(model.rectTransform,24,83,24,236); model.textWrappingMode=TextWrappingModes.Normal;

        TMP_Text variantTitle=Text("VariantTitle",safe.transform,"Configuração do tecido",25,Navy,FontStyles.Bold); Top(variantTitle.rectTransform,44,874,44,918);
        UnityEngine.UI.Button single=Button("SinglePanel","Painel único",Muted,Color.white,safe.transform,24); Top(single.GetComponent<RectTransform>(),44,930,700,1010); Outline(single.gameObject,Border);
        UnityEngine.UI.Button doublePanel=Button("DoublePanel","Painel duplo",Color.white,Blue,safe.transform,24); Top(doublePanel.GetComponent<RectTransform>(),718,930,44,1010);

        TMP_Text materialsTitle=Text("MaterialsTitle",safe.transform,"Materiais disponíveis no projeto",25,Navy,FontStyles.Bold); Top(materialsTitle.rectTransform,44,1032,44,1080);
        GameObject fabricCard=Image("FabricMaterialCard",safe.transform,Color.white); Top(fabricCard.GetComponent<RectTransform>(),44,1092,44,1280); Outline(fabricCard,Border);
        UnityEngine.UI.Image fabricSwatch=Image("FabricSwatch",fabricCard.transform,fabric!=null?fabric.color:Color.white).GetComponent<UnityEngine.UI.Image>(); Top(fabricSwatch.rectTransform,20,56,880,148);
        TMP_Text fabricTitle=Text("FabricTitle",fabricCard.transform,"Tecido · CurtainFabric",24,Navy,FontStyles.Bold); Top(fabricTitle.rectTransform,132,30,320,76);
        TMP_Text fabricInfo=Text("FabricInfo",fabricCard.transform,"Material da malha procedural",19,Muted); Top(fabricInfo.rectTransform,132,88,320,140);
        UnityEngine.UI.Button fabricSelect=Button("SelectFabric","Selecionar",Blue,PaleBlue,fabricCard.transform,20); Top(fabricSelect.GetComponent<RectTransform>(),760,56,18,142);

        GameObject rodCard=Image("RodMaterialCard",safe.transform,Color.white); Top(rodCard.GetComponent<RectTransform>(),44,1292,44,1480); Outline(rodCard,Border);
        UnityEngine.UI.Image rodSwatch=Image("RodSwatch",rodCard.transform,rod!=null?rod.color:Color.white).GetComponent<UnityEngine.UI.Image>(); Top(rodSwatch.rectTransform,20,56,880,148);
        TMP_Text rodTitle=Text("RodTitle",rodCard.transform,"Varão e ponteiras · RodMaterial",23,Navy,FontStyles.Bold); Top(rodTitle.rectTransform,132,30,320,76);
        TMP_Text rodInfo=Text("RodInfo",rodCard.transform,"Acabamento do CurtainPrefab",19,Muted); Top(rodInfo.rectTransform,132,88,320,140);
        UnityEngine.UI.Button rodSelect=Button("SelectRodMaterial","Selecionar",Blue,PaleBlue,rodCard.transform,20); Top(rodSelect.GetComponent<RectTransform>(),760,56,18,142);

        GameObject summaryCard=Image("SelectionSummaryCard",safe.transform,PaleBlue); Bottom(summaryCard.GetComponent<RectTransform>(),44,44,165,305); Outline(summaryCard,Border);
        TMP_Text summary=Text("SelectionSummary",summaryCard.transform,string.Empty,22,Navy); Stretch(summary.rectTransform,20,20,12,12); summary.textWrappingMode=TextWrappingModes.Normal;
        UnityEngine.UI.Button apply=Button("ApplySelection","Usar seleção",Color.white,Blue,safe.transform,28); Bottom(apply.GetComponent<RectTransform>(),44,44,34,154);

        CatalogScreenController controller=canvasObject.AddComponent<CatalogScreenController>(); controller.Configure(category,model,summary,fabricSwatch,rodSwatch,single,doublePanel,apply,curtain,fabric,rod);
        UnityEventTools.AddPersistentListener(back.onClick,controller.BackToVisit); UnityEventTools.AddPersistentListener(curtains.onClick,controller.ShowCurtains);
        UnityEventTools.AddPersistentListener(blinds.onClick,controller.ShowBlinds); UnityEventTools.AddPersistentListener(accessories.onClick,controller.ShowAccessories);
        UnityEventTools.AddPersistentListener(single.onClick,controller.SelectSinglePanel); UnityEventTools.AddPersistentListener(doublePanel.onClick,controller.SelectDoublePanel);
        UnityEventTools.AddPersistentListener(fabricSelect.onClick,controller.SelectFabric); UnityEventTools.AddPersistentListener(rodSelect.onClick,controller.SelectRodMaterial);
        UnityEventTools.AddPersistentListener(apply.onClick,controller.ApplySelection);

        GameObject cameraObject=new GameObject("Main Camera",typeof(Camera)); cameraObject.tag="MainCamera"; Camera camera=cameraObject.GetComponent<Camera>(); camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Background; camera.orthographic=true; canvas.worldCamera=camera; canvas.planeDistance=1;
        new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
    }

    private static GameObject Create(string name,Transform parent){var o=new GameObject(name,typeof(RectTransform));o.transform.SetParent(parent,false);return o;}
    private static GameObject Image(string name,Transform parent,Color color){GameObject o=Create(name,parent);var i=o.AddComponent<UnityEngine.UI.Image>();i.color=color;i.raycastTarget=false;if(rounded!=null){i.sprite=rounded;i.type=UnityEngine.UI.Image.Type.Sliced;}return o;}
    private static TMP_Text Text(string name,Transform parent,string value,float size,Color color,FontStyles style=FontStyles.Normal){GameObject o=Create(name,parent);var t=o.AddComponent<TextMeshProUGUI>();t.text=value;t.font=font;t.fontSize=size;t.color=color;t.fontStyle=style;t.textWrappingMode=TextWrappingModes.NoWrap;t.overflowMode=TextOverflowModes.Truncate;t.raycastTarget=false;return t;}
    private static UnityEngine.UI.Button Button(string name,string label,Color foreground,Color background,Transform parent,float size){GameObject o=Image(name,parent,background);var image=o.GetComponent<UnityEngine.UI.Image>();image.raycastTarget=true;var b=o.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=image;var text=Text("Label",o.transform,label,size,foreground,FontStyles.Bold);Stretch(text.rectTransform,8,8,6,6);text.alignment=TextAlignmentOptions.Center;return b;}
    private static void Outline(GameObject o,Color color){var x=o.AddComponent<UnityEngine.UI.Outline>();x.effectColor=color;x.effectDistance=new Vector2(1,-1);}
    private static void Stretch(RectTransform r,float left=0,float right=0,float bottom=0,float top=0){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(left,bottom);r.offsetMax=new Vector2(-right,-top);}
    private static void Top(RectTransform r,float left,float top,float right,float bottom){r.anchorMin=new Vector2(0,1);r.anchorMax=new Vector2(1,1);r.pivot=new Vector2(0,1);r.offsetMin=new Vector2(left,-bottom);r.offsetMax=new Vector2(-right,-top);}
    private static void Bottom(RectTransform r,float left,float right,float bottom,float top){r.anchorMin=Vector2.zero;r.anchorMax=new Vector2(1,0);r.pivot=new Vector2(.5f,0);r.offsetMin=new Vector2(left,bottom);r.offsetMax=new Vector2(-right,top);}
    private static Color Hex(string value)=>ColorUtility.TryParseHtmlString(value,out Color c)?c:Color.magenta;
    private static void AddBuildScenes(){string[] order={"Assets/Scenes/Home.unity","Assets/Scenes/Clients.unity","Assets/Scenes/NewClient.unity","Assets/Scenes/ClientProfile.unity","Assets/Scenes/NewVisit.unity",ScenePath,"Assets/Scenes/AR_Test.unity"};var list=new List<EditorBuildSettingsScene>();foreach(string path in order)if(AssetDatabase.LoadAssetAtPath<SceneAsset>(path)!=null)list.Add(new EditorBuildSettingsScene(path,true));list.AddRange(EditorBuildSettings.scenes.Where(s=>!order.Contains(s.path)));EditorBuildSettings.scenes=list.ToArray();}
}
