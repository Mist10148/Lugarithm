using UnityEngine;

/// <summary>
/// Whether a town minigame station is one of the two non-coding warm-up puzzles
/// or the town's single main coding challenge (the one that gates moving on).
/// </summary>
public enum MinigameStationType { Puzzle, Coding }

/// <summary>
/// The placeholder "flavor" of a station, used to label it and pick a distinct
/// map marker. Puzzle kinds map onto the simple non-coding drills we already have
/// art/logic seeds for (maze, flow/color-connect, block-fill, pattern-match);
/// <see cref="Coding"/> is the lesson-tied programming challenge.
/// </summary>
public enum MinigamePuzzleKind { Maze, ColorConnect, BlockFill, PatternMatch, FlowConnect, CrateStack, Coding, CodingMaze }

/// <summary>
/// Placeholder definition for one interactable town minigame station. Carries the
/// display copy shown in the access panel plus a marker colour so the three
/// stations read as visibly distinct objectives on the map. The actual minigame
/// is not wired yet — these drive the placeholder access flow only.
/// </summary>
public class MinigameStationDef
{
    public string id;
    public string title;        // shown as the station name + panel header
    public string description;  // what the puzzle/challenge asks of the player
    public string concept;      // coding concept tie-in (coding) / heritage hook (puzzle)
    public MinigameStationType type;
    public MinigamePuzzleKind  kind;
    public Color markerColor;

    /// <summary>True for the single main quest per level (the CodeOrder coding
    /// challenge). Main quests are visually distinct from side objectives.</summary>
    public bool isMainQuest;

    public bool IsCoding => type == MinigameStationType.Coding;
}

/// <summary>
/// Authored library of the three minigame stations per overworld town: two simple
/// non-coding puzzles and one lesson-tied coding challenge. Mirrors the pattern of
/// <see cref="TownNpcDialogueLibrary"/> / <see cref="OverworldMapLibrary"/> — code
/// defined now, liftable to data later. Coding concepts follow the README town
/// table (sequencing → conditionals → lists → functions+loops → nested
/// conditionals → multi-variable constraints).
///
/// These are PLACEHOLDERS: the copy and marker are real, but the games behind them
/// are stubbed by the access panel until the full minigames are wired in.
/// </summary>
public static class TownMinigameLibrary
{
    // Distinct map-marker palette so all three objectives are tell-apart at a glance
    // (and distinct from NPC red, jeep amber, exit). Two puzzle hues + one coding hue.
    static readonly Color PuzzleTeal   = new Color(0.22f, 0.70f, 0.74f);
    static readonly Color PuzzlePurple = new Color(0.60f, 0.42f, 0.82f);
    static readonly Color CodingGreen  = new Color(0.28f, 0.74f, 0.42f);
    static readonly Color MainQuestGold = new Color(0.96f, 0.72f, 0.15f);

    /// <summary>
    /// Returns the three station defs for a level in canonical order:
    /// [puzzle A, puzzle B, coding]. The level controller binds them to the map's
    /// Q/Q/C entities in row-major order, per station kind.
    /// </summary>
    public static MinigameStationDef[] ForLevel(int levelIndex)
    {
        switch (levelIndex)
        {
            case 1:  return Molo();
            case 2:  return Oton();
            case 3:  return Tigbauan();
            case 4:  return Miagao();
            case 5:  return SanJoaquin();
            default: return Tutorial();
        }
    }

    /// <summary>Looks up a single station def by id within a level (or null).</summary>
    public static MinigameStationDef Get(int levelIndex, string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (var def in ForLevel(levelIndex))
            if (def.id == id) return def;
        return null;
    }

    // -------------------------------------------------------------------------
    // Builders

    static MinigameStationDef Puzzle(string id, string title, MinigamePuzzleKind kind,
                                     Color color, string description, string hook)
        => new MinigameStationDef
        {
            id = id, title = title, description = description, concept = hook,
            type = MinigameStationType.Puzzle, kind = kind, markerColor = color,
        };

    static MinigameStationDef Coding(string id, string title, string concept,
                                     string description)
        => new MinigameStationDef
        {
            id = id, title = title, description = description, concept = concept,
            type = MinigameStationType.Coding, kind = MinigamePuzzleKind.Coding,
            markerColor = MainQuestGold,
            isMainQuest = true,
        };

