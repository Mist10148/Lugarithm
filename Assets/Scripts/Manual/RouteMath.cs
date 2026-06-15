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
}
