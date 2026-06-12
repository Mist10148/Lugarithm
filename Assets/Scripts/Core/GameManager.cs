using UnityEngine;

/// <summary>
/// Persistent game-wide singleton. Loads the save on boot, carries transient
/// run state across scenes, and is the central access point for progression.
/// Place one on a persistent object in the first-loaded (Bootstrap/Splash)
/// scene so it exists before anything else.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // -------------------------------------------------------------------------
    // Transient run state
    //  Currency earned during the current leg, not yet committed to the save.
    //  Flushed into the save file by SaveProgress().

    public int PendingCurrency { get; set; }

    /// <summary>
    /// Level chosen on the Level Select screen; the drive scenes read this to
    /// know which leg to build. 0 = Tutorial ... 5 = San Joaquin.
    /// </summary>
    public int SelectedLevelIndex { get; set; }

    // -------------------------------------------------------------------------

    void Awake()
    {
        // Enforce a single persistent instance.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SaveSystem.Load();
    }

    // -------------------------------------------------------------------------
    // Progression

    /// <summary>Convenience accessor for the loaded save data.</summary>
    public SaveData Save => SaveSystem.Current;

    /// <summary>
    /// Commits transient run state to the save file. Call on town completion
    /// (the PRD's auto-save point).
    /// </summary>
    public void SaveProgress()
    {
        SaveSystem.Current.currency += PendingCurrency;
        PendingCurrency = 0;
        SaveSystem.AutoSave();
    }

    /// <summary>Records a best score for a level (keeps the higher of old/new).</summary>
    public void RecordLevelScore(int levelIndex, int score)
    {
        LevelScore entry = SaveSystem.Current.bestScores.Find(s => s.levelIndex == levelIndex);
        if (entry == null)
            SaveSystem.Current.bestScores.Add(new LevelScore { levelIndex = levelIndex, score = score });
        else if (score > entry.score)
            entry.score = score;
    }

    /// <summary>Best recorded score for a level, or 0 if it has none yet.</summary>
    public int GetBestScore(int levelIndex)
    {
        LevelScore entry = SaveSystem.Current.bestScores.Find(s => s.levelIndex == levelIndex);
        return entry != null ? entry.score : 0;
    }

    /// <summary>
    /// Finishes a leg: records the score, advances the unlock frontier, and
    /// commits pending currency to the save (the auto-save point).
    /// </summary>
    public void CompleteLevel(int levelIndex, int score)
    {
        RecordLevelScore(levelIndex, score);
        ProgressionRules.CompleteLevel(SaveSystem.Current, levelIndex);
        SaveSystem.Current.UnlockPage(levelIndex);
        SaveSystem.Current.EarnBadge(levelIndex);
        SaveProgress();
    }
}
