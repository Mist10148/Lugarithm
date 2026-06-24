using System.Text;

public sealed class HintContext
{
    public string AuthoredFallback;
    public PassengerDefinition Passenger;
    public int Tier;
    public string PlayerSource;
    public string ParserFeedback;
    public string GoalGap;
    public string Diagnostics;
    public string Concept;
    public string[] AllowedBlocks;
    public string[] AllowedQueries;
}

public static class CopilotHintService
{
    public static AiRequest BuildRequest(HintContext context)
    {
        string tierInstruction = context.Tier <= 0
            ? "Give one subtle nudge identifying the direction of the logical flaw. Do not name a full solution."
            : context.Tier == 1
                ? "Explain the relevant programming concept and how the player's code could use it. Do not provide code."
                : "Provide short language-neutral pseudocode using plain verbs, not executable Lugarithm syntax.";

        StringBuilder prompt = new StringBuilder();
        prompt.AppendLine($"Speak as {context.Passenger.speakerName}: {context.Passenger.voice}");
        prompt.AppendLine($"Hint tier {context.Tier + 1}: {tierInstruction}");
        prompt.AppendLine($"Lesson concept: {context.Concept}");
        prompt.AppendLine($"Allowed actions: {string.Join(", ", context.AllowedBlocks ?? new string[0])}");
        prompt.AppendLine($"Allowed queries: {string.Join(", ", context.AllowedQueries ?? new string[0])}");
        prompt.AppendLine($"Observed goal gap: {context.GoalGap ?? "No run result yet."}");
        prompt.AppendLine($"Diagnosed issues (pre-analyzed, trustworthy): {context.Diagnostics ?? "None."}");
        prompt.AppendLine($"Parser/runtime feedback: {context.ParserFeedback ?? "None."}");
        prompt.AppendLine("PLAYER PROGRAM:");
        prompt.AppendLine(string.IsNullOrWhiteSpace(context.PlayerSource) ? "(empty)" : context.PlayerSource);
        prompt.AppendLine("Authored fallback meaning to preserve:");
        prompt.Append(context.AuthoredFallback);

        return new AiRequest
        {
            Feature = AiFeature.Hint,
            SystemInstruction =
                "You are a patient coding co-pilot for a beginner. Never output executable code or the exact puzzle solution. " +
                "Use only the evidence supplied. Return one or two short encouraging sentences.",
            Prompt = prompt.ToString(),
            MaxOutputTokens = 220
        };
    }
}
