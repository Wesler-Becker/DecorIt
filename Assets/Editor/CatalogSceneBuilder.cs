using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[InitializeOnLoad]
public static class CatalogSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Catalog.unity";
    private const string UxmlPath = "Assets/UI/Catalog/Catalog.uxml";
    private const string PanelSettingsPath = "Assets/UI/Catalog/CatalogPanelSettings.asset";

    static CatalogSceneBuilder() => EditorApplication.delayCall += BuildIfMissing;
    [MenuItem("DecorIt/Build Catalog Screen")] public static void BuildFromMenu() => Build(true);
    private static void BuildIfMissing()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            Build(false);
    }

    private static void Build(bool replaceExisting)
    {
        if (!replaceExisting && AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) return;
        GameObject curtain = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CurtainPrefab.prefab");
        Material fabric = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/CurtainFabric.mat");
        Material rod = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/RodMaterial.mat");
        Scene previous = SceneManager.GetActiveScene();
        bool previousWasCatalog = previous.IsValid() && previous.isLoaded && previous.path == ScenePath;
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            BuildHierarchy(curtain, fabric, rod);
            Scene existingCatalog = SceneManager.GetSceneByPath(ScenePath);
            if (existingCatalog.IsValid() && existingCatalog.isLoaded)
            {
                if (existingCatalog.isDirty)
                    throw new System.InvalidOperationException(
                        "Save or discard the open Catalog scene changes before rebuilding it.");
                EditorSceneManager.CloseScene(existingCatalog, true);
            }
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddBuildScenes();
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
            Scene restore = previous.IsValid() && previous.isLoaded
                ? previous
                : previousWasCatalog
                    ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive)
                    : default;
            if (restore.IsValid() && restore.isLoaded) SceneManager.SetActiveScene(restore);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void BuildHierarchy(GameObject curtain, Material fabric, Material rod)
    {
        VisualTreeAsset uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
        if (uxml == null) throw new System.InvalidOperationException("Catalog.uxml não foi importado.");
        PanelSettings panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
        if (panelSettings == null)
        {
            panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1080, 1920);
            panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panelSettings.match = 0.5f;
            AssetDatabase.CreateAsset(panelSettings, PanelSettingsPath);
        }

        GameObject uiObject = new GameObject("CatalogUI", typeof(UIDocument));
        UIDocument document = uiObject.GetComponent<UIDocument>();
        document.panelSettings = panelSettings;
        document.visualTreeAsset = uxml;
        CatalogScreenController controller = uiObject.AddComponent<CatalogScreenController>();
        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("document").objectReferenceValue = document;
        serialized.FindProperty("curtainPrefab").objectReferenceValue = curtain;
        serialized.FindProperty("defaultFabric").objectReferenceValue = fabric;
        serialized.FindProperty("defaultRod").objectReferenceValue = rod;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddBuildScenes()
    {
        const string bootstrapPath = "Assets/Scenes/UnityBootstrap.unity";
        const string arPath = "Assets/Scenes/AR_Test.unity";
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(bootstrapPath, true),
            new EditorBuildSettingsScene(ScenePath, true),
            new EditorBuildSettingsScene(arPath, true),
        };
    }
}
