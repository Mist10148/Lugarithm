using System.Collections.Generic;
using UnityEngine;

/// <summary>One colour's two hub cells. Colour id is the pair's index.</summary>
public struct FlowPair
{
    public Vector2Int A;
    public Vector2Int B;
    public FlowPair(Vector2Int a, Vector2Int b) { A = a; B = b; }
}

/// <summary>
/// Pure model for the "Non-Intersecting Connections" town puzzle: a grid of
/// colour-paired hub cells. The player draws a path per colour; paths may not
/// cross, overlap, or pass through another colour's hub. Win = every pair joined
/// by a valid, non-overlapping path. No Unity scene dependencies — EditMode
/// tests drive it directly. Colour id == pair index.
/// </summary>
public class FlowConnectBoard
{
    public int Width  { get; }
    public int Height { get; }
    public int ColorCount { get; }

    readonly int[,] _endpoint;                 // colour at a hub cell, else -1
    readonly List<List<Vector2Int>> _paths;    // index = colour; includes the start hub

    // -------------------------------------------------------------------------

    public FlowConnectBoard(int width, int height, IList<FlowPair> pairs)
    {
        Width  = width;
        Height = height;
        ColorCount = pairs.Count;

        _endpoint = new int[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                _endpoint[x, y] = -1;

        _paths = new List<List<Vector2Int>>(pairs.Count);
        for (int c = 0; c < pairs.Count; c++)
        {
            _endpoint[pairs[c].A.x, pairs[c].A.y] = c;
            _endpoint[pairs[c].B.x, pairs[c].B.y] = c;
            _paths.Add(new List<Vector2Int>());
        }
    }

    // -------------------------------------------------------------------------

    public bool InBounds(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < Width && c.y < Height;

    /// <summary>Colour whose hub sits on this cell, or -1.</summary>
    public int EndpointColor(Vector2Int c) => InBounds(c) ? _endpoint[c.x, c.y] : -1;

    /// <summary>Colour currently occupying a cell (hub or drawn path), or -1.</summary>
    public int Owner(Vector2Int c)
    {
        int ep = EndpointColor(c);
        if (ep >= 0) return ep;
        for (int color = 0; color < ColorCount; color++)
            if (_paths[color].Contains(c)) return color;
        return -1;
    }

    public bool HasStarted(int color) => _paths[color].Count > 0;
    public IReadOnlyList<Vector2Int> Path(int color) => _paths[color];

    // -------------------------------------------------------------------------

    /// <summary>(Re)starts a colour's path at one of its hubs.</summary>
    public bool Start(int color, Vector2Int hub)
    {
        if (color < 0 || color >= ColorCount) return false;
        if (EndpointColor(hub) != color) return false;
        _paths[color].Clear();
        _paths[color].Add(hub);
        return true;
    }

    /// <summary>
    /// Extends the active path to an orthogonally-adjacent cell. Stepping back
    /// onto a previous cell truncates the path. Returns true when the path
    /// changed. Entering another colour's cell/hub is rejected (no crossings).
    /// </summary>
    public bool Extend(int color, Vector2Int next)
    {
        if (color < 0 || color >= ColorCount) return false;
        List<Vector2Int> path = _paths[color];
        if (path.Count == 0) return false;

        Vector2Int head = path[path.Count - 1];
        if (!InBounds(next) || Manhattan(head, next) != 1) return false;

        // Backtrack one cell.
        if (path.Count >= 2 && next == path[path.Count - 2])
        {
            path.RemoveAt(path.Count - 1);
            return true;
        }

        // Loop back onto an earlier cell of our own path → truncate to it.
        int existing = path.IndexOf(next);
        if (existing >= 0)
        {
            path.RemoveRange(existing + 1, path.Count - existing - 1);
            return true;
        }

        // The target must be empty or this colour's matching hub.
        int ep = EndpointColor(next);
        if (ep >= 0 && ep != color) return false;          // another colour's hub
        for (int other = 0; other < ColorCount; other++)
            if (other != color && _paths[other].Contains(next)) return false; // another path

        path.Add(next);
        return true;
    }

    public void Clear(int color)
    {
        if (color >= 0 && color < ColorCount) _paths[color].Clear();
    }

    public void ClearAll()
    {
        for (int c = 0; c < ColorCount; c++) _paths[c].Clear();
    }

    // -------------------------------------------------------------------------

    /// <summary>True when a colour's path joins both its hubs contiguously.</summary>
    public bool IsComplete(int color)
    {
        List<Vector2Int> path = _paths[color];
        if (path.Count < 2) return false;

        Vector2Int a = path[0];
        Vector2Int b = path[path.Count - 1];
        if (EndpointColor(a) != color || EndpointColor(b) != color || a == b) return false;

        var seen = new HashSet<Vector2Int>();
        for (int i = 0; i < path.Count; i++)
        {
            if (!seen.Add(path[i])) return false;            // no repeats
            if (i > 0 && Manhattan(path[i - 1], path[i]) != 1) return false;
        }
        return true;
    }

    /// <summary>True when every pair is joined and no two paths share a cell.</summary>
    public bool IsSolved()
    {
        var used = new HashSet<Vector2Int>();
        for (int color = 0; color < ColorCount; color++)
        {
            if (!IsComplete(color)) return false;
            foreach (Vector2Int cell in _paths[color])
                if (!used.Add(cell)) return false;           // overlap across colours
        }
        return true;
    }

    static int Manhattan(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
}
