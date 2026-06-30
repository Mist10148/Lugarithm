/// <summary>
/// Fallback hint content for the code-based minigames, which have no story passenger or per-level
/// <c>assistHints</c> of their own. Gives the shared <see cref="CopilotHintFlow"/> a voice to speak
/// in and tiered authored fallbacks so the maze drill gets the same escalating help (nudge →
/// concept → pseudocode) as the main Automation levels.
/// </summary>
public static class MinigameHintLibrary
{
    public const int HintTierCount = 3;

    /// <summary>A practical, encouraging jeepney mechanic — the minigames' stand-in tutor.</summary>
    public static readonly PassengerDefinition Mechanic = new PassengerDefinition
    {
        id          = "mechanic",
        levelIndex  = -1,
        displayName = "Manong Mekaniko",
        speakerName = "Manong Mekaniko",
        role        = "jeepney mechanic",
        voice       = "A grizzled, kindly jeepney mechanic. Practical and encouraging, talks in short " +
                      "shop-floor advice and never lectures."
    };

    /// <summary>Tier 0/1/2 authored fallbacks for the maze repair drill (nudge → concept → pseudocode).</summary>
    public static readonly string[] MazeHints =
    {
        "Feel along the wall — keep one side of the jeepney against it and you'll thread the maze.",
        "When the same moves repeat, a loop lets you write them once instead of copying the line.",
        "Try: while you're not at the exit — if the way ahead is clear, move; otherwise turn toward the open side.",
    };

    public const string MazeConcept = "maze navigation with loops and conditionals";

    public static readonly string[] RepairOrderHints =
    {
        "Start with the safest first action: stop or release pressure before touching the broken system.",
        "Repair procedures usually go open, check, fix, then refill or close. Look for the first card that breaks that rhythm.",
        "The highlighted card is the first misplaced step. Move the step that should happen there into that slot, then test again.",
    };

    public static readonly string[] CodeOrderHints =
    {
        "Read the goal like a tiny story, then put the line that starts that story first.",
        "Indented lines belong under the nearest if, else, while, for, repeat, or def line above them.",
        "The highlighted line is the first one out of place. Move the line that should run at that moment into that slot.",
    };

    public static string RepairOrderHint(int tier, BreakdownFault fault, int firstWrongIndex)
    {
        tier = ClampTier(tier, RepairOrderHints.Length);
        string hint = RepairOrderHints[tier];
        if (tier == 0)
            return fault == BreakdownFault.Fuel
                ? hint + " For fuel, make the engine safe before the cap or nozzle."
                : hint + " For engine work, release pressure before opening things up.";
        if (tier == 2 && firstWrongIndex >= 0)
            return hint + $" Focus on step {firstWrongIndex + 1}.";
        return hint;
    }

    public static string CodeOrderHint(int tier, int firstWrongIndex)
    {
        tier = ClampTier(tier, CodeOrderHints.Length);
        string hint = CodeOrderHints[tier];
        if (tier == 2 && firstWrongIndex >= 0)
            return hint + $" Focus on line {firstWrongIndex + 1}.";
        return hint;
    }

    public static int ClampTier(int tier, int count = HintTierCount)
    {
        if (count <= 1) return 0;
        if (tier < 0) return 0;
        if (tier >= count) return count - 1;
        return tier;
    }
}
