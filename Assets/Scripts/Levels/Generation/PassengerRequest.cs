using UnityEngine;

/// <summary>
/// A mode-agnostic committed ride: where an ordinary passenger boards and the
/// "dulog" node they want to alight at, with fare/tender precomputed and a color
/// that distinguishes them while aboard. This is the single source the Manual
/// dulog markers and the Automation self-driving agent both read — Manual links
/// it from <c>ManualPassenger</c>, the autopilot consumes the list directly.
/// </summary>
public class PassengerRequest
{
    public int    id;
    public Color  color;

    public int    originNodeId;
    public int    destNodeId;

    /// <summary>Ordinal stop count origin→dest along the route (≥1), for fare math.</summary>
    public int    stopsTraveled;

    public int    fare;
    public int    tender;
}
