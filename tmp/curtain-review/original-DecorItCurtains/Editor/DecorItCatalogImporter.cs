using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DecorIt.Curtains.Editor
{
    // JsonUtility deliberately avoids an optional package dependency.
    [Serializable] internal sealed class CurtainCatalog
    {
        public int schemaVersion;
        public string title;
        public string source;
        public string notes;
        public CurtainFamily[] families;
        public CurtainProduct[] products;
    }

    [Serializable] internal sealed class CurtainFamily
    {
        public string id;
        public string name;
        public string baseColor;
        public string opacity;
        public string normal;
        public string mask;
        public string mode;
        public float threadOpacity;
        public float cutoff;
        public float normalStrength;
        public float uvScale;
        public bool physicalScaleKnown;
        public int referencePage;
        public string notes;
    }

    [Serializable] internal sealed class CurtainProduct
    {
        public string id;
        public string familyId;
        public string name;
        public string code;
        public string colorHex;
        public string mode;
        public int referencePage;
        public string notes;
    }

    internal enum CurtainMode { Opaque = 0, AlphaClip = 1, Transparent = 2 }

    public static class DecorItCatalogImporter
    {
        private const string Root = "Assets/DecorItCurtains";
        private const string CatalogPath = Root + "/catalog.json";
        private const string MaterialFolder = Root + "/Materials/Active";
        private static readonly Regex ValidId = new Regex(@"^[a-z0-9_-]+$", RegexOptions.CultureInvariant);

        [MenuItem("DecorIt/Criar ou atualizar materiais do catalogo")]
        public static void CreateOrUpdateMaterials()
        {
            try
            {
                string shaderName = DetectShaderName();
                Shader shader = Shader.Find(shaderName);
                if (shader == null)
                    throw new InvalidOperationException("Shader nao encontrado: " + shaderName + ". Copie tambem a pasta Shaders.");
                if (!File.Exists(CatalogPath))
                    throw new FileNotFoundException("Catalogo ausente: " + CatalogPath);

                CurtainCatalog catalog = JsonUtility.FromJson<CurtainCatalog>(File.ReadAllText(CatalogPath));
                Dictionary<string, CurtainFamily> families = Validate(catalog);
                // Validation finishes before changing materials or import settings.
                ConfigureTextureImports(catalog.families);
                EnsureFolder(MaterialFolder);

                int created = 0;
                int updated = 0;
                foreach (CurtainProduct product in catalog.products)
                {
                    CurtainFamily family = families[product.familyId];
                    string path = MaterialFolder + "/" + product.id + ".mat";
                    Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    bool isNew = material == null;
                    if (isNew && File.Exists(path))
                        throw new InvalidOperationException("O arquivo existente nao e um Material: " + path);
                    if (isNew)
                        material = new Material(shader);
                    else
                        Undo.RecordObject(material, "Atualizar material DecorIt");

                    material.shader = shader;
                    material.name = product.id;
                    Color tint;
                    ColorUtility.TryParseHtmlString(product.colorHex, out tint);
                    material.SetColor("_BaseColor", tint);
                    material.SetTexture("_BaseMap", LoadTexture(family.baseColor, Texture2D.whiteTexture));
                    material.SetTexture("_OpacityMap", LoadTexture(family.opacity, Texture2D.whiteTexture));
                    // A null normal texture uses the shader's neutral "bump" default.
                    material.SetTexture("_NormalMap", LoadTexture(family.normal, null));
                    material.SetFloat("_NormalStrength", family.normalStrength);
                    material.SetFloat("_ThreadOpacity", family.threadOpacity);
                    material.SetFloat("_Cutoff", family.cutoff);
                    material.SetTextureScale("_BaseMap", Vector2.one * family.uvScale);
                    material.SetTextureOffset("_BaseMap", Vector2.zero);
                    material.SetFloat("_Cull", (float)CullMode.Off);
                    ConfigureMode(material, ParseMode(string.IsNullOrEmpty(product.mode) ? family.mode : product.mode));
                    material.enableInstancing = true;
                    material.doubleSidedGI = true;
                    EditorUtility.SetDirty(material);
                    if (isNew)
                    {
                        AssetDatabase.CreateAsset(material, path);
                        created++;
                    }
                    else updated++;
                }

                AssetDatabase.SaveAssets();
                Debug.Log("DecorIt: " + created + " materiais criados, " + updated + " atualizados, " +
                    families.Count + " tramas compartilhadas. Pipeline: " + shaderName +
                    ". Revise escala UV, cor e transparencia na iluminacao do projeto.");
                EditorUtility.DisplayDialog("DecorIt", created + " materiais criados e " + updated +
                    " atualizados.\nPasta: " + MaterialFolder +
                    "\nA escala fisica e as cores precisam de validacao com a amostra real.", "OK");
            }
            catch (Exception exception)
            {
                Debug.LogError("DecorIt: " + exception.Message);
                EditorUtility.DisplayDialog("DecorIt - importacao interrompida", exception.Message, "OK");
            }
        }

        private static string DetectShaderName()
        {
            RenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline;
            if (pipeline == null) return "DecorIt/Curtain BuiltIn";
            string name = pipeline.GetType().FullName ?? pipeline.GetType().Name;
            if (name.IndexOf("UniversalRenderPipelineAsset", StringComparison.Ordinal) >= 0)
                return "DecorIt/Curtain URP";
            throw new NotSupportedException("Pipeline nao suportado: " + name +
                ". Este pacote inclui Built-in e URP. HDRP exige adaptacao do shader.");
        }

        private static Dictionary<string, CurtainFamily> Validate(CurtainCatalog catalog)
        {
            if (catalog == null || catalog.schemaVersion != 1 || catalog.families == null || catalog.products == null)
                throw new InvalidDataException("catalog.json invalido. Esperado schemaVersion 1, families e products.");
            var families = new Dictionary<string, CurtainFamily>(StringComparer.Ordinal);
            var usages = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (CurtainFamily family in catalog.families)
            {
                CheckId(family == null ? null : family.id);
                if (families.ContainsKey(family.id)) throw new InvalidDataException("Familia duplicada: " + family.id);
                families.Add(family.id, family);
                ParseMode(family.mode);
                CheckRange(family.threadOpacity, 0, 1, family.id + ".threadOpacity");
                CheckRange(family.cutoff, 0.001f, 1, family.id + ".cutoff");
                CheckRange(family.normalStrength, 0, 2, family.id + ".normalStrength");
                CheckRange(family.uvScale, 0.001f, 10000, family.id + ".uvScale");
                ValidateTexture(family.baseColor, "color", usages, true);
                ValidateTexture(family.opacity, "opacity", usages, false);
                ValidateTexture(family.normal, "normal", usages, false);
                if (family.mode != "Opaque" && string.IsNullOrEmpty(family.opacity))
                    throw new InvalidDataException("Familia com transparencia precisa de opacity: " + family.id);
            }
            var productIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (CurtainProduct product in catalog.products)
            {
                CheckId(product == null ? null : product.id);
                if (!productIds.Add(product.id)) throw new InvalidDataException("Produto duplicado: " + product.id);
                CurtainFamily family;
                if (string.IsNullOrEmpty(product.familyId) || !families.TryGetValue(product.familyId, out family))
                    throw new InvalidDataException("Familia inexistente para produto: " + product.id);
                Color tint;
                if (string.IsNullOrEmpty(product.colorHex) || !Regex.IsMatch(product.colorHex, @"^#[0-9a-fA-F]{6}$") ||
                    !ColorUtility.TryParseHtmlString(product.colorHex, out tint))
                    throw new InvalidDataException("colorHex deve ser #RRGGBB: " + product.id);
                CurtainMode mode = ParseMode(string.IsNullOrEmpty(product.mode) ? family.mode : product.mode);
                if (mode != CurtainMode.Opaque && string.IsNullOrEmpty(family.opacity))
                    throw new InvalidDataException("Produto com transparencia precisa de opacity: " + product.id);
            }
            return families;
        }

        private static void CheckId(string id)
        {
            if (string.IsNullOrEmpty(id) || !ValidId.IsMatch(id))
                throw new InvalidDataException("ID invalido: " + id + ". Use letras minusculas ASCII, numeros, _ e -.");
        }

        private static void CheckRange(float value, float min, float max, string field)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < min || value > max)
                throw new InvalidDataException("Valor fora do intervalo [" + min + ", " + max + "]: " + field);
        }

        private static string TexturePath(string relative)
        {
            if (string.IsNullOrEmpty(relative)) return null;
            string normalized = relative.Replace('\\', '/');
            if (normalized.StartsWith("/", StringComparison.Ordinal) || normalized.Contains(":") ||
                Array.Exists(normalized.Split('/'), segment => segment == ".."))
                throw new InvalidDataException("Caminho de textura fora do pacote: " + relative);
            return Root + "/" + normalized;
        }

        private static void ValidateTexture(string relative, string role, Dictionary<string, string> usages, bool required)
        {
            if (string.IsNullOrEmpty(relative))
            {
                if (required) throw new InvalidDataException("Textura obrigatoria ausente: " + role);
                return;
            }
            string path = TexturePath(relative);
            if (!File.Exists(path)) throw new FileNotFoundException("Textura nao encontrada: " + path);
            string previous;
            if (usages.TryGetValue(path, out previous) && previous != role)
                throw new InvalidDataException("Uma textura nao pode ser " + previous + " e " + role + ": " + path);
            usages[path] = role;
        }

        private static CurtainMode ParseMode(string value)
        {
            switch (value)
            {
                case "Opaque": return CurtainMode.Opaque;
                case "AlphaClip": return CurtainMode.AlphaClip;
                case "Transparent": return CurtainMode.Transparent;
                default: throw new InvalidDataException("Modo invalido: " + value);
            }
        }

        private static void ConfigureTextureImports(CurtainFamily[] families)
        {
            var done = new HashSet<string>(StringComparer.Ordinal);
            foreach (CurtainFamily family in families)
            {
                ConfigureTexture(family.baseColor, "color", done);
                ConfigureTexture(family.opacity, "opacity", done);
                ConfigureTexture(family.normal, "normal", done);
            }
        }

        private static void ConfigureTexture(string relative, string role, HashSet<string> done)
        {
            string path = TexturePath(relative);
            if (path == null || !done.Add(path)) return;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidDataException("Arquivo nao e uma textura: " + path);
            importer.textureType = role == "normal" ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = role == "color";
            importer.convertToNormalmap = false;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 4;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.isReadable = false;
            importer.maxTextureSize = 2048;
            importer.textureCompression = role == "opacity" ? TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static Texture2D LoadTexture(string relative, Texture2D fallback)
        {
            if (string.IsNullOrEmpty(relative)) return fallback;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath(relative));
            if (texture == null) throw new InvalidDataException("Falha ao carregar textura: " + relative);
            return texture;
        }

        private static void ConfigureMode(Material material, CurtainMode mode)
        {
            bool transparent = mode == CurtainMode.Transparent;
            material.SetFloat("_Surface", (float)mode);
            material.SetFloat("_SrcBlend", (float)(transparent ? BlendMode.SrcAlpha : BlendMode.One));
            material.SetFloat("_DstBlend", (float)(transparent ? BlendMode.OneMinusSrcAlpha : BlendMode.Zero));
            material.SetFloat("_ZWrite", transparent ? 0 : 1);
            material.renderQueue = transparent ? (int)RenderQueue.Transparent :
                mode == CurtainMode.AlphaClip ? (int)RenderQueue.AlphaTest : (int)RenderQueue.Geometry;
            material.SetOverrideTag("RenderType", transparent ? "Transparent" :
                mode == CurtainMode.AlphaClip ? "TransparentCutout" : "Opaque");
            // Blended curtains do not cast a hard cutout shadow. Physical transmission is not modeled.
            material.SetShaderPassEnabled("ShadowCaster", !transparent);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
