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

    /// <summary>Whole-scene background chunks for this layout/chunk delta (may be empty).</summary>
    public List<ScenePlacement> scenePlacements = new List<ScenePlacement>();
}

public static class ManualLayoutProjector
{
    public static ManualLayoutResult Project(TownLayout layout)
    {
        var result = new ManualLayoutResult
        {
            // Scene-template worlds drive along the painted curves; the node
            // spine stays cardinal for Automation.
            trunk = layout.sceneDrivePath.Count > 1
                ? layout.sceneDrivePath.ToArray() : layout.TrunkPolyline(),
            start = layout.Node(layout.startNodeId),
            dest  = layout.Node(layout.destNodeId),
        };

        foreach (TownEdge e in layout.edges)
            result.segments.Add(new RoadSegment(
                layout.Node(e.a).pos, layout.Node(e.b).pos, e.isTrunk));
        result.segments.AddRange(layout.sceneExtraSegments);

        foreach (TownNode n in layout.nodes)
            if (n.IsStop) result.stops.Add(n);

        result.scenePlacements.AddRange(layout.scenePlacements);

        // Earlier → later along the route, so dulog "later stop" logic lines up.
        result.stops.Sort((p, q) => p.alongTrunk.CompareTo(q.alongTrunk));
        return result;
    }

    /// <summary>
    /// Projects only the delta of an appended chunk. Stops include the old
    /// destination (now demoted) and every new boardable node; segments include
    /// only the new edges; the trunk is the new trunk extension.
    /// </summary>
    public static ManualLayoutResult ProjectChunk(TownLayout layout, TownChunk chunk)
    {
        var result = new ManualLayoutResult
        {
            trunk = layout.sceneDrivePath.Count > 1
                ? layout.sceneDrivePath.ToArray() : layout.TrunkPolyline(),
            start = layout.Node(layout.startNodeId),
            dest  = layout.Node(layout.destNodeId),
        };

        foreach (TownEdge e in chunk.edges)
            result.segments.Add(new RoadSegment(
                layout.Node(e.a).pos, layout.Node(e.b).pos, e.isTrunk));
        result.segments.AddRange(chunk.sceneExtraSegments);

        var seen = new HashSet<int>();
        foreach (TownNode n in chunk.nodes)
        {
            if (n.IsStop && seen.Add(n.id))
                result.stops.Add(n);
        }

        // Make sure the old destination (now a regular stop) is included.
        TownNode oldDest = layout.Node(chunk.nodes.Count > 0 ? chunk.nodes[0].id : layout.destNodeId);
        if (oldDest.IsStop && seen.Add(oldDest.id))
            result.stops.Add(oldDest);

        result.scenePlacements.AddRange(chunk.scenePlacements);

        result.stops.Sort((p, q) => p.alongTrunk.CompareTo(q.alongTrunk));
        return result;
    }
}
