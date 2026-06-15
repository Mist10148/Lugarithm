using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Projects a generated <see cref="TownLayout"/> into an Automation-mode grid:
/// every road edge is rasterized 4-connected (orthogonal staircase, never
/// diagonal, like <see cref="RouteToGrid"/>) so the N/E/S/W grid agent can always
/// follow it. Terminals become 'S'/'D', stop nodes become 'P', junctions become
/// plain road. Each node's <see cref="TownNode.gridCell"/> is filled in so the
/// shared passenger requests can be mapped onto the grid. Output feeds
/// <see cref="GridModel.Parse"/>.
/// </summary>
public static class GridLayoutProjector
{
    /// <summary>
    /// Rasterizes the layout at <paramref name="cellSize"/> world units per cell.
    /// Mutates every node's <see cref="TownNode.gridCell"/>.
    /// <paramref name="facing"/> is the start facing (0=N,1=E,2=S,3=W).
    /// </summary>
    public static string[] ToGridMap(TownLayout layout, float cellSize,
                                     out int facing, out List<string> errors)
    {
        return ToGridMap(layout, cellSize, out _, out facing, out errors);
    }

    /// <summary>
    /// Rasterizes the layout and returns the <see cref="GridTransform"/> so
    /// callers can convert grid cells back to world coordinates exactly.
    /// </summary>
    public static string[] ToGridMap(TownLayout layout, float cellSize,
                                     out GridTransform transform, out int facing,
                                     out List<string> errors)
    {
        errors = new List<string>();
        facing = 1;
        transform = new GridTransform();

        if (layout == null || layout.nodes.Count == 0)
        {
            errors.Add("empty layout.");
            return new[] { "S.", ".D" };
        }

        // Bounding box of all node positions.
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;
        foreach (TownNode n in layout.nodes)
        {
            minX = Mathf.Min(minX, n.pos.x); maxX = Mathf.Max(maxX, n.pos.x);
            minY = Mathf.Min(minY, n.pos.y); maxY = Mathf.Max(maxY, n.pos.y);
        }

        const int border = 1;
        float extentX = Mathf.Max(maxX - minX, 0.001f);
        float extentY = Mathf.Max(maxY - minY, 0.001f);
        float cell    = Mathf.Max(0.5f, cellSize);

        // Logical → grid. Flip Y so larger world-y (forward / north) maps to the
        // TOP of the grid (smaller grid y), matching GridModel and RouteToGrid.
        Vector2Int ToCell(Vector2 p) => new Vector2Int(
            border + Mathf.RoundToInt((p.x - minX) / cell),
            border + Mathf.RoundToInt((maxY - p.y) / cell));

        transform = new GridTransform(minX, maxY, cell, border);

        int width  = border * 2 + Mathf.RoundToInt(extentX / cell) + 1;
        int height = border * 2 + Mathf.RoundToInt(extentY / cell) + 1;

        var glyphs = new char[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                glyphs[x, y] = '#';

        // Record each node's cell.
        foreach (TownNode n in layout.nodes)
            n.gridCell = ToCell(n.pos);

        // Rasterize every edge as connected road.
        foreach (TownEdge e in layout.edges)
            March(glyphs, layout.Node(e.a).gridCell, layout.Node(e.b).gridCell);

        // Overlay markers. Stops first, then terminals last so they always win
        // their own cell even if a stop rounded onto them.
        foreach (TownNode n in layout.nodes)
            if (n.kind == NodeKind.Stop || n.kind == NodeKind.HeritageSite || n.kind == NodeKind.NpcDrop)
                Set(glyphs, n.gridCell, 'P');

        Vector2Int startCell = layout.Node(layout.startNodeId).gridCell;
        Vector2Int destCell  = layout.Node(layout.destNodeId).gridCell;
        Set(glyphs, destCell,  'D');
        Set(glyphs, startCell, 'S');

        // Start facing = direction from start toward its first trunk neighbour.
        if (layout.trunkNodeIds.Count >= 2 && layout.trunkNodeIds[0] == layout.startNodeId)
        {
            Vector2Int next = layout.Node(layout.trunkNodeIds[1]).gridCell;
            int f = FacingFromDelta(next - startCell);
            if (f >= 0) facing = f;
        }

        var map = new string[height];
        for (int y = 0; y < height; y++)
        {
            var sb = new System.Text.StringBuilder(width);
            for (int x = 0; x < width; x++) sb.Append(glyphs[x, y]);
            map[y] = sb.ToString();
        }
        return map;
    }

    // -------------------------------------------------------------------------

    static void March(char[,] g, Vector2Int a, Vector2Int b)
    {
        Vector2Int cur = a;
        Mark(g, cur);
        int guard = g.GetLength(0) * g.GetLength(1) + 4;
        while (cur != b && guard-- > 0)
        {
            int dx = b.x - cur.x, dy = b.y - cur.y;
            if (Mathf.Abs(dx) >= Mathf.Abs(dy) && dx != 0) cur.x += (int)Mathf.Sign(dx);
            else if (dy != 0)                              cur.y += (int)Mathf.Sign(dy);
            else                                           cur.x += (int)Mathf.Sign(dx);
            Mark(g, cur);
        }
    }

    static void Mark(char[,] g, Vector2Int c)
    {
        if (InBounds(g, c) && g[c.x, c.y] == '#') g[c.x, c.y] = '.';
    }

    static void Set(char[,] g, Vector2Int c, char ch)
    {
        if (InBounds(g, c)) g[c.x, c.y] = ch;
    }

    static bool InBounds(char[,] g, Vector2Int c) =>
        c.x >= 0 && c.y >= 0 && c.x < g.GetLength(0) && c.y < g.GetLength(1);

    static int FacingFromDelta(Vector2Int d)
    {
        if (d == Vector2Int.zero) return -1;
        if (Mathf.Abs(d.x) >= Mathf.Abs(d.y)) return d.x >= 0 ? 1 : 3; // E / W
        return d.y > 0 ? 2 : 0;                                        // S / N (grid y down = south)
    }
}
