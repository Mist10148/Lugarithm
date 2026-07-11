using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>Regression coverage for roadside stop visuals in streamed towns.</summary>
public class RouteVisualBuilderTests
{
    [Test]
    public void TopDownGridSpace_RemoveWaitingPeeps_RemovesMultipleAtOneStop()
    {
        var root = new GameObject("TopDownPeepRemovalTest");

        try
        {
            TownLayout layout = BuildTinyPassengerTown(twoAtOrigin: true);
            var space = new TopDownGridSpace(layout, cellSize: 6f, roadHalfWidth: 3f, root.transform);
            Vector2Int originCell = layout.Node(1).gridCell;
            StopZone zone = space.RouteContext.ZoneByNode[1];

            Assert.AreEqual(2, zone.WaitingCount);
            Assert.IsTrue(space.IsOccupied(originCell));

            space.RemoveWaitingPeeps(originCell, 2);

            Assert.AreEqual(0, zone.WaitingCount);
            Assert.IsFalse(space.IsOccupied(originCell));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void AppendProcedural_DemotedTerminalKeepsSignAndPassengersOffNewRoad()
    {
        const float roadHalfWidth = 3f;
        var root = new GameObject("RouteVisualBuilderTests");

        try
        {
            var oldTerminal = new TownNode
            {
                id = 1, pos = new Vector2(10f, 0f), kind = NodeKind.TerminalEnd, name = "Old Terminal",
            };
            var initial = new ManualLayoutResult
            {
                trunk = new[] { Vector2.zero, oldTerminal.pos },
                dest = oldTerminal,
                segments = new List<RoadSegment> { new RoadSegment(Vector2.zero, oldTerminal.pos, true) },
                stops = new List<TownNode> { oldTerminal },
            };

            RouteContext ctx = RouteVisualBuilder.BuildProcedural(root.transform, initial, roadHalfWidth);
            StopZone oldZone = ctx.Zones[0];

            var newTerminal = new TownNode
            {
                id = 2, pos = new Vector2(10f, -10f), kind = NodeKind.TerminalEnd, name = "New Terminal",
            };
            oldTerminal.kind = NodeKind.Stop;
            var delta = new ManualLayoutResult
            {
                trunk = new[] { Vector2.zero, oldTerminal.pos, newTerminal.pos },
                dest = newTerminal,
                segments = new List<RoadSegment> { new RoadSegment(oldTerminal.pos, newTerminal.pos, true) },
                stops = new List<TownNode> { oldTerminal, newTerminal },
            };

            RouteVisualBuilder.AppendProcedural(root.transform, ctx, delta, roadHalfWidth);
            oldZone.SpawnWaitingPeeps(1, new Vector2(roadHalfWidth + 2.1f, -0.8f), Vector2.right);

            Transform sign = oldZone.transform.Find("Sign");
            Transform passenger = oldZone.transform.Find("Peep_0");
            Assert.IsNotNull(sign);
            Assert.IsNotNull(passenger);
            Assert.IsFalse(oldZone.IsDestination);

            Assert.Greater(RouteMath.NearestDistanceToGraph(ctx.Segments, sign.position), roadHalfWidth,
                "the demoted terminal's sign must remain outside both incident roads");
            Assert.Greater(RouteMath.NearestDistanceToGraph(ctx.Segments, passenger.position), roadHalfWidth,
                "passengers spawned after the turn must remain beside the road");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    static TownLayout BuildTinyPassengerTown(bool twoAtOrigin)
    {
        var layout = new TownLayout
        {
            seed = 17,
            startNodeId = 0,
            destNodeId = 2,
        };

        layout.nodes.Add(new TownNode
        {
            id = 0, pos = Vector2.zero, kind = NodeKind.TerminalStart, name = "Start", alongTrunk = 0f,
        });
        layout.nodes.Add(new TownNode
        {
            id = 1, pos = new Vector2(20f, 0f), kind = NodeKind.Stop, name = "Stop", alongTrunk = 20f,
        });
        layout.nodes.Add(new TownNode
        {
            id = 2, pos = new Vector2(40f, 0f), kind = NodeKind.TerminalEnd, name = "Dest", alongTrunk = 40f,
        });
        layout.trunkNodeIds.AddRange(new[] { 0, 1, 2 });
        layout.edges.Add(new TownEdge(0, 1, true));
        layout.edges.Add(new TownEdge(1, 2, true));

        int count = twoAtOrigin ? 2 : 1;
        for (int i = 0; i < count; i++)
        {
            layout.requests.Add(new PassengerRequest
            {
                id = i,
                originNodeId = 1,
                destNodeId = 2,
                color = StopZone.PeepColor(20 + i),
                fare = 13,
                tender = 13,
                stopsTraveled = 1,
            });
        }

        return layout;
    }

    [Test]
    public void AppendProcedural_ParentsNewVisualsUnderChunkRoot()
    {
        const float roadHalfWidth = 3f;
        var root = new GameObject("RVB_ChunkRootTest");

        try
        {
            var oldTerminal = new TownNode
            {
                id = 1, pos = new Vector2(10f, 0f), kind = NodeKind.TerminalEnd, name = "Old Terminal",
            };
            var initial = new ManualLayoutResult
            {
                trunk = new[] { Vector2.zero, oldTerminal.pos },
                dest = oldTerminal,
                segments = new List<RoadSegment> { new RoadSegment(Vector2.zero, oldTerminal.pos, true) },
                stops = new List<TownNode> { oldTerminal },
            };

            RouteContext ctx = RouteVisualBuilder.BuildProcedural(root.transform, initial, roadHalfWidth);
            StopZone oldZone = ctx.Zones[0];

            var newTerminal = new TownNode
            {
                id = 2, pos = new Vector2(20f, 0f), kind = NodeKind.TerminalEnd, name = "New Terminal",
                alongTrunk = 20f,
            };
            oldTerminal.kind = NodeKind.Stop;
            var delta = new ManualLayoutResult
            {
                trunk = new[] { Vector2.zero, oldTerminal.pos, newTerminal.pos },
                dest = newTerminal,
                segments = new List<RoadSegment> { new RoadSegment(oldTerminal.pos, newTerminal.pos, true) },
                stops = new List<TownNode> { oldTerminal, newTerminal },
            };

            var chunkRoot = new GameObject("ChunkRoot").transform;
            chunkRoot.SetParent(root.transform, false);
            RouteVisualBuilder.AppendProcedural(root.transform, ctx, delta, roadHalfWidth, chunkRoot);

            // Scene-template worlds group visuals under "SceneChunks"; the
            // placeholder fallback keeps the legacy "Road" container. This test
            // builds its layout by hand (no scene placements), so it exercises
            // the fallback path.
            Assert.IsTrue(chunkRoot.Find("Road") != null || chunkRoot.Find("SceneChunks") != null,
                "new road visuals should be grouped under the chunk");
            Assert.AreEqual(chunkRoot, ctx.ZoneByNode[newTerminal.id].transform.parent);

            chunkRoot.gameObject.SetActive(false);
            Assert.IsTrue(oldZone.gameObject.activeInHierarchy,
                "deactivating the appended chunk must not hide existing route visuals");
            Assert.IsFalse(ctx.ZoneByNode[newTerminal.id].gameObject.activeInHierarchy);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void ClearestRoadside_AvoidsParallelRoadOnPreferredSide()
    {
        const float rhw = 3f;
        var segments = new List<RoadSegment>
        {
            new RoadSegment(new Vector2(0f, 0f),  new Vector2(40f, 0f),  true),   // the stop's own road
            new RoadSegment(new Vector2(0f, -5f), new Vector2(40f, -5f), true),   // a parallel road to the south
        };

        // The incident-leg "preferred" side of a horizontal road is south (0,-1), but a
        // parallel road sits there — so the clearance-aware choice must flip north.
        Vector2 outward = RouteMath.ClearestRoadside(new Vector2(20f, 0f), segments, rhw, Vector2.down);

        Assert.Greater(outward.y, 0.5f, "should point to the open north side, away from the parallel road");
        float clearance = RouteMath.NearestDistanceToGraph(
            segments, new Vector2(20f, 0f) + outward * (rhw + 2.6f));
        Assert.Greater(clearance, rhw, "the chosen roadside must clear every road, not just the incident one");
    }

    [Test]
    public void ClearestRoadside_OpenStraightRoad_KeepsConsistentPreferredSide()
    {
        const float rhw = 3f;
        var segments = new List<RoadSegment> { new RoadSegment(Vector2.zero, new Vector2(40f, 0f), true) };

        // Both sides are clear, so the preferred (incident-leg) side wins the tie — stops on
        // one road must all pick the same side, exactly as today.
        Vector2 outward = RouteMath.ClearestRoadside(new Vector2(20f, 0f), segments, rhw, Vector2.down);

        Assert.AreEqual(0f, outward.x, 0.01f);
        Assert.AreEqual(-1f, outward.y, 0.01f);
    }

    [Test]
    public void BuildProcedural_StopBesideParallelRoad_KeepsSignAndPassengersOffBothRoads()
    {
        const float rhw = 3f;
        var root = new GameObject("RVB_ParallelRoadTest");

        try
        {
            var stop = new TownNode { id = 1, pos = new Vector2(20f, 0f), kind = NodeKind.Stop, name = "Sitio" };
            var dest = new TownNode { id = 2, pos = new Vector2(40f, 0f), kind = NodeKind.TerminalEnd, name = "Dest" };
            var layout = new ManualLayoutResult
            {
                trunk = new[] { Vector2.zero, dest.pos },
                dest  = dest,
                segments = new List<RoadSegment>
                {
                    new RoadSegment(Vector2.zero,         new Vector2(40f, 0f),  true),   // own road
                    new RoadSegment(new Vector2(0f, -5f), new Vector2(40f, -5f), true),   // parallel, south
                },
                stops = new List<TownNode> { stop, dest },
            };

            RouteContext ctx = RouteVisualBuilder.BuildProcedural(root.transform, layout, rhw);
            StopZone zone = ctx.ZoneByNode[stop.id];
            zone.SpawnWaitingPeeps(1, new Vector2(rhw + 2.1f, -0.8f), Vector2.right);

            Transform sign = zone.transform.Find("Sign");
            Transform peep = zone.transform.Find("Peep_0");
            Assert.IsNotNull(sign);
            Assert.IsNotNull(peep);
            Assert.Greater(RouteMath.NearestDistanceToGraph(layout.segments, sign.position), rhw,
                "the stop sign must clear the nearby parallel road, not just its own");
            Assert.Greater(RouteMath.NearestDistanceToGraph(layout.segments, peep.position), rhw,
                "waiting passengers must clear the nearby parallel road");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void BuildProcedural_CornerWithNonIntegerLengths_HasNoGapsAndACornerCap()
    {
        const float rhw = 3f;
        var root = new GameObject("RVB_CornerTest");

        try
        {
            Vector2 bend = new Vector2(10.5f, 0f);
            Vector2 end  = new Vector2(10.5f, 7.3f);
            var dest = new TownNode { id = 1, pos = end, kind = NodeKind.TerminalEnd, name = "Dest" };
            var layout = new ManualLayoutResult
            {
                trunk = new[] { Vector2.zero, bend, end },
                dest  = dest,
                segments = new List<RoadSegment>
                {
                    new RoadSegment(Vector2.zero, bend, true),
                    new RoadSegment(bend, end, true),
                },
                stops = new List<TownNode> { dest },
            };

            RouteVisualBuilder.BuildProcedural(root.transform, layout, rhw);

            var tilePositions = new List<Vector2>();
            bool capAtBend = false;
            foreach (Transform t in root.GetComponentsInChildren<Transform>())
            {
                if (t.name == "RoadTile" || t.name == "RoadCap")
                    tilePositions.Add(t.position);
                if (t.name == "RoadCap" && ((Vector2)t.position - bend).magnitude < 0.01f)
                    capAtBend = true;
            }

            Assert.IsTrue(capAtBend, "a square RoadCap must sit exactly on the bend vertex");
            Assert.IsTrue(HasTileNear(tilePositions, end, 0.01f),
                "a tile must sit exactly at the route end despite the non-integer segment length");

            var poly = new[] { Vector2.zero, bend, end };
            float total = RouteMath.TotalLength(poly);
            for (float d = 0f; d <= total; d += 0.5f)
            {
                Vector2 p = RouteMath.PointAt(poly, d);
                Assert.IsTrue(HasTileNear(tilePositions, p, 0.75f),
                    $"road coverage gap near along={d:0.0} ({p})");
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    static bool HasTileNear(List<Vector2> tiles, Vector2 point, float radius)
    {
        foreach (Vector2 t in tiles)
            if ((t - point).magnitude <= radius)
                return true;
        return false;
    }

    [Test]
    public void BuildProcedural_SceneWorld_StampsPaintedRoadMetrics_AndKeepsPropsOffTheAsphalt()
    {
        var root = new GameObject("RVB_SceneMetricsTest");

        try
        {
            var stop = new TownNode { id = 1, pos = new Vector2(20f, 0f), kind = NodeKind.Stop, name = "Sitio" };
            var dest = new TownNode { id = 2, pos = new Vector2(40f, 0f), kind = NodeKind.TerminalEnd, name = "Dest" };
            var layout = new ManualLayoutResult
            {
                trunk = new[] { Vector2.zero, dest.pos },
                dest  = dest,
                segments = new List<RoadSegment> { new RoadSegment(Vector2.zero, dest.pos, true) },
                stops = new List<TownNode> { stop, dest },
            };
            layout.scenePlacements.Add(new ScenePlacement
            {
                sprite = "town_horizontal_0", center = new Vector2(20f, 0f), order = 0,
            });

            // Any scene placement means the painted 12-unit road drives the
            // metrics, regardless of the placeholder half-width passed in.
            RouteContext ctx = RouteVisualBuilder.BuildProcedural(root.transform, layout, 3f);

            Assert.AreEqual(RoadMetrics.SceneRoadHalfWidth, ctx.RoadHalfWidth, 0.001f);
            Assert.AreEqual(RoadMetrics.SceneLaneOffset, ctx.LaneOffset, 0.001f);

            StopZone zone = ctx.ZoneByNode[stop.id];
            zone.SpawnWaitingPeeps(1, new Vector2(ctx.RoadHalfWidth + 2.1f, -0.8f), Vector2.right);

            Transform sign = zone.transform.Find("Sign");
            Transform peep = zone.transform.Find("Peep_0");
            Assert.IsNotNull(sign);
            Assert.IsNotNull(peep);
            Assert.Greater(RouteMath.NearestDistanceToGraph(layout.segments, sign.position),
                RoadMetrics.SceneRoadHalfWidth,
                "the stop sign must sit beyond the painted road's 6-unit half-width, not on the asphalt");
            Assert.Greater(RouteMath.NearestDistanceToGraph(layout.segments, peep.position),
                RoadMetrics.SceneRoadHalfWidth,
                "waiting passengers must sit beyond the painted road's 6-unit half-width");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void RoadsideDecorator_StraightTrunk_PlacesFrontageOffRoadAndClearOfStops()
    {
        if (Resources.Load<Sprite>("Placeholders/bldg_sari_sari") == null)
            Assert.Ignore("Placeholder building art not generated yet (run Lugarithm/Generate Placeholder Art).");

        const float rhw = 3f;
        var root = new GameObject("RVB_DecoratorTest");

        try
        {
            var segments = new List<RoadSegment> { new RoadSegment(Vector2.zero, new Vector2(60f, 0f), true) };
            var stops    = new List<Vector2> { new Vector2(30f, 0f) };

            RoadsideDecorator.DecorateSegments(root.transform, segments, segments, stops, rhw, seed: 1);

            SpriteRenderer[] spawned = root.GetComponentsInChildren<SpriteRenderer>();
            Assert.Greater(spawned.Length, 0, "continuous frontage should spawn buildings/folk along the trunk");

            foreach (SpriteRenderer sr in spawned)
            {
                Vector2 p = sr.transform.position;
                Assert.Greater(RouteMath.NearestDistanceToGraph(segments, p), rhw,
                    $"{sr.gameObject.name} must sit beside the road, never on it");
                Assert.Greater((p - stops[0]).magnitude, rhw,
                    $"{sr.gameObject.name} must stay clear of the stop's sign/peep window");
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
