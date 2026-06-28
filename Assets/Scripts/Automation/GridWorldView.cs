using UnityEngine;

/// <summary>
/// Renders a <see cref="GridModel"/> as an isometric diamond grid (2D iso,
/// PRD §5.3) and frames the world camera around it. Pure presentation —
/// all rules live in <see cref="AgentSim"/>.
/// </summary>
public class GridWorldView : MonoBehaviour, IGridSpace, IStopView
{
    public const float TileW = 1f;
    public const float TileH = 0.5f;

    GridModel _grid;
    readonly System.Collections.Generic.Dictionary<Vector2Int, SpriteRenderer> _stopMarkers =
        new System.Collections.Generic.Dictionary<Vector2Int, SpriteRenderer>();

    // -------------------------------------------------------------------------

    /// <summary>Spawns the tile sprites for a parsed map (clears any old ones).</summary>
    public void Build(GridModel grid)
    {
        _grid = grid;
        _stopMarkers.Clear();

        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        Sprite grass = Resources.Load<Sprite>("Placeholders/iso_ground_grass");
        Sprite wall  = Resources.Load<Sprite>("Placeholders/iso_wall");
        Sprite start = Resources.Load<Sprite>("Placeholders/iso_start");
        Sprite dest  = Resources.Load<Sprite>("Placeholders/iso_dest");
        Sprite stop  = Resources.Load<Sprite>("Placeholders/iso_stop");
        Sprite peep  = Resources.Load<Sprite>("Placeholders/peep");

        for (int y = 0; y < grid.Height; y++)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                var cell = grid.Get(x, y);
                var tile = new GameObject($"Tile_{x}_{y}");
                tile.transform.SetParent(transform, false);
                tile.transform.localPosition = IsoLocal(x, y);

                var sr = tile.AddComponent<SpriteRenderer>();
                sr.sprite = TileSprite(cell, grass, wall, start, dest, stop);
                // Walls are raised blocks; sort them just above their own cell so
                // their body draws over the floor behind and the cell in front
                // (higher x+y) still occludes the wall's lower-front edge.
                sr.sortingOrder = x + y;

                // A waiting peep stands on each passenger stop.
                if (cell == GridModel.Cell.Stop)
                {
                    var marker = new GameObject("WaitingPeep");
                    marker.transform.SetParent(tile.transform, false);
                    marker.transform.localPosition = new Vector3(0f, 0.22f, 0f);
                    var peepSr = marker.AddComponent<SpriteRenderer>();
                    peepSr.sprite = peep;
                    peepSr.sortingOrder = x + y + 1;
                    peepSr.color = StopZone.PeepColor(x * 13 + y * 7);
                    _stopMarkers[new Vector2Int(x, y)] = peepSr;
                }
            }
        }
    }

    static Sprite TileSprite(GridModel.Cell cell, Sprite grass, Sprite wall,
                             Sprite start, Sprite dest, Sprite stop)
    {
        switch (cell)
        {
            case GridModel.Cell.Wall:        return wall;
            case GridModel.Cell.Start:       return start;
            case GridModel.Cell.Destination: return dest;
            case GridModel.Cell.Stop:        return stop;
            default:                         return grass;
        }
    }

    /// <summary>Tints the waiting peeps on the given stop cells (per-rider colors).</summary>
    public void ColorStops(System.Collections.Generic.IEnumerable<
                               System.Collections.Generic.KeyValuePair<Vector2Int, Color>> colors)
    {
        foreach (var pair in colors)
            if (_stopMarkers.TryGetValue(pair.Key, out SpriteRenderer marker) && marker != null)
                marker.color = pair.Value;
    }

    /// <summary>Shows/hides the waiting peep on a stop (picked up / reset).</summary>
    public void SetStopOccupied(Vector2Int cell, bool occupied)
    {
        if (!occupied)
        {
            RemoveWaitingPeeps(cell, 1);
            return;
        }

        if (_stopMarkers.TryGetValue(cell, out SpriteRenderer marker) && marker != null)
            marker.enabled = true;
    }

    public void RemoveWaitingPeeps(Vector2Int cell, int count)
    {
        if (count <= 0) return;
        if (_stopMarkers.TryGetValue(cell, out SpriteRenderer marker) && marker != null)
            marker.enabled = false;
    }

    public void ResetStops()
    {
        foreach (var pair in _stopMarkers)
            if (pair.Value != null) pair.Value.enabled = true;
    }

    // -------------------------------------------------------------------------
    // Iso math

    public static Vector3 IsoLocal(int x, int y)
    {
        return IsoProjection.Project(new Vector2(x, y));
    }

    public Vector3 CellToWorld(Vector2Int cell)
    {
        return transform.TransformPoint(IsoLocal(cell.x, cell.y));
    }

    public int SortOrder(Vector2Int cell) => cell.x + cell.y;

    /// <summary>
    /// Screen direction of one grid step while facing <paramref name="facing"/>
    /// (0=N up-right, 1=E down-right, 2=S down-left, 3=W up-left).
    /// </summary>
    public static Vector2 FacingScreenDirection(int facing)
    {
        switch (((facing % 4) + 4) % 4)
        {
            case 0:  return new Vector2(0.5f, 0.25f).normalized;
            case 1:  return new Vector2(0.5f, -0.25f).normalized;
            case 2:  return new Vector2(-0.5f, -0.25f).normalized;
            default: return new Vector2(-0.5f, 0.25f).normalized;
        }
    }

    /// <summary>Implementation of <see cref="IGridSpace.FacingDirection"/>.</summary>
    public Vector2 FacingDirection(int facing)
    {
        return FacingScreenDirection(facing);
    }

    // -------------------------------------------------------------------------

    /// <summary>Centers the camera on the grid and zooms to fit it.</summary>
    public void FrameCamera(Camera cam)
    {
        if (cam == null || _grid == null) return;

        Vector3 min = new Vector3(float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue);

        for (int y = 0; y < _grid.Height; y++)
        {
            for (int x = 0; x < _grid.Width; x++)
            {
                Vector3 p = CellToWorld(new Vector2Int(x, y));
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }
        }

        Vector3 center = (min + max) * 0.5f;
        cam.transform.position = new Vector3(center.x, center.y, cam.transform.position.z);

        float halfHeight = (max.y - min.y) * 0.5f + 1.2f;
        float halfWidth  = (max.x - min.x) * 0.5f + 1.2f;
        cam.orthographicSize = Mathf.Max(halfHeight, halfWidth / Mathf.Max(0.1f, cam.aspect));
    }
}
