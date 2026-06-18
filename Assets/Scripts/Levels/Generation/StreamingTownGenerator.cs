using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Append-only generator for endless free-roam legs. A <see cref="StreamingTown"/>
/// is seeded from the level definition; chunks extend the trunk forward,
/// hang branch stubs, and commit ordinary-passenger rides whose origins lie in
/// the new/last chunk. Deterministic per (baseSeed, chunkIndex).
/// </summary>
public class StreamingTown
{
    public TownLayout Layout;
    public int        NextChunkIndex;
    public Vector2    TrunkEndPos;
    public int        BaseSeed;
    public float      CellSize;
    public FareTable  Fares;
    public float      RoadHalfWidth;

    /// <summary>Nodes added by the most recent chunk (for delta projection).</summary>
    public List<TownNode> LastChunkNodes = new List<TownNode>();

    /// <summary>Edges added by the most recent chunk.</summary>
    public List<TownEdge> LastChunkEdges = new List<TownEdge>();
}

public static class StreamingTownGenerator
{
    const int MaxChunkRetries = 12;

    /// <summary>Begins a streaming town from the level's authored trunk.</summary>
    public static StreamingTown Begin(ProceduralLayoutDefinition def, FareTable fares, int baseSeed)
    {
        var s = new StreamingTown
        {
            Layout = TownLayoutGenerator.Generate(def, fares, baseSeed),
            NextChunkIndex = 0,
            BaseSeed = baseSeed,
            CellSize = def.gen.gridCellSize,
            Fares = fares ?? new FareTable(),
            RoadHalfWidth = def.trunk.Length >= 2 ? Vector2.Distance(def.trunk[0], def.trunk[1]) * 0.5f : 3f,
        };

        TownNode dest = s.Layout.Node(s.Layout.destNodeId);
        s.TrunkEndPos = dest.pos;
        return s;
    }

    /// <summary>
    /// Appends the next chunk and returns the delta of new nodes/edges/requests.
    /// The returned <see cref="TownChunk"/> can be projected into Manual/Automation
    /// worlds without rebuilding the existing scene.
    /// </summary>
    public static TownChunk AppendChunk(StreamingTown s)
    {
        if (s == null || s.Layout == null) return null;

        var chunk = new TownChunk { baseSeed = s.BaseSeed, chunkIndex = s.NextChunkIndex };
        int seed = CombineSeed(s.BaseSeed, s.NextChunkIndex);

        for (int attempt = 0; attempt < MaxChunkRetries; attempt++)
        {
            chunk = TryAppend(s, seed + attempt);
            if (chunk != null) break;
        }

        if (chunk == null)
            chunk = FallbackChunk(s);

        s.NextChunkIndex++;
        s.LastChunkNodes = chunk.nodes;
        s.LastChunkEdges = chunk.edges;

        if (chunk.nodes.Count > 0)
        {
            // The new terminal-end is the last trunk node of the chunk.
            foreach (TownNode n in chunk.nodes)
            {
                if (n.kind == NodeKind.TerminalEnd)
                {
                    s.TrunkEndPos = n.pos;
                    break;
                }
            }
        }

        return chunk;
    }

