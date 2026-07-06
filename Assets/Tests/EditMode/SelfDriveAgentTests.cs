using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Keystone test for the self-driving agent: on the generated Oton town, the
/// autopilot must deliver every rider, collect every fare, and finish at the
/// destination — across many seeds. Mirrors how Level1SolutionTests guards the
/// authored content.
/// </summary>
public class SelfDriveAgentTests
{
    static readonly int[] ProceduralLevels = { 2, 3, 4, 5 };

    class Run
    {
        public GridModel grid;
        public List<GridRide> rides;
        public AutomationPuzzleDefinition def;
        public int facing;
    }

    static Run Build(int seed) => Build(2, seed);

    static Run Build(int level, int seed)
    {
        LevelDefinition levelDef = LevelLibrary.Get(level);
        ProceduralLayoutDefinition pdef = levelDef.procedural;
        TownLayout layout = TownLayoutGenerator.Generate(pdef, levelDef.fares, seed);

        var run = new Run();
        run.def  = SelfDrivePlanner.BuildPuzzle(layout, pdef.gen.gridCellSize, level, out run.rides, out run.facing);
        run.grid = GridModel.Parse(run.def.gridMap, out _);
        return run;
    }

    static AgentSim RunAutopilot(Run r)
    {
        var sim = new AgentSim(r.grid, new FareTable(), r.facing);
        sim.LoadRides(r.rides);

        List<string> plan = SelfDrivePlanner.Plan(r.grid, r.rides, r.facing, r.grid.DestPos);
        foreach (string action in plan) sim.Apply(action);
        return sim;
    }

    [Test]
    public void EverySeed_DeliversEveryRider_CollectsFares_AndReachesDestination()
    {
        foreach (int level in ProceduralLevels)
        {
            for (int seed = 0; seed < 25; seed++)
            {
                Run r = Build(level, seed);
                Assert.Greater(r.rides.Count, 0, $"level {level}, seed {seed}: should have riders");

                AgentSim sim = RunAutopilot(r);

                Assert.IsTrue(sim.IsWin(r.def), $"level {level}, seed {seed}: {sim.DescribeGoalGap(r.def)}");
                Assert.AreEqual(r.rides.Count, sim.PassengersDelivered, $"level {level}, seed {seed}: all delivered");

                int expectedFares = 0;
                foreach (GridRide ride in r.rides) expectedFares += ride.fare;
                Assert.AreEqual(expectedFares, sim.FaresCollected, $"level {level}, seed {seed}: every fare collected");
            }
        }
    }

    [Test]
    public void ParSteps_EqualTheAutopilotPlanLength()
    {
        for (int seed = 0; seed < 15; seed++)
        {
            Run r = Build(seed);
            AgentSim sim = RunAutopilot(r);
            Assert.AreEqual(r.def.parSteps, sim.StepsUsed,
                $"seed {seed}: par should match the autopilot's plan length");
        }
    }

    [Test]
    public void ReferenceSolution_Compiles()
    {
        foreach (int level in ProceduralLevels)
        {
            Parser.Compile(SelfDrivePlanner.TemplateForLevel(level).optimalSolutionText, out var errors);
            CollectionAssert.IsEmpty(errors, $"level {level} displayed reference solution must compile");
        }
    }

    // The function-structured autopilot (def drive()/handlePassengers()/handleFares()/
    // handleDropoffs()) must actually run to a win — calling helpers as statements lets
    // their actions yield across ticks, so the riding logic executes for real.
    [Test]
    public void ReferenceSolution_FunctionStructured_DrivesEverySeedToAWin()
    {
        foreach (int level in ProceduralLevels)
        {
            LevelDefinition levelDef = LevelLibrary.Get(level);
            ProceduralLayoutDefinition pdef = levelDef.procedural;
            ProgramNode program = Parser.Compile(SelfDrivePlanner.TemplateForLevel(level).optimalSolutionText, out var errors);
            CollectionAssert.IsEmpty(errors, $"level {level} compile errors");

            for (int seed = 0; seed < 12; seed++)
            {
                TownLayout layout = TownLayoutGenerator.Generate(pdef, levelDef.fares, seed);
                AutomationPuzzleDefinition def = SelfDrivePlanner.BuildPuzzle(
                    layout, pdef.gen.gridCellSize, level, out List<GridRide> rides, out int facing);
                GridModel grid = GridModel.Parse(def.gridMap, out _);
                var sim = new AgentSim(grid, levelDef.fares, facing);
                sim.LoadRides(rides);

                Assert.IsTrue(HeadlessProgramRunner.Verify(program, sim, def, out string gap),
                    $"level {level}, seed {seed}: {gap}");
            }
        }
    }

    [Test]
    public void ReferenceSolution_DodgesTrafficWhenCarIsInFront()
    {
        string[] map =
        {
            "#######",
            "#...D.#",
            "#S....#",
            "#.....#",
            "#######",
        };
        GridModel grid = GridModel.Parse(map, out _);
        var def = new AutomationPuzzleDefinition
        {
            endlessRoute = true,
            requireAllPassengersDelivered = false,
        };
        var sim = new AgentSim(grid, new FareTable(), 1)
        {
            EndlessRoute = true,
            TrafficEnabled = true,
        };
        sim.LoadRides(new List<GridRide>());
        sim.SetTrafficCells(new[] { new Vector2Int(2, 2), new Vector2Int(4, 1) });

        ProgramNode program = Parser.Compile(SelfDrivePlanner.ReferenceSolution, out var errors);
        CollectionAssert.IsEmpty(errors);

        Assert.IsTrue(HeadlessProgramRunner.VerifyReport(program, sim, def, out RunReport report), report.GoalGap);
        Assert.Greater(report.Trace.Count, 0);
        Assert.AreEqual("moveLeft", report.Trace[0].Action);
        Assert.IsFalse(report.Trace[0].Blocked);
    }

