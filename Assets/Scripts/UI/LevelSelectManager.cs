using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Drives the Level Select screen: Tutorial + the five towns. Lock state and
/// best scores come from the save; clicking an unlocked level launches it in
/// the gameplay mode chosen in Settings (Manual or Automation).
/// </summary>
public class LevelSelectManager : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector References

    [Header("Entries (index 0 = Tutorial ... 5 = San Joaquin)")]
    [SerializeField] private LevelSelectEntry[] entries;

    [Header("Buttons")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button settingsButton;

    [Header("Wallet")]
    [SerializeField] private TMP_Text walletLabel;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName   = "MainMenu";
    [SerializeField] private string manualSceneName     = "ManualDrive";
    [SerializeField] private string automationSceneName = "CodeDrive";

    // -------------------------------------------------------------------------

    void Start()
    {
        if (backButton != null)
            backButton.onClick.AddListener(() => LoadScene(mainMenuSceneName));

        if (settingsButton != null)
            settingsButton.onClick.AddListener(() => UniversalSettingsManager.Ensure()?.Open());

        RefreshEntries();

        if (walletLabel != null)
            walletLabel.text = $"₱ {SaveSystem.Current.currency}";
    }

    // -------------------------------------------------------------------------

    void RefreshEntries()
    {
        if (entries == null) return;

        SaveData save = SaveSystem.Current;

        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i] == null) continue;

            LevelDefinition def = LevelLibrary.Get(i);
            bool unlocked   = ProgressionRules.IsUnlocked(save, i);
            bool completed  = ProgressionRules.IsCompleted(save, i);
            bool hasContent = def.hasContent;

            string name = i == 0 ? "Tutorial" : $"Level {i}  —  {def.displayName}";

            string status;
            if (!unlocked)        status = "LOCKED";
            else if (!hasContent) status = "COMING SOON";
            else if (completed)   status = "COMPLETED";
            else                  status = "PLAY";

            int best = BestScoreFor(i);
            string bestText = best > 0 ? $"Best: {best}" : "";

            int levelIndex = i; // capture for the closure
            entries[i].Setup(name, status, bestText,
                locked: !unlocked,
                interactable: unlocked && hasContent,
                onClick: () => OnLevelClicked(levelIndex));
        }
    }

    int BestScoreFor(int levelIndex)
    {
        if (GameManager.Instance != null)
            return GameManager.Instance.GetBestScore(levelIndex);

        LevelScore entry = SaveSystem.Current.bestScores.Find(s => s.levelIndex == levelIndex);
        return entry != null ? entry.score : 0;
    }

    // -------------------------------------------------------------------------

    void OnLevelClicked(int levelIndex)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SelectedLevelIndex = levelIndex;

        // If the level has an overworld scene, go there instead of the jeep minigame
        LevelDefinition def = LevelLibrary.Get(levelIndex);
        if (!string.IsNullOrEmpty(def.overworldSceneName))
        {
            LoadScene(def.overworldSceneName);
            return;
        }

        bool manual = SaveSystem.Current.settings.manualMode;
        LoadScene(manual ? manualSceneName : automationSceneName);
    }

    void LoadScene(string sceneName)
    {
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.TransitionTo(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }
}
