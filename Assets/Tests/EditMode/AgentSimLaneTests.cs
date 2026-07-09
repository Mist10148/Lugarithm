using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>Two-lane road mode: the procedural trunk is one grid cell wide but
/// carries a left and a right lane as sub-cell state on the AgentSim.</summary>
public class AgentSimLaneTests
{
    // A single-cell-wide east-west road — exactly the procedural trunk shape.
    static AgentSim NewLaneSim()
    {
        GridModel grid = GridModel.Parse(new[]
        {
            "#######",
            "#S...D#",
            "#######",
        }, out List<string> errors);
        CollectionAssert.IsEmpty(errors);

        var sim = new AgentSim(grid, new FareTable(), startFacing: 1) // East
        {
            LaneMode = true,
            TrafficEnabled = true,
            TrafficBlocksMovement = true,
        };
        return sim;
    }

    // Facing East (1): right lane = South (2), left lane = North (0).
    const int EastRight = 2;
    const int EastLeft  = 0;

    [Test]
    public void StartsInRightLane_OfStartFacing()
    {
        var sim = NewLaneSim();
        Assert.AreEqual(1, sim.LaneSide);
        Assert.AreEqual(EastRight, sim.LaneCardinal);
    }

    [Test]
    public void MoveLeft_FlipsLaneWithoutChangingPosition_AndBumpsWhenAlreadyLeft()
    {
        var sim = NewLaneSim();
        Vector2Int start = sim.Position;

        AgentActionResult first = sim.Apply("moveLeft");
        Assert.IsFalse(first.Blocked);
        Assert.AreEqual(start, sim.Position, "a lane change must not move the cell");
        Assert.AreEqual(EastLeft, sim.LaneCardinal);
        Assert.AreEqual(EastRight, first.LaneBefore);
        Assert.AreEqual(EastLeft, first.LaneAfter);

        AgentActionResult second = sim.Apply("moveLeft");
        Assert.IsTrue(second.Blocked);
        StringAssert.Contains("already in the left lane", second.Warning);

        AgentActionResult back = sim.Apply("moveRight");
        Assert.IsFalse(back.Blocked);
        Assert.AreEqual(EastRight, sim.LaneCardinal);
    }

    [Test]
    public void CarInFront_IsLaneAware()
    {
        var sim = NewLaneSim();
        Vector2Int front = sim.Position + new Vector2Int(1, 0);

        // Car in the LEFT lane of the cell ahead — my (right) lane is clear.
        sim.SetTrafficOccupancy(new Dictionary<Vector2Int, int> { [front] = 1 << EastLeft });
        Assert.IsFalse(sim.EvaluateQuery("carInFront"));
        Assert.IsFalse(sim.EvaluateQuery("leftIsClear"),
            "left lane holds a car ahead, so it is not clear");

        // Car in MY lane ahead.
        sim.SetTrafficOccupancy(new Dictionary<Vector2Int, int> { [front] = 1 << EastRight });
        Assert.IsTrue(sim.EvaluateQuery("carInFront"));
        Assert.IsTrue(sim.EvaluateQuery("leftIsClear"));
        Assert.IsFalse(sim.EvaluateQuery("rightIsClear"), "already in the right lane");
    }

    [Test]
    public void AvoidTraffic_SwitchesToTheClearLane_OrWaitsWhenBoxedIn()
    {
        var sim = NewLaneSim();
        Vector2Int front = sim.Position + new Vector2Int(1, 0);

        sim.SetTrafficOccupancy(new Dictionary<Vector2Int, int> { [front] = 1 << EastRight });
        AgentActionResult dodge = sim.Apply("avoidTraffic");
        Assert.AreEqual("moveLeft", dodge.Action);
        Assert.AreEqual(EastLeft, sim.LaneCardinal);
        Assert.IsFalse(dodge.Blocked);

        // Both lanes blocked ahead → wait, boxed in.
        sim.SetTrafficOccupancy(new Dictionary<Vector2Int, int>
        {
            [front] = (1 << EastLeft) | (1 << EastRight),
        });
        AgentActionResult boxed = sim.Apply("avoidTraffic");
        Assert.AreEqual("wait", boxed.Action);
        StringAssert.Contains("boxed in", boxed.Warning);

        // Clear road while still in the left lane → merges back home.
        sim.ClearTraffic();
        AgentActionResult merge = sim.Apply("avoidTraffic");
        Assert.AreEqual("moveRight", merge.Action);
        Assert.IsFalse(merge.Blocked);
        Assert.AreEqual(EastRight, sim.LaneCardinal, "must return to the home (right) lane");
    }

