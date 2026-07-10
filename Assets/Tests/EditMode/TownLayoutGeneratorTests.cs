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
    static readonly int[] ProceduralLevels = { 2, 3, 4, 5 };

    static ProceduralLayoutDefinition OtonDef() => LevelLibrary.Get(2).procedural;

    static TownLayout Gen(int seed) =>
        TownLayoutGenerator.Generate(OtonDef(), new FareTable(), seed);

    // -------------------------------------------------------------------------
    // Solvability

    [Test]
    public void EverySeed_ProducesASolvableTown()
    {
        foreach (int level in ProceduralLevels)
        {
            LevelDefinition levelDef = LevelLibrary.Get(level);
            ProceduralLayoutDefinition def = levelDef.procedural;
            for (int seed = 0; seed < 50; seed++)
            {
                TownLayout layout = TownLayoutGenerator.Generate(def, levelDef.fares, seed);
                Assert.IsTrue(TownLayoutGenerator.IsSolvable(layout, def.gen.gridCellSize),
                    $"level {level}, seed {seed}: generated town must be solvable in graph and grid");
            }
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
        // In scene-template worlds the road is wherever the scene art painted
        // it, so anchors bind to the road rather than authored coordinates. The
        // contract: every anchor exists, keeps its kind, keeps its story ORDER
        // along the trunk, and lands at the SAME position for every seed
        // (the initial chain is seed-independent by design).
        string[] names = { "Molo Boundary", "Batiano River", "Poblacion", "Oton Market" };
        NodeKind[] kinds = { NodeKind.TerminalStart, NodeKind.NpcDrop,
                             NodeKind.HeritageSite, NodeKind.TerminalEnd };

        Vector2[] reference = null;
        for (int seed = 0; seed < 30; seed++)
        {
            TownLayout layout = Gen(seed);
            var positions = new Vector2[names.Length];
            float prevAlong = float.NegativeInfinity;
            for (int i = 0; i < names.Length; i++)
            {
                TownNode node = layout.nodes.Find(n => n.name == names[i]);
                Assert.IsNotNull(node, $"anchor '{names[i]}' must exist (seed {seed})");
                Assert.AreEqual(kinds[i], node.kind, $"anchor '{names[i]}' keeps its kind");
                Assert.GreaterOrEqual(node.alongTrunk, prevAlong,
                    $"anchor '{names[i]}' must keep its story order along the trunk");
                prevAlong = node.alongTrunk;
                positions[i] = node.pos;
            }

            if (reference == null) reference = positions;
            else
                for (int i = 0; i < names.Length; i++)
                    Assert.AreEqual(reference[i], positions[i],
                        $"anchor '{names[i]}' must not move across seeds (seed {seed})");

            Assert.AreEqual(NodeKind.TerminalStart, layout.Node(layout.startNodeId).kind);
            Assert.AreEqual(NodeKind.TerminalEnd,   layout.Node(layout.destNodeId).kind);
        }
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
        foreach (int level in ProceduralLevels)
        {
            ProceduralLayoutDefinition def = LevelLibrary.Get(level).procedural;
            for (int seed = 0; seed < 40; seed++)
            {
                TownLayout layout = TownLayoutGenerator.Generate(def, new FareTable(), seed);
                int branches = 0;
                foreach (TownEdge e in layout.edges) if (!e.isTrunk) branches++;
                Assert.LessOrEqual(branches, def.gen.branchCountMax, $"level {level}, seed {seed}: branch cap");
                Assert.GreaterOrEqual(branches, 0, $"level {level}, seed {seed}");
            }
        }
    }

    // -------------------------------------------------------------------------
    // Passenger requests are well-formed

    [Test]
    public void Requests_BoardEarly_AlightLater_WithMatchingFare()
    {
        foreach (int level in ProceduralLevels)
        {
            LevelDefinition levelDef = LevelLibrary.Get(level);
            ProceduralLayoutDefinition def = levelDef.procedural;
            for (int seed = 0; seed < 40; seed++)
            {
                TownLayout layout = TownLayoutGenerator.Generate(def, levelDef.fares, seed);

                Assert.LessOrEqual(layout.requests.Count, def.gen.passengerCountMax,
                    $"level {level}, seed {seed}: rider cap");

                foreach (PassengerRequest r in layout.requests)
                {
                    TownNode origin = layout.Node(r.originNodeId);
                    TownNode dest   = layout.Node(r.destNodeId);

                    Assert.AreNotEqual(r.originNodeId, r.destNodeId,
                        $"level {level}, seed {seed}: rider {r.id} must travel");
                    Assert.GreaterOrEqual(dest.alongTrunk, origin.alongTrunk,
                        $"level {level}, seed {seed}: rider {r.id} alights later than they board");
                    Assert.IsTrue(origin.IsStop && dest.IsStop,
                        $"level {level}, seed {seed}: rider {r.id} uses stop nodes");
                    Assert.AreEqual(FareMath.ComputeFare(r.stopsTraveled, levelDef.fares), r.fare,
                        $"level {level}, seed {seed}: rider {r.id} fare matches FareMath");
                    Assert.GreaterOrEqual(r.tender, r.fare,
                        $"level {level}, seed {seed}: rider {r.id} tenders enough");
                }
            }
        }
    }
}
