using System.Text;

/// <summary>
/// Assembles the Gemini prompt for the Virtual Coding Mentor by injecting the
/// player's solution, the optimal solution, and level metadata.
/// </summary>
public static class CodingMentorService
{
    const string SystemInstruction =
        "You are a friendly coding mentor in Lugarithm, a game that teaches programming " +
        "through jeepney driving puzzles. You speak warmly and encouragingly to players " +
        "aged 10–16.";

    const string Rules =
        "Rules:\n" +
        "- In 3–4 sentences, tell the player what their solution did well, what the optimal " +
        "does differently, and why it is more efficient.\n" +
        "- Name the coding concept (given below) and connect it to what the player wrote.\n" +
        "- Never show or quote code — describe it in plain words.\n" +
        "- Be encouraging: lead with something they got right.";

    public static string BuildPrompt(
        string levelDisplayName,
        string conceptName,
        int    stepsUsed,
        int    parSteps,
        int    retries,
        bool   usedCodeEditor,
        string playerSolution,
        string optimalSolution)
    {
        var sb = new StringBuilder();
        sb.AppendLine(SystemInstruction);
        sb.AppendLine();
        sb.AppendLine(Rules);
        sb.AppendLine();
        sb.AppendLine($"Level: {levelDisplayName}");
        sb.AppendLine($"Coding concept: {conceptName}");
        sb.AppendLine($"Steps used: {stepsUsed} (par: {parSteps})");
        sb.AppendLine($"Retries: {retries}");
        sb.AppendLine($"Editor mode: {(usedCodeEditor ? "Code" : "Block")}");
        sb.AppendLine();
        sb.AppendLine("=== PLAYER SOLUTION ===");
        sb.AppendLine(playerSolution);
        sb.AppendLine("=== OPTIMAL SOLUTION ===");
        sb.AppendLine(optimalSolution);

        return sb.ToString();
    }
}
