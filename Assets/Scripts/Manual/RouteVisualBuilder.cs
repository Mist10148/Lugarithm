using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawned route context handed back to the drive controller: the polyline,
/// its length, and the live stop zones.
/// </summary>
public class RouteContext
{
    public Vector2[] Waypoints;
    public float     TotalLength;
    public StopZone[] Zones;
    public StopZone   DestinationZone;

    /// <summary>
    /// All drivable road segments (trunk + branches) for off-road distance
    /// checks. Null for the authored single-polyline route.
    /// </summary>
    public List<RoadSegment> Segments;

    /// <summary>Maps a procedural town node id to its spawned stop zone.</summary>
    public Dictionary<int, StopZone> ZoneByNode;
}

/// <summary>
/// Builds the manual-mode world at runtime from a route definition:
/// road tiles along the waypoint polyline, stop zones with signs, waiting
/// peeps, and floating labels so players can see each stop and the terminal.
/// All placeholder sprites — rendered in plain top-down perspective.
/// </summary>
public static class RouteVisualBuilder
{
    // -------------------------------------------------------------------------

    public static RouteContext Build(Transform parent, ManualRouteDefinition def)
    {
        var ctx = new RouteContext
        {
            Waypoints   = def.waypoints,
            TotalLength = RouteMath.TotalLength(def.waypoints),
            Zones       = new StopZone[def.stops.Length],
        };

        BuildRoad(parent, def, ctx.TotalLength);

        for (int i = 0; i < def.stops.Length; i++)
        {
            ctx.Zones[i] = BuildStop(parent, def, def.stops[i], i);
            if (def.stops[i].isDestination)
                ctx.DestinationZone = ctx.Zones[i];
        }

        return ctx;
    }

    // -------------------------------------------------------------------------
    // Procedural town (shared TownLayout). Renders trunk + branch roads and a
    // stop zone at every boardable node; waiting peeps are spawned later by the
    // PassengerManager so they carry the committed rider colors.

    public static RouteContext BuildProcedural(Transform parent, ManualLayoutResult layout,
                                               float roadHalfWidth)
    {
        var ctx = new RouteContext
        {
            Waypoints       = layout.trunk,
            TotalLength     = RouteMath.TotalLength(layout.trunk),
            Zones           = new StopZone[layout.stops.Count],
            Segments        = layout.segments,
            ZoneByNode      = new Dictionary<int, StopZone>(),
        };

        Sprite roadSprite = Resources.Load<Sprite>("Placeholders/road_tile");
        var roadRoot = new GameObject("Road");
        roadRoot.transform.SetParent(parent, false);
        foreach (RoadSegment s in layout.segments)
            TileSegment(roadRoot.transform, roadSprite, s.a, s.b,
                        roadHalfWidth * (s.isTrunk ? 2f : 1.5f));

        for (int i = 0; i < layout.stops.Count; i++)
        {
            TownNode node = layout.stops[i];
            bool isDest = node.id == layout.dest.id;
            StopZone zone = BuildProceduralStop(parent, node, i, isDest, roadHalfWidth);
            ctx.Zones[i] = zone;
            ctx.ZoneByNode[node.id] = zone;
            if (isDest) ctx.DestinationZone = zone;
        }

        return ctx;
    }

