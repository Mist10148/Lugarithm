using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dresses the procedural jeepney street: lines trunk road segments with continuous
/// Filipino-heritage building frontage on every clear side, and sprinkles ambient
/// "townsfolk" on the sidewalk so the street feels lived-in. Purely visual — buildings
/// and folk have no colliders and never board (they are NOT passengers).
///
/// Every position is gated by <see cref="RouteMath.NearestDistanceToGraph"/> so nothing
/// lands on a road (even where the streamed Manhattan grid folds close) and stays clear
/// of the stop signs/peeps. Spawned per-chunk by <see cref="RouteVisualBuilder"/> and
/// deterministic per road segment, so a given town always dresses the same way.
/// </summary>
public static class RoadsideDecorator
{
    // Heritage mix — repeats weight the distribution (chapel & nipa rarer).
    static readonly string[] Kinds =
    {
        "bldg_sari_sari",  "bldg_sari_sari",
        "bldg_carinderia", "bldg_carinderia",
        "bldg_bahay_bato", "bldg_bahay_bato",
        "bldg_nipa",       "bldg_chapel",
    };

    const float FrontGap     = 2.4f;   // sidewalk between road edge and building front
    const float BuildingGap  = 7.0f;   // grass gap between buildings (sparse frontage — perf)
    const float BuildingChance = 0.5f; // fraction of clear slots that actually get a building
    const float EndPad       = 2.0f;   // keep clear of segment ends / corners
    const float StopPad      = 3.5f;   // extra clearance around stop signs / waiting peeps
    const float FolkOffset   = 1.8f;   // sidewalk distance for ambient people
    const float NearbyRadius = 16f;    // clearance checks only look at nearby roads
    const float RoadMargin   = 1.0f;   // a point is "off-road" past roadHalfWidth + this

    const int BuildingSorting = -10;   // above road (-50), below peeps (5) / jeepney (10)
    const int FolkSorting     = 5;

    static readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

    /// <summary>
    /// Decorates the trunk segments in <paramref name="newSegments"/>. Clearance is
    /// tested against <paramref name="allSegments"/> (the full graph) so buildings never
    /// cover a crossing or parallel road, and against <paramref name="stopPositions"/> so
    /// they never bury a stop. Containers ("Scenery", "StreetLife") are created lazily
    /// under <paramref name="parent"/>.
    /// </summary>
    public static void DecorateSegments(Transform parent,
        IReadOnlyList<RoadSegment> newSegments, IReadOnlyList<RoadSegment> allSegments,
        IReadOnlyList<Vector2> stopPositions, float roadHalfWidth, int seed,
        Transform chunkRoot = null)
    {
        if (parent == null || newSegments == null) return;

        Transform visualParent = chunkRoot != null ? chunkRoot : parent;
        Transform scenery    = FindOrCreate(visualParent, "Scenery");
        Transform streetLife = FindOrCreate(visualParent, "StreetLife");

        foreach (RoadSegment seg in newSegments)
        {
            if (!seg.isTrunk) continue;                   // line the main street only

            Vector2 a = seg.a, b = seg.b;
            Vector2 delta = b - a;
            float len = delta.magnitude;
            if (len < 6f) continue;
            Vector2 dir  = delta / len;
            Vector2 perp = new Vector2(-dir.y, dir.x);

            List<RoadSegment> nearby = CollectNearby(allSegments, a, b, NearbyRadius);

            for (int sideSign = -1; sideSign <= 1; sideSign += 2)
            {
                Vector2 sideNormal = perp * sideSign;
                Vector2 toRoad     = -sideNormal;
                float   angle      = Vector2.SignedAngle(Vector2.down, toRoad);

                var rng = new System.Random(HashSeed(seed, a, b, sideSign));

                float d = EndPad;
                while (d < len - EndPad)
                {
                    string kind   = Kinds[rng.Next(Kinds.Length)];
                    Sprite sprite = Load(kind);
                    if (sprite == null) break;            // art not generated — bail this side

                    Vector2 size = sprite.bounds.size;    // x = width along road, y = depth
                    float halfW = size.x * 0.5f;
                    float halfD = size.y * 0.5f;

                    float centerAlong = d + halfW;
                    if (centerAlong + halfW > len - EndPad) break;

                    Vector2 alongPt  = a + dir * centerAlong;
                    Vector2 center   = alongPt + sideNormal * (roadHalfWidth + FrontGap + halfD);

                    // Sample the whole rotated footprint, not just the frontage: a deep
                    // building can sit clear at its front edge yet hang a back corner onto
                    // a road the streamed Manhattan town has folded close behind it.
                    bool clear = BuildingFootprintClear(nearby, center, dir, sideNormal,
                                                        halfW, halfD, roadHalfWidth);
                    // Measure against the centerline point so the stop's clear window
                    // (where its sign + peeps sit) is kept open on BOTH sides of the road.
                    bool nearStop = NearAnyStop(stopPositions, alongPt, halfW + StopPad);

                    if (clear && !nearStop)
                    {
                        // Only dress some clear slots so the street stays sparse (far fewer
                        // GameObjects per chunk → no streaming hitch), the rest stay grass.
                        if (rng.NextDouble() < BuildingChance)
                        {
                            SpawnBuilding(scenery, sprite, kind, center, angle);
                            MaybeSpawnFolk(streetLife, rng, alongPt, sideNormal, dir, halfW,
                                           roadHalfWidth, nearby, stopPositions);
                        }
                        d += size.x + BuildingGap;
                    }
                    else
                    {
                        d += 2f;                          // blocked slot — leave a gap, try ahead
                    }
                }
            }
        }
    }

