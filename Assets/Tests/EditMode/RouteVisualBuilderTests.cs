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
}
