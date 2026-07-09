using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Top-down animated agent for Automation mode. Reuses the Manual jeepney
/// sprite, moves it along the top-down road, and rotates the body to face the
/// current grid direction.
/// </summary>
public class TopDownAgentView : MonoBehaviour, IPathAgentView, IStreamingAgentView
{
    [Header("References")]
    [SerializeField] public SpriteRenderer body;

    IGridSpace _space;
    IStopView  _stopView;

    // Persistent motion state carried ACROSS animation batches so a drive that is
    // streamed in 4-cell chunks reads as one continuous cruise instead of easing to
    // a stop and re-accelerating every batch. Reset only on a genuine teleport (SnapTo)
    // or a real stop (see PlayAction).
    float      _cruiseSpeed;   // world units / sec, eased toward the batch's target speed
    float      _speedVel;      // SmoothDamp velocity ref for _cruiseSpeed
    Quaternion _bodyRot;       // lagged body heading, carried across batches
    bool       _hasBodyRot;

    // How quickly the cruise speed ramps up from a standstill. Bigger = lazier launch.
    const float AccelSmoothTime = 0.45f;

    // Full needle deflection lines up with Manual's topSpeed via SpeedGauge.TopSpeed.
    const float TopSpeed = SpeedGauge.TopSpeed;

    public float CurrentSpeed => _cruiseSpeed;
    public float CurrentSpeed01 => SpeedGauge.Normalize(_cruiseSpeed);

    // Two-lane rendering: matches RoadTrafficController.laneOffset so the jeepney
    // lines up with the traffic lanes, inside the existing road art.
    const float LaneVisualOffset = 1.35f;

    /// <summary>Supplies the sim's current world-cardinal lane (or -1 outside lane
    /// mode) for poses that don't come from an action result (SnapTo after a reset).
    /// Wired by the drive controller.</summary>
    public System.Func<int> LaneCardinalSource;

    Vector3 LaneOffsetWorld(int laneCardinal)
    {
        if (laneCardinal < 0 || _space == null) return Vector3.zero;
        return (Vector3)(_space.FacingDirection(laneCardinal) * LaneVisualOffset);
    }

    Vector3 WorldOf(Vector2Int cell, int laneCardinal) =>
        _space.CellToWorld(cell) + LaneOffsetWorld(laneCardinal);

    // -------------------------------------------------------------------------

    public void Init(IGridSpace space, Vector2Int cell, int facing)
    {
        _space = space;
        _stopView = space as IStopView;
        SnapTo(cell, facing);
    }

    public void SnapTo(Vector2Int cell, int facing)
    {
        int lane = LaneCardinalSource != null ? LaneCardinalSource() : -1;
        transform.position = WorldOf(cell, lane);
        SetSortOrder(cell);
        SetBodyFacing(facing);

        // A teleport breaks motion continuity: drop cruise momentum and re-pin the
        // lagged heading to the snapped facing so the next drive eases in cleanly.
        _cruiseSpeed = 0f;
        _speedVel    = 0f;
        _bodyRot     = body != null ? body.transform.localRotation : Quaternion.identity;
        _hasBodyRot  = true;
    }

    public void RebindSpacePreservingPose(IGridSpace space, IStopView stopView, Vector2Int cell)
    {
        _space = space;
        _stopView = stopView;
        SetSortOrder(cell);

        if (!_hasBodyRot)
        {
            _bodyRot = body != null ? body.transform.localRotation : Quaternion.identity;
            _hasBodyRot = true;
        }
    }

    // -------------------------------------------------------------------------

