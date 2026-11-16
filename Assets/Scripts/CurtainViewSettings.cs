using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class CurtainLayerViewSettings
{
    public float widthCentimeters;
    public float heightCentimeters;
    public string panelType;
    public int supportIndex;
    public float spacingMillimeters = -1f;
    public string fabricStyle = "Natural";
    public string fabricMaterialPath = "Assets/Materials/CurtainFabric.mat";
    public string fabricMaterialName = "CurtainFabric";
    public float foldsPerMeter = 5f;
    public float foldDepthCentimeters = 6f;
    public float foldProfile = 1.15f;
    public float naturalness = 0.8f;
    public int seed = 17;

    public CurtainLayerViewSettings Clone()
    {
        return JsonUtility.FromJson<CurtainLayerViewSettings>(JsonUtility.ToJson(this));
    }
}

[Serializable]
public sealed class CurtainViewSettings
{
    public const int MaximumLayerCount = 8;

    public string modelName = "CurtainPrefab";
    public int seed = 17;
    public float widthCentimeters = 240f;
    public float heightCentimeters = 220f;
    public string panelType = "DoublePanel";

    // These fields keep old saved configurations and visit drafts compatible.
    // They always mirror the first/front layer after EnsureLayers is called.
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

    // New fields are appended after the original serialized layout.
    public float layerSpacingCentimeters = 6f;
    public List<CurtainLayerViewSettings> layers = new List<CurtainLayerViewSettings>();
    public string supportType = "Rod";
    public bool doubleSupport;
    public float secondaryOpening;

    public int GetSupportIndex(int layerIndex) => doubleSupport ? GetLayer(layerIndex).supportIndex : 0;

    public float GetOpening(int layerIndex) => GetSupportIndex(layerIndex) == 0 ? opening : secondaryOpening;

    public void SetOpening(int layerIndex, float value)
    {
        if (GetSupportIndex(layerIndex) == 0) opening = Mathf.Clamp01(value);
        else secondaryOpening = Mathf.Clamp01(value);
    }

    public void EnsureLayers()
    {
        if (layers == null) layers = new List<CurtainLayerViewSettings>();

        if (layers.Count == 0)
        {
            if (layerSpacingCentimeters <= 0f) layerSpacingCentimeters = 6f;
            layers.Add(new CurtainLayerViewSettings
            {
                fabricStyle = string.IsNullOrEmpty(fabricStyle) ? "Natural" : fabricStyle,
                fabricMaterialPath = fabricMaterialPath,
                fabricMaterialName = fabricMaterialName,
                foldsPerMeter = foldsPerMeter > 0f ? foldsPerMeter : 5f,
                foldDepthCentimeters = Mathf.Clamp(foldDepthCentimeters, 0f, 100f),
                foldProfile = foldProfile > 0f ? foldProfile : 1.15f,
                naturalness = Mathf.Clamp01(naturalness),
                seed = seed
            });
        }

        while (layers.Count > MaximumLayerCount)
            layers.RemoveAt(layers.Count - 1);

        for (int i = 0; i < layers.Count; i++)
        {
            if (layers[i] == null) layers[i] = new CurtainLayerViewSettings();
            CurtainLayerViewSettings layer = layers[i];
            // Older configurations stored one set of dimensions for all layers.
            layer.widthCentimeters = Mathf.Clamp(layer.widthCentimeters > 0f ? layer.widthCentimeters : widthCentimeters, 10f, 10000f);
            layer.heightCentimeters = Mathf.Clamp(layer.heightCentimeters > 0f ? layer.heightCentimeters : heightCentimeters, 10f, 10000f);
            if (string.IsNullOrEmpty(layer.panelType)) layer.panelType = panelType;
            if (layer.panelType != "SinglePanel") layer.panelType = "DoublePanel";
            layer.supportIndex = Mathf.Clamp(layer.supportIndex, 0, 1);
            layer.spacingMillimeters = Mathf.Clamp(layer.spacingMillimeters < 0f ? layerSpacingCentimeters * 10f : layer.spacingMillimeters, 0f, 1000f);
            if (string.IsNullOrEmpty(layer.fabricStyle)) layer.fabricStyle = "Natural";
            layer.foldsPerMeter = Mathf.Clamp(layer.foldsPerMeter, 1f, 32f);
            layer.foldDepthCentimeters = Mathf.Clamp(layer.foldDepthCentimeters, 0f, 100f);
            layer.foldProfile = Mathf.Clamp(layer.foldProfile <= 0f ? 1f : layer.foldProfile, 0.5f, 2f);
            layer.naturalness = Mathf.Clamp01(layer.naturalness);
        }

        layerSpacingCentimeters = Mathf.Clamp(layerSpacingCentimeters, 0f, 100f);
        if (supportType != "Rail") supportType = "Rod";
        opening = Mathf.Clamp01(opening);
        secondaryOpening = Mathf.Clamp01(secondaryOpening);
        SyncLegacyFromFrontLayer();
    }

    public CurtainLayerViewSettings GetLayer(int index)
    {
        EnsureLayers();
        return layers[Mathf.Clamp(index, 0, layers.Count - 1)];
    }

    public bool AddLayerFrom(int sourceIndex)
    {
        EnsureLayers();
        if (layers.Count >= MaximumLayerCount) return false;
        CurtainLayerViewSettings source = GetLayer(sourceIndex);
        CurtainLayerViewSettings added = source.Clone();
        if (added == null) added = new CurtainLayerViewSettings();
        added.seed = unchecked(source.seed + layers.Count * 37);
        layers.Add(added);
        return true;
    }

    public bool RemoveLayer(int index)
    {
        EnsureLayers();
        if (layers.Count <= 1 || index < 0 || index >= layers.Count) return false;
        layers.RemoveAt(index);
        SyncLegacyFromFrontLayer();
        return true;
    }

    public void SyncLegacyFromFrontLayer()
    {
        if (layers == null || layers.Count == 0 || layers[0] == null) return;
        CurtainLayerViewSettings front = layers[0];
        widthCentimeters = front.widthCentimeters;
        heightCentimeters = front.heightCentimeters;
        panelType = front.panelType;
        fabricStyle = front.fabricStyle;
        fabricMaterialPath = front.fabricMaterialPath;
        fabricMaterialName = front.fabricMaterialName;
        foldsPerMeter = front.foldsPerMeter;
        foldDepthCentimeters = front.foldDepthCentimeters;
        foldProfile = front.foldProfile;
        naturalness = front.naturalness;
        seed = front.seed;
    }
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
            last.EnsureLayers();
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
        CurtainViewSettings clone = source == null ? new CurtainViewSettings() :
            JsonUtility.FromJson<CurtainViewSettings>(JsonUtility.ToJson(source));
        if (clone == null) clone = new CurtainViewSettings();
        clone.EnsureLayers();
        return clone;
    }

    public static CurtainViewSettings FromDraft(VisitDraft draft)
    {
        if (draft != null && draft.curtainSettings != null &&
            draft.curtainSettings.layers != null && draft.curtainSettings.layers.Count > 0)
            return Clone(draft.curtainSettings);
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
        settings.EnsureLayers();
        return settings;
    }

    public static void ApplyToDraft(CurtainViewSettings settings, VisitDraft draft)
    {
        if (settings == null || draft == null) return;
        settings.EnsureLayers();
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
        draft.curtainSettings = Clone(settings);
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
