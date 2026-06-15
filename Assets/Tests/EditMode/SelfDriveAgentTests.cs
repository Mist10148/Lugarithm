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
    class Run
    {
        public GridModel grid;
        public List<GridRide> rides;
        public AutomationPuzzleDefinition def;
        public int facing;
    }

    static Run Build(int seed)
    {
        ProceduralLayoutDefinition pdef = LevelLibrary.Get(2).procedural;
        TownLayout layout = TownLayoutGenerator.Generate(pdef, new FareTable(), seed);

        var run = new Run();
        run.def  = SelfDrivePlanner.BuildPuzzle(layout, pdef.gen.gridCellSize, out run.rides, out run.facing);
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
        for (int seed = 0; seed < 40; seed++)
        {
            Run r = Build(seed);
            Assert.Greater(r.rides.Count, 0, $"seed {seed}: should have riders");

            AgentSim sim = RunAutopilot(r);

            Assert.IsTrue(sim.IsWin(r.def), $"seed {seed}: {sim.DescribeGoalGap(r.def)}");
            Assert.AreEqual(r.rides.Count, sim.PassengersDelivered, $"seed {seed}: all delivered");

            int expectedFares = 0;
            foreach (GridRide ride in r.rides) expectedFares += ride.fare;
            Assert.AreEqual(expectedFares, sim.FaresCollected, $"seed {seed}: every fare collected");
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
        Parser.Compile(SelfDrivePlanner.ReferenceSolution, out var errors);
        CollectionAssert.IsEmpty(errors, "the displayed reference solution must compile");
    }
}
