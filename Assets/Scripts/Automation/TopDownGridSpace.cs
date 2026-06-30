using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Top-down presentation of a procedural town for Automation mode. Builds the
/// same road the Manual drive uses and maps the interpreter grid back onto it
/// using <see cref="GridTransform"/>.
/// </summary>
public class TopDownGridSpace : IGridSpace, IStopView
{
    GridTransform _transform;
    readonly RouteContext  _routeContext;
    readonly Transform     _worldRoot;
    readonly float         _roadHalfWidth;

    readonly Dictionary<Vector2Int, bool> _occupied = new Dictionary<Vector2Int, bool>();
    readonly Dictionary<Vector2Int, StopZone> _zonesByCell = new Dictionary<Vector2Int, StopZone>();
    readonly Dictionary<Vector2Int, List<Color>> _peepColorsByCell = new Dictionary<Vector2Int, List<Color>>();
    readonly Dictionary<Vector2Int, int> _waitingCountsByCell = new Dictionary<Vector2Int, int>();

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

        RefreshFromLayout(layout);
    }

    /// <summary>
    /// Re-derives the grid mapping, stop cells, occupancy and waiting-peep colors from
    /// a (possibly grown) layout WITHOUT rebuilding road visuals — the streamed chunk's
    /// tiles/buildings are appended separately via <see cref="RouteVisualBuilder.AppendProcedural"/>.
    /// Cell indices may shift when the layout grows; callers re-pin the agent by its world
    /// position afterwards. When <paramref name="rides"/> is supplied, only un-boarded,
    /// un-delivered rides leave a waiting peep (so already-served stops don't re-populate).
    /// </summary>
    public void RefreshFromLayout(TownLayout layout, IReadOnlyList<GridRide> rides = null)
    {
        if (layout == null) return;

        GridLayoutProjector.ToGridMap(layout, _transform.cellSize, out _transform, out _, out _);

        _occupied.Clear();
        _zonesByCell.Clear();
        _peepColorsByCell.Clear();
        _waitingCountsByCell.Clear();

        foreach (TownNode n in layout.nodes)
            if (n.IsStop)
            {
                _occupied[n.gridCell] = false;
                if (_routeContext.ZoneByNode != null &&
                    _routeContext.ZoneByNode.TryGetValue(n.id, out StopZone zone))
                    _zonesByCell[n.gridCell] = zone;
            }

        if (rides != null)
        {
            foreach (GridRide ride in rides)
            {
                if (ride == null || ride.aboard || ride.delivered) continue;
                AddWaitingPeepColor(ride.origin, ride.color);
            }
        }
        else
        {
            foreach (PassengerRequest req in layout.requests)
                AddWaitingPeepColor(layout.Node(req.originNodeId).gridCell, req.color);
        }

        // A stop is "occupied" exactly where a waiting peep stands.
        foreach (KeyValuePair<Vector2Int, List<Color>> pair in _peepColorsByCell)
        {
            _waitingCountsByCell[pair.Key] = pair.Value.Count;
            _occupied[pair.Key] = true;
        }

        SpawnWaitingPeeps();
    }

    void AddWaitingPeepColor(Vector2Int cell, Color color)
    {
        if (!_peepColorsByCell.TryGetValue(cell, out List<Color> colors))
        {
            colors = new List<Color>();
            _peepColorsByCell[cell] = colors;
        }
        colors.Add(color);
    }

    // -------------------------------------------------------------------------
    // IGridSpace

    public Vector3 CellToWorld(Vector2Int cell)
    {
        return _transform.CellToWorld(cell);
    }

    public Vector2Int WorldToCell(Vector2 world)
    {
        return _transform.WorldToCell(world);
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
        {
            int count = _peepColorsByCell.TryGetValue(cell, out List<Color> colors) ? colors.Count : 0;
            _waitingCountsByCell[cell] = count;
            _occupied[cell] = count > 0;
        }
        SpawnWaitingPeeps();
    }

    public void SetStopOccupied(Vector2Int cell, bool occupied)
    {
        if (!occupied)
        {
            RemoveWaitingPeeps(cell, 1);
            return;
        }

        int count = _peepColorsByCell.TryGetValue(cell, out List<Color> colors) ? Mathf.Max(1, colors.Count) : 1;
        _waitingCountsByCell[cell] = count;
        _occupied[cell] = true;
    }

    public void RemoveWaitingPeeps(Vector2Int cell, int count)
    {
        if (count <= 0) return;

        int current = _waitingCountsByCell.TryGetValue(cell, out int waiting) ? waiting : 0;
        int remove = Mathf.Min(count, Mathf.Max(0, current));

        if (_zonesByCell.TryGetValue(cell, out StopZone zone) && zone != null)
        {
            for (int i = 0; i < remove; i++)
            {
                GameObject peep = zone.TakeWaitingPeep();
                if (peep != null) Object.Destroy(peep);
            }
        }

        int remaining = Mathf.Max(0, current - remove);
        _waitingCountsByCell[cell] = remaining;
        _occupied[cell] = remaining > 0;
    }

    /// <summary>True if the stop at this cell still has a waiting passenger.</summary>
    public bool IsOccupied(Vector2Int cell)
    {
        return _occupied.TryGetValue(cell, out bool occupied) && occupied;
    }

    public void SpawnAlightingPeeps(Vector2Int cell, IReadOnlyList<Color> colors)
    {
        if (colors == null || colors.Count == 0) return;

        Sprite peepSprite = Resources.Load<Sprite>("Placeholders/peep");
        _zonesByCell.TryGetValue(cell, out StopZone zone);

        // Lay the alighting peeps out beside the sign exactly like the waiting line, so a
        // drop-off reads at the stop. Parent to the zone (inherits the road's orientation)
        // when there is one; otherwise drop them in world space at the cell.
        Vector2 startLocal = new Vector2(_roadHalfWidth + 2.1f, -0.8f);

        for (int i = 0; i < colors.Count; i++)
        {
            var peep = new GameObject("AlightingPeep");
            if (zone != null)
            {
                peep.transform.SetParent(zone.transform, false);
                peep.transform.localPosition = (Vector3)(startLocal + Vector2.right * (0.75f * i));
            }
            else
            {
                peep.transform.SetParent(_worldRoot, false);
                peep.transform.position = CellToWorld(cell) +
                    new Vector3(startLocal.x, startLocal.y - 0.75f * i, 0f);
            }

            var sr = peep.AddComponent<SpriteRenderer>();
            sr.sprite = peepSprite;
            sr.sortingOrder = 5;
            sr.color = colors[i];

            Object.Destroy(peep, 6f);
        }
    }

    void SpawnWaitingPeeps()
    {
        foreach (KeyValuePair<Vector2Int, StopZone> pair in _zonesByCell)
        {
            StopZone zone = pair.Value;
            if (zone == null) continue;

            zone.ClearWaitingPeeps();
            if (!_peepColorsByCell.TryGetValue(pair.Key, out List<Color> colors) || colors.Count <= 0)
                continue;

            int count = _waitingCountsByCell.TryGetValue(pair.Key, out int waiting)
                ? Mathf.Clamp(waiting, 0, colors.Count)
                : colors.Count;
            if (count <= 0) continue;

            if (count == colors.Count)
                zone.SpawnWaitingPeeps(colors, new Vector2(_roadHalfWidth + 2.1f, -0.8f), Vector2.right);
            else
                zone.SpawnWaitingPeeps(colors.GetRange(0, count),
                    new Vector2(_roadHalfWidth + 2.1f, -0.8f), Vector2.right);
        }
    }
}