    public IEnumerator PlayAction(AgentActionResult result, float duration)
    {
        // A flowing cruise only happens through PlayContinuousPath. Anything routed here as a
        // single beat that isn't locomotion (a stop, fare, change, idle wait) is a genuine halt,
        // so drop cruise momentum — the next drive eases in from rest instead of resuming at speed.
        switch (result.Action)
        {
            case "moveForward":
            case "moveLeft":
            case "moveRight":
            case "turnLeft":
            case "turnRight":
                break;
            default:
                _cruiseSpeed = 0f;
                break;
        }

        switch (result.Action)
        {
            case "moveForward":
                if (result.Blocked)
                    yield return Bump(result.FacingBefore, duration);
                else
                    yield return MoveBetween(result, duration);
                break;

            case "turnLeft":
            case "turnRight":
                // In lane mode the lane offset swings with the facing, so the body
                // rotation and the small positional slide play together.
                yield return Turn(result.FacingBefore, result.FacingAfter, duration,
                                  WorldOf(result.From, result.LaneBefore),
                                  WorldOf(result.To, result.LaneAfter));
                break;

            case "moveLeft":
                // Slide sideways without rotating — the body keeps facing ahead, so
                // the jeepney drifts into the lane (cell strafe or sub-cell lane).
                if (result.Blocked)
                    yield return Bump((result.FacingBefore + 3) % 4, duration);
                else
                    yield return MoveBetween(result, duration);
                break;

            case "moveRight":
                if (result.Blocked)
                    yield return Bump((result.FacingBefore + 1) % 4, duration);
                else
                    yield return MoveBetween(result, duration);
                break;

            case "pickUp":
                if (_stopView != null && result.PickedUp)
                    _stopView.RemoveWaitingPeeps(result.From, Mathf.Max(1, result.PickedUpCount));
                yield return Pop("Placeholders/peep", duration);
                break;

            case "dropOff":
                // Only show a passenger alighting when one actually did — dropOff() at a stop
                // that isn't a rider's marked destination delivers nobody, so it must not animate.
                if (result.DroppedOff)
                {
                    if (_stopView != null)
                        _stopView.SpawnAlightingPeeps(result.To, result.DroppedOffColors);
                    yield return Pop("Placeholders/peep", duration);
                }
                else
                    yield return new WaitForSeconds(duration);
                break;

            case "collectFare":
                yield return Pop("Placeholders/coin", duration);
                break;

            case "giveChange":
                yield return Pop("Placeholders/coin", duration);
                break;

            default:
                yield return new WaitForSeconds(duration);
                break;
        }
    }

    public IEnumerator PlayPath(IReadOnlyList<AgentActionResult> moves, float secondsPerStep)
    {
        if (moves == null || moves.Count == 0) yield break;

        bool hasForwardMove = false;
        foreach (AgentActionResult result in moves)
            if (IsCruiseMove(result))
                hasForwardMove = true;

        if (hasForwardMove)
        {
            yield return PlayContinuousPath(moves, secondsPerStep);
            // A traffic bump ends a batch, and the continuous polyline only stitches
            // un-blocked moves — play the terminal bump beat explicitly so hitting a
            // car is visible instead of silently swallowed.
            AgentActionResult last = moves[moves.Count - 1];
            if (last.Blocked)
                yield return PlayAction(last, secondsPerStep);
            yield break;
        }

        foreach (AgentActionResult result in moves)
        {
            float duration = result.Action == "moveForward"
                ? secondsPerStep
                : Mathf.Max(0.04f, secondsPerStep * 0.35f);
            yield return PlayAction(result, duration);
        }
    }

    // -------------------------------------------------------------------------

    static bool IsCruiseMove(AgentActionResult result)
    {
        if (result.Blocked) return false;
        return result.Action == "moveForward" ||
               result.Action == "moveLeft" ||
               result.Action == "moveRight";
    }

    IEnumerator PlayContinuousPath(IReadOnlyList<AgentActionResult> moves, float secondsPerStep)
    {
        // Stitch the forward moves into one world-space polyline so the jeepney drives the
        // whole stretch as a single heavy motion — accelerate, cruise, settle — instead of
        // easing to a near-stop at every cell. Rotation lags the travel direction so corners
        // feel weighty, like a real loaded jeepney leaning into the turn. Lane changes ride
        // the same polyline as lateral drift: the body keeps pointing down the road (the
        // heading it had when the slide started) instead of swinging 90° sideways.
        var pts     = new List<Vector3>();
        var toCells = new List<Vector2Int>();
        var rotDirs = new List<Vector3>();   // per-segment body-heading target
        foreach (AgentActionResult result in moves)
        {
            if (!IsCruiseMove(result)) continue;
            if (pts.Count == 0) pts.Add(WorldOf(result.From, result.LaneBefore));
            pts.Add(WorldOf(result.To, result.LaneAfter));
            toCells.Add(result.To);
            rotDirs.Add(result.Action == "moveForward"
                ? _space.CellToWorld(result.To) - _space.CellToWorld(result.From)
                : (Vector3)(_space.FacingDirection(result.FacingBefore)));
        }
        if (pts.Count < 2) yield break;

        int segCount = pts.Count - 1;
        var segLen   = new float[segCount];
        float total  = 0f;
        for (int i = 0; i < segCount; i++) { segLen[i] = Vector3.Distance(pts[i], pts[i + 1]); total += segLen[i]; }
        if (total < 1e-4f) yield break;

        // Constant cruise speed for this batch. secondsPerStep is constant across batches,
        // so targetSpeed is too — the jeepney holds one speed through every chunk and corner
        // instead of re-running an ease-in/ease-out per batch ("pumping").
        float duration    = Mathf.Max(0.04f, secondsPerStep * segCount);
        float targetSpeed = total / duration;

        if (!_hasBodyRot)
        {
            _bodyRot    = body != null ? body.transform.localRotation : Quaternion.identity;
            _hasBodyRot = true;
        }

        float traveled = 0f;
        while (traveled < total)
        {
            // Ease the speed up only when launching from rest (_cruiseSpeed near 0); mid-drive
            // batches start already at targetSpeed, so there's no per-batch slowdown.
            _cruiseSpeed = Mathf.SmoothDamp(_cruiseSpeed, targetSpeed, ref _speedVel, AccelSmoothTime);
            traveled    += Mathf.Max(_cruiseSpeed, 0.0001f) * Time.deltaTime;
            float dist   = Mathf.Min(traveled, total);

            int s = 0;
            while (s < segCount - 1 && dist > segLen[s]) { dist -= segLen[s]; s++; }
            float u = segLen[s] > 1e-4f ? Mathf.Clamp01(dist / segLen[s]) : 1f;
            transform.position = Vector3.Lerp(pts[s], pts[s + 1], u);

            Vector3 dir = rotDirs[s];
            if (dir.sqrMagnitude > 1e-5f && body != null)
            {
                Quaternion target = BodyRotationFromDir(dir);
                // Frame-rate-independent lag, carried in _bodyRot so heading flows smoothly
                // across batch boundaries (no snap to the final segment when a batch ends).
                _bodyRot = Quaternion.Slerp(_bodyRot, target, 1f - Mathf.Exp(-9f * Time.deltaTime));
                body.transform.localRotation = _bodyRot;
            }

            SetSortOrder(toCells[Mathf.Min(s, toCells.Count - 1)]);
            yield return null;
        }

        transform.position = pts[pts.Count - 1];
        SetSortOrder(toCells[toCells.Count - 1]);
    }

