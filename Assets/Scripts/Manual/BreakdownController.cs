using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Triggers the Manual Mode breakdown: at a scripted fraction of the route a
/// warning fires, the jeepney coasts to a stop, and a repair minigame interrupts
/// the drive. The fault (engine vs fuel) and the interface (non-code taps vs
/// arranging code blocks) are both rolled at random for every breakdown,
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
    CodeFixMinigame      _codeFix;        // code · either fault
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
                     PatternMatchMinigame engineRepair, RefuelMinigame refuel, CodeFixMinigame codeFix,
                     ToastNotification toast, DriveScoreTracker tracker,
                     float routeLength, float triggerFraction)
    {
        _jeepney      = jeepney;
        _engineRepair = engineRepair;
        _refuel       = refuel;
        _codeFix      = codeFix;
        _toast        = toast;
        _tracker      = tracker;

        bool anyPanel = engineRepair != null || refuel != null || codeFix != null;
        _armed = triggerFraction > 0f && anyPanel;
        _triggerDistance = routeLength * triggerFraction;
    }

    /// <summary>Called each frame by the drive controller with route progress.</summary>
    public void Tick(float distanceAlongRoute)
    {
        if (!_armed || _inProgress) return;

        if (distanceAlongRoute >= _triggerDistance)
        {
            _armed = false;
            StartCoroutine(BreakdownSequence());
        }
    }

    // -------------------------------------------------------------------------

    IEnumerator BreakdownSequence()
    {
        _inProgress = true;

        // Roll the fault and the interface fresh each run.
        bool fuel = UnityEngine.Random.value < 0.5f;
        bool code = UnityEngine.Random.value < 0.5f;
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
            if (_toast != null)
                _toast.Show(result.TimedOut
                    ? "Patched it up… barely. (−100)"
                    : "Back on the road!");
            finished = true;
        };

        // Dispatch with graceful fallbacks if a panel is missing.
        if (code && _codeFix != null)
            _codeFix.Show(fault, seed, onDone);
        else if (fuel && _refuel != null)
            _refuel.Show(seed, onDone);
        else if (_engineRepair != null)
            _engineRepair.Show(seed, EngineHeadlines[seed % EngineHeadlines.Length], onDone);
        else if (_refuel != null)
            _refuel.Show(seed, onDone);
        else if (_codeFix != null)
            _codeFix.Show(fault, seed, onDone);
        else
            finished = true;

        yield return new WaitUntil(() => finished);

        _jeepney.InputLocked = false;
        _inProgress = false;
    }
}