    static TownChunk TryAppend(StreamingTown s, int seed)
    {
        var rng = new System.Random(seed);
        var layout = s.Layout;
        var chunk = new TownChunk { baseSeed = s.BaseSeed, chunkIndex = s.NextChunkIndex };

        // Snapshot the committed layout so a failed attempt can be rolled back
        // cleanly — otherwise rejected chunks leave dead-end branches and stranded
        // riders behind, which is what made later free-roam legs degrade.
        int nodeCount0  = layout.nodes.Count;
        int edgeCount0  = layout.edges.Count;
        int reqCount0   = layout.requests.Count;
        int trunkCount0 = layout.trunkNodeIds.Count;
        int destId0     = layout.destNodeId;

        // Find the current terminal-end node and demote it to a regular stop.
        TownNode oldDest = layout.Node(layout.destNodeId);
        NodeKind oldDestKind0 = oldDest.kind;
        string   oldDestName0 = oldDest.name;
        oldDest.kind = NodeKind.Stop;
        if (string.IsNullOrEmpty(oldDest.name) || oldDest.name == "Destination")
            oldDest.name = $"Sitio {layout.nodes.Count}";
        chunk.nodes.Add(oldDest);

        // Current trunk end direction, snapped to a cardinal so the road stays
        // grid-aligned (horizontal/vertical only — Manhattan streets).
        int oldDestTrunkIdx = layout.trunkNodeIds.IndexOf(oldDest.id);
        if (oldDestTrunkIdx < 1) oldDestTrunkIdx = layout.trunkNodeIds.Count - 1;
        Vector2 prevPos = layout.Node(layout.trunkNodeIds[oldDestTrunkIdx - 1]).pos;
        Vector2 dir = SnapToCardinal(oldDest.pos - prevPos);

        // Extend the trunk with 2-4 segments; each turn is straight or a single
        // 90° bend, and every node is snapped to the grid for crisp corners.
        int bendCount = rng.Next(2, 5);
        float segLen = 24f + (float)rng.NextDouble() * 12f;

        TownNode last = oldDest;
        for (int i = 0; i < bendCount; i++)
        {
            dir = TurnCardinal(dir, rng);
            Vector2 nextPos = SnapToGrid(last.pos + dir * segLen, s.CellSize);

            var node = new TownNode
            {
                id = layout.nodes.Count,
                pos = nextPos,
                kind = NodeKind.Junction,
                name = $"Bend {layout.nodes.Count}",
                alongTrunk = last.alongTrunk + segLen,
            };
            if (i == bendCount - 1)
            {
                node.kind = NodeKind.TerminalEnd;
                node.name = "Destination";
            }

            layout.nodes.Add(node);
            layout.trunkNodeIds.Add(node.id);
            var edge = new TownEdge(last.id, node.id, isTrunk: true);
            layout.edges.Add(edge);
            chunk.nodes.Add(node);
            chunk.edges.Add(edge);
            chunk.newTrunkNodeIds.Add(node.id);

            last = node;
        }

        layout.destNodeId = last.id;
        s.TrunkEndPos = last.pos;

        // Hang 0-2 branches off the new trunk nodes.
        int wantBranches = rng.Next(0, 3);
        float minClear = Mathf.Max(s.CellSize * 1.5f, 3f);
        int madeBranches = 0;
        var branchRoots = new List<int>(chunk.newTrunkNodeIds);
        // Skip the very last (destination) for branch roots.
        if (branchRoots.Count > 0) branchRoots.RemoveAt(branchRoots.Count - 1);
        TownLayoutGeneratorShuffle(branchRoots, rng);

        foreach (int rootId in branchRoots)
        {
            if (madeBranches >= wantBranches) break;
            TownNode root = layout.Node(rootId);
            Vector2 trunkDir = SnapToCardinal(root.pos - layout.Node(layout.trunkNodeIds[
                Mathf.Max(0, layout.trunkNodeIds.IndexOf(rootId) - 1)]).pos);
            Vector2 perp = new Vector2(-trunkDir.y, trunkDir.x) *
                           (rng.Next(2) == 0 ? 1f : -1f);
            float len = 8f + (float)rng.NextDouble() * 6f;
            Vector2 tip = SnapToGrid(root.pos + perp * len, s.CellSize);

            bool tooClose = false;
            foreach (TownNode n in layout.nodes)
                if (Vector2.Distance(n.pos, tip) < minClear) { tooClose = true; break; }
            if (tooClose) continue;

            var stop = new TownNode
            {
                id = layout.nodes.Count,
                pos = tip,
                kind = NodeKind.Stop,
                name = $"Sitio {layout.nodes.Count}",
                alongTrunk = root.alongTrunk,
            };
            layout.nodes.Add(stop);
            var branchEdge = new TownEdge(rootId, stop.id, isTrunk: false);
            layout.edges.Add(branchEdge);
            chunk.nodes.Add(stop);
            chunk.edges.Add(branchEdge);
            madeBranches++;
        }

        // Commit a few rides whose origins are in the new chunk.
        var stops = new List<TownNode>();
        foreach (TownNode n in layout.nodes)
            if (n.IsStop) stops.Add(n);
        stops.Sort((p, q) => p.alongTrunk.CompareTo(q.alongTrunk));

        int pcount = Mathf.Clamp(rng.Next(1, 4), 1, Mathf.Max(1, stops.Count / 3));
        int firstNewStop = stops.IndexOf(oldDest);
        if (firstNewStop < 0) firstNewStop = stops.Count - 1;

        for (int i = 0; i < pcount; i++)
        {
            int origin = rng.Next(firstNewStop, Mathf.Max(firstNewStop + 1, stops.Count - 1));
            int destIdx = rng.Next(origin + 1, stops.Count);
            int travelled = Mathf.Max(1, destIdx - origin);
            int fare = FareMath.ComputeFare(travelled, s.Fares);

            var req = new PassengerRequest
            {
                id = layout.requests.Count,
                color = RiderColor(layout.requests.Count),
                originNodeId = stops[origin].id,
                destNodeId = stops[destIdx].id,
                stopsTraveled = travelled,
                fare = fare,
                tender = FareMath.GenerateTender(fare, rng),
            };
            layout.requests.Add(req);
            chunk.requests.Add(req);
        }

        if (!ValidateChunk(layout, s.CellSize))
        {
            // Roll back every mutation this attempt made to the shared layout so a
            // rejected chunk never corrupts the committed town.
            layout.nodes.RemoveRange(nodeCount0, layout.nodes.Count - nodeCount0);
            layout.edges.RemoveRange(edgeCount0, layout.edges.Count - edgeCount0);
            layout.requests.RemoveRange(reqCount0, layout.requests.Count - reqCount0);
            layout.trunkNodeIds.RemoveRange(trunkCount0, layout.trunkNodeIds.Count - trunkCount0);
            layout.destNodeId = destId0;
            oldDest.kind = oldDestKind0;
            oldDest.name = oldDestName0;
            s.TrunkEndPos = oldDest.pos;
            return null;
        }

        return chunk;
    }

