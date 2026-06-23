using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

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
}
