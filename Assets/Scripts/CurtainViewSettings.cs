using System;
using UnityEngine;

[Serializable]
public sealed class CurtainViewSettings
{
    public string modelName = "CurtainPrefab";
    public int seed = 17;
    public float widthCentimeters = 240f;
    public float heightCentimeters = 220f;
    public string panelType = "DoublePanel";
    public string fabricStyle = "Natural";
    public string fabricMaterialPath = "Assets/Materials/CurtainFabric.mat";
    public string fabricMaterialName = "CurtainFabric";
    public string rodMaterialPath = "Assets/Materials/RodMaterial.mat";
    public string rodMaterialName = "RodMaterial";
    public float foldsPerMeter = 5f;
    public float foldDepthCentimeters = 6f;
    public float foldProfile = 1.15f;
    public float naturalness = 0.8f;
    public float opening = 0f;
}

public static class CurtainViewSettingsStore
{
    private const string PreferenceKey = "DecorIt.LastCurtainSettings.v1";
    private static CurtainViewSettings last;

    public static CurtainViewSettings LoadLast()
    {
        if (last == null)
        {
            string json = PlayerPrefs.GetString(PreferenceKey, string.Empty);
            last = string.IsNullOrEmpty(json)
                ? new CurtainViewSettings()
                : JsonUtility.FromJson<CurtainViewSettings>(json) ?? new CurtainViewSettings();
        }
        return Clone(last);
    }

    public static void SaveLast(CurtainViewSettings settings)
    {
        if (settings == null) return;
        last = Clone(settings);
        PlayerPrefs.SetString(PreferenceKey, JsonUtility.ToJson(last));
        PlayerPrefs.Save();
    }

    public static CurtainViewSettings Clone(CurtainViewSettings source)
    {
        return source == null ? new CurtainViewSettings() :
            JsonUtility.FromJson<CurtainViewSettings>(JsonUtility.ToJson(source));
    }

    public static CurtainViewSettings FromDraft(VisitDraft draft)
    {
        CurtainViewSettings settings = new CurtainViewSettings();
        if (draft == null) return settings;
        if (!string.IsNullOrWhiteSpace(draft.modelName) && draft.modelName != "Cortina sob medida")
            settings.modelName = draft.modelName;
        settings.widthCentimeters = draft.widthCentimeters > 0f ? draft.widthCentimeters : settings.widthCentimeters;
        settings.heightCentimeters = draft.heightCentimeters > 0f ? draft.heightCentimeters : settings.heightCentimeters;
        settings.panelType = draft.panelType;
        settings.fabricStyle = draft.fabricStyle;
        settings.fabricMaterialName = draft.fabricMaterialName;
        settings.fabricMaterialPath = draft.fabricMaterialPath;
        settings.rodMaterialName = draft.rodMaterialName;
        settings.rodMaterialPath = draft.rodMaterialPath;
        settings.foldsPerMeter = draft.foldsPerMeter;
        settings.foldDepthCentimeters = draft.foldDepthCentimeters;
        settings.foldProfile = draft.foldProfile;
        settings.naturalness = draft.fabricNaturalness;
        settings.seed = draft.fabricSeed;
        settings.opening = draft.opening;
        return settings;
    }

    public static void ApplyToDraft(CurtainViewSettings settings, VisitDraft draft)
    {
        if (settings == null || draft == null) return;
        draft.modelName = settings.modelName;
        draft.widthCentimeters = settings.widthCentimeters;
        draft.heightCentimeters = settings.heightCentimeters;
        draft.panelType = settings.panelType;
        draft.fabricStyle = settings.fabricStyle;
        draft.fabricMaterialName = settings.fabricMaterialName;
        draft.fabricMaterialPath = settings.fabricMaterialPath;
        draft.rodMaterialName = settings.rodMaterialName;
        draft.rodMaterialPath = settings.rodMaterialPath;
        draft.foldsPerMeter = settings.foldsPerMeter;
        draft.foldDepthCentimeters = settings.foldDepthCentimeters;
        draft.foldProfile = settings.foldProfile;
        draft.fabricNaturalness = settings.naturalness;
        draft.fabricSeed = settings.seed;
        draft.opening = settings.opening;
    }

    public static Material ResolveFabric(string assetPath, string materialName)
    {
        CurtainMaterialCatalog catalog = Resources.Load<CurtainMaterialCatalog>("CurtainMaterials");
        if (catalog == null || catalog.entries == null) return null;
        foreach (CurtainMaterialCatalog.Entry entry in catalog.entries)
        {
            if (entry.material == null) continue;
            if ((!string.IsNullOrEmpty(assetPath) && entry.assetPath == assetPath) ||
                (!string.IsNullOrEmpty(materialName) &&
                 (entry.material.name == materialName || entry.displayName == materialName)))
                return entry.material;
        }
        return null;
    }
}
