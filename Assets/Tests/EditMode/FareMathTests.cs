using System;
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>EditMode tests for the pure fare/change arithmetic.</summary>
public class FareMathTests
{
    static FareTable DefaultTable() => new FareTable { baseFare = 13, perStopIncrement = 2 };

    // -------------------------------------------------------------------------
    // Pricing

    [Test]
    public void ComputeFare_FirstStop_IsBaseFare()
    {
        Assert.AreEqual(13, FareMath.ComputeFare(1, DefaultTable()));
    }

    [Test]
    public void ComputeFare_ExtraStops_AddIncrement()
    {
        Assert.AreEqual(17, FareMath.ComputeFare(3, DefaultTable()));
        Assert.AreEqual(21, FareMath.ComputeFare(5, DefaultTable()));
    }

    [Test]
    public void ComputeFare_ZeroOrNegativeStops_ClampsToBase()
    {
        Assert.AreEqual(13, FareMath.ComputeFare(0, DefaultTable()));
        Assert.AreEqual(13, FareMath.ComputeFare(-2, DefaultTable()));
    }

    // -------------------------------------------------------------------------
    // Tender

    [Test]
    public void GenerateTender_IsAlwaysAtLeastTheFare()
    {
        foreach (int fare in new[] { 13, 17, 21, 23, 99 })
        {
            for (int seed = 0; seed < 250; seed++)
            {
                int tender = FareMath.GenerateTender(fare, new Random(seed));
                Assert.GreaterOrEqual(tender, fare, $"fare {fare}, seed {seed}");
            }
        }
    }

    [Test]
    public void GenerateTender_ProducesExactAndBigBills()
    {
        var seen = new HashSet<int>();
        for (int seed = 0; seed < 250; seed++)
            seen.Add(FareMath.GenerateTender(13, new Random(seed)));

        Assert.Contains(13,  new List<int>(seen), "exact tender should occur");
        Assert.Contains(20,  new List<int>(seen), "rounded ₱20 tender should occur");
        Assert.Contains(100, new List<int>(seen), "₱100 bill should occur");
    }

    // -------------------------------------------------------------------------
    // Change

    [Test]
    public void ChangeFor_BasicAndClamped()
    {
        Assert.AreEqual(7, FareMath.ChangeFor(20, 13));
        Assert.AreEqual(0, FareMath.ChangeFor(13, 13));
        Assert.AreEqual(0, FareMath.ChangeFor(10, 13)); // underpayment never owes negative
    }

    [Test]
    public void MakeChange_UsesFewestPieces()
    {
        CollectionAssert.AreEqual(new[] { 20, 10, 5, 1, 1 }, FareMath.MakeChange(37));
        CollectionAssert.AreEqual(new[] { 50, 20, 10, 5, 1, 1 }, FareMath.MakeChange(87));
        CollectionAssert.IsEmpty(FareMath.MakeChange(0));
    }

    [Test]
    public void MakeChange_AlwaysSumsToAmount()
    {
        for (int amount = 0; amount <= 150; amount++)
        {
            int sum = 0;
            foreach (int piece in FareMath.MakeChange(amount))
                sum += piece;
            Assert.AreEqual(amount, sum);
        }
    }

    [Test]
    public void ValidateChange_AcceptsAnyCombinationWithTheRightTotal()
    {
        Assert.IsTrue(FareMath.ValidateChange(new[] { 5, 1, 1 }, 7));
        Assert.IsTrue(FareMath.ValidateChange(new[] { 1, 1, 5 }, 7));
        Assert.IsFalse(FareMath.ValidateChange(new[] { 10 }, 7));
        Assert.IsTrue(FareMath.ValidateChange(new int[0], 0));
        Assert.IsTrue(FareMath.ValidateChange(null, 0));
    }
}
