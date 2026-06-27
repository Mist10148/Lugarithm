using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Reusable settings panel (used on Main Menu and Level Select), organized into
/// sections: Gameplay, Controls, Audio, Language &amp; Text, and Appearance.
/// Either/or settings use <see cref="SegmentedSelector"/> pills (both labels
/// visible, active one highlighted) instead of ambiguous toggles. Values bind
/// through <see cref="SettingsManager"/> when present, falling back to the loaded
/// save so the panel still works when a scene is played directly in the editor.
/// Place this component on an always-active object; <see cref="root"/> is the
/// panel that gets shown/hidden.
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button     closeButton;

    [Header("Gameplay")]
    [SerializeField] private SegmentedSelector driveModeSelector;  // 0 = Manual, 1 = Automation
    [SerializeField] private SegmentedSelector codingSelector;     // 0 = Blocks, 1 = Code

    [Header("Controls")]
    [SerializeField] private SegmentedSelector brakeSelector;      // 0 = Hold, 1 = Toggle

    [Header("Audio")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Language & Text")]
    [SerializeField] private SegmentedSelector languageSelector;   // 0 = English, 1 = Filipino
    [SerializeField] private SegmentedSelector subtitlesSelector;  // 0 = On, 1 = Off
    [SerializeField] private SegmentedSelector dialogueSpeedSelector; // 0..3 Slow/Normal/Fast/Instant

    [Header("Appearance")]
    [SerializeField] private Button   themeButton;
    [SerializeField] private TMP_Text themeLabel;

    private bool _bound;

    // Shorthand for the settings block inside the loaded save.
    private static GameSettings S => SaveSystem.Current.settings;

    // -------------------------------------------------------------------------

    void Start()
    {
        Bind();
        if (root != null) root.SetActive(false);
    }

    // -------------------------------------------------------------------------
    // Public API

    public void Open()
    {
        Bind();
        Refresh();
        if (root != null) root.SetActive(true);
    }

    public void Close()
    {
        if (root != null) root.SetActive(false);
    }

    // -------------------------------------------------------------------------

    void Bind()
    {
        if (_bound) return;
        _bound = true;

        if (closeButton != null) closeButton.onClick.AddListener(Close);

        if (driveModeSelector != null)
            driveModeSelector.OnValueChanged += i =>
                Set(s => s.manualMode = (i == 0), m => m.ManualMode = (i == 0));

        if (codingSelector != null)
            codingSelector.OnValueChanged += i => Set(s => s.blockMode = (i == 0), m => m.BlockMode = (i == 0));

        if (brakeSelector != null)
            brakeSelector.OnValueChanged += i =>
            {
                BrakeMode mode = i == 0 ? BrakeMode.Hold : BrakeMode.Toggle;
                Set(s => s.brakeMode = (int)mode, m => m.BrakeMode = mode);
            };

        if (musicSlider != null)
            musicSlider.onValueChanged.AddListener(v =>
                Set(s => s.musicVolume = v, m => m.MusicVolume = v));

        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(v =>
                Set(s => s.sfxVolume = v, m => m.SfxVolume = v));

        if (languageSelector != null)
            languageSelector.OnValueChanged += i =>
            {
                GameLanguage lang = (GameLanguage)i;
                Set(s => s.language = (int)lang, m => m.Language = lang);
            };

        if (subtitlesSelector != null)
            subtitlesSelector.OnValueChanged += i =>
                Set(s => s.subtitles = (i == 0), m => m.Subtitles = (i == 0));

        if (dialogueSpeedSelector != null)
            dialogueSpeedSelector.OnValueChanged += i =>
            {
                DialogueSpeed speed = (DialogueSpeed)i;
                Set(s => s.dialogueSpeed = (int)speed, m => m.DialogueSpeed = speed);
            };

        if (themeButton != null) themeButton.onClick.AddListener(CycleTheme);
    }

    // A tiny indirection so every setter goes through SettingsManager when it
    // exists (applies live + persists) and falls back to a direct save otherwise.
    void Set(System.Action<GameSettings> toSave, System.Action<SettingsManager> toManager)
    {
        if (SettingsManager.Instance != null) toManager(SettingsManager.Instance);
        else { toSave(S); SaveSystem.Save(); }
        UpdateThemeLabel();
    }

    // -------------------------------------------------------------------------
    // Code theme (kept as a cycle button — themes are unlockable purchases)

    void CycleTheme()
    {
        int current = S.codeThemeId;
        int next = current;
        for (int i = 1; i <= CodeThemeLibrary.Count; i++)
        {
            int candidate = (current + i) % CodeThemeLibrary.Count;
            if (SaveSystem.Current.HasTheme(candidate) || TryBuyTheme(candidate))
            {
                next = candidate;
                break;
            }
        }

        if (next != current)
            Set(s => s.codeThemeId = next, m => m.CodeThemeId = next);
        UpdateThemeLabel();
    }

    bool TryBuyTheme(int themeId)
    {
        if (SettingsManager.Instance != null) return SettingsManager.Instance.TryBuyTheme(themeId);

        CodeTheme theme = CodeThemeLibrary.Get(themeId);
        if (SaveSystem.Current.HasTheme(themeId)) return true;
        if (SaveSystem.Current.currency < theme.cost) return false;

        SaveSystem.Current.currency -= theme.cost;
        SaveSystem.Current.UnlockTheme(themeId);
        SaveSystem.Save();
        return true;
    }

    // -------------------------------------------------------------------------

    void Refresh()
    {
        if (driveModeSelector     != null) driveModeSelector.SetValueWithoutNotify(S.manualMode ? 0 : 1);
        if (codingSelector        != null) codingSelector.SetValueWithoutNotify(S.blockMode ? 0 : 1);
        if (brakeSelector         != null) brakeSelector.SetValueWithoutNotify(S.brakeMode == (int)BrakeMode.Toggle ? 1 : 0);
        if (musicSlider           != null) musicSlider.SetValueWithoutNotify(S.musicVolume);
        if (sfxSlider             != null) sfxSlider.SetValueWithoutNotify(S.sfxVolume);
        if (languageSelector      != null) languageSelector.SetValueWithoutNotify(Mathf.Clamp(S.language, 0, 1));
        if (subtitlesSelector     != null) subtitlesSelector.SetValueWithoutNotify(S.subtitles ? 0 : 1);
        if (dialogueSpeedSelector != null) dialogueSpeedSelector.SetValueWithoutNotify(Mathf.Clamp(S.dialogueSpeed, 0, 3));
        UpdateThemeLabel();
    }

    public void RefreshTheme() => UpdateThemeLabel();

    void UpdateThemeLabel()
    {
        if (themeLabel == null) return;
        CodeTheme theme = CodeThemeLibrary.Get(S.codeThemeId);
        string locked = SaveSystem.Current.HasTheme(theme.id) ? "" : $"  —  ₱{theme.cost} (tap to buy)";
        themeLabel.text = $"{theme.name.ToUpper()}{locked}";
    }
}
