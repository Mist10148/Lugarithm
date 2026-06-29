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

    /// <summary>
    /// Spends from the player's visible wallet (saved currency + pending run
    /// earnings), clamping at zero so gameplay is never blocked by being broke.
    /// Any shortfall becomes debt instead of being forgiven — see <see cref="ShortfallToDebt"/>.
    /// Pending earnings are spent first so the HUD stays consistent mid-run.
    /// </summary>
    public int SpendCurrency(int amount)
    {
        if (amount <= 0) return 0;

        int pending = PendingCurrency;
        int shortfall = ShortfallToDebt(SaveSystem.Current, ref pending, amount);
        PendingCurrency = pending;

        int spent = amount - shortfall;
        if (spent > 0 || shortfall > 0) SaveSystem.AutoSave();
        return spent;
    }

    /// <summary>Pure spending seam for tests and non-MonoBehaviour callers.</summary>
    public static int SpendCurrency(SaveData save, ref int pendingCurrency, int amount)
    {
        if (amount <= 0 || save == null) return 0;

        int spent = 0;
        int fromPending = Mathf.Min(Mathf.Max(0, pendingCurrency), amount);
        pendingCurrency -= fromPending;
        spent += fromPending;

        int remaining = amount - fromPending;
        int fromSaved = Mathf.Min(Mathf.Max(0, save.currency), remaining);
        save.currency -= fromSaved;
        spent += fromSaved;

        return spent;
    }

    /// <summary>
    /// Pure debt-accrual seam: spends via the existing clamped helper, then puts
    /// whatever's left unpaid onto debt instead of forgiving it.
    /// </summary>
    public static int ShortfallToDebt(SaveData save, ref int pendingCurrency, int amount)
    {
        int spent = SpendCurrency(save, ref pendingCurrency, amount);
        int shortfall = amount - spent;
        if (shortfall > 0 && save != null) save.debt += shortfall;
        return shortfall;
    }

    /// <summary>
    /// Records currency the player earned (fares, score bonuses). Routes through
    /// this — never PendingCurrency += directly — so any outstanding debt from an
    /// underfunded refuel is paid down automatically out of the very next earning.
    /// </summary>
    public void EarnCurrency(int amount)
    {
        if (amount <= 0) return;

        int remainder = EarnCurrency(SaveSystem.Current, amount);
        if (remainder > 0) PendingCurrency += remainder;
        if (remainder != amount) SaveSystem.AutoSave();   // some/all went to debt
    }

    /// <summary>Pure earning seam: pays down debt first, returns the remainder to add to PendingCurrency.</summary>
    public static int EarnCurrency(SaveData save, int amount)
    {
        if (amount <= 0 || save == null) return 0;

        int debtPayment = Mathf.Min(Mathf.Max(0, save.debt), amount);
        save.debt -= debtPayment;
        return amount - debtPayment;
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
