using UnityEngine;

/// <summary>
/// Presentation-neutral coordinate space for the automation grid. Converts
/// grid cells to world positions, provides sorting order, and gives the world
/// direction for each grid facing (0=N,1=E,2=S,3=W).
/// </summary>
public interface IGridSpace
{
    /// <summary>World position of the center of <paramref name="cell"/>.</summary>
    Vector3 CellToWorld(Vector2Int cell);

    /// <summary>Sorting order for a sprite at <paramref name="cell"/>.</summary>
    int SortOrder(Vector2Int cell);

    /// <summary>World direction vector for the given facing.</summary>
    Vector2 FacingDirection(int facing);
}
