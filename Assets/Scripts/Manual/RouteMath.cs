using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure polyline helpers for the manual drive route: arc lengths, sampling,
/// and nearest-point projection (used for off-road detection and the
/// breakdown trigger point).
/// </summary>
public static class RouteMath
{
    public static float TotalLength(Vector2[] points)
    {
        float length = 0f;
        for (int i = 1; i < points.Length; i++)
            length += Vector2.Distance(points[i - 1], points[i]);
        return length;
    }

    /// <summary>Point on the polyline at an arc-length distance from the start.</summary>
    public static Vector2 PointAt(Vector2[] points, float distance)
    {
        if (points.Length == 0) return Vector2.zero;
        if (distance <= 0f) return points[0];

        for (int i = 1; i < points.Length; i++)
        {
            float segment = Vector2.Distance(points[i - 1], points[i]);
            if (distance <= segment && segment > 0f)
                return Vector2.Lerp(points[i - 1], points[i], distance / segment);
            distance -= segment;
        }

        return points[points.Length - 1];
    }

    /// <summary>Direction of travel at an arc-length distance from the start.</summary>
    public static Vector2 DirectionAt(Vector2[] points, float distance)
    {
        if (points.Length < 2) return Vector2.up;

        for (int i = 1; i < points.Length; i++)
        {
            float segment = Vector2.Distance(points[i - 1], points[i]);
            if (distance <= segment)
                return (points[i] - points[i - 1]).normalized;
            distance -= segment;
        }

        return (points[points.Length - 1] - points[points.Length - 2]).normalized;
    }

    /// <summary>
    /// Projects a world position onto the route. Returns the arc-length along
    /// the route of the closest point; <paramref name="distanceFromRoute"/> is
    /// how far off the centerline the position sits.
    /// </summary>
    public static float NearestDistanceAlong(Vector2[] points, Vector2 position,
                                             out float distanceFromRoute)
    {
        float bestAlong = 0f;
        float bestSqr   = float.MaxValue;
        float walked    = 0f;

        for (int i = 1; i < points.Length; i++)
        {
            Vector2 a = points[i - 1];
            Vector2 b = points[i];
            Vector2 ab = b - a;
            float segment = ab.magnitude;
            if (segment <= 0.0001f) continue;

            float t = Mathf.Clamp01(Vector2.Dot(position - a, ab) / (segment * segment));
            Vector2 closest = a + ab * t;
            float sqr = (position - closest).sqrMagnitude;

            if (sqr < bestSqr)
            {
                bestSqr   = sqr;
                bestAlong = walked + segment * t;
            }

            walked += segment;
        }

        distanceFromRoute = Mathf.Sqrt(bestSqr);
        return bestAlong;
    }

    /// <summary>
    /// Arc-length distance from <paramref name="distance"/> to the nearest real
    /// corner — an interior vertex where the polyline actually changes direction.
    /// Collinear interior vertices (a straight road split into segments by the
    /// streaming generator) are ignored, so only genuine 90° turns count.
    /// Returns <see cref="float.MaxValue"/> when there is no corner.
    /// </summary>
    public static float DistanceToNearestCorner(Vector2[] points, float distance)
    {
        if (points == null || points.Length < 3) return float.MaxValue;

        float best   = float.MaxValue;
        float walked  = 0f;
        for (int i = 1; i < points.Length - 1; i++)
        {
            walked += Vector2.Distance(points[i - 1], points[i]);   // arc-length at vertex i

            Vector2 inDir  = points[i]     - points[i - 1];
            Vector2 outDir = points[i + 1] - points[i];
            if (inDir.sqrMagnitude < 1e-6f || outDir.sqrMagnitude < 1e-6f) continue;
            if (Vector2.Dot(inDir.normalized, outDir.normalized) > 0.99f) continue;  // ~straight

            best = Mathf.Min(best, Mathf.Abs(distance - walked));
        }
        return best;
    }

    /// <summary>
    /// Shortest distance from a world position to any road segment in the graph
    /// (trunk + branches). Off-road detection for the procedural town uses this
    /// instead of <see cref="NearestDistanceAlong"/> so a brief detour onto a
    /// branch stub isn't counted as leaving the road.
    /// </summary>
    public static float NearestDistanceToGraph(IReadOnlyList<RoadSegment> segments, Vector2 position)
    {
        if (segments == null || segments.Count == 0) return float.MaxValue;

        float bestSqr = float.MaxValue;
        foreach (RoadSegment s in segments)
        {
            Vector2 ab = s.b - s.a;
            float lenSqr = ab.sqrMagnitude;
            float t = lenSqr <= 0.0001f ? 0f : Mathf.Clamp01(Vector2.Dot(position - s.a, ab) / lenSqr);
            Vector2 closest = s.a + ab * t;
            float sqr = (position - closest).sqrMagnitude;
            if (sqr < bestSqr) bestSqr = sqr;
        }

        return Mathf.Sqrt(bestSqr);
    }

    /// <summary>
    /// Picks the outward roadside direction that keeps a probe point furthest from
    /// EVERY road in the graph, so a stop's sign and waiting peeps land in genuinely
    /// clear space — even where the streamed Manhattan trunk folds back near itself
    /// or a parallel/crossing road runs close (the cause of signs drifting onto the
    /// road as generation continues). Evaluates the 8 compass directions and probes
    /// at <paramref name="roadHalfWidth"/> + the peep reach. Near-ties fall back to
    /// <paramref name="preferred"/> (the incident-leg normal) so stops on an open
    /// straight road keep the single consistent side they pick today.
    /// </summary>
    public static Vector2 ClearestRoadside(Vector2 pos, IReadOnlyList<RoadSegment> segments,
                                           float roadHalfWidth, Vector2 preferred)
    {
        preferred = preferred.sqrMagnitude < 1e-6f ? Vector2.down : preferred.normalized;
        if (segments == null || segments.Count == 0) return preferred;

        float reach = roadHalfWidth + 2.6f;   // sign sits at +1.1, peeps out to ~+2.1
        const float eps = 0.05f;

        // Seed with the preferred direction at perfect alignment, so any compass
        // candidate must be STRICTLY clearer (not merely tied) to displace it.
        Vector2 best      = preferred;
        float   bestClear = NearestDistanceToGraph(segments, pos + preferred * reach);
        float   bestAlign = 1f;

        for (int k = 0; k < 8; k++)
        {
            float ang = k * 45f * Mathf.Deg2Rad;
            Vector2 cand = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            float clear = NearestDistanceToGraph(segments, pos + cand * reach);
            float align = Vector2.Dot(cand, preferred);

            bool better = clear > bestClear + eps ||
                          (clear > bestClear - eps && align > bestAlign);
            if (better) { best = cand; bestClear = clear; bestAlign = align; }
        }

        return best;
    }
}
