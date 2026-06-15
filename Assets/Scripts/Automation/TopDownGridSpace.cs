using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Top-down presentation of a procedural town for Automation mode. Builds the
/// same road the Manual drive uses and maps the interpreter grid back onto it
/// using <see cref="GridTransform"/>.
/// </summary>
public class TopDownGridSpace : IGridSpace, IStopView
{
    readonly GridTransform _transform;
    readonly RouteContext  _routeContext;
    readonly Transform     _worldRoot;
    readonly float         _roadHalfWidth;

    readonly Dictionary<Vector2Int, bool> _occupied = new Dictionary<Vector2Int, bool>();

    public RouteContext RouteContext => _routeContext;
    public Transform    WorldRoot    => _worldRoot;

    /// <summary>
    /// Builds the top-down road and stop zones from a generated town layout.
    /// </summary>
    public TopDownGridSpace(TownLayout layout, float cellSize, float roadHalfWidth,
                            Transform worldRoot)
    {
        _worldRoot     = worldRoot;
        _roadHalfWidth = roadHalfWidth;

        GridLayoutProjector.ToGridMap(layout, cellSize, out _transform, out _, out _);
        _routeContext = RouteVisualBuilder.BuildProcedural(
            worldRoot, ManualLayoutProjector.Project(layout), roadHalfWidth);

        foreach (TownNode n in layout.nodes)
            if (n.IsStop)
                _occupied[n.gridCell] = true;
    }

    // -------------------------------------------------------------------------
    // IGridSpace

    public Vector3 CellToWorld(Vector2Int cell)
    {
        return _transform.CellToWorld(cell);
    }

    public int SortOrder(Vector2Int cell)
    {
        // Higher y is "north" / further back in top-down; sort by y so the
        // agent draws on top of roads behind it and under roads in front.
        return cell.y * 1000 + cell.x;
    }

    public Vector2 FacingDirection(int facing)
    {
        switch (((facing % 4) + 4) % 4)
        {
            case 0:  return Vector2.up;    // North
            case 1:  return Vector2.right; // East
            case 2:  return Vector2.down;  // South
            default: return Vector2.left;  // West
        }
    }

    // -------------------------------------------------------------------------
    // IStopView

    public void ResetStops()
    {
        var cells = new List<Vector2Int>(_occupied.Keys);
        foreach (Vector2Int cell in cells)
            _occupied[cell] = true;
    }

    public void SetStopOccupied(Vector2Int cell, bool occupied)
    {
        _occupied[cell] = occupied;
    }

    /// <summary>True if the stop at this cell still has a waiting passenger.</summary>
    public bool IsOccupied(Vector2Int cell)
    {
        return _occupied.TryGetValue(cell, out bool occupied) && occupied;
    }
}
