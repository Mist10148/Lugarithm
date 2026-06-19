using System;

/// <summary>
/// Runs BOTH town-gate mini-games (FlowConnect + CrateStack) back-to-back in a
/// seed-randomized order — advancing a town now requires clearing both. Scores
/// are averaged and timeouts OR-ed into a single result. Shared by Manual and
/// Automation so the gate behaves identically in both modes.
/// </summary>
public static class TownGateRunner
{
    public static bool RunBoth(FlowConnectMinigame flow, CrateStackMinigame crate,
                               int seed, Action<MinigameResult> onDone)
    {
        // Need both to run a dual gate; otherwise fall back to whichever exists.
        if (flow == null || crate == null)
        {
            if (flow  != null) { flow.Show(seed, onDone);  return true; }
            if (crate != null) { crate.Show(seed, onDone); return true; }
            return false;
        }

        var rng = new System.Random(seed);
        bool flowFirst = rng.Next(2) == 0;
        int  total = 0;
        bool timedOut = false;

        Action<MinigameResult> second = r2 =>
        {
            total += r2.Score; timedOut |= r2.TimedOut;
            onDone(new MinigameResult { Score = total / 2, TimedOut = timedOut });
        };
        Action<MinigameResult> first = r1 =>
        {
            total += r1.Score; timedOut |= r1.TimedOut;
            if (flowFirst) crate.Show(seed + 7, second);
            else           flow.Show(seed + 7, second);
        };

        if (flowFirst) flow.Show(seed, first);
        else           crate.Show(seed, first);
        return true;
    }
}
