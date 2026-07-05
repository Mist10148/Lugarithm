using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Map-agnostic breadth-first pathfinding over a <see cref="GridModel"/>: it only
/// asks <see cref="GridModel.IsWalkable"/>, so it works on any generated grid.
/// The grids are small and unweighted, so BFS yields a shortest path. Used both
/// to prove generated layouts are solvable and to drive the self-driving agent.
/// </summary>
public static class GridPathfinder
{
    // NAVIGATION: grid-cell BFS, deterministic — not free-roam. The agent moves
    // one cell at a time over a rasterized grid; there is no node-graph following
    // or random branching here.

    /// <summary>
    /// Shortest 4-connected path of cells from <paramref name="from"/> to
    /// <paramref name="to"/> inclusive, or null when unreachable. The start cell
    /// is included; a from==to query returns a single-cell path.
    /// </summary>
    public static List<Vector2Int> Path(GridModel grid, Vector2Int from, Vector2Int to)
        => Path(grid, from, to, null);

    public static List<Vector2Int> Path(GridModel grid, Vector2Int from, Vector2Int to,
                                        IReadOnlyCollection<Vector2Int> blocked)
    {
        if (grid == null || !grid.IsWalkable(from) || !grid.IsWalkable(to)) return null;
        if (from == to) return new List<Vector2Int> { from };

        var came  = new Dictionary<Vector2Int, Vector2Int>();
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(from);
        came[from] = from;

        while (queue.Count > 0)
        {
            Vector2Int cur = queue.Dequeue();
            if (cur == to) return Reconstruct(came, from, to);

            foreach (Vector2Int d in AgentSim.FacingDeltas)
            {
                Vector2Int next = cur + d;
                if (came.ContainsKey(next) || !grid.IsWalkable(next)) continue;
                if (next != to && IsBlocked(blocked, next)) continue;
                came[next] = cur;
                queue.Enqueue(next);
            }
        }

        return null;
    }

    /// <summary>True when a 4-connected walkable path exists between the cells.</summary>
    public static bool Reachable(GridModel grid, Vector2Int from, Vector2Int to) =>
        Path(grid, from, to) != null;

    /// <summary>
    /// Turns a cell path into the ordered agent actions that walk it from
    /// <paramref name="startFacing"/> — turnLeft/turnRight to face each step, then
    /// moveForward. Empty when the path is null or a single cell.
    /// </summary>
    public static List<string> ToActions(List<Vector2Int> path, int startFacing)
    {
        var actions = new List<string>();
        if (path == null || path.Count < 2) return actions;

        int facing = ((startFacing % 4) + 4) % 4;
        for (int i = 1; i < path.Count; i++)
        {
            int want = FacingFromDelta(path[i] - path[i - 1]);
            if (want < 0) continue;                 // non-adjacent step — shouldn't happen

            // Rotate the short way toward the desired facing.
            int diff = ((want - facing) % 4 + 4) % 4;
            if (diff == 1) { actions.Add("turnRight"); }
            else if (diff == 3) { actions.Add("turnLeft"); }
            else if (diff == 2) { actions.Add("turnRight"); actions.Add("turnRight"); }
            facing = want;

            actions.Add("moveForward");
        }
        return actions;
    }

    // -------------------------------------------------------------------------

    static List<Vector2Int> Reconstruct(Dictionary<Vector2Int, Vector2Int> came,
                                        Vector2Int from, Vector2Int to)
    {
        var path = new List<Vector2Int>();
        Vector2Int cur = to;
        while (cur != from)
        {
            path.Add(cur);
            cur = came[cur];
        }
        path.Add(from);
        path.Reverse();
        return path;
    }

    static bool IsBlocked(IReadOnlyCollection<Vector2Int> blocked, Vector2Int cell)
    {
        if (blocked == null) return false;
        foreach (Vector2Int item in blocked)
            if (item == cell) return true;
        return false;
    }

    static int FacingFromDelta(Vector2Int d)
    {
        for (int f = 0; f < AgentSim.FacingDeltas.Length; f++)
            if (AgentSim.FacingDeltas[f] == d) return f;
        return -1;
    }
}
