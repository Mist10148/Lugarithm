using System;

/// <summary>
/// Pure score formulas for both gameplay modes. There is no fail state —
/// mistakes only dent the score, and currency earned is derived from it
/// (PRD §6.4). No UnityEngine dependency — covered by EditMode tests.
/// </summary>
public static class ScoreCalculator
{
    /// <summary>Text/Code Mode pays out more than Block Mode (PRD §8.1).</summary>
    public const float CodeModeMultiplier = 1.5f;

    // -------------------------------------------------------------------------
    // Automation Mode

    /// <summary>
    /// Score for a solved automation puzzle. Penalties: steps over par,
    /// retries, and letting the soft timer expire. Floor of 100 — solving
    /// always pays something.
    /// </summary>
    public static int AutomationScore(int steps, int parSteps, float elapsedSeconds,
                                      float softTimerSeconds, int retries, bool codeMode)
    {
        int score = 1000
                  - 15 * Math.Max(0, steps - parSteps)
                  - 50 * Math.Max(0, retries)
                  - (softTimerSeconds > 0f && elapsedSeconds > softTimerSeconds ? 150 : 0);

        score = Math.Max(100, score);

        if (codeMode)
            score = (int)(score * CodeModeMultiplier);

        return score;
    }

    // -------------------------------------------------------------------------
    // Manual Mode

    /// <summary>
    /// Score for a manual drive leg. Rewards exact change and happy
    /// passengers; penalises wrong change, timeouts, missed stops, and a
    /// fumbled breakdown repair. Finishing under par time adds up to 600.
    /// </summary>
    public static int ManualScore(int faresExact, int faresWrong, int faresTimedOut,
                                  int missedStops, int satisfactionBonus,
                                  bool breakdownTimedOut,
                                  float elapsedSeconds, float parTimeSeconds)
    {
        float underPar = Math.Max(0f, Math.Min(parTimeSeconds - elapsedSeconds, 300f));

        int score = 1000
                  + 10  * Math.Max(0, faresExact)
                  + Math.Max(0, satisfactionBonus)
                  - 50  * Math.Max(0, faresWrong)
                  - 25  * Math.Max(0, faresTimedOut)
                  - 100 * Math.Max(0, missedStops)
                  - (breakdownTimedOut ? 100 : 0)
                  + (int)(underPar * 2f);

        return Math.Max(0, score);
    }

    // -------------------------------------------------------------------------
    // Currency

    /// <summary>In-game cash earned for a leg, derived from its score.</summary>
    public static int CurrencyFor(int score)
    {
        return Math.Max(0, score) / 10;
    }
}
