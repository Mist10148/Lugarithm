/// <summary>
/// Local, zero-token intent classifier for the in-editor co-pilot. When the agent is in
/// <see cref="VibeMode.Auto"/>, this routes a free-text message to a concrete mode so the player
/// never has to pick Ask/Plan/Agent/Refactor by hand — "can you solve this for me" just builds,
/// "make this shorter" streamlines, "why isn't this working" explains. Keyword heuristics with a
/// safe <see cref="VibeMode.Ask"/> default; order matters (most specific intent first).
/// </summary>
public static class VibeIntentRouter
{
    public static VibeMode Classify(string message, bool hasEditorCode)
    {
        string m = (message ?? string.Empty).ToLowerInvariant();

        // Refactor only applies when there's existing code to improve.
        if (hasEditorCode && ContainsAny(m,
                "shorter", "streamline", "simplify", "refactor", "tidy", "clean it",
                "clean up", "compress", "condense", "optimi", "instead of repeat",
                "instead of spam", "use a loop instead", "with a loop", "fewer line", "less code"))
            return VibeMode.Refactor;

        // Agent: just make/solve it.
        if (ContainsAny(m,
                "solve", "do it for me", "do this for me", "make it", "write it", "write the",
                "automate", "finish it", "build the", "code it", "just give me", "complete it"))
            return VibeMode.Agent;

        // Plan: asking for an approach, not an answer.
        if (ContainsAny(m,
                "how do i", "how to", "how can i", "approach", "where do i start",
                "what should i", "steps to", "strategy", "plan for", "first step"))
            return VibeMode.Plan;

        // Everything else — questions, "why", "stuck", "error" — is a read-only explanation.
        return VibeMode.Ask;
    }

    /// <summary>True when the message asks to undo the last refactor placement.</summary>
    public static bool IsUndo(string message)
    {
        string m = (message ?? string.Empty).ToLowerInvariant().Trim();
        return ContainsAny(m, "undo", "revert", "put it back", "put my", "go back",
                              "restore", "original version", "my version");
    }

    static bool ContainsAny(string haystack, params string[] needles)
    {
        foreach (string n in needles)
            if (haystack.Contains(n)) return true;
        return false;
    }
}
