using UnityEngine;

/// <summary>
/// Fast sampling over a route polyline. Precomputes cumulative arc lengths so
/// <see cref="PointAt"/>/<see cref="DirectionAt"/> are O(log n) binary searches
/// instead of RouteMath's O(n) walks, and <see cref="Project"/> keeps a segment
/// hint so projecting a continuously moving target is O(1) amortized. The
/// endless streamed road only ever grows its polyline, so these queries are the
/// hot per-frame cost that used to scale with distance driven.
/// </summary>
public class RouteCursor
{
    readonly Vector2[] _points;
    readonly float[]   _cum;    // cumulative arc length at each vertex
    int _hint = 1;              // segment index (1-based: points[i-1] → points[i])

    // Warm-pass window around the hint, in segments. The projected target moves
    // continuously, so the nearest segment is almost always within a couple of
    // segments of last frame's.
    const int   ProjectWindow   = 8;
    // Warm result farther off the road than this means a cold hint or a
    // teleport — fall back to the exact full scan.
    const float ProjectAcceptDistance = 12f;

    public RouteCursor(Vector2[] points)
    {
        _points = points ?? new Vector2[0];
        _cum = new float[Mathf.Max(1, _points.Length)];
        _cum[0] = 0f;
        for (int i = 1; i < _points.Length; i++)
            _cum[i] = _cum[i - 1] + Vector2.Distance(_points[i - 1], _points[i]);
    }

    public float TotalLength => _cum[_cum.Length - 1];

    /// <summary>True when this cursor was built over exactly this polyline
    /// instance — streaming replaces the array, which invalidates the cursor.</summary>
    public bool Covers(Vector2[] points) => ReferenceEquals(points, _points);

    /// <summary>Segment index (1-based) containing the arc position.</summary>
    int SegmentAt(float along)
    {
        int lo = 1, hi = _points.Length - 1;
        while (lo < hi)
        {
            int mid = (lo + hi) >> 1;
            if (_cum[mid] < along) lo = mid + 1;
            else hi = mid;
        }
        return lo;
    }

    public Vector2 PointAt(float along)
    {
        if (_points.Length == 0) return Vector2.zero;
        if (_points.Length == 1 || along <= 0f) return _points[0];
        if (along >= TotalLength) return _points[_points.Length - 1];

        int i = SegmentAt(along);
        float segment = _cum[i] - _cum[i - 1];
        return segment > 0f
            ? Vector2.Lerp(_points[i - 1], _points[i], (along - _cum[i - 1]) / segment)
            : _points[i];
    }

    public Vector2 DirectionAt(float along)
    {
        if (_points.Length < 2) return Vector2.up;

        int i = along >= TotalLength ? _points.Length - 1 : SegmentAt(along);
        Vector2 dir = _points[i] - _points[i - 1];
        return dir.sqrMagnitude > 1e-8f ? dir.normalized : Vector2.up;
    }

    /// <summary>Smoothed-tangent lateral basis — see <see cref="RouteMath.SmoothedLeft"/>.</summary>
    public Vector2 SmoothedLeft(float along, float halfLength)
    {
        Vector2 tangent = PointAt(along + halfLength) - PointAt(along - halfLength);
        if (tangent.sqrMagnitude < 1e-6f)
        {
            tangent = DirectionAt(along);
            if (tangent.sqrMagnitude < 1e-6f) tangent = Vector2.up;
        }
        tangent.Normalize();
        return new Vector2(-tangent.y, tangent.x);
    }

    /// <summary>
    /// Projects a world position onto the route (arc length of the closest
    /// point; <paramref name="distanceFromRoute"/> = lateral distance). Scans a
    /// small window around the previous result first and falls back to the full
    /// scan only when the warm result is implausibly far off the road. Where a
    /// streamed Manhattan road folds near itself this prefers the segment the
    /// target was already on — which is the desired progress behavior — instead
    /// of snapping to a fold like the global scan could.
    /// </summary>
    public float Project(Vector2 position, out float distanceFromRoute)
    {
        if (_points.Length < 2)
        {
            distanceFromRoute = float.MaxValue;
            return 0f;
        }

        float bestSqr = float.MaxValue, bestAlong = 0f;
        int bestSeg = _hint;

        int lo = Mathf.Max(1, _hint - ProjectWindow);
        int hi = Mathf.Min(_points.Length - 1, _hint + ProjectWindow);
        ScanRange(position, lo, hi, ref bestSqr, ref bestAlong, ref bestSeg);

        if (bestSqr > ProjectAcceptDistance * ProjectAcceptDistance)
            ScanRange(position, 1, _points.Length - 1, ref bestSqr, ref bestAlong, ref bestSeg);

        _hint = bestSeg;
        distanceFromRoute = Mathf.Sqrt(bestSqr);
        return bestAlong;
    }

    void ScanRange(Vector2 position, int from, int to,
                   ref float bestSqr, ref float bestAlong, ref int bestSeg)
    {
        for (int i = from; i <= to; i++)
        {
            Vector2 a = _points[i - 1];
            Vector2 ab = _points[i] - a;
            float segment = _cum[i] - _cum[i - 1];
            if (segment <= 0.0001f) continue;

            float t = Mathf.Clamp01(Vector2.Dot(position - a, ab) / (segment * segment));
            Vector2 closest = a + ab * t;
            float sqr = (position - closest).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr   = sqr;
                bestAlong = _cum[i - 1] + segment * t;
                bestSeg   = i;
            }
        }
    }
}
