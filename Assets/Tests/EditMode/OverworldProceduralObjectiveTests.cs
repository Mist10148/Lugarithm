using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class OverworldProceduralObjectiveTests
{
    [Test]
    public void ProceduralMaze_IsDeterministicReachableAndTierSized()
    {
        for (int level = 0; level <= 5; level++)
            for (int seed = 0; seed < 100; seed++)
            {
                MazeLayout first = OverworldPuzzleGenerator.GenerateMaze(level, seed);
                MazeLayout second = OverworldPuzzleGenerator.GenerateMaze(level, seed);
                Assert.AreEqual(OverworldPuzzleTuning.GridSize(level), first.width);
                Assert.AreEqual(first.width, first.height);
                Assert.IsTrue(OverworldPuzzleGenerator.MazeReachable(first), $"level {level}, seed {seed}");
                Assert.AreEqual(MazeSignature(first), MazeSignature(second));
            }
    }

    [Test]
    public void ProceduralFill_HasKnownContinuousAllCellSolution()
    {
        for (int level = 0; level <= 5; level++)
            for (int seed = 0; seed < 100; seed++)
            {
                FillLayout layout = OverworldPuzzleGenerator.GenerateFill(level, seed);
                FillLayout repeated = OverworldPuzzleGenerator.GenerateFill(level, seed);
                var seen = new HashSet<Vector2Int>();
                for (int i = 0; i < layout.solution.Length; i++)
                {
                    Vector2Int cell = layout.solution[i];
                    Assert.IsTrue(layout.active[cell.y, cell.x]);
                    Assert.IsTrue(seen.Add(cell), $"repeat at {cell}, level {level}, seed {seed}");
                    if (i > 0) Assert.AreEqual(1, Manhattan(layout.solution[i - 1], cell));
                }
                Assert.AreEqual(ActiveCount(layout), layout.solution.Length);
                CollectionAssert.AreEqual(layout.solution, repeated.solution);
            }
    }

    [Test]
    public void ProceduralFlow_ReferenceSolutionAlwaysSolves()
    {
        for (int level = 0; level <= 5; level++)
            for (int seed = 0; seed < 100; seed++)
            {
                FlowConnectLayout layout = FlowConnectLayouts.Generate(level, seed);
                Assert.AreEqual(OverworldPuzzleTuning.FlowPairs(level), layout.Pairs.Length);
                var board = new FlowConnectBoard(layout.Width, layout.Height, layout.Pairs);
                for (int color = 0; color < layout.Solution.Length; color++)
                {
                    Vector2Int[] path = layout.Solution[color];
                    Assert.IsTrue(board.Start(color, path[0]));
                    for (int i = 1; i < path.Length; i++) Assert.IsTrue(board.Extend(color, path[i]));
                }
                Assert.IsTrue(board.IsSolved(), $"level {level}, seed {seed}");
            }
    }

    [Test]
    public void RouteRotation_StartsScrambledAndKnownRotationsSolve()
    {
        for (int level = 0; level <= 5; level++)
            for (int seed = 0; seed < 100; seed++)
            {
                RouteRotationLayout layout = RouteRotationGenerator.Generate(level, seed);
                var board = new RouteRotationBoard(layout);
                Assert.IsFalse(board.IsSolved(), $"initially solved: level {level}, seed {seed}");
                for (int y = 0; y < layout.height; y++)
                    for (int x = 0; x < layout.width; x++)
                        if (layout.colors[y, x] >= 0)
                            board.Rotate(x, y, 4 - layout.rotations[y, x]);
                Assert.IsTrue(board.IsSolved(), $"level {level}, seed {seed}");
            }
    }

    [Test]
    public void Crates_AreTierSizedDeterministicAndNotInitiallySolved()
    {
        for (int level = 0; level <= 5; level++)
            for (int seed = 0; seed < 100; seed++)
            {
                int count = OverworldPuzzleTuning.CrateCount(level);
                var first = new CrateStackPuzzle(count, seed);
                var second = new CrateStackPuzzle(count, seed);
                Assert.AreEqual(count, first.Count);
                Assert.IsFalse(first.IsSolved());
                CollectionAssert.AreEqual(first.Order, second.Order);
            }
    }

    [Test]
    public void EveryCodeOrderSolution_IsValidPara()
    {
        for (int level = 0; level <= 5; level++)
        {
            MinigameStationDef def = System.Array.Find(
                TownMinigameLibrary.ForLevel(level), station => station.kind == MinigamePuzzleKind.Coding);
            Assert.IsNotNull(def);
            CodingPuzzle puzzle = OverworldPuzzleLibrary.GetCoding(def.id, def.concept);
            string source = CodeRunHistory.SourceFromLines(puzzle.orderedLines);
            ProgramNode program = Parser.Compile(source, out List<LangError> errors);
            Assert.IsNotNull(program);
            CollectionAssert.IsEmpty(errors, $"level {level}:\n{source}");
        }
    }

    [Test]
    public void CrateMoveTo_SupportsDragStyleReordering()
    {
        var puzzle = new CrateStackPuzzle(5, 7);
        int moved = puzzle.Order[0];
        Assert.IsTrue(puzzle.MoveTo(0, 4));
        Assert.AreEqual(moved, puzzle.Order[4]);
        Assert.IsFalse(puzzle.MoveTo(4, 4));
    }

    static string MazeSignature(MazeLayout maze)
    {
        var chars = new char[maze.width * maze.height];
        for (int y = 0; y < maze.height; y++)
            for (int x = 0; x < maze.width; x++) chars[y * maze.width + x] = maze.wall[y, x] ? '#' : '.';
        return new string(chars);
    }

    static int ActiveCount(FillLayout layout)
    {
        int count = 0;
        for (int y = 0; y < layout.height; y++)
            for (int x = 0; x < layout.width; x++) if (layout.active[y, x]) count++;
        return count;
    }

    static int Manhattan(Vector2Int a, Vector2Int b)
        => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
}