    Quaternion BodyRotationFromDir(Vector3 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        return Quaternion.Euler(0f, 0f, angle);
    }

    IEnumerator MoveBetween(AgentActionResult result, float duration)
    {
        Vector3 a = WorldOf(result.From, result.LaneBefore);
        Vector3 b = WorldOf(result.To, result.LaneAfter);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            transform.position = Vector3.Lerp(a, b, t);
            if (t > 0.5f) SetSortOrder(result.To);
            yield return null;
        }

        transform.position = b;
        SetSortOrder(result.To);
    }

    IEnumerator Bump(int facing, float duration)
    {
        _cruiseSpeed = 0f;   // hit a wall — kill momentum so the next drive eases in
        Vector3 origin = transform.position;
        Vector3 push   = (Vector3)(_space.FacingDirection(facing) * 0.16f);

        float half = Mathf.Max(0.05f, duration * 0.5f);
        float elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(origin, origin + push, elapsed / half);
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(origin + push, origin, elapsed / half);
            yield return null;
        }
        transform.position = origin;
    }

    IEnumerator Turn(int fromFacing, int toFacing, float duration,
                     Vector3? fromPos = null, Vector3? toPos = null)
    {
        Quaternion a = BodyRotation(fromFacing);
        Quaternion b = BodyRotation(toFacing);
        bool slide = fromPos.HasValue && toPos.HasValue &&
                     (toPos.Value - fromPos.Value).sqrMagnitude > 1e-6f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _bodyRot = Quaternion.Slerp(a, b, t);
            body.transform.localRotation = _bodyRot;
            if (slide)
                transform.position = Vector3.Lerp(fromPos.Value, toPos.Value,
                                                  Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        _bodyRot = b;
        _hasBodyRot = true;
        body.transform.localRotation = b;
        if (slide) transform.position = toPos.Value;
    }

    IEnumerator Pop(string spritePath, float duration)
    {
        var icon = new GameObject("Pop");
        icon.transform.SetParent(transform, false);
        icon.transform.localPosition = new Vector3(0f, 0.45f, 0f);

        var sr = icon.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>(spritePath);
        sr.sortingOrder = 999;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            icon.transform.localPosition = new Vector3(0f, 0.45f + 0.35f * t, 0f);
            sr.color = new Color(1f, 1f, 1f, 1f - t);
            yield return null;
        }

        Object.Destroy(icon);
    }

    // -------------------------------------------------------------------------

    void SetSortOrder(Vector2Int cell)
    {
        if (body != null)
            body.sortingOrder = _space.SortOrder(cell) + 1;
    }

    void SetBodyFacing(int facing)
    {
        if (body != null)
            body.transform.localRotation = BodyRotation(facing);
    }

    Quaternion BodyRotation(int facing)
    {
        Vector2 dir = _space.FacingDirection(facing);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        return Quaternion.Euler(0f, 0f, angle);
    }
}
