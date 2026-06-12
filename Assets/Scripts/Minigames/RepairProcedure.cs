using System.Collections.Generic;

/// <summary>Which system failed mid-drive.</summary>
public enum BreakdownFault { Engine, Fuel }

/// <summary>
/// Pure definition of the code-based repair minigames: the correct ordered
/// sequence of instruction "blocks" for each fault, plus validation of a
/// player's candidate ordering. No Unity dependencies — EditMode tests verify
/// the procedures and the first-wrong-index logic.
/// </summary>
public static class RepairProcedure
{
    static readonly string[] EngineSteps =
    {
        "releasePressure()",
        "openHood()",
        "drainCoolant()",
        "replaceBelt()",
        "refillCoolant()",
        "closeHood()",
    };

    static readonly string[] FuelSteps =
    {
        "cutEngine()",
        "openCap()",
        "insertNozzle()",
        "pump()",
        "removeNozzle()",
        "closeCap()",
    };

    // -------------------------------------------------------------------------

    /// <summary>The correct ordering of instruction blocks for a fault.</summary>
    public static string[] Steps(BreakdownFault fault)
    {
        string[] source = fault == BreakdownFault.Fuel ? FuelSteps : EngineSteps;
        return (string[])source.Clone();
    }

    public static int StepCount(BreakdownFault fault) =>
        fault == BreakdownFault.Fuel ? FuelSteps.Length : EngineSteps.Length;

    public static string Title(BreakdownFault fault) =>
        fault == BreakdownFault.Fuel
            ? "FUEL SYSTEM — order the refuel steps:"
            : "ENGINE TROUBLE — order the repair steps:";

    // -------------------------------------------------------------------------

    /// <summary>
    /// Index of the first block the player has out of place, or -1 when the whole
    /// candidate matches the correct order.
    /// </summary>
    public static int FirstWrongIndex(IList<string> candidate, BreakdownFault fault)
    {
        string[] correct = fault == BreakdownFault.Fuel ? FuelSteps : EngineSteps;
        if (candidate == null) return 0;

        int n = correct.Length;
        for (int i = 0; i < n; i++)
        {
            if (i >= candidate.Count) return i;
            if (candidate[i] != correct[i]) return i;
        }
        return candidate.Count == n ? -1 : n;
    }

    public static bool IsCorrect(IList<string> candidate, BreakdownFault fault) =>
        FirstWrongIndex(candidate, fault) < 0;
}
