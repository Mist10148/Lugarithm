/// <summary>
/// Runs a compiled program to completion synchronously against an <see cref="AgentSim"/>,
/// with no view, coroutine clock, or playback delay. It mirrors the semantics of
/// <see cref="ExecutionController.ExecutionLoop"/> exactly — drain nav-macro moves, step the
/// <see cref="Interpreter"/>, apply each action, deliver value-returning results, and stop on
/// finish / runtime error / win — so a dry run reaches the same outcome the player would see.
///
/// The Vibe agent uses this to check that a generated program actually solves the maze before
/// dropping it into the editor; on failure it feeds the goal gap back for a logical repair.
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
        goalGap = null;
        if (program == null || sim == null)
        {
            goalGap = "there was no program to run.";
            return false;
        }

        var vm = new Interpreter();
        vm.Load(program);

        for (int turn = 0; turn < MaxTurns; turn++)
        {
            // A nav macro (driveToNextStop / driveToDestination) plans primitive moves into
            // the sim's queue; drain them one per turn, exactly like the live loop.
            if (sim.HasPendingMoves)
            {
                sim.Apply(sim.DequeueMove());
                if (sim.IsWin(def)) return true;
                continue;
            }

            StepResult step = vm.Step(sim);

            if (step.RuntimeError != null)
            {
                goalGap = step.RuntimeError.Message;
                return false;
            }

            if (step.Finished)
            {
                if (sim.IsWin(def)) return true;
                goalGap = sim.DescribeGoalGap(def) ?? "the program ended without reaching the goal.";
                return false;
            }

            AgentActionResult result = sim.Apply(step.ActionName);
            if (!string.IsNullOrEmpty(step.BindResultTo))
                vm.DeliverActionResult(result.ReturnValue);

            if (sim.IsWin(def)) return true;
        }

        goalGap = "the program ran too long without reaching the goal.";
        return false;
    }
}
