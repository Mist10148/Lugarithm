using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Named layout blueprint for the Settings window, measured against the
/// canonical 1920×1080 canvas (the window itself is a fixed 700×665 rect,
/// anchored top-left internally so every row Y is a negative offset from the
/// window's top edge). Centralizing these values keeps the panel's geometry in
/// one auditable place instead of scattering unexplained numbers through the
/// builder. Values match the shipped reference layout — see
/// <see cref="SettingsPanelBuilder"/>.
/// </summary>
public static class SettingsLayout
{
    // Outer window ---------------------------------------------------------
    public static readonly Vector2 WindowSize = new Vector2(700f, 665f);

    // Header ---------------------------------------------------------------
    public static readonly Vector2 TitleOffset   = new Vector2(0f, -18f);   // from window top-center
    public static readonly Vector2 TitleSize     = new Vector2(350f, 44f);
    public const float TitleFont = 34f;
    public static readonly Vector4 TitleTextPad  = new Vector4(40f, 8f, -40f, -8f); // L,B,R,T offsets

    // Section heading + icon ----------------------------------------------
    public const float IconColumnX  = 31f;   // left edge of the icon frame
    public const float IconSize     = 38f;
    public const float IconInset    = 10f;   // padding of the icon inside its frame
    public const float HeadingX     = 80f;   // common left baseline for section headings
    public static readonly Vector2 HeadingSize = new Vector2(550f, 24f);
    public const float HeadingFont  = 22f;

    // Rows -----------------------------------------------------------------
    public const float LabelX   = 88f;    // common left edge for every row label
    public const float CtrlX    = 301f;   // common starting X for every control group
    public const float SegH     = 30f;    // segmented-pill height
    public const float SegGap   = 6f;     // gap between pills
    public const float LabelFont = 16f;
    public static readonly Vector2 LabelSize = new Vector2(190f, 30f);

    // Segmented-pill widths by option count/label length.
    public const float SegWideWidth    = 138f; // two wide options (Manual/Automation …)
    public const float SegMediumWidth  = 98f;  // two short options (On/Off)
    public const float SegCompactWidth = 70f;  // four options (dialogue speed)
    public const float SegDefaultFont  = 19f;
    public const float SegCompactFont  = 12f;

    // Sliders --------------------------------------------------------------
    public static readonly Vector2 SliderSize = new Vector2(280f, 20f);

    // Appearance / theme row ----------------------------------------------
    public static readonly Vector2 ThemeButtonSize = new Vector2(105f, 38f);
    public const float ThemeButtonFont = 18f;
    public const float ThemeValueGap   = 120f; // value label offset from CtrlX
    public static readonly Vector2 ThemeValueSize = new Vector2(180f, 38f);
    public const float ThemeValueFont  = 22f;

    // Divider --------------------------------------------------------------
    public const float DividerX     = 28f;
    public const float DividerWidth = 644f;
    public const float DividerThick = 2f;
    public static readonly Color32 DividerColor = new Color32(92, 58, 94, 255);

    // Bottom Close button --------------------------------------------------
    // The reused window sprite has ~37px of transparent padding below its drawn
    // border, so the button must sit ~40px up to land inside the visible frame.
    public static readonly Vector2 CloseSize   = new Vector2(261f, 34f);
    public static readonly Vector2 CloseOffset = new Vector2(0f, 38f); // from window bottom-center
    public const float CloseFont = 22f;
    public static readonly Color32 CloseLabelColor = new Color32(30, 20, 15, 255); // dark, on the gold button

    // Row Y offsets from the window's top edge (negative = downward).
    public const float SectionGameplayY   = -72f;
    public const float RowDriveModeY       = -100f;
    public const float RowCodingY          = -138f;
    public const float DividerGameplayY    = -178f;

    public const float SectionControlsY    = -195f;
    public const float RowBrakeY           = -222f;
    public const float DividerControlsY    = -257f;

    public const float SectionAudioY       = -273f;
    public const float RowMusicY           = -304f;
    public const float RowSfxY             = -337f;
    public const float DividerAudioY       = -365f;

    public const float SectionLanguageY    = -382f;
    public const float RowLanguageY        = -410f;
    public const float RowSubtitlesY       = -448f;
    public const float RowDialogueSpeedY   = -486f;
    public const float DividerLanguageY    = -515f;

    public const float SectionAppearanceY  = -528f;
    public const float RowThemeY           = -556f;
}

