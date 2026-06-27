using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode tests for AgentSim's opt-in ride mode (per-passenger dulog cells) and
/// the high-level navigation macros that plan a path into the move queue.
/// </summary>
public class AgentSimRideTests
{
    static GridModel StraightGrid() =>
        GridModel.Parse(new[]
        {
            "#######",
            "#S.P.D#",
            "#######",
        }, out _);

    [Test]
    public void RideMode_BoardsAtOrigin_PaysFare_AndAlightsAtItsOwnStop()
    {
        GridModel grid = StraightGrid();
        var sim = new AgentSim(grid, new FareTable(), startFacing: 1);
        sim.LoadRides(new List<GridRide>
        {
            new GridRide { id = 0, origin = new Vector2Int(3, 1), dest = new Vector2Int(5, 1), fare = 17, tender = 20 },
        });

        Assert.AreEqual(1, sim.RemainingWaiting);

        sim.Apply("moveForward");
        sim.Apply("moveForward");                 // at the pickup (3,1)
        var board = sim.Apply("pickUp");
        Assert.IsTrue(board.PickedUp);
        Assert.AreEqual(1, sim.PassengersAboard);
        Assert.AreEqual(0, sim.RemainingWaiting);

        var fare = sim.Apply("collectFare");
        Assert.AreEqual(17, fare.FareCollected);
        Assert.AreEqual(20, sim.ReadReporter("cashTendered", new List<Value>()).AsInt());
        Assert.AreEqual(3, sim.ReadReporter("changeOwed", new List<Value>()).AsInt());
        Assert.AreEqual(3, sim.Apply("giveChange", new[] { Value.Int(3) }).ChangeGiven);

        Assert.IsFalse(sim.EvaluateQuery("atRequestedStop"), "their stop is further along");
        sim.Apply("moveForward");
        sim.Apply("moveForward");                 // at (5,1) = their dulog (and D)
        Assert.IsTrue(sim.EvaluateQuery("atRequestedStop"));
        Assert.IsTrue(sim.EvaluateQuery("hasPassengerAboard"));

        var drop = sim.Apply("dropOff");
        Assert.IsTrue(drop.DroppedOff);
        Assert.AreEqual(1, drop.DeliveredCount);
        Assert.AreEqual(0, sim.PassengersAboard);
        Assert.AreEqual(1, sim.PassengersDelivered);

        var def = new AutomationPuzzleDefinition { requireAllPassengersDelivered = true };
        Assert.IsTrue(sim.IsWin(def));
    }

    [Test]
    public void DriveToDestination_PlansAPath_DrainedToReachD()
    {
        GridModel grid = StraightGrid();
        var sim = new AgentSim(grid, new FareTable(), startFacing: 1);
        sim.LoadRides(new List<GridRide>());

        var macro = sim.Apply("driveToDestination");
        Assert.IsNull(macro.Warning, macro.Warning);
        Assert.IsTrue(sim.HasPendingMoves, "the macro should queue moves");

        int guard = 50;
        while (sim.HasPendingMoves && guard-- > 0)
            sim.Apply(sim.DequeueMove());

        Assert.AreEqual(grid.DestPos, sim.Position);
    }

    [Test]
    public void DriveToTerminal_IsAliasForDestination()
    {
        GridModel grid = StraightGrid();
        var sim = new AgentSim(grid, new FareTable(), startFacing: 1);
        sim.LoadRides(new List<GridRide>());

        sim.Apply("driveToTerminal");
        int guard = 50;
        while (sim.HasPendingMoves && guard-- > 0)
            sim.Apply(sim.DequeueMove());

        Assert.AreEqual(grid.DestPos, sim.Position);
        Assert.IsTrue(sim.EvaluateQuery("routeComplete"));
    }

