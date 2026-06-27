using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Everything one agent action changed, for the view layer to animate and the
/// console to narrate. A blocked move or an empty pickUp is a warning, never
/// a hard error — the run always continues (no fail states, PRD §5.5).
/// </summary>
public class AgentActionResult
{
    public string Action;
    public Vector2Int From, To;
    public int FacingBefore, FacingAfter;

    public bool   Blocked;
    public string Warning;

    public bool PickedUp;
    public bool DroppedOff;
    public int  DeliveredCount;
    public int  FareCollected;

    /// <summary>Value returned by a value-returning action (e.g. collectFare).</summary>
    public Value ReturnValue;
}

/// <summary>
/// One committed ride on the grid: the cell a passenger boards at, the dulog
/// cell they want to alight at, and the fare. Used by the procedural town and
/// the self-driving agent so passengers have individual destinations rather than
/// the single generic 'D'. Maps 1:1 from a <see cref="PassengerRequest"/>.
/// </summary>
public class GridRide
{
    public int        id;
    public int        originNodeId = -1;
    public int        destNodeId = -1;
    public Vector2Int origin;
    public Vector2Int dest;
    public int        fare;
    public Color      color;

    public bool aboard;
    public bool delivered;
    public bool paid;
}

/// <summary>
/// The deterministic jeepney simulation for Automation Mode — the single
/// source of truth for gameplay semantics. The view layer animates whatever
/// this says happened, and the EditMode tests drive it directly.
/// Implements <see cref="IAgentApi"/> so the interpreter can ask it questions.
///
/// Two passenger modes: the default generic-stop mode (every 'P' is a passenger
/// bound for 'D') and an opt-in ride mode (<see cref="LoadRides"/>) where each
/// passenger has an individual dulog cell — used by the procedural town and the
/// self-driving agent. The default path is unchanged so existing puzzles behave
/// exactly as before.
/// </summary>
public class AgentSim : IAgentApi
{
    /// <summary>Facing deltas, indexed 0=N (up, y−1), 1=E, 2=S, 3=W.</summary>
    public static readonly Vector2Int[] FacingDeltas =
    {
        new Vector2Int(0, -1),
        new Vector2Int(1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(-1, 0),
    };

    public static readonly string[] FacingNames = { "N", "E", "S", "W" };

    GridModel _grid;
    readonly FareTable _fares;
    readonly int       _startFacing;
    readonly HashSet<Vector2Int> _waiting = new HashSet<Vector2Int>();

    public Vector2Int Position { get; private set; }
    public int Facing          { get; private set; }

    public int StepsUsed           { get; private set; }
    public int PassengersAboard    { get; private set; }
    public int PassengersDelivered { get; private set; }

    /// <summary>Boarded passengers who haven't paid yet.</summary>
    public int UnpaidFares { get; private set; }

    /// <summary>Pesos collected via collectFare().</summary>
    public int FaresCollected { get; private set; }

    public int RemainingWaiting => _rides != null ? CountRemainingRides() : _waiting.Count;

    /// <summary>Total passengers this puzzle expects delivered — ride count in ride mode,
    /// stop count otherwise. Pairs with <see cref="PassengersDelivered"/> for "k of N" progress.</summary>
    public int TotalPassengers => _rides != null ? _rides.Count : _grid.StopCells.Count;

    // Ride mode (procedural town / self-driving) and the nav-macro move queue.
    List<GridRide> _rides;
    readonly Queue<string> _pending = new Queue<string>();

    /// <summary>Per-passenger rides, or null in generic-stop mode.</summary>
    public IReadOnlyList<GridRide> Rides => _rides;

    /// <summary>Pending primitive moves from a nav macro, drained one per visual step.</summary>
    public bool HasPendingMoves => _pending.Count > 0;
    public string DequeueMove() => _pending.Dequeue();

    public GridModel Grid => _grid;

    /// <summary>Seat capacity; reporters read this as the world state.</summary>
    public int SeatCapacity = 8;

    /// <summary>
    /// Switches to ride mode: each passenger boards at <see cref="GridRide.origin"/>
    /// and alights at their own <see cref="GridRide.dest"/>. Replaces the generic
    /// 'P'→'D' stops.
    /// </summary>
    public void LoadRides(List<GridRide> rides)
    {
        _rides = rides;
        _waiting.Clear();
        ResetRides();
    }

    /// <summary>
    /// Rebinds the sim to a reprojected grid while preserving route progress.
    /// Used by procedural Automation when the town streams in another chunk and
    /// cell coordinates may shift.
    /// </summary>
    public void RebindGrid(GridModel grid, Vector2Int position, int facing, List<GridRide> rides)
    {
        _grid = grid;
        Position = position;
        Facing = ((facing % 4) + 4) % 4;
        _pending.Clear();

        if (rides != null)
        {
            _rides = rides;
            _waiting.Clear();
        }
    }

    // -------------------------------------------------------------------------

    public AgentSim(GridModel grid, FareTable fares, int startFacing)
    {
        _grid        = grid;
        _fares       = fares ?? new FareTable();
        _startFacing = ((startFacing % 4) + 4) % 4;
        Reset();
    }

    /// <summary>A fresh sim over the same grid, fares, start facing and rides, in its
    /// puzzle-start state. Used to dry-run a generated program (headless verification)
    /// without disturbing the live sim the player sees.</summary>
    public AgentSim CloneFresh()
    {
        var copy = new AgentSim(_grid, _fares, _startFacing) { SeatCapacity = SeatCapacity };
        if (_rides != null)
        {
            // Rides carry mutable run-state (aboard/delivered/paid); clone them so the dry
            // run can't bleed into the originals. LoadRides resets the copies to start.
            var ridesCopy = new List<GridRide>(_rides.Count);
            foreach (GridRide ride in _rides)
                ridesCopy.Add(new GridRide
                {
                    id = ride.id, originNodeId = ride.originNodeId, destNodeId = ride.destNodeId,
                    origin = ride.origin, dest = ride.dest,
                    fare = ride.fare, color = ride.color,
                });
            copy.LoadRides(ridesCopy);
        }
        return copy;
    }

    /// <summary>Puts the world back to its puzzle-start state.</summary>
    public void Reset()
    {
        Position            = _grid.StartPos;
        Facing              = _startFacing;
        StepsUsed           = 0;
        PassengersAboard    = 0;
        PassengersDelivered = 0;
        UnpaidFares         = 0;
        FaresCollected      = 0;

        _pending.Clear();

        if (_rides != null)
        {
            ResetRides();
            return;   // ride mode ignores the generic stop set
        }

        _waiting.Clear();
        foreach (Vector2Int stop in _grid.StopCells)
            _waiting.Add(stop);
    }

    void ResetRides()
    {
        if (_rides == null) return;
        foreach (GridRide ride in _rides)
        {
            ride.aboard    = false;
            ride.delivered = false;
            ride.paid      = false;
        }
    }

    int CountRemainingRides()
    {
        int n = 0;
        foreach (GridRide ride in _rides)
            if (!ride.delivered && !ride.aboard) n++;
        return n;
    }

    // -------------------------------------------------------------------------
    // Actions

    /// <summary>Executes one agent action and reports what changed.</summary>
    public AgentActionResult Apply(string action)
    {
        var r = new AgentActionResult
        {
            Action       = action,
            From         = Position,
            To           = Position,
            FacingBefore = Facing,
            FacingAfter  = Facing,
        };

        StepsUsed++;

        switch (action)
        {
            case "moveForward":
                // NAVIGATION: grid-cell BFS, deterministic — not free-roam.
                // This is the only locomotion primitive; path targets come from
                // GridPathfinder, not the node graph.
                Vector2Int target = Position + FacingDeltas[Facing];
                if (_grid.IsWalkable(target))
                {
                    Position = target;
                    r.To = target;
                }
                else
                {
                    r.Blocked = true;
                    r.Warning = "bumped into a wall — moveForward() needs clear road ahead.";
                }
                break;

            case "turnLeft":
                Facing = (Facing + 3) % 4;
                r.FacingAfter = Facing;
                break;

            case "turnRight":
                Facing = (Facing + 1) % 4;
                r.FacingAfter = Facing;
                break;

            case "pickUp":
                if (_rides != null) { PickUpRides(r); break; }
                if (_waiting.Remove(Position))
                {
                    PassengersAboard++;
                    UnpaidFares++;
                    r.PickedUp = true;
                }
                else
                {
                    r.Warning = "no passenger is waiting here.";
                }
                break;

            case "dropOff":
                if (_rides != null) { DropOffRides(r); break; }
                if (PassengersAboard == 0)
                {
                    r.Warning = "no passengers aboard.";
                }
                else if (Position == _grid.DestPos)
                {
                    r.DeliveredCount     = PassengersAboard;
                    PassengersDelivered += PassengersAboard;
                    PassengersAboard     = 0;
                    r.DroppedOff         = true;
                }
                else
                {
                    r.Warning = "nobody wants to get off here — take them to the destination (D).";
                }
                break;

            case "collectFare":
                if (_rides != null) { CollectRideFares(r); break; }
                if (UnpaidFares > 0)
                {
                    int amount      = UnpaidFares * FareMath.ComputeFare(1, _fares);
                    FaresCollected += amount;
                    r.FareCollected = amount;
                    r.ReturnValue   = Value.Int(amount);
                    UnpaidFares     = 0;
                }
                else
                {
                    r.Warning = "no fares to collect right now.";
                }
                break;

            case "wait":
                // Idle tick — nothing changes.
                break;

            case "driveToNextStop":
            {
                Vector2Int? stop = NearestRelevantStop();
                EnqueuePathTo(stop ?? _grid.DestPos, r);
                break;
            }

            case "driveToDestination":
                EnqueuePathTo(_grid.DestPos, r);
                break;

            case "driveToTerminal":
                EnqueuePathTo(_grid.DestPos, r);
                break;

            default:
                r.Warning = $"unknown action '{action}'.";
                break;
        }

        return r;
    }

    // -------------------------------------------------------------------------
    // Ride-mode actions

    void PickUpRides(AgentActionResult r)
    {
        bool boarded = false;
        foreach (GridRide ride in _rides)
            if (!ride.aboard && !ride.delivered && ride.origin == Position)
            {
                ride.aboard = true;
                PassengersAboard++;
                UnpaidFares++;
                boarded = true;
            }

        if (boarded) r.PickedUp = true;
        else         r.Warning = "no passenger is waiting here.";
    }

    void DropOffRides(AgentActionResult r)
    {
        int delivered = 0;
        int unpaidHere = 0;
        foreach (GridRide ride in _rides)
            if (ride.aboard && ride.dest == Position)
            {
                if (!ride.paid)
                {
                    unpaidHere++;
                    continue;
                }
                ride.aboard    = false;
                ride.delivered = true;
                PassengersAboard--;
                PassengersDelivered++;
                delivered++;
            }

        if (delivered > 0) { r.DroppedOff = true; r.DeliveredCount = delivered; }
        else if (unpaidHere > 0) r.Warning = "collect fare before letting this passenger off.";
        else if (PassengersAboard == 0) r.Warning = "no passengers aboard.";
        else r.Warning = "nobody wants to get off here.";
    }

    void CollectRideFares(AgentActionResult r)
    {
        int amount = 0;
        foreach (GridRide ride in _rides)
            if (ride.aboard && !ride.paid) { amount += ride.fare; ride.paid = true; }

        if (amount > 0)
        {
            FaresCollected += amount;
            r.FareCollected = amount;
            r.ReturnValue   = Value.Int(amount);
            UnpaidFares     = 0;
        }
        else
        {
            r.Warning = "no fares to collect right now.";
        }
    }

    // -------------------------------------------------------------------------
    // Navigation macros (high-level building blocks)

    /// <summary>Nearest un-boarded pickup or aboard drop-off cell, by path length.</summary>
    Vector2Int? NearestRelevantStop()
    {
        if (_rides == null) return null;

        Vector2Int? best = null;
        int bestLen = int.MaxValue;
        foreach (GridRide ride in _rides)
        {
            if (ride.delivered) continue;
            Vector2Int target = ride.aboard ? ride.dest : ride.origin;
            List<Vector2Int> path = GridPathfinder.Path(_grid, Position, target);
            if (path == null) continue;
            if (path.Count < bestLen) { bestLen = path.Count; best = target; }
        }
        return best;
    }

    void EnqueuePathTo(Vector2Int target, AgentActionResult r)
    {
        List<Vector2Int> path = GridPathfinder.Path(_grid, Position, target);
        if (path == null) { r.Warning = "can't find a road to there."; return; }
        foreach (string move in GridPathfinder.ToActions(path, Facing))
            _pending.Enqueue(move);
    }

    // -------------------------------------------------------------------------
    // Queries (IAgentApi)

    public bool EvaluateQuery(string name)
    {
        switch (name)
        {
            case "frontIsClear":  return _grid.IsWalkable(Position + FacingDeltas[Facing]);
            case "leftIsClear":   return _grid.IsWalkable(Position + FacingDeltas[(Facing + 3) % 4]);
            case "rightIsClear":  return _grid.IsWalkable(Position + FacingDeltas[(Facing + 1) % 4]);
            case "atStop":        return _grid.Get(Position) == GridModel.Cell.Stop;
            case "atDestination": return Position == _grid.DestPos;
            case "routeComplete": return RouteComplete();
            case "hasPassengerAboard": return PassengersAboard > 0;
            case "atRequestedStop":    return AtRequestedStop();
            case "atGoal":             return Position == _grid.DestPos;
            case "isMarked":           return false;
            case "passengerWaiting":   return PassengerWaitingHere();
            case "isFull":             return PassengersAboard >= SeatCapacity;
            default:              return false;
        }
    }

    // Reporters (IAgentApi)

    public Value ReadReporter(string name, IReadOnlyList<Value> args)
    {
        switch (name)
        {
            case "seatsLeft":           return Value.Int(Math.Max(0, SeatCapacity - PassengersAboard));
            case "passengerCount":      return Value.Int(PassengersAboard);
            case "distanceTraveled":    return Value.Int(StepsUsed);
            case "distanceToDestination":
            {
                int dist = GridPathfinder.Path(_grid, Position, _grid.DestPos)?.Count ?? 0;
                return Value.Int(dist);
            }
            case "position":            return Value.Tuple(new[] { Value.Int(Position.x), Value.Int(Position.y) });
            case "passengerType":       return Value.Str("regular");
            case "fareOwed":            return Value.Int(FareOwed());
            case "currentStop":         return Value.Str("");
            case "nextStop":            return Value.Str("");
            default:                    return Value.None;
        }
    }

    bool AtRequestedStop()
    {
        if (_rides != null)
        {
            foreach (GridRide ride in _rides)
                if (ride.aboard && ride.dest == Position) return true;
            return false;
        }
        return Position == _grid.DestPos && PassengersAboard > 0;
    }

    bool PassengerWaitingHere()
    {
        if (_rides != null)
        {
            foreach (GridRide ride in _rides)
                if (!ride.aboard && !ride.delivered && ride.origin == Position)
                    return true;
            return false;
        }
        return _waiting.Contains(Position);
    }

    int FareOwed()
    {
        if (_rides != null)
        {
            int amount = 0;
            foreach (GridRide ride in _rides)
                if (ride.aboard && !ride.paid) amount += ride.fare;
            return amount;
        }
        return UnpaidFares * FareMath.ComputeFare(1, _fares);
    }

    bool RouteComplete()
    {
        if (Position != _grid.DestPos) return false;

        if (_rides != null)
        {
            foreach (GridRide ride in _rides)
                if (!ride.delivered) return false;
            return PassengersAboard == 0 && UnpaidFares == 0;
        }

        return _waiting.Count == 0 && PassengersAboard == 0 && UnpaidFares == 0;
    }

    // -------------------------------------------------------------------------
    // Goal

    /// <summary>True when the puzzle's win condition is met right now.</summary>
    public bool IsWin(AutomationPuzzleDefinition def)
    {
        if (Position != _grid.DestPos) return false;

        if (def != null && def.requireAllPassengersDelivered)
        {
            return RouteComplete();
        }
        return true;
    }

    /// <summary>Plain-English reason the goal isn't met yet, or null if it is. Names the
    /// specific where/what (final cell vs destination, how many of N riders are left) so the
    /// co-pilot can diagnose the actual gap rather than restate a generic miss.</summary>
    public string DescribeGoalGap(AutomationPuzzleDefinition def)
    {
        if (Position != _grid.DestPos)
            return $"The jeepney stopped at ({Position.x},{Position.y}) but the destination (D) " +
                   $"is at ({_grid.DestPos.x},{_grid.DestPos.y}) — it never reached D.";

        if (def != null && def.requireAllPassengersDelivered)
        {
            if (_rides != null)
            {
                int left = 0;
                foreach (GridRide ride in _rides) if (!ride.delivered) left++;
                if (left > 0)
                    return $"{left} of {_rides.Count} passengers still need dropping at their stop — " +
                           "tend every rider before finishing at D.";
                return null;
            }
            if (_waiting.Count > 0)
                return $"{_waiting.Count} passenger(s) still waiting at a stop (P) — " +
                       "pickUp() when you're on their cell.";
            if (PassengersAboard > 0)
                return $"{PassengersAboard} passenger(s) still aboard — dropOff() at the destination.";
        }

        return null;
    }
}