/// <summary>
/// Builds the Settings panel subtree and wires the <see cref="SettingsPanel"/>
/// component. Now consumed once by <c>SettingsOverlayBuilder</c> to bake the
/// single universal overlay prefab (previously called per-scene, which produced
/// duplicate panels). Organized into sections — Gameplay, Controls, Audio,
/// Language &amp; Text, Appearance — with <see cref="SegmentedSelector"/> pills for
/// either/or settings (both labels visible, active highlighted) instead of
/// ambiguous toggles. Every label/option is localized, so flipping the Language
/// pill re-renders this panel (and the rest of the UI) live. All geometry comes
/// from <see cref="SettingsLayout"/>.
/// </summary>
public static class SettingsPanelBuilder
{
    public static SettingsPanel Build(Transform canvasRoot)
    {
        // Always-active host so SettingsPanel.Start runs while the overlay is hidden.
        var host = UIFactory.CreateRect(canvasRoot, "Settings", Vector2.zero, Vector2.one);
        var panel = host.gameObject.AddComponent<SettingsPanel>();

        // Full-screen dim overlay (the toggled root).
        var overlay = UIFactory.CreatePanel(host, "SettingsOverlay", Vector2.zero, Vector2.one,
                                            new Color(0f, 0f, 0f, 0.65f));

        // Centered window.
        var window = UIFactory.CreatePanel(overlay, "Window",
                                           new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                           UIFactory.PanelDark);
        UIFactory.Place(window, new Vector2(0.5f, 0.5f), Vector2.zero, SettingsLayout.WindowSize);
        var windowImage = window.GetComponent<Image>();
        windowImage.sprite = LugarithmUiSkin.SettingsWindow;
        windowImage.type = Image.Type.Simple;
        windowImage.preserveAspect = false;
        windowImage.color = Color.white;

        var titlePlate = UIFactory.CreatePanel(window, "TitlePlate", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Color.white);
        titlePlate.GetComponent<Image>().sprite = null;
        titlePlate.GetComponent<Image>().color = Color.clear;
        UIFactory.Place(titlePlate, new Vector2(0.5f, 1f), SettingsLayout.TitleOffset, SettingsLayout.TitleSize);
        var title = UIFactory.CreateLocalizedText(titlePlate, "Title", "settings.title", SettingsLayout.TitleFont, UIFactory.Accent);
        title.rectTransform.offsetMin = new Vector2(SettingsLayout.TitleTextPad.x, SettingsLayout.TitleTextPad.y);
        title.rectTransform.offsetMax = new Vector2(SettingsLayout.TitleTextPad.z, SettingsLayout.TitleTextPad.w);

        // --- GAMEPLAY ----------------------------------------------------------
        Section(window, SettingsLayout.SectionGameplayY, "settings.section.gameplay", LugarithmUiSkin.IconControls);
        SegmentedSelector driveMode = SelectorRow(window, SettingsLayout.RowDriveModeY, "settings.drivemode",
                                                  new[] { "opt.manual", "opt.automation" }, SettingsLayout.SegWideWidth);
        SegmentedSelector coding    = SelectorRow(window, SettingsLayout.RowCodingY, "settings.codinginterface",
                                                  new[] { "opt.blocks", "opt.code" }, SettingsLayout.SegWideWidth);
        Divider(window, SettingsLayout.DividerGameplayY);

        // --- CONTROLS ----------------------------------------------------------
        Section(window, SettingsLayout.SectionControlsY, "settings.section.controls", LugarithmUiSkin.IconSteering);
        SegmentedSelector brake = SelectorRow(window, SettingsLayout.RowBrakeY, "settings.spacebrake",
                                              new[] { "opt.hold", "opt.toggle" }, SettingsLayout.SegWideWidth);
        Divider(window, SettingsLayout.DividerControlsY);

        // --- AUDIO -------------------------------------------------------------
        Section(window, SettingsLayout.SectionAudioY, "settings.section.audio", LugarithmUiSkin.IconAudio);
        Slider music = SliderRow(window, SettingsLayout.RowMusicY, "settings.musicvolume");
        Slider sfx   = SliderRow(window, SettingsLayout.RowSfxY, "settings.sfxvolume");
        Divider(window, SettingsLayout.DividerAudioY);

        // --- LANGUAGE & TEXT ---------------------------------------------------
        Section(window, SettingsLayout.SectionLanguageY, "settings.section.languagetext", LugarithmUiSkin.IconDialogue);
        SegmentedSelector language  = SelectorRow(window, SettingsLayout.RowLanguageY, "settings.language",
                                                  new[] { "opt.english", "opt.filipino" }, SettingsLayout.SegWideWidth);
        SegmentedSelector subtitles = SelectorRow(window, SettingsLayout.RowSubtitlesY, "settings.subtitles",
                                                  new[] { "opt.on", "opt.off" }, SettingsLayout.SegMediumWidth);
        SegmentedSelector dlgSpeed  = SelectorRow(window, SettingsLayout.RowDialogueSpeedY, "settings.dialoguespeed",
                                                  new[] { "opt.speed.slow", "opt.speed.normal",
                                                          "opt.speed.fast", "opt.speed.instant" },
                                                  SettingsLayout.SegCompactWidth, SettingsLayout.SegCompactFont);
        Divider(window, SettingsLayout.DividerLanguageY);

        // --- APPEARANCE --------------------------------------------------------
        Section(window, SettingsLayout.SectionAppearanceY, "settings.section.appearance", LugarithmUiSkin.IconCode);
        Button themeButton = BuildThemeRow(window, SettingsLayout.RowThemeY, out TextMeshProUGUI themeValue);

        // --- Close -------------------------------------------------------------
        // A real, nine-sliced button so "CLOSE" reads as a control (not stray text)
        // and sits inside the frame. Sliced scales the primary sprite cleanly to the
        // wide footprint.
        Button close = UIFactory.CreateButton(window, "CloseButton", "Close", SettingsLayout.CloseSize, SettingsLayout.CloseFont);
        close.image.sprite = LugarithmUiSkin.ButtonPrimary;
        close.image.type = Image.Type.Sliced;
        close.image.color = Color.white;
        UIFactory.LocalizeButton(close, "common.close");
        var closeLabel = close.GetComponentInChildren<TextMeshProUGUI>();
        if (closeLabel != null)
        {
            closeLabel.color = SettingsLayout.CloseLabelColor;
            closeLabel.alignment = TextAlignmentOptions.Center;
        }
        UIFactory.Place(close, new Vector2(0.5f, 0f), SettingsLayout.CloseOffset, SettingsLayout.CloseSize);

        // --- Wire + hide -------------------------------------------------------
        SceneBuilderUtil.Wire(panel, "root",                  overlay.gameObject);
        SceneBuilderUtil.Wire(panel, "closeButton",           close);
        SceneBuilderUtil.Wire(panel, "driveModeSelector",     driveMode);
        SceneBuilderUtil.Wire(panel, "codingSelector",        coding);
        SceneBuilderUtil.Wire(panel, "brakeSelector",         brake);
        SceneBuilderUtil.Wire(panel, "musicSlider",           music);
        SceneBuilderUtil.Wire(panel, "sfxSlider",             sfx);
        SceneBuilderUtil.Wire(panel, "languageSelector",      language);
        SceneBuilderUtil.Wire(panel, "subtitlesSelector",     subtitles);
        SceneBuilderUtil.Wire(panel, "dialogueSpeedSelector", dlgSpeed);
        SceneBuilderUtil.Wire(panel, "themeButton",           themeButton);
        SceneBuilderUtil.Wire(panel, "themeLabel",            themeValue);

        UIFactory.ApplyBlueprintSkin(overlay);
        overlay.gameObject.SetActive(false);
        return panel;
    }

