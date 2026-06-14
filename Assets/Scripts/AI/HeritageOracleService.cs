using System.Text;

/// <summary>
/// Assembles the Gemini prompt for the Almanac Oracle by injecting heritage facts
/// from <see cref="HeritageLibrary"/> gated by the player's unlock frontier.
/// Spoiler-gating rules:
///   - An entry is included when levelIndex == -1 (Guimbal drive-through, always)
///     or levelIndex &lt;= currentLevelIndex.
///   - holdForReveal facts are included only when the town's level is fully behind
///     the frontier (levelIndex &lt; currentLevelIndex), meaning its completion
///     cutscene has already played.
/// </summary>
public static class HeritageOracleService
{
    const string SystemInstruction =
        "You are the Almanac Oracle in Lugarithm, a game about jeepney heritage along the " +
        "Iloilo coast in the Philippines. Speak with warmth and cultural pride — like a " +
        "knowledgeable lola who loves her history. Only reference facts from the heritage " +
        "dossier below; never invent details. If a question clearly touches a town not yet " +
        "in the dossier, say: \"My records for that town are still sealed.\" " +
        "Keep your reply to 2–3 sentences.";

    public static string BuildPrompt(string question, int currentLevelIndex)
    {
        var sb = new StringBuilder();
        sb.AppendLine(SystemInstruction);
        sb.AppendLine();
        sb.AppendLine("=== HERITAGE DOSSIER ===");

        foreach (var entry in HeritageLibrary.All)
        {
            bool entryUnlocked   = entry.levelIndex == -1 || entry.levelIndex <= currentLevelIndex;
            bool revealsUnlocked = entry.levelIndex == -1 || entry.levelIndex < currentLevelIndex;
            if (!entryUnlocked) continue;

            sb.AppendLine($"[{entry.townName}] {entry.theme}");
            foreach (var fact in entry.keyFacts)
            {
                if (fact.holdForReveal && !revealsUnlocked) continue;
                sb.AppendLine($"  • {fact.headline}: {fact.detail}");
            }
        }

        sb.AppendLine("========================");
        sb.AppendLine();
        sb.Append($"Player asks: {question}");
        return sb.ToString();
    }

    /// <summary>
    /// Returns a short fallback string drawn from the current town's driveSpend text
    /// when the API call fails or is unavailable.
    /// </summary>
    public static string FallbackResponse(int currentLevelIndex)
    {
        var entry = HeritageLibrary.ForLevel(currentLevelIndex);
        if (entry != null)
            return $"(The Oracle speaks from memory:) {entry.driveSpend}";
        return "The Oracle's voice fades for a moment. Try again later.";
    }
}
