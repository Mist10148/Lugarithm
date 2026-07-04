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

    static AgentSim NewWideSim()
    {
        // A two-cell-wide road so a lane change has somewhere to slide.
        GridModel grid = GridModel.Parse(new[]
        {
            "#####",
            "#S..#",
            "#...#",
            "#..D#",
            "#####",
        }, out List<string> errors);
        CollectionAssert.IsEmpty(errors);
        return new AgentSim(grid, new FareTable(), startFacing: 1); // facing East
    }

    [Test]
    public void MoveRight_StrafesPerpendicular_WithoutChangingFacing()
    {
        var sim = NewWideSim(); // at (1,1) facing East

        var result = sim.Apply("moveRight"); // right of East = South

        Assert.IsFalse(result.Blocked);
        Assert.AreEqual(new Vector2Int(1, 2), sim.Position); // slid one lane over
        Assert.AreEqual(1, sim.Facing);                       // heading unchanged — no turn
    }

    [Test]
    public void MoveLeft_IntoAWall_Bumps_WithoutTurning()
    {
        var sim = NewWideSim(); // at (1,1) facing East

        var result = sim.Apply("moveLeft"); // left of East = North → wall

        Assert.IsTrue(result.Blocked);
        StringAssert.Contains("lane", result.Warning);
        Assert.AreEqual(new Vector2Int(1, 1), sim.Position); // didn't move
        Assert.AreEqual(1, sim.Facing);                       // heading unchanged
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

    // -------------------------------------------------------------------------
    // Navigation-sensor reporters

    [Test]
    public void FacingReporter_MatchesCurrentHeading()
    {
        var sim = NewSim(out _);
        Assert.AreEqual(1, sim.ReadReporter("facing", new List<Value>()).AsInt());

        sim.Apply("turnLeft");
        Assert.AreEqual(0, sim.ReadReporter("facing", new List<Value>()).AsInt());
    }

    [Test]
    public void DestinationPositionReporter_ReturnsDestCell()
    {
        var sim = NewSim(out GridModel grid);
        Value v = sim.ReadReporter("destinationPosition", new List<Value>());
        Assert.AreEqual(ValueKind.Tuple, v.Kind);
        var arr = (Value[])v.Obj;
        Assert.AreEqual(grid.DestPos.x, arr[0].AsInt());
        Assert.AreEqual(grid.DestPos.y, arr[1].AsInt());
    }

    [Test]
    public void DirectionTo_ReturnsFirstStepHeading()
    {
        var sim = NewSim(out GridModel grid);
        // From S(1,1) facing East to D(3,3): path is (1,1)->(2,1)->(3,1)->(3,2)->(3,3)
        Assert.AreEqual(1, sim.ReadReporter("directionTo",
            new List<Value> { Value.Int(grid.DestPos.x), Value.Int(grid.DestPos.y) }).AsInt());

        // Same cell returns None.
        Assert.AreEqual(ValueKind.None, sim.ReadReporter("directionTo",
            new List<Value> { Value.Int(sim.Position.x), Value.Int(sim.Position.y) }).Kind);
    }

    [Test]
    public void DistanceTo_ReturnsShortestPathLength()
    {
        var sim = NewSim(out GridModel grid);
        // From (1,1) to (3,3): 4 cells inclusive, so distance = 4.
        Assert.AreEqual(4, sim.ReadReporter("distanceTo",
            new List<Value> { Value.Int(grid.DestPos.x), Value.Int(grid.DestPos.y) }).AsInt());
    }

    [Test]
    public void StoryDropoffArmed_QueryAndPositionTrackStoryState()
    {
        GridModel grid = GridModel.Parse(new[]
        {
            "################",
            "#S............D#",
            "################",
        }, out List<string> errors);
        CollectionAssert.IsEmpty(errors);

        var sim = new AgentSim(grid, new FareTable(), startFacing: 1);
        Assert.IsFalse(sim.EvaluateQuery("storyDropoffArmed"));
        Assert.AreEqual(ValueKind.None, sim.ReadReporter("storyDropoffPosition", new List<Value>()).Kind);

        sim.ArmStoryDropoff(new Vector2Int(12, 1));
        Assert.IsTrue(sim.EvaluateQuery("storyDropoffArmed"));

        Value pos = sim.ReadReporter("storyDropoffPosition", new List<Value>());
        Assert.AreEqual(ValueKind.Tuple, pos.Kind);
        var arr = (Value[])pos.Obj;
        Assert.AreEqual(12, arr[0].AsInt());
        Assert.AreEqual(1, arr[1].AsInt());
    }
}
