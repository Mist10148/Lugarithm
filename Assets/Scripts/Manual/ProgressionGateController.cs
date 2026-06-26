using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Pops the level's progression mini-game (the non-code "town gate" — FlowConnect
/// for Molo, CrateStack for Oton) at a randomized point mid-drive, instead of
/// after arrival. Mirrors <see cref="BreakdownController"/>: armed once per leg,
/// ticked each frame with route progress, and — unlike a breakdown — MANDATORY:
/// driving stays locked until the puzzle is solved (the minigame's onDone only
/// fires on a solve). Levels with no town puzzle never arm.
/// </summary>
public class ProgressionGateController : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float warningSeconds = 1.6f;

    JeepneyController   _jeepney;
    FlowConnectMinigame _flow;
    CrateStackMinigame  _crate;
    ToastNotification   _toast;
    DriveScoreTracker   _tracker;
    TownPuzzleKind      _kind;

    float _triggerDistance = -1f;
    bool  _armed;
    bool  _inProgress;

    public bool InProgress => _inProgress;

    // -------------------------------------------------------------------------

    public void Init(TownPuzzleKind kind,
                     FlowConnectMinigame flow, CrateStackMinigame crate,
                     JeepneyController jeepney, ToastNotification toast,
                     DriveScoreTracker tracker, float routeLength)
    {
        _kind    = kind;
        _flow    = flow;
        _crate   = crate;
        _jeepney = jeepney;
        _toast   = toast;
        _tracker = tracker;

        bool hasPanel = (kind == TownPuzzleKind.FlowConnect && flow != null)
                     || (kind == TownPuzzleKind.CrateStack && crate != null);
        _armed = kind != TownPuzzleKind.None && hasPanel;

        // Fire in the first third-to-half of the trunk so it lands during the
        // story drive and tends to sit apart from the mid-route breakdown.
        _triggerDistance = routeLength * UnityEngine.Random.Range(0.2f, 0.45f);
    }

    /// <summary>Called each frame by the drive controller with route progress.</summary>
    public void Tick(float distanceAlongRoute)
    {
        if (!_armed || _inProgress) return;
        if (distanceAlongRoute < _triggerDistance) return;

        _armed = false;
        StartCoroutine(GateSequence());
    }

    // -------------------------------------------------------------------------

    IEnumerator GateSequence()
    {
        _inProgress = true;

        if (_toast != null)
            _toast.Show(_kind == TownPuzzleKind.CrateStack
                ? "Cargo's shifted — sort the load before you go on!"
                : "Roadblock ahead — sort the route before you go on!");

        yield return new WaitForSeconds(warningSeconds);

        if (_jeepney != null) _jeepney.InputLocked = true;

        bool finished = false;
        int  seed = UnityEngine.Random.Range(0, 99999);
        Action<MinigameResult> onDone = result =>
        {
            if (_tracker != null) _tracker.AddSatisfaction(result != null ? result.Score : 0);
            if (_toast != null) _toast.Show("Sorted — back on the road!");
            finished = true;
        };

        // Mandatory: the matching minigame only calls back once it's solved.
        if (_kind == TownPuzzleKind.FlowConnect && _flow != null)
            _flow.Show(seed, onDone);
        else if (_kind == TownPuzzleKind.CrateStack && _crate != null)
            _crate.Show(seed, onDone);
        else
            finished = true;

        yield return new WaitUntil(() => finished);

        if (_jeepney != null) _jeepney.InputLocked = false;
        _inProgress = false;
    }
}
