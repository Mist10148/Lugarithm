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
            case 2:  return Stub(2);
            case 3:  return Stub(3);
            case 4:  return Stub(4);
            case 5:  return Stub(5);
            default: return Tutorial();
        }
    }

    // -------------------------------------------------------------------------
    // Tutorial — linear sequencing, short intro drive

    static LevelDefinition Tutorial()
    {
        return new LevelDefinition
        {
            levelIndex  = 0,
            displayName = Names[0],
            hasContent  = true,
            fares       = new FareTable(),

            manual = new ManualRouteDefinition
            {
                waypoints = new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(0f, 24f),
                    new Vector2(12f, 36f),
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
            },

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
                goalText       = "Drive from the garage (S) to the terminal (D). " +
                                 "Pick up the waiting passenger (P) on the way and drop them off.",
                allowedBlocks  = new[] { "moveForward", "turnLeft", "turnRight", "pickUp", "dropOff" },
                allowedQueries = new string[0],
                parSteps       = 16,
                softTimerSeconds = 300f,
                requireAllPassengersDelivered = true,
                codeScaffold =
                    "# Goal: drive from S to D.\n" +
                    "# Pick up the passenger at the stop (P) on the way.\n" +
                    "# Actions: moveForward(), turnLeft(), turnRight(),\n" +
                    "#          pickUp(), dropOff()\n",
                optimalSolutionText =
                    "moveForward()\n" +
                    "moveForward()\n" +
                    "moveForward()\n" +
                    "pickUp()\n" +
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
        };
    }

    // -------------------------------------------------------------------------
    // Level 1 — Iloilo City (Molo): conditionals (while / if), maze escape

    static LevelDefinition Level1Molo()
    {
        return new LevelDefinition
        {
            levelIndex  = 1,
            displayName = Names[1],
            hasContent  = true,
            fares       = new FareTable(),

            manual = new ManualRouteDefinition
            {
                waypoints = new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(0f, 30f),
                    new Vector2(18f, 42f),
                    new Vector2(18f, 72f),
                    new Vector2(0f, 84f),
                    new Vector2(0f, 114f),
                    new Vector2(22f, 128f),
                    new Vector2(22f, 156f),
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
            },

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
