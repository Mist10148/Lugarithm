using NUnit.Framework;
using UnityEngine;

/// <summary>
/// RouteCursor must agree with the legacy RouteMath walkers (it replaces them on
/// hot per-frame paths), and the smoothed-tangent lateral basis must stay
/// continuous through 90° corners so lane offsets never collapse to or cross
/// the centerline mid-turn.
/// </summary>
public class RouteCursorTests
{
    static Vector2[] LPolyline() => new[]
    {
        Vector2.zero,
        new Vector2(40f, 0f),
        new Vector2(40f, 40f),
        new Vector2(80f, 40f),
    };

    [Test]
    public void PointAt_DirectionAt_TotalLength_AgreeWithRouteMath()
    {
        Vector2[] line = LPolyline();
        var cursor = new RouteCursor(line);

        Assert.AreEqual(RouteMath.TotalLength(line), cursor.TotalLength, 0.001f);

        for (float along = -2f; along <= cursor.TotalLength + 2f; along += 1.7f)
        {
            Vector2 expected = RouteMath.PointAt(line, along);
            Vector2 actual   = cursor.PointAt(along);
            Assert.Less((expected - actual).magnitude, 0.001f, $"PointAt disagrees at along={along}");

            Vector2 expectedDir = RouteMath.DirectionAt(line, along);
            Vector2 actualDir   = cursor.DirectionAt(along);
            Assert.Greater(Vector2.Dot(expectedDir, actualDir), 0.999f,
                $"DirectionAt disagrees at along={along}");
        }
    }

    [Test]
    public void Project_TracksMovingTarget_LikeNearestDistanceAlong()
    {
        Vector2[] line = LPolyline();
        var cursor = new RouteCursor(line);

        // Sweep a target along the whole route with a small lateral wobble —
        // the hinted projection must match the exact full scan at every step.
        for (float along = 0.5f; along < cursor.TotalLength; along += 0.8f)
        {
            Vector2 center = RouteMath.PointAt(line, along);
            Vector2 dir = RouteMath.DirectionAt(line, along);
            Vector2 left = new Vector2(-dir.y, dir.x);
            Vector2 pos = center + left * Mathf.Sin(along) * 1.2f;

            float expected = RouteMath.NearestDistanceAlong(line, pos, out float expectedOff);
            float actual   = cursor.Project(pos, out float actualOff);

            Assert.AreEqual(expected, actual, 0.01f, $"Project along disagrees at along={along}");
            Assert.AreEqual(expectedOff, actualOff, 0.01f, $"Project lateral disagrees at along={along}");
        }
    }

    [Test]
    public void Project_RecoversFromTeleport_ViaFullScanFallback()
    {
        // A long line (30 segments) so the teleport target sits far outside the
        // hinted scan window and forces the exact full-scan fallback.
        var points = new Vector2[31];
        for (int i = 0; i < points.Length; i++)
            points[i] = new Vector2(i * 10f, 0f);
        var cursor = new RouteCursor(points);

        cursor.Project(new Vector2(3f, 0f), out _);                        // warm the hint at the start
        float along = cursor.Project(new Vector2(250f, 0.5f), out float off);

        Assert.AreEqual(250f, along, 0.01f,
            "a teleport past the hint window must fall back to the exact scan");
        Assert.AreEqual(0.5f, off, 0.01f);
    }

    [Test]
    public void Covers_IsTrueOnlyForTheSamePolylineInstance()
    {
        Vector2[] line = LPolyline();
        var cursor = new RouteCursor(line);

        Assert.IsTrue(cursor.Covers(line));
        Assert.IsFalse(cursor.Covers(LPolyline()),
            "streaming replaces the waypoint array — a cursor over the old array must report stale");
    }

    [Test]
    public void RouteContextCursor_RebuildsWhenStreamingReplacesWaypoints()
    {
        var ctx = new RouteContext { Waypoints = LPolyline() };
        RouteCursor first = ctx.Cursor;
        Assert.IsNotNull(first);
        Assert.AreSame(first, ctx.Cursor, "unchanged waypoints must reuse the cached cursor");

        ctx.Waypoints = new[] { Vector2.zero, new Vector2(40f, 0f), new Vector2(40f, 40f),
                                new Vector2(80f, 40f), new Vector2(80f, 80f) };
        RouteCursor second = ctx.Cursor;
        Assert.AreNotSame(first, second, "a replaced waypoint array must rebuild the cursor");
        Assert.AreEqual(160f, second.TotalLength, 0.001f);
    }

    [Test]
    public void SmoothedLeft_IsContinuousThroughACorner_AndOffsetPointKeepsItsSide()
    {
        Vector2[] line = LPolyline();
        var cursor = new RouteCursor(line);
        const float offset = 3f;                    // scene-art lane center
        const float half = offset * 1.2f;           // > offset·π/4 forward-motion bound

        Vector2 prevLeft = cursor.SmoothedLeft(35f - 0.25f, half);
        Vector2 prevPoint = cursor.PointAt(35f - 0.25f) + prevLeft * offset;

        // Walk the corner at along=40 (35 → 45). The basis must rotate smoothly
        // (no flips) and the offset lane point must advance monotonically without
        // ever landing on the far side of the centerline.
        for (float along = 35f; along <= 45f; along += 0.25f)
        {
            Vector2 left = cursor.SmoothedLeft(along, half);
            Assert.Greater(Vector2.Dot(left, prevLeft), 0.9f,
                $"lateral basis must rotate continuously through the corner (along={along})");

            Vector2 point = cursor.PointAt(along) + left * offset;
            Assert.Less((point - prevPoint).magnitude, 1.0f,
                $"offset lane path must be continuous (along={along})");
            Assert.Greater(Vector2.Dot(point - prevPoint, cursor.DirectionAt(along)), -0.001f,
                $"offset lane point must keep moving forward through the corner (along={along})");

            // Same-side check: the offset point stays a real distance left of the
            // centerline — it never collapses onto or crosses to the other lane.
            cursor.Project(point, out float lateral);
            Assert.Greater(lateral, offset * 0.4f,
                $"offset lane point must stay on its own side of the road (along={along})");

            prevLeft = left;
            prevPoint = point;
        }
    }

    [Test]
    public void SmoothedLeft_CursorAgreesWithRouteMath()
    {
        Vector2[] line = LPolyline();
        var cursor = new RouteCursor(line);

        for (float along = 1f; along < 119f; along += 3.3f)
        {
            Vector2 fromCursor = cursor.SmoothedLeft(along, 3.6f);
            Vector2 fromMath   = RouteMath.SmoothedLeft(line, along, 3.6f);
            Assert.Greater(Vector2.Dot(fromCursor, fromMath), 0.999f,
                $"cursor and RouteMath smoothed bases disagree at along={along}");
        }
    }

    [Test]
    public void SmoothedLeft_OnAStraight_MatchesThePerpendicular()
    {
        var cursor = new RouteCursor(new[] { Vector2.zero, new Vector2(50f, 0f) });
        Vector2 left = cursor.SmoothedLeft(25f, 3.6f);
        Assert.AreEqual(0f, left.x, 0.001f);
        Assert.AreEqual(1f, left.y, 0.001f);
    }
}
