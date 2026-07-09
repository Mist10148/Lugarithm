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

    // Smoothed lane-offset channel (the traffic-car visualSide pattern): animation
    // writes the CENTERLINE position and composes this world-space lateral offset on
    // top each frame — so lane switches drift at cruise speed, in-place switches
    // glide, and the corner lane-cardinal flip swings instead of jumping 2×1.35.
    Vector3 _laneOffsetVisual;
    Vector3 _laneOffsetVel;
    int     _laneTargetCardinal = -1;
    const float LaneSmoothTime = 0.25f;

    // Corners get chamfered by this much on each leg so the jeepney sweeps a turn at
    // constant speed (with the lagged body heading) instead of pivoting on a vertex.
    const float CornerCutDistance = 0.9f;

    /// <summary>Supplies the sim's current world-cardinal lane (or -1 outside lane
    /// mode) for poses that don't come from an action result (SnapTo after a reset).
    /// Wired by the drive controller.</summary>
    public System.Func<int> LaneCardinalSource;

    Vector3 LaneOffsetWorld(int laneCardinal)
    {
        if (laneCardinal < 0 || _space == null) return Vector3.zero;
        return (Vector3)(_space.FacingDirection(laneCardinal) * LaneVisualOffset);
    }

    void SetLaneTarget(int laneCardinal) => _laneTargetCardinal = laneCardinal;

    /// <summary>Advances the lane-offset channel one frame toward its target and
    /// returns the current offset. Every animation frame composes position as
    /// centerline + this, so lateral motion is always SmoothDamp-continuous.</summary>
    Vector3 StepLaneOffset()
    {
        Vector3 target = LaneOffsetWorld(_laneTargetCardinal);
        _laneOffsetVisual = Vector3.SmoothDamp(_laneOffsetVisual, target,
                                               ref _laneOffsetVel, LaneSmoothTime);
        return _laneOffsetVisual;
    }

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
        SetLaneTarget(lane);
        _laneOffsetVisual = LaneOffsetWorld(lane);   // teleports stay teleports — no drift-in
        _laneOffsetVel    = Vector3.zero;
        transform.position = _space.CellToWorld(cell) + _laneOffsetVisual;
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
        // Only a genuine service halt (boarding, fares, change) drops cruise momentum — the
        // jeepney really is stopped at the curb there. Logic beats (wait, avoidTraffic checks,
        // queries) must NOT kill it, or every loop-iteration check re-ramps the cruise from rest.
        switch (result.Action)
        {
            case "pickUp":
            case "collectFare":
            case "giveChange":
                _cruiseSpeed = 0f;
                break;
            case "dropOff":
                if (result.DroppedOff) _cruiseSpeed = 0f;   // somebody actually alighted
                break;
        }

        switch (result.Action)
        {
            case "moveForward":
                // A single move rides the same constant-speed cruise path as a batch,
                // so it reuses _cruiseSpeed momentum instead of easing in from rest.
                if (result.Blocked)
                    yield return Bump(result.FacingBefore, duration);
                else
                    yield return PlayContinuousPath(new[] { result }, duration);
                break;

            case "turnLeft":
            case "turnRight":
                yield return Turn(result, duration);
                break;

            case "moveLeft":
                // Lane change: a merging diagonal rides the cruise (the lane channel
                // supplies the lateral drift); an in-place switch glides the channel.
                if (result.Blocked)
                    yield return Bump((result.FacingBefore + 3) % 4, duration);
                else if (result.From != result.To)
                    yield return PlayContinuousPath(new[] { result }, duration);
                else
                    yield return LaneGlide(result, duration);
                break;

            case "moveRight":
                if (result.Blocked)
                    yield return Bump((result.FacingBefore + 1) % 4, duration);
                else if (result.From != result.To)
                    yield return PlayContinuousPath(new[] { result }, duration);
                else
                    yield return LaneGlide(result, duration);
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

        // The continuous path needs at least one real travel segment; a batch of only
        // turns / in-place lane switches plays per-action (rotation + lane glide).
        bool hasTravel = false;
        foreach (AgentActionResult result in moves)
            if (IsCruiseMove(result) && result.From != result.To)
                hasTravel = true;

        if (hasTravel)
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
        // Stitch the batch into one CENTERLINE polyline driven at constant speed — one
        // heavy motion: accelerate, cruise, settle. The lane offset is a separate
        // smoothed channel composed on top each frame, so lane changes render as a
        // lateral drift at cruise speed and the offset swings smoothly through corners.
        // Turns contribute no vertex; corners get chamfered so the jeepney sweeps them
        // (rotation lags the travel direction — a loaded jeepney leaning into the turn).
        var pts   = new List<Vector3>();
        var dirs  = new List<Vector3>();     // per-segment travel direction
        var lanes = new List<int>();         // per-segment lane-cardinal target
        var cells = new List<Vector2Int>();  // per-segment sort-order cell
        int trailingLane = int.MinValue;     // lane retarget after the last vertex
        int travelSegs = 0;

        foreach (AgentActionResult m in moves)
        {
            if (m.Blocked) break;   // a bump ends the batch; PlayPath replays it as its own beat
            bool isTurn = m.Action == "turnLeft" || m.Action == "turnRight";
            if (!isTurn && !IsCruiseMove(m)) continue;

            if (m.From != m.To)
            {
                Vector3 a = _space.CellToWorld(m.From);
                Vector3 b = _space.CellToWorld(m.To);
                if (pts.Count == 0) pts.Add(a);
                pts.Add(b);
                dirs.Add(b - a);
                lanes.Add(m.LaneAfter);
                cells.Add(m.To);
                travelSegs++;
                trailingLane = int.MinValue;
            }
            else
            {
                // Turn or in-place lane switch: no vertex — it only retargets the lane
                // channel (a turn's new heading shows up in the next segment's dir).
                trailingLane = m.LaneAfter;
            }
        }
        if (travelSegs == 0) yield break;

        ApplyCornerCuts(pts, dirs, lanes, cells);

        int segCount = pts.Count - 1;
        var segLen   = new float[segCount];
        float total  = 0f;
        for (int i = 0; i < segCount; i++) { segLen[i] = Vector3.Distance(pts[i], pts[i + 1]); total += segLen[i]; }
        if (total < 1e-4f) yield break;

        // Constant cruise speed pinned to the ORIGINAL cell count, so chamfered corners
        // (slightly shorter path) don't change the pace between batches ("pumping").
        float duration    = Mathf.Max(0.04f, secondsPerStep * travelSegs);
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

            SetLaneTarget(lanes[s]);
            transform.position = Vector3.Lerp(pts[s], pts[s + 1], u) + StepLaneOffset();

            Vector3 dir = dirs[s];
            if (dir.sqrMagnitude > 1e-5f && body != null)
            {
                Quaternion target = BodyRotationFromDir(dir);
                // Frame-rate-independent lag, carried in _bodyRot so heading flows smoothly
                // across batch boundaries (no snap to the final segment when a batch ends).
                _bodyRot = Quaternion.Slerp(_bodyRot, target, 1f - Mathf.Exp(-9f * Time.deltaTime));
                body.transform.localRotation = _bodyRot;
            }

            SetSortOrder(cells[Mathf.Min(s, cells.Count - 1)]);
            yield return null;
        }

        // Land on the centerline endpoint plus the channel's CURRENT offset — a
        // half-finished lane drift keeps drifting through the next batch, never snaps.
        transform.position = pts[pts.Count - 1] + _laneOffsetVisual;
        if (trailingLane != int.MinValue) SetLaneTarget(trailingLane);
        SetSortOrder(cells[cells.Count - 1]);
    }

    /// <summary>Chamfers every interior vertex where the heading changes: the sharp
    /// corner point becomes an entry/exit pair CornerCutDistance up each leg (clamped
    /// to half the shorter leg), and the inserted diagonal blends heading + starts the
    /// outgoing lane swing early — a swept corner at constant speed, matching Manual's
    /// cornerEaseDistance feel.</summary>
    static void ApplyCornerCuts(List<Vector3> pts, List<Vector3> dirs,
                                List<int> lanes, List<Vector2Int> cells)
    {
        for (int i = dirs.Count - 1; i >= 1; i--)
        {
            Vector3 dPrev = dirs[i - 1].normalized;
            Vector3 dNext = dirs[i].normalized;
            if (Vector3.Angle(dPrev, dNext) < 1f) continue;

            float lenPrev = Vector3.Distance(pts[i - 1], pts[i]);
            float lenNext = Vector3.Distance(pts[i], pts[i + 1]);
            float cut = Mathf.Min(CornerCutDistance, lenPrev * 0.5f, lenNext * 0.5f);
            if (cut < 0.05f) continue;

            Vector3 entry = pts[i] - dPrev * cut;
            Vector3 exit  = pts[i] + dNext * cut;
            pts[i] = entry;
            pts.Insert(i + 1, exit);
            dirs.Insert(i, exit - entry);
            lanes.Insert(i, lanes[i]);      // start swinging toward the outgoing lane in the corner
            cells.Insert(i, cells[i - 1]);  // corner cell = incoming segment's destination
        }
    }

    Quaternion BodyRotationFromDir(Vector3 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        return Quaternion.Euler(0f, 0f, angle);
    }

    IEnumerator LaneGlide(AgentActionResult result, float duration)
    {
        // In-place lane switch (wall/corner ahead): the cell doesn't change — retarget
        // the lane channel and let it drift over the beat. Cruise momentum is left
        // alone so a following drive resumes at speed.
        SetLaneTarget(result.LaneAfter);
        Vector3 center = _space.CellToWorld(result.To);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = center + StepLaneOffset();
            yield return null;
        }
        transform.position = center + _laneOffsetVisual;
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

    IEnumerator Turn(AgentActionResult result, float duration)
    {
        // Standalone in-place pivot (a turn with no travel batch around it). Rotation
        // starts from the CURRENT lagged heading — never snaps back to the exact grid
        // facing — and the lane offset swings via the channel as its cardinal rotates.
        Quaternion a = _hasBodyRot && body != null
            ? _bodyRot
            : BodyRotation(result.FacingBefore);
        Quaternion b = BodyRotation(result.FacingAfter);
        SetLaneTarget(result.LaneAfter);
        Vector3 center = _space.CellToWorld(result.From);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _bodyRot = Quaternion.Slerp(a, b, t);
            if (body != null) body.transform.localRotation = _bodyRot;
            transform.position = center + StepLaneOffset();
            yield return null;
        }

        _bodyRot = b;
        _hasBodyRot = true;
        if (body != null) body.transform.localRotation = b;
        transform.position = center + _laneOffsetVisual;
    }

    // Pooled pop icon: one persistent child + a sprite cache, so every fare/pickup beat
    // doesn't allocate a GameObject and hit Resources.Load (hitches near stops).
    GameObject     _popIcon;
    SpriteRenderer _popRenderer;
    static readonly Dictionary<string, Sprite> _popSpriteCache = new Dictionary<string, Sprite>();

    IEnumerator Pop(string spritePath, float duration)
    {
        if (_popIcon == null)
        {
            _popIcon = new GameObject("Pop");
            _popIcon.transform.SetParent(transform, false);
            _popRenderer = _popIcon.AddComponent<SpriteRenderer>();
            _popRenderer.sortingOrder = 999;
        }

        if (!_popSpriteCache.TryGetValue(spritePath, out Sprite sprite) || sprite == null)
        {
            sprite = Resources.Load<Sprite>(spritePath);
            _popSpriteCache[spritePath] = sprite;
        }
        _popRenderer.sprite = sprite;
        _popIcon.SetActive(true);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _popIcon.transform.localPosition = new Vector3(0f, 0.45f + 0.35f * t, 0f);
            _popRenderer.color = new Color(1f, 1f, 1f, 1f - t);
            yield return null;
        }

        _popIcon.SetActive(false);
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
