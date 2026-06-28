using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A small maze for the grid-puzzle minigame: a w×h field of walls with a start
/// and a goal. Authored as char rows ('#' wall, '.' open, 'S' start, 'G' goal) so
/// layouts stay readable and are eyeball-verifiable as solvable.
/// </summary>
public class MazeLayout
{
    public int width;
    public int height;
    public bool[,] wall;          // [y, x]
    public Vector2Int start;
    public Vector2Int goal;

    public static MazeLayout Parse(string[] rows)
    {
        int h = rows.Length;
        int w = 0;
        for (int i = 0; i < h; i++) if (rows[i].Length > w) w = rows[i].Length;

        var m = new MazeLayout { width = w, height = h, wall = new bool[h, w] };
        for (int y = 0; y < h; y++)
        {
            string row = rows[y];
            for (int x = 0; x < w; x++)
            {
                char c = x < row.Length ? row[x] : '#';
                m.wall[y, x] = c == '#';
                if (c == 'S') m.start = new Vector2Int(x, y);
                if (c == 'G') m.goal  = new Vector2Int(x, y);
            }
        }
        return m;
    }
}

/// <summary>A code-ordering puzzle: the program lines in their CORRECT order, plus
/// a one-line goal. The minigame shuffles them and asks the player to reorder.</summary>
public class CodingPuzzle
{
    public string goal;
    public string[] orderedLines;   // correct order
}

/// <summary>
/// Authored content for the lightweight overworld minigames. Mazes are picked by a
/// stable hash of the station id (so a station is always the same maze); coding
/// puzzles are keyed by station id with a concept-based fallback. All content is
/// designed to be solvable as authored.
/// </summary>
public static class OverworldPuzzleLibrary
{
    // -------------------------------------------------------------------------
    // Mazes — serpentine corridors, guaranteed solvable by construction.

    static readonly string[][] Mazes =
    {
        new[]
        {
            "S.....",
            "#####.",
            "......",
            ".#####",
            "......",
            "#####G",
        },
        new[]
        {
            "S.....",
            ".#####",
            "......",
            "#####.",
            "......",
            "G#####",
        },
        new[]
        {
            "S#...#",
            ".#.#.#",
            ".#.#.#",
            ".#.#.#",
            ".#.#.#",
            "...#G#",
        },
    };

    public static MazeLayout GetMaze(string stationId)
    {
        int idx = StableIndex(stationId, Mazes.Length);
        return MazeLayout.Parse(Mazes[idx]);
    }

    // -------------------------------------------------------------------------
    // Block-fill — a full rectangle (all tiles fillable). A Hamiltonian path
    // always exists on a rectangle (snake row by row), so any rectangle is safe.

    /// <summary>Returns the fill-grid size for a station (w, h). Small + always solvable.</summary>
    public static Vector2Int GetFillSize(string stationId)
    {
        // A couple of shapes, chosen stably; both are full rectangles.
        return StableIndex(stationId, 2) == 0 ? new Vector2Int(5, 5) : new Vector2Int(6, 4);
    }

    // -------------------------------------------------------------------------
    // Coding — line-ordering puzzles, concept-tied. Keyed by station id with a
    // per-concept fallback so every coding station has content.

    public static CodingPuzzle GetCoding(string stationId, string concept)
    {
        if (CodingById.TryGetValue(stationId, out var p)) return p;
        return ForConcept(concept);
    }

    static readonly Dictionary<string, CodingPuzzle> CodingById = new Dictionary<string, CodingPuzzle>
    {
        ["tut_code"] = new CodingPuzzle
        {
            goal = "Leave the garage and reach the first stop, in order.",
            orderedLines = new[] { "startEngine()", "releaseBrake()", "driveToNextStop()", "openDoor()" },
        },
        ["molo_code"] = new CodingPuzzle
        {
            goal = "Pick up a waiting passenger, otherwise drive on.",
            orderedLines = new[] { "if passengerWaiting():", "    pickUp()", "else:", "    driveOn()" },
        },
        ["oton_code"] = new CodingPuzzle
        {
            goal = "Visit each stop in the list and drop off there.",
            orderedLines = new[] { "stops = [ 'Molo', 'Oton', 'Tigbauan' ]", "for s in stops:", "    driveTo(s)", "    dropOff()" },
        },
        ["tig_code"] = new CodingPuzzle
        {
            goal = "Define the weave step, then repeat it.",
            orderedLines = new[] { "def weave():", "    overThread()", "    underThread()", "repeat 3: weave()" },
        },
        ["miag_code"] = new CodingPuzzle
        {
            goal = "At a stop, drop those who want off; else keep their seat.",
            orderedLines = new[] { "if atStop():", "    if wantsOff():", "        dropOff()", "    else:", "        keepSeat()" },
        },
        ["sj_code"] = new CodingPuzzle
        {
            goal = "Run the full route, boarding only when seats allow.",
            orderedLines = new[] { "while not routeComplete():", "    if seatsLeft() and passengerWaiting():", "        pickUp()", "    collectFare()", "    driveToNextStop()" },
        },
    };

    static CodingPuzzle ForConcept(string concept)
    {
        // Generic fallback so any future coding station still has a playable puzzle.
        return new CodingPuzzle
        {
            goal = string.IsNullOrEmpty(concept) ? "Order the program." : $"Order the program ({concept}).",
            orderedLines = new[] { "startEngine()", "driveToNextStop()", "dropOff()", "openDoor()" },
        };
    }

    // -------------------------------------------------------------------------

    /// <summary>Deterministic non-negative index in [0,count) from a string id.</summary>
    static int StableIndex(string id, int count)
    {
        if (string.IsNullOrEmpty(id) || count <= 1) return 0;
        unchecked
        {
            int h = 17;
            foreach (char c in id) h = h * 31 + c;
            return (h & 0x7fffffff) % count;
        }
    }
}
