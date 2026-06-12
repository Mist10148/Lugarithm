using System;
using UnityEngine;

/// <summary>
/// Code-defined content for one level (leg): the Manual Mode route, the
/// Automation Mode puzzle, and the fare table. Plain serializable data so a
/// later pass can lift it into ScriptableObjects without rework.
/// </summary>
[Serializable]
public class LevelDefinition
{
    public int    levelIndex;
    public string displayName;

    /// <summary>False for levels that only exist as locked menu entries.</summary>
    public bool hasContent;

    public ManualRouteDefinition     manual;
    public AutomationPuzzleDefinition auto;
    public FareTable                 fares = new FareTable();

    /// <summary>
    /// The required non-code town puzzle shown on arrival, before results — the
    /// gate that must be solved to advance. None for levels without one (the
    /// Tutorial's non-code beat is the in-drive Coin Drawer).
    /// </summary>
    public TownPuzzleKind townPuzzle = TownPuzzleKind.None;
}

/// <summary>Which on-arrival non-code town puzzle a level gates advancement on.</summary>
public enum TownPuzzleKind
{
    None,
    FlowConnect,   // Non-Intersecting Connections (Molo)
    CrateStack,    // Market Crate Stacking (Oton)
}

/// <summary>One passenger stop along a manual route, pinned to a waypoint.</summary>
[Serializable]
public class ManualStopDefinition
{
    public string stopName;
    public int    waypointIndex;
    public int    waitingPassengers;
    public bool   isDestination;
}

/// <summary>The Manual Mode drive: a waypoint polyline with stops along it.</summary>
[Serializable]
public class ManualRouteDefinition
{
    public Vector2[] waypoints;
    public float     roadHalfWidth = 3f;
    public ManualStopDefinition[] stops;
    public int       seatCapacity = 8;

    /// <summary>Route fraction (0..1) where the breakdown fires; negative = none.</summary>
    public float breakdownAtRouteFraction = -1f;

    public float parTimeSeconds = 180f;
}

/// <summary>
/// The Automation Mode puzzle: a grid map plus the rules of engagement.
/// Map legend: '#' wall · '.' road · 'S' start · 'D' destination · 'P' stop
/// with a waiting passenger (their destination is the 'D' cell).
/// </summary>
[Serializable]
public class AutomationPuzzleDefinition
{
    public string[] gridMap;

    /// <summary>0 = North (up), 1 = East, 2 = South, 3 = West.</summary>
    public int startFacing = 1;

    public string goalText;

    /// <summary>Action/control blocks offered in the palette for this level.</summary>
    public string[] allowedBlocks;

    /// <summary>Condition queries offered on if/while blocks for this level.</summary>
    public string[] allowedQueries;

    public int   parSteps;
    public float softTimerSeconds = 300f;

    /// <summary>Canonical solution, shown in the post-level analytics panel.</summary>
    public string optimalSolutionText;

    /// <summary>Pre-filled comment scaffold for the text editor.</summary>
    public string codeScaffold;

    /// <summary>When true, winning needs every passenger picked up and dropped at D.</summary>
    public bool requireAllPassengersDelivered = true;

    /// <summary>
    /// When true, the routed CodeDrive scene uses this <see cref="gridMap"/> as-is
    /// instead of deriving a grid from the manual route — used for levels whose
    /// code puzzle is a standalone maze (e.g. Oton) rather than a route mirror.
    /// </summary>
    public bool useAuthoredGrid = false;
}

/// <summary>Fare pricing for a leg (PHP). Base covers the first stop.</summary>
[Serializable]
public class FareTable
{
    public int baseFare         = 13;
    public int perStopIncrement = 2;
}
