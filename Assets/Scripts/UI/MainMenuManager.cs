using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;

/// <summary>
/// Drives the Main Menu screen.
/// New Game starts a fresh run (keeping settings) and Continue resumes one —
/// both land on the Level Select screen. Settings opens the single universal
/// overlay via <see cref="UniversalSettingsManager"/>. Scene loads route through
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
    [SerializeField] private Button journalButton;
    [SerializeField] private Button quitButton;

    [Header("Scene Names")]
    [SerializeField] private string levelSelectSceneName = "LevelSelect";

    // -------------------------------------------------------------------------

    void OnEnable()
    {
        ConfigureButtons();
    }

    void Start()
    {
        ConfigureButtons();
    }

    void ConfigureButtons()
    {
        // Gray out Continue if there's no active run.
        if (continueButton != null)
            continueButton.interactable = SaveSystem.HasSave();

        WireMenuButton(newGameButton, OnNewGame);
        WireMenuButton(continueButton, OnContinue);
        WireMenuButton(settingsButton, OnOpenSettings);
        WireMenuButton(journalButton, OnOpenJournal);
        WireMenuButton(quitButton, OnQuit);
    }

    void WireMenuButton(Button button, Action action)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => PlayMenuButton(button, action));
    }

    void PlayMenuButton(Button button, Action action)
    {
        if (button == null)
            return;

        var flash = button.GetComponent<MenuButtonPressFlash>();
        if (flash != null)
            flash.Play(action);
        else
            action?.Invoke();
    }

    // -------------------------------------------------------------------------
    // Button Handlers

    public void OnNewGame()
    {
        SaveSystem.StartNewRun();        // resets progress, keeps settings
        LoadScene(levelSelectSceneName);
    }

    public void OnContinue()
    {
        LoadScene(levelSelectSceneName);
    }

    public void OnOpenSettings()
    {
        UniversalSettingsManager.Ensure()?.Open();
    }

    public void OnOpenJournal()
    {
        AlmanacManager.Instance?.Open();
    }

    public void OnQuit()
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
