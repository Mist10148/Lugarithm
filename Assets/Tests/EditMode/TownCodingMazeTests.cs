using System.Collections.Generic;
using NUnit.Framework;

public class TownCodingMazeTests
{
    [Test]
    public void TutorialCodingMaze_IsStraightLine()
    {
        AutomationPuzzleDefinition def = TownCodingMazeLibrary.ForLevel(0);

        Assert.AreEqual(1, def.gridMap.Length);
        Assert.AreEqual("S..D", def.gridMap[0]);
        Assert.AreEqual(1, def.startFacing);
        CollectionAssert.AreEqual(new[] { "moveForward" }, def.allowedBlocks);
        Assert.AreEqual(3, def.parSteps);
    }

    [Test]
    public void TutorialCodingMaze_SolvesWithOnlyMoveForward()
    {
        AutomationPuzzleDefinition def = TownCodingMazeLibrary.ForLevel(0);
        GridModel grid = GridModel.Parse(def.gridMap, out List<string> mapErrors);
        Assert.IsEmpty(mapErrors);

        ProgramNode program = Parser.Compile(def.optimalSolutionText, out List<LangError> errors);
        Assert.IsEmpty(errors);

        var sim = new AgentSim(grid, new FareTable(), def.startFacing);
        Assert.IsTrue(HeadlessProgramRunner.Verify(program, sim, def, out string gap), gap);
    }

    [Test]
    public void LaterCodingMazes_UseMazeVocabularyAndWallFollowerSolves()
    {
        for (int level = 1; level <= 5; level++)
        {
            AutomationPuzzleDefinition def = TownCodingMazeLibrary.ForLevel(level);
            GridModel grid = GridModel.Parse(def.gridMap, out List<string> mapErrors);
            Assert.IsEmpty(mapErrors, $"level {level} map errors");

            CollectionAssert.Contains(def.allowedBlocks, "while", $"level {level} allows loops");
            CollectionAssert.Contains(def.allowedQueries, "frontIsClear", $"level {level} allows maze queries");

            ProgramNode program = Parser.Compile(MazeContent.WallFollower, out List<LangError> errors);
            Assert.IsEmpty(errors, $"level {level} compile errors");

            var sim = new AgentSim(grid, new FareTable(), def.startFacing);
            Assert.IsTrue(HeadlessProgramRunner.Verify(program, sim, def, out string gap),
                $"level {level}: {gap}");
        }
    }
}
