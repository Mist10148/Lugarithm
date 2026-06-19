using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Triggers the Manual Mode breakdown: at a scripted fraction of the route a
/// warning fires, the jeepney coasts to a stop, and a repair minigame interrupts
/// the drive. The fault (engine vs fuel) and the interface (non-code taps/gauge
/// vs escaping a code-driven maze) are both rolled at random for every breakdown,
/// independent of the player's Manual/Automation setting, so any of four
/// variants can appear. The run always continues afterwards (PRD §5.4).
/// </summary>
public class BreakdownController : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float warningSeconds = 2.2f;

    JeepneyController    _jeepney;
    PatternMatchMinigame _engineRepair;   // non-code · engine
    RefuelMinigame       _refuel;         // non-code · fuel
    MazeRepairMinigame   _maze;           // code · either fault (escape a maze)
    ToastNotification    _toast;
    DriveScoreTracker    _tracker;

    float _triggerDistance = -1f;
    bool  _armed;
    bool  _inProgress;

    public bool InProgress => _inProgress;

    static readonly string[] EngineHeadlines =
    {
        "ENGINE OVERHEAT!  Fit the parts in order:",
        "BELT SNAPPED!  Fit the parts in order:",
        "ENGINE TROUBLE!  Fit the parts in order:",
    };

    // -------------------------------------------------------------------------

    public void Init(JeepneyController jeepney,
                     PatternMatchMinigame engineRepair, RefuelMinigame refuel, MazeRepairMinigame maze,
                     ToastNotification toast, DriveScoreTracker tracker,
                     float routeLength, float triggerFraction)
    {
        _jeepney      = jeepney;
        _engineRepair = engineRepair;
        _refuel       = refuel;
        _maze         = maze;
        _toast        = toast;
        _tracker      = tracker;

        bool anyPanel = engineRepair != null || refuel != null || maze != null;
        // A repair breakdown now happens on every level (the tutorial included).
        // When the level doesn't script a fraction, roll a random mid-route point.
        _armed = anyPanel;
        float frac = triggerFraction > 0f ? triggerFraction
                                          : UnityEngine.Random.Range(0.35f, 0.7f);
        _triggerDistance = routeLength * frac;
    }

    /// <summary>Called each frame by the drive controller with route progress.</summary>
    public void Tick(float distanceAlongRoute)
    {
        if (_inProgress) return;

        // Tank ran dry → refuel mini-game, independent of the scripted breakdown.
        if (_jeepney != null && _refuel != null && _jeepney.Fuel01 <= 0f)
        {
            StartCoroutine(BreakdownSequence(forceFuel: true));
            return;
        }

        if (!_armed) return;
        if (distanceAlongRoute >= _triggerDistance)
        {
            _armed = false;
            StartCoroutine(BreakdownSequence(forceFuel: false));
        }
    }

    // -------------------------------------------------------------------------

    IEnumerator BreakdownSequence(bool forceFuel)
    {
        _inProgress = true;

        // Roll the fault and the interface fresh each run. A dry tank forces a
        // (non-code) refuel; otherwise either fault and either interface can appear.
        bool fuel = forceFuel || UnityEngine.Random.value < 0.5f;
        bool code = !forceFuel && UnityEngine.Random.value < 0.5f;
        int  seed = UnityEngine.Random.Range(0, 99999);
        BreakdownFault fault = fuel ? BreakdownFault.Fuel : BreakdownFault.Engine;

        if (_toast != null)
            _toast.Show(fuel
                ? "The tank's run dry…  pull over and refuel!"
                : "The engine is chugging…  something's about to give!");

        yield return new WaitForSeconds(warningSeconds);

        _jeepney.InputLocked = true;

        bool finished = false;
        Action<MinigameResult> onDone = result =>
        {
            _tracker.SetBreakdownResult(result.TimedOut);
            if (fuel && _jeepney != null) _jeepney.Refuel();   // fill the tank back up
            if (_toast != null)
                _toast.Show(result.TimedOut
                    ? "Patched it up… barely. (−100)"
                    : "Back on the road!");
            finished = true;
        };

        // Dispatch with graceful fallbacks if a panel is missing.
        if (code && _maze != null)
            _maze.Show(fault, seed, onDone);
        else if (fuel && _refuel != null)
            _refuel.Show(seed, onDone);
        else if (_engineRepair != null)
            _engineRepair.Show(seed, EngineHeadlines[seed % EngineHeadlines.Length], onDone);
        else if (_refuel != null)
            _refuel.Show(seed, onDone);
        else if (_maze != null)
            _maze.Show(fault, seed, onDone);
        else
            finished = true;

        yield return new WaitUntil(() => finished);

        _jeepney.InputLocked = false;
        _inProgress = false;
    }
}
