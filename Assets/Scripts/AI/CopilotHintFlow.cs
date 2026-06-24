using System.Collections.Generic;

/// <summary>
/// Shared assembly of a co-pilot hint request: compile the player's code, dry-run it for a fresh
/// goal gap, pre-analyze it into concrete <see cref="CodeDiagnostics"/> evidence, and package it
/// all into the <see cref="CopilotHintService"/> request. Both the Automation drive and the code
/// minigames call this so the hint is equally sharp wherever the player is coding. Pure builder —
/// the caller owns the streaming + label.
/// </summary>
public static class CopilotHintFlow
{
    public static AiRequest BuildRequest(string source, AgentSim sim, AutomationPuzzleDefinition def,
                                         string authoredFallback, PassengerDefinition pax,
                                         int tier, string concept)
    {
        ProgramNode program = Parser.Compile(source ?? "", out List<LangError> errors);
        string parserFeedback = errors.Count > 0
            ? string.Join("; ", errors.ConvertAll(e => e.ToString())) : "None";

        // Dry-run the current code so the gap + diagnostics describe what's in the editor now.
        RunReport report = null;
        if (program != null && errors.Count == 0 && sim != null && def != null)
            HeadlessProgramRunner.VerifyReport(program, sim.CloneFresh(), def, out report);

        string gap = report != null ? report.GoalGap
                   : sim != null ? sim.DescribeGoalGap(def) : null;
        string diagnostics = CodeDiagnostics.Analyze(source, report,
            def != null ? def.allowedBlocks : null).Summary;

        return CopilotHintService.BuildRequest(new HintContext
        {
            AuthoredFallback = authoredFallback,
            Passenger        = pax,
            Tier             = tier,
            PlayerSource     = source,
            ParserFeedback   = parserFeedback,
            GoalGap          = gap,
            Diagnostics      = diagnostics,
            Concept          = concept,
            AllowedBlocks    = def != null ? def.allowedBlocks : null,
            AllowedQueries   = def != null ? def.allowedQueries : null,
        });
    }
}
