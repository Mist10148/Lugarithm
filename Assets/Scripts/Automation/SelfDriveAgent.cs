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
        "moveForward", "turnLeft", "turnRight", "pickUp", "dropOff", "collectFare", "giveChange",
        "driveToNextStop", "driveToTerminal", "driveToDropoff", "keepDriving",
        "while", "if", "ifElse",
        "functionDef", "callFunction",
    };

    public static readonly string[] NavQueries =
    {
        "frontIsClear", "leftIsClear", "rightIsClear", "atStop", "routeComplete", "moreRoad",
        "hasPassengerAboard", "atRequestedStop", "passengerWaiting",
    };

    public static readonly string[] NavReporters =
    {
        "fareOwed", "cashTendered", "changeOwed",
        "seatsLeft", "passengerCount", "distanceToDestination", "distanceTraveled",
    };

    public const string ReferenceSolution =
        "# Self-driving jeepney for the endless road: cruise forever, tend every rider, and\n" +
        "# drop your front-seat story passenger at their marked stop along the way.\n" +
        "# moreRoad() is always true (the road never ends); driveToDropoff() cruises until the\n" +
        "# story drop-off is ready, then heads straight for it.\n" +
        "def drive():\n" +
        "    while moreRoad():\n" +
        "        driveToDropoff()\n" +
        "        handleDropoffs()\n" +
        "        handlePassengers()\n" +
        "        handleFares()\n" +
        "\n" +
        "def handlePassengers():\n" +
        "    if passengerWaiting():\n" +
        "        pickUp()\n" +
        "\n" +
        "def handleFares():\n" +
        "    if hasPassengerAboard():\n" +
        "        collectFare()\n" +
        "        giveChange(changeOwed())\n" +
        "\n" +
        "def handleDropoffs():\n" +
        "    if atRequestedStop():\n" +
        "        dropOff()\n" +
        "\n" +
        "drive()\n";

    public const string Level3ReferenceSolution =
        "# Tigbauan pattern: name each repeated move once, then let the loop weave the trip.\n" +
        "def drive():\n" +
        "    while not routeComplete():\n" +
        "        driveToDropoff()\n" +
        "        tendStop()\n" +
        "\n" +
        "def tendStop():\n" +
        "    handleDropoffs()\n" +
        "    handlePassengers()\n" +
        "    handleFares()\n" +
        "\n" +
        "def handlePassengers():\n" +
        "    if passengerWaiting():\n" +
        "        pickUp()\n" +
        "\n" +
        "def handleFares():\n" +
        "    if hasPassengerAboard():\n" +
        "        collectFare()\n" +
        "        giveChange(changeOwed())\n" +
        "\n" +
        "def handleDropoffs():\n" +
        "    if atRequestedStop():\n" +
        "        dropOff()\n" +
        "\n" +
        "drive()\n";

    public const string Level4ReferenceSolution =
        "# Miag-ao facade logic: decisions inside decisions, like layers in the stone.\n" +
        "def drive():\n" +
        "    while not routeComplete():\n" +
        "        driveToDropoff()\n" +
        "        if atRequestedStop():\n" +
        "            if hasPassengerAboard():\n" +
        "                settleFare()\n" +
        "                dropOff()\n" +
        "        else:\n" +
        "            if passengerWaiting():\n" +
        "                if seatsLeft() > 0:\n" +
        "                    pickUp()\n" +
        "                    settleFare()\n" +
        "\n" +
        "def settleFare():\n" +
        "    if fareOwed() > 0:\n" +
        "        collectFare()\n" +
        "        if changeOwed() > 0:\n" +
        "            giveChange(changeOwed())\n" +
        "\n" +
        "drive()\n";

    public const string Level5ReferenceSolution =
        "# San Joaquin final road: hold seats, fares, change, and drop-offs together.\n" +
        "def drive():\n" +
        "    while not routeComplete():\n" +
        "        driveToDropoff()\n" +
        "        serveCurrentStop()\n" +
        "\n" +
        "def serveCurrentStop():\n" +
        "    if atRequestedStop():\n" +
        "        if fareOwed() > 0:\n" +
        "            collectFare()\n" +
        "        if changeOwed() > 0:\n" +
        "            giveChange(changeOwed())\n" +
        "        dropOff()\n" +
        "    if passengerWaiting():\n" +
        "        if seatsLeft() > 0:\n" +
        "            pickUp()\n" +
        "            if fareOwed() > 0:\n" +
        "                collectFare()\n" +
        "            if changeOwed() > 0:\n" +
        "                giveChange(changeOwed())\n" +
        "\n" +
        "drive()\n";

    /// <summary>Synthesizes rides for an authored grid that has only generic 'P'
    /// stops (no committed per-passenger routes): every stop is a pickup bound for
    /// the terminal D. Lets the ride-mode autopilot drive authored levels too —
    /// picking up, collecting fares, and dropping at the destination. Returns an
    /// empty list when the grid has no stops (a plain start→destination run).</summary>
    public static List<GridRide> RidesFromGrid(GridModel grid, FareTable fares)
    {
        var rides = new List<GridRide>();
        if (grid == null) return rides;
        int fare = fares != null ? fares.baseFare : 13;
        int id = 0;
        foreach (Vector2Int stop in grid.StopCells)
            rides.Add(new GridRide
            {
                id     = id++,
                origin = stop,
                dest   = grid.DestPos,
                fare   = fare,
                tender = fare,
                color  = new Color(0.95f, 0.65f, 0.15f),
                destLabel = "Stop",   // no per-stop name data on an authored grid
            });
        return rides;
    }

    /// <summary>Maps the layout's committed rides onto grid cells (needs a projected layout).</summary>
    public static List<GridRide> RidesFromLayout(TownLayout layout)
    {
        var rides = new List<GridRide>();
        foreach (PassengerRequest req in layout.requests)
        {
            TownNode destNode = layout.Node(req.destNodeId);
            rides.Add(new GridRide
            {
                id     = req.id,
                originNodeId = req.originNodeId,
                destNodeId = req.destNodeId,
                origin = layout.Node(req.originNodeId).gridCell,
                dest   = destNode.gridCell,
                fare   = req.fare,
                tender = req.tender,
                color  = req.color,
                destLabel = !string.IsNullOrEmpty(destNode.name) ? destNode.name : "Stop",
            });
        }
        return rides;
    }

    /// <summary>
    /// Projects a generated town into an Automation puzzle (authored grid) and the
    /// matching ride list, with par set to the autopilot's own plan length.
    /// </summary>
    public static AutomationPuzzleDefinition BuildPuzzle(TownLayout layout, float cellSize,
                                                         out List<GridRide> rides, out int startFacing)
        => BuildPuzzle(layout, cellSize, levelIndex: 2, out rides, out startFacing);

    public static AutomationPuzzleDefinition BuildPuzzle(TownLayout layout, float cellSize,
                                                         int levelIndex,
                                                         out List<GridRide> rides, out int startFacing)
    {
        string[] map = GridLayoutProjector.ToGridMap(layout, cellSize, out startFacing, out _);
        rides = RidesFromLayout(layout);

        GridModel grid = GridModel.Parse(map, out _);
        List<string> plan = Plan(grid, rides, startFacing, grid.DestPos);
        AutomationPuzzleDefinition template = TemplateForLevel(levelIndex);

        return new AutomationPuzzleDefinition
        {
            gridMap         = map,
            startFacing     = startFacing,
            useAuthoredGrid = true,
            requireAllPassengersDelivered = true,
            endlessRoute    = true,   // procedural street keeps streaming; win = required riders delivered
            allowedBlocks   = NavBlocks,
            allowedQueries  = NavQueries,
            allowedReporters = NavReporters,
            parSteps        = plan.Count,
            softTimerSeconds = template.softTimerSeconds,
            goalText = template.goalText,
            codeScaffold = template.codeScaffold,
            optimalSolutionText = template.optimalSolutionText,
        };
    }

    public static AutomationPuzzleDefinition TemplateForLevel(int levelIndex)
    {
        switch (levelIndex)
        {
            case 3:
                return new AutomationPuzzleDefinition
                {
                    gridMap = new[] { "#####", "#S.D#", "#####" },
                    startFacing = 1,
                    parSteps = 2,
                    requireAllPassengersDelivered = false,
                    allowedBlocks = NavBlocks,
                    allowedQueries = NavQueries,
                    allowedReporters = NavReporters,
                    softTimerSeconds = 600f,
                    goalText = "Tigbauan weave run: use helper functions and a loop to repeat the safe service pattern. " +
                               "Drive to each needed stop, pick up riders, collect fares, give exact change, and drop " +
                               "your story passenger at the marked weaving village stop.",
                    codeScaffold =
                        "# New idea: functions name the pattern; loops repeat it.\n" +
                        "# Try helpers like drive(), tendStop(), handlePassengers(), handleFares().\n" +
                        "# Navigation: driveToDropoff()\n" +
                        "# Service: pickUp(), collectFare(), giveChange(changeOwed()), dropOff()\n" +
                        "# Ask: passengerWaiting(), hasPassengerAboard(), atRequestedStop(), routeComplete()\n",
                    optimalSolutionText = Level3ReferenceSolution,
                };
            case 4:
                return new AutomationPuzzleDefinition
                {
                    gridMap = new[] { "#####", "#S.D#", "#####" },
                    startFacing = 1,
                    parSteps = 2,
                    requireAllPassengersDelivered = false,
                    allowedBlocks = NavBlocks,
                    allowedQueries = NavQueries,
                    allowedReporters = NavReporters,
                    softTimerSeconds = 600f,
                    goalText = "Miag-ao fortress run: decisions now sit inside other decisions. At each stop, check " +
                               "drop-offs before pickups, settle fare/change, and only board riders when seats remain.",
                    codeScaffold =
                        "# New idea: nested if statements handle layered rules.\n" +
                        "# Check requested stops, then passengers, then fare/change.\n" +
                        "# Reporters: seatsLeft(), fareOwed(), changeOwed()\n" +
                        "# Queries: atRequestedStop(), passengerWaiting(), hasPassengerAboard(), routeComplete()\n",
                    optimalSolutionText = Level4ReferenceSolution,
                };
            case 5:
                return new AutomationPuzzleDefinition
                {
                    gridMap = new[] { "#####", "#S.D#", "#####" },
                    startFacing = 1,
                    parSteps = 2,
                    requireAllPassengersDelivered = false,
                    allowedBlocks = NavBlocks,
                    allowedQueries = NavQueries,
                    allowedReporters = NavReporters,
                    softTimerSeconds = 600f,
                    goalText = "San Joaquin final road: balance several constraints at once. Watch seats, fares, " +
                               "change, waiting riders, and requested drop-offs until the last passenger reaches Campo Santo.",
                    codeScaffold =
                        "# Final idea: coordinate many values at once.\n" +
                        "# Use seatsLeft(), fareOwed(), changeOwed(), passengerWaiting(), atRequestedStop().\n" +
                        "# Keep looping until routeComplete(), then the final reveal can land.\n",
                    optimalSolutionText = Level5ReferenceSolution,
                };
            default:
                return new AutomationPuzzleDefinition
                {
                    gridMap = new[] { "#####", "#S.D#", "#####" },
                    startFacing = 1,
                    parSteps = 2,
                    requireAllPassengersDelivered = false,
                    allowedBlocks = NavBlocks,
                    allowedQueries = NavQueries,
                    allowedReporters = NavReporters,
                    softTimerSeconds = 600f,
                    goalText = "Endless run: the road never ends. Pick up riders, collect fares, give exact " +
                               "change, and drop your story passenger at their stop (driveToDropoff()). The leg " +
                               "completes when they are delivered - keepDriving() to cruise on and serve more.",
                    codeScaffold =
                        "# Split the ride into helper functions, then call drive():\n" +
                        "#   drive(), handlePassengers(), handleFares(), handleDropoffs()\n" +
                        "# Navigation: driveToNextStop(), driveToDropoff(), keepDriving()\n" +
                        "# Tend riders: pickUp(), collectFare(), giveChange(changeOwed()), dropOff()\n" +
                        "# Ask: passengerWaiting(), hasPassengerAboard(), atRequestedStop(), routeComplete()\n" +
                        "# Cruise forever:  while True:  keepDriving()\n",
                    optimalSolutionText = ReferenceSolution,
                };
        }
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
            if (boarded) { actions.Add("pickUp"); actions.Add("collectFare"); actions.Add("giveChange"); }

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

    public IEnumerator Drive(GridModel grid, AgentSim sim, IAgentView view,
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
