using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>Two-lane road mode: the procedural trunk is one grid cell wide but
/// carries a left and a right lane as sub-cell state on the AgentSim. Lane
/// changes are merging diagonals (one cell forward) on open road, avoidTraffic
/// triggers early to keep a 1-cell gap, and merges scan the oncoming lane.</summary>
public class AgentSimLaneTests
{
    // A single-cell-wide east-west road — exactly the procedural trunk shape.
    // Long enough for the 4-cell oncoming scan to fit inside the road.
    static AgentSim NewLaneSim()
    {
        GridModel grid = GridModel.Parse(new[]
        {
            "############",
            "#S........D#",
            "############",
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

    static readonly Vector2Int East = new Vector2Int(1, 0);

    [Test]
    public void StartsInRightLane_OfStartFacing()
    {
        var sim = NewLaneSim();
        Assert.AreEqual(1, sim.LaneSide);
        Assert.AreEqual(EastRight, sim.LaneCardinal);
    }

    [Test]
    public void MoveLeft_MergesDiagonallyForward_AndBumpsWhenAlreadyLeft()
    {
        var sim = NewLaneSim();
        Vector2Int start = sim.Position;

        AgentActionResult first = sim.Apply("moveLeft");
        Assert.IsFalse(first.Blocked);
        Assert.AreEqual(start + East, sim.Position,
            "an open-road lane change merges one cell forward (a real diagonal)");
        Assert.AreEqual(EastLeft, sim.LaneCardinal);
        Assert.AreEqual(EastRight, first.LaneBefore);
        Assert.AreEqual(EastLeft, first.LaneAfter);
        Assert.AreEqual(start, first.From);
        Assert.AreEqual(start + East, first.To);

        AgentActionResult second = sim.Apply("moveLeft");
        Assert.IsTrue(second.Blocked);
        StringAssert.Contains("already in the left lane", second.Warning);

        AgentActionResult back = sim.Apply("moveRight");
        Assert.IsFalse(back.Blocked);
        Assert.AreEqual(EastRight, sim.LaneCardinal);
        Assert.AreEqual(start + East + East, sim.Position, "merging back also advances");
    }

    [Test]
    public void LaneSwitch_FallsBackToInPlace_AtWall()
    {
        // Facing East with a wall directly ahead: the diagonal is impossible, so
        // the lane switch happens in place (e.g. adjusting position at a corner).
        GridModel grid = GridModel.Parse(new[]
        {
            "###",
            "#S#",
            "#D#",
            "###",
        }, out List<string> errors);
        CollectionAssert.IsEmpty(errors);
        var sim = new AgentSim(grid, new FareTable(), startFacing: 1)
        {
            LaneMode = true,
            TrafficEnabled = true,
        };
        Vector2Int start = sim.Position;

        AgentActionResult flip = sim.Apply("moveLeft");
        Assert.IsFalse(flip.Blocked);
        Assert.AreEqual(start, sim.Position, "no road ahead — switch in place");
        Assert.AreEqual(EastLeft, sim.LaneCardinal);
    }

    [Test]
    public void CarInFront_IsLaneAware()
    {
        var sim = NewLaneSim();
        Vector2Int front = sim.Position + East;

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
    public void CarInFront_TriggersAtTwoCells_ForTheOneCellGap()
    {
        var sim = NewLaneSim();

        // Two cells ahead: still "in front" — dodging now keeps a 1-cell gap.
        sim.SetTrafficOccupancy(new Dictionary<Vector2Int, int>
        {
            [sim.Position + East + East] = 1 << EastRight,
        });
        Assert.IsTrue(sim.EvaluateQuery("carInFront"));

        // Three cells ahead: not yet.
        sim.SetTrafficOccupancy(new Dictionary<Vector2Int, int>
        {
            [sim.Position + East + East + East] = 1 << EastRight,
        });
        Assert.IsFalse(sim.EvaluateQuery("carInFront"));
    }

    [Test]
    public void AvoidTraffic_SwitchesToTheClearLane_OrWaitsWhenBoxedIn()
    {
        var sim = NewLaneSim();
        Vector2Int start = sim.Position;
        Vector2Int front = start + East;

        sim.SetTrafficOccupancy(new Dictionary<Vector2Int, int> { [front] = 1 << EastRight });
        AgentActionResult dodge = sim.Apply("avoidTraffic");
        Assert.AreEqual("moveLeft", dodge.Action);
        Assert.AreEqual(EastLeft, sim.LaneCardinal);
        Assert.IsFalse(dodge.Blocked);
        Assert.AreEqual(front, sim.Position, "the dodge merges forward alongside the car");

        // Fresh sim, both lanes blocked ahead → wait, boxed in.
        var boxedSim = NewLaneSim();
        boxedSim.SetTrafficOccupancy(new Dictionary<Vector2Int, int>
        {
            [boxedSim.Position + East] = (1 << EastLeft) | (1 << EastRight),
        });
        AgentActionResult boxed = boxedSim.Apply("avoidTraffic");
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
    public void AvoidTraffic_DodgesEarly_KeepingOneCellGap()
    {
        var sim = NewLaneSim();
        Vector2Int start = sim.Position;

        // Car TWO cells ahead in my lane: dodge now, before ever reaching its bumper.
        sim.SetTrafficOccupancy(new Dictionary<Vector2Int, int>
        {
            [start + East + East] = 1 << EastRight,
        });
        AgentActionResult dodge = sim.Apply("avoidTraffic");
        Assert.AreEqual("moveLeft", dodge.Action);
        Assert.IsFalse(dodge.Blocked);
        Assert.AreEqual(start + East, sim.Position);
        Assert.AreEqual(EastLeft, sim.LaneCardinal);
    }

    [Test]
    public void AvoidTraffic_WaitsForApproachingOncomingCar()
    {
        var sim = NewLaneSim();
        Vector2Int start = sim.Position;
        Vector2Int carAhead = start + East + East;   // same-lane car, 2 cells out

        // An oncoming car 4 cells down the left lane is heading our way — merging
        // into it would cut it off, so hold the lane instead.
        sim.SetTrafficOccupancy(new Dictionary<Vector2Int, int>
        {
            [carAhead] = 1 << EastRight,
            [start + East * 4] = 1 << EastLeft,
        });
        AgentActionResult hold = sim.Apply("avoidTraffic");
        Assert.AreEqual("wait", hold.Action);
        StringAssert.Contains("boxed in", hold.Warning);
        Assert.AreEqual(start, sim.Position);

        // Same oncoming car beyond the scan window → safe to pull out.
        sim.SetTrafficOccupancy(new Dictionary<Vector2Int, int>
        {
            [carAhead] = 1 << EastRight,
            [start + East * 5] = 1 << EastLeft,
        });
        AgentActionResult dodge = sim.Apply("avoidTraffic");
        Assert.AreEqual("moveLeft", dodge.Action);
        Assert.IsFalse(dodge.Blocked);
    }

    [Test]
    public void AvoidTraffic_HoldsLeftLane_UntilHomeLaneClears()
    {
        var sim = NewLaneSim();
        Vector2Int front = sim.Position + East;

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
        Vector2Int front = start + East;

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
    public void DriveMacro_StopsOneCellShortOfTraffic()
    {
        var sim = NewLaneSim();
        Vector2Int start = sim.Position;

        // Car in my lane 3 cells down the road: the planned path must stop early
        // (keeping the 1-cell gap) instead of ramming its bumper and bumping.
        sim.SetTrafficOccupancy(new Dictionary<Vector2Int, int>
        {
            [start + East * 3] = 1 << EastRight,
        });

        sim.Apply("driveToDestination");
        Assert.IsTrue(sim.HasPendingMoves);
        while (sim.HasPendingMoves)
        {
            AgentActionResult move = sim.Apply(sim.DequeueMove());
            Assert.IsFalse(move.Blocked, "a traffic-aware plan never bumps");
        }
        Assert.AreEqual(start + East, sim.Position,
            "stop with one empty cell between us and the car");
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
        Vector2Int front = sim.Position + East;
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

    // -------------------------------------------------------------------------
    // Post-win free roam: routeComplete() un-latches so cruise loops keep driving.

    static AgentSim NewStoryLegSim()
    {
        GridModel grid = GridModel.Parse(new[]
        {
            "############",
            "#S........D#",
            "############",
        }, out List<string> errors);
        CollectionAssert.IsEmpty(errors);
        return new AgentSim(grid, new FareTable(), startFacing: 1)
        {
            EndlessRoute = true,
            StoryLegMode = true,
        };
    }

    [Test]
    public void RouteComplete_UnlatchesInFreeRoamCruise()
    {
        var sim = NewStoryLegSim();
        sim.ArmStoryDropoff(sim.Position);
        sim.Apply("dropOff");
        Assert.IsTrue(sim.StoryDelivered);
        Assert.IsTrue(sim.EvaluateQuery("routeComplete"));

        sim.FreeRoamCruise = true;
        Assert.IsFalse(sim.EvaluateQuery("routeComplete"),
            "free roam must un-latch completion so cruise loops keep driving");
        Assert.IsFalse(sim.IsWin(null));

        Assert.IsTrue(sim.CloneFresh().FreeRoamCruise,
            "dry-run clones must cruise like the live sim");
    }

    [Test]
    public void FreeRoamCruise_KeepsRouteCompleteLoopAlive()
    {
        var sim = NewStoryLegSim();
        sim.ArmStoryDropoff(sim.Position);
        sim.Apply("dropOff");
        sim.FreeRoamCruise = true;

        var program = Parser.Compile("while not routeComplete():\n    wait()\n", out var errors);
        CollectionAssert.IsEmpty(errors);
        var vm = new Interpreter();
        vm.Load(program);

        for (int i = 0; i < 50; i++)
        {
            StepResult step = vm.Step(sim);
            Assert.IsFalse(step.Finished,
                "a delivered story must not end the cruise once free roam is on");
            Assert.IsNull(step.RuntimeError);
        }
    }
}
