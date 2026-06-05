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

    public bool      hasActiveRun          = false;
    public int       currentTownIndex      = 0;            // 0 = Oton ... 4 = San Joaquin
    public List<int> collectedJournalPages = new List<int>();
    public int       currency              = 0;
    public List<TownScore> bestScores      = new List<TownScore>();

    // -------------------------------------------------------------------------
    // Settings (persist independently of run progress)

    public GameSettings settings = new GameSettings();
}

/// <summary>Best recorded score for a single town, keyed by town index.</summary>
[Serializable]
public class TownScore
{
    public int townIndex;
    public int score;
}

/// <summary>
/// Player-configurable settings. Mirrors the Settings table in the PRD (§9).
/// Defaults: Block Mode, 80% volumes, Normal dialogue speed, subtitles on.
/// </summary>
[Serializable]
public class GameSettings
{
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
