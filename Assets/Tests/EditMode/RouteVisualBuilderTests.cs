using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>Regression coverage for roadside stop visuals in streamed towns.</summary>
public class RouteVisualBuilderTests
{
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
