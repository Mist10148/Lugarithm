using System.Collections.Generic;
using UnityEngine;

/// <summary>One primitive step of a headless run, recorded for diagnostics: where the jeepney
/// was, which way it faced, and what the action did. Lets the co-pilot reason about the actual
/// path (never moved, blocked early, looped over the same cell) rather than only the end state.</summary>
public sealed class TraceStep
{
    public string     Action;
    public Vector2Int Pos;
    public int        Facing;
    public bool       Blocked;
    public bool       PickedUp;
    public bool       DroppedOff;
}

/// <summary>The full outcome of a headless dry run — win/goal-gap plus the telemetry the
/// diagnostics and hint layers need: semantic action count, delivered-of-total, final pose,
/// whether a runtime error tripped, and the per-step trace.</summary>
public sealed class RunReport
{
    public bool            Win;
    public string          GoalGap;
    public int             Steps;            // interpreter ActionsExecuted (semantic statements run)
    public int             DeliveredCount;
    public int             TotalPassengers;
    public Vector2Int      FinalPos;
    public int             FinalFacing;
    public bool            RuntimeErrored;
    public List<TraceStep> Trace = new List<TraceStep>();
}

/// <summary>
/// Runs a compiled program to completion synchronously against an <see cref="AgentSim"/>,
/// with no view, coroutine clock, or playback delay. It mirrors the semantics of
/// <see cref="ExecutionController.ExecutionLoop"/> exactly — drain nav-macro moves, step the
/// <see cref="Interpreter"/>, apply each action, deliver value-returning results, and stop on
/// finish / runtime error / win — so a dry run reaches the same outcome the player would see.
///
/// The Vibe agent uses this to check that a generated program actually solves the maze before
/// dropping it into the editor; on failure it feeds the goal gap back for a logical repair.
/// The <see cref="RunReport"/> overload additionally records telemetry for the diagnostics layer.
/// </summary>
public static class HeadlessProgramRunner
{
    // Defensive ceiling on loop turns. The interpreter's own MaxActions/MaxEvaluations guards
    // already trip infinite programs; this only bounds the surrounding drain/step loop so a
    // pathological nav-macro queue can never spin forever.
    const int MaxTurns = 200000;

    /// <summary>Runs <paramref name="program"/> on <paramref name="sim"/> and reports whether
    /// the puzzle's win condition was met. On failure, <paramref name="goalGap"/> carries a
    /// plain-English reason (a runtime error message, or the sim's goal-gap description).</summary>
    public static bool Verify(ProgramNode program, AgentSim sim, AutomationPuzzleDefinition def,
                              out string goalGap)
    {
        bool win = VerifyReport(program, sim, def, out RunReport report);
        goalGap = report.GoalGap;
        return win;
    }

    /// <summary>As <see cref="Verify(ProgramNode,AgentSim,AutomationPuzzleDefinition,out string)"/>,
    /// but fills a full <see cref="RunReport"/> with the per-step trace and run telemetry. A distinct
    /// name (not an overload) so callers that discard with <c>out _</c> stay unambiguous.</summary>
    public static bool VerifyReport(ProgramNode program, AgentSim sim, AutomationPuzzleDefinition def,
                                    out RunReport report)
    {
        report = new RunReport();
        if (program == null || sim == null)
        {
            report.GoalGap = "there was no program to run.";
            return false;
        }

        var vm = new Interpreter();
        vm.Load(program);

        bool won = false;
        for (int turn = 0; turn < MaxTurns; turn++)
        {
            // A nav macro (driveToNextStop / driveToDestination) plans primitive moves into
            // the sim's queue; drain them one per turn, exactly like the live loop.
            if (sim.HasPendingMoves)
            {
                Record(report, sim.Apply(sim.DequeueMove()), sim);
                if (sim.IsWin(def)) { won = true; break; }
                continue;
            }

            StepResult step = vm.Step(sim);

            if (step.RuntimeError != null)
            {
                report.RuntimeErrored = true;
                report.GoalGap = step.RuntimeError.Message;
                break;
            }

            if (step.Finished)
            {
                if (sim.IsWin(def)) { won = true; }
                else report.GoalGap = sim.DescribeGoalGap(def) ?? "the program ended without reaching the goal.";
                break;
            }

            AgentActionResult result = sim.Apply(step.ActionName, step.ActionArgs);
            Record(report, result, sim);
            if (!string.IsNullOrEmpty(step.BindResultTo))
                vm.DeliverActionResult(result.ReturnValue);

            if (sim.IsWin(def)) { won = true; break; }

            if (turn == MaxTurns - 1)
                report.GoalGap = "the program ran too long without reaching the goal.";
        }

        report.Win             = won;
        report.Steps           = vm.ActionsExecuted;
        report.DeliveredCount  = sim.PassengersDelivered;
        report.TotalPassengers = sim.TotalPassengers;
        report.FinalPos        = sim.Position;
        report.FinalFacing     = sim.Facing;
        if (won) report.GoalGap = null;
        return won;
    }

    static void Record(RunReport report, AgentActionResult r, AgentSim sim)
    {
        report.Trace.Add(new TraceStep
        {
            Action     = r.Action,
            Pos        = sim.Position,
            Facing     = sim.Facing,
            Blocked    = r.Blocked,
            PickedUp   = r.PickedUp,
            DroppedOff = r.DroppedOff,
        });
    }
}