    /// <summary>
    /// Adds a chunk's roads and stops to an existing top-down procedural world.
    /// The existing destination stop is demoted to an ordinary stop; the new
    /// destination becomes the terminal-end of the appended trunk.
    /// </summary>
    public static void AppendProcedural(Transform parent, RouteContext ctx,
                                        ManualLayoutResult delta, float roadHalfWidth)
    {
        if (ctx == null || delta == null) return;

        Sprite roadSprite = Resources.Load<Sprite>("Placeholders/road_tile");
        Transform roadRoot = parent.Find("Road");
        if (roadRoot == null)
        {
            var rr = new GameObject("Road");
            rr.transform.SetParent(parent, false);
            roadRoot = rr.transform;
        }

        foreach (RoadSegment s in delta.segments)
        {
            ctx.Segments.Add(s);
            TileSegment(roadRoot, roadSprite, s.a, s.b,
                        roadHalfWidth * (s.isTrunk ? 2f : 1.5f));
        }

        var zones = new List<StopZone>(ctx.Zones);
        foreach (TownNode node in delta.stops)
        {
            if (ctx.ZoneByNode != null && ctx.ZoneByNode.ContainsKey(node.id))
            {
                // Demote old destination to ordinary stop.
                if (ctx.ZoneByNode[node.id] == ctx.DestinationZone)
                    ctx.DestinationZone.IsDestination = false;
                continue;
            }

            bool isDest = node.id == delta.dest.id;
            StopZone zone = BuildProceduralStop(parent, node, zones.Count, isDest, roadHalfWidth);
            zones.Add(zone);
            if (ctx.ZoneByNode != null) ctx.ZoneByNode[node.id] = zone;
            if (isDest) ctx.DestinationZone = zone;
        }
        ctx.Zones = zones.ToArray();

        // Extend the trunk polyline with the new trunk vertices.
        if (delta.trunk != null && delta.trunk.Length > 0)
        {
            var waypoints = new List<Vector2>(ctx.Waypoints);
            foreach (Vector2 p in delta.trunk)
            {
                if (waypoints.Count == 0 || Vector2.Distance(waypoints[waypoints.Count - 1], p) > 0.001f)
                    waypoints.Add(p);
            }
            ctx.Waypoints = waypoints.ToArray();
            ctx.TotalLength = RouteMath.TotalLength(ctx.Waypoints);
        }
    }

    static void TileSegment(Transform parent, Sprite sprite, Vector2 a, Vector2 b, float width)
    {
        Vector2 delta = b - a;
        float len = delta.magnitude;
        if (len < 0.0001f) return;
        Vector2 dir = delta / len;

        for (float d = 0f; d <= len; d += 1f)
        {
            var tile = new GameObject("RoadTile");
            tile.transform.SetParent(parent, false);
            tile.transform.position = a + dir * d;
            tile.transform.rotation = Quaternion.FromToRotation(Vector3.up, (Vector3)dir);
            tile.transform.localScale = new Vector3(width, 1.25f, 1f);

            var sr = tile.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = -50;
        }
    }

    static StopZone BuildProceduralStop(Transform parent, TownNode node, int ordinal,
                                        bool isDest, float roadHalfWidth)
    {
        var go = new GameObject($"Stop_{ordinal}_{node.name}");
        go.transform.SetParent(parent, false);
        go.transform.position = node.pos;

        var collider = go.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(roadHalfWidth * 2f + 5f, 7f);

        var zone = go.AddComponent<StopZone>();
        zone.StopIndex     = ordinal;
        zone.StopName      = node.name;
        zone.IsDestination = isDest;

        var sign = new GameObject("Sign");
        sign.transform.SetParent(go.transform, false);
        sign.transform.localPosition = new Vector3(roadHalfWidth + 1.1f, 0f, 0f);
        var signSr = sign.AddComponent<SpriteRenderer>();
        signSr.sprite = Resources.Load<Sprite>("Placeholders/stop_sign");
        signSr.sortingOrder = 6;
        if (isDest) signSr.color = new Color(0.45f, 1f, 0.5f);
        else if (node.kind == NodeKind.HeritageSite) signSr.color = new Color(1f, 0.85f, 0.4f);
        else if (node.kind == NodeKind.NpcDrop)      signSr.color = new Color(0.6f, 0.8f, 1f);

        var labelGo = new GameObject("StopLabel");
        labelGo.transform.SetParent(go.transform, false);
        var text = labelGo.AddComponent<TMPro.TextMeshPro>();
        text.text = isDest
            ? $"<color=#7CFC72>{node.name}</color>\n<color=#FFD700>TERMINAL</color>"
            : node.name;
        text.fontSize  = 2.0f;
        text.alignment = TMPro.TextAlignmentOptions.Center;
        text.color = new Color(1f, 1f, 1f, 0.95f);
        text.sortingOrder = 25;
        labelGo.transform.localPosition = new Vector3(0f, -1.5f, 0f);

        return zone;
    }

