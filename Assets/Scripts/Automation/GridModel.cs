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

    // -------------------------------------------------------------------------
    // Streaming append

    /// <summary>
    /// Extends the grid by appending rows and/or columns. Existing cell positions
    /// keep their current values; the agent and any placed objects are not moved.
    /// New rows/cols are filled with wall, then the given map rows are stamped on
    /// top starting at <paramref name="rowOffset"/>, <paramref name="colOffset"/>.
    /// </summary>
    public void AppendChunk(int newRows, int newCols, string[] mapRows,
                            int rowOffset, int colOffset)
    {
        int newWidth  = Mathf.Max(Width,  colOffset + (mapRows.Length > 0 ? mapRows[0].Length : 0));
        int newHeight = Mathf.Max(Height, rowOffset + mapRows.Length);
        if (newRows > 0) newHeight = Mathf.Max(newHeight, Height + newRows);
        if (newCols > 0) newWidth  = Mathf.Max(newWidth,  Width  + newCols);

        var next = new Cell[newWidth, newHeight];
        for (int x = 0; x < newWidth; x++)
            for (int y = 0; y < newHeight; y++)
                next[x, y] = Cell.Wall;

        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                next[x, y] = _cells[x, y];

        for (int y = 0; y < mapRows.Length; y++)
        {
            string row = mapRows[y] ?? "";
            int gy = rowOffset + y;
            for (int x = 0; x < row.Length; x++)
            {
                int gx = colOffset + x;
                char c = row[x];
                switch (c)
                {
                    case '#': next[gx, gy] = Cell.Wall; break;
                    case '.': next[gx, gy] = Cell.Road; break;
                    case 'S': next[gx, gy] = Cell.Start; break;
                    case 'D': next[gx, gy] = Cell.Destination; break;
                    case 'P':
                        next[gx, gy] = Cell.Stop;
                        StopCells.Add(new Vector2Int(gx, gy));
                        break;
                }
            }
        }

        _cells = next;
        Width  = newWidth;
        Height = newHeight;
    }
}