    [Test]
    public void AvoidTraffic_HoldsLeftLane_UntilHomeLaneClears()
    {
        var sim = NewLaneSim();
        Vector2Int front = sim.Position + new Vector2Int(1, 0);

        // Dodge into the left lane around a car in my lane ahead.
        sim.SetTrafficOccupancy(new Dictionary<Vector2Int, int> { [front] = 1 << EastRight });
        Assert.AreEqual("moveLeft", sim.Apply("avoidTraffic").Action);
        Assert.AreEqual(EastLeft, sim.LaneCardinal);

        // The overtaken car now sits in the home lane right beside me → hold the left lane.
        sim.SetTrafficOccupancy(new Dictionary<Vector2Int, int> { [sim.Position] = 1 << EastRight });
        AgentActionResult hold = sim.Apply("avoidTraffic");
        Assert.AreEqual("wait", hold.Action);
        StringAssert.Contains("merge back", hold.Warning);
        Assert.AreEqual(EastLeft, sim.LaneCardinal, "must not merge into an occupied lane");

        // Home lane clears → merge back.
        sim.ClearTraffic();
        AgentActionResult merge = sim.Apply("avoidTraffic");
        Assert.AreEqual("moveRight", merge.Action);
        Assert.AreEqual(EastRight, sim.LaneCardinal);
    }

    [Test]
    public void AvoidTraffic_ClearRoadInHomeLane_IsAQuietWait()
    {
        var sim = NewLaneSim();
        AgentActionResult idle = sim.Apply("avoidTraffic");
        Assert.AreEqual("wait", idle.Action);
        Assert.IsNull(idle.Warning);
        Assert.AreEqual(EastRight, sim.LaneCardinal);
    }

    [Test]
    public void MoveForward_BumpsOnSameLaneCar_ButNotOtherLane()
    {
        var sim = NewLaneSim();
        Vector2Int start = sim.Position;
        Vector2Int front = start + new Vector2Int(1, 0);

        // Car in MY lane ahead → bump, no pass-through.
        sim.SetTrafficOccupancy(new Dictionary<Vector2Int, int> { [front] = 1 << EastRight });
        AgentActionResult bump = sim.Apply("moveForward");
        Assert.IsTrue(bump.Blocked);
        StringAssert.Contains("traffic ahead", bump.Warning);
        Assert.AreEqual(start, sim.Position, "must not drive through a car");

        // Car only in the ONCOMING lane ahead → my lane is open.
        sim.SetTrafficOccupancy(new Dictionary<Vector2Int, int> { [front] = 1 << EastLeft });
        AgentActionResult pass = sim.Apply("moveForward");
        Assert.IsFalse(pass.Blocked);
        Assert.AreEqual(front, sim.Position);
    }

    [Test]
    public void FlushPendingMoves_DropsQueuedMacroPath()
    {
        var sim = NewLaneSim();
        sim.Apply("driveToDestination");
        Assert.IsTrue(sim.HasPendingMoves);

        sim.FlushPendingMoves();
        Assert.IsFalse(sim.HasPendingMoves);
    }

    [Test]
    public void Turning_KeepsTheRelativeLane()
    {
        var sim = NewLaneSim();
        Assert.AreEqual(EastRight, sim.LaneCardinal);   // right of East = South

        AgentActionResult turn = sim.Apply("turnLeft"); // now facing North
        Assert.AreEqual(1, sim.LaneSide, "still the right-hand lane");
        Assert.AreEqual(1, sim.LaneCardinal, "right of North = East");
        Assert.AreEqual(1, turn.LaneAfter);
    }

    [Test]
    public void LegacySetTrafficCells_BlocksBothLanes()
    {
        var sim = NewLaneSim();
        Vector2Int front = sim.Position + new Vector2Int(1, 0);
        sim.SetTrafficCells(new[] { front });

        Assert.IsTrue(sim.EvaluateQuery("carInFront"));
        Assert.IsFalse(sim.EvaluateQuery("leftIsClear"));
    }

    [Test]
    public void LaneModeOff_KeepsLegacyCellStrafe()
    {
        GridModel grid = GridModel.Parse(new[]
        {
            "#####",
            "#S..#",
            "#...#",
            "#..D#",
            "#####",
        }, out List<string> errors);
        CollectionAssert.IsEmpty(errors);
        var sim = new AgentSim(grid, new FareTable(), startFacing: 1);

        AgentActionResult strafe = sim.Apply("moveRight");
        Assert.IsFalse(strafe.Blocked);
        Assert.AreEqual(new Vector2Int(1, 2), sim.Position,
            "outside lane mode moveRight must still strafe a full cell (mazes)");
        Assert.AreEqual(-1, strafe.LaneAfter);
    }
}