    // -------------------------------------------------------------------------

    static void BuildRoad(Transform parent, ManualRouteDefinition def, float totalLength)
    {
        Sprite roadSprite = Resources.Load<Sprite>("Placeholders/road_tile");
        var roadRoot = new GameObject("Road");
        roadRoot.transform.SetParent(parent, false);

        const float step = 1.0f;
        for (float d = 0f; d <= totalLength; d += step)
        {
            Vector2 point     = RouteMath.PointAt(def.waypoints, d);
            Vector2 direction = RouteMath.DirectionAt(def.waypoints, d);

            var tile = new GameObject("RoadTile");
            tile.transform.SetParent(roadRoot.transform, false);
            tile.transform.position = point;
            tile.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
            tile.transform.localScale = new Vector3(def.roadHalfWidth * 2f, 1.25f, 1f);

            var sr = tile.AddComponent<SpriteRenderer>();
            sr.sprite = roadSprite;
            sr.sortingOrder = -50;
        }
    }

    static StopZone BuildStop(Transform parent, ManualRouteDefinition def,
                              ManualStopDefinition stop, int index)
    {
        Vector2 position  = def.waypoints[stop.waypointIndex];
        Vector2 direction = RouteDirectionAtWaypoint(def.waypoints, stop.waypointIndex);

        var go = new GameObject($"Stop_{index}_{stop.stopName}");
        go.transform.SetParent(parent, false);
        go.transform.position = position;

        var collider = go.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(def.roadHalfWidth * 2f + 5f, 7f);
        go.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);

        var zone = go.AddComponent<StopZone>();
        zone.StopIndex     = index;
        zone.StopName      = stop.stopName;
        zone.IsDestination = stop.isDestination;

        // Sign at the roadside (green-tinted for the destination / terminal).
        var sign = new GameObject("Sign");
        sign.transform.SetParent(go.transform, false);
        sign.transform.localPosition = new Vector3(def.roadHalfWidth + 1.1f, 0f, 0f);
        var signSr = sign.AddComponent<SpriteRenderer>();
        signSr.sprite = Resources.Load<Sprite>("Placeholders/stop_sign");
        signSr.sortingOrder = 6;
        if (stop.isDestination)
            signSr.color = new Color(0.45f, 1f, 0.5f);

        // Waiting peeps line up past the sign, away from the road.
        if (stop.waitingPassengers > 0)
            zone.SpawnWaitingPeeps(stop.waitingPassengers,
                                   new Vector2(def.roadHalfWidth + 2.1f, -0.8f),
                                   Vector2.right);

        // Floating label so the player always knows where stops are and where to go.
        BuildStopLabel(go.transform, stop, def.roadHalfWidth);

        return zone;
    }

    static void BuildStopLabel(Transform stop, ManualStopDefinition stopDef, float roadHalfWidth)
    {
        var labelGo = new GameObject("StopLabel");
        labelGo.transform.SetParent(stop, false);

        var text = labelGo.AddComponent<TMPro.TextMeshPro>();
        text.text = stopDef.isDestination
            ? $"<color=#7CFC72>{stopDef.stopName}</color>\n<color=#FFD700>TERMINAL</color>"
            : stopDef.stopName;
        text.fontSize = 2.0f;
        text.alignment = TMPro.TextAlignmentOptions.Center;
        text.color = new Color(1f, 1f, 1f, 0.95f);
        text.sortingOrder = 25;

        // Place the label just behind the stop so it's readable while driving up.
        labelGo.transform.localPosition = new Vector3(0f, -1.5f, 0f);

        // Face upward in world space (top-down camera looks at -Z, so text faces +Z).
        labelGo.transform.rotation = Quaternion.identity;
    }

    static Vector2 RouteDirectionAtWaypoint(Vector2[] points, int index)
    {
        if (points.Length < 2) return Vector2.up;
        if (index >= points.Length - 1)
            return (points[points.Length - 1] - points[points.Length - 2]).normalized;
        return (points[index + 1] - points[index]).normalized;
    }
}
