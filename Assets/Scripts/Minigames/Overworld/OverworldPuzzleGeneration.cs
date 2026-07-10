using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Shared difficulty curve for the six overworld visits.</summary>
public static class OverworldPuzzleTuning
{
    public static int GridSize(int levelIndex)
    {
        if (levelIndex <= 0) return 5;
        if (levelIndex <= 2) return 6;
        if (levelIndex <= 4) return 7;
        return 8;
    }

    public static int FlowPairs(int levelIndex)
        => Mathf.Clamp(2 + ((levelIndex + 1) / 2), 2, 5);

    public static int CrateCount(int levelIndex)
        => Mathf.Clamp(4 + ((levelIndex + 1) / 2), 4, 7);

    public static int PatternLength(int levelIndex)
        => Mathf.Clamp(3 + levelIndex, 3, 7);

    public static float PatternCueSeconds(int levelIndex)
        => Mathf.Max(0.30f, 0.58f - 0.045f * levelIndex);

    public static float SoftTimerSeconds(int expectedMoves)
        => Mathf.Clamp(expectedMoves * 4f, 45f, 120f);
}

/// <summary>Procedurally generated fill board with a known Hamiltonian solution.</summary>
public sealed class FillLayout
{
    public int width;
    public int height;
    public bool[,] active;
    public Vector2Int[] solution;
}

/// <summary>Validated procedural layouts for the click/drag grid objectives.</summary>
public static class OverworldPuzzleGenerator
{
    static readonly Vector2Int[] Cardinal =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
    };

    public static MazeLayout GenerateMaze(int levelIndex, int seed)
    {
        int size = OverworldPuzzleTuning.GridSize(levelIndex);
        var random = new System.Random(seed);
        var maze = new MazeLayout
        {
            width = size,
            height = size,
            wall = new bool[size, size],
            start = Vector2Int.zero,
            goal = new Vector2Int(size - 1, size - 1),
        };

        Divide(maze.wall, 0, 0, size, size, random);
        maze.wall[maze.start.y, maze.start.x] = false;
        maze.wall[maze.goal.y, maze.goal.x] = false;

        if (!MazeReachable(maze))
        {
            // Preserve the procedural walls but carve a deterministic edge route
            // rather than falling back to a differently sized authored board.
            for (int x = 0; x < size; x++) maze.wall[0, x] = false;
            for (int y = 0; y < size; y++) maze.wall[y, size - 1] = false;
        }
        return maze;
    }

    public static FillLayout GenerateFill(int levelIndex, int seed)
    {
        int size = OverworldPuzzleTuning.GridSize(levelIndex);
        int[] targets = { 12, 18, 24, 30, 36, 42 };
        int target = targets[Mathf.Clamp(levelIndex, 0, targets.Length - 1)];
        target = Mathf.Min(target, size * size);

        var random = new System.Random(seed);
        var path = new List<Vector2Int> { new Vector2Int(random.Next(size), random.Next(size)) };
        var used = new HashSet<Vector2Int>(path);
        int budget = 250000;
        if (!GrowPath(path, used, size, target, random, ref budget))
        {
            path.Clear();
            for (int y = 0; y < size && path.Count < target; y++)
            {
                if ((y & 1) == 0)
                    for (int x = 0; x < size && path.Count < target; x++) path.Add(new Vector2Int(x, y));
                else
                    for (int x = size - 1; x >= 0 && path.Count < target; x--) path.Add(new Vector2Int(x, y));
            }
        }

        var active = new bool[size, size];
        foreach (Vector2Int cell in path) active[cell.y, cell.x] = true;
        return new FillLayout { width = size, height = size, active = active, solution = path.ToArray() };
    }

    static bool GrowPath(List<Vector2Int> path, HashSet<Vector2Int> used, int size,
                         int target, System.Random random, ref int budget)
    {
        if (path.Count >= target) return true;
        if (--budget <= 0) return false;

        var dirs = new List<Vector2Int>(Cardinal);
        Shuffle(dirs, random);
        Vector2Int head = path[path.Count - 1];
        foreach (Vector2Int dir in dirs)
        {
            Vector2Int next = head + dir;
            if (next.x < 0 || next.y < 0 || next.x >= size || next.y >= size || used.Contains(next))
                continue;
            used.Add(next);
            path.Add(next);
            if (GrowPath(path, used, size, target, random, ref budget)) return true;
            path.RemoveAt(path.Count - 1);
            used.Remove(next);
        }
        return false;
    }

    static void Divide(bool[,] wall, int x, int y, int width, int height, System.Random random)
    {
        if (width < 3 || height < 3) return;
        bool horizontal = height > width || (height == width && random.Next(2) == 0);
        if (horizontal)
        {
            int wallY = y + 1 + random.Next(height - 2);
            int gapX = x + random.Next(width);
            for (int px = x; px < x + width; px++) if (px != gapX) wall[wallY, px] = true;
            Divide(wall, x, y, width, wallY - y, random);
            Divide(wall, x, wallY + 1, width, y + height - wallY - 1, random);
        }
        else
        {
            int wallX = x + 1 + random.Next(width - 2);
            int gapY = y + random.Next(height);
            for (int py = y; py < y + height; py++) if (py != gapY) wall[py, wallX] = true;
            Divide(wall, x, y, wallX - x, height, random);
            Divide(wall, wallX + 1, y, x + width - wallX - 1, height, random);
        }
    }

    public static bool MazeReachable(MazeLayout maze)
    {
        if (maze == null || maze.wall == null) return false;
        var seen = new HashSet<Vector2Int> { maze.start };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(maze.start);
        while (queue.Count > 0)
        {
            Vector2Int cell = queue.Dequeue();
            if (cell == maze.goal) return true;
            foreach (Vector2Int dir in Cardinal)
            {
                Vector2Int next = cell + dir;
                if (next.x < 0 || next.y < 0 || next.x >= maze.width || next.y >= maze.height ||
                    maze.wall[next.y, next.x] || !seen.Add(next)) continue;
                queue.Enqueue(next);
            }
        }
        return false;
    }

    static void Shuffle<T>(IList<T> list, System.Random random)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

