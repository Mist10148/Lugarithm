using UnityEngine;

/// <summary>
/// Shared isometric projection. The simulation (physics, triggers, route math)
/// runs on a flat logical XY plane; this maps a logical position to where it is
/// drawn on screen in isometric perspective. It is linear, so it also projects
/// direction/velocity vectors. The ratios match <see cref="GridWorldView"/>'s
/// diamond tiles (1 wide, 0.5 tall) so the grid world and the continuous manual
/// world share one perspective.
/// </summary>
public static class IsoProjection
{
    public const float TileW = 1f;
    public const float TileH = 0.5f;

    /// <summary>Logical (x,y) → on-screen isometric position (z = 0).</summary>
    public static Vector3 Project(Vector2 logical)
    {
        return new Vector3((logical.x - logical.y) * TileW * 0.5f,
                           -(logical.x + logical.y) * TileH * 0.5f, 0f);
    }

    /// <summary>Projects a direction/velocity (linear — no translation term).</summary>
    public static Vector2 ProjectVector(Vector2 v)
    {
        return new Vector2((v.x - v.y) * TileW * 0.5f,
                           -(v.x + v.y) * TileH * 0.5f);
    }

    /// <summary>Painter's-order key: larger = nearer the viewer (drawn on top).</summary>
    public static int SortOrder(Vector2 logical)
    {
        return Mathf.RoundToInt((logical.x + logical.y) * 4f);
    }
}
