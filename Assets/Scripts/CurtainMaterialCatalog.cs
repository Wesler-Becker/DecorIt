using System;
using System.Globalization;
using UnityEngine;

/// <summary>References the imported fabrics so their materials and textures are included in mobile builds.</summary>
public sealed class CurtainMaterialCatalog : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        public string displayName;
        public string assetPath;
        public Material material;
    }

    public Entry[] entries = Array.Empty<Entry>();

    public static bool MatchesSearch(string displayName, string assetPath, string query)
    {
        query = (query ?? string.Empty).Trim();
        if (query.Length == 0) return true;
        const CompareOptions options = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;
        CompareInfo compare = CultureInfo.InvariantCulture.CompareInfo;
        return compare.IndexOf(displayName ?? string.Empty, query, options) >= 0 ||
            compare.IndexOf(System.IO.Path.GetFileNameWithoutExtension(assetPath ?? string.Empty).Replace('_', ' '), query, options) >= 0;
    }
}