    // -------------------------------------------------------------------------

    static void Section(RectTransform window, float y, string key, Sprite icon)
    {
        var iconFrame = UIFactory.CreatePanel(window, ShortName(key) + "IconFrame",
                                              new Vector2(0f, 1f), new Vector2(0f, 1f), Color.white);
        var iconFrameImage = iconFrame.GetComponent<Image>();
        iconFrameImage.sprite = SettingsControlSprite(ShortName(key) == "gameplay" ? "icon_gameplay" :
                                                     ShortName(key) == "controls" ? "icon_controls" :
                                                     ShortName(key) == "audio" ? "icon_audio" :
                                                     ShortName(key) == "languagetext" ? "icon_dialogue" : "icon_code");
        iconFrameImage.type = Image.Type.Simple;
        iconFrameImage.preserveAspect = true;
        UIFactory.Place(iconFrame, new Vector2(0f, 1f), new Vector2(SettingsLayout.IconColumnX, y + 4f),
                        new Vector2(SettingsLayout.IconSize, SettingsLayout.IconSize));
        var iconRect = UIFactory.CreateRect(iconFrame, "Icon", Vector2.zero, Vector2.one,
                                            new Vector2(SettingsLayout.IconInset, SettingsLayout.IconInset),
                                            new Vector2(-SettingsLayout.IconInset, -SettingsLayout.IconInset));
        var iconImage = iconRect.gameObject.AddComponent<Image>();
        iconImage.sprite = null;
        iconImage.color = Color.clear;
        iconImage.raycastTarget = false;

        var header = UIFactory.CreateLocalizedText(window, ShortName(key) + "Header", key, SettingsLayout.HeadingFont,
                                                   UIFactory.Accent, TextAlignmentOptions.MidlineLeft);
        UIFactory.Place(header, new Vector2(0f, 1f), new Vector2(SettingsLayout.HeadingX, y), SettingsLayout.HeadingSize);
    }

