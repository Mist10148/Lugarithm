using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEngine;

public class AiIntegrationTests
{
    [Test]
    public void SseParser_HandlesPacketsSplitAcrossLinesAndJson()
    {
        var received = new List<string>();
        var parser = new GeminiSseParser(received.Add);
        byte[] first = Encoding.UTF8.GetBytes("data: {\"one\":1");
        byte[] second = Encoding.UTF8.GetBytes("}\n\ndata: {\"two\":2}\n\n");
        parser.Feed(first, first.Length);
        Assert.That(received, Is.Empty);
        parser.Feed(second, second.Length);
        Assert.That(received, Is.EqualTo(new[] { "{\"one\":1}", "{\"two\":2}" }));
    }

    [Test]
    public void GroundingValidator_RejectsInventedAndDroppedNumbers()
    {
        Assert.IsTrue(AiGroundingValidator.ValidatePartial(
            "The church was completed in 1888.", "The old church still stands.", ""));
        Assert.IsTrue(AiGroundingValidator.ValidateParaphrase(
            "The church was completed in 1888.", "They completed the church in 1888.", "", out _));
        Assert.IsFalse(AiGroundingValidator.ValidateParaphrase(
            "The church was completed in 1888.", "They completed it in 1890.", "", out _));
        Assert.IsFalse(AiGroundingValidator.ValidateParaphrase(
            "The church was completed in 1888.", "They completed the church long ago.", "", out _));
    }

    [Test]
    public void DialogueChoice_UpdatesSessionToneAndAffinity()
    {
        var conversation = new DialogueConversation { startNode = "hub", hubNode = "hub" };
        conversation.nodes["hub"] = new DialogueNode
        {
            id = "hub", kind = DialogueNodeKind.Hub, lines = new DialogueLine[0],
            choices = new[] { new DialogueChoice { target = "end", tone = DialogueTone.Warm, affinityDelta = 2 } }
        };
        conversation.nodes["end"] = new DialogueNode
        {
            id = "end", kind = DialogueNodeKind.End,
            lines = new[] { new DialogueLine { speaker = "NPC", text = "Done" } }, choices = new DialogueChoice[0]
        };
        var runtime = new DialogueRuntime(conversation);
        runtime.Begin();
        runtime.Choose("end");
        Assert.AreEqual(DialogueTone.Warm, runtime.Tone);
        Assert.AreEqual(2, runtime.Affinity);
    }

    [Test]
    public void Rag_LocksFutureTownBeforeAnyNetworkPrompt()
    {
        var save = new SaveData { currentLevelIndex = 0 };
        Assert.IsTrue(KnowledgeRagService.TryGetLockedTownMessage("Tell me about Oton", save, out string message));
        Assert.That(message, Does.Contain("locked"));
    }

    [Test]
    public void Rag_ReturnsUnlockedCodingReference()
    {
        var save = new SaveData { currentLevelIndex = 0 };
        var hits = KnowledgeRagService.Retrieve("How do I write code and sequence actions?", save, 4, KnowledgeDomain.Coding);
        Assert.That(hits.Count, Is.GreaterThan(0));
        Assert.That(hits[0].Chunk.domain, Is.EqualTo(KnowledgeDomain.Coding));
    }

    [Test]
    public void Analytics_UsesDeterministicHundredPointBreakdown()
    {
        string source = "moveForward()\nmoveForward()";
        CodeAnalysis result = CodeAnalyticsService.Analyze(source, source, 10, 10, 0, 20f, 30f);
        Assert.AreEqual(100, result.EfficiencyScore);
        Assert.AreEqual(50, result.StepScore);
        Assert.AreEqual(20, result.RetryScore);
        Assert.AreEqual(15, result.TimeScore);
        Assert.AreEqual(15, result.StructureScore);
    }

    [Test]
    public void Analytics_ClassifiesNestedRouteLoops()
    {
        string source = "while not atDestination():\n    while frontIsClear():\n        moveForward()";
        CodeAnalysis result = CodeAnalyticsService.Analyze(source, source, 10, 10, 0, 1f, 0f);
        Assert.AreEqual(2, result.LoopDepth);
        Assert.AreEqual("O(n^2)", result.ComplexityClass);
    }

    [Test]
    public void Analytics_IncludesAttemptCountWithoutChangingScore()
    {
        string source = "moveForward()\nmoveForward()";
        CodeAnalysis result = CodeAnalyticsService.Analyze(source, source, 10, 10, 0, 20f, 30f,
            null, attemptCount: 3);
        Assert.AreEqual(100, result.EfficiencyScore);
        Assert.AreEqual(3, result.AttemptCount);
        StringAssert.Contains("runs 3", result.Summary);
    }

    [Test]
    public void RunHistory_RecordsStartedRunsAndSnapshots()
    {
        var history = new CodeRunHistory();
        CodeRunAttempt first = history.RecordStarted("moveForward()", "Code");
        history.Complete(first, false, "Stopped", 1, "Blocked at wall.");
        CodeRunAttempt second = history.RecordStarted("turnLeft()", "Code");

        Assert.AreEqual(2, history.Count);
        Assert.AreEqual("moveForward()", history.Attempts[0].source);
        Assert.AreEqual("Stopped", history.Attempts[0].status);
        Assert.AreEqual("turnLeft()", history.Last.source);
        Assert.AreEqual(2, history.Snapshot().Length);

        history.Complete(second, true, "Solved", 2, "Solved in 2 steps.");
        Assert.IsTrue(history.Last.succeeded);
    }

