using System;
using System.Collections.Generic;

/// <summary>
/// Serializable container for all persisted game state.
/// Written to / read from disk by <see cref="SaveSystem"/> as JSON.
/// </summary>
[Serializable]
public class SaveData
{
    // -------------------------------------------------------------------------
    // Run progress
    //  hasActiveRun stays false until the player starts a New Game, which lets
    //  the Main Menu's Continue button key off progress (not file existence)
    //  while settings still get saved.
    //
    //  currentLevelIndex is the "unlock frontier": level N is unlocked iff
    //  N <= currentLevelIndex. 0 = Tutorial, 1 = Iloilo City (Molo), 2 = Oton,
    //  3 = Tigbauan, 4 = Miag-ao, 5 = San Joaquin. Completing the last level
    //  pushes the frontier to 6 so IsCompleted(5) still reads true.

    public bool      hasActiveRun          = false;
    public int       currentLevelIndex     = 0;
    public List<int> collectedJournalPages = new List<int>();
    public int       currency              = 0;
    public List<LevelScore> bestScores     = new List<LevelScore>();
    public List<int> earnedBadges          = new List<int>();

    // -------------------------------------------------------------------------
    // Journal helpers

    public bool HasPage(int pageId)
        => collectedJournalPages != null && collectedJournalPages.Contains(pageId);

    public void UnlockPage(int pageId)
    {
        if (collectedJournalPages == null) collectedJournalPages = new List<int>();
        if (!collectedJournalPages.Contains(pageId)) collectedJournalPages.Add(pageId);
    }

    // -------------------------------------------------------------------------
    // Badge helpers

    public bool HasBadge(int levelIndex)
        => earnedBadges != null && earnedBadges.Contains(levelIndex);

    public void EarnBadge(int levelIndex)
    {
        if (earnedBadges == null) earnedBadges = new List<int>();
        if (!earnedBadges.Contains(levelIndex)) earnedBadges.Add(levelIndex);
    }

    // -------------------------------------------------------------------------
    // Settings (persist independently of run progress)

    public GameSettings settings = new GameSettings();
}

/// <summary>Best recorded score for a single level, keyed by level index.</summary>
[Serializable]
public class LevelScore
{
    public int levelIndex;
    public int score;
}

/// <summary>
/// Player-configurable settings. Mirrors the Settings table in the PRD (§9).
/// Defaults: Manual Mode, Block Mode, 80% volumes, Normal dialogue speed,
/// subtitles on.
/// </summary>
[Serializable]
public class GameSettings
{
    public bool  manualMode    = true;                     // true = Manual, false = Automation
    public bool  blockMode     = true;                     // true = Easy/Block, false = Hard/Code
    public float musicVolume   = 0.8f;
    public float sfxVolume     = 0.8f;
    public int   dialogueSpeed = (int)DialogueSpeed.Normal;
    public bool  subtitles     = true;
}

/// <summary>Dialogue reveal speed options (PRD §9).</summary>
public enum DialogueSpeed
{
    Slow    = 0,
    Normal  = 1,
    Fast    = 2,
    Instant = 3
}
