using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Drives the Main Menu screen.
/// Wires up New Game / Continue / Settings / Quit buttons and shows/hides the
/// settings panel. Settings are read from / written to <see cref="SettingsManager"/>
/// (applied live), and scene loads route through <see cref="SceneTransitionManager"/>.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector References

    [Header("Buttons")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    [Header("Settings Panel")]
    [SerializeField] private GameObject settingsPanel;

    [Header("Settings Controls")]
    [SerializeField] private Toggle   blockModeToggle;   // ON = Block Mode, OFF = Code Mode
    [SerializeField] private Slider   musicVolumeSlider;
    [SerializeField] private Slider   sfxVolumeSlider;
    [SerializeField] private TMP_Text difficultyLabel;   // Shows "EASY — Block Mode" or "HARD — Code Mode"

    [Header("Scene Names")]
    [Tooltip("Scene loaded for a fresh run / first town leg.")]
    [SerializeField] private string firstTownSceneName = "Drive_Oton"; // placeholder
    [Tooltip("One scene per town leg, indexed by currentTownIndex (0 = Oton). " +
             "Falls back to the first town scene if empty or out of range.")]
    [SerializeField] private string[] townSceneNames;

    // -------------------------------------------------------------------------

    void Start()
    {
        // Gray out Continue if there's no active run.
        continueButton.interactable = SaveSystem.HasSave();

        // Wire buttons
        newGameButton.onClick.AddListener(OnNewGame);
        continueButton.onClick.AddListener(OnContinue);
        settingsButton.onClick.AddListener(OnOpenSettings);
        quitButton.onClick.AddListener(OnQuit);

        // Settings panel starts closed
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        // Bind the settings controls to SettingsManager
        BindSettingsControls();
    }

    // -------------------------------------------------------------------------
    // Button Handlers

    void OnNewGame()
    {
        SaveSystem.StartNewRun();        // resets progress, keeps settings
        LoadScene(firstTownSceneName);
    }

    void OnContinue()
    {
        LoadScene(SceneForCurrentTown());
    }

    void OnOpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    /// <summary>Called by the Settings panel's Close button.</summary>
    public void OnCloseSettings()
    {
        // Settings persist live through SettingsManager as the controls change,
        // so closing just hides the panel.
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // -------------------------------------------------------------------------
    // Settings binding

    void BindSettingsControls()
    {
        SettingsManager settings = SettingsManager.Instance;

        if (blockModeToggle != null)
        {
            bool isBlockMode = settings != null ? settings.BlockMode : true;
            blockModeToggle.SetIsOnWithoutNotify(isBlockMode);
            UpdateDifficultyLabel(isBlockMode);

            blockModeToggle.onValueChanged.AddListener(isOn =>
            {
                if (SettingsManager.Instance != null)
                    SettingsManager.Instance.BlockMode = isOn;
                UpdateDifficultyLabel(isOn);
            });
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.SetValueWithoutNotify(settings != null ? settings.MusicVolume : 0.8f);
            musicVolumeSlider.onValueChanged.AddListener(v =>
            {
                if (SettingsManager.Instance != null)
                    SettingsManager.Instance.MusicVolume = v;
            });
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.SetValueWithoutNotify(settings != null ? settings.SfxVolume : 0.8f);
            sfxVolumeSlider.onValueChanged.AddListener(v =>
            {
                if (SettingsManager.Instance != null)
                    SettingsManager.Instance.SfxVolume = v;
            });
        }
    }

    void UpdateDifficultyLabel(bool isBlockMode)
    {
        if (difficultyLabel == null) return;
        difficultyLabel.text = isBlockMode
            ? "EASY  —  Block Mode"
            : "HARD  —  Code Mode";
    }

    // -------------------------------------------------------------------------
    // Scene routing

    string SceneForCurrentTown()
    {
        int index = SaveSystem.Current.currentTownIndex;
        if (townSceneNames != null && index >= 0 && index < townSceneNames.Length
            && !string.IsNullOrEmpty(townSceneNames[index]))
        {
            return townSceneNames[index];
        }

        // Fallback until per-town scenes are authored.
        return firstTownSceneName;
    }

    void LoadScene(string sceneName)
    {
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.TransitionTo(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }
}
