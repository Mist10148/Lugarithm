using System.Collections;
using UnityEngine;

/// <summary>
/// Triggers the Manual Mode breakdown: at a scripted fraction of the route a
/// warning fires, the jeepney coasts to a stop, and the pattern-match repair
/// minigame interrupts the drive. The run always continues afterwards.
/// </summary>
public class BreakdownController : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float warningSeconds = 2.2f;

    JeepneyController    _jeepney;
    PatternMatchMinigame _minigame;
    ToastNotification    _toast;
    DriveScoreTracker    _tracker;

    float _triggerDistance = -1f;
    bool  _armed;
    bool  _inProgress;

    public bool InProgress => _inProgress;

    // -------------------------------------------------------------------------

    public void Init(JeepneyController jeepney, PatternMatchMinigame minigame,
                     ToastNotification toast, DriveScoreTracker tracker,
                     float routeLength, float triggerFraction)
    {
        _jeepney  = jeepney;
        _minigame = minigame;
        _toast    = toast;
        _tracker  = tracker;

        _armed = triggerFraction > 0f && minigame != null;
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

        if (_toast != null)
            _toast.Show("The engine is chugging…  something's about to give!");

        yield return new WaitForSeconds(warningSeconds);

        _jeepney.InputLocked = true;

        bool finished = false;
        _minigame.Show(Random.Range(0, 99999), result =>
        {
            _tracker.SetBreakdownResult(result.TimedOut);
            if (_toast != null)
                _toast.Show(result.TimedOut
                    ? "Patched it up… barely. (−100)"
                    : "Back on the road!");
            finished = true;
        });

        yield return new WaitUntil(() => finished);

        _jeepney.InputLocked = false;
        _inProgress = false;
    }
}
