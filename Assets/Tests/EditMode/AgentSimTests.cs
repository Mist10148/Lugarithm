using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>EditMode tests for the deterministic grid jeepney simulation.</summary>
public class AgentSimTests
{
    static AgentSim NewSim(out GridModel grid)
    {
        grid = GridModel.Parse(new[]
        {
            "#####",
            "#S.P#",
            "###.#",
            "###D#",
            "#####",
        }, out List<string> errors);
        CollectionAssert.IsEmpty(errors);

        return new AgentSim(grid, new FareTable(), startFacing: 1); // facing East
    }

    // -------------------------------------------------------------------------

    [Test]
    public void MoveForward_AdvancesOneCell()
    {
        var sim = NewSim(out _);
        var result = sim.Apply("moveForward");

        Assert.IsFalse(result.Blocked);
        Assert.AreEqual(new Vector2Int(2, 1), sim.Position);
        Assert.AreEqual(1, sim.StepsUsed);
    }

    [Test]
    public void MoveForward_IntoAWall_IsABlockedNoOp_NotAnError()
    {
        var sim = NewSim(out _);
        sim.Apply("turnLeft"); // face North into the wall
        var result = sim.Apply("moveForward");

        Assert.IsTrue(result.Blocked);
        StringAssert.Contains("wall", result.Warning);
        Assert.AreEqual(new Vector2Int(1, 1), sim.Position);
        Assert.AreEqual(2, sim.StepsUsed); // bump still costs a step
    }

    [Test]
    public void Turns_CycleAllFourFacings()
    {
        var sim = NewSim(out _);

        sim.Apply("turnLeft");
        Assert.AreEqual(0, sim.Facing); // E → N

        sim.Apply("turnLeft");
        Assert.AreEqual(3, sim.Facing); // N → W

        sim.Apply("turnRight");
        Assert.AreEqual(0, sim.Facing); // W → N
    }

    [Test]
    public void PickUp_OnAStop_BoardsThePassenger_OnceOnly()
    {
        var sim = NewSim(out _);
        sim.Apply("moveForward");
        sim.Apply("moveForward"); // now on P (3,1)

        var first = sim.Apply("pickUp");
        Assert.IsTrue(first.PickedUp);
        Assert.AreEqual(1, sim.PassengersAboard);
        Assert.AreEqual(0, sim.RemainingWaiting);

        var second = sim.Apply("pickUp");
        Assert.IsFalse(second.PickedUp);
        StringAssert.Contains("no passenger", second.Warning);
    }

    [Test]
    public void CollectFare_ChargesBaseFarePerBoardedPassenger()
    {
        var sim = NewSim(out _);
        sim.Apply("moveForward");
        sim.Apply("moveForward");
        sim.Apply("pickUp");

        var collect = sim.Apply("collectFare");
        Assert.AreEqual(13, collect.FareCollected);
        Assert.AreEqual(13, sim.FaresCollected);

        var again = sim.Apply("collectFare");
        Assert.AreEqual(0, again.FareCollected);
        StringAssert.Contains("no fares", again.Warning);
    }

    [Test]
    public void DropOff_OnlyDeliversAtTheDestination()
    {
        var sim = NewSim(out _);
        sim.Apply("moveForward");
        sim.Apply("moveForward");
        sim.Apply("pickUp");

        var early = sim.Apply("dropOff");
        Assert.IsFalse(early.DroppedOff);
        Assert.AreEqual(1, sim.PassengersAboard);

        sim.Apply("turnRight");   // face South
        sim.Apply("moveForward"); // (3,2)
        sim.Apply("moveForward"); // (3,3) = D

        var final = sim.Apply("dropOff");
        Assert.IsTrue(final.DroppedOff);
        Assert.AreEqual(1, final.DeliveredCount);
        Assert.AreEqual(0, sim.PassengersAboard);
        Assert.AreEqual(1, sim.PassengersDelivered);
    }

    [Test]
    public void Queries_DescribeTheSurroundings()
    {
        var sim = NewSim(out _);

        Assert.IsTrue(sim.EvaluateQuery("frontIsClear"));   // (2,1) road
        Assert.IsFalse(sim.EvaluateQuery("leftIsClear"));   // (1,0) wall
        Assert.IsFalse(sim.EvaluateQuery("rightIsClear"));  // (1,2) wall
        Assert.IsFalse(sim.EvaluateQuery("atStop"));
        Assert.IsFalse(sim.EvaluateQuery("atDestination"));

        sim.Apply("moveForward");
        sim.Apply("moveForward");
        Assert.IsTrue(sim.EvaluateQuery("atStop"));
    }

    [Test]
    public void Reset_RestoresTheWholeWorld()
    {
        var sim = NewSim(out var grid);
        sim.Apply("moveForward");
        sim.Apply("moveForward");
        sim.Apply("pickUp");
        sim.Apply("collectFare");

        sim.Reset();

        Assert.AreEqual(grid.StartPos, sim.Position);
        Assert.AreEqual(1, sim.Facing);
        Assert.AreEqual(0, sim.StepsUsed);
        Assert.AreEqual(0, sim.PassengersAboard);
        Assert.AreEqual(0, sim.FaresCollected);
        Assert.AreEqual(1, sim.RemainingWaiting);
    }

    [Test]
    public void IsWin_RequiresDeliveryWhenThePuzzleSaysSo()
    {
        var def = new AutomationPuzzleDefinition { requireAllPassengersDelivered = true };
        var sim = NewSim(out _);

        // Drive straight to D, ignoring the passenger.
        sim.Apply("moveForward");
        sim.Apply("moveForward");
        sim.Apply("turnRight");
        sim.Apply("moveForward");
        sim.Apply("moveForward");

        Assert.IsFalse(sim.IsWin(def));
        StringAssert.Contains("waiting", sim.DescribeGoalGap(def));

        def.requireAllPassengersDelivered = false;
        Assert.IsTrue(sim.IsWin(def));
    }

    [Test]
    public void EndlessKeepDriving_QueuesOnlyAShortHop()
    {
        GridModel grid = GridModel.Parse(new[]
        {
            "################",
            "#S............D#",
            "################",
        }, out List<string> errors);
        CollectionAssert.IsEmpty(errors);

        var sim = new AgentSim(grid, new FareTable(), startFacing: 1) { EndlessRoute = true };
        sim.Apply("keepDriving");

        Assert.Greater(sim.PendingMoveCount, 0);
        Assert.LessOrEqual(sim.PendingMoveCount, 4,
            "endless cruise macros must yield often enough for stream-ahead generation");
    }

    [Test]
    public void EndlessDriveToDropoff_QueuesOnlyAShortHop()
    {
        GridModel grid = GridModel.Parse(new[]
        {
            "################",
            "#S............D#",
            "################",
        }, out List<string> errors);
        CollectionAssert.IsEmpty(errors);

        var sim = new AgentSim(grid, new FareTable(), startFacing: 1) { EndlessRoute = true };
        sim.ArmStoryDropoff(new Vector2Int(12, 1));
        sim.Apply("driveToDropoff");

        Assert.Greater(sim.PendingMoveCount, 0);
        Assert.LessOrEqual(sim.PendingMoveCount, 4,
            "story drop-off navigation must not monopolize the execution loop");
    }
}
