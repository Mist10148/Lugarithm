using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode tests for the hybrid procedural town generator. Oton (level 2) is the
/// proving ground: fixed anchors stay put, the per-run graph is always solvable,
/// and committed passenger rides are well-formed.
/// </summary>
public class TownLayoutGeneratorTests
{
    static ProceduralLayoutDefinition OtonDef() => LevelLibrary.Get(2).procedural;

    static TownLayout Gen(int seed) =>
        TownLayoutGenerator.Generate(OtonDef(), new FareTable(), seed);

    // -------------------------------------------------------------------------
    // Solvability

    [Test]
    public void EverySeed_ProducesASolvableTown()
    {
        ProceduralLayoutDefinition def = OtonDef();
        for (int seed = 0; seed < 50; seed++)
        {
            TownLayout layout = TownLayoutGenerator.Generate(def, new FareTable(), seed);
            Assert.IsTrue(TownLayoutGenerator.IsSolvable(layout, def.gen.gridCellSize),
                $"seed {seed}: generated town must be solvable in graph and grid");
        }
    }

    [Test]
    public void Deterministic_SameSeedSameTown()
    {
        for (int seed = 0; seed < 10; seed++)
        {
            TownLayout a = Gen(seed);
            TownLayout b = Gen(seed);

            Assert.AreEqual(a.nodes.Count, b.nodes.Count, $"seed {seed}: node count");
            Assert.AreEqual(a.edges.Count, b.edges.Count, $"seed {seed}: edge count");
            Assert.AreEqual(a.requests.Count, b.requests.Count, $"seed {seed}: request count");

            for (int i = 0; i < a.nodes.Count; i++)
                Assert.AreEqual(a.nodes[i].pos, b.nodes[i].pos, $"seed {seed}: node {i} position");

            for (int i = 0; i < a.requests.Count; i++)
            {
                Assert.AreEqual(a.requests[i].originNodeId, b.requests[i].originNodeId);
                Assert.AreEqual(a.requests[i].destNodeId, b.requests[i].destNodeId);
                Assert.AreEqual(a.requests[i].fare, b.requests[i].fare);
            }
        }
    }

    // -------------------------------------------------------------------------
    // Fixed anchors never move

    [Test]
    public void Anchors_AreImmovableAcrossSeeds()
    {
        for (int seed = 0; seed < 30; seed++)
        {
            TownLayout layout = Gen(seed);

            AssertAnchor(layout, "Molo Boundary", new Vector2(0f, 0f),   NodeKind.TerminalStart);
            AssertAnchor(layout, "Batiano River", new Vector2(0f, 28f),  NodeKind.NpcDrop);
            AssertAnchor(layout, "Poblacion",     new Vector2(16f, 70f), NodeKind.HeritageSite);
            AssertAnchor(layout, "Oton Market",   new Vector2(2f, 112f), NodeKind.TerminalEnd);

            Assert.AreEqual(NodeKind.TerminalStart, layout.Node(layout.startNodeId).kind);
            Assert.AreEqual(NodeKind.TerminalEnd,   layout.Node(layout.destNodeId).kind);
        }
    }

    static void AssertAnchor(TownLayout layout, string name, Vector2 pos, NodeKind kind)
    {
        TownNode node = layout.nodes.Find(n => n.name == name);
        Assert.IsNotNull(node, $"anchor '{name}' must exist");
        Assert.AreEqual(pos, node.pos, $"anchor '{name}' must not move");
        Assert.AreEqual(kind, node.kind, $"anchor '{name}' keeps its kind");
    }

    // -------------------------------------------------------------------------
    // Sensible roads: the only dead-ends are valid stops (no mid-route dead-ends)

    [Test]
    public void DeadEnds_AreOnlyTerminalsOrStops()
    {
        for (int seed = 0; seed < 40; seed++)
        {
            TownLayout layout = Gen(seed);
            var degree = new Dictionary<int, int>();
            foreach (TownNode n in layout.nodes) degree[n.id] = 0;
            foreach (TownEdge e in layout.edges) { degree[e.a]++; degree[e.b]++; }

            foreach (TownNode n in layout.nodes)
                if (degree[n.id] <= 1)
                    Assert.AreNotEqual(NodeKind.Junction, n.kind,
                        $"seed {seed}: node '{n.name}' is a dead-end but only a bend (illegal)");
        }
    }

    [Test]
    public void BranchCount_StaysWithinBounds()
    {
        ProceduralLayoutDefinition def = OtonDef();
        for (int seed = 0; seed < 40; seed++)
        {
            TownLayout layout = Gen(seed);
            int branches = 0;
            foreach (TownEdge e in layout.edges) if (!e.isTrunk) branches++;
            Assert.LessOrEqual(branches, def.gen.branchCountMax, $"seed {seed}: branch cap");
            Assert.GreaterOrEqual(branches, 0, $"seed {seed}");
        }
    }

    // -------------------------------------------------------------------------
    // Passenger requests are well-formed

    [Test]
    public void Requests_BoardEarly_AlightLater_WithMatchingFare()
    {
        var fares = new FareTable();
        ProceduralLayoutDefinition def = OtonDef();

        for (int seed = 0; seed < 40; seed++)
        {
            TownLayout layout = TownLayoutGenerator.Generate(def, fares, seed);

            Assert.LessOrEqual(layout.requests.Count, def.gen.passengerCountMax, $"seed {seed}: rider cap");

            foreach (PassengerRequest r in layout.requests)
            {
                TownNode origin = layout.Node(r.originNodeId);
                TownNode dest   = layout.Node(r.destNodeId);

                Assert.AreNotEqual(r.originNodeId, r.destNodeId, $"seed {seed}: rider {r.id} must travel");
                Assert.GreaterOrEqual(dest.alongTrunk, origin.alongTrunk,
                    $"seed {seed}: rider {r.id} alights later than they board");
                Assert.IsTrue(origin.IsStop && dest.IsStop, $"seed {seed}: rider {r.id} uses stop nodes");
                Assert.AreEqual(FareMath.ComputeFare(r.stopsTraveled, fares), r.fare,
                    $"seed {seed}: rider {r.id} fare matches FareMath");
                Assert.GreaterOrEqual(r.tender, r.fare, $"seed {seed}: rider {r.id} tenders enough");
            }
        }
    }
}
