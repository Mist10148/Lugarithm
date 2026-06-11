using UnityEngine;

/// <summary>
/// Renders a <see cref="GridModel"/> as an isometric diamond grid (2D iso,
/// PRD §5.3) and frames the world camera around it. Pure presentation —
/// all rules live in <see cref="AgentSim"/>.
/// </summary>
public class GridWorldView : MonoBehaviour
{
    public const float TileW = 1f;
    public const float TileH = 0.5f;

    static readonly Color RoadColor  = new Color(0.58f, 0.58f, 0.62f);
    static readonly Color WallColor  = new Color(0.20f, 0.22f, 0.28f);
    static readonly Color StartColor = new Color(0.35f, 0.55f, 0.95f);
    static readonly Color DestColor  = new Color(0.35f, 0.85f, 0.45f);
    static readonly Color StopColor  = new Color(0.95f, 0.75f, 0.25f);

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

        Sprite diamond = Resources.Load<Sprite>("Placeholders/diamond");
        Sprite peep    = Resources.Load<Sprite>("Placeholders/peep");

        for (int y = 0; y < grid.Height; y++)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                var cell = grid.Get(x, y);
                var tile = new GameObject($"Tile_{x}_{y}");
                tile.transform.SetParent(transform, false);
                tile.transform.localPosition = IsoLocal(x, y) +
                    (cell == GridModel.Cell.Wall ? new Vector3(0f, 0.14f, 0f) : Vector3.zero);

                var sr = tile.AddComponent<SpriteRenderer>();
                sr.sprite = diamond;
                sr.sortingOrder = x + y;
                sr.color = TileColor(cell);

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

    /// <summary>Shows/hides the waiting peep on a stop (picked up / reset).</summary>
    public void SetStopOccupied(Vector2Int cell, bool occupied)
    {
        if (_stopMarkers.TryGetValue(cell, out SpriteRenderer marker) && marker != null)
            marker.enabled = occupied;
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
        return new Vector3((x - y) * TileW * 0.5f, -(x + y) * TileH * 0.5f, 0f);
    }

    public Vector3 CellToWorld(Vector2Int cell)
    {
        return transform.TransformPoint(IsoLocal(cell.x, cell.y));
    }

    public static int SortOrder(Vector2Int cell) => cell.x + cell.y;

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

    static Color TileColor(GridModel.Cell cell)
    {
        switch (cell)
        {
            case GridModel.Cell.Wall:        return WallColor;
            case GridModel.Cell.Start:       return StartColor;
            case GridModel.Cell.Destination: return DestColor;
            case GridModel.Cell.Stop:        return StopColor;
            default:                         return RoadColor;
        }
    }
}
