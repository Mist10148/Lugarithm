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
            StopZone zone = BuildProceduralStop(parent, node, i, isDest, roadHalfWidth, layout.segments);
            ctx.Zones[i] = zone;
            ctx.ZoneByNode[node.id] = zone;
            if (isDest) ctx.DestinationZone = zone;
        }

        // Dress the street: continuous heritage frontage + ambient folk beside the road.
        var stopPositions = new List<Vector2>(layout.stops.Count);
        foreach (TownNode n in layout.stops) stopPositions.Add(n.pos);
        RoadsideDecorator.DecorateSegments(parent, layout.segments, layout.segments,
                                           stopPositions, roadHalfWidth, seed: 0);

        return ctx;
    }

    /// <summary>
    /// Adds a chunk's roads and stops to an existing top-down procedural world.
    /// The existing destination stop is demoted to an ordinary stop; the new
    /// destination becomes the terminal-end of the appended trunk.
    /// </summary>
    public static void AppendProcedural(Transform parent, RouteContext ctx,
                                        ManualLayoutResult delta, float roadHalfWidth,
                                        Transform chunkRoot = null)
    {
        if (ctx == null || delta == null) return;
        if (ctx.Segments == null) ctx.Segments = new List<RoadSegment>();

        Transform visualParent = chunkRoot != null ? chunkRoot : parent;
        Sprite roadSprite = Resources.Load<Sprite>("Placeholders/road_tile");
        Transform roadRoot = visualParent.Find("Road");
        if (roadRoot == null)
        {
            var rr = new GameObject("Road");
            rr.transform.SetParent(visualParent, false);
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
            if (ctx.ZoneByNode != null && ctx.ZoneByNode.TryGetValue(node.id, out StopZone existingZone))
            {
                // The previous terminal has just gained a new outgoing road.
                // Its original one-leg orientation may now point down that road,
                // so recompute its roadside against the complete graph before
                // passenger peeps are added by the drive controller.
                bool remainsDestination = node.id == delta.dest.id;
                RefreshProceduralStop(existingZone, node, remainsDestination, roadHalfWidth, ctx.Segments);
                continue;
            }

            bool isDest = node.id == delta.dest.id;
            StopZone zone = BuildProceduralStop(visualParent, node, zones.Count, isDest, roadHalfWidth, ctx.Segments);
            zones.Add(zone);
            if (ctx.ZoneByNode != null) ctx.ZoneByNode[node.id] = zone;
            if (isDest) ctx.DestinationZone = zone;
        }
        ctx.Zones = zones.ToArray();

        // Rebuild the drive line from the chunk's trunk polyline. ProjectChunk
        // carries the *full* cumulative trunk (TrunkPolyline), not just the new
        // vertices, so appending it would fold a second copy of the whole route
        // onto the tail — the jeep would reach the frontier and drive all the way
        // back to the start. Replacing keeps the shared prefix identical, so the
        // preserved arc-length still points to the same world spot.
        if (delta.trunk != null && delta.trunk.Length > 0)
        {
            ctx.Waypoints   = SanitizePolyline(delta.trunk);
            ctx.TotalLength = RouteMath.TotalLength(ctx.Waypoints);
        }

        // Dress only this chunk's new trunk, but clear it against the whole graph so
        // buildings never land on a road the streamed town has wound close by.
        var stopPositions = new List<Vector2>();
        if (ctx.ZoneByNode != null)
            foreach (StopZone z in ctx.ZoneByNode.Values)
                if (z != null) stopPositions.Add((Vector2)z.transform.position);
        RoadsideDecorator.DecorateSegments(parent, delta.segments, ctx.Segments,
                                           stopPositions, roadHalfWidth, seed: 0,
                                           chunkRoot: visualParent);
    }

    /// <summary>
    /// Collapses near-duplicate vertices and drops any that would create a
    /// reversing micro-segment, so a grid-snapped node can't make PointAt /
    /// DirectionAt step backward at a fold.
    /// </summary>
    static Vector2[] SanitizePolyline(Vector2[] pts)
    {
        var outp = new List<Vector2>(pts.Length);
        foreach (Vector2 p in pts)
        {
            if (outp.Count == 0) { outp.Add(p); continue; }

            Vector2 last = outp[outp.Count - 1];
            if ((p - last).sqrMagnitude < 1e-4f) continue;   // duplicate

            if (outp.Count >= 2)
            {
                Vector2 prevDir = (last - outp[outp.Count - 2]).normalized;
                Vector2 newDir  = (p - last).normalized;
                if (Vector2.Dot(prevDir, newDir) < -0.5f) continue;   // reversing spur
            }

            outp.Add(p);
        }
        return outp.ToArray();
    }

    static void TileSegment(Transform parent, Sprite sprite, Vector2 a, Vector2 b, float width)
    {
        Vector2 delta = b - a;
        float len = delta.magnitude;
        if (len < 0.0001f) return;
        Vector2 dir = delta / len;

        // Safety net: render tiles strictly along a cardinal so roads always read
        // as 90° Manhattan streets, even if a stray near-diagonal segment slips in.
        Vector2 tileDir = Mathf.Abs(dir.x) >= Mathf.Abs(dir.y)
            ? new Vector2(Mathf.Sign(dir.x), 0f)
            : new Vector2(0f, Mathf.Sign(dir.y));

        for (float d = 0f; d < len; d += 1f)
            PlaceRoadTile(parent, sprite, a + dir * d, tileDir, width);

        // One tile exactly at the far endpoint — a non-integer segment length
        // otherwise leaves a sub-unit gap right at the bend or chunk seam.
        PlaceRoadTile(parent, sprite, b, tileDir, width);

        // Square caps at both endpoints: an axis-aligned width×width square
        // centered on the shared vertex covers the outer-corner notch where two
        // legs meet at 90°, whatever the orientation of either leg. Overdraw on
        // straight joints is invisible (same sprite, same sorting order).
        PlaceRoadCap(parent, sprite, a, width);
        PlaceRoadCap(parent, sprite, b, width);
    }

    static void PlaceRoadTile(Transform parent, Sprite sprite, Vector2 pos, Vector2 tileDir, float width)
    {
        var tile = new GameObject("RoadTile");
        tile.transform.SetParent(parent, false);
        tile.transform.position = pos;
        tile.transform.rotation = Quaternion.FromToRotation(Vector3.up, (Vector3)tileDir);
        tile.transform.localScale = new Vector3(width, 1.25f, 1f);

        var sr = tile.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = -50;
    }

    static void PlaceRoadCap(Transform parent, Sprite sprite, Vector2 pos, float width)
    {
        var cap = new GameObject("RoadCap");
        cap.transform.SetParent(parent, false);
        cap.transform.position = pos;
        cap.transform.localScale = new Vector3(width, width, 1f);

        var sr = cap.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = -50;
    }

    static StopZone BuildProceduralStop(Transform parent, TownNode node, int ordinal,
                                        bool isDest, float roadHalfWidth, List<RoadSegment> segments)
    {
        var go = new GameObject($"Stop_{ordinal}_{node.name}");
        go.transform.SetParent(parent, false);
        go.transform.position = node.pos;

        // Orient the stop so its local +X points into the clearest open space
        // beside the road. At an L-corner the two legs are 90° apart, so a plain
        // perpendicular would aim straight down the other leg and drop the sign /
        // peeps onto the road; the open-quadrant outward normal keeps them off it
        // for any orientation. Sign, peeps, and label all hang off local +X.
        Vector2 outward = RoadsideOutwardFromSegments(node.pos, segments, roadHalfWidth);
        go.transform.rotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.right, outward));

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
        sign.transform.rotation = Quaternion.identity;   // upright, even at corner stops
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
        labelGo.transform.rotation = Quaternion.identity;   // keep the name upright

        return zone;
    }

    /// <summary>
    /// Reorients a procedural stop and refreshes the visuals that change when a
    /// streamed terminal becomes an ordinary stop. Child signs and peeps use
    /// local coordinates, so rotating the zone moves them together to the new
    /// roadside without disturbing the stop trigger itself.
    /// </summary>
    static void RefreshProceduralStop(StopZone zone, TownNode node, bool isDest,
                                      float roadHalfWidth, List<RoadSegment> segments)
    {
        if (zone == null || node == null) return;

        Vector2 outward = RoadsideOutwardFromSegments(node.pos, segments, roadHalfWidth);
        zone.transform.rotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.right, outward));
        zone.StopName = node.name;
        zone.IsDestination = isDest;

        Transform sign = zone.transform.Find("Sign");
        if (sign != null)
        {
            sign.localPosition = new Vector3(roadHalfWidth + 1.1f, 0f, 0f);
            sign.rotation = Quaternion.identity;

            SpriteRenderer signRenderer = sign.GetComponent<SpriteRenderer>();
            if (signRenderer != null)
            {
                signRenderer.color = Color.white;
                if (isDest)                                signRenderer.color = new Color(0.45f, 1f, 0.5f);
                else if (node.kind == NodeKind.HeritageSite) signRenderer.color = new Color(1f, 0.85f, 0.4f);
                else if (node.kind == NodeKind.NpcDrop)      signRenderer.color = new Color(0.6f, 0.8f, 1f);
            }
        }

        Transform label = zone.transform.Find("StopLabel");
        if (label != null)
        {
            TMPro.TextMeshPro text = label.GetComponent<TMPro.TextMeshPro>();
            if (text != null)
                text.text = isDest
                    ? $"<color=#7CFC72>{node.name}</color>\n<color=#FFD700>TERMINAL</color>"
                    : node.name;
            label.rotation = Quaternion.identity;
        }
    }

    /// <summary>
    /// Outward roadside normal at a node (procedural town): a unit vector pointing
    /// into the clearest space beside the road, away from every incident road leg.
    /// Incident roads are found by distance to each segment (robust to streamed
    /// nodes that drift off their exact endpoints). See <see cref="RoadsideOutward"/>.
    /// </summary>
    static Vector2 RoadsideOutwardFromSegments(Vector2 pos, List<RoadSegment> segments, float roadHalfWidth)
    {
        var legs = new List<Vector2>();
        Vector2 nearestDir = Vector2.right;
        float   nearestDistSqr = float.PositiveInfinity;

        if (segments != null)
        {
            // A stop is "on" a road segment when the node lies near that segment's
            // line (not just its endpoints). Streamed chunks snap nodes to the grid,
            // so endpoint-exact matching drifts and misses legs — measuring distance
            // to the whole segment is robust to that and also handles a stop sitting
            // partway along a straight trunk run.
            float nearTol = Mathf.Max(roadHalfWidth + 1f, 2f);
            float nearTolSqr = nearTol * nearTol;
            const float endFrac = 0.15f;   // how close to an endpoint counts as "the end"

            foreach (RoadSegment s in segments)
            {
                Vector2 ab = s.b - s.a;
                float abLenSqr = ab.sqrMagnitude;
                if (abLenSqr < 1e-4f) continue;

                float t = Vector2.Dot(pos - s.a, ab) / abLenSqr;
                Vector2 closest = s.a + ab * Mathf.Clamp01(t);
                float distSqr = (closest - pos).sqrMagnitude;

                Vector2 dir = ab / Mathf.Sqrt(abLenSqr);
                if (distSqr < nearestDistSqr) { nearestDistSqr = distSqr; nearestDir = dir; }

                if (distSqr > nearTolSqr) continue;   // node not on this road

                // Add the open-road direction(s) leaving the node: toward b when the
                // node is at/near a, toward a when near b, and BOTH when mid-segment.
                if (t > endFrac)        legs.Add(-dir);
                if (t < 1f - endFrac)   legs.Add(dir);
            }
        }

        // Fallback: no incident leg found (badly drifted node) — push to the side of
        // the nearest road rather than blindly downward onto it.
        if (legs.Count == 0)
            legs.Add(nearestDir);

        // The incident legs give a consistent "preferred" side; refine it against the
        // whole graph so the sign/peeps never land on a non-incident road that the
        // streamed town has wound close by.
        Vector2 preferred = RoadsideOutward(legs);
        return RouteMath.ClearestRoadside(pos, segments, roadHalfWidth, preferred);
    }

    /// <summary>
    /// Outward roadside normal at a waypoint on an authored polyline route, taken
    /// from the incident segments toward the neighbouring waypoints.
    /// </summary>
    static Vector2 RoadsideOutwardFromPolyline(Vector2[] points, int index)
    {
        var legs = new List<Vector2>();
        if (points != null && points.Length >= 2 && index >= 0 && index < points.Length)
        {
            Vector2 pos = points[index];
            if (index + 1 < points.Length)
            {
                Vector2 d = points[index + 1] - pos;
                if (d.sqrMagnitude > 1e-4f) legs.Add(d.normalized);
            }
            if (index - 1 >= 0)
            {
                Vector2 d = points[index - 1] - pos;
                if (d.sqrMagnitude > 1e-4f) legs.Add(d.normalized);
            }
        }
        return RoadsideOutward(legs);
    }

    /// <summary>
    /// Clearest outward normal given the incident road leg unit vectors (each
    /// pointing from the node along a road). With two-plus legs that leave an open
    /// side (an L-corner, T, or Y) the normal is the bisector of that open side;
    /// for a straight-through node, opposite legs, or a single branch tip it falls
    /// back to a perpendicular of the primary (first / trunk-preferred) leg.
    /// </summary>
    static Vector2 RoadsideOutward(List<Vector2> legs)
    {
        if (legs == null || legs.Count == 0) return Vector2.down;

        // Straight road, single leg, or dead-end stub: every leg lies on one axis, so
        // any "furthest" compass direction would point back down the road. A
        // perpendicular keeps the sign & peeps cleanly beside it. The axis is
        // canonicalised so every stop on the same road picks the SAME side.
        Vector2 primary = legs[0].normalized;
        bool colinear = true;
        foreach (Vector2 l in legs)
            if (Mathf.Abs(Vector2.Dot(l.normalized, primary)) < 0.95f) { colinear = false; break; }

        if (colinear)
        {
            if (primary.x < -1e-4f || (Mathf.Abs(primary.x) < 1e-4f && primary.y < 0f))
                primary = -primary;
            return new Vector2(primary.y, -primary.x);   // rotate -90°, beside the road
        }

        // Corner / T / Y: pick, from 8 compass directions, the one pointing furthest
        // from every connected leg — the open quadrant — so the sign/peeps land in
        // clear space and never down another leg.
        Vector2 best = Vector2.down;
        float   bestClearance = float.NegativeInfinity;
        for (int k = 0; k < 8; k++)
        {
            float ang = k * 45f * Mathf.Deg2Rad;
            Vector2 cand = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));

            float maxDot = -1f;   // alignment with the closest leg (1 = straight down it)
            foreach (Vector2 l in legs)
                maxDot = Mathf.Max(maxDot, Vector2.Dot(cand, l.normalized));

            float clearance = -maxDot;   // higher = further from every road
            if (clearance > bestClearance) { bestClearance = clearance; best = cand; }
        }
        return best;
    }

    // -------------------------------------------------------------------------

    static void BuildRoad(Transform parent, ManualRouteDefinition def, float totalLength)
    {
        Sprite roadSprite = Resources.Load<Sprite>("Placeholders/road_tile");
        var roadRoot = new GameObject("Road");
        roadRoot.transform.SetParent(parent, false);

        float width = def.roadHalfWidth * 2f;
        const float step = 1.0f;
        for (float d = 0f; d < totalLength; d += step)
        {
            Vector2 point     = RouteMath.PointAt(def.waypoints, d);
            Vector2 direction = RouteMath.DirectionAt(def.waypoints, d);
            PlaceRoadTile(roadRoot.transform, roadSprite, point, direction, width);
        }
        // Exact end tile (non-integer totalLength) plus square caps at every
        // bend vertex so 90° corners have no notch.
        PlaceRoadTile(roadRoot.transform, roadSprite,
                      RouteMath.PointAt(def.waypoints, totalLength),
                      RouteMath.DirectionAt(def.waypoints, totalLength), width);
        for (int i = 1; i < def.waypoints.Length - 1; i++)
            PlaceRoadCap(roadRoot.transform, roadSprite, def.waypoints[i], width);
    }

    static StopZone BuildStop(Transform parent, ManualRouteDefinition def,
                              ManualStopDefinition stop, int index)
    {
        Vector2 position = def.waypoints[stop.waypointIndex];

        var go = new GameObject($"Stop_{index}_{stop.stopName}");
        go.transform.SetParent(parent, false);
        go.transform.position = position;

        var collider = go.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(def.roadHalfWidth * 2f + 5f, 7f);

        // Local +X faces the open roadside (see RoadsideOutward) so the sign and
        // peeps sit beside the road, not on it.
        Vector2 outward = RoadsideOutwardFromPolyline(def.waypoints, stop.waypointIndex);
        go.transform.rotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.right, outward));

        var zone = go.AddComponent<StopZone>();
        zone.StopIndex     = index;
        zone.StopName      = stop.stopName;
        zone.IsDestination = stop.isDestination;

        // Sign at the roadside (green-tinted for the destination / terminal).
        var sign = new GameObject("Sign");
        sign.transform.SetParent(go.transform, false);
        sign.transform.localPosition = new Vector3(def.roadHalfWidth + 1.1f, 0f, 0f);
        sign.transform.rotation = Quaternion.identity;   // upright, even at corner stops
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
}
