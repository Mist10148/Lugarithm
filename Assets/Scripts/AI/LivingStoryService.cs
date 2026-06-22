using System.Text;

public static class LivingStoryService
{
    const string SystemInstruction =
        "You are a performance layer for authored dialogue in Lugarithm. You may change phrasing only. " +
        "Never change, add, remove, correct, or speculate about facts, plot events, instructions, names, dates, or numbers. " +
        "Return only the spoken line in one or two natural sentences.";

    public static AiRequest BuildRequest(string originalLine, PassengerDefinition passenger,
                                         int currentLevelIndex, DialogueTone tone, int affinity)
    {
        var hits = KnowledgeRagService.Retrieve(originalLine, SaveSystem.Current, 2, KnowledgeDomain.Heritage);
        string context = KnowledgeRagService.FormatContext(hits);
        StringBuilder prompt = new StringBuilder();
        prompt.AppendLine($"Speaker: {passenger.speakerName}");
        prompt.AppendLine($"Voice: {passenger.voice}");
        prompt.AppendLine($"Background: {passenger.background}");
        prompt.AppendLine($"Player tone this conversation: {tone}; affinity: {affinity}.");
        prompt.AppendLine("Use tone only to adjust warmth or reserve; do not change content.");
        if (!string.IsNullOrEmpty(context))
        {
            prompt.AppendLine("Grounding excerpts (support only; do not introduce unused details):");
            prompt.AppendLine(context);
        }
        prompt.AppendLine("Authored line to paraphrase exactly:");
        prompt.Append(originalLine);

        return new AiRequest
        {
            Feature = AiFeature.Dialogue,
            SystemInstruction = SystemInstruction,
            Prompt = prompt.ToString(),
            MaxOutputTokens = 180
        };
    }

    public static string ContextForValidation(string originalLine)
        => KnowledgeRagService.FormatContext(
            KnowledgeRagService.Retrieve(originalLine, SaveSystem.Current, 2, KnowledgeDomain.Heritage));
}
