using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Shared maze test plumbing: run the canonical wall-follower against a maze and
/// inspect the rendered grid.
/// </summary>
static class MazeTestUtil
{
    /// <summary>Runs the wall-follower to completion; returns the finished sim.</summary>
    public static AgentSim RunWallFollower(AutomationPuzzleDefinition def, out StepResult last)
    {
        ProgramNode program = Parser.Compile(MazeContent.WallFollower, out var compileErrors);
        CollectionAssert.IsEmpty(compileErrors, "wall-follower must compile");

        GridModel grid = GridModel.Parse(def.gridMap, out var mapErrors);
        CollectionAssert.IsEmpty(mapErrors, "maze map must parse cleanly");

        var sim = new AgentSim(grid, new FareTable(), def.startFacing);
        var vm  = new Interpreter();
        vm.Load(program);

        last = null;
        for (int i = 0; i < 5000; i++)
        {
            last = vm.Step(sim);
            if (last.Finished || last.RuntimeError != null) return sim;
            sim.Apply(last.ActionName);
        }

        Assert.Fail("wall-follower never finished within the safety limit");
        return sim;
    }

    /// <summary>Counts open cells, orthogonal open-open edges, and the S/D markers.</summary>
    public static void Inspect(string[] map, out int openCells, out int edges, out int starts, out int dests)
    {
        int h = map.Length;
        openCells = edges = starts = dests = 0;

        bool Open(int x, int y) =>
            y >= 0 && y < h && x >= 0 && x < map[y].Length && map[y][x] != '#';

        for (int y = 0; y < h; y++)
        {
            string row = map[y];
            for (int x = 0; x < row.Length; x++)
            {
                char c = row[x];
                if (c == '#') continue;

                openCells++;
                if (c == 'S') starts++;
                if (c == 'D') dests++;
                if (Open(x + 1, y)) edges++;   // edge to the east
                if (Open(x, y + 1)) edges++;   // edge to the south
            }
        }
    }
}

/// <summary>EditMode tests for the procedural maze generator.</summary>
public class MazeGeneratorTests
{
    [Test]
    public void Generated_ParseCleanly_WithExactlyOneStartAndDest()
    {
        for (int seed = 0; seed < 40; seed++)
        {
            AutomationPuzzleDefinition def = MazeGenerator.Generate(5, 5, seed);

            GridModel.Parse(def.gridMap, out var errors);
            CollectionAssert.IsEmpty(errors, $"seed {seed}");

            MazeTestUtil.Inspect(def.gridMap, out _, out _, out int s, out int d);
            Assert.AreEqual(1, s, $"seed {seed}: one start");
            Assert.AreEqual(1, d, $"seed {seed}: one destination");
        }
    }

    [Test]
    public void Generated_ArePerfectMazes_TreeStructureNoLoops()
    {
        for (int seed = 0; seed < 40; seed++)
        {
            AutomationPuzzleDefinition def = MazeGenerator.Generate(6, 5, seed * 7 + 1);
            MazeTestUtil.Inspect(def.gridMap, out int cells, out int edges, out _, out _);

            // A perfect maze is a spanning tree of its open cells: edges == cells − 1.
            Assert.AreEqual(cells - 1, edges,
                $"seed {seed}: expected a loop-free tree (edges == cells-1)");
        }
    }

    [Test]
    public void Generated_AreSolvableByWallFollower_WithinTheActionGuard()
    {
        foreach (int seed in new[] { 1, 2, 3, 7, 13, 42, 99, 123, 777 })
        {
            foreach ((int c, int r) in new[] { (3, 3), (4, 4), (5, 5), (6, 6), (7, 7) })
            {
                AutomationPuzzleDefinition def = MazeGenerator.Generate(c, r, seed);
                AgentSim sim = MazeTestUtil.RunWallFollower(def, out StepResult last);

                Assert.IsNull(last.RuntimeError,
                    $"seed {seed} {c}x{r}: {(last.RuntimeError != null ? last.RuntimeError.ToString() : "")}");
                Assert.IsTrue(sim.IsWin(def),
                    $"seed {seed} {c}x{r}: the wall-follower should reach D");
            }
        }
    }
}

/// <summary>EditMode tests for the curated Maze Lab series.</summary>
public class MazeLibraryTests
{
    [Test]
    public void Library_HasAnEscalatingSeries()
    {
        Assert.Greater(MazeLibrary.Count, 0);

        for (int i = 0; i < MazeLibrary.Count; i++)
        {
            AutomationPuzzleDefinition def = MazeLibrary.Get(i);
            Assert.IsNotNull(def.gridMap);
            Assert.Greater(def.gridMap.Length, 0, $"maze {i} has a map");
            CollectionAssert.Contains(def.allowedBlocks, "while", $"maze {i} unlocks while");
            Assert.IsFalse(def.requireAllPassengersDelivered, "mazes carry no passengers");
        }
    }

    [Test]
    public void EveryAuthoredMaze_IsSolvedByTheWallFollower()
    {
        for (int i = 0; i < MazeLibrary.Count; i++)
        {
            AutomationPuzzleDefinition def = MazeLibrary.Get(i);
            AgentSim sim = MazeTestUtil.RunWallFollower(def, out StepResult last);

            Assert.IsNull(last.RuntimeError,
                last.RuntimeError != null ? last.RuntimeError.ToString() : null);
            Assert.IsTrue(sim.IsWin(def), $"maze {i} should be solved by the wall-follower");
        }
    }
}
