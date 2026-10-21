using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DecorIt.Curtains.Editor
{
    /// <summary>Derives technical maps from the authored textile sources, preserving existing asset GUIDs.</summary>
    public static class CurtainTextureMaps
    {
        [Serializable] private sealed class Manifest { public Entry[] entries; }
        [Serializable] private sealed class Entry
        {
            public string id;
            public string source;
            public bool coverageFromAlpha;
            public bool sourceIsCoverage;
            public float tileSizeMeters;
            public float threadOpacity;
            public float normalStrength;
        }

        [MenuItem("DecorIt/Reconstruir mapas das fontes de tecido")]
        public static void RebuildSources()
        {
            Build("ArtSource/Curtains/rebuild-manifest.json");
            DecorItCatalogImporter.Rebuild();
        }

        public static void Build(string manifestPath)
        {
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
            const string root = "Assets/DecorItCurtains/";
            var catalog = JsonUtility.FromJson<CurtainCatalog>(File.ReadAllText(root + "catalog.json"));
            foreach (var entry in manifest.entries)
            {
                var family = Array.Find(catalog.families, f => f.id == entry.id);
                if (family == null) throw new InvalidDataException("Familia desconhecida: " + entry.id);
                var source = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
                try
                {
                    if (!source.LoadImage(File.ReadAllBytes(entry.source), false))
                        throw new InvalidDataException("Fonte invalida: " + entry.source);
                    int w = source.width, h = source.height;
                    var pixels = source.GetPixels32();
                    var height = new float[pixels.Length];
                    var opacity = new Color32[pixels.Length];
                    var albedo = entry.sourceIsCoverage ? new Color32[pixels.Length] : null;
                    float sum = 0;
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        Color32 p = pixels[i];
                        float luminance = (0.2126f * p.r + 0.7152f * p.g + 0.0722f * p.b) / 255f;
                        // Authored alpha encodes holes. Never classify holes from the brightness of a photo.
                        float coverage = entry.sourceIsCoverage
                            ? Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.06f, 0.94f, luminance))
                            : entry.coverageFromAlpha
                            ? Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.035f, 0.95f, p.a / 255f))
                            : family.mode == "Opaque" ? 1 : Mathf.Lerp(0.86f, 1, luminance);
                        byte a = (byte)Mathf.RoundToInt(coverage * 255);
                        opacity[i] = new Color32(a, a, a, 255);
                        height[i] = entry.coverageFromAlpha || entry.sourceIsCoverage ? coverage * (0.7f + 0.3f * luminance) : luminance;
                        if (albedo != null)
                        {
                            // Neutral yarn colour extends into holes to avoid dark mip fringes.
                            byte shade = (byte)Mathf.RoundToInt(243 + 12 * coverage);
                            albedo[i] = new Color32(shade, shade, shade, 255);
                        }
                        sum += coverage;
                    }
                    var normals = new Color32[pixels.Length];
                    for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float dx = height[y * w + (x + 1) % w] - height[y * w + (x + w - 1) % w];
                        float dy = height[((y + 1) % h) * w + x] - height[((y + h - 1) % h) * w + x];
                        Vector3 n = new Vector3(-dx * 2.5f, -dy * 2.5f, 1).normalized;
                        normals[y * w + x] = (Color32)new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1);
                    }
                    // Keep the authored RGB image at native resolution; upscaling would add no detail.
                    if (albedo != null) SaveMap(root + family.baseColor, w, h, albedo);
                    else File.Copy(entry.source, root + family.baseColor, true);
                    if (family.mode == "Opaque") SaveMap(root + family.opacity, 1, 1, new[] {new Color32(255,255,255,255)});
                    else SaveMap(root + family.opacity, w, h, opacity);
                    SaveMap(root + family.normal, w, h, normals);
                    family.tileSizeMeters = entry.tileSizeMeters;
                    family.uvScale = 1f / entry.tileSizeMeters;
                    family.threadOpacity = entry.threadOpacity;
                    family.normalStrength = entry.normalStrength;
                    family.physicalScaleKnown = false;
                    if (family.mode != "Opaque") family.mode = "Transparent";
                    family.transmission = family.mode == "Opaque" ? 0 : 0.3f;
                    family.notes = "Reconstrucao visual da fotografia do catalogo; fonte: " + entry.source +
                        ". Escala artistica estimada, nao medida. Mapas de relevo e transmissao aproximados. " +
                        (entry.sourceIsCoverage ? "Fonte autoral e uma mascara de filamentos, nao uma fotografia." :
                        entry.coverageFromAlpha ? "Vazados derivados do alpha da trama reconstruida." : "Sem segmentacao de furos por luminosidade.");
                    string provenance = JsonUtility.ToJson(new Provenance {
                        familyId = entry.id, referencePage = family.referencePage, source = entry.source,
                        width = w, height = h, tileSizeMeters = entry.tileSizeMeters,
                        coverageFromAuthoredAlpha = entry.coverageFromAlpha, sourceIsCoverage = entry.sourceIsCoverage, meanCoverage = sum / pixels.Length,
                        method = entry.sourceIsCoverage ? "Mascara de fios reconstruida por image_gen; albedo neutro; normal aproximado por derivada periodica" :
                            "Albedo reconstruido por image_gen; alpha autoral onde disponivel; normal aproximado"
                    }, true);
                    File.WriteAllText(root + "Textures/" + entry.id + "/provenance.json", provenance);
                }
                finally { UnityEngine.Object.DestroyImmediate(source); }
            }
            catalog.notes = "Biblioteca reconstruida a partir do catalogo. UVs metricas: tileSizeMeters indica uma escala visual estimada, nao medida. " +
                "Cores por variante conservadas das amostras menores. Alpha de vazados e relevo sao aproximacoes da trama reconstruida. " +
                "Validar cor e escala com amostra real; a largura comercial do tecido nao mede o tamanho dos fios.";
            File.WriteAllText(root + "catalog.json", JsonUtility.ToJson(catalog, true));
            AssetDatabase.Refresh();
        }

        [Serializable] private sealed class Provenance
        {
            public string familyId;
            public int referencePage;
            public string source;
            public int width, height;
            public float tileSizeMeters, meanCoverage;
            public bool coverageFromAuthoredAlpha;
            public bool sourceIsCoverage;
            public bool physicalScaleKnown = false;
            public bool heightIsMeasured = false;
            public string normalConvention = "Unity tangent +Y";
            public string method;
        }

        private static void SaveMap(string path, int w, int h, Color32[] pixels)
        {
            var map = new Texture2D(w, h, TextureFormat.RGB24, false, true);
            try { map.SetPixels32(pixels); map.Apply(false); File.WriteAllBytes(path, map.EncodeToPNG()); }
            finally { UnityEngine.Object.DestroyImmediate(map); }
        }
    }
}
