using System;

/// <summary>
/// Pure rules for level locking and completion, keyed off
/// <see cref="SaveData.currentLevelIndex"/> (the "unlock frontier").
/// Level indices: 0 = Tutorial, 1 = Iloilo City (Molo), 2 = Oton,
/// 3 = Tigbauan, 4 = Miag-ao, 5 = San Joaquin.
/// </summary>
public static class ProgressionRules
{
    /// <summary>Tutorial + the five towns.</summary>
    public const int LevelCount = 6;

    // -------------------------------------------------------------------------

    /// <summary>A level is playable once the frontier has reached it.</summary>
    public static bool IsUnlocked(SaveData save, int levelIndex)
    {
        if (save == null) return levelIndex == 0;
        return levelIndex >= 0 && levelIndex <= save.currentLevelIndex;
    }

    /// <summary>A level counts as completed once the frontier has passed it.</summary>
    public static bool IsCompleted(SaveData save, int levelIndex)
    {
        return save != null && levelIndex >= 0 && levelIndex < save.currentLevelIndex;
    }

    /// <summary>
    /// Advances the unlock frontier past <paramref name="levelIndex"/>.
    /// Replays never regress the frontier. The frontier may reach
    /// <see cref="LevelCount"/> (one past the last level) so the final level
    /// still reads as completed.
    /// </summary>
    public static void CompleteLevel(SaveData save, int levelIndex)
    {
        if (save == null || levelIndex < 0 || levelIndex >= LevelCount) return;

        int next = Math.Min(levelIndex + 1, LevelCount);
        if (next > save.currentLevelIndex)
            save.currentLevelIndex = next;
    }
}
