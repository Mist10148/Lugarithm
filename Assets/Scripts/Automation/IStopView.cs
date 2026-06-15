using UnityEngine;

/// <summary>
/// Presentation-neutral view of passenger stops. Lets the execution controller
/// reset stop occupancy regardless of whether stops are iso tiles or top-down
/// road zones.
/// </summary>
public interface IStopView
{
    /// <summary>Marks every stop as having a waiting passenger.</summary>
    void ResetStops();

    /// <summary>Shows/hides the waiting passenger marker on a stop cell.</summary>
    void SetStopOccupied(Vector2Int cell, bool occupied);
}
