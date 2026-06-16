using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static source of truth for all level content. Tutorial and Level 1
/// (Iloilo City / Molo) are fully authored; Levels 2–5 are locked stubs.
/// The automation maps and their canonical solutions are verified by
/// EditMode tests (Level1SolutionTests), so authoring mistakes fail CI.
/// </summary>
public static class LevelLibrary
{
    public const int Count = 6;

    public static readonly string[] Names =
    {
        "Tutorial",
        "Iloilo City (Molo)",
        "Oton",
        "Tigbauan",
        "Miag-ao",
        "San Joaquin",
    };

    // -------------------------------------------------------------------------

    /// <summary>Definition for a level index (0..5). Out of range → Tutorial.</summary>
    public static LevelDefinition Get(int index)
    {
        switch (index)
        {
            case 1:  return Level1Molo();
            case 2:  return Oton();
            case 3:  return Stub(3);
            case 4:  return Stub(4);
            case 5:  return Stub(5);
            default: return Tutorial();
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Derives a procedural layout from an authored manual route. Fixed stops become
    /// anchors (start, destination, and a few story beats); the rest of the trunk
    /// vertices are left for the generator to populate with ordinary stops.
    /// </summary>
    static ProceduralLayoutDefinition FromManual(ManualRouteDefinition m, TownGenParams gen)
    {
        var anchors = new List<AnchorNode>();
        for (int i = 0; i < m.stops.Length; i++)
        {
            ManualStopDefinition stop = m.stops[i];
            if (stop.waypointIndex < 0 || stop.waypointIndex >= m.waypoints.Length)
                continue;

            AnchorKind kind;
            if (i == 0)                   kind = AnchorKind.TerminalStart;
            else if (stop.isDestination)  kind = AnchorKind.TerminalEnd;
            else if (stop.waitingPassengers > 0 && anchors.Count % 2 == 1)
                kind = AnchorKind.NpcDrop;          // alternate flavor for story beats
            else if (stop.waitingPassengers > 0)
                kind = AnchorKind.HeritageSite;
            else
                continue; // ordinary trunk vertex; generator will create a regular stop

            anchors.Add(new AnchorNode
            {
                name     = stop.stopName,
                kind     = kind,
                position = m.waypoints[stop.waypointIndex]
            });
        }

        return new ProceduralLayoutDefinition
        {
            enabled = true,
            trunk   = m.waypoints,
            anchors = anchors.ToArray(),
            gen     = gen
        };
    }

    // -------------------------------------------------------------------------
    // Tutorial — linear sequencing, short intro drive

    static LevelDefinition Tutorial()
    {
        var manual = new ManualRouteDefinition
        {
            waypoints = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 24f),
                new Vector2(12f, 24f),
                new Vector2(12f, 60f),
            },
            roadHalfWidth = 3f,
            seatCapacity  = 4,
            breakdownAtRouteFraction = -1f,
            parTimeSeconds = 90f,
            stops = new[]
            {
                new ManualStopDefinition { stopName = "Garage",         waypointIndex = 0, waitingPassengers = 0 },
                new ManualStopDefinition { stopName = "Calle Real",     waypointIndex = 1, waitingPassengers = 2 },
                new ManualStopDefinition { stopName = "Plaza Terminal", waypointIndex = 3, isDestination = true },
            },
        };

        return new LevelDefinition
        {
            levelIndex  = 0,
            displayName = Names[0],
            hasContent  = true,
            fares       = new FareTable(),

            manual = manual,

            auto = new AutomationPuzzleDefinition
            {
                gridMap = new[]
                {
                    "########",
                    "#S..P..#",
                    "#####..#",
                    "#D.....#",
                    "########",
                },
                startFacing    = 1, // East
                goalText       = "Drive from the garage (S) to the terminal (D). Pick up the " +
                                 "waiting passenger (P), collect their fare, and drop them off.",
                allowedBlocks  = new[] { "moveForward", "turnLeft", "turnRight",
                                         "pickUp", "collectFare", "dropOff" },
                allowedQueries = new string[0],
                parSteps       = 17,
                softTimerSeconds = 300f,
                requireAllPassengersDelivered = true,
                codeScaffold =
                    "# Goal: drive from S to D.\n" +
                    "# Pick up the passenger at the stop (P), collect the fare,\n" +
                    "# then drop them off at the terminal.\n" +
                    "# Actions: moveForward(), turnLeft(), turnRight(),\n" +
                    "#          pickUp(), collectFare(), dropOff()\n",
                optimalSolutionText =
                    "moveForward()\n" +
                    "moveForward()\n" +
                    "moveForward()\n" +
                    "pickUp()\n" +
                    "collectFare()\n" +
                    "moveForward()\n" +
                    "moveForward()\n" +
                    "turnRight()\n" +
                    "moveForward()\n" +
                    "moveForward()\n" +
                    "turnRight()\n" +
                    "moveForward()\n" +
                    "moveForward()\n" +
                    "moveForward()\n" +
                    "moveForward()\n" +
                    "moveForward()\n" +
                    "dropOff()\n",
            },

            procedural = FromManual(manual, new TownGenParams
            {
                branchCountMin = 0, branchCountMax = 1,
                branchSpacing  = 18f,
                branchLenMin   = 6f, branchLenMax = 10f,
                passengerCountMin = 1, passengerCountMax = 2,
                passengerDensity  = 0.5f,
                gridCellSize      = 6f,
            }),
        };
    }

