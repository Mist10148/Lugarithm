using UnityEngine;

/// <summary>
/// Single source of truth for the vehicle direction contract shared by the
/// player jeepney (manual + automation) and NPC traffic.
///
/// World convention: +Y = North/up, -Y = South/down, +X = East/right, -X = West/left.
///
/// The current vehicle sprite sheets (player_jeepney_sheet, filipino_traffic_sheet)
/// are all authored with the vehicle FRONT toward the bottom of the cell, i.e.
/// facing <see cref="VehicleHeading.South"/> at zero rotation (see <see cref="ArtBaseFacing"/>).
/// Renderers rotate the transform by <see cref="FacingAngleDegrees"/> so the art's
/// authored front points along the actual movement vector. This is presentation
/// only — it never changes physics, routing, or gameplay state.
/// </summary>
public static class VehicleFacing
{
    public enum VehicleHeading
    {
        North,
        NorthEast,
        East,
        SouthEast,
        South,
        SouthWest,
        West,
        NorthWest
    }

    /// <summary>
    /// Authored facing of every vehicle sprite currently in the project. Kept as
    /// one constant because all sheets share it; a single divergent slice would
    /// be handled with a per-index override at that renderer, not by changing this.
    /// </summary>
    public const VehicleHeading ArtBaseFacing = VehicleHeading.South;

    static readonly float Root2Inv = 1f / Mathf.Sqrt(2f);

    /// <summary>Unit vector a heading points toward in world space (+Y = North).</summary>
    public static Vector2 DirectionOf(VehicleHeading heading)
    {
        switch (heading)
        {
            case VehicleHeading.North:     return new Vector2(0f, 1f);
            case VehicleHeading.NorthEast: return new Vector2(Root2Inv, Root2Inv);
            case VehicleHeading.East:      return new Vector2(1f, 0f);
            case VehicleHeading.SouthEast: return new Vector2(Root2Inv, -Root2Inv);
            case VehicleHeading.South:     return new Vector2(0f, -1f);
            case VehicleHeading.SouthWest: return new Vector2(-Root2Inv, -Root2Inv);
            case VehicleHeading.West:      return new Vector2(-1f, 0f);
            case VehicleHeading.NorthWest: return new Vector2(-Root2Inv, Root2Inv);
            default:                       return new Vector2(0f, 1f);
        }
    }

    /// <summary>
    /// Z-rotation (degrees) that turns an art sprite whose front is authored at
    /// <paramref name="baseFacing"/> so it visually points along <paramref name="movement"/>.
    /// Continuous (not quantized) — callers keep their own smoothing. Returns 0
    /// for degenerate movement so callers can fall back to their last angle.
    /// </summary>
    public static float FacingAngleDegrees(Vector2 movement, VehicleHeading baseFacing)
    {
        if (movement.sqrMagnitude < 1e-8f) return 0f;
        return Vector2.SignedAngle(DirectionOf(baseFacing), movement);
    }

    /// <summary>
    /// Classifies a movement vector into a discrete heading. Below
    /// <paramref name="threshold"/> the last valid heading is preserved (no south
    /// default, no flicker at a stop). Used for logic/tests/labels, not the
    /// smooth render angle. In four-direction mode diagonals snap to the nearest
    /// cardinal via the dominant axis.
    /// </summary>
    public static VehicleHeading Resolve(Vector2 velocity, VehicleHeading last,
                                         float threshold = 0.02f, bool eightWay = false)
    {
        if (velocity.sqrMagnitude < threshold * threshold) return last;

        if (!eightWay)
        {
            if (Mathf.Abs(velocity.x) > Mathf.Abs(velocity.y))
                return velocity.x >= 0f ? VehicleHeading.East : VehicleHeading.West;
            return velocity.y >= 0f ? VehicleHeading.North : VehicleHeading.South;
        }

        // Compass bearing clockwise from North: 0=N, 90=E, 180=S, 270=W. The enum
        // is ordered in 45 degree clockwise steps, so the rounded sector IS the enum value.
        float bearing = Mathf.Atan2(velocity.x, velocity.y) * Mathf.Rad2Deg;
        int sector = Mathf.RoundToInt(bearing / 45f);
        sector = ((sector % 8) + 8) % 8;
        return (VehicleHeading)sector;
    }

    /// <summary>
    /// Local-space anchor for a rear-mounted effect (exhaust/smoke) on an art
    /// sprite authored at <paramref name="baseFacing"/>. The effect is parented
    /// to the body and rotates with it, so "behind" is fixed in art space:
    /// opposite the authored front, offset sideways by <paramref name="lateral"/>.
    /// For a South-facing sprite this yields +Y (top of the cell = the rear).
    /// </summary>
    public static Vector3 RearAnchorLocal(VehicleHeading baseFacing, float lateral, float distance)
    {
        Vector2 f = DirectionOf(baseFacing);
        return new Vector3(lateral - f.x * distance, -f.y * distance, 0f);
    }
}
