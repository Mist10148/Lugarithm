/// <summary>
/// Fallback hint content for the code-based minigames, which have no story passenger or per-level
/// <c>assistHints</c> of their own. Gives the shared <see cref="CopilotHintFlow"/> a voice to speak
/// in and tiered authored fallbacks so the maze drill gets the same escalating help (nudge →
/// concept → pseudocode) as the main Automation levels.
/// </summary>
public static class MinigameHintLibrary
{
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
}
