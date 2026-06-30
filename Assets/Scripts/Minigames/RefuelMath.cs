using System;
using UnityEngine;

/// <summary>
/// Pure arithmetic for the non-code Refuel minigame: where the target fuel band
/// sits, how much one pump tap adds, and how a final fill level scores. No Unity
/// scene dependencies, so EditMode tests pin the boundaries.
/// </summary>
public static class RefuelMath
{
    /// <summary>Tank runs 0 (empty) .. 1 (full).</summary>
    public const float TankCapacity = 1f;

    /// <summary>
    /// Shared fuel-drain rate (fraction of a full tank per real second of active
    /// driving in Manual Mode, or per simulated step-second in Automation Mode).
    /// Single source of truth so the two modes can't drift apart again.
    /// </summary>
    public const float FuelDrainPerSecond = 0.006f;

    // Band is always landable: its width is wider than the largest pump tap, so
    // there is always a tap that lands inside it coming from below.
    const float BandWidth   = 0.16f;
    const float BandLoMin   = 0.52f;
    const float BandLoMax   = 0.74f;

    const float TapMin      = 0.06f;
    const float TapMax      = 0.10f;

    const float StartMin    = 0.06f;
    const float StartMax    = 0.20f;

    const int   MinRefuelCost = 15;
    const int   MaxRefuelCost = 60;
    const float CostPerMissedScorePoint = 0.5f;

    // -------------------------------------------------------------------------

    /// <summary>Random target band [lo, hi] for this attempt.</summary>
    public static void Target(System.Random rng, out float lo, out float hi)
    {
        lo = Lerp(rng, BandLoMin, BandLoMax);
        hi = lo + BandWidth;
    }

    /// <summary>Starting fuel level (close to empty) for this attempt.</summary>
    public static float StartFill(System.Random rng) => Lerp(rng, StartMin, StartMax);

    /// <summary>How much one pump tap adds.</summary>
    public static float TapAmount(System.Random rng) => Lerp(rng, TapMin, TapMax);

    /// <summary>True when <paramref name="fill"/> sits inside the band.</summary>
    public static bool InBand(float fill, float lo, float hi) => fill >= lo && fill <= hi;

    /// <summary>
    /// Score for stopping at <paramref name="fill"/>. Inside the band is full
    /// marks; outside scales down with distance; a timeout costs an extra dent.
    /// Floored at 10 — there is no fail state.
    /// </summary>
    public static int ScoreFor(float fill, float lo, float hi, bool timedOut)
    {
        int score = 100;
        if (timedOut) score -= 40;

        float miss = fill < lo ? lo - fill
                   : fill > hi ? fill - hi
                   : 0f;
        score -= Mathf.RoundToInt(miss * 300f);

        return Mathf.Max(10, score);
    }

    /// <summary>Money charged for a real refuel, scaled by minigame performance.</summary>
    public static int CostForScore(int score)
    {
        int clampedScore = Mathf.Clamp(score, 0, 100);
        int cost = MinRefuelCost + Mathf.RoundToInt((100 - clampedScore) * CostPerMissedScorePoint);
        return Mathf.Clamp(cost, MinRefuelCost, MaxRefuelCost);
    }

    // -------------------------------------------------------------------------

    static float Lerp(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);
}
