using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode tests for the polyline corner detection that keeps the lane-following
/// jeepney from cutting/reversing through 90° turns.
/// </summary>
public class RouteMathTests
{
    [Test]
    public void DistanceToNearestCorner_StraightLine_HasNoCorner()
    {
        // Collinear points (a straight road split into segments by streaming) are
        // not corners.
        var pts = new[] { new Vector2(0, 0), new Vector2(0, 5), new Vector2(0, 10) };
        Assert.AreEqual(float.MaxValue, RouteMath.DistanceToNearestCorner(pts, 4f));
    }

    [Test]
    public void DistanceToNearestCorner_LShape_MeasuresToTheBend()
    {
        // Corner is the bend at (0,10), arc-length 10 from the start.
        var pts = new[] { new Vector2(0, 0), new Vector2(0, 10), new Vector2(10, 10) };

        Assert.AreEqual(0f, RouteMath.DistanceToNearestCorner(pts, 10f), 0.001f);
        Assert.AreEqual(3f, RouteMath.DistanceToNearestCorner(pts, 7f),  0.001f);  // before
        Assert.AreEqual(3f, RouteMath.DistanceToNearestCorner(pts, 13f), 0.001f);  // after
    }

    [Test]
    public void DistanceToNearestCorner_IgnoresCollinearMidpoints()
    {
        // Straight vertex at arc 5, real corner at arc 10 — the straight one must
        // be ignored, so distance from arc 6 is to the corner (4), not the split (1).
        var pts = new[]
        {
            new Vector2(0, 0), new Vector2(0, 5), new Vector2(0, 10), new Vector2(10, 10),
        };
        Assert.AreEqual(4f, RouteMath.DistanceToNearestCorner(pts, 6f), 0.001f);
    }
}
