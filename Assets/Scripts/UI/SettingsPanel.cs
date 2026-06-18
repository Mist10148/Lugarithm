using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Reusable settings panel (used on Main Menu and Level Select). Only two
/// controls are live this phase — Gameplay Mode (Manual/Automation) and
/// Difficulty (Block/Code); every other row is a visual placeholder. Values
/// bind through <see cref="SettingsManager"/> when present, falling back to
/// the loaded save so the panel still works when a scene is played directly
/// in the editor without the Bootstrap managers.
/// Place this component on an always-active object; <see cref="root"/> is the
/// panel that gets shown/hidden.
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button     closeButton;

    [Header("Gameplay Mode")]
    [SerializeField] private Toggle   manualModeToggle;   // ON = Manual, OFF = Automation
    [SerializeField] private TMP_Text gameplayModeLabel;

    [Header("Difficulty")]
    [SerializeField] private Toggle   blockModeToggle;    // ON = Block (easy), OFF = Code (hard)
    [SerializeField] private TMP_Text difficultyLabel;

    [Header("Brake Mode")]
    [SerializeField] private Toggle   brakeModeToggle;    // ON = Toggle, OFF = Hold
    [SerializeField] private TMP_Text brakeModeLabel;

    [Header("Code Theme")]
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

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (manualModeToggle != null)
        {
            manualModeToggle.onValueChanged.AddListener(isOn =>
            {
                if (SettingsManager.Instance != null)
                    SettingsManager.Instance.ManualMode = isOn;
                else { S.manualMode = isOn; SaveSystem.Save(); }
                UpdateLabels();
            });
        }

        if (blockModeToggle != null)
        {
            blockModeToggle.onValueChanged.AddListener(isOn =>
            {
                if (SettingsManager.Instance != null)
                    SettingsManager.Instance.BlockMode = isOn;
                else { S.blockMode = isOn; SaveSystem.Save(); }
                UpdateLabels();
            });
        }

        if (brakeModeToggle != null)
        {
            brakeModeToggle.onValueChanged.AddListener(isOn =>
            {
                BrakeMode mode = isOn ? BrakeMode.Toggle : BrakeMode.Hold;
                if (SettingsManager.Instance != null)
                    SettingsManager.Instance.BrakeMode = mode;
                else { S.brakeMode = (int)mode; SaveSystem.Save(); }
                UpdateLabels();
            });
        }

        if (themeButton != null)
        {
            themeButton.onClick.AddListener(CycleTheme);
        }
    }

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
        {
            if (SettingsManager.Instance != null)
                SettingsManager.Instance.CodeThemeId = next;
            else
            {
                S.codeThemeId = next;
                SaveSystem.Save();
            }
        }
        UpdateLabels();
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

    void Refresh()
    {
        if (manualModeToggle != null) manualModeToggle.SetIsOnWithoutNotify(S.manualMode);
        if (blockModeToggle  != null) blockModeToggle.SetIsOnWithoutNotify(S.blockMode);
        if (brakeModeToggle  != null)
            brakeModeToggle.SetIsOnWithoutNotify(S.brakeMode == (int)BrakeMode.Toggle);
        UpdateLabels();
    }

    public void RefreshTheme() => UpdateLabels();

    void UpdateLabels()
    {
        if (gameplayModeLabel != null)
            gameplayModeLabel.text = S.manualMode
                ? "MANUAL  —  drive it yourself"
                : "AUTOMATION  —  program the jeepney";

        if (difficultyLabel != null)
            difficultyLabel.text = S.blockMode
                ? "EASY  —  Block Mode"
                : "HARD  —  Code Mode";

        if (brakeModeLabel != null)
            brakeModeLabel.text = S.brakeMode == (int)BrakeMode.Toggle
                ? "TOGGLE  —  tap Space to brake / release"
                : "HOLD  —  brake while Space is held";

        if (themeLabel != null)
        {
            CodeTheme theme = CodeThemeLibrary.Get(S.codeThemeId);
            string locked = SaveSystem.Current.HasTheme(theme.id)
                ? ""
                : $"  —  ₱{theme.cost} (tap to buy)";
            themeLabel.text = $"{theme.name.ToUpper()}{locked}";
        }
    }
}
