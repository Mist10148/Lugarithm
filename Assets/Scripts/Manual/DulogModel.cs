using UnityEngine;

/// <summary>How "live" an onboard passenger's dulog (alight) target is right now.</summary>
public enum DulogState
{
    Onboard,      // aboard, target still far off
    Approaching,  // target is within the reveal range — marker pulses, ramps up
    InRange,      // close enough to request the stop ("Para!")
}

/// <summary>
/// Pure math for the shared "dulog" highlighting, reused by the Manual markers
/// and (for symmetry) the Automation beacon: given how far the jeepney is from a
/// passenger's target stop, how intense the marker should be and which state it
/// is in. No Unity scene dependency, so it is covered by EditMode tests.
/// </summary>
public static class DulogModel
{
    /// <summary>Beyond this world distance the target is just "onboard" (no pulse).</summary>
    public const float ApproachRange = 30f;

    /// <summary>At or under this world distance the passenger can request the stop.</summary>
    public const float RequestRange = 7f;

    /// <summary>0 far away → 1 in range; drives marker pulse/scale/alpha.</summary>
    public static float Approach01(float distance)
    {
        if (distance <= RequestRange) return 1f;
        if (distance >= ApproachRange) return 0f;
        return (ApproachRange - distance) / (ApproachRange - RequestRange);
    }

    public static DulogState State(float distance)
    {
        if (distance <= RequestRange) return DulogState.InRange;
        if (distance <= ApproachRange) return DulogState.Approaching;
        return DulogState.Onboard;
    }
}
