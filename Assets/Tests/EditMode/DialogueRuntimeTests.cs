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
    public void TutorialAdvance_BlockedUntilDrivingAndFareHeard()
    {
        DialogueRuntime rt = new DialogueRuntime(DialogueLibrary.ForLevel(0));
        rt.Begin();
        rt.AdvanceLine();
        rt.AdvanceLine(); // to HUB-T

        var choices = rt.AvailableChoices();
        Assert.IsFalse(choices.Any(c => c.target == "T-ADV"), "T-ADV should be locked before tutorials");

        Assert.AreEqual("HUB-T", rt.CurrentNodeId, "Should be at hub before choosing T2");
        rt.Choose("T2");
        Assert.AreEqual("T2", rt.CurrentNodeId, "Should have jumped to T2");
        Assert.IsTrue(rt.HasHeard("T2"), "T2 should be heard after choosing it");

        rt.Choose("T2a");
        rt.AdvanceLine(); // T2a -> T2b
        rt.AdvanceLine(); // T2b line exhausted, driving-tutorial event pending
        rt.ClearEvent();  // resolves back to hub

        choices = rt.AvailableChoices();
        Assert.IsFalse(choices.Any(c => c.target == "T-ADV"), "T-ADV still locked after only T2");

        rt.Choose("T3");
        Assert.IsTrue(rt.HasHeard("T2") && rt.HasHeard("T3"), "T2 and T3 should both be heard now");
        rt.AdvanceLine();
        rt.AdvanceLine(); // T3 lines exhausted, fare-tutorial event pending
        rt.ClearEvent();  // resolves back to hub

        choices = rt.AvailableChoices();
        string heard = string.Join(",", rt.HeardNodes);
        string available = string.Join(",", choices.Select(c => c.target));
        Assert.IsTrue(choices.Any(c => c.target == "T-ADV"),
            $"T-ADV should unlock after T2 + T3 (heard: {heard}; available: {available})");
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
