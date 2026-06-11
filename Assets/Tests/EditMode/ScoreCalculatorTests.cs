using NUnit.Framework;

/// <summary>EditMode tests for the pure score formulas.</summary>
public class ScoreCalculatorTests
{
    // -------------------------------------------------------------------------
    // Automation Mode

    [Test]
    public void AutomationScore_AtPar_NoPenalties_Is1000()
    {
        Assert.AreEqual(1000, ScoreCalculator.AutomationScore(50, 50, 60f, 300f, 0, codeMode: false));
    }

    [Test]
    public void AutomationScore_StepsOverPar_AreDocked()
    {
        Assert.AreEqual(850, ScoreCalculator.AutomationScore(60, 50, 60f, 300f, 0, codeMode: false));
    }

    [Test]
    public void AutomationScore_RetriesAndTimeout_AreDocked()
    {
        Assert.AreEqual(900, ScoreCalculator.AutomationScore(50, 50, 60f, 300f, 2, codeMode: false));
        Assert.AreEqual(850, ScoreCalculator.AutomationScore(50, 50, 400f, 300f, 0, codeMode: false));
    }

    [Test]
    public void AutomationScore_HasAFloorOf100()
    {
        Assert.AreEqual(100, ScoreCalculator.AutomationScore(999, 10, 60f, 300f, 9, codeMode: false));
    }

    [Test]
    public void AutomationScore_CodeMode_Pays50PercentMore()
    {
        int block = ScoreCalculator.AutomationScore(50, 50, 60f, 300f, 0, codeMode: false);
        int code  = ScoreCalculator.AutomationScore(50, 50, 60f, 300f, 0, codeMode: true);
        Assert.AreEqual(1500, code);
        Assert.Greater(code, block);
    }

    [Test]
    public void AutomationScore_DisabledSoftTimer_NeverTimesOut()
    {
        Assert.AreEqual(1000, ScoreCalculator.AutomationScore(50, 50, 9999f, 0f, 0, codeMode: false));
    }

    // -------------------------------------------------------------------------
    // Manual Mode

    [Test]
    public void ManualScore_CleanSlowRun_Is1000()
    {
        Assert.AreEqual(1000, ScoreCalculator.ManualScore(0, 0, 0, 0, 0, false, 300f, 240f));
    }

    [Test]
    public void ManualScore_RewardsFaresSatisfactionAndSpeed()
    {
        // 5 exact fares (+50), 80 satisfaction (+80), 100s under par (+200)
        Assert.AreEqual(1330, ScoreCalculator.ManualScore(5, 0, 0, 0, 80, false, 140f, 240f));
    }

    [Test]
    public void ManualScore_DocksMistakes()
    {
        // 1 wrong (−50), 2 timed out (−50), 1 missed stop (−100), breakdown fumbled (−100)
        Assert.AreEqual(700, ScoreCalculator.ManualScore(0, 1, 2, 1, 0, true, 300f, 240f));
    }

    [Test]
    public void ManualScore_NeverGoesNegative()
    {
        Assert.AreEqual(0, ScoreCalculator.ManualScore(0, 20, 20, 20, 0, true, 999f, 240f));
    }

    // -------------------------------------------------------------------------
    // Currency

    [Test]
    public void CurrencyFor_IsScoreOverTen_FlooredAtZero()
    {
        Assert.AreEqual(100, ScoreCalculator.CurrencyFor(1000));
        Assert.AreEqual(133, ScoreCalculator.CurrencyFor(1330));
        Assert.AreEqual(0,   ScoreCalculator.CurrencyFor(-50));
    }
}
