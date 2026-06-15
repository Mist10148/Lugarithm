using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode tests for the manual-mode projection of a generated town: the
/// road-segment graph off-road distance and the projected route data.
/// </summary>
public class RouteGraphProjectionTests
{
    [Test]
    public void NearestDistanceToGraph_PicksClosestSegment()
    {
        var segments = new List<RoadSegment>
        {
            new RoadSegment(new Vector2(0f, 0f), new Vector2(10f, 0f), true),  // along x
            new RoadSegment(new Vector2(10f, 0f), new Vector2(10f, 10f), false) // up at the end
        };

        // Above the first segment.
        Assert.AreEqual(3f, RouteMath.NearestDistanceToGraph(segments, new Vector2(5f, 3f)), 1e-3f);
        // Nearer the vertical branch.
        Assert.AreEqual(2f, RouteMath.NearestDistanceToGraph(segments, new Vector2(12f, 5f)), 1e-3f);
        // On a segment endpoint.
        Assert.AreEqual(0f, RouteMath.NearestDistanceToGraph(segments, new Vector2(10f, 0f)), 1e-3f);
    }

    [Test]
    public void NearestDistanceToGraph_EmptyIsFarAway()
    {
        Assert.AreEqual(float.MaxValue, RouteMath.NearestDistanceToGraph(null, Vector2.zero));
        Assert.AreEqual(float.MaxValue,
            RouteMath.NearestDistanceToGraph(new List<RoadSegment>(), Vector2.zero));
    }

    [Test]
    public void Project_OrdersStopsAlongTrunk_WithTerminalsResolved()
    {
        ProceduralLayoutDefinition def = LevelLibrary.Get(2).procedural;
        TownLayout layout = TownLayoutGenerator.Generate(def, new FareTable(), 11);

        ManualLayoutResult result = ManualLayoutProjector.Project(layout);

        Assert.AreEqual(layout.edges.Count, result.segments.Count, "one segment per edge");
        Assert.AreEqual(NodeKind.TerminalStart, result.start.kind);
        Assert.AreEqual(NodeKind.TerminalEnd,   result.dest.kind);

        for (int i = 1; i < result.stops.Count; i++)
            Assert.GreaterOrEqual(result.stops[i].alongTrunk, result.stops[i - 1].alongTrunk,
                "stops are ordered earlier → later along the trunk");

        // The destination terminal is the last (furthest-along) stop.
        Assert.AreEqual(result.dest.id, result.stops[result.stops.Count - 1].id);
    }
}
