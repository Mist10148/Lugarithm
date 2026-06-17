using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode tests for the endless append generator: deterministic chunks,
/// solvability, and a frontier that advances.
/// </summary>
public class StreamingTownGeneratorTests
{
    static StreamingTown Begin(int levelIndex, int seed)
    {
        LevelDefinition def = LevelLibrary.Get(levelIndex);
        return StreamingTownGenerator.Begin(def.procedural, def.fares, seed);
    }

    [Test]
    public void Begin_ProducesSolvableLayout()
    {
        StreamingTown s = Begin(2, 123);
        Assert.IsNotNull(s);
        Assert.IsNotNull(s.Layout);
        Assert.IsTrue(TownLayoutGenerator.IsSolvable(s.Layout, s.CellSize));
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
    public void AppendChunk_AdvancesFrontier()
    {
        StreamingTown s = Begin(2, 789);
        Vector2 before = s.TrunkEndPos;

        TownChunk chunk = StreamingTownGenerator.AppendChunk(s);
        Assert.IsNotNull(chunk);
        Assert.IsTrue(chunk.nodes.Count > 0);

        Assert.Greater(Vector2.Distance(before, s.TrunkEndPos), 1f,
            "frontier should move forward after append");
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
