using System.Collections.Generic;
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

    /// <summary>Removes one or more waiting passenger markers from a stop cell.</summary>
    void RemoveWaitingPeeps(Vector2Int cell, int count);

    /// <summary>Spawns one short-lived peep per color beside the stop at <paramref name="cell"/>
    /// to show passengers alighting on dropOff() — mirrors Manual's exiting-peep so an
    /// automation delivery is visible at the destination, not just at the jeepney.</summary>
    void SpawnAlightingPeeps(Vector2Int cell, IReadOnlyList<Color> colors);
}
