using UnityEngine;

/// <summary>
/// One authored "Non-Intersecting Connections" board: hub pairs plus a reference
/// solution (one full path per colour). The reference solution is what the
/// EditMode tests apply to prove the board is solvable and that
/// <see cref="FlowConnectBoard.IsSolved"/> accepts it. Every layout here is
/// solvable by construction (paths never share a cell).
/// </summary>
public class FlowConnectLayout
{
    public int Width;
    public int Height;
    public FlowPair[] Pairs;
    public Vector2Int[][] Solution;   // per colour, the full path incl. both hubs
}

/// <summary>
/// The small rotation of authored Molo transit-hub boards. All are 5×5 and
/// guaranteed solvable. Picked deterministically by seed.
/// </summary>
public static class FlowConnectLayouts
{
    public static FlowConnectLayout Get(int seed)
    {
        int i = ((seed % All.Length) + All.Length) % All.Length;
        return All[i];
    }

    public static readonly FlowConnectLayout[] All =
    {
        Frame(), Bands(), Columns(), LoomStripes(), CoastalTurns()
    };

    // -------------------------------------------------------------------------

    static Vector2Int V(int x, int y) => new Vector2Int(x, y);

    /// <summary>Edge-frame board: two side columns + two short top/bottom links.</summary>
    static FlowConnectLayout Frame()
    {
        return new FlowConnectLayout
        {
            Width = 5, Height = 5,
            Pairs = new[]
            {
                new FlowPair(V(0,0), V(0,4)),   // left column
                new FlowPair(V(4,0), V(4,4)),   // right column
                new FlowPair(V(1,0), V(3,0)),   // top link
                new FlowPair(V(1,4), V(3,4)),   // bottom link
            },
            Solution = new[]
            {
                new[] { V(0,0), V(0,1), V(0,2), V(0,3), V(0,4) },
                new[] { V(4,0), V(4,1), V(4,2), V(4,3), V(4,4) },
                new[] { V(1,0), V(2,0), V(3,0) },
                new[] { V(1,4), V(2,4), V(3,4) },
            },
        };
    }

    /// <summary>Horizontal bands (with one short middle band).</summary>
    static FlowConnectLayout Bands()
    {
        return new FlowConnectLayout
        {
            Width = 5, Height = 5,
            Pairs = new[]
            {
                new FlowPair(V(0,0), V(4,0)),
                new FlowPair(V(0,1), V(4,1)),
                new FlowPair(V(1,2), V(3,2)),
                new FlowPair(V(0,3), V(4,3)),
                new FlowPair(V(0,4), V(4,4)),
            },
            Solution = new[]
            {
                new[] { V(0,0), V(1,0), V(2,0), V(3,0), V(4,0) },
                new[] { V(0,1), V(1,1), V(2,1), V(3,1), V(4,1) },
                new[] { V(1,2), V(2,2), V(3,2) },
                new[] { V(0,3), V(1,3), V(2,3), V(3,3), V(4,3) },
                new[] { V(0,4), V(1,4), V(2,4), V(3,4), V(4,4) },
            },
        };
    }

    /// <summary>Five vertical columns.</summary>
    static FlowConnectLayout Columns()
    {
        var pairs  = new FlowPair[5];
        var sol    = new Vector2Int[5][];
        for (int x = 0; x < 5; x++)
        {
            pairs[x] = new FlowPair(V(x, 0), V(x, 4));
            sol[x]   = new[] { V(x, 0), V(x, 1), V(x, 2), V(x, 3), V(x, 4) };
        }
        return new FlowConnectLayout { Width = 5, Height = 5, Pairs = pairs, Solution = sol };
    }

    /// <summary>Six thin weave bands: more endpoints, still readable on the 5x5 board.</summary>
    static FlowConnectLayout LoomStripes()
    {
        return new FlowConnectLayout
        {
            Width = 5, Height = 5,
            Pairs = new[]
            {
                new FlowPair(V(0,0), V(4,0)),
                new FlowPair(V(0,1), V(4,1)),
                new FlowPair(V(0,2), V(4,2)),
                new FlowPair(V(0,3), V(4,3)),
                new FlowPair(V(0,4), V(2,4)),
                new FlowPair(V(3,4), V(4,4)),
            },
            Solution = new[]
            {
                new[] { V(0,0), V(1,0), V(2,0), V(3,0), V(4,0) },
                new[] { V(0,1), V(1,1), V(2,1), V(3,1), V(4,1) },
                new[] { V(0,2), V(1,2), V(2,2), V(3,2), V(4,2) },
                new[] { V(0,3), V(1,3), V(2,3), V(3,3), V(4,3) },
                new[] { V(0,4), V(1,4), V(2,4) },
                new[] { V(3,4), V(4,4) },
            },
        };
    }

    /// <summary>Frame-plus-bands board with a long outside turn and tight inner links.</summary>
    static FlowConnectLayout CoastalTurns()
    {
        return new FlowConnectLayout
        {
            Width = 5, Height = 5,
            Pairs = new[]
            {
                new FlowPair(V(0,0), V(4,4)),
                new FlowPair(V(0,4), V(3,4)),
                new FlowPair(V(0,1), V(0,3)),
                new FlowPair(V(1,1), V(3,1)),
                new FlowPair(V(1,2), V(3,2)),
                new FlowPair(V(1,3), V(3,3)),
            },
            Solution = new[]
            {
                new[] { V(0,0), V(1,0), V(2,0), V(3,0), V(4,0), V(4,1), V(4,2), V(4,3), V(4,4) },
                new[] { V(0,4), V(1,4), V(2,4), V(3,4) },
                new[] { V(0,1), V(0,2), V(0,3) },
                new[] { V(1,1), V(2,1), V(3,1) },
                new[] { V(1,2), V(2,2), V(3,2) },
                new[] { V(1,3), V(2,3), V(3,3) },
            },
        };
    }
}
