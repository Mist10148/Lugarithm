using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Drives the Main Menu screen.
/// New Game starts a fresh run (keeping settings) and Continue resumes one —
/// both land on the Level Select screen. Settings opens the shared
/// <see cref="SettingsPanel"/>. Scene loads route through
/// <see cref="SceneTransitionManager"/>.
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

    [Header("Panels")]
    [SerializeField] private SettingsPanel settingsPanel;

    [Header("Scene Names")]
    [SerializeField] private string levelSelectSceneName = "LevelSelect";

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
    }

    // -------------------------------------------------------------------------
    // Button Handlers

    void OnNewGame()
    {
        SaveSystem.StartNewRun();        // resets progress, keeps settings
        LoadScene(levelSelectSceneName);
    }

    void OnContinue()
    {
        LoadScene(levelSelectSceneName);
    }

    void OnOpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.Open();
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
    // Scene routing

    void LoadScene(string sceneName)
    {
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.TransitionTo(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }
}
