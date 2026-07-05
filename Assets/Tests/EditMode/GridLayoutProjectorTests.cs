using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode tests for projecting a generated town into an Automation grid: the
/// rasterized map must parse cleanly, expose exactly one S/D, place every stop on
/// its own reachable cell, and record each node's grid cell.
/// </summary>
public class GridLayoutProjectorTests
{
    static readonly int[] ProceduralLevels = { 2, 3, 4, 5 };

    [Test]
    public void Projection_ParsesCleanly_WithOneStartAndDest()
    {
        foreach (int level in ProceduralLevels)
        {
            LevelDefinition levelDef = LevelLibrary.Get(level);
            ProceduralLayoutDefinition def = levelDef.procedural;
            for (int seed = 0; seed < 40; seed++)
            {
                TownLayout layout = TownLayoutGenerator.Generate(def, levelDef.fares, seed);
                string[] map = GridLayoutProjector.ToGridMap(layout, def.gen.gridCellSize, out int facing, out var projErrors);

                CollectionAssert.IsEmpty(projErrors, $"level {level}, seed {seed}: projection errors");

                GridModel grid = GridModel.Parse(map, out var mapErrors);
                CollectionAssert.IsEmpty(mapErrors, $"level {level}, seed {seed}: grid parse errors");

                Assert.GreaterOrEqual(facing, 0);
                Assert.Less(facing, 4, $"level {level}, seed {seed}: valid start facing");
                Assert.AreNotEqual(grid.StartPos, grid.DestPos, $"level {level}, seed {seed}: S and D distinct");
            }
        }
    }

    [Test]
    public void Projection_FillsEveryNodeGridCell()
    {
        ProceduralLayoutDefinition def = LevelLibrary.Get(2).procedural;
        TownLayout layout = TownLayoutGenerator.Generate(def, new FareTable(), 7);
        GridLayoutProjector.ToGridMap(layout, def.gen.gridCellSize, out _, out _);

        var cells = new HashSet<Vector2Int>();
        foreach (TownNode n in layout.nodes)
        {
            Assert.GreaterOrEqual(n.gridCell.x, 0, $"node '{n.name}' cell on-grid");
            Assert.GreaterOrEqual(n.gridCell.y, 0, $"node '{n.name}' cell on-grid");
            cells.Add(n.gridCell);
        }

        // The terminals and stops must not collapse onto each other.
        int stopLike = 0;
        foreach (TownNode n in layout.nodes) if (n.IsStop) stopLike++;
        Assert.GreaterOrEqual(cells.Count, 2, "nodes occupy distinct cells");
        Assert.Greater(stopLike, 0);
    }

    [Test]
    public void StopCells_MatchParsedGrid()
    {
        foreach (int level in ProceduralLevels)
        {
            LevelDefinition levelDef = LevelLibrary.Get(level);
            ProceduralLayoutDefinition def = levelDef.procedural;
            TownLayout layout = TownLayoutGenerator.Generate(def, levelDef.fares, 3);
            string[] map = GridLayoutProjector.ToGridMap(layout, def.gen.gridCellSize, out _, out _);
            GridModel grid = GridModel.Parse(map, out _);

            // Every procedural stop node lands on a 'P' cell, reachable from start.
            foreach (TownNode n in layout.nodes)
            {
                if (n.kind == NodeKind.TerminalStart || n.kind == NodeKind.TerminalEnd || !n.IsStop)
                    continue;
                Assert.AreEqual(GridModel.Cell.Stop, grid.Get(n.gridCell),
                    $"level {level}: stop '{n.name}' should be a 'P' cell");
                Assert.IsTrue(GridPathfinder.Reachable(grid, grid.StartPos, n.gridCell),
                    $"level {level}: stop '{n.name}' must be reachable");
            }
        }
    }
}

/// <summary>EditMode tests for the map-agnostic grid pathfinder.</summary>
public class GridPathfinderTests
{
    static GridModel Parse(params string[] rows) => GridModel.Parse(rows, out _);

    [Test]
    public void Path_FindsShortestStraightRun()
    {
        GridModel grid = Parse(
            "######",
            "#S..D#",
            "######");

        List<Vector2Int> path = GridPathfinder.Path(grid, grid.StartPos, grid.DestPos);
        Assert.IsNotNull(path);
        Assert.AreEqual(4, path.Count, "S,.,.,D is four cells");
        Assert.AreEqual(grid.StartPos, path[0]);
        Assert.AreEqual(grid.DestPos, path[path.Count - 1]);
    }

    [Test]
    public void Path_IsNullWhenUnreachable()
    {
        GridModel grid = Parse(
            "#####",
            "#S#D#",
            "#####");

        Assert.IsNull(GridPathfinder.Path(grid, grid.StartPos, grid.DestPos));
        Assert.IsFalse(GridPathfinder.Reachable(grid, grid.StartPos, grid.DestPos));
    }

    [Test]
    public void ToActions_TurnsAndMovesAlongAnLBend()
    {
        // S at (1,1) facing East; D one right and one down — path turns south.
        GridModel grid = Parse(
            "####",
            "#S.#",
            "##D#",
            "####");

        List<Vector2Int> path = GridPathfinder.Path(grid, grid.StartPos, grid.DestPos);
        List<string> actions = GridPathfinder.ToActions(path, startFacing: 1); // East

        // East one, turn to South, south one.
        CollectionAssert.AreEqual(
            new[] { "moveForward", "turnRight", "moveForward" }, actions);
    }

    [Test]
    public void Path_SameCellReturnsSingleStep()
    {
        GridModel grid = Parse(
            "####",
            "#S.#",
            "#.D#",
            "####");

        List<Vector2Int> path = GridPathfinder.Path(grid, grid.StartPos, grid.StartPos);
        Assert.AreEqual(1, path.Count);
        CollectionAssert.IsEmpty(GridPathfinder.ToActions(path, 1));
    }
}
