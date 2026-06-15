using UnityEngine;

/// <summary>
/// Affine transform used by <see cref="GridLayoutProjector"/> to map between
/// top-down world coordinates and grid cells. Both the rasterizer and the
/// top-down automation view share this so they cannot drift apart.
/// </summary>
public struct GridTransform
{
    public readonly float minX;
    public readonly float maxY;
    public readonly float cellSize;
    public readonly int   border;

    public GridTransform(float minX, float maxY, float cellSize, int border)
    {
        this.minX     = minX;
        this.maxY     = maxY;
        this.cellSize = cellSize;
        this.border   = border;
    }

    /// <summary>World position of the center of <paramref name="cell"/>.</summary>
    public Vector3 CellToWorld(Vector2Int cell)
    {
        return new Vector3(
            minX + (cell.x - border) * cellSize,
            maxY - (cell.y - border) * cellSize,
            0f);
    }

    /// <summary>Grid cell of a world position.</summary>
    public Vector2Int WorldToCell(Vector2 world)
    {
        return new Vector2Int(
            border + Mathf.RoundToInt((world.x - minX) / cellSize),
            border + Mathf.RoundToInt((maxY - world.y) / cellSize));
    }
}
