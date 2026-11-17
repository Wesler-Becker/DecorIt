using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEngine;
using Newtonsoft.Json;
using Object=UnityEngine.Object;
public static class VerifyCurtainRebuildSources
{
    static string Hash(string p){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(p)));}
    static void Image(string path,Color a,Color b){var tex=new Texture2D(4,4,TextureFormat.RGBA32,false);for(int y=0;y<4;y++)for(int x=0;x<4;x++)tex.SetPixel(x,y,(x+y)%2==0?a:b);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());Object.DestroyImmediate(tex);}
    public static string Run(){
        string root="Temp/CurtainRebuildTest/"+Guid.NewGuid().ToString("N")+"/", dir=root+"Textures/";Directory.CreateDirectory(dir+"same");Directory.CreateDirectory(dir+"coverage");
        string same=dir+"same/BaseColor.png", mask=root+"coverage-source.png", coverage=dir+"coverage/BaseColor.png";
        Image(same,Color.white,Color.red);Image(mask,Color.white,Color.black);Image(coverage,Color.green,Color.blue);string before=Hash(same),beforeMask=Hash(mask);
        var catalog=new {families=new[]{new {id="same",baseColor="Textures/same/BaseColor.png",normal="Textures/same/Normal.png",opacity="Textures/same/Opacity.png",mode="Opaque"},new {id="coverage",baseColor="Textures/coverage/BaseColor.png",normal="Textures/coverage/Normal.png",opacity="Textures/coverage/Opacity.png",mode="Transparent"}}};
        File.WriteAllText(root+"catalog.json",JsonConvert.SerializeObject(catalog));
        var entries=new[]{new{id="same",source=same,sourceIsCoverage=false,coverageFromAlpha=false,tileSizeMeters=.04f,threadOpacity=1f,normalStrength=.3f},new{id="coverage",source=mask,sourceIsCoverage=true,coverageFromAlpha=false,tileSizeMeters=.08f,threadOpacity=.9f,normalStrength=.4f}};
        string manifest=root+"manifest.json";File.WriteAllText(manifest,JsonConvert.SerializeObject(new{entries}));
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("DecorIt.Curtains.Editor.CurtainTextureMaps")).First(t=>t!=null);var build=type.GetMethod("Build",BindingFlags.Public|BindingFlags.Static);
        build.Invoke(null,new object[]{manifest,root});
        if(Hash(same)!=before||Hash(mask)!=beforeMask)throw new Exception("Source content changed");
        foreach(var path in new[]{dir+"same/Normal.png",dir+"same/Opacity.png",dir+"coverage/Normal.png",dir+"coverage/Opacity.png",dir+"coverage/BaseColor.png"})if(!File.Exists(path))throw new Exception("Missing output: "+path);
        if(Hash(coverage)==beforeMask)throw new Exception("Coverage mask was copied instead of converted");
        File.WriteAllText(manifest,JsonConvert.SerializeObject(new{entries=new[]{new{id="same",source=same,sourceIsCoverage=true}}}));
        bool rejected=false;try{build.Invoke(null,new object[]{manifest,root});}catch(TargetInvocationException e){rejected=e.InnerException is InvalidDataException;}
        if(!rejected||Hash(same)!=before)throw new Exception("Aliased coverage source was not protected");
        return "PASS: BaseColor as its own source remains unchanged; separate Voil-style mask generates maps; unsafe mask alias is rejected. Fixture: "+root;
    }
}
