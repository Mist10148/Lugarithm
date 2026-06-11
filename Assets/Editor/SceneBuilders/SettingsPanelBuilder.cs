using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the shared Settings panel subtree (used by the MainMenu and
/// LevelSelect builders) and wires the <see cref="SettingsPanel"/> component.
/// Two functional rows (Gameplay Mode, Difficulty); the rest are visibly
/// disabled placeholders per the current phase scope.
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
        UIFactory.Place(window, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 660f));

        var title = UIFactory.CreateText(window, "Title", "SETTINGS", 44f, UIFactory.Accent);
        UIFactory.Place(title, new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(700f, 60f));

        // --- Functional rows ---------------------------------------------------

        Toggle manualToggle = BuildFunctionalRow(window, -110f, "Gameplay Mode",
                                                 out TextMeshProUGUI gameplayValue);
        Toggle blockToggle  = BuildFunctionalRow(window, -180f, "Difficulty",
                                                 out TextMeshProUGUI difficultyValue);

        var divider = UIFactory.CreatePanel(window, "Divider",
                                            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                            new Color(1f, 1f, 1f, 0.12f));
        UIFactory.Place(divider, new Vector2(0.5f, 1f), new Vector2(0f, -232f), new Vector2(690f, 2f));

        // --- Placeholder rows ---------------------------------------------------

        BuildPlaceholderSliderRow(window, -270f, "Music Volume");
        BuildPlaceholderSliderRow(window, -330f, "SFX Volume");
        BuildPlaceholderTextRow(window,  -390f, "Dialogue Speed", "Normal");
        BuildPlaceholderToggleRow(window, -450f, "Subtitles");

        // --- Close ---------------------------------------------------------------

        Button close = UIFactory.CreateButton(window, "CloseButton", "Close", new Vector2(220f, 54f));
        UIFactory.Place(close, new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(220f, 54f));

        // --- Wire + hide ----------------------------------------------------------

        SceneBuilderUtil.Wire(panel, "root",              overlay.gameObject);
        SceneBuilderUtil.Wire(panel, "closeButton",       close);
        SceneBuilderUtil.Wire(panel, "manualModeToggle",  manualToggle);
        SceneBuilderUtil.Wire(panel, "gameplayModeLabel", gameplayValue);
        SceneBuilderUtil.Wire(panel, "blockModeToggle",   blockToggle);
        SceneBuilderUtil.Wire(panel, "difficultyLabel",   difficultyValue);

        overlay.gameObject.SetActive(false);
        return panel;
    }

    // -------------------------------------------------------------------------

    static Toggle BuildFunctionalRow(RectTransform window, float y, string label,
                                     out TextMeshProUGUI valueLabel)
    {
        var rowLabel = UIFactory.CreateText(window, label + "Label", label, 27f,
                                            UIFactory.TextBright, TextAlignmentOptions.MidlineLeft);
        UIFactory.Place(rowLabel, new Vector2(0f, 1f), new Vector2(40f, y), new Vector2(260f, 50f));

        Toggle toggle = UIFactory.CreateToggle(window, label + "Toggle", new Vector2(50f, 50f));
        UIFactory.Place(toggle, new Vector2(0f, 1f), new Vector2(320f, y), new Vector2(50f, 50f));

        valueLabel = UIFactory.CreateText(window, label + "Value", "", 24f,
                                          UIFactory.Accent, TextAlignmentOptions.MidlineLeft);
        UIFactory.Place(valueLabel, new Vector2(0f, 1f), new Vector2(390f, y), new Vector2(340f, 50f));

        return toggle;
    }

    static void BuildPlaceholderSliderRow(RectTransform window, float y, string label)
    {
        AddPlaceholderLabel(window, y, label);

        Slider slider = UIFactory.CreateSlider(window, label + "Slider", new Vector2(280f, 36f));
        UIFactory.Place(slider, new Vector2(0f, 1f), new Vector2(320f, y - 7f), new Vector2(280f, 36f));
        slider.interactable = false;

        AddComingSoon(window, y);
    }

    static void BuildPlaceholderTextRow(RectTransform window, float y, string label, string value)
    {
        AddPlaceholderLabel(window, y, label);

        var valueText = UIFactory.CreateText(window, label + "Value", value, 24f,
                                             UIFactory.TextDim, TextAlignmentOptions.MidlineLeft);
        UIFactory.Place(valueText, new Vector2(0f, 1f), new Vector2(330f, y), new Vector2(260f, 50f));

        AddComingSoon(window, y);
    }

    static void BuildPlaceholderToggleRow(RectTransform window, float y, string label)
    {
        AddPlaceholderLabel(window, y, label);

        Toggle toggle = UIFactory.CreateToggle(window, label + "Toggle", new Vector2(44f, 44f));
        UIFactory.Place(toggle, new Vector2(0f, 1f), new Vector2(325f, y - 3f), new Vector2(44f, 44f));
        toggle.interactable = false;

        AddComingSoon(window, y);
    }

    static void AddPlaceholderLabel(RectTransform window, float y, string label)
    {
        var text = UIFactory.CreateText(window, label + "Label", label, 25f,
                                        UIFactory.TextDim, TextAlignmentOptions.MidlineLeft);
        UIFactory.Place(text, new Vector2(0f, 1f), new Vector2(40f, y), new Vector2(260f, 50f));
    }

    static void AddComingSoon(RectTransform window, float y)
    {
        var note = UIFactory.CreateText(window, "ComingSoon" + y, "(coming soon)", 20f,
                                        UIFactory.TextDim, TextAlignmentOptions.MidlineLeft);
        UIFactory.Place(note, new Vector2(0f, 1f), new Vector2(620f, y), new Vector2(140f, 50f));
    }
}
