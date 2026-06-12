using System.Collections.Generic;
using NUnit.Framework;

/// <summary>EditMode tests for the code-fix repair procedures + validation.</summary>
public class RepairProcedureTests
{
    static readonly BreakdownFault[] Faults = { BreakdownFault.Engine, BreakdownFault.Fuel };

    [Test]
    public void Steps_AreSixUnique_PerFault()
    {
        foreach (BreakdownFault fault in Faults)
        {
            string[] steps = RepairProcedure.Steps(fault);
            Assert.AreEqual(6, steps.Length, $"{fault}");
            Assert.AreEqual(6, new HashSet<string>(steps).Count, $"{fault} steps must be unique");
            Assert.AreEqual(steps.Length, RepairProcedure.StepCount(fault));
        }
    }

    [Test]
    public void Engine_And_Fuel_Procedures_Differ()
    {
        CollectionAssert.AreNotEqual(RepairProcedure.Steps(BreakdownFault.Engine),
                                     RepairProcedure.Steps(BreakdownFault.Fuel));
    }

    [Test]
    public void CorrectOrder_Validates()
    {
        foreach (BreakdownFault fault in Faults)
        {
            string[] correct = RepairProcedure.Steps(fault);
            Assert.AreEqual(-1, RepairProcedure.FirstWrongIndex(correct, fault));
            Assert.IsTrue(RepairProcedure.IsCorrect(correct, fault));
        }
    }

    [Test]
    public void FirstWrongIndex_PointsAtTheFirstMisplacedStep()
    {
        BreakdownFault fault = BreakdownFault.Engine;
        var candidate = new List<string>(RepairProcedure.Steps(fault));
        (candidate[2], candidate[3]) = (candidate[3], candidate[2]);

        Assert.AreEqual(2, RepairProcedure.FirstWrongIndex(candidate, fault));
        Assert.IsFalse(RepairProcedure.IsCorrect(candidate, fault));
    }

    [Test]
    public void NullOrShortCandidate_IsWrong()
    {
        BreakdownFault fault = BreakdownFault.Fuel;
        Assert.AreEqual(0, RepairProcedure.FirstWrongIndex(null, fault));

        string[] correct = RepairProcedure.Steps(fault);
        var twoCorrect = new List<string> { correct[0], correct[1] };
        Assert.AreEqual(2, RepairProcedure.FirstWrongIndex(twoCorrect, fault),
            "first two right, then the rest are missing");
    }
}
