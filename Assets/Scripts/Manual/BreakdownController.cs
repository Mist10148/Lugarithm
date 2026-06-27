using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Triggers Manual Mode roadside interruptions. Scripted mid-route breakdowns
/// are engine repairs only; the refuel minigame is reserved for a genuinely
/// empty tank, except for the tutorial dialogue drill wired elsewhere.
/// The run always continues afterwards (PRD Section 5.4).
/// </summary>
public class BreakdownController : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float warningSeconds = 2.2f;

    JeepneyController    _jeepney;
    PatternMatchMinigame _engineRepair;   // non-code engine
    RefuelMinigame       _refuel;         // non-code fuel
    MazeRepairMinigame   _maze;           // code repair
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

        bool anyRepairPanel = engineRepair != null || maze != null;
        // A repair breakdown now happens on every level (the tutorial included).
        // When the level doesn't script a fraction, roll a random mid-route point.
        _armed = anyRepairPanel;
        float frac = triggerFraction > 0f ? triggerFraction
                                          : UnityEngine.Random.Range(0.35f, 0.7f);
        _triggerDistance = routeLength * frac;
    }

    /// <summary>Called each frame by the drive controller with route progress.</summary>
    public void Tick(float distanceAlongRoute)
    {
        if (_inProgress) return;

        // Tank ran dry: refuel minigame, independent of the scripted breakdown.
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

        // A dry tank forces a refuel. Scripted/random breakdowns stay engine-only.
        bool fuel = forceFuel;
        bool code = !fuel && UnityEngine.Random.value < 0.5f;
        int  seed = UnityEngine.Random.Range(0, 99999);
        BreakdownFault fault = fuel ? BreakdownFault.Fuel : BreakdownFault.Engine;

        if (_toast != null)
            _toast.Show(fuel
                ? "The tank's run dry... pull over and refuel!"
                : "The engine is chugging... something's about to give!");

        yield return new WaitForSeconds(warningSeconds);

        _jeepney.InputLocked = true;

        bool finished = false;
        Action<MinigameResult> onDone = result =>
        {
            if (result != null)
                _tracker.SetBreakdownResult(result.TimedOut);

            int refuelSpent = 0;
            if (fuel && _jeepney != null)
            {
                _jeepney.Refuel();
                int cost = RefuelMath.CostForScore(result != null ? result.Score : 0);
                refuelSpent = GameManager.Instance != null
                    ? GameManager.Instance.SpendCurrency(cost)
                    : cost;
            }

            if (_toast != null)
            {
                string message = result != null && result.TimedOut
                    ? "Patched it up... barely. (-100)"
                    : "Back on the road!";
                if (fuel)
                    message = $"Refuel cost: PHP {refuelSpent}. " + message;
                _toast.Show(message);
            }
            finished = true;
        };

        // Dispatch with graceful fallbacks if a repair panel is missing. Refuel
        // is only used for the forced empty-tank path above.
        if (fuel && _refuel != null)
            _refuel.Show(seed, onDone);
        else if (code && _maze != null)
            _maze.Show(fault, seed, onDone);
        else if (_engineRepair != null)
            _engineRepair.Show(seed, EngineHeadlines[seed % EngineHeadlines.Length], onDone);
        else if (_maze != null)
            _maze.Show(fault, seed, onDone);
        else
            finished = true;

        yield return new WaitUntil(() => finished);

        _jeepney.InputLocked = false;
        _inProgress = false;
    }
}
