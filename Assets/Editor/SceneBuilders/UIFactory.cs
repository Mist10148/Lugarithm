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
    public static readonly Color TutorialPlum = new Color(0.025f, 0.065f, 0.055f, 0.97f);
    public static readonly Color TutorialCream = new Color(0.93f, 0.94f, 0.82f, 1f);
    public static readonly Color TutorialMuted = new Color(0.68f, 0.75f, 0.67f, 1f);
    public static readonly Color TutorialGold = new Color(0.96f, 0.65f, 0.14f, 1f);
    public static readonly Color TutorialButton = new Color(0.20f, 0.34f, 0.29f, 1f);
    public static readonly Color TutorialBorder = new Color(0.78f, 0.64f, 0.28f, 1f);
    public static readonly Color TutorialCell = new Color(0.13f, 0.18f, 0.16f, 1f);

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
    /// Applies the Main Menu / Level Select pixel treatment to an already-built
    /// tutorial UI subtree. This changes presentation components only and leaves
    /// hierarchy, object identity, callbacks, and serialized controller wiring intact.
    /// </summary>
    public static void ApplyTutorialPixelTheme(Transform root)
    {
        if (root == null) return;

        TMP_FontAsset pixelFont = SproutLandsMenuFont.EnsureFontAsset();
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.text == "▶ RUN") text.text = "RUN";
            else if (text.text == "↺ Reset") text.text = "RESET";
            else if (text.text == "🤖 Autopilot") text.text = "AUTOPILOT";
            else if (text.text == "💡 Hint") text.text = "HINT";
            else if (text.text == "▲") text.text = "UP";
            else if (text.text == "▼") text.text = "V";
            else if (text.text == "Next ▶") text.text = "NEXT";

            if (pixelFont != null)
                text.font = pixelFont;

            string name = text.gameObject.name;
            if (name == "Title" || name == "LevelName" || name == "SpeakerLabel" ||
                name == "Category" || name == "Timer")
                text.color = TutorialGold;
            else if (name == "Instruction" || name == "Goal" || name == "Hint" ||
                     name == "HintLabel" || name == "PlaceholderNote")
                text.color = TutorialMuted;
            else
                text.color = TutorialCream;

            text.outlineColor = new Color32(42, 18, 24, 220);
            text.outlineWidth = 0.12f;
        }

        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            string buttonName = button.gameObject.name;
            if (buttonName.StartsWith("Cell_") || button.GetComponent<FlowCell>() != null)
                continue;

            Image image = button.targetGraphic as Image ?? button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = BuiltinSprite("UISprite.psd");
                image.type = Image.Type.Sliced;
                image.preserveAspect = false;
                image.color = TutorialButton;
                button.targetGraphic = image;

                Outline outline = image.GetComponent<Outline>();
                if (outline == null)
                    outline = image.gameObject.AddComponent<Outline>();
                outline.effectColor = TutorialBorder;
                outline.effectDistance = new Vector2(2f, -2f);
                outline.useGraphicAlpha = true;
            }

            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.20f, 1.20f, 1.12f, 1f);
            colors.pressedColor = new Color(0.72f, 0.78f, 0.72f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.42f, 0.48f, 0.43f, 0.55f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
        }

        foreach (Image image in root.GetComponentsInChildren<Image>(true))
        {
            string name = image.gameObject.name;
            if (name != "Window" && name != "DialogueBar" && name != "JournalCard" &&
                name != "PromptBg" && name != "AnalysisGroup" && name != "MazePanel")
                continue;

            image.sprite = BuiltinSprite("UISprite.psd");
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            image.color = TutorialPlum;

            Outline outline = image.GetComponent<Outline>();
            if (outline == null)
                outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = TutorialBorder;
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = true;
        }
    }

    public static TMP_Dropdown CreateDropdown(Transform parent, string name,
                                              Vector2 size, float fontSize = 17f)
    {
        var rt = CreateRect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        rt.sizeDelta = size;

        var image = rt.gameObject.AddComponent<Image>();
        image.sprite = BuiltinSprite("InputFieldBackground.psd");
        image.type = Image.Type.Sliced;
        image.color = ButtonFace;

        var dropdown = rt.gameObject.AddComponent<TMP_Dropdown>();
        dropdown.targetGraphic = image;

        TMP_Text caption = CreateText(rt, "Label", "", fontSize, TextBright,
                                      TextAlignmentOptions.MidlineLeft);
        caption.rectTransform.offsetMin = new Vector2(12f, 3f);
        caption.rectTransform.offsetMax = new Vector2(-34f, -3f);
        caption.textWrappingMode = TextWrappingModes.NoWrap;
        caption.overflowMode = TextOverflowModes.Ellipsis;
        dropdown.captionText = caption;

        TMP_Text arrow = CreateText(rt, "Arrow", "▼", fontSize, TextDim,
                                    TextAlignmentOptions.Center);
        Place(arrow, new Vector2(1f, 0.5f), new Vector2(-17f, 0f), new Vector2(28f, size.y - 6f));

        RectTransform template = CreatePanel(rt, "Template", Vector2.zero, Vector2.one, PanelDarker);
        Place(template, new Vector2(0.5f, 0f), new Vector2(0f, -4f), new Vector2(size.x, 160f));
        template.pivot = new Vector2(0.5f, 1f);
        var templateImage = template.GetComponent<Image>();
        templateImage.sprite = BuiltinSprite("UISprite.psd");
        templateImage.type = Image.Type.Sliced;

        var scroll = template.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;

        RectTransform viewport = CreatePanel(template, "Viewport", Vector2.zero, Vector2.one,
                                             new Color(0f, 0f, 0f, 0.01f));
        viewport.offsetMin = new Vector2(4f, 4f);
        viewport.offsetMax = new Vector2(-4f, -4f);
        var mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        scroll.viewport = viewport;

        RectTransform content = CreateRect(viewport, "Content", new Vector2(0f, 1f), new Vector2(1f, 1f));
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 32f);
        scroll.content = content;
        AddVerticalLayout(content, 0f, align: TextAnchor.UpperCenter);
        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform item = CreateRect(content, "Item", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        item.sizeDelta = new Vector2(size.x - 8f, 32f);
        SetLayoutSize(item, size.x - 8f, 32f);
        var itemBg = item.gameObject.AddComponent<Image>();
        itemBg.sprite = BuiltinSprite("UISprite.psd");
        itemBg.type = Image.Type.Sliced;
        itemBg.color = ButtonFace;
        var toggle = item.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = itemBg;
        TMP_Text itemText = CreateText(item, "Item Label", "Option", fontSize, TextBright,
                                       TextAlignmentOptions.MidlineLeft);
        itemText.rectTransform.offsetMin = new Vector2(10f, 2f);
        itemText.rectTransform.offsetMax = new Vector2(-10f, -2f);
        itemText.textWrappingMode = TextWrappingModes.NoWrap;
        itemText.overflowMode = TextOverflowModes.Ellipsis;
        dropdown.itemText = itemText;
        dropdown.template = template;
        template.gameObject.SetActive(false);

        return dropdown;
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

        if (string.IsNullOrWhiteSpace(label))
        {
            var tmp = button.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
                tmp.gameObject.SetActive(false);
        }

        return button;
    }

    /// <summary>
    /// Menu-only helper that builds a square face with one centered icon and an
    /// optional caption. This keeps the bottom menu actions readable without
    /// relying on baked-in composite art.
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
            face.type = Image.Type.Simple;
            face.preserveAspect = false;
            face.color = Color.white;
        }

        if (iconSprite != null)
        {
            var icon = CreateRect(button.transform, "Icon",
                                  new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            icon.sizeDelta = new Vector2(iconSize, iconSize);
            icon.anchoredPosition = Vector2.zero;
            icon.pivot = new Vector2(0.5f, 0.5f);

            var iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.sprite = iconSprite;
            iconImage.type = Image.Type.Simple;
            iconImage.color = Color.white;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }

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

        return button;
    }

    /// <summary>
    /// Menu tile helper for the lower main-menu options.
    /// Builds a clean face button with a centered icon and a separate caption
    /// underneath so the text never sits inside the clickable area.
    /// </summary>
    public static Button CreateIconCaptionTile(Transform parent, string name, string caption,
                                               Vector2 buttonSize, Sprite faceSprite,
                                               Sprite iconSprite, float iconSize = 36f,
                                               float captionFontSize = 14f,
                                               Color? captionColor = null)
    {
        var button = CreateArtButton(parent, name, string.Empty, buttonSize, faceSprite,
                                     captionFontSize, captionColor ?? TextBright);

        var face = button.image;
        if (face != null)
        {
            face.type = Image.Type.Simple;
            face.preserveAspect = true;
            face.color = Color.white;
        }

        var hiddenLabel = button.transform.Find("Label");
        if (hiddenLabel != null)
            UnityEngine.Object.DestroyImmediate(hiddenLabel.gameObject);

        var buttonRect = button.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 1f);
        buttonRect.anchorMax = new Vector2(0.5f, 1f);
        buttonRect.pivot = new Vector2(0.5f, 1f);
        buttonRect.anchoredPosition = new Vector2(0f, -6f);
        buttonRect.sizeDelta = buttonSize;

        if (iconSprite != null)
        {
            var icon = CreateRect(button.transform, "Icon",
                                  new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            icon.sizeDelta = new Vector2(iconSize, iconSize);
            icon.anchoredPosition = Vector2.zero;
            icon.pivot = new Vector2(0.5f, 0.5f);

            var iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.sprite = iconSprite;
            iconImage.type = Image.Type.Simple;
            iconImage.color = Color.white;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }

        var label = CreateText(parent, $"{name}Caption", caption ?? string.Empty, captionFontSize,
                               captionColor ?? TextBright, TextAlignmentOptions.Center);
        label.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        label.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        label.rectTransform.pivot = new Vector2(0.5f, 0f);
        label.rectTransform.anchoredPosition = new Vector2(0f, 12f);
        label.rectTransform.sizeDelta = new Vector2(buttonSize.x + 48f, 24f);
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;

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
    // Segmented selector (pill buttons — a clearer either/or-or-more control)

    /// <summary>
    /// A horizontal row of pill buttons for a mutually-exclusive setting
    /// (Manual/Automation, Blocks/Code, Slow/Normal/Fast/Instant, …). The container
    /// carries a <see cref="SegmentedSelector"/> that highlights the active option;
    /// segment index == option index. Anchored top-left so it flows beneath a row
    /// label placed with <see cref="Place"/>.
    /// </summary>
    public static SegmentedSelector CreateSegmentedSelector(Transform parent, string name,
                                                            string[] options, float segmentWidth,
                                                            float height, float gap = 6f,
                                                            float fontSize = 19f,
                                                            string[] optionKeys = null)
    {
        int n = options != null ? options.Length : 0;
        float totalW = n > 0 ? n * segmentWidth + (n - 1) * gap : segmentWidth;

        var rt = CreateRect(parent, name, new Vector2(0f, 1f), new Vector2(0f, 1f));
        rt.pivot     = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(totalW, height);

        var selector = rt.gameObject.AddComponent<SegmentedSelector>();
        for (int i = 0; i < n; i++)
        {
            Button seg = CreateButton(rt, $"{name}_seg{i}", options[i],
                                      new Vector2(segmentWidth, height), fontSize);
            Place(seg, new Vector2(0f, 1f), new Vector2(i * (segmentWidth + gap), 0f),
                  new Vector2(segmentWidth, height));
            if (optionKeys != null && i < optionKeys.Length)
                LocalizeButton(seg, optionKeys[i]);
        }
        return selector;
    }

    // -------------------------------------------------------------------------
    // Localization

    /// <summary>Makes an existing TMP label follow the active UI language: seeds
    /// the English text now (build time) and attaches a <see cref="LocalizedLabel"/>
    /// carrying the key (set via SerializedObject so it persists in the scene).</summary>
    public static void Localize(TMP_Text label, string key)
    {
        if (label == null || string.IsNullOrEmpty(key)) return;
        label.text = LocalizationTable.Get(key, GameLanguage.English);
        var loc = label.gameObject.GetComponent<LocalizedLabel>();
        if (loc == null) loc = label.gameObject.AddComponent<LocalizedLabel>();
        SceneBuilderUtil.Wire(loc, "key", key);
    }

    /// <summary>Localizes a <see cref="CreateButton"/> / <see cref="CreateArtButton"/>
    /// label by key.</summary>
    public static void LocalizeButton(Button button, string key)
    {
        if (button == null) return;
        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null) Localize(label, key);
    }

    /// <summary>CreateText that follows the active UI language.</summary>
    public static TextMeshProUGUI CreateLocalizedText(Transform parent, string name, string key,
                                                      float fontSize, Color color,
                                                      TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        var tmp = CreateText(parent, name, LocalizationTable.Get(key, GameLanguage.English),
                             fontSize, color, alignment);
        Localize(tmp, key);
        return tmp;
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

    /// <summary>
    /// Adds a draggable vertical scrollbar pinned to the right edge of a
    /// <see cref="CreateScrollView"/> result, and insets the viewport so content
    /// never sits under the bar. Auto-hides when the content fits.
    /// </summary>
    public static Scrollbar AddVerticalScrollbar(ScrollRect scroll, float width = 12f,
                                                 bool permanent = false)
    {
        var sbRt = CreateRect(scroll.transform, "Scrollbar Vertical",
                              new Vector2(1f, 0f), new Vector2(1f, 1f),
                              new Vector2(-width - 2f, 4f), new Vector2(-2f, -4f));
        var track = sbRt.gameObject.AddComponent<Image>();
        track.color = new Color(PanelDarker.r, PanelDarker.g, PanelDarker.b, 0.9f);

        var scrollbar = sbRt.gameObject.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        var slidingArea = CreateRect(sbRt, "Sliding Area", Vector2.zero, Vector2.one,
                                     new Vector2(1f, 1f), new Vector2(-1f, -1f));
        var handle = CreateRect(slidingArea, "Handle", Vector2.zero, Vector2.one);
        var handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.sprite = BuiltinSprite("UISprite.psd");
        handleImage.type   = Image.Type.Sliced;
        handleImage.color  = Accent;

        scrollbar.handleRect    = handle;
        scrollbar.targetGraphic = handleImage;

        scroll.verticalScrollbar = scrollbar;
        // Permanent keeps the bar always visible (so the user can see there's more to read
        // even before they hover); AutoHide tucks it away when everything already fits.
        scroll.verticalScrollbarVisibility = permanent
            ? ScrollRect.ScrollbarVisibility.Permanent
            : ScrollRect.ScrollbarVisibility.AutoHide;

        // Keep content clear of the bar.
        if (scroll.viewport != null)
            scroll.viewport.offsetMax = new Vector2(scroll.viewport.offsetMax.x - width - 4f,
                                                    scroll.viewport.offsetMax.y);
        return scrollbar;
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