    [Test]
    public void DriveToNextStop_HeadsToTheNearestPickup()
    {
        GridModel grid = StraightGrid();
        var sim = new AgentSim(grid, new FareTable(), startFacing: 1);
        sim.LoadRides(new List<GridRide>
        {
            new GridRide { id = 0, origin = new Vector2Int(3, 1), dest = new Vector2Int(5, 1), fare = 13 },
        });

        sim.Apply("driveToNextStop");
        int guard = 50;
        while (sim.HasPendingMoves && guard-- > 0)
            sim.Apply(sim.DequeueMove());

        Assert.AreEqual(new Vector2Int(3, 1), sim.Position, "should arrive at the pickup stop");
        Assert.IsTrue(sim.Apply("pickUp").PickedUp);
    }

    [Test]
    public void RideMode_QueriesWaitingAndFareAtCurrentStop()
    {
        GridModel grid = StraightGrid();
        var sim = new AgentSim(grid, new FareTable(), startFacing: 1);
        sim.LoadRides(new List<GridRide>
        {
            new GridRide { id = 0, origin = new Vector2Int(3, 1), dest = new Vector2Int(5, 1), fare = 21, tender = 50 },
        });

        Assert.IsFalse(sim.EvaluateQuery("passengerWaiting"));
        sim.Apply("moveForward");
        sim.Apply("moveForward");
        Assert.IsTrue(sim.EvaluateQuery("passengerWaiting"));

        sim.Apply("pickUp");
        Assert.AreEqual(21, sim.ReadReporter("fareOwed", new List<Value>()).AsInt());
        Assert.AreEqual(21, sim.Apply("collectFare").FareCollected);
        Assert.AreEqual(0, sim.ReadReporter("fareOwed", new List<Value>()).AsInt());
        Assert.AreEqual(50, sim.ReadReporter("cashTendered", new List<Value>()).AsInt());
        Assert.AreEqual(29, sim.ReadReporter("changeOwed", new List<Value>()).AsInt());
    }

    [Test]
    public void RideMode_DropOffRequiresFareFirst()
    {
        GridModel grid = StraightGrid();
        var sim = new AgentSim(grid, new FareTable(), startFacing: 1);
        sim.LoadRides(new List<GridRide>
        {
            new GridRide { id = 0, origin = new Vector2Int(3, 1), dest = new Vector2Int(5, 1), fare = 17, tender = 20 },
        });

        sim.Apply("driveToNextStop");
        while (sim.HasPendingMoves) sim.Apply(sim.DequeueMove());
        sim.Apply("pickUp");
        sim.Apply("driveToNextStop");
        while (sim.HasPendingMoves) sim.Apply(sim.DequeueMove());

        var early = sim.Apply("dropOff");
        Assert.IsFalse(early.DroppedOff);
        StringAssert.Contains("collect fare", early.Warning);
        Assert.IsFalse(sim.EvaluateQuery("routeComplete"));
    }

    [Test]
    public void RideMode_WrongChangeBlocksDropOff_UntilExactChangeIsGiven()
    {
        GridModel grid = StraightGrid();
        var sim = new AgentSim(grid, new FareTable(), startFacing: 1);
        sim.LoadRides(new List<GridRide>
        {
            new GridRide { id = 0, origin = new Vector2Int(3, 1), dest = new Vector2Int(5, 1), fare = 17, tender = 50 },
        });

        sim.Apply("driveToNextStop");
        while (sim.HasPendingMoves) sim.Apply(sim.DequeueMove());
        sim.Apply("pickUp");
        sim.Apply("collectFare");

        Assert.AreEqual(33, sim.ReadReporter("changeOwed", new List<Value>()).AsInt());
        var wrong = sim.Apply("giveChange", new[] { Value.Int(30) });
        StringAssert.Contains("wrong change", wrong.Warning);

        sim.Apply("driveToNextStop");
        while (sim.HasPendingMoves) sim.Apply(sim.DequeueMove());
        var blocked = sim.Apply("dropOff");
        Assert.IsFalse(blocked.DroppedOff);

        sim.Apply("giveChange", new[] { Value.Int(33) });
        var drop = sim.Apply("dropOff");
        Assert.IsTrue(drop.DroppedOff);
        Assert.IsTrue(sim.EvaluateQuery("routeComplete"));
    }
}