    // -------------------------------------------------------------------------
    // Level 1 — Iloilo City (Molo): conditionals (while / if), maze escape

    static LevelDefinition Level1Molo()
    {
        var manual = new ManualRouteDefinition
        {
            waypoints = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 30f),
                new Vector2(18f, 30f),
                new Vector2(18f, 72f),
                new Vector2(0f, 72f),
                new Vector2(0f, 114f),
                new Vector2(24f, 114f),
                new Vector2(24f, 156f),
            },
            roadHalfWidth = 3f,
            seatCapacity  = 8,
            breakdownAtRouteFraction = 0.55f,
            parTimeSeconds = 240f,
            stops = new[]
            {
                new ManualStopDefinition { stopName = "Iloilo Terminal", waypointIndex = 0, waitingPassengers = 0 },
                new ManualStopDefinition { stopName = "Molo Church",     waypointIndex = 1, waitingPassengers = 2 },
                new ManualStopDefinition { stopName = "Yusay-Consing",   waypointIndex = 3, waitingPassengers = 3 },
                new ManualStopDefinition { stopName = "Avanceña St",     waypointIndex = 5, waitingPassengers = 2 },
                new ManualStopDefinition { stopName = "Molo Plaza",      waypointIndex = 7, isDestination = true },
            },
        };

        return new LevelDefinition
        {
            levelIndex  = 1,
            displayName = Names[1],
            hasContent  = true,
            fares       = new FareTable(),
            townPuzzle  = TownPuzzleKind.FlowConnect,   // Molo: non-intersecting transit links

            manual = manual,

            auto = new AutomationPuzzleDefinition
            {
                // Tree maze (no loops) — the right-hand rule visits every cell.
                gridMap = new[]
                {
                    "###########",
                    "#S....#...#",
                    "#####.#.#.#",
                    "#P....#.#.#",
                    "#.#####.#.#",
                    "#.#...P.#.#",
                    "#.#.#####.#",
                    "#...#....D#",
                    "###########",
                },
                startFacing    = 1, // East
                goalText       = "Escape the Molo back-alleys to the plaza (D). The route twists — " +
                                 "use while and if to feel your way along the walls. Pick up both " +
                                 "waiting passengers (P) and collect their fares.",
                allowedBlocks  = new[] { "moveForward", "turnLeft", "turnRight",
                                         "pickUp", "dropOff", "collectFare",
                                         "while", "if", "ifElse" },
                allowedQueries = new[] { "frontIsClear", "leftIsClear", "rightIsClear",
                                         "atStop", "atDestination" },
                parSteps       = 50,
                softTimerSeconds = 480f,
                requireAllPassengersDelivered = true,
                codeScaffold =
                    "# Goal: reach Molo Plaza (D).\n" +
                    "# Walls block the way - use while / if to follow them.\n" +
                    "# Queries: frontIsClear(), leftIsClear(), rightIsClear(),\n" +
                    "#          atStop(), atDestination()\n" +
                    "# Actions: moveForward(), turnLeft(), turnRight(),\n" +
                    "#          pickUp(), dropOff(), collectFare()\n",
                optimalSolutionText =
                    "while not atDestination():\n" +
                    "    if rightIsClear():\n" +
                    "        turnRight()\n" +
                    "        moveForward()\n" +
                    "    else:\n" +
                    "        if frontIsClear():\n" +
                    "            moveForward()\n" +
                    "        else:\n" +
                    "            turnLeft()\n" +
                    "    if atStop():\n" +
                    "        pickUp()\n" +
                    "        collectFare()\n" +
                    "dropOff()\n",
            },

            procedural = FromManual(manual, new TownGenParams
            {
                branchCountMin = 1, branchCountMax = 2,
                branchSpacing  = 18f,
                branchLenMin   = 8f, branchLenMax = 14f,
                passengerCountMin = 2, passengerCountMax = 4,
                passengerDensity  = 0.7f,
                gridCellSize      = 6f,
            }),
        };
    }

    // -------------------------------------------------------------------------
    // Level 2 — Oton: code gate is a maze (Reeborg-style), non-code gate stacks
    // market crates. Minimal playable leg; heritage content lands in a later pass.

    static LevelDefinition Oton()
    {
        // The automation puzzle is a curated perfect maze (verified solvable by
        // the wall-follower in MazeLibraryTests); flag it so CodeDrive uses this
        // grid as-is rather than deriving one from the manual route.
        AutomationPuzzleDefinition maze = MazeLibrary.Get(3);
        maze.useAuthoredGrid = true;
        maze.goalText =
            "Oton back-lanes: program the jeepney out of the maze to the market (D). " +
            "Keep one hand on the wall — while not atDestination(), feel along it with if / else.";

        return new LevelDefinition
        {
            levelIndex  = 2,
            displayName = Names[2],
            hasContent  = true,
            fares       = new FareTable(),
            townPuzzle  = TownPuzzleKind.CrateStack,
            auto        = maze,

            manual = new ManualRouteDefinition
            {
                waypoints = new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(0f, 28f),
                    new Vector2(16f, 28f),
                    new Vector2(16f, 70f),
                    new Vector2(16f, 84f),
                    new Vector2(2f, 84f),
                    new Vector2(2f, 112f),
                },
                roadHalfWidth = 3f,
                seatCapacity  = 8,
                breakdownAtRouteFraction = 0.5f,
                parTimeSeconds = 200f,
                stops = new[]
                {
                    new ManualStopDefinition { stopName = "Molo Boundary", waypointIndex = 0, waitingPassengers = 0 },
                    new ManualStopDefinition { stopName = "Batiano River", waypointIndex = 1, waitingPassengers = 2 },
                    new ManualStopDefinition { stopName = "Poblacion",     waypointIndex = 3, waitingPassengers = 2 },
                    new ManualStopDefinition { stopName = "Oton Market",   waypointIndex = 6, isDestination = true },
                },
            },

            // Per-run procedural town. Fixed anchors (terminals + heritage / NPC
            // drops) pin to the authored trunk above; the generator hangs branch
            // side-streets and ordinary-passenger rides around them. The authored
            // manual/auto stay as the deterministic fallback (enabled = false).
            procedural = new ProceduralLayoutDefinition
            {
                enabled = true,
                trunk = new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(0f, 30f),
                    new Vector2(18f, 30f),
                    new Vector2(18f, 72f),
                    new Vector2(0f, 72f),
                    new Vector2(0f, 114f),
                    new Vector2(6f, 114f),
                },
                anchors = new[]
                {
                    new AnchorNode { name = "Molo Boundary", kind = AnchorKind.TerminalStart, position = new Vector2(0f, 0f) },
                    new AnchorNode { name = "Batiano River", kind = AnchorKind.NpcDrop,       position = new Vector2(0f, 30f) },
                    new AnchorNode { name = "Poblacion",     kind = AnchorKind.HeritageSite,  position = new Vector2(18f, 72f) },
                    new AnchorNode { name = "Oton Market",   kind = AnchorKind.TerminalEnd,   position = new Vector2(6f, 114f) },
                },
                gen = new TownGenParams
                {
                    branchCountMin = 1, branchCountMax = 3,
                    branchSpacing  = 18f,
                    branchLenMin   = 8f, branchLenMax = 14f,
                    passengerCountMin = 2, passengerCountMax = 5,
                    passengerDensity  = 0.8f,
                    gridCellSize      = 6f,
                },
            },
        };
    }

    // -------------------------------------------------------------------------

    /// <summary>Locked "coming soon" entry for towns not built yet.</summary>
    static LevelDefinition Stub(int index)
    {
        return new LevelDefinition
        {
            levelIndex  = index,
            displayName = Names[index],
            hasContent  = false,
        };
    }
}
