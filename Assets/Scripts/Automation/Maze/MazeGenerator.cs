using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Generates a random <b>perfect</b> maze (a spanning tree of cells — exactly one
/// path between any two cells, no loops, no isolated wall islands) via the
/// recursive-backtracker algorithm, rendered into the automation grid legend
/// ('#'/'.'/'S'/'D'). Because the maze is simply connected, the right-hand
/// wall-follower in <see cref="MazeContent.WallFollower"/> always reaches D, so
/// every generated maze is solvable. Deterministic for a given seed.
/// </summary>
public static class MazeGenerator
{
    // Cell-space direction order N, E, S, W.
    static readonly int[] Dx = { 0, 1, 0, -1 };
    static readonly int[] Dy = { -1, 0, 1, 0 };
    static readonly int[] Opposite = { 2, 3, 0, 1 };

    /// <summary>Builds a perfect maze of <paramref name="cols"/>×<paramref name="rows"/> cells.</summary>
    public static AutomationPuzzleDefinition Generate(int cols, int rows, int seed)
    {
        cols = Math.Max(2, cols);
        rows = Math.Max(2, rows);
        var rng = new System.Random(seed);

        var visited = new bool[cols, rows];
        var open     = new bool[cols, rows, 4];   // carved passage per direction

        var stack = new Stack<(int x, int y)>();
        visited[0, 0] = true;
        stack.Push((0, 0));

        while (stack.Count > 0)
        {
            (int cx, int cy) = stack.Peek();

            var dirs = new List<int> { 0, 1, 2, 3 };
            Shuffle(dirs, rng);

            bool advanced = false;
            foreach (int d in dirs)
            {
                int nx = cx + Dx[d];
                int ny = cy + Dy[d];
                if (nx < 0 || ny < 0 || nx >= cols || ny >= rows || visited[nx, ny]) continue;

                open[cx, cy, d] = true;
                open[nx, ny, Opposite[d]] = true;
                visited[nx, ny] = true;
                stack.Push((nx, ny));
                advanced = true;
                break;
            }
            if (!advanced) stack.Pop();
        }

        // Render: a cell at (cx,cy) sits at char (2cx+1, 2cy+1); carved passages
        // open the wall char between two cells.
        int w = 2 * cols + 1;
        int h = 2 * rows + 1;
        var g = new char[w, h];
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                g[x, y] = '#';

        for (int cx = 0; cx < cols; cx++)
        {
            for (int cy = 0; cy < rows; cy++)
            {
                int gx = 2 * cx + 1;
                int gy = 2 * cy + 1;
                g[gx, gy] = '.';
                if (open[cx, cy, 0]) g[gx, gy - 1] = '.';
                if (open[cx, cy, 1]) g[gx + 1, gy] = '.';
                if (open[cx, cy, 2]) g[gx, gy + 1] = '.';
                if (open[cx, cy, 3]) g[gx - 1, gy] = '.';
            }
        }

        int sx = 1, sy = 1;                 // cell (0,0)
        int dx = 2 * cols - 1, dy = 2 * rows - 1; // cell (cols-1, rows-1)
        g[sx, sy] = 'S';
        g[dx, dy] = 'D';

        int startFacing = FacingTowardOpening(g, w, h, sx, sy);

        var map = new string[h];
        for (int y = 0; y < h; y++)
        {
            var sb = new StringBuilder(w);
            for (int x = 0; x < w; x++) sb.Append(g[x, y]);
            map[y] = sb.ToString();
        }

        return new AutomationPuzzleDefinition
        {
            gridMap        = map,
            startFacing    = startFacing,
            goalText       = "Escape the maze: reach the destination (D). Keep one hand on the wall.",
            allowedBlocks  = MazeContent.Blocks,
            allowedQueries = MazeContent.Queries,
            parSteps       = cols * rows * 3,
            softTimerSeconds = 600f,
            requireAllPassengersDelivered = false,
            codeScaffold        = MazeContent.Scaffold,
            optimalSolutionText = MazeContent.WallFollower,
        };
    }

    // -------------------------------------------------------------------------

    static int FacingTowardOpening(char[,] g, int w, int h, int sx, int sy)
    {
        // char-space neighbours in facing order N,E,S,W
        int[] fx = { 0, 1, 0, -1 };
        int[] fy = { -1, 0, 1, 0 };
        for (int f = 0; f < 4; f++)
        {
            int nx = sx + fx[f];
            int ny = sy + fy[f];
            if (nx >= 0 && ny >= 0 && nx < w && ny < h && g[nx, ny] != '#')
                return f;
        }
        return 1; // East fallback (a carved maze always has an opening)
    }

    static void Shuffle(List<int> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
