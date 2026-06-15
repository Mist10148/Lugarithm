using System.Text;

/// <summary>
/// Assembles the Gemini prompt for the Living Story Engine. When the player
/// revisits a dialogue node, the model rephrases the original line in the
/// passenger's voice while staying grounded to the spoiler-gated heritage
/// dossier — no invented facts.
/// </summary>
public static class LivingStoryService
{
    public static string BuildPrompt(
        string originalLine,
        PassengerDefinition passenger,
        int currentLevelIndex)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"You are {passenger.speakerName} in Lugarithm, a Philippine heritage jeepney game.");
        sb.AppendLine($"Character: {passenger.voice}. Background: {passenger.background}.");
        sb.AppendLine();
        sb.AppendLine("You are talking to the player about heritage topics while they ride your jeepney. The player has heard this exact line before. Rewrite it so it feels fresh — same meaning, same facts, but phrased differently, as if you're remembering it from a different angle. One to two sentences only. Stay strictly in character.");
        sb.AppendLine();
        sb.AppendLine("ONLY use facts from the heritage dossier below. Do not invent facts, dates, names, or events.");
        sb.AppendLine();
        sb.AppendLine("=== HERITAGE DOSSIER ===");

        foreach (var entry in HeritageLibrary.All)
        {
            bool unlocked = entry.levelIndex == -1 || entry.levelIndex <= currentLevelIndex;
            bool revealed = entry.levelIndex == -1 || entry.levelIndex < currentLevelIndex;
            if (!unlocked) continue;

            sb.AppendLine($"[{entry.townName}]");
            foreach (var fact in entry.keyFacts)
            {
                if (fact.holdForReveal && !revealed) continue;
                sb.AppendLine($"  • {fact.headline}: {fact.detail}");
            }
        }

        sb.AppendLine("========================");
        sb.AppendLine();
        sb.Append($"Original line: \"{originalLine}\"");
        return sb.ToString();
    }
}
