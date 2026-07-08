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
    public int  PickedUpCount;
    public int  DroppedOffCount;
    public int  DeliveredCount;
    public int  FareCollected;
    public int  ChangeGiven;

    /// <summary>Value returned by a value-returning action (e.g. collectFare).</summary>
    public Value ReturnValue;

    /// <summary>Ride ids boarded this action (ride mode only) — pairs 1:1 with
    /// BoardedDestLabels so the view layer can claim one ribbon chip per id.</summary>
    public List<int>    BoardedRideIds;
    public List<string> BoardedDestLabels;

    /// <summary>Ride ids delivered this action (ride mode only) — the view layer
    /// hides the matching chip for each id.</summary>
    public List<int> DeliveredRideIds;

    /// <summary>Tints of the passengers who alighted this action — one per delivered
    /// head, in ride color where known. The view layer spawns a lingering peep beside
    /// the drop-off stop in each color so an automation delivery reads like a Manual one.</summary>
    public List<Color> DroppedOffColors;
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
    public int        tender;
    public Color      color;
    public string     destLabel = "Stop";   // display name for the ribbon chip

    public bool aboard;
    public bool delivered;
    public bool fareCollected;
    public bool changeSettled;
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

    /// <summary>Cash handed over for fares that still need change settled.</summary>
    public int TenderCollected { get; private set; }

    /// <summary>Pesos returned through giveChange().</summary>
    public int ChangeGiven { get; private set; }

    public int RemainingWaiting => _rides != null ? CountRemainingRides() : _waiting.Count;

    /// <summary>Total passengers this puzzle expects delivered — ride count in ride mode,
    /// stop count otherwise. Pairs with <see cref="PassengersDelivered"/> for "k of N" progress.</summary>
    public int TotalPassengers => _rides != null ? _rides.Count : _grid.StopCells.Count;

    // Ride mode (procedural town / self-driving) and the nav-macro move queue.
    List<GridRide> _rides;
    readonly Queue<string> _pending = new Queue<string>();
    readonly HashSet<Vector2Int> _trafficCells = new HashSet<Vector2Int>();

    /// <summary>Per-passenger rides, or null in generic-stop mode.</summary>
    public IReadOnlyList<GridRide> Rides => _rides;

    /// <summary>Pending primitive moves from a nav macro, drained one per visual step.</summary>
    public bool HasPendingMoves => _pending.Count > 0;
    public int PendingMoveCount => _pending.Count;
    public string DequeueMove() => _pending.Dequeue();

    public GridModel Grid => _grid;

    /// <summary>When true, sparse road traffic participates in movement queries.</summary>
    public bool TrafficEnabled;
    /// <summary>When true, traffic is a hard obstacle for primitive forward moves.
    /// Procedural drive scenes turn this off so traffic is ambience/sensor data,
    /// not a temporary wall that can erase a valid road.</summary>
    public bool TrafficBlocksMovement = true;
    public IReadOnlyCollection<Vector2Int> TrafficCells => _trafficCells;

    /// <summary>Seat capacity; reporters read this as the world state.</summary>
    public int SeatCapacity = 8;

    /// <summary>Endless procedural road: the route never finishes at a fixed terminal,
    /// so completion keys on the story drop-off being reached, not on standing at D.
    /// Set by the controller for procedural Automation legs.</summary>
    public bool EndlessRoute;

    /// <summary>Live story leg (driven by the controller, not a headless test). When set,
    /// the endless route only completes once the controller arms a story drop-off and the
    /// jeepney reaches it — so the leg can't finish early off filler riders, and the road
    /// stays endless until then. Headless tests leave this false and use "all rides
    /// delivered" as the solvability condition.</summary>
    public bool StoryLegMode;

    const int EndlessMacroMaxSteps = 4;

    /// <summary>The story passenger's drop-off cell, armed by the controller a short buffer
    /// after the dialogue ends. The story passenger alights there (like any NPC) when
    /// dropOff() is called on the cell, which sets <see cref="StoryDelivered"/>.</summary>
    public bool       StoryDropoffArmed;
    public Vector2Int StoryDropoffCell;

    /// <summary>True once the story passenger has been dropped at the armed cell. Completes the
    /// leg; the controller hides the front-seat card on this transition. The road keeps going.</summary>
    public bool StoryDelivered;

    /// <summary>Arms the story drop-off at <paramref name="cell"/> (called when the dialogue
    /// ends, after a buffer). Until armed, a story leg can't complete. The controller keeps the
    /// cell pinned to a fixed world position across streaming (so no road cap is needed).</summary>
    public void ArmStoryDropoff(Vector2Int cell)
    {
        StoryDropoffCell  = cell;
        StoryDropoffArmed = true;
    }

    /// <summary>A walkable cell <paramref name="steps"/> along the road ahead of the jeepney
    /// (toward the current frontier), clamped to the path. Used to place the story drop-off a
    /// short buffer ahead so the player/code drives to it before the leg ends.</summary>
    public Vector2Int CellAhead(int steps)
    {
        List<Vector2Int> path = GridPathfinder.Path(_grid, Position, _grid.DestPos);
        if (path == null || path.Count == 0) return Position;
        int idx = Mathf.Clamp(steps, 0, path.Count - 1);
        return path[idx];
    }

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
        var copy = new AgentSim(_grid, _fares, _startFacing)
        {
            SeatCapacity = SeatCapacity,
            TrafficEnabled = TrafficEnabled,
            TrafficBlocksMovement = TrafficBlocksMovement,
            EndlessRoute = EndlessRoute,
            StoryLegMode = StoryLegMode,
        };
        copy.SetTrafficCells(_trafficCells);
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
                    fare = ride.fare, tender = ride.tender, color = ride.color,
                    destLabel = ride.destLabel,
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
        TenderCollected     = 0;
        ChangeGiven         = 0;

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

    public void SetTrafficCells(IEnumerable<Vector2Int> cells)
    {
        _trafficCells.Clear();
        if (cells == null) return;
        foreach (Vector2Int cell in cells)
            if (_grid != null && _grid.IsWalkable(cell))
                _trafficCells.Add(cell);
    }

    public void ClearTraffic()
    {
        _trafficCells.Clear();
    }

    void ResetRides()
    {
        if (_rides == null) return;
        foreach (GridRide ride in _rides)
        {
            ride.aboard    = false;
            ride.delivered = false;
            ride.fareCollected = false;
            ride.changeSettled = false;
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
    public AgentActionResult Apply(string action, IReadOnlyList<Value> args = null)
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
                if (TrafficBlocksMovementAt(target))
                {
                    r.Blocked = true;
                    r.Warning = "traffic ahead - use carInFront() and moveLeft()/moveRight() to dodge.";
                }
                else if (_grid.IsWalkable(target))
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

            case "moveLeft":
                // Lane change: strafe one cell to the left of the heading, keeping facing.
                ApplyLaneChange(r, (Facing + 3) % 4, "left");
                break;

            case "moveRight":
                // Lane change: strafe one cell to the right of the heading, keeping facing.
                ApplyLaneChange(r, (Facing + 1) % 4, "right");
                break;

            case "avoidTraffic":
                // Built-in dodge: slide into a clear lane when a car blocks the cell
                // ahead. Rewrites r.Action to the primitive actually performed so the
                // view/HUD/duration layers need no special handling.
                if (!TrafficPresent(Position + FacingDeltas[Facing]))
                {
                    r.Action = "wait";
                }
                else
                {
                    Vector2Int leftCell = Position + FacingDeltas[(Facing + 3) % 4];
                    Vector2Int rightCell = Position + FacingDeltas[(Facing + 1) % 4];
                    if (_grid.IsWalkable(leftCell) && !TrafficPresent(leftCell))
                    {
                        ApplyLaneChange(r, (Facing + 3) % 4, "left");
                        r.Action = "moveLeft";
                    }
                    else if (_grid.IsWalkable(rightCell) && !TrafficPresent(rightCell))
                    {
                        ApplyLaneChange(r, (Facing + 1) % 4, "right");
                        r.Action = "moveRight";
                    }
                    else
                    {
                        r.Action = "wait";
                        r.Warning = "boxed in — waiting for traffic to clear.";
                    }
                }
                break;

            case "pickUp":
                if (_rides != null) { PickUpRides(r); break; }
                if (_waiting.Remove(Position))
                {
                    PassengersAboard++;
                    UnpaidFares++;
                    r.PickedUp = true;
                    r.PickedUpCount = 1;
                }
                else
                {
                    r.Warning = "no passenger is waiting here.";
                }
                break;

            case "dropOff":
                // The front-seat story passenger alights at their marked drop-off, like an NPC.
                if (StoryDropoffArmed && !StoryDelivered && Position == StoryDropoffCell)
                {
                    StoryDelivered = true;
                    r.DroppedOff   = true;
                    r.DroppedOffCount = 1;
                    AddAlightingColor(r, DefaultPeepColor);
                }
                if (_rides != null) { DropOffRides(r); break; }
                if (Position == _grid.DestPos && PassengersAboard > 0)
                {
                    r.DeliveredCount     = PassengersAboard;
                    r.DroppedOffCount    = PassengersAboard;
                    for (int i = 0; i < PassengersAboard; i++)
                        AddAlightingColor(r, DefaultPeepColor);
                    PassengersDelivered += PassengersAboard;
                    PassengersAboard     = 0;
                    r.DroppedOff         = true;
                }
                else if (!r.DroppedOff)
                {
                    r.Warning = PassengersAboard == 0
                        ? "no passengers aboard."
                        : "nobody wants to get off here — take them to the destination (D).";
                }
                break;

            case "collectFare":
                if (_rides != null) { CollectRideFares(r); break; }
                if (UnpaidFares > 0)
                {
                    int amount      = UnpaidFares * FareMath.ComputeFare(1, _fares);
                    FaresCollected += amount;
                    TenderCollected += amount;
                    r.FareCollected = amount;
                    r.ReturnValue   = Value.Int(amount);
                    UnpaidFares     = 0;
                }
                else
                {
                    r.Warning = "no fares to collect right now.";
                }
                break;

            case "giveChange":
                GiveChange(r, args);
                break;

            case "wait":
                // Idle tick — nothing changes.
                break;

            case "driveToNextStop":
            {
                Vector2Int? stop = NearestRelevantStop();
                EnqueuePathTo(stop ?? _grid.DestPos, r, EndlessRoute ? EndlessMacroMaxSteps : int.MaxValue);
                break;
            }

            case "driveToDestination":
                EnqueuePathTo(_grid.DestPos, r, EndlessRoute ? EndlessMacroMaxSteps : int.MaxValue);
                break;

            case "driveToTerminal":
                EnqueuePathTo(_grid.DestPos, r, EndlessRoute ? EndlessMacroMaxSteps : int.MaxValue);
                break;

            case "driveToDropoff":
            {
                // While the story drop-off is armed and the passenger is still aboard, head
                // straight for it — dropping them there is the leg's end. Otherwise cruise the
                // endless road like keepDriving() (serve a nearby rider, else a short hop).
                if (StoryDropoffArmed && !StoryDelivered)
                {
                    EnqueuePathTo(StoryDropoffCell, r, EndlessRoute ? EndlessMacroMaxSteps : int.MaxValue);
                }
                else
                {
                    Vector2Int? stop = NearestRelevantStop();
                    if (stop.HasValue) EnqueuePathTo(stop.Value, r, EndlessRoute ? EndlessMacroMaxSteps : int.MaxValue);
                    else               EnqueuePathTo(_grid.DestPos, r, maxSteps: 4);
                }
                break;
            }

            case "keepDriving":
            {
                // Endless cruise primitive: serve a nearby rider if there is one, else
                // take a short hop toward the receding frontier so the controller can
                // stream the next stretch ahead of us (the road never ends or stalls).
                Vector2Int? stop = NearestRelevantStop();
                if (stop.HasValue) EnqueuePathTo(stop.Value, r, EndlessRoute ? EndlessMacroMaxSteps : int.MaxValue);
                else               EnqueuePathTo(_grid.DestPos, r, maxSteps: 4);
                break;
            }

            default:
                r.Warning = $"unknown action '{action}'.";
                break;
        }

        return r;
    }

    /// <summary>Strafes one cell along <paramref name="dirIndex"/> (a perpendicular of the current
    /// heading) without rotating — the grid analog of a Manual lane change. Bumps when there's no
    /// open lane to slide into (e.g. a single-lane stretch).</summary>
    void ApplyLaneChange(AgentActionResult r, int dirIndex, string sideName)
    {
        Vector2Int target = Position + FacingDeltas[dirIndex];
        if (_grid.IsWalkable(target))
        {
            Position = target;
            r.To = target;
        }
        else
        {
            r.Blocked = true;
            r.Warning = $"no open lane to the {sideName} — there's no road to slide into.";
        }
    }

    // -------------------------------------------------------------------------
    // Ride-mode actions

    /// <summary>Neutral tint for an alighting passenger whose ride color is unknown
    /// (generic-stop puzzles and the front-seat story rider).</summary>
    static readonly Color DefaultPeepColor = new Color(0.85f, 0.85f, 0.9f);

    static void AddAlightingColor(AgentActionResult r, Color color)
    {
        r.DroppedOffColors ??= new List<Color>();
        r.DroppedOffColors.Add(color);
    }

    void PickUpRides(AgentActionResult r)
    {
        int boarded = 0;
        foreach (GridRide ride in _rides)
            if (!ride.aboard && !ride.delivered && ride.origin == Position)
            {
                ride.aboard = true;
                if (ride.tender <= 0) ride.tender = ride.fare;
                PassengersAboard++;
                UnpaidFares++;
                boarded++;

                r.BoardedRideIds ??= new List<int>();
                r.BoardedDestLabels ??= new List<string>();
                r.BoardedRideIds.Add(ride.id);
                r.BoardedDestLabels.Add(ride.destLabel);
            }

        if (boarded > 0)
        {
            r.PickedUp = true;
            r.PickedUpCount = boarded;
        }
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

                r.DeliveredRideIds ??= new List<int>();
                r.DeliveredRideIds.Add(ride.id);
                AddAlightingColor(r, ride.color);
            }

        if (delivered > 0)
        {
            r.DroppedOff = true;
            r.DeliveredCount += delivered;
            r.DroppedOffCount += delivered;
        }
        else if (r.DroppedOff) { /* the story passenger alighted here — no warning */ }
        else if (unpaidHere > 0) r.Warning = "collect fare and give exact change before letting this passenger off.";
        else if (PassengersAboard == 0) r.Warning = "no passengers aboard.";
        else r.Warning = "nobody wants to get off here.";
    }

    void CollectRideFares(AgentActionResult r)
    {
        int amount = 0;
        int tender = 0;
        foreach (GridRide ride in _rides)
        {
            if (!ride.aboard || ride.fareCollected) continue;

            int paidCash = ride.tender > 0 ? ride.tender : ride.fare;
            amount += ride.fare;
            tender += paidCash;
            ride.fareCollected = true;

            if (FareMath.ChangeFor(paidCash, ride.fare) == 0)
            {
                ride.changeSettled = true;
                ride.paid = true;
            }
        }

        if (amount > 0)
        {
            FaresCollected += amount;
            TenderCollected += tender;
            r.FareCollected = amount;
            r.ReturnValue   = Value.Int(amount);
            UnpaidFares     = CountUnsettledAboard();
        }
        else
        {
            r.Warning = "no fares to collect right now.";
        }
    }

    void GiveChange(AgentActionResult r, IReadOnlyList<Value> args)
    {
        int owed = ChangeOwed();
        int offered = owed;
        if (args != null && args.Count > 0)
            offered = (int)args[0].AsInt();

        if (owed <= 0)
        {
            if (offered != 0)
                r.Warning = "no change is owed right now.";
            return;
        }

        if (offered != owed)
        {
            r.Warning = $"wrong change: give ₱{owed}.";
            return;
        }

        if (_rides != null)
        {
            foreach (GridRide ride in _rides)
                if (ride.aboard && ride.fareCollected && !ride.changeSettled)
                {
                    ride.changeSettled = true;
                    ride.paid = true;
                }
        }

        ChangeGiven += offered;
        r.ChangeGiven = offered;
        UnpaidFares = CountUnsettledAboard();
    }

    // -------------------------------------------------------------------------
    // Navigation macros (high-level building blocks)

    /// <summary>Nearest un-boarded pickup or aboard drop-off cell, by path length.</summary>
    Vector2Int? NearestRelevantStop()
    {
        if (_rides == null) return null;

        // On the endless road the jeepney only ever marches forward, so ignore any
        // stop that sits *behind* us (farther from the receding frontier D than we are).
        // Otherwise the pathfinder happily routes backward to a passed-by stop and the
        // jeepney appears to reverse before a stop sign, then resume forward once it's
        // served — the reported bug. Authored (finite) puzzles may legitimately backtrack,
        // so this forward-only gate applies to endless legs only.
        int selfToDest = EndlessRoute ? PathLenToDest(Position) : 0;

        Vector2Int? best = null;
        int bestLen = int.MaxValue;
        foreach (GridRide ride in _rides)
        {
            if (ride.delivered) continue;
            Vector2Int target = ride.aboard ? ride.dest : ride.origin;
            if (EndlessRoute && PathLenToDest(target) > selfToDest) continue;   // behind us — never double back
            List<Vector2Int> path = GridPathfinder.Path(_grid, Position, target);
            if (path == null) continue;
            if (path.Count < bestLen) { bestLen = path.Count; best = target; }
        }
        return best;
    }

    /// <summary>Path length from <paramref name="from"/> to the frontier D (int.MaxValue if
    /// unreachable). Smaller means closer to the frontier — i.e. farther "forward" along the
    /// endless road — so it orders cells by forward progress.</summary>
    int PathLenToDest(Vector2Int from)
    {
        List<Vector2Int> path = GridPathfinder.Path(_grid, from, _grid.DestPos);
        return path?.Count ?? int.MaxValue;
    }

    void EnqueuePathTo(Vector2Int target, AgentActionResult r, int maxSteps = int.MaxValue)
    {
        List<Vector2Int> path = GridPathfinder.Path(_grid, Position, target);
        if (path == null) { r.Warning = "can't find a road to there."; return; }
        int enqueued = 0;
        foreach (string move in GridPathfinder.ToActions(path, Facing))
        {
            if (enqueued >= maxSteps) break;   // short hop: keep the streamer ahead of us
            _pending.Enqueue(move);
            enqueued++;
        }
    }

    // -------------------------------------------------------------------------
    // Queries (IAgentApi)

    public bool EvaluateQuery(string name)
    {
        switch (name)
        {
            case "frontIsClear":  return IsClear(Position + FacingDeltas[Facing]);
            case "leftIsClear":   return IsClear(Position + FacingDeltas[(Facing + 3) % 4]);
            case "rightIsClear":  return IsClear(Position + FacingDeltas[(Facing + 1) % 4]);
            case "carInFront":    return TrafficPresent(Position + FacingDeltas[Facing]);
            case "atStop":        return _grid.Get(Position) == GridModel.Cell.Stop;
            case "atDestination": return Position == _grid.DestPos;
            case "routeComplete": return RouteComplete();
            case "moreRoad":      return true;   // the procedural road never ends — loop forever
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
            case "cashTendered":        return Value.Int(CashTendered());
            case "changeOwed":          return Value.Int(ChangeOwed());
            case "currentStop":         return Value.Str("");
            case "nextStop":            return Value.Str("");
            default:                    return Value.None;
        }
    }

    bool AtRequestedStop()
    {
        // The story passenger's marked drop-off counts as a requested stop, so the same
        // `if atRequestedStop(): dropOff()` that serves NPC riders drops them too.
        if (StoryDropoffArmed && !StoryDelivered && Position == StoryDropoffCell) return true;

        if (_rides != null)
        {
            foreach (GridRide ride in _rides)
                if (ride.aboard && ride.dest == Position) return true;
            return false;
        }
        return Position == _grid.DestPos && PassengersAboard > 0;
    }

    bool IsClear(Vector2Int cell)
    {
        return _grid.IsWalkable(cell) && !TrafficPresent(cell);
    }

    bool TrafficPresent(Vector2Int cell)
    {
        return TrafficEnabled && _trafficCells.Contains(cell);
    }

    bool TrafficBlocksMovementAt(Vector2Int cell)
    {
        return TrafficBlocksMovement && TrafficPresent(cell);
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
                if (ride.aboard && !ride.fareCollected) amount += ride.fare;
            return amount;
        }
        return UnpaidFares * FareMath.ComputeFare(1, _fares);
    }

    int CashTendered()
    {
        if (_rides != null)
        {
            int amount = 0;
            foreach (GridRide ride in _rides)
                if (ride.aboard && ride.fareCollected && !ride.changeSettled)
                    amount += ride.tender > 0 ? ride.tender : ride.fare;
            return amount;
        }
        return 0;
    }

    int ChangeOwed()
    {
        if (_rides != null)
        {
            int amount = 0;
            foreach (GridRide ride in _rides)
                if (ride.aboard && ride.fareCollected && !ride.changeSettled)
                    amount += FareMath.ChangeFor(ride.tender > 0 ? ride.tender : ride.fare, ride.fare);
            return amount;
        }
        return 0;
    }

    int CountUnsettledAboard()
    {
        if (_rides != null)
        {
            int count = 0;
            foreach (GridRide ride in _rides)
                if (ride.aboard && !ride.paid)
                    count++;
            return count;
        }
        return UnpaidFares;
    }

    bool RouteComplete()
    {
        // Endless road: the leg ends at the story drop-off, not at a receding terminal.
        if (EndlessRoute)
        {
            // Live story leg: complete only once the story passenger has been dropped at their
            // marked stop (dialogue over + buffer + dropOff). Filler riders never finish the
            // leg, so the road stays endless until the story is delivered.
            if (StoryLegMode)
                return StoryDelivered;

            // Headless / solvability path (no controller): "all riders delivered".
            if (_rides != null)
            {
                foreach (GridRide ride in _rides)
                    if (!ride.delivered) return false;
                return PassengersAboard == 0 && UnpaidFares == 0 && ChangeOwed() == 0;
            }
            return _waiting.Count == 0 && PassengersAboard == 0 && UnpaidFares == 0;
        }

        // Authored ride levels still finish at the fixed destination D.
        if (Position != _grid.DestPos) return false;
        if (_rides != null)
        {
            foreach (GridRide ride in _rides)
                if (!ride.delivered) return false;
            return PassengersAboard == 0 && UnpaidFares == 0 && ChangeOwed() == 0;
        }
        return _waiting.Count == 0 && PassengersAboard == 0 && UnpaidFares == 0;
    }

    // -------------------------------------------------------------------------
    // Goal

    /// <summary>True when the puzzle's win condition is met right now.</summary>
    public bool IsWin(AutomationPuzzleDefinition def)
    {
        // Endless road: completion is the story drop-off (live legs) or all riders delivered
        // (headless), not "standing on the receding frontier D" — so the leg can finish while
        // the road keeps generating.
        if ((def != null && def.endlessRoute) || EndlessRoute)
            return RouteComplete();

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
        bool endless = (def != null && def.endlessRoute) || EndlessRoute;

        // On the endless road there is no fixed D to reach, so never complain about that.
        if (endless && StoryLegMode)
        {
            if (StoryDelivered) return null;
            if (!StoryDropoffArmed)
                return "the story isn't over yet — keep driving (keepDriving / driveToDropoff) " +
                       "and the drop-off appears once the chat wraps up.";
            return "drive to the story passenger's marked drop-off (driveToDropoff) and dropOff() " +
                   "to finish the leg.";
        }
        if (endless && _rides != null)
        {
            int left = 0;
            foreach (GridRide ride in _rides) if (!ride.delivered) left++;
            if (left > 0)
                return $"{left} rider(s) still need their drop-off.";
            return null;
        }

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
