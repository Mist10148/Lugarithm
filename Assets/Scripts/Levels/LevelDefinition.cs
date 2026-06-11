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
}

/// <summary>Fare pricing for a leg (PHP). Base covers the first stop.</summary>
[Serializable]
public class FareTable
{
    public int baseFare         = 13;
    public int perStopIncrement = 2;
}
