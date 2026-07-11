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
    const float LabelX = 88f;
    const float CtrlX  = 301f;
    const float SegH   = 30f;

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
        UIFactory.Place(window, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 665f));
        var windowImage = window.GetComponent<Image>();
        windowImage.sprite = LugarithmUiSkin.SettingsWindow;
        windowImage.type = Image.Type.Simple;
        windowImage.preserveAspect = false;
        windowImage.color = Color.white;

        var titlePlate = UIFactory.CreatePanel(window, "TitlePlate", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Color.white);
        titlePlate.GetComponent<Image>().sprite = null;
        titlePlate.GetComponent<Image>().color = Color.clear;
        UIFactory.Place(titlePlate, new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(350f, 44f));
        var title = UIFactory.CreateLocalizedText(titlePlate, "Title", "settings.title", 34f, UIFactory.Accent);
        title.rectTransform.offsetMin = new Vector2(40f, 8f);
        title.rectTransform.offsetMax = new Vector2(-40f, -8f);

        // --- GAMEPLAY ----------------------------------------------------------
        Section(window, -72f, "settings.section.gameplay", LugarithmUiSkin.IconControls);
        SegmentedSelector driveMode = SelectorRow(window, -100f, "settings.drivemode",
                                                  new[] { "opt.manual", "opt.automation" }, 138f);
        SegmentedSelector coding    = SelectorRow(window, -138f, "settings.codinginterface",
                                                  new[] { "opt.blocks", "opt.code" }, 138f);
        Divider(window, -178f);

        // --- CONTROLS ----------------------------------------------------------
        Section(window, -195f, "settings.section.controls", LugarithmUiSkin.IconSteering);
        SegmentedSelector brake = SelectorRow(window, -222f, "settings.spacebrake",
                                              new[] { "opt.hold", "opt.toggle" }, 138f);
        Divider(window, -257f);

        // --- AUDIO -------------------------------------------------------------
        Section(window, -273f, "settings.section.audio", LugarithmUiSkin.IconAudio);
        Slider music = SliderRow(window, -304f, "settings.musicvolume");
        Slider sfx   = SliderRow(window, -337f, "settings.sfxvolume");
        Divider(window, -365f);

        // --- LANGUAGE & TEXT ---------------------------------------------------
        Section(window, -382f, "settings.section.languagetext", LugarithmUiSkin.IconDialogue);
        SegmentedSelector language  = SelectorRow(window, -410f, "settings.language",
                                                  new[] { "opt.english", "opt.filipino" }, 138f);
        SegmentedSelector subtitles = SelectorRow(window, -448f, "settings.subtitles",
                                                  new[] { "opt.on", "opt.off" }, 98f);
        SegmentedSelector dlgSpeed  = SelectorRow(window, -486f, "settings.dialoguespeed",
                                                  new[] { "opt.speed.slow", "opt.speed.normal",
                                                          "opt.speed.fast", "opt.speed.instant" }, 70f, 12f);
        Divider(window, -515f);

        // --- APPEARANCE --------------------------------------------------------
        Section(window, -535f, "settings.section.appearance", LugarithmUiSkin.IconCode);
        Button themeButton = BuildThemeRow(window, -565f, out TextMeshProUGUI themeValue);

        // --- Close -------------------------------------------------------------
        Button close = UIFactory.CreateButton(window, "CloseButton", "Close", new Vector2(261f, 47f), 22f);
        close.image.sprite = null;
        close.image.color = Color.clear;
        UIFactory.LocalizeButton(close, "common.close");
        UIFactory.Place(close, new Vector2(0.5f, 0f), new Vector2(0f, 15f), new Vector2(261f, 47f));

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
        UIFactory.Place(iconFrame, new Vector2(0f, 1f), new Vector2(31f, y + 4f), new Vector2(38f, 38f));
        var iconRect = UIFactory.CreateRect(iconFrame, "Icon", Vector2.zero, Vector2.one,
                                            new Vector2(10f, 10f), new Vector2(-10f, -10f));
        var iconImage = iconRect.gameObject.AddComponent<Image>();
        iconImage.sprite = null;
        iconImage.color = Color.clear;
        iconImage.raycastTarget = false;

        var header = UIFactory.CreateLocalizedText(window, ShortName(key) + "Header", key, 22f,
                                                   UIFactory.Accent, TextAlignmentOptions.MidlineLeft);
        UIFactory.Place(header, new Vector2(0f, 1f), new Vector2(80f, y), new Vector2(550f, 24f));
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
        Slider slider = UIFactory.CreateSlider(window, ShortName(labelKey) + "Slider", new Vector2(280f, 20f));
        UIFactory.Place(slider, new Vector2(0f, 1f), new Vector2(CtrlX, y - 4f), new Vector2(280f, 20f));
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.interactable = true;
        return slider;
    }

    static Button BuildThemeRow(RectTransform window, float y, out TextMeshProUGUI valueLabel)
    {
        RowLabel(window, y, "settings.codetheme");

        Button button = UIFactory.CreateButton(window, "ThemeButton", "Cycle", new Vector2(105f, 38f), 18f);
        UIFactory.LocalizeButton(button, "settings.theme.cycle");
        UIFactory.Place(button, new Vector2(0f, 1f), new Vector2(CtrlX, y - 2f), new Vector2(105f, 38f));

        // Value is set at runtime from the theme name (not a fixed UI string), so it
        // is not localized.
        valueLabel = UIFactory.CreateText(window, "ThemeValue", "", 22f,
                                          UIFactory.Accent, TextAlignmentOptions.MidlineLeft);
        UIFactory.Place(valueLabel, new Vector2(0f, 1f), new Vector2(CtrlX + 120f, y), new Vector2(180f, 38f));
        return button;
    }

    static void RowLabel(RectTransform window, float y, string labelKey)
    {
        var rowLabel = UIFactory.CreateLocalizedText(window, ShortName(labelKey) + "Label", labelKey, 16f,
                                                     UIFactory.TextBright, TextAlignmentOptions.MidlineLeft);
        UIFactory.Place(rowLabel, new Vector2(0f, 1f), new Vector2(LabelX, y), new Vector2(190f, 30f));
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
                                         new Vector2(0f, 1f), new Color32(92, 58, 94, 255));
        UIFactory.Place(rule, new Vector2(0f, 1f), new Vector2(28f, y), new Vector2(644f, 2f));
        rule.GetComponent<Image>().raycastTarget = false;
    }

    static Sprite SettingsControlSprite(string name)
    {
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
            $"Assets/UI/Sprites/LugarithmUi/Settings/Controls/{name}.png");
    }
}
