using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// What a town node is. Terminals and story nodes come from authored anchors;
/// Stop nodes are procedural ordinary-passenger stops; Junctions are pure trunk
/// bends (geometry only, never a stop).
/// </summary>
public enum NodeKind
{
    TerminalStart,
    TerminalEnd,
    HeritageSite,
    NpcDrop,
    Stop,
    Junction,
}

/// <summary>
/// One node in a generated town: a point in Manual-mode world space, its role,
/// and — once <c>GridLayoutProjector</c> has run — the Automation grid cell it
/// rasterizes to. <see cref="alongTrunk"/> is the arc-length of the node's
/// attachment point along the trunk, used to order stops "earlier → later".
/// </summary>
public class TownNode
{
    public int        id;
    public Vector2    pos;
    public NodeKind   kind;
    public string     name;
    public float      alongTrunk;
    public Vector2Int gridCell;   // filled by GridLayoutProjector

    /// <summary>Terminals/heritage/npc/stop nodes are boardable & alightable; junctions are not.</summary>
    public bool IsStop => kind != NodeKind.Junction;
}

/// <summary>An undirected road segment between two nodes.</summary>
public class TownEdge
{
    public int  a;
    public int  b;
    public bool isTrunk;

    public TownEdge(int a, int b, bool isTrunk)
    {
        this.a = a;
        this.b = b;
        this.isTrunk = isTrunk;
    }
}

/// <summary>
/// A fully generated town for one run: the road graph (nodes + edges), the
/// committed passenger requests, and the seed it came from. The single shared
/// source both drive modes project from.
/// </summary>
public class TownLayout
{
    public int seed;
    public int startNodeId;
    public int destNodeId;

    public readonly List<TownNode>        nodes    = new List<TownNode>();
    public readonly List<TownEdge>        edges    = new List<TownEdge>();
    public readonly List<PassengerRequest> requests = new List<PassengerRequest>();

    /// <summary>Trunk node ids in spine order (terminal-start → terminal-end).</summary>
    public readonly List<int> trunkNodeIds = new List<int>();

    /// <summary>
    /// Whole-scene background chunks (in chain order) when the route geometry is
    /// driven by <see cref="SceneTemplateLibrary"/>; empty otherwise.
    /// </summary>
    public readonly List<ScenePlacement> scenePlacements = new List<ScenePlacement>();

    /// <summary>
    /// The smooth driving polyline (start → frontier) in scene-template worlds:
    /// follows the painted curves, unlike the cardinal trunk node spine. Manual
    /// driving/traffic use this; the node graph stays Manhattan for Automation.
    /// Empty outside scene-template worlds.
    /// </summary>
    public readonly List<Vector2> sceneDrivePath = new List<Vector2>();

    /// <summary>Diagonal road segments covering painted curves (off-road math
    /// only — never graph edges).</summary>
    public readonly List<RoadSegment> sceneExtraSegments = new List<RoadSegment>();

    public TownNode Node(int id) => nodes[id];

    /// <summary>The trunk spine as a polyline in Manual-mode world units.</summary>
    public Vector2[] TrunkPolyline()
    {
        var pts = new Vector2[trunkNodeIds.Count];
        for (int i = 0; i < trunkNodeIds.Count; i++)
            pts[i] = nodes[trunkNodeIds[i]].pos;
        return pts;
    }

    /// <summary>Neighbouring node ids of <paramref name="id"/> across all edges.</summary>
    public IEnumerable<int> Neighbours(int id)
    {
        foreach (TownEdge e in edges)
        {
            if (e.a == id) yield return e.b;
            else if (e.b == id) yield return e.a;
        }
    }
}