    [Test]
    public void ProceduralPalette_UsesRouteComplete_NotAtDestination()
    {
        Run r = Build(5);

        CollectionAssert.Contains(r.def.allowedQueries, "routeComplete");
        CollectionAssert.DoesNotContain(r.def.allowedQueries, "atDestination");
        CollectionAssert.Contains(r.def.allowedBlocks, "driveToTerminal");
    }

    [Test]
    public void StreamedContinuation_RemapsRides_AndCanCompleteNextRoute()
    {
        ProceduralLayoutDefinition pdef = LevelLibrary.Get(2).procedural;
        var stream = StreamingTownGenerator.Begin(pdef, new FareTable(), 909);
        int oldTerminalId = stream.Layout.destNodeId;

        AutomationPuzzleDefinition def = SelfDrivePlanner.BuildPuzzle(
            stream.Layout, pdef.gen.gridCellSize, out List<GridRide> rides, out int facing);
        GridModel grid = GridModel.Parse(def.gridMap, out _);
        var sim = new AgentSim(grid, new FareTable(), facing);
        sim.LoadRides(rides);

        ProgramNode program = Parser.Compile(SelfDrivePlanner.ReferenceSolution, out var errors);
        CollectionAssert.IsEmpty(errors);
        Assert.IsTrue(HeadlessProgramRunner.Verify(program, sim, def, out string firstGap), firstGap);

        StreamingTownGenerator.AppendChunk(stream);
        AutomationPuzzleDefinition nextDef = SelfDrivePlanner.BuildPuzzle(
            stream.Layout, pdef.gen.gridCellSize, out List<GridRide> nextRides, out int nextFacing);
        TransferState(rides, nextRides);
        GridModel nextGrid = GridModel.Parse(nextDef.gridMap, out _);

        Vector2Int oldTerminalCell = stream.Layout.Node(oldTerminalId).gridCell;
        sim.RebindGrid(nextGrid, oldTerminalCell, sim.Facing, nextRides);

        Assert.IsFalse(sim.EvaluateQuery("routeComplete"),
            "the appended chunk should add a new terminal and optional riders");
        Assert.IsTrue(HeadlessProgramRunner.Verify(program, sim, nextDef, out string nextGap), nextGap);
    }

    // Authored levels (no committed rides) must also be fully autopilot-able: the
    // button is now shown everywhere, synthesizing rides from the grid's 'P' stops.
    [Test]
    public void AuthoredGridWithStops_AutopilotTendsEveryStop_AndReachesDestination()
    {
        string[] map =
        {
            "########",
            "#S....D#",
            "#.####.#",
            "#P....P#",
            "########",
        };
        GridModel grid = GridModel.Parse(map, out _);
        var fares = new FareTable();

        List<GridRide> rides = SelfDrivePlanner.RidesFromGrid(grid, fares);
        Assert.AreEqual(2, rides.Count, "both 'P' stops become rides bound for D");

        var def = new AutomationPuzzleDefinition { requireAllPassengersDelivered = true };
        var sim = new AgentSim(grid, fares, 1);
        sim.LoadRides(rides);

        List<string> plan = SelfDrivePlanner.Plan(grid, rides, 1, grid.DestPos);
        foreach (string action in plan) sim.Apply(action);

        Assert.IsTrue(sim.IsWin(def), sim.DescribeGoalGap(def));
        Assert.AreEqual(2, sim.PassengersDelivered, "both passengers delivered");
        Assert.AreEqual(2 * fares.baseFare, sim.FaresCollected, "both fares collected");
    }

    [Test]
    public void AuthoredGridWithNoStops_AutopilotJustReachesDestination()
    {
        string[] map = { "#####", "#S.D#", "#####" };
        GridModel grid = GridModel.Parse(map, out _);

        List<GridRide> rides = SelfDrivePlanner.RidesFromGrid(grid, new FareTable());
        Assert.AreEqual(0, rides.Count, "no stops → no synthesized rides");

        var def = new AutomationPuzzleDefinition { requireAllPassengersDelivered = true };
        var sim = new AgentSim(grid, new FareTable(), 1);
        sim.LoadRides(rides);

        foreach (string action in SelfDrivePlanner.Plan(grid, rides, 1, grid.DestPos))
            sim.Apply(action);

        Assert.IsTrue(sim.IsWin(def), sim.DescribeGoalGap(def));
    }

    static void TransferState(List<GridRide> oldRides, List<GridRide> newRides)
    {
        var oldById = new Dictionary<int, GridRide>();
        foreach (GridRide ride in oldRides)
            oldById[ride.id] = ride;

        foreach (GridRide ride in newRides)
            if (oldById.TryGetValue(ride.id, out GridRide old))
            {
                ride.aboard    = old.aboard;
                ride.delivered = old.delivered;
                ride.fareCollected = old.fareCollected;
                ride.changeSettled = old.changeSettled;
                ride.paid      = old.paid;
                ride.tender    = old.tender;
            }
    }
}
