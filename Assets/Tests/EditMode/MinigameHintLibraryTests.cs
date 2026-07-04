using NUnit.Framework;

public class MinigameHintLibraryTests
{
    [Test]
    public void ClampTier_StaysInsideAvailableHints()
    {
        Assert.AreEqual(0, MinigameHintLibrary.ClampTier(-5, 3));
        Assert.AreEqual(1, MinigameHintLibrary.ClampTier(1, 3));
        Assert.AreEqual(2, MinigameHintLibrary.ClampTier(99, 3));
    }

    [Test]
    public void RepairOrderHint_UsesFaultAndWrongStep()
    {
        string early = MinigameHintLibrary.RepairOrderHint(0, BreakdownFault.Fuel, -1);
        StringAssert.Contains("fuel", early.ToLowerInvariant());

        string late = MinigameHintLibrary.RepairOrderHint(2, BreakdownFault.Engine, 3);
        StringAssert.Contains("step 4", late.ToLowerInvariant());
    }

    [Test]
    public void CodeOrderHint_ReferencesWrongLineOnlyOnLateTier()
    {
        string early = MinigameHintLibrary.CodeOrderHint(0, 2);
        Assert.IsFalse(early.ToLowerInvariant().Contains("line 3"));

        string late = MinigameHintLibrary.CodeOrderHint(2, 2);
        StringAssert.Contains("line 3", late.ToLowerInvariant());
    }

    [Test]
    public void HintSets_HaveThreeTiers()
    {
        Assert.AreEqual(MinigameHintLibrary.HintTierCount, MinigameHintLibrary.MazeHints.Length);
        Assert.AreEqual(MinigameHintLibrary.HintTierCount, MinigameHintLibrary.RepairOrderHints.Length);
        Assert.AreEqual(MinigameHintLibrary.HintTierCount, MinigameHintLibrary.CodeOrderHints.Length);
    }
}
