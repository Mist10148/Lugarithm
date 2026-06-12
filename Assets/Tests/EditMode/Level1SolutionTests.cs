using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// Keystone content tests: the canonical solutions stored in
/// <see cref="LevelLibrary"/> must actually solve their own maps, within par.
/// If a map or solution is mis-authored, these fail in CI instead of in a
/// playtest.
/// </summary>
public class Level1SolutionTests
{
    /// <summary>Compiles and runs a solution against a puzzle, start to finish.</summary>
    static AgentSim RunSolution(AutomationPuzzleDefinition def, out StepResult last)
    {
        var program = Parser.Compile(def.optimalSolutionText, out var compileErrors);
        CollectionAssert.IsEmpty(compileErrors, "canonical solution must compile");

        var grid = GridModel.Parse(def.gridMap, out var mapErrors);
        CollectionAssert.IsEmpty(mapErrors, "map must parse cleanly");

        var sim = new AgentSim(grid, new FareTable(), def.startFacing);
        var vm  = new Interpreter();
        vm.Load(program);

        last = null;
        for (int i = 0; i < 5000; i++)
        {
            last = vm.Step(sim);
            if (last.Finished) return sim;
            sim.Apply(last.ActionName);
        }

        Assert.Fail("solution never finished within the safety limit");
        return sim;
    }

    // -------------------------------------------------------------------------
    // Maps

    [Test]
    public void TutorialMap_ParsesCleanly()
    {
        GridModel.Parse(LevelLibrary.Get(0).auto.gridMap, out var errors);
        CollectionAssert.IsEmpty(errors);
    }

    [Test]
    public void Level1Map_ParsesCleanly()
    {
        var grid = GridModel.Parse(LevelLibrary.Get(1).auto.gridMap, out var errors);

        CollectionAssert.IsEmpty(errors);
        Assert.AreEqual(2, grid.StopCells.Count, "Level 1 should have two passenger stops");
    }

    // -------------------------------------------------------------------------
    // Tutorial solution

    [Test]
    public void TutorialSolution_WinsExactlyAtPar()
    {
        var def = LevelLibrary.Get(0).auto;
        var sim = RunSolution(def, out var last);

        Assert.IsNull(last.RuntimeError);
        Assert.IsTrue(sim.IsWin(def), sim.DescribeGoalGap(def) ?? "should have won");
        Assert.AreEqual(1, sim.PassengersDelivered);
        Assert.AreEqual(def.parSteps, sim.StepsUsed,
            "par should equal the canonical solution's step count");
    }

    // -------------------------------------------------------------------------
    // Level 1 (Molo) solution — right-hand-rule maze

    [Test]
    public void Level1Solution_WinsExactlyAtPar()
    {
        var def = LevelLibrary.Get(1).auto;
        var sim = RunSolution(def, out var last);

        Assert.IsNull(last.RuntimeError,
            last.RuntimeError != null ? last.RuntimeError.ToString() : null);
        Assert.IsTrue(sim.IsWin(def), sim.DescribeGoalGap(def) ?? "should have won");
        Assert.AreEqual(2, sim.PassengersDelivered, "both passengers must be delivered");
        Assert.AreEqual(26, sim.FaresCollected, "two base fares of ₱13 must be collected");
        Assert.AreEqual(def.parSteps, sim.StepsUsed,
            "par should equal the canonical solution's step count");
    }

    [Test]
    public void Level1Solution_StaysWellUnderTheLoopGuard()
    {
        var def = LevelLibrary.Get(1).auto;
        RunSolution(def, out var last);

        Assert.IsTrue(last.Finished);
        Assert.IsNull(last.RuntimeError);
    }

    // -------------------------------------------------------------------------
    // Level 2 (Oton) — code gate is an authored maze, solved by the wall-follower

    [Test]
    public void OtonMaze_UsesAuthoredGrid_AndIsSolvedByItsSolution()
    {
        var def = LevelLibrary.Get(2).auto;
        Assert.IsTrue(def.useAuthoredGrid, "Oton must use its authored maze grid, not a route mirror");

        var sim = RunSolution(def, out var last);

        Assert.IsNull(last.RuntimeError,
            last.RuntimeError != null ? last.RuntimeError.ToString() : null);
        Assert.IsTrue(sim.IsWin(def), sim.DescribeGoalGap(def) ?? "Oton maze should be solved");
    }

    [Test]
    public void Oton_HasACrateStackTownGate()
    {
        Assert.AreEqual(TownPuzzleKind.CrateStack, LevelLibrary.Get(2).townPuzzle);
    }

    // -------------------------------------------------------------------------
    // Library shape

    [Test]
    public void Library_HasSixLevels_WithTutorialAndLevel1Playable()
    {
        Assert.AreEqual(6, LevelLibrary.Count);
        Assert.AreEqual(ProgressionRules.LevelCount, LevelLibrary.Count);

        for (int i = 0; i < LevelLibrary.Count; i++)
        {
            var def = LevelLibrary.Get(i);
            Assert.AreEqual(i, def.levelIndex);
            Assert.IsFalse(string.IsNullOrEmpty(def.displayName));
        }

        Assert.IsTrue(LevelLibrary.Get(0).hasContent);
        Assert.IsTrue(LevelLibrary.Get(1).hasContent);
        Assert.IsTrue(LevelLibrary.Get(2).hasContent);   // Oton is now playable
        Assert.IsFalse(LevelLibrary.Get(3).hasContent);
    }

    [Test]
    public void PlayableLevels_HaveManualRoutesWithOneDestination()
    {
        foreach (int i in new[] { 0, 1, 2 })
        {
            ManualRouteDefinition route = LevelLibrary.Get(i).manual;
            Assert.IsNotNull(route, $"level {i} needs a manual route");
            Assert.GreaterOrEqual(route.waypoints.Length, 2);

            int destinations = 0;
            foreach (ManualStopDefinition stop in route.stops)
            {
                Assert.IsTrue(stop.waypointIndex >= 0 && stop.waypointIndex < route.waypoints.Length,
                    $"stop '{stop.stopName}' points at a missing waypoint");
                if (stop.isDestination) destinations++;
            }

            Assert.AreEqual(1, destinations, $"level {i} needs exactly one destination stop");
        }
    }
}