    static MinigameStationDef CodingMaze(string id, string title, string concept,
                                         string description)
        => new MinigameStationDef
        {
            id = id, title = title, description = description, concept = concept,
            type = MinigameStationType.Coding, kind = MinigamePuzzleKind.CodingMaze,
            markerColor = CodingGreen,
        };

    // -------------------------------------------------------------------------
    // Level 0 — Tutorial (sequencing)

    static MinigameStationDef[] Tutorial() => new[]
    {
        Puzzle("tut_maze", "Garage Maze", MinigamePuzzleKind.Maze, PuzzleTeal,
            "Trace a path through the jeepney garage to reach the parked jeep without hitting a wall.",
            "Warm-up: read a layout step by step."),
        Puzzle("tut_fill", "Capiz Window", MinigamePuzzleKind.BlockFill, PuzzlePurple,
            "Draw one line from the start square to the end square that fills every tile of the capiz-shell window grid.",
            "Plan a route that covers everything once."),
        Puzzle("tut_connect", "Route Links", MinigamePuzzleKind.FlowConnect, PuzzleTeal,
            "Connect each matching stop without crossing routes before the jeepney leaves the garage.",
            "Plan clear paths before a trip begins."),
        Puzzle("tut_crates", "Supply Stack", MinigamePuzzleKind.CrateStack, PuzzlePurple,
            "Sort the garage crates so the heaviest load sits safely at the bottom.",
            "Balance the jeepney before departure."),
        Coding("tut_code", "First Route", "Sequencing",
            "Order the driving steps so the jeepney leaves the garage and reaches the first stop — your first taste of writing a program in order."),
        CodingMaze("tut_coding_maze", "Straight Road", "Sequencing",
            "Write the shortest route to drive straight from the start marker to the destination."),
    };

    // -------------------------------------------------------------------------
    // Level 1 — Iloilo City / Molo (conditionals)

    static MinigameStationDef[] Molo() => new[]
    {
        Puzzle("molo_connect", "Loom Threads", MinigamePuzzleKind.ColorConnect, PuzzleTeal,
            "Connect each pair of matching coloured spools across the grid without crossing the threads.",
            "Molo's textile-trade heritage."),
        Puzzle("molo_maze", "Twin-Spire Path", MinigamePuzzleKind.Maze, PuzzlePurple,
            "Find the way through the plaza around Molo Church's twin spires to the market.",
            "The 'feminist church' of Molo."),
        Puzzle("molo_gate_connect", "Transit Links", MinigamePuzzleKind.FlowConnect, PuzzleTeal,
            "Connect matching route markers across the plaza without crossing lines.",
            "Molo's busy roads and gathering places."),
        Puzzle("molo_gate_crates", "Market Load", MinigamePuzzleKind.CrateStack, PuzzlePurple,
            "Restack the market cargo from lightest on top to heaviest below.",
            "Safe loading before the road south."),
        Coding("molo_code", "If It Rains", "Conditionals",
            "Teach the jeepney to decide: IF a passenger is waiting, pick them up; ELSE drive on. The town's first conditional logic."),
        CodingMaze("molo_coding_maze", "Alley Route", "Conditionals",
            "Use code to drive through a small route maze and reach the destination marker."),
    };

    // -------------------------------------------------------------------------
    // Level 2 — Oton (lists & indexing)

    static MinigameStationDef[] Oton() => new[]
    {
        Puzzle("oton_fill", "Gold Mask Mosaic", MinigamePuzzleKind.BlockFill, PuzzleTeal,
            "Fill the burial-mound grid in a single unbroken line to reassemble the Oton Gold Mask.",
            "Pre-colonial gold-working of Oton."),
        Puzzle("oton_pattern", "River Trade Tally", MinigamePuzzleKind.PatternMatch, PuzzlePurple,
            "Match the sequence of trade goods shown before the Batiano boats cast off.",
            "Batiano River trade routes."),
        Puzzle("oton_gate_connect", "Harbor Routes", MinigamePuzzleKind.FlowConnect, PuzzleTeal,
            "Link each matching harbor marker without crossing trade paths.",
            "Oton's old river and sea routes."),
        Puzzle("oton_gate_crates", "Cargo Stack", MinigamePuzzleKind.CrateStack, PuzzlePurple,
            "Stack the trade crates with the heaviest cargo at the bottom.",
            "Careful loading for river travel."),
        Coding("oton_code", "Passenger Manifest", "Lists & indexing",
            "Use a list of stops and index into it to drop passengers in the right order along the river road."),
        CodingMaze("oton_coding_maze", "Market Maze", "Lists & indexing",
            "Navigate a tighter market route by writing a reusable maze-driving program."),
    };

