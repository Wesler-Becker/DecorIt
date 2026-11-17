using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
public static class VerifyRailModel
{
    public static string Run()
    {
        var model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CurtainPrefab.prefab"));
        model.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            var controller = model.GetComponent<CurtainController>();
            var settings = new CurtainViewSettings {supportType="Rail"}; settings.EnsureLayers(); settings.AddLayerFrom(0); settings.layers[1].supportIndex=1;
            int cases=0;
            foreach(string panelType in new[]{"SinglePanel","DoublePanel"}) foreach(bool dual in new[]{false,true}) foreach(float open in new[]{0f,.5f,1f}) {
                foreach(var layer in settings.layers) layer.panelType=panelType; settings.doubleSupport=dual; settings.opening=open; settings.secondaryOpening=1-open; controller.ApplySettings(settings);
                var rails=model.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh!=null && f.sharedMesh.name=="Curtain rail downward C profile").ToArray();
                if(rails.Length!=(dual?2:1))throw new Exception("Incorrect channel count");
                foreach(var rail in rails){
                    Bounds b=rail.GetComponent<Renderer>().bounds;
                    if(Mathf.Abs(b.min.y-.020f)>.0001f)throw new Exception("Rail is not 20 mm above fabric");
                    var collider=rail.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=rail.sharedMesh;
                    RaycastHit middle,lip;
                    bool centerHit=collider.Raycast(new Ray(rail.transform.TransformPoint(new Vector3(0,-.02f,0)),Vector3.up),out middle,.05f);
                    bool lipHit=collider.Raycast(new Ray(rail.transform.TransformPoint(new Vector3(0,-.02f,.007f)),Vector3.up),out lip,.05f);
                    if(!centerHit||!lipHit||middle.distance-lip.distance<.008f)throw new Exception("Rail channel does not open downwards");
                    Object.DestroyImmediate(collider);
                }
                var hooks=model.GetComponentsInChildren<MeshFilter>().Single(f=>f.gameObject.name=="RailCarriersAndHooks");
                if(hooks.sharedMesh.vertexCount==0||hooks.sharedMesh.bounds.min.y<-.002f||hooks.sharedMesh.bounds.max.y>.030f)throw new Exception("Hooks are inverted or too tall");
                if(model.GetComponentsInChildren<Transform>().Any(t=>t.name=="DoubleRailBackplate")!=dual)throw new Exception("Incorrect shared backplate visibility");
                cases++;
            }
            settings.doubleSupport=true; settings.layers[1].spacingMillimeters=2; controller.ApplySettings(settings);
            var bodies=model.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh!=null && f.sharedMesh.name=="Curtain rail downward C profile").ToArray();
            if(Mathf.Abs(bodies[0].transform.position.z-bodies[1].transform.position.z)<.0199f)throw new Exception("Channels overlap at small cloth spacing");
            if(Mathf.Abs(controller.GetLayerClearanceMillimeters(1)-2)>.001f)throw new Exception("Cloth spacing changed");
            var stress = new CurtainViewSettings { supportType="Rail", doubleSupport=true, opening=1f, secondaryOpening=.5f };
            stress.EnsureLayers(); while(stress.layers.Count<8) stress.AddLayerFrom(0);
            for(int i=0;i<stress.layers.Count;i++){stress.layers[i].widthCentimeters=1000;stress.layers[i].foldsPerMeter=12;stress.layers[i].supportIndex=i%2;stress.layers[i].panelType="DoublePanel";}
            controller.ApplySettings(stress);
            var batch=model.GetComponentsInChildren<MeshFilter>().Single(f=>f.gameObject.name=="RailCarriersAndHooks");
            if(batch.sharedMesh.vertexCount>65535)throw new Exception("Hooks exceed 16-bit mesh capacity");
            settings.supportType="Rod";controller.ApplySettings(settings);
            if(model.GetComponentsInChildren<Transform>().Any(t=>t.name=="RailCarriersAndHooks"||t.name=="DoubleRailBackplate"))throw new Exception("Rail parts remain visible with rod");
            var rod=model.GetComponentsInChildren<MeshFilter>().First(f=>f.gameObject.name=="RodBody");
            if(Mathf.Abs(rod.transform.localPosition.y-(-.1f))>.0001f)throw new Exception("Rod offset changed");
            Render(model,controller,settings,true); settings.layers.RemoveAt(1); Render(model,controller,settings,false);
            return cases+" single/double/opening cases passed; downward slots, 20 mm clearance, short hooks, 2 mm fabric spacing and rod restoration verified. Images in Temp/RailReview.";
        }finally{Object.DestroyImmediate(model);}
    }
    static void Render(GameObject model,CurtainController controller,CurtainViewSettings settings,bool dual){
        settings.supportType="Rail";settings.doubleSupport=dual;settings.opening=0f;settings.secondaryOpening=0f;if(settings.layers.Count>1)settings.layers[1].spacingMillimeters=60;controller.ApplySettings(settings);
        var preview=new PreviewRenderUtility();
        var railMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));railMaterial.SetColor("_BaseColor",new Color(.78f,.8f,.81f));railMaterial.SetFloat("_Metallic",.55f);railMaterial.SetFloat("_Smoothness",.45f);
        var clothMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));clothMaterial.SetColor("_BaseColor",new Color(.35f,.5f,.56f));clothMaterial.SetFloat("_Cull",0f);
        try{
            preview.camera.orthographic=true;preview.camera.orthographicSize=.17f;
            preview.camera.transform.position=new Vector3(1.6f,-.35f,.5f);preview.camera.transform.LookAt(new Vector3(1.08f,-.065f,-.03f));
            preview.camera.nearClipPlane=.001f;preview.camera.farClipPlane=10;preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=new Color(.15f,.18f,.21f);
            preview.ambientColor=new Color(.5f,.5f,.5f);preview.lights[0].intensity=1.4f;preview.lights[0].transform.rotation=Quaternion.Euler(35,30,0);preview.lights[1].intensity=.6f;preview.lights[1].transform.rotation=Quaternion.Euler(310,200,0);
            preview.BeginStaticPreview(new Rect(0,0,1400,900));
            foreach(var filter in model.GetComponentsInChildren<MeshFilter>()){
                var renderer=filter.GetComponent<MeshRenderer>();if(renderer==null||!renderer.enabled||filter.sharedMesh==null)continue;
                var mat=filter.GetComponent<ProceduralCurtainMesh>()!=null?clothMaterial:filter.gameObject.name=="RailCarriersAndHooks"?renderer.sharedMaterial:railMaterial;
                preview.DrawMesh(filter.sharedMesh,filter.transform.localToWorldMatrix,mat,0);
            }
            preview.Render(true);var texture=preview.EndStaticPreview();Directory.CreateDirectory("Temp/RailReview");File.WriteAllBytes("Temp/RailReview/"+(dual?"double":"single")+".png",texture.EncodeToPNG());Object.DestroyImmediate(texture);
        }finally{preview.Cleanup();Object.DestroyImmediate(railMaterial);Object.DestroyImmediate(clothMaterial);}
    }
}
