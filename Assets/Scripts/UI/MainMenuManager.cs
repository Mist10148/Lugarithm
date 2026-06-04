using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Drives the Main Menu screen.
/// Wires up New Game / Continue / Settings / Quit buttons
/// and shows/hides the settings panel.
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
    [SerializeField] private string firstTownSceneName = "Drive_Oton"; // placeholder

    // -------------------------------------------------------------------------
    // Prefs keys (keep in sync with SettingsManager when that exists)
    private const string PREF_BLOCK_MODE     = "pref_block_mode";
    private const string PREF_MUSIC_VOLUME   = "pref_music_vol";
    private const string PREF_SFX_VOLUME     = "pref_sfx_vol";

    // -------------------------------------------------------------------------

    void Start()
    {
        // Gray out Continue if there's no save
        continueButton.interactable = SaveSystem.HasSave();

        // Wire buttons
        newGameButton.onClick.AddListener(OnNewGame);
        continueButton.onClick.AddListener(OnContinue);
        settingsButton.onClick.AddListener(OnOpenSettings);
        quitButton.onClick.AddListener(OnQuit);

        // Settings panel starts closed
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        // Load saved settings into the settings panel controls
        LoadSettingsIntoUI();
    }

    // -------------------------------------------------------------------------
    // Button Handlers

    void OnNewGame()
    {
        SaveSystem.ClearSave();
        SceneManager.LoadScene(firstTownSceneName);
    }

    void OnContinue()
    {
        // TODO: Read save file and route to correct scene/town
        SceneManager.LoadScene(firstTownSceneName);
    }

    void OnOpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    /// <summary>Called by the Settings panel's Close button.</summary>
    public void OnCloseSettings()
    {
        SaveSettingsFromUI();
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
    // Settings helpers

    void LoadSettingsIntoUI()
    {
        if (blockModeToggle != null)
        {
            bool isBlockMode = PlayerPrefs.GetInt(PREF_BLOCK_MODE, 1) == 1;
            blockModeToggle.isOn = isBlockMode;
            UpdateDifficultyLabel(isBlockMode);

            // Update label whenever the toggle changes
            blockModeToggle.onValueChanged.AddListener(isOn =>
            {
                UpdateDifficultyLabel(isOn);
            });
        }

        if (musicVolumeSlider != null)
            musicVolumeSlider.value = PlayerPrefs.GetFloat(PREF_MUSIC_VOLUME, 0.8f);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = PlayerPrefs.GetFloat(PREF_SFX_VOLUME, 0.8f);
    }

    void SaveSettingsFromUI()
    {
        if (blockModeToggle != null)
            PlayerPrefs.SetInt(PREF_BLOCK_MODE, blockModeToggle.isOn ? 1 : 0);

        if (musicVolumeSlider != null)
            PlayerPrefs.SetFloat(PREF_MUSIC_VOLUME, musicVolumeSlider.value);

        if (sfxVolumeSlider != null)
            PlayerPrefs.SetFloat(PREF_SFX_VOLUME, sfxVolumeSlider.value);

        PlayerPrefs.Save();
    }

    void UpdateDifficultyLabel(bool isBlockMode)
    {
        if (difficultyLabel == null) return;
        difficultyLabel.text = isBlockMode
            ? "EASY  —  Block Mode"
            : "HARD  —  Code Mode";
    }
}
