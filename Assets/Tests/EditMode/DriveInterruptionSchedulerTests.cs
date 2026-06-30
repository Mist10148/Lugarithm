using NUnit.Framework;

public class DriveInterruptionSchedulerTests
{
    [Test]
    public void ProgressionGates_FireFlowThenCrate_AndOnlyTwice()
    {
        var scheduler = new DriveInterruptionScheduler(123);

        Assert.IsFalse(scheduler.TryStartProgression(0f, out _));

        Assert.IsTrue(scheduler.TryStartProgression(1f, out TownPuzzleKind first));
        Assert.AreEqual(TownPuzzleKind.FlowConnect, first);

        Assert.IsTrue(scheduler.TryStartProgression(1f, out TownPuzzleKind second));
        Assert.AreEqual(TownPuzzleKind.CrateStack, second);

        Assert.IsFalse(scheduler.TryStartProgression(1f, out TownPuzzleKind third));
        Assert.AreEqual(TownPuzzleKind.None, third);
        Assert.IsTrue(scheduler.AllProgressionGatesDone);
    }

    [Test]
    public void ForceNextProgression_CompletesMissedGatesInOrder()
    {
        var scheduler = new DriveInterruptionScheduler(456);

        Assert.IsTrue(scheduler.TryForceNextProgression(out TownPuzzleKind first));
        Assert.AreEqual(TownPuzzleKind.FlowConnect, first);

        Assert.IsTrue(scheduler.TryForceNextProgression(out TownPuzzleKind second));
        Assert.AreEqual(TownPuzzleKind.CrateStack, second);

        Assert.IsFalse(scheduler.TryForceNextProgression(out _));
    }

    [Test]
    public void Repairs_GuaranteeTwoBeforeRareExtras()
    {
        var scheduler = new DriveInterruptionScheduler(789);

        Assert.IsTrue(scheduler.TryStartRepair(1f, 1f));
        Assert.IsTrue(scheduler.TryStartRepair(1f, 1f));
        Assert.AreEqual(2, scheduler.CompletedRepairs);
        Assert.IsTrue(scheduler.GuaranteedRepairsDone);

        Assert.IsFalse(scheduler.TryStartRepair(1.05f, 0f), "extra repairs obey cooldown");
        Assert.IsFalse(scheduler.TryStartRepair(1.20f, 1f), "extra repairs are rare");
        Assert.IsTrue(scheduler.TryStartRepair(1.20f, 0f), "a rare roll can add an extra");
        Assert.AreEqual(3, scheduler.CompletedRepairs);
    }

    [Test]
    public void Refuel_OnlyFiresAtEmptyTank()
    {
        var scheduler = new DriveInterruptionScheduler(1);

        Assert.IsFalse(scheduler.ShouldRefuel(0.01f));
        Assert.IsTrue(scheduler.ShouldRefuel(0f));
        Assert.IsTrue(scheduler.ShouldRefuel(-0.1f));
    }
}
