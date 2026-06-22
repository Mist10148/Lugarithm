using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Composable UI construction for the scene builders, with one consistent
/// placeholder style (dark panels, amber accents). Everything is plain
/// uGUI + TMP; the art pass later only swaps sprites and colors.
/// </summary>
public static class UIFactory
{
    // Palette
    public static readonly Color PanelDark   = new Color(0.10f, 0.12f, 0.16f, 0.96f);
    public static readonly Color PanelDarker = new Color(0.06f, 0.07f, 0.10f, 0.98f);
    public static readonly Color ButtonFace  = new Color(0.18f, 0.22f, 0.30f, 1f);
    public static readonly Color Accent      = new Color(0.95f, 0.65f, 0.15f, 1f);
    public static readonly Color TextBright  = new Color(0.93f, 0.93f, 0.88f, 1f);
    public static readonly Color TextDim     = new Color(0.62f, 0.64f, 0.66f, 1f);

    /// <summary>
    /// Optional menu-specific TMP font. Scene builders can set this while
    /// assembling a screen that needs a custom face, then restore it after.
    /// </summary>
    public static TMP_FontAsset FontOverride { get; set; }

    // -------------------------------------------------------------------------
    // Canvas

    /// <summary>Screen-Space-Overlay canvas scaling against 1920×1080.</summary>
    public static Canvas CreateCanvas(string name, int sortOrder = 0)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortOrder;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    // -------------------------------------------------------------------------
    // Rect / panel primitives

