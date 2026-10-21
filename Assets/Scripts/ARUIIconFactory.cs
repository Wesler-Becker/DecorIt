using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class ARUIIconFactory
{
    private static readonly Color BrandBlue = new Color(0.157f, 0.408f, 0.573f, 0.94f);
    private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
    private static Sprite roundedSprite;
    private static Sprite circleSprite;

    public static Sprite RoundedSprite => roundedSprite != null ? roundedSprite : roundedSprite = CreateShapeSprite(false);
    public static Sprite CircleSprite => circleSprite != null ? circleSprite : circleSprite = CreateShapeSprite(true);

    public static RectTransform GetCameraControlsRoot(Canvas canvas)
    {
        Transform existing = canvas.transform.Find("CameraControlsSafeArea");
        if (existing != null) return (RectTransform)existing;
        GameObject root = new GameObject("CameraControlsSafeArea", typeof(RectTransform), typeof(SafeAreaFitter));
        root.transform.SetParent(canvas.transform, false);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.localScale = Vector3.one;
        return rect;
    }

    public static void ConfigureButton(Button button, string iconName, string caption, float iconSize = 56f, float fontSize = 22f, float buttonWidth = 136f)
    {
        if (button == null) return;

        RectTransform buttonRect = button.transform as RectTransform;
        if (buttonRect != null)
        {
            buttonRect.sizeDelta = new Vector2(buttonWidth, 132f);
            button.image.color = BrandBlue;
        }

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = caption;
            label.fontSize = fontSize;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = false;
            label.raycastTarget = false;
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(3f, 3f);
            labelRect.offsetMax = new Vector2(-3f, -92f);
        }

        Transform iconTransform = button.transform.Find("ARIcon");
        if (iconTransform == null)
        {
            GameObject iconObject = new GameObject("ARIcon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(button.transform, false);
            iconTransform = iconObject.transform;
        }

        RectTransform iconRect = (RectTransform)iconTransform;
        iconRect.anchorMin = new Vector2(0.5f, 1f);
        iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.anchoredPosition = new Vector2(0f, -10f);
        iconRect.sizeDelta = new Vector2(iconSize, iconSize);
        Image icon = iconTransform.GetComponent<Image>();
        icon.sprite = LoadSprite(iconName);
        icon.preserveAspect = true;
        icon.color = Color.white;
        icon.raycastTarget = false;
    }

    public static void SetSelected(Button button, bool selected)
    {
        if (button == null) return;
        button.transition = Selectable.Transition.None;
        button.image.color = selected ? BrandBlue : new Color(0.12f, 0.17f, 0.21f, 0.96f);
        Color foreground = Color.white;
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.color = foreground;
        Transform icon = button.transform.Find("ARIcon");
        if (icon != null) icon.GetComponent<Image>().color = foreground;
    }

    public static void SetIcon(Image image, string iconName)
    {
        if (image == null) return;
        image.sprite = LoadSprite(iconName);
        image.preserveAspect = true;
        image.color = Color.white;
        image.raycastTarget = false;
    }

    public static void ConfigureCameraButton(Button button, string iconName, float size, Color background, Color iconColor)
    {
        if (button == null) return;
        RectTransform rect = button.transform as RectTransform;
        rect.sizeDelta = new Vector2(size, size);
        Image backgroundImage = button.GetComponent<Image>();
        backgroundImage.sprite = CircleSprite;
        backgroundImage.type = Image.Type.Simple;
        backgroundImage.color = background;
        button.targetGraphic = backgroundImage;

        TMP_Text oldLabel = button.GetComponentInChildren<TMP_Text>(true);
        if (oldLabel != null) oldLabel.gameObject.SetActive(false);

        Transform iconTransform = button.transform.Find("ARIcon");
        if (iconTransform == null)
        {
            GameObject iconObject = new GameObject("ARIcon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(button.transform, false);
            iconTransform = iconObject.transform;
        }

        RectTransform iconRect = (RectTransform)iconTransform;
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = Vector2.one * size * (iconName == "Shutter" ? 1f : 0.58f);
        Image icon = iconTransform.GetComponent<Image>();
        icon.sprite = LoadSprite(iconName);
        icon.preserveAspect = true;
        icon.color = iconColor;
        icon.raycastTarget = false;
    }

    private static Sprite CreateShapeSprite(bool circle)
    {
        const int resolution = 128;
        const float radius = 25f;
        Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
        {
            name = circle ? "ARCameraCircle" : "ARCameraRoundedRect",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };
        Color32[] pixels = new Color32[resolution * resolution];
        for (int y = 0; y < resolution; y++)
        for (int x = 0; x < resolution; x++)
        {
            float px = x + 0.5f - resolution * 0.5f;
            float py = y + 0.5f - resolution * 0.5f;
            float distance;
            if (circle)
                distance = Mathf.Sqrt(px * px + py * py) - (resolution * 0.5f - 2f);
            else
            {
                float qx = Mathf.Abs(px) - (resolution * 0.5f - radius);
                float qy = Mathf.Abs(py) - (resolution * 0.5f - radius);
                distance = Mathf.Min(Mathf.Max(qx, qy), 0f) +
                    Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f)) - radius;
            }
            byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(0.5f - distance) * 255f);
            pixels[y * resolution + x] = new Color32(255, 255, 255, alpha);
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        Vector4 border = circle ? Vector4.zero : new Vector4(34f, 34f, 34f, 34f);
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, resolution, resolution), new Vector2(0.5f, 0.5f), resolution, 0, SpriteMeshType.FullRect, border);
        sprite.name = circle ? "ARCameraCircle" : "ARCameraRoundedRect";
        return sprite;
    }

    private static Sprite LoadSprite(string iconName)
    {
        if (Sprites.TryGetValue(iconName, out Sprite cached)) return cached;
        if (iconName == "Menu" || iconName == "Close" || iconName == "Add" || iconName == "Edit" || iconName == "Shutter" || iconName == "Search")
        {
            Sprite glyph = CreateCameraGlyph(iconName);
            Sprites.Add(iconName, glyph);
            return glyph;
        }
        Texture2D texture = Resources.Load<Texture2D>("ARIcons/" + iconName);
        if (texture == null)
        {
            Debug.LogWarning("Ícone de interface não encontrado: " + iconName);
            return null;
        }
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = "ARIcon_" + iconName;
        Sprites.Add(iconName, sprite);
        return sprite;
    }

    private static Sprite CreateCameraGlyph(string name)
    {
        Vector4[] lines;
        switch (name)
        {
            case "Menu": lines = new[] {new Vector4(22, 92, 106, 92), new Vector4(22, 64, 106, 64), new Vector4(22, 36, 106, 36)}; break;
            case "Close": lines = new[] {new Vector4(28, 28, 100, 100), new Vector4(28, 100, 100, 28)}; break;
            case "Add": lines = new[] {new Vector4(22, 64, 106, 64), new Vector4(64, 22, 64, 106)}; break;
            case "Edit": lines = new[] {new Vector4(25, 22, 34, 51), new Vector4(34, 51, 87, 104), new Vector4(87, 104, 106, 85), new Vector4(106, 85, 53, 32), new Vector4(53, 32, 25, 22), new Vector4(78, 95, 97, 76), new Vector4(34, 51, 53, 32)}; break;
            case "Search": lines = new[] {new Vector4(48, 89, 55, 96), new Vector4(55, 96, 64, 99), new Vector4(64, 99, 73, 96), new Vector4(73, 96, 80, 89), new Vector4(80, 89, 83, 80), new Vector4(83, 80, 80, 71), new Vector4(80, 71, 73, 64), new Vector4(73, 64, 64, 61), new Vector4(64, 61, 55, 64), new Vector4(55, 64, 48, 71), new Vector4(48, 71, 45, 80), new Vector4(45, 80, 48, 89), new Vector4(79, 65, 106, 38)}; break;
            default: lines = new Vector4[0]; break;
        }
        const int resolution = 128;
        Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
        {name = "ARCamera" + name, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp};
        Color32[] pixels = new Color32[resolution * resolution];
        for (int y = 0; y < resolution; y++)
        for (int x = 0; x < resolution; x++)
        {
            Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
            float distance = float.MaxValue;
            foreach (Vector4 line in lines)
            {
                Vector2 a = new Vector2(line.x, line.y);
                Vector2 delta = new Vector2(line.z, line.w) - a;
                float t = Mathf.Clamp01(Vector2.Dot(point - a, delta) / delta.sqrMagnitude);
                distance = Mathf.Min(distance, Vector2.Distance(point, a + delta * t) - 4f);
            }
            if (name == "Shutter")
            {
                float radius = Vector2.Distance(point, new Vector2(64f, 64f));
                distance = Mathf.Min(Mathf.Abs(radius - 58f) - 2.5f, radius - 49f);
            }
            pixels[y * resolution + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(0.5f - distance) * 255f));
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), 100f);
    }
}
