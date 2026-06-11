using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Parsed automation-mode map. Row 0 of the source strings is the TOP of the
/// map, so North (facing 0) decreases y. Legend: '#' wall · '.' road ·
/// 'S' start · 'D' destination · 'P' passenger stop.
/// Pure data — no scene objects; <see cref="GridWorldView"/> renders it.
/// </summary>
public class GridModel
{
    public enum Cell { Wall, Road, Start, Destination, Stop }

    public int Width  { get; private set; }
    public int Height { get; private set; }

    public Vector2Int StartPos { get; private set; }
    public Vector2Int DestPos  { get; private set; }

    /// <summary>Cells that begin the puzzle with a waiting passenger.</summary>
    public readonly List<Vector2Int> StopCells = new List<Vector2Int>();

    Cell[,] _cells;

    // -------------------------------------------------------------------------

    /// <summary>
    /// Parses map strings. Never throws — authoring problems land in
    /// <paramref name="errors"/> (and are also caught by EditMode tests).
    /// </summary>
    public static GridModel Parse(string[] map, out List<string> errors)
    {
        errors = new List<string>();
        var model = new GridModel();

        if (map == null || map.Length == 0)
        {
            errors.Add("map is empty.");
            model.Width  = 1;
            model.Height = 1;
            model._cells = new Cell[1, 1];
            return model;
        }

        model.Height = map.Length;
        model.Width  = 0;
        foreach (string row in map)
            model.Width = Mathf.Max(model.Width, row != null ? row.Length : 0);

        model._cells = new Cell[model.Width, model.Height];

        int startCount = 0, destCount = 0;

        for (int y = 0; y < model.Height; y++)
        {
            string row = map[y] ?? "";
            for (int x = 0; x < model.Width; x++)
            {
                char c = x < row.Length ? row[x] : '#'; // pad ragged rows with wall

                switch (c)
                {
                    case '#': model._cells[x, y] = Cell.Wall; break;
                    case '.': model._cells[x, y] = Cell.Road; break;
                    case 'S':
                        model._cells[x, y] = Cell.Start;
                        model.StartPos = new Vector2Int(x, y);
                        startCount++;
                        break;
                    case 'D':
                        model._cells[x, y] = Cell.Destination;
                        model.DestPos = new Vector2Int(x, y);
                        destCount++;
                        break;
                    case 'P':
                        model._cells[x, y] = Cell.Stop;
                        model.StopCells.Add(new Vector2Int(x, y));
                        break;
                    default:
                        model._cells[x, y] = Cell.Wall;
                        errors.Add($"unknown map character '{c}' at ({x},{y}) — treated as a wall.");
                        break;
                }
            }
        }

        if (startCount != 1) errors.Add($"map needs exactly one start 'S' (found {startCount}).");
        if (destCount  != 1) errors.Add($"map needs exactly one destination 'D' (found {destCount}).");

        return model;
    }

    // -------------------------------------------------------------------------

    /// <summary>Cell at (x,y); anything out of bounds reads as wall.</summary>
    public Cell Get(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) return Cell.Wall;
        return _cells[x, y];
    }

    public Cell Get(Vector2Int p) => Get(p.x, p.y);

    public bool IsWalkable(Vector2Int p) => Get(p) != Cell.Wall;
}