    // -------------------------------------------------------------------------

    static void SpawnBuilding(Transform parent, Sprite sprite, string name, Vector2 pos, float angle)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = (Vector3)pos;
        go.transform.rotation = Quaternion.Euler(0f, 0f, angle);   // front faces the road

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = BuildingSorting;
    }

    static void MaybeSpawnFolk(Transform parent, System.Random rng, Vector2 alongPt,
                               Vector2 sideNormal, Vector2 dir, float halfW, float roadHalfWidth,
                               List<RoadSegment> nearby, IReadOnlyList<Vector2> stops)
    {
        if (rng.NextDouble() > 0.15) return;              // ~15% of frontages have folk (perf)
        Sprite folkSprite = Load("townsfolk");
        if (folkSprite == null) return;

        int n = 1;                                        // a single ambient person
        for (int i = 0; i < n; i++)
        {
            float jitter = (float)(rng.NextDouble() * 2.0 - 1.0) * halfW * 0.6f;
            Vector2 pos = alongPt + dir * jitter + sideNormal * (roadHalfWidth + FolkOffset);
            if (!RoadClear(nearby, pos, roadHalfWidth)) continue;
            if (NearAnyStop(stops, pos, StopPad)) continue;

            var folk = new GameObject("Townsfolk");
            folk.transform.SetParent(parent, false);
            folk.transform.position = (Vector3)pos;

            var sr = folk.AddComponent<SpriteRenderer>();
            sr.sprite = folkSprite;
            sr.sortingOrder = FolkSorting;
            sr.color = AmbientColor(rng);                 // muted, unlike the saturated boardable peeps
        }
    }

    static bool RoadClear(List<RoadSegment> segments, Vector2 p, float roadHalfWidth)
        => RouteMath.NearestDistanceToGraph(segments, p) > roadHalfWidth + RoadMargin;

    /// <summary>True only if every point of the building's rotated footprint (a 3×3
    /// lattice across width × depth) clears the road graph — corners included.</summary>
    static bool BuildingFootprintClear(List<RoadSegment> segments, Vector2 center,
                                       Vector2 dir, Vector2 sideNormal,
                                       float halfW, float halfD, float roadHalfWidth)
    {
        Vector2[] alongSamples = { -dir * halfW, Vector2.zero, dir * halfW };
        Vector2[] depthSamples = { -sideNormal * halfD, Vector2.zero, sideNormal * halfD };
        foreach (Vector2 al in alongSamples)
            foreach (Vector2 dp in depthSamples)
                if (!RoadClear(segments, center + al + dp, roadHalfWidth))
                    return false;
        return true;
    }

    static bool NearAnyStop(IReadOnlyList<Vector2> stops, Vector2 p, float radius)
    {
        if (stops == null) return false;
        float r2 = radius * radius;
        foreach (Vector2 s in stops)
            if ((s - p).sqrMagnitude < r2) return true;
        return false;
    }

    /// <summary>Coarse spatial filter: only roads whose midpoint/endpoints are within a
    /// gate of this segment can matter for its roadside clearance, so per-slot checks
    /// stay cheap as the streamed graph grows unbounded.</summary>
    static List<RoadSegment> CollectNearby(IReadOnlyList<RoadSegment> all, Vector2 a, Vector2 b, float radius)
    {
        var list = new List<RoadSegment>();
        if (all == null) return list;

        Vector2 mid = (a + b) * 0.5f;
        float gate = Vector2.Distance(a, b) + radius;
        float gate2 = gate * gate;
        foreach (RoadSegment s in all)
        {
            if (((s.a + s.b) * 0.5f - mid).sqrMagnitude <= gate2 ||
                (s.a - mid).sqrMagnitude <= gate2 ||
                (s.b - mid).sqrMagnitude <= gate2)
                list.Add(s);
        }
        return list;
    }

    static Sprite Load(string name)
    {
        if (_spriteCache.TryGetValue(name, out Sprite s)) return s;
        s = Resources.Load<Sprite>("Placeholders/" + name);
        _spriteCache[name] = s;
        return s;
    }

    static Transform FindOrCreate(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        if (t != null) return t;
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static Color AmbientColor(System.Random rng)
    {
        // Muted clothing tones — deliberately lower saturation/value than
        // StopZone.PeepColor so ambient folk never read as boardable passengers.
        float hue = (float)rng.NextDouble();
        return Color.HSVToRGB(hue, 0.30f, 0.72f);
    }

    static int HashSeed(int seed, Vector2 a, Vector2 b, int side)
    {
        unchecked
        {
            int h = seed;
            h = (h * 397) ^ Mathf.RoundToInt(a.x * 16f);
            h = (h * 397) ^ Mathf.RoundToInt(a.y * 16f);
            h = (h * 397) ^ Mathf.RoundToInt(b.x * 16f);
            h = (h * 397) ^ Mathf.RoundToInt(b.y * 16f);
            h = (h * 397) ^ side;
            return h & 0x7fffffff;
        }
    }
}
