using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// The CodeDrive scene derives its tile grid from each level's manual route.
/// These tests guarantee the derived grid is well-formed (exactly one start and
/// destination) and actually solvable — the destination and every passenger
/// stop must be reachable from the start over walkable, 4-connected cells.
/// </summary>
public class RouteToGridTests
{
    static readonly Vector2Int[] Dirs =
    {
        new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0),
    };

    static GridModel Derive(int levelIndex, out int startFacing)
    {
        ManualRouteDefinition route = LevelLibrary.Get(levelIndex).manual;
        RouteToGrid.Result result = RouteToGrid.FromManualRoute(route);
        startFacing = result.StartFacing;

        GridModel grid = GridModel.Parse(result.Map, out var errors);
        CollectionAssert.IsEmpty(errors, "derived grid must parse cleanly (exactly one S and one D)");
        return grid;
    }

    static bool Reachable(GridModel grid, Vector2Int from, Vector2Int to)
    {
        var seen = new HashSet<Vector2Int> { from };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(from);

        while (queue.Count > 0)
        {
            Vector2Int c = queue.Dequeue();
            if (c == to) return true;
            foreach (Vector2Int d in Dirs)
            {
                Vector2Int n = c + d;
                if (!seen.Contains(n) && grid.IsWalkable(n)) { seen.Add(n); queue.Enqueue(n); }
            }
        }
        return false;
    }

    static void AssertSolvable(int level)
    {
        GridModel grid = Derive(level, out int facing);

        Assert.IsTrue(facing >= 0 && facing < 4, "start facing must be 0..3");
        Assert.IsTrue(grid.IsWalkable(grid.StartPos), "start cell must be walkable");
        Assert.IsTrue(grid.IsWalkable(grid.DestPos),  "destination cell must be walkable");
        Assert.IsTrue(Reachable(grid, grid.StartPos, grid.DestPos),
            "the destination must be reachable from the start over walkable cells");

        foreach (Vector2Int stop in grid.StopCells)
            Assert.IsTrue(Reachable(grid, grid.StartPos, stop),
                "every passenger stop must be reachable from the start");
    }

    [Test] public void Tutorial_DerivedGrid_IsSolvable() => AssertSolvable(0);
    [Test] public void Level1_DerivedGrid_IsSolvable()   => AssertSolvable(1);

    [Test]
    public void DegenerateRoute_StillProducesAValidGrid()
    {
        var route = new ManualRouteDefinition { waypoints = new Vector2[0] };
        GridModel grid = GridModel.Parse(RouteToGrid.FromManualRoute(route).Map, out var errors);
        CollectionAssert.IsEmpty(errors, "fallback grid must parse cleanly");
        Assert.IsTrue(Reachable(grid, grid.StartPos, grid.DestPos));
    }
}
