using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class ARUIIconFactory
{
    private static readonly Color BrandBlue = new Color(0.157f, 0.408f, 0.573f, 0.94f);
    private static readonly Color SelectedColor = new Color(0.98f, 0.85f, 0.48f, 0.98f);
    private static readonly Color Ink = new Color(0.09f, 0.16f, 0.22f, 1f);
    private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

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
        button.image.color = selected ? SelectedColor : BrandBlue;
        Color foreground = selected ? Ink : Color.white;
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

    private static Sprite LoadSprite(string iconName)
    {
        if (Sprites.TryGetValue(iconName, out Sprite cached)) return cached;
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
}