    static TownChunk FallbackChunk(StreamingTown s)
    {
        // If random append keeps failing, just extend the trunk straight ahead.
        var layout = s.Layout;
        var chunk = new TownChunk { baseSeed = s.BaseSeed, chunkIndex = s.NextChunkIndex };

        TownNode oldDest = layout.Node(layout.destNodeId);
        oldDest.kind = NodeKind.Stop;
        chunk.nodes.Add(oldDest);

        int oldDestTrunkIdx = layout.trunkNodeIds.IndexOf(oldDest.id);
        if (oldDestTrunkIdx < 1) oldDestTrunkIdx = layout.trunkNodeIds.Count - 1;
        Vector2 prevPos = layout.Node(layout.trunkNodeIds[oldDestTrunkIdx - 1]).pos;
        Vector2 dir = SnapToCardinal(oldDest.pos - prevPos);

        var node = new TownNode
        {
            id = layout.nodes.Count,
            pos = SnapToGrid(oldDest.pos + dir * 30f, s.CellSize),
            kind = NodeKind.TerminalEnd,
            name = "Destination",
            alongTrunk = oldDest.alongTrunk + 30f,
        };
        layout.nodes.Add(node);
        layout.trunkNodeIds.Add(node.id);
        var edge = new TownEdge(oldDest.id, node.id, isTrunk: true);
        layout.edges.Add(edge);
        chunk.nodes.Add(node);
        chunk.edges.Add(edge);
        chunk.newTrunkNodeIds.Add(node.id);
        layout.destNodeId = node.id;

        return chunk;
    }

    static bool ValidateChunk(TownLayout layout, float cellSize)
    {
        return TownLayoutGenerator.IsSolvable(layout, cellSize);
    }

    static int CombineSeed(int baseSeed, int chunkIndex)
    {
        unchecked
        {
            int h = baseSeed;
            h = (h * 397) ^ chunkIndex;
            return h & 0x7fffffff;
        }
    }

    /// <summary>Snaps a point to the generation grid so corners stay crisp.</summary>
    static Vector2 SnapToGrid(Vector2 p, float cell)
    {
        cell = Mathf.Max(0.5f, cell);
        return new Vector2(
            Mathf.Round(p.x / cell) * cell,
            Mathf.Round(p.y / cell) * cell);
    }

    /// <summary>Snaps a direction to the nearest cardinal (axis-aligned) unit vector.</summary>
    static Vector2 SnapToCardinal(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) return Vector2.up;
        return Mathf.Abs(dir.x) >= Mathf.Abs(dir.y)
            ? new Vector2(Mathf.Sign(dir.x), 0f)
            : new Vector2(0f, Mathf.Sign(dir.y));
    }

    /// <summary>
    /// Returns the next cardinal heading: mostly straight, occasionally a single
    /// 90° left or right turn. Never reverses (no 180° U-turn onto the road).
    /// </summary>
    static Vector2 TurnCardinal(Vector2 dir, System.Random rng)
    {
        double r = rng.NextDouble();
        if (r < 0.6) return dir;                          // straight ahead
        return r < 0.8
            ? new Vector2(-dir.y, dir.x)                  // turn left (+90°)
            : new Vector2(dir.y, -dir.x);                 // turn right (−90°)
    }

    static void TownLayoutGeneratorShuffle<T>(IList<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    static Color RiderColor(int i)
    {
        float hue = (i * 0.61803398875f) % 1f;
        return Color.HSVToRGB(hue, 0.62f, 0.96f);
    }
}

/// <summary>Delta produced by <see cref="StreamingTownGenerator.AppendChunk"/>.</summary>
public class TownChunk
{
    public int baseSeed;
    public int chunkIndex;
    public readonly List<TownNode>        nodes        = new List<TownNode>();
    public readonly List<TownEdge>        edges        = new List<TownEdge>();
    public readonly List<PassengerRequest> requests     = new List<PassengerRequest>();
    public readonly List<int>             newTrunkNodeIds = new List<int>();
}
