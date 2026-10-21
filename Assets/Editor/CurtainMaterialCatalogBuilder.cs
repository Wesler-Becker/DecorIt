using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class CurtainMaterialCatalogBuilder : IPreprocessBuildWithReport
{
    private const string Folder = "Assets/DecorItCurtains/Materials/Active";
    private const string AssetPath = "Assets/Resources/CurtainMaterials.asset";
    [Serializable] private sealed class SourceCatalog { public Product[] products; }
    [Serializable] private sealed class Product { public string id; public string name; public string code; }

    public int callbackOrder => 0;
    public void OnPreprocessBuild(BuildReport report) => Refresh();

    [MenuItem("DecorIt/Atualizar opções de tecidos AR")]
    public static void Refresh()
    {
        if (!AssetDatabase.IsValidFolder(Folder)) return;
        var products = File.Exists("Assets/DecorItCurtains/catalog.json")
            ? JsonUtility.FromJson<SourceCatalog>(File.ReadAllText("Assets/DecorItCurtains/catalog.json")).products
            : Array.Empty<Product>();
        var names = products.ToDictionary(p => p.id, p => string.IsNullOrWhiteSpace(p.code) ? p.name : p.name + " · " + p.code);
        var entries = AssetDatabase.FindAssets("t:Material", new[] {Folder})
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(path => new CurtainMaterialCatalog.Entry {
                assetPath = path,
                material = AssetDatabase.LoadAssetAtPath<Material>(path),
                displayName = names.TryGetValue(Path.GetFileNameWithoutExtension(path), out var name)
                    ? name : Path.GetFileNameWithoutExtension(path).Replace('_', ' '),
            })
            .Where(entry => entry.material != null)
            .OrderBy(entry => entry.displayName, StringComparer.OrdinalIgnoreCase).ToArray();
        var catalog = AssetDatabase.LoadAssetAtPath<CurtainMaterialCatalog>(AssetPath);
        if (catalog == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            catalog = ScriptableObject.CreateInstance<CurtainMaterialCatalog>();
            AssetDatabase.CreateAsset(catalog, AssetPath);
        }
        catalog.entries = entries;
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        Debug.Log("Tecidos AR: " + entries.Length + " materiais de Active incluídos no catálogo.");
    }
}
