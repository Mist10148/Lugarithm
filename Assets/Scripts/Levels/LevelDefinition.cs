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
    /// Optional per-run procedural town (fixed anchors + randomized branches and
    /// passengers). When null or <c>enabled == false</c>, the authored
    /// <see cref="manual"/>/<see cref="auto"/> content is used as-is.
    /// </summary>
    public ProceduralLayoutDefinition procedural;

    /// <summary>
    /// The required non-code town puzzle shown on arrival, before results — the
    /// gate that must be solved to advance. None for levels without one (the
    /// Tutorial's non-code beat is the in-drive Coin Drawer).
    /// </summary>
    public TownPuzzleKind townPuzzle = TownPuzzleKind.None;

    /// <summary>
    /// When set, the level's primary scene is this top-down overworld map instead
    /// of the jeep minigame. The jeep minigame (ManualDrive / CodeDrive) becomes
    /// accessible from within the overworld via interaction triggers.
    /// </summary>
    public string overworldSceneName = "";
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

    /// <summary>Value reporters (e.g. seatsLeft, position) offered in this level.</summary>
    public string[] allowedReporters;

    public int   parSteps;
    public float softTimerSeconds = 300f;

    /// <summary>Canonical solution, shown in the post-level analytics panel.</summary>
    public string optimalSolutionText;

    /// <summary>Pre-filled comment scaffold for the text editor.</summary>
    public string codeScaffold;

    /// <summary>When true, winning needs every passenger picked up and dropped at D.</summary>
    public bool requireAllPassengersDelivered = true;

    /// <summary>
    /// When true, the road is an endless procedural street: the win is "all the
    /// <i>required</i> riders delivered" (not "reach the receding frontier D"), and
    /// generation keeps streaming forever afterwards. Set for procedural Automation
    /// legs; authored mazes leave it false so they still finish at D.
    /// </summary>
    public bool endlessRoute = false;

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

// -----------------------------------------------------------------------------
// Procedural world generation (hybrid: fixed anchors + per-run randomness).
// See Assets/Scripts/Levels/Generation/. The authored manual/auto above stay as
// the deterministic fallback when procedural.enabled is false (or unset).

/// <summary>
/// Hybrid procedural layout for a level. The <see cref="trunk"/> spine and the
/// <see cref="anchors"/> pinned to it are authored and immovable across runs
/// (story coherence); <see cref="gen"/> tunes the per-run randomness (branch
/// side-streets, ordinary-passenger boarding/alight nodes). Consumed by
/// <c>TownLayoutGenerator</c>; projected into both drive modes.
/// </summary>
[Serializable]
public class ProceduralLayoutDefinition
{
    /// <summary>When false, the level uses its authored manual/auto content as-is.</summary>
    public bool enabled;

    /// <summary>
    /// The authored road spine in Manual-mode world units (includes bends). The
    /// jeepney drives this; branches hang off it. Usually the same polyline as
    /// <see cref="ManualRouteDefinition.waypoints"/>.
    /// </summary>
    public Vector2[] trunk;

    /// <summary>Fixed, named nodes pinned onto the trunk; never move across runs.</summary>
    public AnchorNode[] anchors;

    public TownGenParams gen = new TownGenParams();
}

/// <summary>A fixed story node pinned to a trunk vertex.</summary>
[Serializable]
public class AnchorNode
{
    public string     name;
    public AnchorKind kind;
    public Vector2    position;
}

/// <summary>Role of a fixed anchor along the trunk.</summary>
public enum AnchorKind
{
    TerminalStart,  // the leg's origin terminal (S)
    TerminalEnd,    // the leg's destination terminal (D)
    HeritageSite,   // a local heritage landmark (boardable/alightable, dialogue anchor)
    NpcDrop,        // a main-NPC drop point the player must visit
}

/// <summary>Tunables for the per-run randomness (spawn balance / density).</summary>
[Serializable]
public class TownGenParams
{
    [Header("Branch side-streets")]
    // 0/0 = no intersecting side-streets. Every stop then sits on the single
    // forward road, so a passenger's destination is always reachable and no
    // stop-sign/passenger can spawn on a road the player can't drive to.
    public int   branchCountMin = 0;
    public int   branchCountMax = 0;
    public float branchSpacing  = 18f;  // min arc-length between branch roots (world units)
    public float branchLenMin   = 8f;
    public float branchLenMax   = 14f;

    [Header("Passengers")]
    public int   passengerCountMin = 2;
    public int   passengerCountMax = 5;
    public float passengerDensity  = 0.8f; // riders per boardable stop, before clamping

    [Header("Grid projection")]
    public float gridCellSize = 6f;        // Manual world units per Automation grid cell
}
