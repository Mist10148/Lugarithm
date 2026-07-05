using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode tests for the endless append generator: deterministic chunks,
/// solvability, and a frontier that advances.
/// </summary>
public class StreamingTownGeneratorTests
{
    static readonly int[] ProceduralLevels = { 2, 3, 4, 5 };

    static StreamingTown Begin(int levelIndex, int seed)
    {
        LevelDefinition def = LevelLibrary.Get(levelIndex);
        return StreamingTownGenerator.Begin(def.procedural, def.fares, seed);
    }

    [Test]
    public void Begin_ProducesSolvableLayout()
    {
        foreach (int level in ProceduralLevels)
        {
            StreamingTown s = Begin(level, 123);
            Assert.IsNotNull(s, $"level {level}");
            Assert.IsNotNull(s.Layout, $"level {level}");
            Assert.IsTrue(TownLayoutGenerator.IsSolvable(s.Layout, s.CellSize), $"level {level}");
        }
    }

    [Test]
    public void AppendChunk_IsDeterministic()
    {
        StreamingTown a = Begin(2, 456);
        StreamingTown b = Begin(2, 456);

        TownChunk ca = StreamingTownGenerator.AppendChunk(a);
        TownChunk cb = StreamingTownGenerator.AppendChunk(b);

        Assert.AreEqual(ca.nodes.Count, cb.nodes.Count);
        Assert.AreEqual(ca.edges.Count, cb.edges.Count);
        Assert.AreEqual(ca.requests.Count, cb.requests.Count);

        for (int i = 0; i < ca.nodes.Count; i++)
            Assert.AreEqual(ca.nodes[i].pos, cb.nodes[i].pos, $"node {i} position");
    }

    [Test]
    public void AppendChunk_MultipleChunks_AreDeterministic()
    {
        StreamingTown a = Begin(2, 4567);
        StreamingTown b = Begin(2, 4567);

        for (int chunkIndex = 0; chunkIndex < 5; chunkIndex++)
        {
            TownChunk ca = StreamingTownGenerator.AppendChunk(a);
            TownChunk cb = StreamingTownGenerator.AppendChunk(b);

            Assert.AreEqual(ca.nodes.Count, cb.nodes.Count, $"chunk {chunkIndex}: node count");
            Assert.AreEqual(ca.edges.Count, cb.edges.Count, $"chunk {chunkIndex}: edge count");
            Assert.AreEqual(ca.requests.Count, cb.requests.Count, $"chunk {chunkIndex}: ride count");
            Assert.AreEqual(a.TrunkEndPos, b.TrunkEndPos, $"chunk {chunkIndex}: frontier");
        }
    }

    [Test]
    public void AppendChunk_AdvancesFrontier()
    {
        StreamingTown s = Begin(2, 789);
        Vector2 before = s.Layout.Node(s.Layout.destNodeId).pos;

        TownChunk chunk = StreamingTownGenerator.AppendChunk(s);
        Assert.IsNotNull(chunk);
        Assert.IsTrue(chunk.nodes.Count > 0);

        Assert.Greater(Vector2.Distance(before, s.TrunkEndPos), 1f,
            "frontier should move forward after append");
    }

    [Test]
    public void FallbackChunk_AdvancesFrontier()
    {
        StreamingTown s = Begin(2, 790);
        Vector2 before = s.TrunkEndPos;

        MethodInfo fallback = typeof(StreamingTownGenerator).GetMethod(
            "FallbackChunk", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(fallback);

        var chunk = fallback.Invoke(null, new object[] { s }) as TownChunk;
        Assert.IsNotNull(chunk);
        Assert.Greater(Vector2.Distance(before, s.Layout.Node(s.Layout.destNodeId).pos), 1f,
            "the guaranteed fallback must still extend the road");
    }

    [Test]
    public void AppendChunk_KeepsLayoutSolvable()
    {
        StreamingTown s = Begin(2, 321);
        for (int i = 0; i < 3; i++)
        {
            TownChunk chunk = StreamingTownGenerator.AppendChunk(s);
            Assert.IsNotNull(chunk);
            Assert.IsTrue(TownLayoutGenerator.IsSolvable(s.Layout, s.CellSize),
                $"chunk {i} must keep the layout solvable");
        }
    }

    [Test]
    public void AppendChunk_NewDestination_IsTerminalEnd()
    {
        StreamingTown s = Begin(2, 654);
        StreamingTownGenerator.AppendChunk(s);

        TownNode dest = s.Layout.Node(s.Layout.destNodeId);
        Assert.AreEqual(NodeKind.TerminalEnd, dest.kind);
    }

    [Test]
    public void StreamedTrunk_NeverNetsBackward_AlongForwardAxis()
    {
        // "Roads only go forward": every streamed trunk segment must make non-negative
        // progress along the town's forward axis (lateral turns are allowed; a U-turn
        // that folds the road backward is not).
        foreach (int seed in new[] { 7, 88, 909, 1234, 55 })
        {
            StreamingTown s = Begin(2, seed);
            int k0 = s.Layout.trunkNodeIds.Count;   // authored trunk is exempt
            for (int i = 0; i < 6; i++) StreamingTownGenerator.AppendChunk(s);

            Vector2 fwd = s.ForwardAxis;
            List<int> trunk = s.Layout.trunkNodeIds;
            for (int i = Mathf.Max(1, k0 - 1); i < trunk.Count; i++)
            {
                Vector2 a = s.Layout.Node(trunk[i - 1]).pos;
                Vector2 b = s.Layout.Node(trunk[i]).pos;
                float proj = Vector2.Dot(b - a, fwd);
                Assert.GreaterOrEqual(proj, -0.01f,
                    $"seed {seed}: streamed trunk segment {i} nets backward (proj {proj}) along {fwd}");
            }
        }
    }

    [Test]
    public void AppendedRoads_AreAxisAligned()
    {
        // Manhattan streets: every road segment must be purely horizontal or
        // vertical (only 90° turns) even after several streamed chunks.
        foreach (int seed in new[] { 11, 202, 3003, 44 })
        {
            StreamingTown s = Begin(2, seed);
            for (int i = 0; i < 5; i++) StreamingTownGenerator.AppendChunk(s);

            foreach (TownEdge e in s.Layout.edges)
            {
                Vector2 a = s.Layout.Node(e.a).pos;
                Vector2 b = s.Layout.Node(e.b).pos;
                bool axisAligned = Mathf.Abs(a.x - b.x) < 0.01f || Mathf.Abs(a.y - b.y) < 0.01f;
                Assert.IsTrue(axisAligned,
                    $"seed {seed}: edge {e.a}->{e.b} is diagonal ({a} -> {b})");
            }
        }
    }
}
