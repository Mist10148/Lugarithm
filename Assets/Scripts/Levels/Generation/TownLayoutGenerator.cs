using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds a per-run <see cref="TownLayout"/> from a level's
/// <see cref="ProceduralLayoutDefinition"/>: the authored trunk + anchors stay
/// fixed (story coherence) and the seeded RNG hangs branch side-streets and
/// commits ordinary-passenger rides around them. Every layout is validated to be
/// solvable in BOTH the graph and the rasterized grid before it is returned;
/// a layout that fails bumps the seed and regenerates (bounded retries), so a
/// sensible, solvable town always comes back. Deterministic for a given seed.
/// </summary>
public static class TownLayoutGenerator
{
    const int   MaxRetries     = 32;
    const float AnchorMatchEps = 0.75f;

    public static TownLayout Generate(ProceduralLayoutDefinition def, FareTable fares, int seed)
    {
        fares = fares ?? new FareTable();

        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            TownLayout layout = Build(def, fares, seed + attempt);
            if (IsSolvable(layout, def.gen.gridCellSize))
                return layout;
        }

        // Exhausted retries — fall back to the trunk-only town (no branches),
        // which is solvable by construction. Authoring errors surface in tests.
        TownLayout fallback = Build(def, fares, seed, trunkOnly: true);
        return fallback;
    }

    // -------------------------------------------------------------------------

    static TownLayout Build(ProceduralLayoutDefinition def, FareTable fares, int seed,
                            bool trunkOnly = false)
    {
        var rng    = new System.Random(seed);
        var layout = new TownLayout { seed = seed };

        Vector2[] trunk = def.trunk;
        TownGenParams gen = def.gen ?? new TownGenParams();

        // 1. Trunk nodes (anchors annotate matching vertices; the rest are bends).
        int stopNameCursor = 1;
        for (int i = 0; i < trunk.Length; i++)
        {
            AnchorNode anchor = MatchAnchor(def.anchors, trunk[i]);
            var node = new TownNode { id = layout.nodes.Count, pos = trunk[i] };

            if (anchor != null)
            {
                node.kind = ToNodeKind(anchor.kind);
                node.name = anchor.name;
            }
            else if (i == 0 || i == trunk.Length - 1)
            {
                node.kind = i == 0 ? NodeKind.TerminalStart : NodeKind.TerminalEnd;
                node.name = i == 0 ? "Terminal" : "Destination";
            }
            else
            {
                // A non-anchor interior bend becomes an ordinary procedural stop.
                node.kind = NodeKind.Stop;
                node.name = $"Sitio {stopNameCursor++}";
            }

            layout.nodes.Add(node);
            layout.trunkNodeIds.Add(node.id);
        }

        // 2. Trunk edges + arc-length along the spine.
        float along = 0f;
        layout.nodes[layout.trunkNodeIds[0]].alongTrunk = 0f;
        for (int i = 1; i < layout.trunkNodeIds.Count; i++)
        {
            int a = layout.trunkNodeIds[i - 1];
            int b = layout.trunkNodeIds[i];
            layout.edges.Add(new TownEdge(a, b, isTrunk: true));
            along += Vector2.Distance(layout.Node(a).pos, layout.Node(b).pos);
            layout.Node(b).alongTrunk = along;
        }

        // Terminals.
        layout.startNodeId = FindKind(layout, NodeKind.TerminalStart, layout.trunkNodeIds[0]);
        layout.destNodeId  = FindKind(layout, NodeKind.TerminalEnd, layout.trunkNodeIds[layout.trunkNodeIds.Count - 1]);

        // 3. Branch side-streets (skipped for the trunk-only fallback).
        if (!trunkOnly)
            GrowBranches(layout, gen, rng, ref stopNameCursor);

        // 4. Commit ordinary-passenger rides.
        BuildRequests(layout, gen, fares, rng);

        return layout;
    }

    // -------------------------------------------------------------------------
    // Branches

    static void GrowBranches(TownLayout layout, TownGenParams gen, System.Random rng,
                             ref int stopNameCursor)
    {
        int want = RandRange(rng, gen.branchCountMin, gen.branchCountMax);
        if (want <= 0) return;

        float spanEnd = layout.Node(layout.trunkNodeIds[layout.trunkNodeIds.Count - 1]).alongTrunk;
        float minClear = Mathf.Max(gen.gridCellSize * 1.5f, 3f);

        // Candidate roots: interior trunk nodes (not the terminals).
        var roots = new List<int>();
        for (int i = 1; i < layout.trunkNodeIds.Count - 1; i++)
            roots.Add(layout.trunkNodeIds[i]);
        Shuffle(roots, rng);

        var usedAlong = new List<float>();
        int made = 0;

        foreach (int rootId in roots)
        {
            if (made >= want) break;

            TownNode root = layout.Node(rootId);

            // Keep branch roots clear of the terminals and of each other.
            if (root.alongTrunk < gen.branchSpacing) continue;
            if (spanEnd - root.alongTrunk < gen.branchSpacing) continue;
            if (TooClose(usedAlong, root.alongTrunk, gen.branchSpacing)) continue;

            Vector2 dir  = TrunkDirAt(layout, rootId);
            Vector2 perp = new Vector2(-dir.y, dir.x) * (rng.Next(2) == 0 ? 1f : -1f);
            float   len  = (float)(gen.branchLenMin + rng.NextDouble() * (gen.branchLenMax - gen.branchLenMin));
            float   jit  = (float)(rng.NextDouble() * 2f - 1f) * (len * 0.15f);
            Vector2 tip  = root.pos + perp * len + dir * jit;

            if (!FarFromAll(layout, tip, minClear)) continue;

            var stop = new TownNode
            {
                id         = layout.nodes.Count,
                pos        = tip,
                kind       = NodeKind.Stop,
                name       = $"Sitio {stopNameCursor++}",
                alongTrunk = root.alongTrunk,   // ordered with its root along the route
            };
            layout.nodes.Add(stop);
            layout.edges.Add(new TownEdge(rootId, stop.id, isTrunk: false));

            usedAlong.Add(root.alongTrunk);
            made++;
        }
    }

    // -------------------------------------------------------------------------
    // Passengers

    static void BuildRequests(TownLayout layout, TownGenParams gen, FareTable fares,
                              System.Random rng)
    {
        // Boardable/alightable stops, ordered earlier → later along the route.
        var stops = new List<TownNode>();
        foreach (TownNode n in layout.nodes)
            if (n.IsStop) stops.Add(n);
        stops.Sort((p, q) => p.alongTrunk.CompareTo(q.alongTrunk));

        if (stops.Count < 2) return;

        int target = Mathf.RoundToInt(gen.passengerDensity * stops.Count);
        int count  = Mathf.Clamp(target, gen.passengerCountMin, gen.passengerCountMax);
        count = Mathf.Min(count, stops.Count * 2);

        for (int i = 0; i < count; i++)
        {
            int origin = rng.Next(stops.Count - 1);          // must have a later stop
            int dest   = PickDest(origin, stops.Count, rng);
            int travelled = Mathf.Max(1, dest - origin);

            int fare = FareMath.ComputeFare(travelled, fares);

            layout.requests.Add(new PassengerRequest
            {
                id            = i,
                color         = RiderColor(i),
                originNodeId  = stops[origin].id,
                destNodeId    = stops[dest].id,
                stopsTraveled = travelled,
                fare          = fare,
                tender        = FareMath.GenerateTender(fare, rng),
            });
        }
    }

    /// <summary>Later stops only; the final terminal is the most likely ask (mirrors Manual).</summary>
    static int PickDest(int origin, int stopCount, System.Random rng)
    {
        int laterCount = stopCount - 1 - origin;
        if (laterCount <= 1) return stopCount - 1;
        if (rng.NextDouble() < 0.5d) return stopCount - 1;     // the terminal
        return origin + 1 + rng.Next(laterCount - 1);          // a non-final later stop
    }

    static Color RiderColor(int i)
    {
        // Golden-ratio hue spacing keeps onboard riders visually distinct.
        float hue = (i * 0.61803398875f) % 1f;
        return Color.HSVToRGB(hue, 0.62f, 0.96f);
    }

    // -------------------------------------------------------------------------
    // Validation

    /// <summary>Graph-connected AND the rasterized grid is start↔dest↔every-stop reachable.</summary>
    public static bool IsSolvable(TownLayout layout, float cellSize)
    {
        if (!GraphConnected(layout)) return false;

        string[] map = GridLayoutProjector.ToGridMap(layout, cellSize, out _, out var errors);
        if (errors.Count > 0) return false;

        GridModel grid = GridModel.Parse(map, out var mapErrors);
        if (mapErrors.Count > 0) return false;

        Vector2Int s = grid.StartPos;
        Vector2Int d = grid.DestPos;
        if (s == d) return false;
        if (!GridPathfinder.Reachable(grid, s, d)) return false;

        // Every distinct stop cell must be reachable and not collide with S/D.
        var seen = new HashSet<Vector2Int>();
        foreach (TownNode n in layout.nodes)
        {
            if (!n.IsStop || n.kind == NodeKind.TerminalStart || n.kind == NodeKind.TerminalEnd)
                continue;
            if (n.gridCell == s || n.gridCell == d) return false;   // a stop swallowed a terminal
            if (!seen.Add(n.gridCell)) return false;                // two stops on one cell
            if (!GridPathfinder.Reachable(grid, s, n.gridCell)) return false;
        }

        return true;
    }

    static bool GraphConnected(TownLayout layout)
    {
        if (layout.nodes.Count == 0) return false;

        var seen  = new HashSet<int> { layout.startNodeId };
        var stack = new Stack<int>();
        stack.Push(layout.startNodeId);

        while (stack.Count > 0)
        {
            int cur = stack.Pop();
            foreach (int nb in layout.Neighbours(cur))
                if (seen.Add(nb)) stack.Push(nb);
        }

        return seen.Count == layout.nodes.Count;
    }

    // -------------------------------------------------------------------------
    // Helpers

    static AnchorNode MatchAnchor(AnchorNode[] anchors, Vector2 pos)
    {
        if (anchors == null) return null;
        foreach (AnchorNode a in anchors)
            if (Vector2.Distance(a.position, pos) <= AnchorMatchEps) return a;
        return null;
    }

    static NodeKind ToNodeKind(AnchorKind k)
    {
        switch (k)
        {
            case AnchorKind.TerminalStart: return NodeKind.TerminalStart;
            case AnchorKind.TerminalEnd:   return NodeKind.TerminalEnd;
            case AnchorKind.HeritageSite:  return NodeKind.HeritageSite;
            default:                       return NodeKind.NpcDrop;
        }
    }

    static int FindKind(TownLayout layout, NodeKind kind, int fallback)
    {
        foreach (int id in layout.trunkNodeIds)
            if (layout.Node(id).kind == kind) return id;
        return fallback;
    }

    static Vector2 TrunkDirAt(TownLayout layout, int nodeId)
    {
        int idx = layout.trunkNodeIds.IndexOf(nodeId);
        int prev = Mathf.Max(0, idx - 1);
        int next = Mathf.Min(layout.trunkNodeIds.Count - 1, idx + 1);
        Vector2 d = layout.Node(layout.trunkNodeIds[next]).pos - layout.Node(layout.trunkNodeIds[prev]).pos;
        return d.sqrMagnitude > 0.0001f ? d.normalized : Vector2.up;
    }

    static bool FarFromAll(TownLayout layout, Vector2 p, float minDist)
    {
        foreach (TownNode n in layout.nodes)
            if (Vector2.Distance(n.pos, p) < minDist) return false;
        return true;
    }

    static bool TooClose(List<float> values, float v, float minGap)
    {
        foreach (float x in values)
            if (Mathf.Abs(x - v) < minGap) return true;
        return false;
    }

    static int RandRange(System.Random rng, int min, int max) =>
        max <= min ? min : rng.Next(min, max + 1);

    static void Shuffle<T>(IList<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
