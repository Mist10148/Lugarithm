using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the Automation-mode puzzle and rides for a generated town and plans a
/// self-driving route through them. Pure/static so it doubles as the solvability
/// oracle in EditMode tests (the way the wall-follower proves the authored mazes).
/// </summary>
public static class SelfDrivePlanner
{
    public static readonly string[] NavBlocks =
    {
        "moveForward", "turnLeft", "turnRight", "pickUp", "dropOff", "collectFare",
        "driveToNextStop", "driveToDestination", "while", "if", "ifElse",
    };

    public static readonly string[] NavQueries =
    {
        "frontIsClear", "leftIsClear", "rightIsClear", "atStop", "atDestination",
        "hasPassengerAboard", "atRequestedStop",
    };

    public const string ReferenceSolution =
        "# Self-driving jeepney: visit each stop, tend riders, finish at the terminal.\n" +
        "driveToNextStop()\n" +
        "pickUp()\n" +
        "collectFare()\n" +
        "while hasPassengerAboard():\n" +
        "    driveToNextStop()\n" +
        "    if atRequestedStop():\n" +
        "        dropOff()\n" +
        "    if atStop():\n" +
        "        pickUp()\n" +
        "        collectFare()\n" +
        "driveToDestination()\n";

    /// <summary>Maps the layout's committed rides onto grid cells (needs a projected layout).</summary>
    public static List<GridRide> RidesFromLayout(TownLayout layout)
    {
        var rides = new List<GridRide>();
        foreach (PassengerRequest req in layout.requests)
            rides.Add(new GridRide
            {
                id     = req.id,
                origin = layout.Node(req.originNodeId).gridCell,
                dest   = layout.Node(req.destNodeId).gridCell,
                fare   = req.fare,
                color  = req.color,
            });
        return rides;
    }

    /// <summary>
    /// Projects a generated town into an Automation puzzle (authored grid) and the
    /// matching ride list, with par set to the autopilot's own plan length.
    /// </summary>
    public static AutomationPuzzleDefinition BuildPuzzle(TownLayout layout, float cellSize,
                                                         out List<GridRide> rides, out int startFacing)
    {
        string[] map = GridLayoutProjector.ToGridMap(layout, cellSize, out startFacing, out _);
        rides = RidesFromLayout(layout);

        GridModel grid = GridModel.Parse(map, out _);
        List<string> plan = Plan(grid, rides, startFacing, grid.DestPos);

        return new AutomationPuzzleDefinition
        {
            gridMap         = map,
            startFacing     = startFacing,
            useAuthoredGrid = true,
            requireAllPassengersDelivered = true,
            allowedBlocks   = NavBlocks,
            allowedQueries  = NavQueries,
            parSteps        = plan.Count,
            softTimerSeconds = 600f,
            goalText = "Self-driving run: program the jeepney to pick up every rider, " +
                       "collect fares, drop each at their stop, then finish at the terminal (D). " +
                       "Use driveToNextStop() / driveToDestination() to navigate.",
            codeScaffold = "# Navigation blocks plan a path for you:\n" +
                           "#   driveToNextStop(), driveToDestination()\n" +
                           "# Tend riders: pickUp(), collectFare(), dropOff()\n" +
                           "# Ask: hasPassengerAboard(), atRequestedStop()\n",
            optimalSolutionText = ReferenceSolution,
        };
    }

