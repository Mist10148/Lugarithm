using NUnit.Framework;

/// <summary>EditMode tests for the pure refuel-gauge arithmetic.</summary>
public class RefuelMathTests
{
    [Test]
    public void Target_IsAValidLandableBand()
    {
        for (int seed = 0; seed < 200; seed++)
        {
            RefuelMath.Target(new System.Random(seed), out float lo, out float hi);

            Assert.Greater(hi, lo, $"seed {seed}");
            Assert.GreaterOrEqual(lo, 0f);
            Assert.LessOrEqual(hi, 1f);

            float tap = RefuelMath.TapAmount(new System.Random(seed));
            Assert.Less(tap, hi - lo, "a tap must fit inside the band so it's landable from below");
        }
    }

    [Test]
    public void StartFill_BeginsNearEmpty()
    {
        for (int seed = 0; seed < 100; seed++)
        {
            float start = RefuelMath.StartFill(new System.Random(seed));
            Assert.GreaterOrEqual(start, 0f);
            Assert.Less(start, 0.5f, "the tank starts low");
        }
    }

    [Test]
    public void ScoreFor_InBand_IsFullMarks()
    {
        Assert.AreEqual(100, RefuelMath.ScoreFor(0.60f, 0.55f, 0.70f, timedOut: false));
        Assert.AreEqual(60,  RefuelMath.ScoreFor(0.60f, 0.55f, 0.70f, timedOut: true));
    }

    [Test]
    public void ScoreFor_AtBandEdges_IsFullMarks()
    {
        Assert.AreEqual(100, RefuelMath.ScoreFor(0.55f, 0.55f, 0.70f, false));
        Assert.AreEqual(100, RefuelMath.ScoreFor(0.70f, 0.55f, 0.70f, false));
    }

    [Test]
    public void ScoreFor_OutsideBand_DentsByDistance_FlooredAtTen()
    {
        int under = RefuelMath.ScoreFor(0.40f, 0.55f, 0.70f, false);
        Assert.Less(under, 100);
        Assert.GreaterOrEqual(under, 10);

        int over = RefuelMath.ScoreFor(1.00f, 0.55f, 0.70f, false);
        Assert.Less(over, 100);

        int wayOff = RefuelMath.ScoreFor(0.00f, 0.55f, 0.70f, false);
        Assert.AreEqual(10, wayOff, "a huge miss floors at 10 — never a fail state");
    }

    [Test]
    public void InBand_IncludesBoundaries()
    {
        Assert.IsTrue(RefuelMath.InBand(0.55f, 0.55f, 0.70f));
        Assert.IsTrue(RefuelMath.InBand(0.70f, 0.55f, 0.70f));
        Assert.IsFalse(RefuelMath.InBand(0.54f, 0.55f, 0.70f));
        Assert.IsFalse(RefuelMath.InBand(0.71f, 0.55f, 0.70f));
    }

    [Test]
    public void CostForScore_Perfect_IsMinimum()
    {
        Assert.AreEqual(15, RefuelMath.CostForScore(100));
    }

    [Test]
    public void CostForScore_TimedOutInBand_ScalesFromScore()
    {
        int score = RefuelMath.ScoreFor(0.60f, 0.55f, 0.70f, timedOut: true);

        Assert.AreEqual(60, score);
        Assert.AreEqual(35, RefuelMath.CostForScore(score));
    }

    [Test]
    public void CostForScore_LowScore_IsCapped()
    {
        Assert.AreEqual(60, RefuelMath.CostForScore(10));
        Assert.AreEqual(60, RefuelMath.CostForScore(-50));
    }
}
