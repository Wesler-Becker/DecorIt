using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
public static class VerifyClosedCarrier
{
    public static string Run()
    {
        var vertices = new List<Vector3>(); var triangles = new List<int>();
        typeof(CurtainController).Assembly.GetType("CurtainRailGeometry").GetMethod("Hook",BindingFlags.Public|BindingFlags.Static).Invoke(null,new object[]{vertices,triangles,Matrix4x4.identity,new Vector3(0,-.022f,0)});
        var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        var obj=new GameObject("Temporary closed carrier check"){hideFlags=HideFlags.HideAndDontSave};
        var collider=obj.AddComponent<MeshCollider>();collider.sharedMesh=mesh;
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.SetColor("_BaseColor",new Color(.88f,.9f,.91f));material.SetFloat("_Metallic",0);material.SetFloat("_Smoothness",.3f);
        var preview=new PreviewRenderUtility();
        try {
            RaycastHit hit;
            if(collider.Raycast(new Ray(new Vector3(0,-.014f,.03f),Vector3.back),out hit,.06f))throw new Exception("The eyelet hole is blocked");
            foreach(var point in new[]{new Vector3(0,-.0065f,.03f),new Vector3(0,-.0203f,.03f),new Vector3(.0048f,-.014f,.03f),new Vector3(-.0048f,-.014f,.03f)})
                if(!collider.Raycast(new Ray(point,Vector3.back),out hit,.06f))throw new Exception("The eyelet outline is open");
            if(vertices.Count>128)throw new Exception("Carrier exceeds batching budget");
            preview.camera.orthographic=true;preview.camera.orthographicSize=.016f;preview.camera.transform.position=new Vector3(.008f,.002f,.06f);preview.camera.transform.LookAt(new Vector3(0,-.009f,0));preview.camera.nearClipPlane=.001f;preview.camera.farClipPlane=1;
            preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=new Color(.12f,.15f,.18f);preview.ambientColor=new Color(.55f,.55f,.55f);
            preview.lights[0].intensity=1.4f;preview.lights[0].transform.rotation=Quaternion.Euler(30,25,0);preview.lights[1].intensity=.6f;
            preview.BeginStaticPreview(new Rect(0,0,900,1000));preview.DrawMesh(mesh,Matrix4x4.identity,material,0);preview.Render(true);var texture=preview.EndStaticPreview();
            Directory.CreateDirectory("Temp/RailReview");File.WriteAllBytes("Temp/RailReview/closed-carrier.png",texture.EncodeToPNG());Object.DestroyImmediate(texture);
            return "Closed D-shaped eyelet: hole clear, all four sides present; "+vertices.Count+" vertices. Preview: Temp/RailReview/closed-carrier.png";
        }finally {preview.Cleanup();Object.DestroyImmediate(obj);Object.DestroyImmediate(mesh);Object.DestroyImmediate(material);}
    }
}
