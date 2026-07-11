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

    // Two-lane rendering: must match the traffic lane spacing so the jeepney
    // lines up with the cars. Pushed by the drive controller from the built
    // route's metrics (RouteContext.LaneOffset — scene art ±3, placeholder ±1.35).
    [System.NonSerialized] public float LaneVisualOffset = RoadMetrics.PlaceholderLaneOffset;

    // Smoothed lane-offset channel, mirroring JeepneyController's model: a signed
    // lateral SCALAR (SmoothDamped at Manual's laneSmoothTime) composed along a
    // smoothed "left of travel" BASIS (RouteMath.SmoothedLeft over the batch
    // centerline). Keeping the same lane side through a turn holds a constant
    // scalar while the basis sweeps the corner arc — the old world-cardinal
    // vector channel swung across the road on a straight chord every corner and
    // jumped 2×offset on rebinds.
    float   _laneScalar;          // current signed offset (world units along _laneLeft)
    float   _laneScalarVel;       // SmoothDamp velocity
    float   _laneScalarTarget;    // ±LaneVisualOffset (0 outside lane mode)
    Vector2 _laneLeft = Vector2.up;   // smoothed left-of-travel basis, persistent across beats/batches
    const float LaneSmoothTime = 0.28f;   // matches JeepneyController.laneSmoothTime

    // Rolling tail of the previous batch's centerline (~2×basis half-length) so
    // the smoothing window reaches back across batch seams; without it a corner
    // at a streaming boundary kinks the basis. Cleared on genuine teleports.
    readonly List<Vector2> _lineTail = new List<Vector2>();

    Vector3 LaneOffsetVector => (Vector3)(_laneLeft * _laneScalar);

    // True while an animation coroutine owns transform.position. Between beats
    // (wait/service/logic steps) Update keeps the lane channel drifting instead —
    // a half-finished lane change must never freeze mid-drift like Manual never does.
    bool _animating;

    // Floor of the corner chamfer on each leg so the jeepney sweeps a turn at
    // constant speed (with the lagged body heading) instead of pivoting on a
    // vertex. The effective cut scales with the lane offset — on the wide scene
    // road a 0.9 chamfer is far too short and the lane swing would visibly pass
    // through the centerline mid-corner.
    const float CornerCutDistance = 0.9f;

    float CornerCut() => Mathf.Max(CornerCutDistance, LaneVisualOffset);

    /// <summary>Supplies the sim's current world-cardinal lane (or -1 outside lane
    /// mode) for poses that don't come from an action result (SnapTo after a reset).
    /// Wired by the drive controller.</summary>
    public System.Func<int> LaneCardinalSource;

    // ---- Traffic collision (view layer — the deterministic sim is untouched) ----

    // Decaying away-from-the-car nudge composed into the lane channel while a
    // soft contact is live (mirrors JeepneyController.ApplySoftTrafficContact).
    float _contactNudgeScalar;
    float _contactNudgeUntil;

    // Distance the jeepney may still advance before rear-ending the same-lane
    // car ahead — pushed every traffic tick, +inf when the lane is clear.
    // Expressed as remaining distance because the view doesn't track arc-length.
    float _followGateRemaining = float.PositiveInfinity;
    float _gateStallSince = -1f;
    float _gateSuppressedUntil;
    const float GateStallTimeout = 2.5f;

    /// <summary>
    /// Traffic contact: bleeds cruise speed and leans the lane channel away from
    /// the car for a beat — the same bump feel Manual has. Purely visual pacing;
    /// the program's step results are unchanged.
    /// </summary>
    public void ApplySoftTrafficContact(Vector2 vehiclePosition)
    {
        _cruiseSpeed *= 0.55f;
        _speedVel = 0f;

        // Project the away-direction onto the lane basis (Manual's pattern): the
        // nudge is a lateral lean, so only its cross-road component matters.
        Vector3 away = transform.position - (Vector3)vehiclePosition;
        float side = Vector2.Dot(new Vector2(away.x, away.y), _laneLeft);
        float sign = Mathf.Abs(side) > 1e-4f ? Mathf.Sign(side) : 1f;
        _contactNudgeScalar = sign * (LaneVisualOffset * 0.35f);
        _contactNudgeUntil = Time.time + 0.35f;
    }

    /// <summary>Remaining forward distance before the same-lane car ahead
    /// (+inf = clear). Enforced by the cruise loop so the jeepney queues at the
    /// car's tail instead of passing through it between grid cells.</summary>
    public void SetTrafficFollowGate(float remaining)
    {
        if (Time.time < _gateSuppressedUntil) return;
        _followGateRemaining = remaining;
    }

    /// <summary>Signed lane side for a world-cardinal lane direction against the
    /// travel direction: +1 when the lane sits on the LEFT of travel, -1 on the
    /// right. On straights the dot is ±1; on chamfer diagonals ±0.707 — the sign
    /// still resolves the correct side.</summary>
    public static float LaneSign(Vector2 laneDir, Vector2 travelDir)
    {
        float dot = Vector2.Dot(laneDir, new Vector2(-travelDir.y, travelDir.x));
        return dot >= 0f ? 1f : -1f;
    }

    float LaneScalarFor(int laneCardinal, Vector2 travelDir)
    {
        if (laneCardinal < 0 || _space == null) return 0f;
        return LaneVisualOffset * LaneSign(_space.FacingDirection(laneCardinal), travelDir);
    }

    void SetLaneTarget(int laneCardinal, Vector2 travelDir)
        => _laneScalarTarget = LaneScalarFor(laneCardinal, travelDir);

    // Half-window of the smoothed lateral basis. Must exceed |offset|·π/4 (see
    // RouteMath.SmoothedLeft) — mirrors JeepneyController's basisHalf.
    float LaneBasisHalf() => Mathf.Max(CornerCut(), LaneVisualOffset * 1.2f);

    /// <summary>Advances the lane-offset channel one frame toward its target and
    /// returns the current offset. Every animation frame composes position as
    /// centerline + this, so lateral motion is always SmoothDamp-continuous —
    /// at Manual's pace, independent of the beat duration.</summary>
    Vector3 StepLaneOffset()
    {
        float target = _laneScalarTarget;
        if (Time.time < _contactNudgeUntil)
            target += _contactNudgeScalar;
        _laneScalar = Mathf.SmoothDamp(_laneScalar, target, ref _laneScalarVel, LaneSmoothTime);
        return LaneOffsetVector;
    }

    void Update()
    {
        if (_animating || _space == null) return;
        // Recover the centerline from the current pose and re-compose with the stepped
        // offset so lateral drift stays alive across the gaps between animation beats.
        Vector3 center = transform.position - LaneOffsetVector;
        transform.position = center + StepLaneOffset();
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
        Vector2 heading = _space.FacingDirection(facing);
        _laneLeft = new Vector2(-heading.y, heading.x);
        _lineTail.Clear();   // a teleport breaks centerline continuity
        int lane = LaneCardinalSource != null ? LaneCardinalSource() : -1;
        SetLaneTarget(lane, heading);
        _laneScalar    = _laneScalarTarget;   // teleports stay teleports — no drift-in
        _laneScalarVel = 0f;
        transform.position = _space.CellToWorld(cell) + LaneOffsetVector;
        SetSortOrder(cell);
        SetBodyFacing(facing);

        // A teleport breaks motion continuity: drop cruise momentum and re-pin the
        // lagged heading to the snapped facing so the next drive eases in cleanly.
        // Traffic gate/contact state belongs to the old position — clear it.
        _cruiseSpeed = 0f;
        _speedVel    = 0f;
        _followGateRemaining = float.PositiveInfinity;
        _gateStallSince      = -1f;
        _gateSuppressedUntil = 0f;
        _contactNudgeUntil   = 0f;
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
                // A blocked lateral keeps cruise momentum — Manual ignores an impossible
                // lane input without halting; the sideways nudge remains as feedback.
                if (result.Blocked)
                    yield return Bump((result.FacingBefore + 3) % 4, duration, killMomentum: false);
                else if (result.From != result.To)
                    yield return PlayContinuousPath(new[] { result }, duration);
                else
                    yield return LaneGlide(result, duration);
                break;

            case "moveRight":
                if (result.Blocked)
                    yield return Bump((result.FacingBefore + 1) % 4, duration, killMomentum: false);
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

        // Per-segment signed lane targets: the sign comes from the segment's own
        // travel direction, so the scalar is CONSTANT while the lane side is —
        // through corners included. Only genuine lane changes move it.
        var laneTargets = new float[segCount];
        for (int i = 0; i < segCount; i++)
            laneTargets[i] = LaneScalarFor(lanes[i], new Vector2(dirs[i].x, dirs[i].y));

        // Smoothing line for the lateral basis: previous batch's tail + this
        // batch's centerline, so the back half-window doesn't clamp at a batch
        // seam. (The FORWARD window still clamps at the batch's last point, so a
        // corner in the final half-window eases in slightly early — same as
        // Manual's cursor at route end.)
        float basisHalf = LaneBasisHalf();
        if (_lineTail.Count > 0 &&
            (_lineTail[_lineTail.Count - 1] - new Vector2(pts[0].x, pts[0].y)).sqrMagnitude > 1e-4f)
            _lineTail.Clear();   // discontinuity — never smooth across it
        int tailPts = Mathf.Max(0, _lineTail.Count - 1);   // tail's last point IS pts[0]
        var line = new Vector2[tailPts + pts.Count];
        float tailArc = 0f;
        for (int i = 0; i < tailPts; i++) line[i] = _lineTail[i];
        for (int i = 1; i < _lineTail.Count; i++)
            tailArc += Vector2.Distance(_lineTail[i - 1], _lineTail[i]);
        for (int i = 0; i < pts.Count; i++)
            line[tailPts + i] = new Vector2(pts[i].x, pts[i].y);

        // Constant cruise speed pinned to the ORIGINAL cell count, so chamfered corners
        // (slightly shorter path) don't change the pace between batches ("pumping").
        float duration    = Mathf.Max(0.04f, secondsPerStep * travelSegs);
        float targetSpeed = total / duration;

        if (!_hasBodyRot)
        {
            _bodyRot    = body != null ? body.transform.localRotation : Quaternion.identity;
            _hasBodyRot = true;
        }

        _animating = true;
        try
        {
            float traveled = 0f;
            while (traveled < total)
            {
                // Ease the speed up only when launching from rest (_cruiseSpeed near 0); mid-drive
                // batches start already at targetSpeed, so there's no per-batch slowdown.
                _cruiseSpeed = Mathf.SmoothDamp(_cruiseSpeed, targetSpeed, ref _speedVel, AccelSmoothTime);
                float step   = Mathf.Max(_cruiseSpeed, 0.0001f) * Time.deltaTime;

                // Same-lane car ahead: queue at its tail instead of passing through.
                if (step > _followGateRemaining)
                {
                    if (_gateStallSince < 0f) _gateStallSince = Time.time;
                    if (Time.time - _gateStallSince > GateStallTimeout)
                    {
                        // Deadlock safety: a frozen program is worse than a
                        // moment of overlap — release the gate for a while.
                        _gateStallSince = -1f;
                        _gateSuppressedUntil = Time.time + 3f;
                        _followGateRemaining = float.PositiveInfinity;
                    }
                    else
                    {
                        step = Mathf.Max(0f, _followGateRemaining);
                        _cruiseSpeed *= 0.5f;
                        _speedVel = 0f;
                    }
                }
                else if (_gateStallSince >= 0f)
                    _gateStallSince = -1f;

                if (!float.IsPositiveInfinity(_followGateRemaining))
                    _followGateRemaining = Mathf.Max(0f, _followGateRemaining - step);

                traveled    += step;
                float dist   = Mathf.Min(traveled, total);
                float along  = tailArc + dist;

                int s = 0;
                while (s < segCount - 1 && dist > segLen[s]) { dist -= segLen[s]; s++; }
                float u = segLen[s] > 1e-4f ? Mathf.Clamp01(dist / segLen[s]) : 1f;

                _laneScalarTarget = laneTargets[s];
                _laneLeft = RouteMath.SmoothedLeft(line, along, basisHalf);
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
            _laneLeft = RouteMath.SmoothedLeft(line, tailArc + total, basisHalf);
            transform.position = pts[pts.Count - 1] + LaneOffsetVector;
            if (trailingLane != int.MinValue)
                SetLaneTarget(trailingLane, new Vector2(dirs[dirs.Count - 1].x, dirs[dirs.Count - 1].y));
            SetSortOrder(cells[cells.Count - 1]);
            KeepLineTail(line, basisHalf);
        }
        finally { _animating = false; }
    }

    /// <summary>Retains the last ~2×<paramref name="basisHalf"/> of centerline so the
    /// next batch's smoothing window reaches back across the seam.</summary>
    void KeepLineTail(Vector2[] line, float basisHalf)
    {
        _lineTail.Clear();
        float want = basisHalf * 2f;
        float kept = 0f;
        _lineTail.Add(line[line.Length - 1]);
        for (int i = line.Length - 2; i >= 0 && kept < want; i--)
        {
            kept += Vector2.Distance(line[i], line[i + 1]);
            _lineTail.Add(line[i]);
        }
        _lineTail.Reverse();
    }

    /// <summary>Chamfers every interior vertex where the heading changes: the sharp
    /// corner point becomes an entry/exit pair CornerCutDistance up each leg (clamped
    /// to half the shorter leg), and the inserted diagonal blends heading + starts the
    /// outgoing lane swing early — a swept corner at constant speed, matching Manual's
    /// cornerEaseDistance feel.</summary>
    void ApplyCornerCuts(List<Vector3> pts, List<Vector3> dirs,
                         List<int> lanes, List<Vector2Int> cells)
    {
        for (int i = dirs.Count - 1; i >= 1; i--)
        {
            Vector3 dPrev = dirs[i - 1].normalized;
            Vector3 dNext = dirs[i].normalized;
            if (Vector3.Angle(dPrev, dNext) < 1f) continue;

            float lenPrev = Vector3.Distance(pts[i - 1], pts[i]);
            float lenNext = Vector3.Distance(pts[i], pts[i + 1]);
            float cut = Mathf.Min(CornerCut(), lenPrev * 0.5f, lenNext * 0.5f);
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
        float angle = VehicleFacing.FacingAngleDegrees(
            new Vector2(dir.x, dir.y), VehicleFacing.ArtBaseFacing);
        return Quaternion.Euler(0f, 0f, angle);
    }

    IEnumerator LaneGlide(AgentActionResult result, float duration)
    {
        // In-place lane switch (wall/corner ahead): the cell doesn't change — retarget
        // the lane scalar (basis frozen — the vehicle isn't travelling) and let it
        // drift over the beat. The beat may end mid-drift at high playback speed;
        // Update() finishes the drift between beats, so there is never a snap.
        // Cruise momentum is left alone so a following drive resumes at speed.
        SetLaneTarget(result.LaneAfter, _space.FacingDirection(result.FacingAfter));
        Vector3 center = _space.CellToWorld(result.To);

        _animating = true;
        try
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position = center + StepLaneOffset();
                yield return null;
            }
            transform.position = center + LaneOffsetVector;
            SetSortOrder(result.To);
        }
        finally { _animating = false; }
    }

    IEnumerator Bump(int facing, float duration, bool killMomentum = true)
    {
        if (killMomentum)
            _cruiseSpeed = 0f;   // hit a wall/car ahead — kill momentum so the next drive eases in
        _animating = true;
        try
        {
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
        finally { _animating = false; }
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
        Vector2 headingA = _space.FacingDirection(result.FacingBefore);
        Vector2 headingB = _space.FacingDirection(result.FacingAfter);
        // Same lane side before and after a turn ⇒ same scalar; the basis rotating
        // with the pivot is what sweeps the offset point around the corner arc.
        SetLaneTarget(result.LaneAfter, headingB);
        Vector3 center = _space.CellToWorld(result.From);

        _animating = true;
        try
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                _bodyRot = Quaternion.Slerp(a, b, t);
                if (body != null) body.transform.localRotation = _bodyRot;
                Vector2 heading = Vector2.Lerp(headingA, headingB, t);
                if (heading.sqrMagnitude > 1e-4f)
                    _laneLeft = new Vector2(-heading.y, heading.x).normalized;
                transform.position = center + StepLaneOffset();
                yield return null;
            }

            _bodyRot = b;
            _hasBodyRot = true;
            if (body != null) body.transform.localRotation = b;
            _laneLeft = new Vector2(-headingB.y, headingB.x);
            transform.position = center + LaneOffsetVector;
        }
        finally { _animating = false; }
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
        float angle = VehicleFacing.FacingAngleDegrees(dir, VehicleFacing.ArtBaseFacing);
        return Quaternion.Euler(0f, 0f, angle);
    }
}