    [Test]
    public void LineOrderSourceText_JoinsLinesDeterministically()
    {
        string source = CodeRunHistory.SourceFromLines(new[] { "startEngine()", "driveToNextStop()", "openDoor()" });
        Assert.AreEqual("startEngine()\ndriveToNextStop()\nopenDoor()", source);
    }

    [Test]
    public void MentorReview_CanPreserveAuthoredReferenceForLineOrder()
    {
        string authored = "startEngine()\ndriveToNextStop()";
        string json = "{\"summary\":\"Good ordering work.\",\"optimizedCode\":\"wrong()\",\"annotations\":[]}";

        Assert.IsTrue(CodingMentorService.TryParseAndValidate(json, authored,
            Array.Empty<string>(), Array.Empty<string>(), 2, out MentorReview review,
            preserveAuthoredOptimal: true));
        Assert.AreEqual(authored, review.optimizedCode);
    }

    [Test]
    public void ExecutionController_PendingMacroMoveKeepsSourceNodeForHighlighting()
    {
        ProgramNode program = Parser.Compile("driveToNextStop()", out List<LangError> errors);
        Assert.IsEmpty(errors);

        var go = new GameObject("ExecutionController_PendingMoveHighlight_Test");
        try
        {
            var exec = go.AddComponent<ExecutionController>();
            StmtNode sourceNode = program.Statements[0];
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;

            typeof(ExecutionController).GetField("_pendingMoveNode", flags).SetValue(exec, sourceNode);
            typeof(ExecutionController).GetField("_pendingMoveSourceAction", flags).SetValue(exec, "driveToNextStop");

            var moveResult = new AgentActionResult { Action = "moveForward" };
            var step = (StepResult)typeof(ExecutionController)
                .GetMethod("PendingMoveStep", flags)
                .Invoke(exec, new object[] { moveResult });

            Assert.AreSame(sourceNode, step.Node);
            Assert.AreEqual("driveToNextStop", step.ActionName);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void GeneratedPolicy_RejectsLockedCommandsAndControlFlow()
    {
        string source = "while frontIsClear():\n    honk()";
        bool valid = GeneratedProgramPolicy.Validate(source,
            new[] { "moveForward" }, new[] { "frontIsClear" }, out List<LangError> errors);
        Assert.IsFalse(valid);
        Assert.That(errors.Count, Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public void Oracle_CitationsMustComeFromRetrievedUnlockedChunks()
    {
        var save = new SaveData { currentLevelIndex = 0 };
        var hits = KnowledgeRagService.Retrieve("coding sequence", save, 2, KnowledgeDomain.Coding);
        Assert.IsTrue(KnowledgeRagService.AreCitationsValid(new[] { hits[0].Chunk.id }, hits));
        Assert.IsFalse(KnowledgeRagService.AreCitationsValid(new[] { "heritage:locked:secret" }, hits));
    }

    [Test]
    public void VibeStructuredResponse_ParsesWithoutApplyingCode()
    {
        Assert.IsTrue(VibeCodingService.TryParse(
            "{\"kind\":\"code\",\"message\":\"Try this.\",\"code\":\"moveForward()\"}",
            out VibeCodeResponse response));
        Assert.AreEqual("moveForward()", response.code);
    }

    [Test]
    public void ActionGraph_CompilesNestedControlFlowWithIndentationAndComments()
    {
        var graph = new ActionGraphResponse
        {
            message = "Wall-follow to the exit.",
            nodes = new[]
            {
                new ActionGraphNode { op = "while", condition = "not atDestination()" },
                new ActionGraphNode { op = "if", condition = "frontIsClear()" },
                new ActionGraphNode { op = "action", name = "moveForward", comment = "step ahead" },
                new ActionGraphNode { op = "else" },
                new ActionGraphNode { op = "action", name = "turnLeft" },
                new ActionGraphNode { op = "endif" },
                new ActionGraphNode { op = "endwhile" },
            }
        };

        Assert.IsTrue(ActionGraphCompiler.TryCompile(graph, out string source, out string error), error);

        string expected =
            "while not atDestination():\n" +
            "    if frontIsClear():\n" +
            "        moveForward()  # step ahead\n" +
            "    else:\n" +
            "        turnLeft()";
        Assert.AreEqual(expected, source);

        // The compiled program must parse under the real language grammar.
        Parser.Compile(source, out List<LangError> parseErrors);
        Assert.IsEmpty(parseErrors);
    }

    [Test]
    public void ActionGraph_RejectsUnbalancedBlocks()
    {
        var graph = new ActionGraphResponse
        {
            nodes = new[]
            {
                new ActionGraphNode { op = "action", name = "moveForward" },
                new ActionGraphNode { op = "endif" },   // no matching if
            }
        };
        Assert.IsFalse(ActionGraphCompiler.TryCompile(graph, out _, out string error));
        Assert.IsNotEmpty(error);
    }

    // -------------------------------------------------------------------------
    // #2 Local result cache

    [Test]
    public void ResponseCache_HitMissAndLruEviction()
    {
        var cache = new AiResponseCache(2);
        Assert.IsFalse(cache.TryGet("a", out _));

        cache.Put("a", "A");
        cache.Put("b", "B");
        Assert.IsTrue(cache.TryGet("a", out string va));   // touches "a" → "b" is now LRU
        Assert.AreEqual("A", va);

        cache.Put("c", "C");                                // over capacity → evicts "b"
        Assert.IsFalse(cache.TryGet("b", out _));
        Assert.IsTrue(cache.TryGet("a", out _));
        Assert.IsTrue(cache.TryGet("c", out _));
    }

    [Test]
    public void DialogueCacheKey_IsStableAndToneSensitive()
    {
        var pax = new PassengerDefinition { id = "lola" };
        string k1 = LivingStoryService.CacheKey("Welcome aboard, child.", pax, 1, DialogueTone.Neutral, 0);
        string k2 = LivingStoryService.CacheKey("Welcome aboard, child.", pax, 1, DialogueTone.Neutral, 0);
        Assert.AreEqual(k1, k2, "same inputs must produce the same key");

        string warm = LivingStoryService.CacheKey("Welcome aboard, child.", pax, 1, DialogueTone.Warm, 0);
        Assert.AreNotEqual(k1, warm, "a tone change must change the key");
    }

    [Test]
    public void OracleCacheKey_NormalizesQuestionAndVariesWithChunks()
    {
        var hitsA = new List<KnowledgeHit> { new KnowledgeHit { Chunk = new KnowledgeChunk { id = "x" } } };
        var hitsB = new List<KnowledgeHit> { new KnowledgeHit { Chunk = new KnowledgeChunk { id = "y" } } };

        string ka = HeritageOracleService.CacheKey("tell me about molo", hitsA);
        Assert.AreEqual(ka, HeritageOracleService.CacheKey("  Tell Me About Molo  ", hitsA),
            "casing/whitespace must not change the key");
        Assert.AreNotEqual(ka, HeritageOracleService.CacheKey("tell me about molo", hitsB),
            "different retrieved records must change the key");
    }

    // -------------------------------------------------------------------------
    // #3 Headless agent verification

    [Test]
    public void HeadlessRunner_VerifiesCanonicalSolution()
    {
        AutomationPuzzleDefinition def = LevelLibrary.Get(0).auto;
        ProgramNode program = Parser.Compile(def.optimalSolutionText, out List<LangError> errors);
        CollectionAssert.IsEmpty(errors);
        var grid = GridModel.Parse(def.gridMap, out _);
        var sim = new AgentSim(grid, new FareTable(), def.startFacing);

        Assert.IsTrue(HeadlessProgramRunner.Verify(program, sim, def, out string gap), gap);
        Assert.IsNull(gap);
    }

    [Test]
    public void HeadlessRunner_RejectsProgramThatMissesTheGoal()
    {
        AutomationPuzzleDefinition def = LevelLibrary.Get(0).auto;
        ProgramNode program = Parser.Compile("wait()", out _);   // does nothing, never reaches D
        var grid = GridModel.Parse(def.gridMap, out _);
        var sim = new AgentSim(grid, new FareTable(), def.startFacing);

        Assert.IsFalse(HeadlessProgramRunner.Verify(program, sim, def, out string gap));
        Assert.IsNotEmpty(gap);
    }

    [Test]
    public void CloneFresh_LeavesTheLiveSimUntouched()
    {
        AutomationPuzzleDefinition def = LevelLibrary.Get(0).auto;
        var grid = GridModel.Parse(def.gridMap, out _);
        var sim = new AgentSim(grid, new FareTable(), def.startFacing);
        var startPos = sim.Position;

        ProgramNode program = Parser.Compile(def.optimalSolutionText, out _);
        HeadlessProgramRunner.Verify(program, sim.CloneFresh(), def, out _);

        Assert.AreEqual(startPos, sim.Position, "verifying on a clone must not move the live sim");
        Assert.AreEqual(0, sim.StepsUsed, "verifying on a clone must not advance the live sim");
    }

    // -------------------------------------------------------------------------
    // #4 AI usage tracker

    [Test]
    public void UsageTracker_AggregatesAndResets()
    {
        AiUsageTracker.Reset();
        AiUsageTracker.Record(AiFeature.Oracle,
            new AiResult { Success = true, Model = "gemini-test", PromptTokens = 10, OutputTokens = 5 });
        AiUsageTracker.Record(AiFeature.Oracle, AiResult.Failed(AiErrorKind.Timeout, "slow", "gemini-test"));

        string summary = AiUsageTracker.Summary();
        StringAssert.Contains("Oracle", summary);
        StringAssert.Contains("10 in + 5 out", summary);

        AiUsageTracker.Reset();
        StringAssert.Contains("No AI calls", AiUsageTracker.Summary());
    }
}
