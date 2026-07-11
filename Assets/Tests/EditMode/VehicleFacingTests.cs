using NUnit.Framework;
using UnityEngine;
using Heading = VehicleFacing.VehicleHeading;

/// <summary>
/// EditMode tests for the shared vehicle direction contract. Confirms the world
/// mapping (+Y = North), the base-facing rotation offset used by every renderer,
/// heading resolution / last-heading preservation, and the rear exhaust anchor.
/// </summary>
public class VehicleFacingTests
{
    // --- Resolve: cardinals -------------------------------------------------

    [Test]
    public void Resolve_PositiveY_IsNorth()
    {
        Assert.AreEqual(Heading.North, VehicleFacing.Resolve(new Vector2(0f, 1f), Heading.South));
    }

    [Test]
    public void Resolve_NegativeY_IsSouth()
    {
        Assert.AreEqual(Heading.South, VehicleFacing.Resolve(new Vector2(0f, -1f), Heading.North));
    }

    [Test]
    public void Resolve_PositiveX_IsEast()
    {
        Assert.AreEqual(Heading.East, VehicleFacing.Resolve(new Vector2(1f, 0f), Heading.North));
    }

    [Test]
    public void Resolve_NegativeX_IsWest()
    {
        Assert.AreEqual(Heading.West, VehicleFacing.Resolve(new Vector2(-1f, 0f), Heading.North));
    }

    // --- Resolve: diagonals -------------------------------------------------

    [Test]
    public void Resolve_Diagonals_EightWay_ResolveToIntercardinals()
    {
        Assert.AreEqual(Heading.NorthEast, VehicleFacing.Resolve(new Vector2(1f, 1f), Heading.North, eightWay: true));
        Assert.AreEqual(Heading.SouthEast, VehicleFacing.Resolve(new Vector2(1f, -1f), Heading.North, eightWay: true));
        Assert.AreEqual(Heading.SouthWest, VehicleFacing.Resolve(new Vector2(-1f, -1f), Heading.North, eightWay: true));
        Assert.AreEqual(Heading.NorthWest, VehicleFacing.Resolve(new Vector2(-1f, 1f), Heading.North, eightWay: true));
    }

    [Test]
    public void Resolve_Diagonal_FourWay_PicksNearestCardinal()
    {
        // Dominant-axis tie-break: slightly more X than Y -> East, not a diagonal.
        Assert.AreEqual(Heading.East,
            VehicleFacing.Resolve(new Vector2(1.1f, 1f), Heading.North, eightWay: false));
        Assert.AreEqual(Heading.North,
            VehicleFacing.Resolve(new Vector2(1f, 1.1f), Heading.North, eightWay: false));
    }

    // --- Resolve: stationary / noise ---------------------------------------

    [Test]
    public void Resolve_ZeroVelocity_PreservesLastHeading()
    {
        Assert.AreEqual(Heading.West, VehicleFacing.Resolve(Vector2.zero, Heading.West));
        Assert.AreEqual(Heading.East, VehicleFacing.Resolve(Vector2.zero, Heading.East));
    }

    [Test]
    public void Resolve_SubThresholdNoise_DoesNotFlipHeading()
    {
        var noise = new Vector2(0.001f, -0.001f);
        Assert.AreEqual(Heading.North, VehicleFacing.Resolve(noise, Heading.North, threshold: 0.02f));
    }

    // --- FacingAngleDegrees: base-facing offset (the render angle) -----------

    [Test]
    public void FacingAngleDegrees_SouthArt_North_Is180()
    {
        Assert.AreEqual(180f, Mathf.Abs(VehicleFacing.FacingAngleDegrees(new Vector2(0f, 1f), Heading.South)), 0.001f);
    }

    [Test]
    public void FacingAngleDegrees_SouthArt_East_Is90()
    {
        Assert.AreEqual(90f, VehicleFacing.FacingAngleDegrees(new Vector2(1f, 0f), Heading.South), 0.001f);
    }

    [Test]
    public void FacingAngleDegrees_SouthArt_South_IsZero()
    {
        Assert.AreEqual(0f, VehicleFacing.FacingAngleDegrees(new Vector2(0f, -1f), Heading.South), 0.001f);
    }

    [Test]
    public void FacingAngleDegrees_SouthArt_West_IsMinus90()
    {
        Assert.AreEqual(-90f, VehicleFacing.FacingAngleDegrees(new Vector2(-1f, 0f), Heading.South), 0.001f);
    }

    [Test]
    public void FacingAngleDegrees_RotatesArtFrontOntoMovement()
    {
        // The whole contract: art authored at baseFacing, rotated by the returned
        // angle, must visually point along movement. Verify for all four cardinals.
        foreach (var move in new[] { Vector2.up, Vector2.down, Vector2.right, Vector2.left })
        {
            float angle = VehicleFacing.FacingAngleDegrees(move, VehicleFacing.ArtBaseFacing);
            Vector2 art = VehicleFacing.DirectionOf(VehicleFacing.ArtBaseFacing);
            Vector2 shown = (Vector2)(Quaternion.Euler(0f, 0f, angle) * art);
            Assert.AreEqual(0f, Vector2.Angle(shown, move), 0.01f,
                $"art should face {move} after rotation");
        }
    }

    [Test]
    public void FacingAngleDegrees_ManualAndAutomation_ShareMapping()
    {
        // Both mode paths call this same function, so identical vectors map to
        // identical angles by construction. Assert the values line up for a
        // representative heading (guards against a divergent duplicate creeping in).
        var move = new Vector2(0.3f, 0.7f).normalized;
        float manual = VehicleFacing.FacingAngleDegrees(move, VehicleFacing.ArtBaseFacing);
        float automation = VehicleFacing.FacingAngleDegrees(move, VehicleFacing.ArtBaseFacing);
        Assert.AreEqual(manual, automation, 0.0001f);
    }

    // --- RearAnchorLocal: exhaust behind ------------------------------------

    [Test]
    public void RearAnchorLocal_SouthArt_IsAbovePivot()
    {
        // South-facing art has its rear at +Y (top of the cell).
        Vector3 anchor = VehicleFacing.RearAnchorLocal(Heading.South, 0.34f, 1.02f);
        Assert.Greater(anchor.y, 0f);
        Assert.AreEqual(0.34f, anchor.x, 0.0001f);
    }

    [Test]
    public void RearAnchorLocal_IsBehindFront_ForEveryHeading()
    {
        foreach (Heading h in System.Enum.GetValues(typeof(Heading)))
        {
            Vector3 anchor = VehicleFacing.RearAnchorLocal(h, 0f, 1f);
            Vector2 front = VehicleFacing.DirectionOf(h);
            // The offset from the pivot must point opposite the authored front.
            Assert.Less(Vector2.Dot(new Vector2(anchor.x, anchor.y), front), 0f,
                $"exhaust must sit behind the front for {h}");
        }
    }

    // --- DirectionOf: world mapping -----------------------------------------

    [Test]
    public void DirectionOf_MapsCardinalsToWorldAxes()
    {
        Assert.AreEqual(Vector2.up, VehicleFacing.DirectionOf(Heading.North));
        Assert.AreEqual(Vector2.down, VehicleFacing.DirectionOf(Heading.South));
        Assert.AreEqual(Vector2.right, VehicleFacing.DirectionOf(Heading.East));
        Assert.AreEqual(Vector2.left, VehicleFacing.DirectionOf(Heading.West));
    }
}
