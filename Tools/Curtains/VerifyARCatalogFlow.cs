using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.XR.ARFoundation;
using Object=UnityEngine.Object;
public static class VerifyARCatalogFlow
{
    static readonly BindingFlags Fields=BindingFlags.NonPublic|BindingFlags.Instance;
    static void Set(object target,string field,object value)=>target.GetType().GetField(field,Fields).SetValue(target,value);
    static T Get<T>(object target,string field)=>(T)target.GetType().GetField(field,Fields).GetValue(target);
    static void Check(bool value,string error){if(!value)throw new Exception(error);}
    static async Task Wait(Func<bool> ready){for(int i=0;i<120;i++){if(ready())return;await Task.Delay(50);}throw new Exception("Scene transition timed out");}
    static CatalogScreenController Catalog()=>SceneManager.GetSceneByName("Catalog").GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CatalogScreenController>()).Single();
    public static async Task<string> Run()
    {
        Check(Application.isPlaying,"Run in Play Mode");
        int frame=Time.frameCount;await Task.Delay(150);Check(Time.frameCount>frame,"Editor frames are not advancing");
        var previous=SceneManager.GetActiveScene();var scene=SceneManager.CreateScene("ARCatalogFlowTest");SceneManager.SetActiveScene(scene);
        var root=new GameObject("Test placement");var placement=root.AddComponent<ARPlacementController>();var surfaces=root.AddComponent<ARSurfaceUI>();
        var manipulator=root.AddComponent<ARObjectManipulator>();Set(placement,"objectManipulator",manipulator);
        var canvas=new GameObject("Test AR canvas",typeof(Canvas),typeof(GraphicRaycaster)).GetComponent<Canvas>();
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CurtainPrefab.prefab");Set(placement,"objectToPlace",prefab);
        try{
            var settings=new CurtainViewSettings{modelName="Retired basic model",widthCentimeters=320,heightCentimeters=210,supportType="Rail",doubleSupport=true};settings.EnsureLayers();settings.AddLayerFrom(0);settings.layers[1].supportIndex=1;
            CurtainViewSettingsStore.SaveLast(settings);VisitDraftSession.Current=new VisitDraft();CatalogSelectionSession.HasConfiguredCurtain=false;CatalogSelectionSession.CurrentConfiguration=null;CatalogSelectionSession.SelectedCurtainPrefab=null;
            placement.InitializeCurtainSettings();Check(CatalogSelectionSession.SelectedCurtainPrefab==prefab,"Wrong fallback prefab");Check(CatalogSelectionSession.CurrentConfiguration.widthCentimeters==320,"Last dimensions were discarded");
            var selected=Object.Instantiate(prefab,new Vector3(2,0,4),Quaternion.Euler(0,33,0));selected.transform.localScale=Vector3.one*.8f;
            Set(placement,"spawnedObject",selected);placement.ApplyCatalogSelection();
            var other=Object.Instantiate(prefab,new Vector3(-2,0,4),Quaternion.identity);other.GetComponent<CurtainController>().UpdateCurtain();float otherWidth=Get<float>(other.GetComponent<CurtainController>(),"widthCm");
            foreach(bool visible in new[]{false,true}){
                if(surfaces.PlanesVisible!=visible)surfaces.ToggleFromCameraMenu();
                Vector3 position=selected.transform.position;Quaternion rotation=selected.transform.rotation;Vector3 scale=selected.transform.localScale;
                Set(manipulator,"wasDragging",true);Set(manipulator,"wasUsingTwoFingers",true); ARCatalogSession.Open(placement);Check(!Get<bool>(manipulator,"wasDragging")&&!Get<bool>(manipulator,"wasUsingTwoFingers"),"Stale gestures survived catalog suspension");Check(!placement.enabled&&!canvas.enabled,"AR input or canvas remains active in catalog");
                await Wait(()=>SceneManager.GetSceneByName("Catalog").isLoaded&&SceneManager.GetActiveScene().name=="Catalog");
                await Wait(()=>Get<TextField>(Catalog(),"widthField")!=null);await Task.Delay(150);
                var catalog=Catalog();Get<TextField>(catalog,"widthField").value=visible?"410":"375";Get<TextField>(catalog,"heightField").value="245";
                if(visible)catalog.Back();else catalog.ApplySelection();
                await Wait(()=>!SceneManager.GetSceneByName("Catalog").isLoaded&&placement.enabled);
                Check(SceneManager.GetActiveScene()==scene,"AR scene was replaced");Check(canvas.enabled,"AR canvas was not restored");
                Check(Get<GameObject>(placement,"spawnedObject")==selected,"Selected object was recreated");
                Check(selected.transform.position==position&&selected.transform.rotation==rotation&&selected.transform.localScale==scale,"Object transform changed");
                Check(Mathf.Abs(Get<float>(selected.GetComponent<CurtainController>(),"widthCm")-(visible?410:375))<.001f,"Existing object did not receive catalog dimensions");
                Check(Get<float>(other.GetComponent<CurtainController>(),"widthCm")==otherWidth,"Another placed object changed");
                Check(surfaces.PlanesVisible==visible,"Plane choice changed after catalog");Check(!Get<bool>(placement,"createNewObjectOnNextPlacement"),"Editing armed duplicate placement");
            }
            Set(placement,"spawnedObject",null);Object.Destroy(selected);await Task.Delay(100);
            ARCatalogSession.Open(placement);await Wait(()=>SceneManager.GetSceneByName("Catalog").isLoaded&&SceneManager.GetActiveScene().name=="Catalog");await Task.Delay(150);Catalog().ApplySelection();
            await Wait(()=>!SceneManager.GetSceneByName("Catalog").isLoaded&&placement.enabled);Check(Get<bool>(placement,"createNewObjectOnNextPlacement"),"No-object configuration did not arm placement");
            var place=typeof(ARPlacementController).GetMethod("TryPlaceObject",Fields);place.Invoke(placement,new object[]{new Vector2(Screen.width/2,Screen.height/2)});
            var added=Get<GameObject>(placement,"spawnedObject");Check(added!=null&&added.name.StartsWith("CurtainPrefab("),"Last configured model was not placed");
            Check(Get<float>(added.GetComponent<CurtainController>(),"widthCm")==410,"Placed model uses raw prefab dimensions");
            place.Invoke(placement,new object[]{new Vector2(Screen.width/2,Screen.height/2)});Check(Get<GameObject>(placement,"spawnedObject")==added,"Second touch added another object");
            var planeObject=new GameObject("Test plane",typeof(ARPlane),typeof(MeshRenderer),typeof(LineRenderer));
            var visibility=typeof(ARSurfaceUI).GetMethod("SetPlaneVisibility",BindingFlags.NonPublic|BindingFlags.Static);
            visibility.Invoke(null,new object[]{planeObject.GetComponent<ARPlane>(),false});planeObject.SetActive(false);planeObject.SetActive(true);
            foreach(var renderer in planeObject.GetComponents<Renderer>()){renderer.enabled=true;Check(renderer.forceRenderingOff,"Hidden plane drawing returned");}
            visibility.Invoke(null,new object[]{planeObject.GetComponent<ARPlane>(),true});Check(planeObject.GetComponents<Renderer>().All(r=>!r.forceRenderingOff),"Showing planes failed");
            Check(planeObject.activeSelf,"Plane detection object was disabled");
            return "PASS: 3 real catalog round trips; confirm and back preserve selected object, other objects, transforms and plane choices; last settings and canonical prefab restored; placement occurs once; plane drawing stays suppressed after reactivation.";
        }finally{
            if(SceneManager.GetSceneByName("Catalog").isLoaded)_ = SceneManager.UnloadSceneAsync("Catalog");
            if(previous.isLoaded)SceneManager.SetActiveScene(previous);
            _ = SceneManager.UnloadSceneAsync(scene);
        }
    }
}
