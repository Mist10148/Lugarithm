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
}

/// <summary>
/// The deterministic jeepney simulation for Automation Mode — the single
/// source of truth for gameplay semantics. The view layer animates whatever
/// this says happened, and the EditMode tests drive it directly.
/// Implements <see cref="IAgentApi"/> so the interpreter can ask it questions.
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

    readonly GridModel _grid;
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

    public int RemainingWaiting => _waiting.Count;

    // -------------------------------------------------------------------------

    public AgentSim(GridModel grid, FareTable fares, int startFacing)
    {
        _grid        = grid;
        _fares       = fares ?? new FareTable();
        _startFacing = ((startFacing % 4) + 4) % 4;
        Reset();
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

        _waiting.Clear();
        foreach (Vector2Int stop in _grid.StopCells)
            _waiting.Add(stop);
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
                if (UnpaidFares > 0)
                {
                    int amount      = UnpaidFares * FareMath.ComputeFare(1, _fares);
                    FaresCollected += amount;
                    r.FareCollected = amount;
                    UnpaidFares     = 0;
                }
                else
                {
                    r.Warning = "no fares to collect right now.";
                }
                break;

            default:
                r.Warning = $"unknown action '{action}'.";
                break;
        }

        return r;
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
            default:              return false;
        }
    }

    // -------------------------------------------------------------------------
    // Goal

    /// <summary>True when the puzzle's win condition is met right now.</summary>
    public bool IsWin(AutomationPuzzleDefinition def)
    {
        if (Position != _grid.DestPos) return false;
        if (def != null && def.requireAllPassengersDelivered)
            return _waiting.Count == 0 && PassengersAboard == 0;
        return true;
    }

    /// <summary>Plain-English reason the goal isn't met yet, or null if it is.</summary>
    public string DescribeGoalGap(AutomationPuzzleDefinition def)
    {
        if (Position != _grid.DestPos)
            return "The jeepney never reached the destination (D).";

        if (def != null && def.requireAllPassengersDelivered)
        {
            if (_waiting.Count > 0)
                return "A passenger is still waiting at a stop (P) — pickUp() when you're on their cell.";
            if (PassengersAboard > 0)
                return "Passengers are still aboard — dropOff() at the destination.";
        }

        return null;
    }
}