    /// <summary>
    /// Greedy self-drive plan: repeatedly drive to the nearest un-boarded pickup or
    /// aboard drop-off, tending riders, then finish at the destination. Returns the
    /// flat primitive-action list (moveForward/turn/pickUp/collectFare/dropOff).
    /// </summary>
    public static List<string> Plan(GridModel grid, List<GridRide> rides, int startFacing, Vector2Int dest)
    {
        var actions = new List<string>();
        if (grid == null) return actions;

        Vector2Int pos = grid.StartPos;
        int facing = ((startFacing % 4) + 4) % 4;

        var aboard    = new HashSet<int>();
        var delivered = new HashSet<int>();

        int guard = (rides != null ? rides.Count : 0) * 2 + 4;
        while (rides != null && delivered.Count < rides.Count && guard-- > 0)
        {
            Vector2Int? target = NearestTarget(grid, pos, rides, aboard, delivered);
            if (target == null) break;

            List<Vector2Int> path = GridPathfinder.Path(grid, pos, target.Value);
            actions.AddRange(GridPathfinder.ToActions(path, facing));
            Advance(path, ref pos, ref facing);

            // Board everyone waiting here, then deliver everyone who wants off here.
            bool boarded = false;
            foreach (GridRide ride in rides)
                if (!aboard.Contains(ride.id) && !delivered.Contains(ride.id) && ride.origin == pos)
                { aboard.Add(ride.id); boarded = true; }
            if (boarded) { actions.Add("pickUp"); actions.Add("collectFare"); }

            var dropped = new List<int>();
            foreach (int id in aboard)
            {
                GridRide ride = Find(rides, id);
                if (ride != null && ride.dest == pos) dropped.Add(id);
            }
            if (dropped.Count > 0)
            {
                actions.Add("dropOff");
                foreach (int id in dropped) { aboard.Remove(id); delivered.Add(id); }
            }
        }

        // Finish at the terminal.
        List<Vector2Int> toEnd = GridPathfinder.Path(grid, pos, dest);
        actions.AddRange(GridPathfinder.ToActions(toEnd, facing));
        Advance(toEnd, ref pos, ref facing);

        return actions;
    }

    // -------------------------------------------------------------------------

    static Vector2Int? NearestTarget(GridModel grid, Vector2Int pos, List<GridRide> rides,
                                     HashSet<int> aboard, HashSet<int> delivered)
    {
        Vector2Int? best = null;
        int bestLen = int.MaxValue;
        foreach (GridRide ride in rides)
        {
            if (delivered.Contains(ride.id)) continue;
            Vector2Int target = aboard.Contains(ride.id) ? ride.dest : ride.origin;
            List<Vector2Int> path = GridPathfinder.Path(grid, pos, target);
            if (path == null) continue;
            if (path.Count < bestLen) { bestLen = path.Count; best = target; }
        }
        return best;
    }

    static void Advance(List<Vector2Int> path, ref Vector2Int pos, ref int facing)
    {
        if (path == null || path.Count == 0) return;
        pos = path[path.Count - 1];
        if (path.Count >= 2)
        {
            Vector2Int d = path[path.Count - 1] - path[path.Count - 2];
            for (int f = 0; f < AgentSim.FacingDeltas.Length; f++)
                if (AgentSim.FacingDeltas[f] == d) { facing = f; break; }
        }
    }

    static GridRide Find(List<GridRide> rides, int id)
    {
        foreach (GridRide ride in rides) if (ride.id == id) return ride;
        return null;
    }
}

/// <summary>
/// Runtime runner for the built-in autopilot: loads the rides, plans a route, and
/// drives the shared <see cref="AgentSim"/> cell-by-cell through the
/// <see cref="JeepneyAgentView"/> so it looks like real driving.
/// </summary>
public class SelfDriveAgent : MonoBehaviour
{
    public bool IsDriving { get; private set; }

    public IEnumerator Drive(GridModel grid, AgentSim sim, JeepneyAgentView view,
                             List<GridRide> rides, int startFacing, float stepSeconds,
                             AutomationPuzzleDefinition def, System.Action<bool> onDone)
    {
        IsDriving = true;
        sim.LoadRides(rides);
        sim.Reset();
        if (view != null) view.SnapTo(sim.Position, sim.Facing);

        List<string> plan = SelfDrivePlanner.Plan(grid, rides, startFacing, grid.DestPos);
        foreach (string action in plan)
        {
            AgentActionResult result = sim.Apply(action);
            if (view != null) yield return view.PlayAction(result, stepSeconds);
            else              yield return null;

            if (sim.IsWin(def)) break;
        }

        IsDriving = false;
        onDone?.Invoke(sim.IsWin(def));
    }
}
