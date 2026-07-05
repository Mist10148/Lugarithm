using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static source of truth for all level content: Tutorial plus the five coastal
/// towns. The automation maps and their canonical solutions are verified by
/// EditMode tests, so authoring mistakes fail CI.
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
            case 3:  return Tigbauan();
            case 4:  return Miagao();
            case 5:  return SanJoaquin();
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
            overworldSceneName = "TopDownLevel",

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
                goalText       = "Teach her to decide for herself. Drive from the garage (S) to the " +
                                 "terminal (D). Use an if to pick up the waiting passenger (P) only " +
                                 "when someone's there, collect the fare, and drop them off. Press Run " +
                                 "as many times as you like — she keeps her place between Runs.",
                // Conditionals arrive in the tutorial: if / else plus the passenger and
                // fare questions to branch on. Movement/board/fare actions stay too.
                allowedBlocks  = new[] { "moveForward", "turnLeft", "turnRight",
                                         "pickUp", "collectFare", "dropOff",
                                         "if", "ifElse" },
                allowedQueries = new[] { "passengerWaiting", "atStop", "atRequestedStop",
                                         "hasPassengerAboard", "frontIsClear" },
                parSteps       = 17,
                softTimerSeconds = 300f,
                requireAllPassengersDelivered = true,
                codeScaffold =
                    "# Goal: drive from S to D and serve the passenger (P).\n" +
                    "# New idea: an 'if' runs a command only when its question is true —\n" +
                    "#   if passengerWaiting():\n" +
                    "#       pickUp()\n" +
                    "# Press Run again and again: she keeps her place, riders and fares\n" +
                    "# between Runs. Reset sends her back to the garage.\n" +
                    "# Questions: passengerWaiting(), atStop(), atRequestedStop(),\n" +
                    "#            hasPassengerAboard(), frontIsClear()\n" +
                    "# Actions: moveForward(), turnLeft(), turnRight(),\n" +
                    "#          pickUp(), collectFare(), dropOff()\n",
                // Same 17 physical actions as a flat drive, but the board/fare pair is
                // guarded by a conditional — control-flow headers cost no steps, so par
                // stays 17 while the solution now demonstrates 'if'.
                optimalSolutionText =
                    "moveForward()\n" +
                    "moveForward()\n" +
                    "moveForward()\n" +
                    "if passengerWaiting():\n" +
                    "    pickUp()\n" +
                    "    collectFare()\n" +
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
                // No side-streets: one continuous forward road (stops sit on the trunk).
                branchCountMin = 0, branchCountMax = 0,
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
            overworldSceneName = "TopDownLevel",        // walk Molo + talk to NPCs, then board

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
                goalText       = "Escape the Molo back-alleys to the plaza (D). You can't count these " +
                                 "twists by hand — wrap your steps in a loop. 'while not atDestination():' " +
                                 "repeats until you're through; keep one hand on the wall inside it. Pick " +
                                 "up both waiting passengers (P) and collect their fares.",
                // Loops become the headline: while / for join the conditionals from the
                // tutorial. Same maze, but now the point is repetition, not counting.
                allowedBlocks  = new[] { "moveForward", "turnLeft", "turnRight",
                                         "pickUp", "dropOff", "collectFare",
                                         "while", "for", "if", "ifElse" },
                allowedQueries = new[] { "frontIsClear", "leftIsClear", "rightIsClear",
                                         "atStop", "atDestination" },
                parSteps       = 50,
                softTimerSeconds = 480f,
                requireAllPassengersDelivered = true,
                codeScaffold =
                    "# Goal: reach Molo Plaza (D).\n" +
                    "# New idea: a loop repeats steps for you, so you never count the road.\n" +
                    "#   while not atDestination():\n" +
                    "#       # feel along the wall in here\n" +
                    "# (An endless street uses the same shape: while moreRoad(): ...)\n" +
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
                // No side-streets: one continuous forward road (stops sit on the trunk).
                branchCountMin = 0, branchCountMax = 0,
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
            "Oton back-lanes: name the move once, reuse it everywhere. Wrap the wall-follow " +
            "step in a function — def followWall(): — then let a loop call it out to the market (D). " +
            "One tidy idea, repeated: that's what a function is for.";
        // Level 2 teaches defining functions. Add 'def' to the palette and rewrite the
        // reference so the wall-follower lives in a helper — identical actions to the
        // inline solver, so it still solves the maze, but now it demonstrates def.
        maze.allowedBlocks = new[] { "moveForward", "turnLeft", "turnRight",
                                     "while", "if", "ifElse", "def" };
        maze.codeScaffold =
            "# Goal: reach the Oton market (D).\n" +
            "# New idea: a function names a routine so you can reuse it.\n" +
            "#   def followWall():\n" +
            "#       # one wall-follow decision goes here\n" +
            "#   while not atDestination():\n" +
            "#       followWall()\n" +
            "# Queries: frontIsClear(), leftIsClear(), rightIsClear(), atDestination()\n" +
            "# Actions: moveForward(), turnLeft(), turnRight()\n";
        maze.optimalSolutionText =
            "def followWall():\n" +
            "    if rightIsClear():\n" +
            "        turnRight()\n" +
            "        moveForward()\n" +
            "    else:\n" +
            "        if frontIsClear():\n" +
            "            moveForward()\n" +
            "        else:\n" +
            "            turnLeft()\n" +
            "\n" +
            "while not atDestination():\n" +
            "    followWall()\n";

        return new LevelDefinition
        {
            levelIndex  = 2,
            displayName = Names[2],
            hasContent  = true,
            fares       = new FareTable(),
            townPuzzle  = TownPuzzleKind.CrateStack,
            overworldSceneName = "TopDownLevel",        // walk Oton + talk to NPCs, then board
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
                    // No side-streets: one continuous forward road (stops sit on the trunk).
                    branchCountMin = 0, branchCountMax = 0,
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

    // -------------------------------------------------------------------------
    // Level 3 - Tigbauan: functions + loops, hablon pattern road

    static LevelDefinition Tigbauan()
    {
        var manual = new ManualRouteDefinition
        {
            waypoints = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 34f),
                new Vector2(20f, 34f),
                new Vector2(20f, 76f),
                new Vector2(44f, 76f),
                new Vector2(44f, 116f),
                new Vector2(18f, 116f),
                new Vector2(18f, 154f),
                new Vector2(34f, 154f),
                new Vector2(34f, 188f),
            },
            roadHalfWidth = 3f,
            seatCapacity = 9,
            breakdownAtRouteFraction = 0.48f,
            parTimeSeconds = 260f,
            stops = new[]
            {
                new ManualStopDefinition { stopName = "Oton Boundary", waypointIndex = 0, waitingPassengers = 0 },
                new ManualStopDefinition { stopName = "Tigbauan Poblacion", waypointIndex = 1, waitingPassengers = 2 },
                new ManualStopDefinition { stopName = "San Juan de Sahagun", waypointIndex = 3, waitingPassengers = 2 },
                new ManualStopDefinition { stopName = "Bantayan Road", waypointIndex = 5, waitingPassengers = 3 },
                new ManualStopDefinition { stopName = "Hablon Looms", waypointIndex = 7, waitingPassengers = 2 },
                new ManualStopDefinition { stopName = "Weaving Village", waypointIndex = 9, isDestination = true },
            },
        };

        return new LevelDefinition
        {
            levelIndex = 3,
            displayName = Names[3],
            hasContent = true,
            fares = new FareTable(),
            townPuzzle = TownPuzzleKind.FlowConnect,
            overworldSceneName = "TopDownLevel",
            manual = manual,
            auto = SelfDrivePlanner.TemplateForLevel(3),
            procedural = new ProceduralLayoutDefinition
            {
                enabled = true,
                trunk = manual.waypoints,
                anchors = new[]
                {
                    new AnchorNode { name = "Oton Boundary", kind = AnchorKind.TerminalStart, position = manual.waypoints[0] },
                    new AnchorNode { name = "Tigbauan Poblacion", kind = AnchorKind.HeritageSite, position = manual.waypoints[1] },
                    new AnchorNode { name = "San Juan de Sahagun", kind = AnchorKind.HeritageSite, position = manual.waypoints[3] },
                    new AnchorNode { name = "Bantayan Road", kind = AnchorKind.NpcDrop, position = manual.waypoints[5] },
                    new AnchorNode { name = "Hablon Looms", kind = AnchorKind.HeritageSite, position = manual.waypoints[7] },
                    new AnchorNode { name = "Weaving Village", kind = AnchorKind.TerminalEnd, position = manual.waypoints[9] },
                },
                gen = new TownGenParams
                {
                    branchCountMin = 0, branchCountMax = 0,
                    branchSpacing = 18f,
                    branchLenMin = 10f, branchLenMax = 16f,
                    passengerCountMin = 4, passengerCountMax = 6,
                    passengerDensity = 0.9f,
                    gridCellSize = 6f,
                },
            },
        };
    }

    // -------------------------------------------------------------------------
    // Level 4 - Miag-ao: nested conditionals, layered facade road

    static LevelDefinition Miagao()
    {
        var manual = new ManualRouteDefinition
        {
            waypoints = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 32f),
                new Vector2(22f, 32f),
                new Vector2(22f, 70f),
                new Vector2(48f, 70f),
                new Vector2(48f, 108f),
                new Vector2(26f, 108f),
                new Vector2(26f, 146f),
                new Vector2(56f, 146f),
                new Vector2(56f, 188f),
                new Vector2(36f, 188f),
                new Vector2(36f, 226f),
            },
            roadHalfWidth = 3f,
            seatCapacity = 10,
            breakdownAtRouteFraction = 0.52f,
            parTimeSeconds = 300f,
            stops = new[]
            {
                new ManualStopDefinition { stopName = "Tigbauan South Road", waypointIndex = 0, waitingPassengers = 0 },
                new ManualStopDefinition { stopName = "Guimbal Crossing", waypointIndex = 2, waitingPassengers = 2 },
                new ManualStopDefinition { stopName = "Coastal Watch Road", waypointIndex = 4, waitingPassengers = 2 },
                new ManualStopDefinition { stopName = "Miag-ao Poblacion", waypointIndex = 6, waitingPassengers = 3 },
                new ManualStopDefinition { stopName = "Indag-an Hablon", waypointIndex = 8, waitingPassengers = 2 },
                new ManualStopDefinition { stopName = "Miag-ao Church", waypointIndex = 11, isDestination = true },
            },
        };

        return new LevelDefinition
        {
            levelIndex = 4,
            displayName = Names[4],
            hasContent = true,
            fares = new FareTable(),
            townPuzzle = TownPuzzleKind.FlowConnect,
            overworldSceneName = "TopDownLevel",
            manual = manual,
            auto = SelfDrivePlanner.TemplateForLevel(4),
            procedural = new ProceduralLayoutDefinition
            {
                enabled = true,
                trunk = manual.waypoints,
                anchors = new[]
                {
                    new AnchorNode { name = "Tigbauan South Road", kind = AnchorKind.TerminalStart, position = manual.waypoints[0] },
                    new AnchorNode { name = "Guimbal Crossing", kind = AnchorKind.HeritageSite, position = manual.waypoints[2] },
                    new AnchorNode { name = "Coastal Watch Road", kind = AnchorKind.NpcDrop, position = manual.waypoints[4] },
                    new AnchorNode { name = "Miag-ao Poblacion", kind = AnchorKind.HeritageSite, position = manual.waypoints[6] },
                    new AnchorNode { name = "Indag-an Hablon", kind = AnchorKind.NpcDrop, position = manual.waypoints[8] },
                    new AnchorNode { name = "Miag-ao Church", kind = AnchorKind.TerminalEnd, position = manual.waypoints[11] },
                },
                gen = new TownGenParams
                {
                    branchCountMin = 0, branchCountMax = 0,
                    branchSpacing = 18f,
                    branchLenMin = 10f, branchLenMax = 16f,
                    passengerCountMin = 5, passengerCountMax = 7,
                    passengerDensity = 1f,
                    gridCellSize = 6f,
                },
            },
        };
    }

    // -------------------------------------------------------------------------
    // Level 5 - San Joaquin: multi-variable constraints, final road

    static LevelDefinition SanJoaquin()
    {
        var manual = new ManualRouteDefinition
        {
            waypoints = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 36f),
                new Vector2(24f, 36f),
                new Vector2(24f, 76f),
                new Vector2(54f, 76f),
                new Vector2(54f, 118f),
                new Vector2(30f, 118f),
                new Vector2(30f, 158f),
                new Vector2(66f, 158f),
                new Vector2(66f, 204f),
                new Vector2(42f, 204f),
                new Vector2(42f, 250f),
                new Vector2(72f, 250f),
                new Vector2(72f, 294f),
            },
            roadHalfWidth = 3f,
            seatCapacity = 10,
            breakdownAtRouteFraction = 0.6f,
            parTimeSeconds = 330f,
            stops = new[]
            {
                new ManualStopDefinition { stopName = "Miag-ao Boundary", waypointIndex = 0, waitingPassengers = 0 },
                new ManualStopDefinition { stopName = "Southern Coast", waypointIndex = 1, waitingPassengers = 2 },
                new ManualStopDefinition { stopName = "San Joaquin Poblacion", waypointIndex = 3, waitingPassengers = 3 },
                new ManualStopDefinition { stopName = "Battle Facade", waypointIndex = 5, waitingPassengers = 2 },
                new ManualStopDefinition { stopName = "Sea Road", waypointIndex = 7, waitingPassengers = 3 },
                new ManualStopDefinition { stopName = "Campo Santo Road", waypointIndex = 10, waitingPassengers = 2 },
                new ManualStopDefinition { stopName = "Campo Santo", waypointIndex = 13, isDestination = true },
            },
        };

        return new LevelDefinition
        {
            levelIndex = 5,
            displayName = Names[5],
            hasContent = true,
            fares = new FareTable(),
            townPuzzle = TownPuzzleKind.FlowConnect,
            overworldSceneName = "TopDownLevel",
            manual = manual,
            auto = SelfDrivePlanner.TemplateForLevel(5),
            procedural = new ProceduralLayoutDefinition
            {
                enabled = true,
                trunk = manual.waypoints,
                anchors = new[]
                {
                    new AnchorNode { name = "Miag-ao Boundary", kind = AnchorKind.TerminalStart, position = manual.waypoints[0] },
                    new AnchorNode { name = "Southern Coast", kind = AnchorKind.NpcDrop, position = manual.waypoints[1] },
                    new AnchorNode { name = "San Joaquin Poblacion", kind = AnchorKind.HeritageSite, position = manual.waypoints[3] },
                    new AnchorNode { name = "Battle Facade", kind = AnchorKind.HeritageSite, position = manual.waypoints[5] },
                    new AnchorNode { name = "Sea Road", kind = AnchorKind.NpcDrop, position = manual.waypoints[7] },
                    new AnchorNode { name = "Campo Santo Road", kind = AnchorKind.HeritageSite, position = manual.waypoints[10] },
                    new AnchorNode { name = "Campo Santo", kind = AnchorKind.TerminalEnd, position = manual.waypoints[13] },
                },
                gen = new TownGenParams
                {
                    branchCountMin = 0, branchCountMax = 0,
                    branchSpacing = 18f,
                    branchLenMin = 10f, branchLenMax = 16f,
                    passengerCountMin = 6, passengerCountMax = 8,
                    passengerDensity = 1f,
                    gridCellSize = 6f,
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