    // -------------------------------------------------------------------------
    // Level 3 — Tigbauan (functions + loops)

    static MinigameStationDef[] Tigbauan() => new[]
    {
        Puzzle("tig_connect", "Hablon Weave", MinigamePuzzleKind.ColorConnect, PuzzleTeal,
            "Reconnect the cut hablon threads, matching each colour end-to-end across the handloom.",
            "Tigbauan's hablon handloom weaving."),
        Puzzle("tig_maze", "Guerrilla Trail", MinigamePuzzleKind.Maze, PuzzlePurple,
            "Slip through the back-trails past the WWII resistance markers to the meeting point.",
            "WWII guerrilla resistance."),
        Puzzle("tig_gate_connect", "Thread Links", MinigamePuzzleKind.FlowConnect, PuzzleTeal,
            "Reconnect matching thread ends without crossing the weave.",
            "Hablon patterns as routes."),
        Puzzle("tig_gate_crates", "Loom Supplies", MinigamePuzzleKind.CrateStack, PuzzlePurple,
            "Order the loom supply crates so the weight is stable.",
            "Prepare the tools before the pattern repeats."),
        Coding("tig_code", "Weave Routine", "Functions + loops",
            "Write a reusable function and loop it to repeat the weave pattern — and to service a run of identical stops."),
        CodingMaze("tig_coding_maze", "Thread Maze", "Functions + loops",
            "Use loops and helper logic to trace a path through the woven route."),
    };

    // -------------------------------------------------------------------------
    // Level 4 — Miag-ao (nested conditionals)

    static MinigameStationDef[] Miagao() => new[]
    {
        Puzzle("miag_fill", "Facade Restoration", MinigamePuzzleKind.BlockFill, PuzzleTeal,
            "Fill the church-facade grid in one stroke to restore the carved coconut tree of life.",
            "Miag-ao Church (UNESCO, 1797)."),
        Puzzle("miag_connect", "Fort-Church Watch", MinigamePuzzleKind.ColorConnect, PuzzlePurple,
            "Link each watch-post to its signal fire around the fort-church without crossing lines.",
            "Its fort-church origin."),
        Puzzle("miag_gate_connect", "Watch Routes", MinigamePuzzleKind.FlowConnect, PuzzleTeal,
            "Connect each paired watch route without letting paths cross.",
            "Signals around the fort-church."),
        Puzzle("miag_gate_crates", "Stonework Stack", MinigamePuzzleKind.CrateStack, PuzzlePurple,
            "Stack restoration materials from lightest to heaviest.",
            "Careful preparation before repair."),
        Coding("miag_code", "Watchman's Rules", "Nested conditionals",
            "Nest conditions: IF it's a stop, THEN if a passenger wants off, drop them; else keep their seat. Decisions inside decisions."),
        CodingMaze("miag_coding_maze", "Fort Route", "Nested conditionals",
            "Navigate a harder route where checking side paths and blocked roads matters."),
    };

    // -------------------------------------------------------------------------
    // Level 5 — San Joaquin (multi-variable constraints)

    static MinigameStationDef[] SanJoaquin() => new[]
    {
        Puzzle("sj_maze", "Campo Santo Paths", MinigamePuzzleKind.Maze, PuzzleTeal,
            "Wind through the baroque cemetery's tiered paths to the chapel at the top.",
            "Campo Santo baroque cemetery."),
        Puzzle("sj_pattern", "Battle Relief", MinigamePuzzleKind.PatternMatch, PuzzlePurple,
            "Reorder the carved scenes to match the Rendicion de Tetuan bas-relief.",
            "The Rendicion de Tetuan facade."),
        Puzzle("sj_gate_connect", "Final Road Links", MinigamePuzzleKind.FlowConnect, PuzzleTeal,
            "Connect each final route marker without crossing dangerous paths.",
            "Safe routing at the end of the coast."),
        Puzzle("sj_gate_crates", "Pilgrim Supplies", MinigamePuzzleKind.CrateStack, PuzzlePurple,
            "Stack the supplies safely before the last stretch.",
            "A balanced load for the final road."),
        Coding("sj_code", "Full Service Run", "Multi-variable constraints",
            "Balance seats, fares, and fuel at once to complete a full route — many variables, all constrained together."),
        CodingMaze("sj_coding_maze", "Final Maze", "Multi-variable constraints",
            "Solve the hardest town maze before the last road south."),
    };
}
