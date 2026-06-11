using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Converts a Manual-mode waypoint route into an Automation tile grid so the
/// code-driven scene mirrors the manual drive. The polyline is scaled down and
/// rasterized 4-connected (orthogonal staircase, never diagonal) so the
/// grid agent — which only moves N/E/S/W — can always follow it. Start sits on
/// the first waypoint, the destination on the destination stop, and a 'P'
/// passenger stop on every other stop. Output feeds <see cref="GridModel.Parse"/>.
/// </summary>
public static class RouteToGrid
{
    public struct Result
    {
        public string[] Map;
        public int      StartFacing;   // 0=N,1=E,2=S,3=W (matches AgentSim)
    }

    /// <param name="targetSpan">Roughly how many cells the longer route axis spans.</param>
    public static Result FromManualRoute(ManualRouteDefinition route, int targetSpan = 16)
    {
        Vector2[] wp = route != null ? route.waypoints : null;
        if (wp == null || wp.Length < 2)
            return new Result { Map = new[] { "S.", ".D" }, StartFacing = 1 };

        // Bounding box of the route.
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;
        foreach (Vector2 p in wp)
        {
            minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
            minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
        }

        const int border = 1;
        float extent = Mathf.Max(maxX - minX, maxY - minY, 0.001f);
        float cell   = extent / Mathf.Max(2, targetSpan);

        // Logical → grid cell. Flip Y so "forward / up" (larger logical y) maps
        // to the TOP of the grid (smaller grid y = North), matching GridModel.
        Vector2Int ToCell(Vector2 p) => new Vector2Int(
            border + Mathf.RoundToInt((p.x - minX) / cell),
            border + Mathf.RoundToInt((maxY - p.y) / cell));

        int width  = border * 2 + Mathf.RoundToInt((maxX - minX) / cell) + 1;
        int height = border * 2 + Mathf.RoundToInt((maxY - minY) / cell) + 1;

        var glyphs = new char[width, height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                glyphs[x, y] = '#';

        // Rasterize the polyline as connected road; remember the order so we can
        // derive the start facing from the first actual step.
        var path = new List<Vector2Int>();
        Vector2Int prev = ToCell(wp[0]);
        Mark(glyphs, prev, path);
        for (int i = 1; i < wp.Length; i++)
        {
            Vector2Int next = ToCell(wp[i]);
            March(glyphs, prev, next, path);
            prev = next;
        }

        // Overlay markers. Destination = the stop flagged isDestination, else
        // the last waypoint.
        Vector2Int startCell = ToCell(wp[0]);
        Vector2Int destCell  = ToCell(wp[wp.Length - 1]);
        if (route.stops != null)
            foreach (ManualStopDefinition s in route.stops)
                if (s.isDestination && s.waypointIndex >= 0 && s.waypointIndex < wp.Length)
                    destCell = ToCell(wp[s.waypointIndex]);

        Set(glyphs, destCell, 'D');                 // dest first so a coincident stop doesn't overwrite it
        if (route.stops != null)
            foreach (ManualStopDefinition s in route.stops)
            {
                if (s.isDestination || s.waypointIndex < 0 || s.waypointIndex >= wp.Length) continue;
                Vector2Int c = ToCell(wp[s.waypointIndex]);
                if (c != startCell && c != destCell) Set(glyphs, c, 'P');
            }
        Set(glyphs, startCell, 'S');                // start last so it always wins its cell

        // Start facing = direction of the first distinct path step.
        int facing = 1;
        foreach (Vector2Int c in path)
        {
            Vector2Int d = c - startCell;
            if (d != Vector2Int.zero) { facing = FacingFromDelta(d); break; }
        }

        var map = new string[height];
        for (int y = 0; y < height; y++)
        {
            var sb = new System.Text.StringBuilder(width);
            for (int x = 0; x < width; x++) sb.Append(glyphs[x, y]);
            map[y] = sb.ToString();
        }

        return new Result { Map = map, StartFacing = facing };
    }

    // -------------------------------------------------------------------------

    static void March(char[,] g, Vector2Int a, Vector2Int b, List<Vector2Int> path)
    {
        Vector2Int cur = a;
        int guard = g.GetLength(0) * g.GetLength(1) + 4;
        while (cur != b && guard-- > 0)
        {
            int dx = b.x - cur.x, dy = b.y - cur.y;
            if (Mathf.Abs(dx) >= Mathf.Abs(dy) && dx != 0) cur.x += (int)Mathf.Sign(dx);
            else if (dy != 0)                              cur.y += (int)Mathf.Sign(dy);
            else                                           cur.x += (int)Mathf.Sign(dx);
            Mark(g, cur, path);
        }
    }

    static void Mark(char[,] g, Vector2Int c, List<Vector2Int> path)
    {
        if (InBounds(g, c))
        {
            if (g[c.x, c.y] == '#') g[c.x, c.y] = '.';
            path.Add(c);
        }
    }

    static void Set(char[,] g, Vector2Int c, char ch)
    {
        if (InBounds(g, c)) g[c.x, c.y] = ch;
    }

    static bool InBounds(char[,] g, Vector2Int c) =>
        c.x >= 0 && c.y >= 0 && c.x < g.GetLength(0) && c.y < g.GetLength(1);

    static int FacingFromDelta(Vector2Int d)
    {
        if (Mathf.Abs(d.x) >= Mathf.Abs(d.y)) return d.x >= 0 ? 1 : 3;  // E / W
        return d.y > 0 ? 2 : 0;                                          // S / N (y down = south)
    }
}
