using System.Text;

/// <summary>
/// The "living story" performance layer for authored passenger dialogue. Unlike the
/// archival Heritage Oracle, this delivers a line <i>in character</i> and aware of the
/// journey so far — the speaker's role, their bond with the player's late father, the
/// current leg of the coastal route, and the player's rapport — while never inventing
/// facts, numbers, names, or plot. The authored line remains the guaranteed fallback,
/// enforced by <see cref="AiGroundingValidator"/>.
/// </summary>
public static class LivingStoryService
{
    const string SystemInstruction =
        "You voice authored dialogue in Lugarithm, a story about a child finishing their late father's " +
        "jeepney route along the Iloilo coast. Deliver the given line as THIS character would actually say " +
        "it — warm, in their distinct voice, aware of the journey and the people in it. This is a storytelling " +
        "voice, not an archive. You may reshape phrasing and warmth only: never change, add, remove, correct, or " +
        "invent facts, plot events, instructions, names, dates, or numbers. Keep it to one or two natural " +
        "spoken sentences.";

    public static AiRequest BuildRequest(string originalLine, PassengerDefinition passenger,
                                         int currentLevelIndex, DialogueTone tone, int affinity)
    {
        var hits = KnowledgeRagService.Retrieve(originalLine, SaveSystem.Current, 2, KnowledgeDomain.Heritage);
        string context = KnowledgeRagService.FormatContext(hits);
        string leg = currentLevelIndex >= 0 && currentLevelIndex < LevelLibrary.Count
            ? LevelLibrary.Names[currentLevelIndex] : "the coastal route";

        StringBuilder prompt = new StringBuilder();
        prompt.AppendLine($"Speaker: {passenger.displayName} — {passenger.role}, from {passenger.town}.");
        prompt.AppendLine($"Voice: {passenger.voice}");
        prompt.AppendLine($"Who they are: {passenger.background}");
        if (!string.IsNullOrWhiteSpace(passenger.relationshipToFather))
            prompt.AppendLine($"Bond with the player's father: {passenger.relationshipToFather}");
        prompt.AppendLine($"Current leg of the journey: {leg}.");
        prompt.AppendLine($"Player's rapport this conversation: tone {tone}, affinity {affinity}.");
        prompt.AppendLine("Use tone and bond only to colour warmth or reserve; never change the content.");
        if (!string.IsNullOrEmpty(context))
        {
            prompt.AppendLine("Heritage the speaker may naturally reference (support only; introduce nothing new):");
            prompt.AppendLine(context);
        }
        prompt.AppendLine("Deliver this line in character, preserving its meaning exactly:");
        prompt.Append(originalLine);

        return new AiRequest
        {
            Feature = AiFeature.Dialogue,
            SystemInstruction = SystemInstruction,
            Prompt = prompt.ToString(),
            MaxOutputTokens = 120
        };
    }

    /// <summary>Cache key for a validated rephrase. Mirrors the inputs <see cref="BuildRequest"/>
    /// varies the prompt on (speaker, leg, tone, rapport) so a hit always corresponds to the
    /// same in-character delivery. Affinity is bucketed by sign — it only colours warmth, never
    /// the grounded content — so small swings still hit the cache.</summary>
    public static string CacheKey(string originalLine, PassengerDefinition passenger,
                                  int currentLevelIndex, DialogueTone tone, int affinity)
    {
        int affinityBucket = affinity == 0 ? 0 : (affinity > 0 ? 1 : -1);
        string speaker = passenger != null ? passenger.id : "?";
        return $"{speaker}|{currentLevelIndex}|{tone}|{affinityBucket}|{originalLine}";
    }

    /// <summary>Token guard: short, trivial lines (greetings, one-word reactions) aren't
    /// worth an API round-trip — the authored line is delivered verbatim instead.</summary>
    public static bool ShouldRephrase(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return false;
        string trimmed = line.Trim();
        if (trimmed.Length < 40) return false;
        return trimmed.Split(' ').Length >= 7;
    }

    public static string ContextForValidation(string originalLine)
        => KnowledgeRagService.FormatContext(
            KnowledgeRagService.Retrieve(originalLine, SaveSystem.Current, 2, KnowledgeDomain.Heritage));
}
