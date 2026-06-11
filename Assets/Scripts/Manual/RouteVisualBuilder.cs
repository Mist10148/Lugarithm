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
}

/// <summary>
/// Builds the manual-mode world at runtime from a route definition:
/// road tiles along the waypoint polyline, stop zones with signs and waiting
/// peeps, and a marked destination. All placeholder sprites.
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

    static void BuildRoad(Transform parent, ManualRouteDefinition def, float totalLength)
    {
        Sprite roadSprite = Resources.Load<Sprite>("Placeholders/iso_ground_path");
        var roadRoot = new GameObject("Road");
        roadRoot.transform.SetParent(parent, false);

        // Lay iso path tiles densely along the projected polyline so the road
        // reads as a continuous diagonal ribbon.
        const float step = 0.5f;
        for (float d = 0f; d <= totalLength; d += step)
        {
            Vector2 point = RouteMath.PointAt(def.waypoints, d);

            var tile = new GameObject("RoadTile");
            tile.transform.SetParent(roadRoot.transform, false);
            tile.transform.position = IsoProjection.Project(point);

            var sr = tile.AddComponent<SpriteRenderer>();
            sr.sprite = roadSprite;
            sr.sortingOrder = -50;   // flat ground layer, under signs/peeps/jeepney
        }
    }

    static StopZone BuildStop(Transform parent, ManualRouteDefinition def,
                              ManualStopDefinition stop, int index)
    {
        Vector2 position  = def.waypoints[stop.waypointIndex];
        Vector2 direction = RouteDirectionAtWaypoint(def.waypoints, stop.waypointIndex);

        // Logical trigger zone — stays on the flat plane so it collides with the
        // logical jeepney body exactly as before.
        var go = new GameObject($"Stop_{index}_{stop.stopName}");
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);

        var collider = go.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(def.roadHalfWidth * 2f + 5f, 7f);

        var zone = go.AddComponent<StopZone>();
        zone.StopIndex     = index;
        zone.StopName      = stop.stopName;
        zone.IsDestination = stop.isDestination;

        // Sign visual at the roadside, projected (parented to the unrotated
        // worldRoot so the billboard sprite stays upright).
        Vector2 signLogical = go.transform.TransformPoint(new Vector3(def.roadHalfWidth + 1.1f, 0f, 0f));
        var sign = new GameObject("Sign");
        sign.transform.SetParent(parent, false);
        sign.transform.position = IsoProjection.Project(signLogical);
        var signSr = sign.AddComponent<SpriteRenderer>();
        signSr.sprite = Resources.Load<Sprite>("Placeholders/stop_sign");
        signSr.sortingOrder = IsoProjection.SortOrder(signLogical) + 2;
        if (stop.isDestination)
            signSr.color = new Color(0.45f, 1f, 0.5f);

        // Waiting peeps line up past the sign, away from the road.
        if (stop.waitingPassengers > 0)
            zone.SpawnWaitingPeeps(stop.waitingPassengers,
                                   new Vector2(def.roadHalfWidth + 2.1f, -0.8f),
                                   Vector2.right);

        return zone;
    }

    static Vector2 RouteDirectionAtWaypoint(Vector2[] points, int index)
    {
        if (points.Length < 2) return Vector2.up;
        if (index >= points.Length - 1)
            return (points[points.Length - 1] - points[points.Length - 2]).normalized;
        return (points[index + 1] - points[index]).normalized;
    }
}
