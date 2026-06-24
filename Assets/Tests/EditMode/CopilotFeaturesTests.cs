using NUnit.Framework;

/// <summary>
/// Covers the co-pilot upgrades: the headless <see cref="RunReport"/> telemetry, the
/// <see cref="CodeDiagnostics"/> anti-pattern detector, the refactor "shorter + still wins" rule,
/// the <see cref="VibeIntentRouter"/> classifier, and the new request builders. Uses a tiny
/// hand-built straight corridor so the assertions don't depend on the authored level maps.
/// </summary>
public class CopilotFeaturesTests
{
    // A 4-cell straight corridor: S at (1,1) facing East, D at (5,1), no passengers.
    static AutomationPuzzleDefinition Corridor()
    {
        return new AutomationPuzzleDefinition
        {
            gridMap = new[]
            {
                "#######",
                "#S...D#",
                "#######",
            },
            startFacing = 1,                       // East (+x)
            goalText = "Drive straight to D.",
            requireAllPassengersDelivered = true,  // satisfied trivially — no stops
            allowedBlocks  = new[] { "moveForward", "while" },
            allowedQueries = new[] { "atDestination" },
        };
    }

    static AgentSim FreshSim(AutomationPuzzleDefinition def)
    {
        GridModel grid = GridModel.Parse(def.gridMap, out var mapErrors);
        CollectionAssert.IsEmpty(mapErrors, "corridor map must parse");
        return new AgentSim(grid, new FareTable(), def.startFacing);
    }

    static bool Run(string source, AutomationPuzzleDefinition def, out RunReport report)
    {
        ProgramNode program = Parser.Compile(source, out var errors);
        CollectionAssert.IsEmpty(errors, "test program must compile");
        return HeadlessProgramRunner.VerifyReport(program, FreshSim(def), def, out report);
    }

    const string Spam = "moveForward()\nmoveForward()\nmoveForward()\nmoveForward()\n";
    const string Loop = "while not atDestination():\n    moveForward()\n";

    // -------------------------------------------------------------------------
    // RunReport

    [Test]
    public void RunReport_OnWin_RecordsTraceAndCleanGap()
    {
        AutomationPuzzleDefinition def = Corridor();
        bool win = Run(Spam, def, out RunReport report);

        Assert.IsTrue(win, "four forward moves should reach D");
        Assert.IsTrue(report.Win);
        Assert.IsNull(report.GoalGap, "a win leaves no goal gap");
        Assert.AreEqual(0, report.TotalPassengers, "corridor has no stops");
        Assert.AreEqual(4, report.Trace.Count, "every primitive move is recorded");
        Assert.IsFalse(report.RuntimeErrored);
    }

    [Test]
    public void RunReport_OnLoss_ReportsWhereItStopped()
    {
        AutomationPuzzleDefinition def = Corridor();
        bool win = Run("wait()\nwait()\n", def, out RunReport report);

        Assert.IsFalse(win);
        Assert.IsFalse(report.Win);
        Assert.IsNotNull(report.GoalGap);
        StringAssert.Contains("destination", report.GoalGap.ToLowerInvariant());
    }

    // -------------------------------------------------------------------------
    // CodeDiagnostics

    [Test]
    public void Diagnostics_FlagsRepeatedPrimitiveAndNoLoop()
    {
        AutomationPuzzleDefinition def = Corridor();
        Run(Spam, def, out RunReport report);

        DiagnosticsResult d = CodeDiagnostics.Analyze(Spam, report, def.allowedBlocks);

        Assert.IsTrue(d.HasRepeatedPrimitive, "four moveForward() in a row is a loop candidate");
        Assert.IsTrue(d.NoLoopButRepeats, "verbose with no loop should be flagged");
        StringAssert.Contains("moveForward", d.Summary);
    }

    [Test]
    public void Diagnostics_CleanOnTheLoopedSolution()
    {
        AutomationPuzzleDefinition def = Corridor();
        Run(Loop, def, out RunReport report);

        DiagnosticsResult d = CodeDiagnostics.Analyze(Loop, report, def.allowedBlocks);

        Assert.IsFalse(d.HasRepeatedPrimitive);
        Assert.IsFalse(d.NoLoopButRepeats);
        Assert.IsFalse(d.Undelivered);
    }

    [Test]
    public void Diagnostics_EmptyEditorIsFlagged()
    {
        DiagnosticsResult d = CodeDiagnostics.Analyze("", null, Corridor().allowedBlocks);
        Assert.IsTrue(d.Empty);
    }

    // -------------------------------------------------------------------------
    // Refactor rule: the looped rewrite still wins AND is strictly shorter.

    [Test]
    public void Refactor_LoopedVersion_WinsAndIsShorter()
    {
        AutomationPuzzleDefinition def = Corridor();

        Assert.IsTrue(Run(Spam, def, out _), "the player's verbose version wins");
        Assert.IsTrue(Run(Loop, def, out _), "the looped rewrite still wins");

        int spam = CodeAnalyticsService.Measure(Spam).Statements;
        int loop = CodeAnalyticsService.Measure(Loop).Statements;
        Assert.Less(loop, spam, "the loop must be fewer statements (the acceptance rule)");

        Assert.AreEqual(0, CodeAnalyticsService.Measure(Spam).MaxLoopDepth);
        Assert.AreEqual(1, CodeAnalyticsService.Measure(Loop).MaxLoopDepth);
    }

    // -------------------------------------------------------------------------
    // Intent router

    [Test]
    public void Router_ClassifiesIntents()
    {
        Assert.AreEqual(VibeMode.Agent,    VibeIntentRouter.Classify("can you solve this for me", true));
        Assert.AreEqual(VibeMode.Refactor, VibeIntentRouter.Classify("make this shorter please", true));
        Assert.AreEqual(VibeMode.Plan,     VibeIntentRouter.Classify("how do I start this?", false));
        Assert.AreEqual(VibeMode.Ask,      VibeIntentRouter.Classify("why isn't this working?", true));
    }

    [Test]
    public void Router_RefactorNeedsExistingCode()
    {
        // Without code in the editor, "make it shorter" can't be a refactor.
        Assert.AreNotEqual(VibeMode.Refactor, VibeIntentRouter.Classify("make it shorter", false));
    }

    [Test]
    public void Router_DetectsUndo()
    {
        Assert.IsTrue(VibeIntentRouter.IsUndo("undo"));
        Assert.IsTrue(VibeIntentRouter.IsUndo("put it back please"));
        Assert.IsFalse(VibeIntentRouter.IsUndo("make a loop"));
    }

    // -------------------------------------------------------------------------
    // Request builders

    [Test]
    public void GhostRequest_IsTinyAndCodeFeatured()
    {
        AiRequest req = VibeCodingService.BuildGhostRequest("moveForward()\n", "Reach D",
            new[] { "moveForward" }, new[] { "atDestination" });

        Assert.AreEqual(AiFeature.VibeCode, req.Feature);
        Assert.AreEqual(32, req.MaxOutputTokens);
        Assert.IsFalse(req.WantsJson, "ghost completion is plain text, not JSON");
        StringAssert.Contains("moveForward", req.Prompt);
    }

    [Test]
    public void RefactorRequest_AsksForAnActionGraph()
    {
        AiRequest req = VibeCodingService.BuildRefactorRequest(Spam, "GOAL: D");
        Assert.AreEqual(AiFeature.VibeCode, req.Feature);
        Assert.IsTrue(req.WantsJson, "refactor returns a validated action graph");
        StringAssert.Contains("moveForward", req.Prompt);
    }
}