    /// <summary>Child RectTransform with anchors; no graphic.</summary>
    public static RectTransform CreateRect(Transform parent, string name,
                                           Vector2 anchorMin, Vector2 anchorMax,
                                           Vector2 offsetMin = default, Vector2 offsetMax = default)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        return rt;
    }

    /// <summary>Anchored, fixed-size child rect.</summary>
    public static RectTransform CreateFixedRect(Transform parent, string name,
                                                Vector2 anchor, Vector2 anchoredPos, Vector2 size)
    {
        var rt = CreateRect(parent, name, anchor, anchor);
        rt.pivot = anchor;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        return rt;
    }

    /// <summary>Filled panel. Use a clear color for invisible containers.</summary>
    public static RectTransform CreatePanel(Transform parent, string name,
                                            Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var rt = CreateRect(parent, name, anchorMin, anchorMax);
        var image = rt.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = color.a > 0.01f;
        return rt;
    }

    public static Image AddImage(RectTransform rt, Color color, Sprite sprite = null)
    {
        var image = rt.gameObject.AddComponent<Image>();
        image.color  = color;
        image.sprite = sprite;
        return image;
    }

    // -------------------------------------------------------------------------
    // Text

    public static TextMeshProUGUI CreateText(Transform parent, string name, string text,
                                             float fontSize, Color color,
                                             TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        var rt = CreateRect(parent, name, Vector2.zero, Vector2.one);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text          = text;
        tmp.fontSize      = fontSize;
        tmp.color         = color;
        tmp.alignment     = alignment;
        tmp.raycastTarget = false;

        TMP_FontAsset font = FontOverride != null ? FontOverride : DefaultFont();
        if (font != null)
            tmp.font = font;

        return tmp;
    }

    static TMP_FontAsset _defaultFont;
    static bool _fontResolved;

    /// <summary>
    /// Default TMP font, resolved once. TMP_Settings throws in batch mode when
    /// the settings asset hasn't loaded, so fall back to the imported
    /// TMP Essential Resources font directly.
    /// </summary>
    static TMP_FontAsset DefaultFont()
    {
        if (_fontResolved) return _defaultFont;
        _fontResolved = true;

        try { _defaultFont = TMP_Settings.defaultFontAsset; }
        catch { _defaultFont = null; }

        if (_defaultFont == null)
            _defaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

        if (_defaultFont == null)
            Debug.LogWarning("[Lugarithm] No TMP font resolved at build time — TMP runtime fallback will be used.");

        return _defaultFont;
    }

    // -------------------------------------------------------------------------
    // Buttons

    public static Button CreateButton(Transform parent, string name, string label,
                                      Vector2 size, float fontSize = 28f)
    {
        var rt = CreateRect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        rt.sizeDelta = size;

        var image = rt.gameObject.AddComponent<Image>();
        image.sprite = BuiltinSprite("UISprite.psd");
        image.type   = Image.Type.Sliced;
        image.color  = ButtonFace;

        var button = rt.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor      = Color.white;
        colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
        colors.pressedColor     = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.disabledColor    = new Color(0.55f, 0.55f, 0.55f, 0.6f);
        button.colors = colors;

        var text = CreateText(rt, "Label", label, fontSize, TextBright);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.rectTransform.offsetMin = new Vector2(8f, 4f);
        text.rectTransform.offsetMax = new Vector2(-8f, -4f);

        return button;
    }

    /// <summary>
    /// Button built from a custom sprite atlas/sheet slice. Keeps the same TMP
    /// label structure as <see cref="CreateButton"/> so menu builders can swap
    /// in pixel-art UI without changing interaction code.
    /// </summary>
    public static Button CreateArtButton(Transform parent, string name, string label,
                                         Vector2 size, Sprite sprite,
                                         float fontSize = 28f,
                                         Color? textColor = null)
    {
        var button = CreateButton(parent, name, label, size, fontSize);
        var image = button.image;
        image.sprite = sprite;
        image.type   = Image.Type.Simple;
        image.color  = Color.white;
        image.preserveAspect = false;

        if (textColor.HasValue)
        {
            var tmp = button.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
                tmp.color = textColor.Value;
        }

        return button;
    }

    /// <summary>
    /// Menu-only helper that builds an icon-led tile with the caption inside
    /// the tile, leaving room for a clean press animation and a visible icon.
    /// </summary>
    public static Button CreateIconButton(Transform parent, string name, string caption,
                                          Vector2 size, Sprite faceSprite, Sprite iconSprite,
                                          float captionFontSize = 14f,
                                          Color? captionColor = null,
                                          float iconSize = 28f)
    {
        var button = CreateArtButton(parent, name, caption ?? string.Empty, size, faceSprite,
                                     captionFontSize, captionColor ?? TextBright);

        var face = button.image;
        if (face != null)
        {
            // Keep the sheet slice as a full sprite so the tile fills its
            // rect cleanly while preserving the intended square proportions.
            face.type = Image.Type.Simple;
            face.preserveAspect = true;
            face.color = Color.white;
        }

        // Keep the label low in the tile so the icon has room above it.
        var label = button.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
        {
            label.alignment = TextAlignmentOptions.Bottom;
            label.enableAutoSizing = true;
            label.fontSizeMin = captionFontSize - 2f;
            label.fontSizeMax = captionFontSize;
            label.rectTransform.anchorMin = new Vector2(0f, 0f);
            label.rectTransform.anchorMax = new Vector2(1f, 1f);
            label.rectTransform.offsetMin = new Vector2(6f, 2f);
            label.rectTransform.offsetMax = new Vector2(-6f, -34f);
            label.raycastTarget = false;
            if (string.IsNullOrWhiteSpace(caption))
                label.gameObject.SetActive(false);
        }

        if (iconSprite != null)
        {
            var icon = CreateRect(button.transform, "Icon", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            icon.sizeDelta = new Vector2(iconSize, iconSize);
            icon.anchoredPosition = new Vector2(0f, 20f);
            var iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.sprite = iconSprite;
            iconImage.color = Color.white;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }

        return button;
    }

    /// <summary>
    /// Menu-only press feedback: light face -> dark face -> return to light,
    /// then run the supplied action.
    /// </summary>
    public static MenuButtonPressFlash AddPressFlash(Button button)
    {
        var flash = button.gameObject.GetComponent<MenuButtonPressFlash>();
        if (flash == null)
            flash = button.gameObject.AddComponent<MenuButtonPressFlash>();
        return flash;
    }

    // -------------------------------------------------------------------------
    // Toggle

    public static Toggle CreateToggle(Transform parent, string name, Vector2 size)
    {
        var rt = CreateRect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        rt.sizeDelta = size;

        var toggle = rt.gameObject.AddComponent<Toggle>();

        var bg = CreateRect(rt, "Background", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
        bg.pivot = new Vector2(0f, 0.5f);
        bg.sizeDelta = new Vector2(size.y, size.y);
        var bgImage = bg.gameObject.AddComponent<Image>();
        bgImage.sprite = BuiltinSprite("UISprite.psd");
        bgImage.type   = Image.Type.Sliced;
        bgImage.color  = PanelDarker;

        var check = CreateRect(bg, "Checkmark", Vector2.zero, Vector2.one,
                               new Vector2(5f, 5f), new Vector2(-5f, -5f));
        var checkImage = check.gameObject.AddComponent<Image>();
        checkImage.color = Accent;

        toggle.targetGraphic = bgImage;
        toggle.graphic       = checkImage;
        toggle.isOn          = true;

        return toggle;
    }

    // -------------------------------------------------------------------------
    // Slider (placeholder rows only — built disabled)

    public static Slider CreateSlider(Transform parent, string name, Vector2 size)
    {
        var rt = CreateRect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        rt.sizeDelta = size;

        var slider = rt.gameObject.AddComponent<Slider>();

        var bg = CreateRect(rt, "Background", new Vector2(0f, 0.35f), new Vector2(1f, 0.65f));
        bg.gameObject.AddComponent<Image>().color = PanelDarker;

        var fillArea = CreateRect(rt, "Fill Area", new Vector2(0f, 0.35f), new Vector2(1f, 0.65f));
        var fill = CreateRect(fillArea, "Fill", Vector2.zero, new Vector2(0.8f, 1f));
        fill.gameObject.AddComponent<Image>().color = Accent;

        slider.fillRect = fill;
        slider.value    = 0.8f;

        return slider;
    }

    // -------------------------------------------------------------------------
    // Scroll view

    /// <summary>
    /// Vertical scroll view; <paramref name="content"/> gets a VerticalLayoutGroup
    /// + ContentSizeFitter so children stack and size it automatically.
    /// </summary>
    public static ScrollRect CreateScrollView(Transform parent, string name,
                                              Vector2 anchorMin, Vector2 anchorMax,
                                              out RectTransform content)
    {
        var rt = CreatePanel(parent, name, anchorMin, anchorMax, PanelDarker);
        var scroll = rt.gameObject.AddComponent<ScrollRect>();

        var viewport = CreateRect(rt, "Viewport", Vector2.zero, Vector2.one,
                                  new Vector2(4f, 4f), new Vector2(-4f, -4f));
        viewport.gameObject.AddComponent<RectMask2D>();
        var vpImage = viewport.gameObject.AddComponent<Image>();
        vpImage.color = Color.clear;
        vpImage.raycastTarget = true; // catches drag for scrolling

        content = CreateRect(viewport, "Content", new Vector2(0f, 1f), Vector2.one);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;

        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6f;
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.childAlignment      = TextAnchor.UpperLeft;
        layout.childControlWidth   = true;
        layout.childControlHeight  = false;
        layout.childForceExpandWidth  = true;
        layout.childForceExpandHeight = false;

        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport   = viewport;
        scroll.content    = content;
        scroll.horizontal = false;
        scroll.vertical   = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 25f;

        return scroll;
    }

    // -------------------------------------------------------------------------
    // Multiline input (code editor)

    /// <summary>Multiline TMP input field mirroring TMP_DefaultControls' structure.</summary>
    public static TMP_InputField CreateMultilineInput(Transform parent, string name,
                                                      Vector2 anchorMin, Vector2 anchorMax,
                                                      float fontSize = 22f)
    {
        var rt = CreateRect(parent, name, anchorMin, anchorMax);
        var image = rt.gameObject.AddComponent<Image>();
        image.sprite = BuiltinSprite("InputFieldBackground.psd");
        image.type   = Image.Type.Sliced;
        image.color  = PanelDarker;

        var input = rt.gameObject.AddComponent<TMP_InputField>();

        var textArea = CreateRect(rt, "Text Area", Vector2.zero, Vector2.one,
                                  new Vector2(10f, 8f), new Vector2(-10f, -8f));
        textArea.gameObject.AddComponent<RectMask2D>();

        var text = CreateText(textArea, "Text", "", fontSize, TextBright, TextAlignmentOptions.TopLeft);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;

        var placeholder = CreateText(textArea, "Placeholder", "", fontSize, TextDim, TextAlignmentOptions.TopLeft);
        placeholder.rectTransform.anchorMin = Vector2.zero;
        placeholder.rectTransform.anchorMax = Vector2.one;
        placeholder.rectTransform.offsetMin = Vector2.zero;
        placeholder.rectTransform.offsetMax = Vector2.zero;
        placeholder.fontStyle = FontStyles.Italic;
        placeholder.raycastTarget = false;

        input.textViewport   = textArea;
        input.textComponent  = text;
        input.placeholder    = placeholder;
        input.lineType       = TMP_InputField.LineType.MultiLineNewline;
        input.caretColor     = Accent;
        input.customCaretColor = true;
        input.selectionColor = new Color(Accent.r, Accent.g, Accent.b, 0.35f);

        return input;
    }

    // -------------------------------------------------------------------------
    // Layout helpers

    public static VerticalLayoutGroup AddVerticalLayout(RectTransform rt, float spacing,
                                                        RectOffset padding = null,
                                                        TextAnchor align = TextAnchor.UpperCenter)
    {
        var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.padding = padding ?? new RectOffset(0, 0, 0, 0);
        layout.childAlignment      = align;
        layout.childControlWidth   = false;
        layout.childControlHeight  = false;
        layout.childForceExpandWidth  = false;
        layout.childForceExpandHeight = false;
        return layout;
    }

    public static HorizontalLayoutGroup AddHorizontalLayout(RectTransform rt, float spacing,
                                                            RectOffset padding = null,
                                                            TextAnchor align = TextAnchor.MiddleLeft)
    {
        var layout = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.padding = padding ?? new RectOffset(0, 0, 0, 0);
        layout.childAlignment      = align;
        layout.childControlWidth   = false;
        layout.childControlHeight  = false;
        layout.childForceExpandWidth  = false;
        layout.childForceExpandHeight = false;
        return layout;
    }

    public static LayoutElement SetLayoutSize(Component target, float width = -1f, float height = -1f)
    {
        var le = target.gameObject.GetComponent<LayoutElement>();
        if (le == null) le = target.gameObject.AddComponent<LayoutElement>();
        if (width  >= 0f) le.preferredWidth  = width;
        if (height >= 0f) le.preferredHeight = height;
        return le;
    }

    /// <summary>Pins a rect to a single anchor with an offset and fixed size.</summary>
    public static void Place(Component target, Vector2 anchor, Vector2 anchoredPos, Vector2 size)
    {
        var rt = (RectTransform)target.transform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot     = anchor;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
    }

    // -------------------------------------------------------------------------

    public static Sprite BuiltinSprite(string file)
    {
        return AssetDatabase.GetBuiltinExtraResource<Sprite>($"UI/Skin/{file}");
    }
}
