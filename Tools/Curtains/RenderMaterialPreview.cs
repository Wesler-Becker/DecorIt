using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Run with unity command run_script; does not open or save the user's scene.
public static class RenderMaterialPreview
{
    public static string Main()
    {
        string folder = "Docs/CurtainMaterialReview";
        Directory.CreateDirectory(folder);
        string[] names = {
            "voil_trend_branco_p4_15652", "voil_trend_bege_p4_15653", "voil_trend_cinza_p4_15654",
            "boucle_cortina_loft_marrom_p6_15651", "tela_creare_branco_p10_15655",
            "voil_urban_cinza_p50_14426", "bordados_ramos_linho_p72_14405",
            "linho_absoluto_linho_p18_15632", "blackout_giardino_bege_p82_15661",
            "linho_voil_strip_211_branco_p76_14557", "voil_lumina_branco_p54_15629"
        };
        foreach (string name in names)
        {
            Render(name, folder + "/" + name + "-300x200.png", false);
            Render(name, folder + "/" + name + "-detail.png", true);
        }
        return folder;
    }

    public static void Render(string name, string path, bool detail)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/DecorItCurtains/Materials/Active/" + name + ".mat");
        if (material == null) throw new InvalidOperationException(name);
        var preview = new PreviewRenderUtility();
        var cloth = new GameObject("Temporary 300 x 200 cm preview") { hideFlags = HideFlags.HideAndDontSave };
        var backdrop = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        var bar = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        var quad = new Mesh();
        try
        {
            var generator = cloth.AddComponent<ProceduralCurtainMesh>();
            generator.Configure(3f, 2f, 16, 0.045f, 1f);
            Mesh mesh = cloth.GetComponent<MeshFilter>().sharedMesh;
            preview.camera.orthographic = true;
            float detailHeight = name.StartsWith("bordados_") ? 0.5f :
                name.StartsWith("linho_voil_strip_") || name.StartsWith("voil_lumina_") ? 0.28f : 0.05f;
            preview.camera.orthographicSize = detail ? detailHeight / 2f : 1.18f;
            preview.camera.transform.position = new Vector3(detail ? 0.12f : 0, -1, -4);
            preview.camera.transform.rotation = Quaternion.identity;
            preview.camera.nearClipPlane = 0.01f;
            preview.camera.farClipPlane = 20;
            preview.camera.clearFlags = CameraClearFlags.SolidColor;
            preview.camera.backgroundColor = new Color(0.08f, 0.1f, 0.12f, 1);
            preview.ambientColor = new Color(0.42f, 0.42f, 0.42f, 1);
            preview.lights[0].intensity = 1.1f;
            preview.lights[0].transform.rotation = Quaternion.Euler(30, 25, 0);
            preview.lights[1].intensity = 0.5f;
            preview.lights[1].transform.rotation = Quaternion.Euler(320, 210, 0);
            backdrop.SetColor("_BaseColor", new Color(0.12f, 0.27f, 0.34f, 1));
            bar.SetColor("_BaseColor", new Color(0.62f, 0.37f, 0.2f, 1));
            quad.vertices = new[] { new Vector3(-0.5f,-0.5f,0),new Vector3(-0.5f,0.5f,0),new Vector3(0.5f,0.5f,0),new Vector3(0.5f,-0.5f,0) };
            quad.triangles = new[] {0,1,2,0,2,3};
            quad.RecalculateNormals();
            preview.BeginStaticPreview(new Rect(0,0,detail ? 1200 : 1800,1200));
            preview.DrawMesh(quad, Matrix4x4.TRS(new Vector3(0,-1,0.5f),Quaternion.identity,new Vector3(4,3,1)),backdrop,0);
            for (int i=0; i<9; i++)
            {
                float x = detail ? -0.02f + i * 0.04f : -1.6f + i * 0.4f;
                preview.DrawMesh(quad, Matrix4x4.TRS(new Vector3(x,-1,0.4f),Quaternion.identity,new Vector3(detail ? 0.012f : 0.045f,3,1)),bar,0);
            }
            preview.DrawMesh(mesh,Matrix4x4.identity,material,0);
            preview.Render(true);
            var result = preview.EndStaticPreview();
            try { File.WriteAllBytes(path, result.EncodeToPNG()); }
            finally { Object.DestroyImmediate(result); }
        }
        finally
        {
            preview.Cleanup();
            Object.DestroyImmediate(cloth);
            Object.DestroyImmediate(backdrop);
            Object.DestroyImmediate(bar);
            Object.DestroyImmediate(quad);
        }
    }
}
