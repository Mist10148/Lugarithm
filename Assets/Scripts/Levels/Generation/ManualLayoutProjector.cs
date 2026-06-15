using System.Collections.Generic;
using UnityEngine;

/// <summary>One drivable road segment in Manual-mode world units.</summary>
public struct RoadSegment
{
    public Vector2 a;
    public Vector2 b;
    public bool    isTrunk;

    public RoadSegment(Vector2 a, Vector2 b, bool isTrunk)
    {
        this.a = a; this.b = b; this.isTrunk = isTrunk;
    }
}

/// <summary>
/// Projects a generated <see cref="TownLayout"/> into the data the Manual-mode
/// world builder needs: the trunk polyline the jeepney drives, every road
/// segment (trunk + branches) for off-road distance checks, and the stop nodes.
/// Pure data — <c>RouteVisualBuilder</c> turns this into scene objects.
/// </summary>
public class ManualLayoutResult
{
    public Vector2[]          trunk;
    public List<RoadSegment>  segments = new List<RoadSegment>();
    public List<TownNode>     stops    = new List<TownNode>();   // boardable nodes incl. terminals
    public TownNode           start;
    public TownNode           dest;
}

public static class ManualLayoutProjector
{
    public static ManualLayoutResult Project(TownLayout layout)
    {
        var result = new ManualLayoutResult
        {
            trunk = layout.TrunkPolyline(),
            start = layout.Node(layout.startNodeId),
            dest  = layout.Node(layout.destNodeId),
        };

        foreach (TownEdge e in layout.edges)
            result.segments.Add(new RoadSegment(
                layout.Node(e.a).pos, layout.Node(e.b).pos, e.isTrunk));

        foreach (TownNode n in layout.nodes)
            if (n.IsStop) result.stops.Add(n);

        // Earlier → later along the route, so dulog "later stop" logic lines up.
        result.stops.Sort((p, q) => p.alongTrunk.CompareTo(q.alongTrunk));
        return result;
    }
}
