using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the shared Settings panel subtree (used by the MainMenu and
/// LevelSelect builders) and wires the <see cref="SettingsPanel"/> component.
/// Organized into sections — Gameplay, Controls, Audio, Language &amp; Text,
/// Appearance — with <see cref="SegmentedSelector"/> pills for either/or settings
/// (both labels visible, active highlighted) instead of ambiguous toggles. Every
/// label/option is localized, so flipping the Language pill re-renders this panel
/// (and the rest of the UI) live.
/// </summary>
public static class SettingsPanelBuilder
{
    // Row geometry
    const float LabelX = 48f;
    const float CtrlX  = 336f;
    const float SegH   = 44f;

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
        UIFactory.Place(window, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 920f));

        var title = UIFactory.CreateLocalizedText(window, "Title", "settings.title", 42f, UIFactory.Accent);
        UIFactory.Place(title, new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(780f, 56f));

        // --- GAMEPLAY ----------------------------------------------------------
        Section(window, -92f, "settings.section.gameplay");
        SegmentedSelector driveMode = SelectorRow(window, -132f, "settings.drivemode",
                                                  new[] { "opt.manual", "opt.automation" }, 152f);
        SegmentedSelector coding    = SelectorRow(window, -190f, "settings.codinginterface",
                                                  new[] { "opt.blocks", "opt.code" }, 152f);

        // --- CONTROLS ----------------------------------------------------------
        Section(window, -252f, "settings.section.controls");
        SegmentedSelector brake = SelectorRow(window, -292f, "settings.spacebrake",
                                              new[] { "opt.hold", "opt.toggle" }, 152f);

        // --- AUDIO -------------------------------------------------------------
        Section(window, -354f, "settings.section.audio");
        Slider music = SliderRow(window, -394f, "settings.musicvolume");
        Slider sfx   = SliderRow(window, -442f, "settings.sfxvolume");

        // --- LANGUAGE & TEXT ---------------------------------------------------
        Section(window, -504f, "settings.section.languagetext");
        SegmentedSelector language  = SelectorRow(window, -544f, "settings.language",
                                                  new[] { "opt.english", "opt.filipino" }, 152f);
        SegmentedSelector subtitles = SelectorRow(window, -602f, "settings.subtitles",
                                                  new[] { "opt.on", "opt.off" }, 110f);
        SegmentedSelector dlgSpeed  = SelectorRow(window, -660f, "settings.dialoguespeed",
                                                  new[] { "opt.speed.slow", "opt.speed.normal",
                                                          "opt.speed.fast", "opt.speed.instant" }, 115f, 17f);

        // --- APPEARANCE --------------------------------------------------------
        Section(window, -722f, "settings.section.appearance");
        Button themeButton = BuildThemeRow(window, -762f, out TextMeshProUGUI themeValue);

        // --- Close -------------------------------------------------------------
        Button close = UIFactory.CreateButton(window, "CloseButton", "Close", new Vector2(220f, 54f));
        UIFactory.LocalizeButton(close, "common.close");
        UIFactory.Place(close, new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(220f, 54f));

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

        overlay.gameObject.SetActive(false);
        return panel;
    }

    // -------------------------------------------------------------------------

    static void Section(RectTransform window, float y, string key)
    {
        var header = UIFactory.CreateLocalizedText(window, ShortName(key) + "Header", key, 22f,
                                                   UIFactory.Accent, TextAlignmentOptions.MidlineLeft);
        UIFactory.Place(header, new Vector2(0f, 1f), new Vector2(36f, y), new Vector2(700f, 32f));

        var rule = UIFactory.CreatePanel(window, ShortName(key) + "Rule",
                                         new Vector2(0f, 1f), new Vector2(0f, 1f),
                                         new Color(1f, 1f, 1f, 0.10f));
        UIFactory.Place(rule, new Vector2(0f, 1f), new Vector2(36f, y - 30f), new Vector2(788f, 2f));
    }

    static SegmentedSelector SelectorRow(RectTransform window, float y, string labelKey,
                                         string[] optionKeys, float segWidth, float fontSize = 19f)
    {
        RowLabel(window, y, labelKey);

        string[] options = new string[optionKeys.Length];
        for (int i = 0; i < optionKeys.Length; i++)
            options[i] = LocalizationTable.Get(optionKeys[i], GameLanguage.English);

        SegmentedSelector sel = UIFactory.CreateSegmentedSelector(
            window, ShortName(labelKey) + "Selector", options, segWidth, SegH, 6f, fontSize, optionKeys);
        ((RectTransform)sel.transform).anchoredPosition = new Vector2(CtrlX, y - 2f);
        return sel;
    }

    static Slider SliderRow(RectTransform window, float y, string labelKey)
    {
        RowLabel(window, y, labelKey);
        Slider slider = UIFactory.CreateSlider(window, ShortName(labelKey) + "Slider", new Vector2(320f, 30f));
        UIFactory.Place(slider, new Vector2(0f, 1f), new Vector2(CtrlX, y - 9f), new Vector2(320f, 30f));
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.interactable = true;
        return slider;
    }

    static Button BuildThemeRow(RectTransform window, float y, out TextMeshProUGUI valueLabel)
    {
        RowLabel(window, y, "settings.codetheme");

        Button button = UIFactory.CreateButton(window, "ThemeButton", "Cycle", new Vector2(120f, 44f), 22f);
        UIFactory.LocalizeButton(button, "settings.theme.cycle");
        UIFactory.Place(button, new Vector2(0f, 1f), new Vector2(CtrlX, y - 2f), new Vector2(120f, 44f));

        // Value is set at runtime from the theme name (not a fixed UI string), so it
        // is not localized.
        valueLabel = UIFactory.CreateText(window, "ThemeValue", "", 22f,
                                          UIFactory.Accent, TextAlignmentOptions.MidlineLeft);
        UIFactory.Place(valueLabel, new Vector2(0f, 1f), new Vector2(CtrlX + 140f, y), new Vector2(380f, 50f));
        return button;
    }

    static void RowLabel(RectTransform window, float y, string labelKey)
    {
        var rowLabel = UIFactory.CreateLocalizedText(window, ShortName(labelKey) + "Label", labelKey, 25f,
                                                     UIFactory.TextBright, TextAlignmentOptions.MidlineLeft);
        UIFactory.Place(rowLabel, new Vector2(0f, 1f), new Vector2(LabelX, y), new Vector2(280f, 50f));
    }

    // Last dot-segment of a key, for readable GameObject names.
    static string ShortName(string key)
    {
        int dot = key.LastIndexOf('.');
        return dot >= 0 && dot < key.Length - 1 ? key.Substring(dot + 1) : key;
    }
}