/// <summary>One generated rotate-the-routes board.</summary>
public sealed class RouteRotationLayout
{
    public int width;
    public int height;
    public int[,] colors;
    public int[,] solvedMasks;
    public bool[,] endpoints;
    public int[,] rotations;
}

/// <summary>Pure model for the distinct Color Connect rotation puzzle.</summary>
public sealed class RouteRotationBoard
{
    public const int North = 1;
    public const int East = 2;
    public const int South = 4;
    public const int West = 8;

    readonly RouteRotationLayout _layout;
    readonly int[,] _rotations;

    public int Width => _layout.width;
    public int Height => _layout.height;

    public RouteRotationBoard(RouteRotationLayout layout)
    {
        _layout = layout;
        _rotations = (int[,])layout.rotations.Clone();
    }

    public int ColorAt(int x, int y) => _layout.colors[y, x];
    public bool IsEndpoint(int x, int y) => _layout.endpoints[y, x];
    public int MaskAt(int x, int y) => RotateMask(_layout.solvedMasks[y, x], _rotations[y, x]);

    public void Rotate(int x, int y, int delta = 1)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height || ColorAt(x, y) < 0) return;
        _rotations[y, x] = ((_rotations[y, x] + delta) % 4 + 4) % 4;
    }

    public bool IsSolved()
    {
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                int color = ColorAt(x, y);
                if (color < 0) continue;
                int mask = MaskAt(x, y);
                if (!EdgeMatches(x, y, North, 0, -1, South, color, mask) ||
                    !EdgeMatches(x, y, East, 1, 0, West, color, mask) ||
                    !EdgeMatches(x, y, South, 0, 1, North, color, mask) ||
                    !EdgeMatches(x, y, West, -1, 0, East, color, mask)) return false;
            }
        return true;
    }

    bool EdgeMatches(int x, int y, int bit, int dx, int dy, int reciprocal,
                     int color, int mask)
    {
        bool exits = (mask & bit) != 0;
        int nx = x + dx, ny = y + dy;
        bool neighbourMatches = nx >= 0 && ny >= 0 && nx < Width && ny < Height &&
                                ColorAt(nx, ny) == color && (MaskAt(nx, ny) & reciprocal) != 0;
        return exits == neighbourMatches;
    }

    public static int RotateMask(int mask, int turns)
    {
        turns = ((turns % 4) + 4) % 4;
        for (int i = 0; i < turns; i++)
            mask = ((mask << 1) & 0xF) | ((mask & West) != 0 ? North : 0);
        return mask;
    }
}

public static class RouteRotationGenerator
{
    public static RouteRotationLayout Generate(int levelIndex, int seed)
    {
        int size = OverworldPuzzleTuning.GridSize(levelIndex);
        int pairs = Mathf.Min(OverworldPuzzleTuning.FlowPairs(levelIndex), size);
        var random = new System.Random(seed);
        var colors = new int[size, size];
        var masks = new int[size, size];
        var endpoints = new bool[size, size];
        var rotations = new int[size, size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++) colors[y, x] = -1;

        var lanes = new List<int>();
        for (int i = 0; i < size; i++) lanes.Add(i);
        Shuffle(lanes, random);
        bool horizontal = random.Next(2) == 0;
        for (int color = 0; color < pairs; color++)
        {
            int lane = lanes[color];
            for (int p = 0; p < size; p++)
            {
                int x = horizontal ? p : lane;
                int y = horizontal ? lane : p;
                colors[y, x] = color;
                bool first = p == 0, last = p == size - 1;
                endpoints[y, x] = first || last;
                masks[y, x] = horizontal
                    ? (first ? RouteRotationBoard.East : last ? RouteRotationBoard.West : RouteRotationBoard.East | RouteRotationBoard.West)
                    : (first ? RouteRotationBoard.South : last ? RouteRotationBoard.North : RouteRotationBoard.North | RouteRotationBoard.South);
                rotations[y, x] = random.Next(4);
            }
        }

        var layout = new RouteRotationLayout
        {
            width = size, height = size, colors = colors,
            solvedMasks = masks, endpoints = endpoints, rotations = rotations,
        };
        var board = new RouteRotationBoard(layout);
        if (board.IsSolved())
        {
            int lane = lanes[0];
            int x = horizontal ? 0 : lane;
            int y = horizontal ? lane : 0;
            rotations[y, x] = (rotations[y, x] + 1) % 4;
        }
        return layout;
    }

    static void Shuffle<T>(IList<T> list, System.Random random)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
