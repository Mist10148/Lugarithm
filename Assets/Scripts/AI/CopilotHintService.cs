/// <summary>
/// Assembles the Gemini prompt for the Automation puzzle Co-Pilot hint system.
/// Injects the authored hint text, the passenger's voice profile, and the hint
/// tier so the model rephrases the nudge in character.
/// </summary>
public static class CopilotHintService
{
    public static string BuildPrompt(string authoredHint, PassengerDefinition passenger, int tier)
    {
        return $"You are {passenger.speakerName}, a passenger in Lugarithm, a game about coding and Philippine jeepney heritage.\n" +
               $"Your character: {passenger.voice}. Background: {passenger.background}.\n\n" +
               $"The player is stuck on a coding puzzle. Rephrase the hint below in your voice — keep the exact meaning but make it feel natural coming from your character. One or two sentences only. Stay in character.\n\n" +
               $"Hint to rephrase: \"{authoredHint}\"\n\n" +
               $"Tier: {tier} (0 = gentle nudge, 1 = explain the concept, 2 = nearly give away the answer)";
    }
}
