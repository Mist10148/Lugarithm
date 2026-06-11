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
        Vector2 side      = new Vector2(direction.y, -direction.x); // right of travel

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

        // Sign at the roadside (green-tinted for the destination).
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
