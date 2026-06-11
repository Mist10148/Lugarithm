using System;
using System.Collections.Generic;

/// <summary>
/// Pure fare arithmetic for the jeepney: fare pricing, the cash a passenger
/// hands over, and change-making against Philippine peso denominations.
/// No UnityEngine dependency — covered by EditMode tests.
/// </summary>
public static class FareMath
{
    /// <summary>
    /// Denomination values the Coin Drawer offers, smallest first.
    /// ₱20 exists as both a coin and a bill; the math only cares about value.
    /// </summary>
    public static readonly int[] Denominations = { 1, 5, 10, 20, 50, 100 };

    // -------------------------------------------------------------------------
    // Pricing

    /// <summary>
    /// Fare for a ride spanning <paramref name="stopsTraveled"/> stops:
    /// the base fare covers the first stop, each extra stop adds the increment.
    /// </summary>
    public static int ComputeFare(int stopsTraveled, FareTable table)
    {
        int extraStops = Math.Max(0, stopsTraveled - 1);
        return table.baseFare + table.perStopIncrement * extraStops;
    }

    // -------------------------------------------------------------------------
    // Tender (what the passenger hands over)

    /// <summary>
    /// Picks a believable cash amount for a passenger paying
    /// <paramref name="fare"/>: exact coins, or a rounded-up ₱20/₱50/₱100.
    /// Always at least the fare.
    /// </summary>
    public static int GenerateTender(int fare, Random rng)
    {
        int roll = rng.Next(100);

        if (roll < 30) return fare;                          // exact change
        if (roll < 60) return RoundUpTo(fare, 20);           // small bill/coin
        if (roll < 85) return Math.Max(50, RoundUpTo(fare, 50));
        return Math.Max(100, RoundUpTo(fare, 100));          // big bill
    }

    static int RoundUpTo(int amount, int unit)
    {
        return ((amount + unit - 1) / unit) * unit;
    }

    // -------------------------------------------------------------------------
    // Change

    /// <summary>Change owed for a fare paid with <paramref name="tender"/>.</summary>
    public static int ChangeFor(int tender, int fare)
    {
        return Math.Max(0, tender - fare);
    }

    /// <summary>
    /// Optimal (fewest pieces) way to hand back <paramref name="amount"/>.
    /// Greedy is exact because peso denominations are canonical.
    /// </summary>
    public static List<int> MakeChange(int amount)
    {
        var pieces = new List<int>();
        for (int i = Denominations.Length - 1; i >= 0; i--)
        {
            int value = Denominations[i];
            while (amount >= value)
            {
                pieces.Add(value);
                amount -= value;
            }
        }
        return pieces;
    }

    /// <summary>
    /// True when the denominations the player picked sum to the change owed.
    /// Any combination with the right total is accepted.
    /// </summary>
    public static bool ValidateChange(IEnumerable<int> selected, int expectedChange)
    {
        int total = 0;
        if (selected != null)
            foreach (int value in selected)
                total += value;
        return total == expectedChange;
    }
}
