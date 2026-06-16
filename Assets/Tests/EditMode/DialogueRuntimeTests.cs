using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>EditMode tests for the pure dialogue state machine.</summary>
public class DialogueRuntimeTests
{
    [Test]
    public void BoardingToHubToTopic_ReturnsToHub()
    {
        DialogueRuntime rt = new DialogueRuntime(DialogueLibrary.ForLevel(1));
        rt.Begin();

        Assert.AreEqual("M-BOARD", rt.CurrentNodeId);
        Assert.IsNotNull(rt.Current);

        rt.AdvanceLine();
        Assert.AreEqual("HUB-M", rt.CurrentNodeId);
        Assert.IsTrue(rt.AvailableChoices().Count > 0);

        rt.Choose("M2");
        Assert.AreEqual("M2", rt.CurrentNodeId);
        Assert.IsNotNull(rt.Current);

        rt.AdvanceLine();
        Assert.AreEqual("HUB-M", rt.CurrentNodeId);
        Assert.IsTrue(rt.AvailableChoices().Any(c => c.target == "M2"));
    }

    [Test]
    public void OnceTopic_DisappearsAfterHeard()
    {
        DialogueRuntime rt = new DialogueRuntime(DialogueLibrary.ForLevel(1));
        rt.Begin();
        rt.AdvanceLine(); // to hub

        rt.Choose("M1");
        rt.AdvanceLine(); // back to hub

        var choices = rt.AvailableChoices();
        Assert.IsFalse(choices.Any(c => c.target == "M1"), "once topic M1 should no longer be available");
    }

    [Test]
    public void ManualTutorialAdvance_BlockedUntilEveryLessonAndDrillDone()
    {
        DialogueRuntime rt = new DialogueRuntime(DialogueLibrary.ForLevel(0, manualMode: true));
        rt.Begin();
        rt.AdvanceLine();
        rt.AdvanceLine(); // to HUB-T

        Assert.AreEqual("HUB-T", rt.CurrentNodeId);
        Assert.IsFalse(rt.AvailableChoices().Any(c => c.target == "T-ADV"),
            "T-ADV should be locked before any lesson");

        // Driving lesson is a branch: pick the topic, take a sub-choice back to the hub.
        rt.Choose("T2");
        Assert.IsTrue(rt.HasHeard("T2"));
        rt.Choose("T2b");
        rt.AdvanceLine(); // T2b line exhausted -> back to hub
        Assert.AreEqual("HUB-T", rt.CurrentNodeId);
        Assert.IsFalse(rt.AvailableChoices().Any(c => c.target == "T-ADV"),
            "still locked after only the driving lesson");

        VisitLineTopic(rt, "T3");   // stops & passengers
        VisitLineTopic(rt, "T4");   // fares & coins
        VisitEventTopic(rt, "T5");  // repair drill
        VisitEventTopic(rt, "T6");  // refuel drill

        var choices = rt.AvailableChoices();
        string heard = string.Join(",", rt.HeardNodes);
        Assert.IsTrue(choices.Any(c => c.target == "T-ADV"),
            $"T-ADV should unlock once every lesson and both drills are done (heard: {heard})");
    }

    [Test]
    public void AutomationTutorialAdvance_BlockedUntilEveryLessonAndDrillDone()
    {
        DialogueRuntime rt = new DialogueRuntime(DialogueLibrary.ForLevel(0, manualMode: false));
        rt.Begin();
        rt.AdvanceLine();
        rt.AdvanceLine(); // to HUB-TA

        Assert.AreEqual("HUB-TA", rt.CurrentNodeId);
        Assert.IsFalse(rt.AvailableChoices().Any(c => c.target == "TA-ADV"),
            "TA-ADV should be locked before any lesson");

        // Driving lesson is a branch.
        rt.Choose("TA2");
        rt.Choose("TA2b");
        rt.AdvanceLine(); // back to hub
        Assert.IsFalse(rt.AvailableChoices().Any(c => c.target == "TA-ADV"));

        VisitLineTopic(rt, "TA3");   // passengers
        VisitLineTopic(rt, "TA4");   // fares
        VisitLineTopic(rt, "TA5");   // sensors
        VisitEventTopic(rt, "TA6");  // repair drill
        VisitEventTopic(rt, "TA7");  // refuel drill

        Assert.IsTrue(rt.AvailableChoices().Any(c => c.target == "TA-ADV"),
            "TA-ADV should unlock once every coding lesson and both drills are done");
    }

    // Choose a plain multi-line topic and advance until it routes back to the hub.
    static void VisitLineTopic(DialogueRuntime rt, string id)
    {
        rt.Choose(id);
        while (rt.CurrentNodeId == id)
            rt.AdvanceLine();
    }

    // Choose an event topic, advance to its pending event, then clear it back to the hub.
    static void VisitEventTopic(DialogueRuntime rt, string id)
    {
        rt.Choose(id);
        while (!rt.IsAwaitingEventClear)
            rt.AdvanceLine();
        rt.ClearEvent();
    }

    [Test]
    public void AffinityAccumulates_OnHeartLinesOnly()
    {
        DialogueRuntime rt = new DialogueRuntime(DialogueLibrary.ForLevel(1));
        rt.Begin();
        rt.AdvanceLine(); // hub

        rt.Choose("M2"); // no affinity
        Assert.AreEqual(0, rt.Affinity);
        rt.AdvanceLine(); // back to hub

        rt.Choose("M3"); // (+♥)
        Assert.AreEqual(1, rt.Affinity);
    }

    [Test]
    public void NormalDrive_NeverSurfacesRevealLine()
    {
        for (int level = 0; level <= 5; level++)
        {
            DialogueConversation convo = DialogueLibrary.ForLevel(level);
            DialogueRuntime rt = new DialogueRuntime(convo);
            rt.Begin();

            int steps = 0;
            while (!rt.IsFinished && steps < 200)
            {
                steps++;
                if (rt.Current != null)
                {
                    Assert.IsFalse(rt.Current.isReveal,
                        $"Level {level}: non-reveal path surfaced a reveal line at node {rt.CurrentNodeId}");
                }

                if (rt.AvailableChoices().Count > 0)
                {
                    // Prefer advance/locked choices, then unvisited topics, then any.
                    IReadOnlyList<DialogueChoice> choices = rt.AvailableChoices();
                    DialogueChoice choice =
                        choices.FirstOrDefault(c => c.target.EndsWith("-ADV") || c.target == "T-ADV") ??
                        choices.FirstOrDefault(c => !rt.HasHeard(c.target)) ??
                        choices.First();
                    rt.Choose(choice.target);
                }
                else if (rt.IsAwaitingEventClear)
                {
                    rt.ClearEvent();
                }
                else
                {
                    rt.AdvanceLine();
                }
            }

            Assert.IsTrue(rt.IsFinished, $"Level {level} should finish within step limit");
        }
    }
}
