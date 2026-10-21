using System;
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
}