    static SegmentedSelector SelectorRow(RectTransform window, float y, string labelKey,
                                         string[] optionKeys, float segWidth,
                                         float fontSize = SettingsLayout.SegDefaultFont)
    {
        RowLabel(window, y, labelKey);

        string[] options = new string[optionKeys.Length];
        for (int i = 0; i < optionKeys.Length; i++)
            options[i] = LocalizationTable.Get(optionKeys[i], GameLanguage.English);

        SegmentedSelector sel = UIFactory.CreateSegmentedSelector(
            window, ShortName(labelKey) + "Selector", options, segWidth, SettingsLayout.SegH,
            SettingsLayout.SegGap, fontSize, optionKeys);
        ((RectTransform)sel.transform).anchoredPosition = new Vector2(SettingsLayout.CtrlX, y - 2f);
        foreach (Button button in sel.GetComponentsInChildren<Button>(true))
        {
            button.image.sprite = SettingsControlSprite("selector_normal");
            button.image.type = Image.Type.Simple;
            button.image.preserveAspect = false;
            button.image.color = Color.white;
        }
        SceneBuilderUtil.Wire(sel, "activeSprite", SettingsControlSprite("selector_selected"));
        SceneBuilderUtil.Wire(sel, "idleSprite", SettingsControlSprite("selector_normal"));
        return sel;
    }

    static Slider SliderRow(RectTransform window, float y, string labelKey)
    {
        RowLabel(window, y, labelKey);
        Slider slider = UIFactory.CreateSlider(window, ShortName(labelKey) + "Slider", SettingsLayout.SliderSize);
        UIFactory.Place(slider, new Vector2(0f, 1f), new Vector2(SettingsLayout.CtrlX, y - 4f), SettingsLayout.SliderSize);
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.interactable = true;
        return slider;
    }

    static Button BuildThemeRow(RectTransform window, float y, out TextMeshProUGUI valueLabel)
    {
        RowLabel(window, y, "settings.codetheme");

        Button button = UIFactory.CreateButton(window, "ThemeButton", "Cycle", SettingsLayout.ThemeButtonSize, SettingsLayout.ThemeButtonFont);
        // Match the other controls' purple pill instead of the default dark button face.
        button.image.sprite = SettingsControlSprite("selector_normal");
        button.image.type = Image.Type.Simple;
        button.image.color = Color.white;
        UIFactory.LocalizeButton(button, "settings.theme.cycle");
        UIFactory.Place(button, new Vector2(0f, 1f), new Vector2(SettingsLayout.CtrlX, y - 2f), SettingsLayout.ThemeButtonSize);

        // Value is set at runtime from the theme name (not a fixed UI string), so it
        // is not localized.
        valueLabel = UIFactory.CreateText(window, "ThemeValue", "", SettingsLayout.ThemeValueFont,
                                          UIFactory.Accent, TextAlignmentOptions.MidlineLeft);
        UIFactory.Place(valueLabel, new Vector2(0f, 1f), new Vector2(SettingsLayout.CtrlX + SettingsLayout.ThemeValueGap, y), SettingsLayout.ThemeValueSize);
        return button;
    }

    static void RowLabel(RectTransform window, float y, string labelKey)
    {
        var rowLabel = UIFactory.CreateLocalizedText(window, ShortName(labelKey) + "Label", labelKey, SettingsLayout.LabelFont,
                                                     UIFactory.TextBright, TextAlignmentOptions.MidlineLeft);
        UIFactory.Place(rowLabel, new Vector2(0f, 1f), new Vector2(SettingsLayout.LabelX, y), SettingsLayout.LabelSize);
    }

    // Last dot-segment of a key, for readable GameObject names.
    static string ShortName(string key)
    {
        int dot = key.LastIndexOf('.');
        return dot >= 0 && dot < key.Length - 1 ? key.Substring(dot + 1) : key;
    }

    static void Divider(RectTransform window, float y)
    {
        var rule = UIFactory.CreatePanel(window, "SectionDivider", new Vector2(0f, 1f),
                                         new Vector2(0f, 1f), SettingsLayout.DividerColor);
        UIFactory.Place(rule, new Vector2(0f, 1f), new Vector2(SettingsLayout.DividerX, y),
                        new Vector2(SettingsLayout.DividerWidth, SettingsLayout.DividerThick));
        rule.GetComponent<Image>().raycastTarget = false;
    }

    static Sprite SettingsControlSprite(string name)
    {
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
            $"Assets/UI/Sprites/LugarithmUi/Settings/Controls/{name}.png");
    }
}
