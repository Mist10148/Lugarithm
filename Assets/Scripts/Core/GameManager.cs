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

    /// <summary>Records a best score for a town (keeps the higher of old/new).</summary>
    public void RecordTownScore(int townIndex, int score)
    {
        TownScore entry = SaveSystem.Current.bestScores.Find(s => s.townIndex == townIndex);
        if (entry == null)
            SaveSystem.Current.bestScores.Add(new TownScore { townIndex = townIndex, score = score });
        else if (score > entry.score)
            entry.score = score;
    }
}
