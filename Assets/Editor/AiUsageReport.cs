using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor menu access to <see cref="AiUsageTracker"/>. Dumps the session's per-feature /
/// per-model request and token tallies to the Console so you can see real AI spend and which
/// keys/models actually served, without wiring any in-game UI.
/// </summary>
public static class AiUsageReport
{
    [MenuItem("Lugarithm/AI Usage Report")]
    public static void Report() => Debug.Log(AiUsageTracker.Summary());

    [MenuItem("Lugarithm/Reset AI Usage Report")]
    public static void Reset()
    {
        AiUsageTracker.Reset();
        Debug.Log("[AI Usage] Tallies reset.");
    }
}
