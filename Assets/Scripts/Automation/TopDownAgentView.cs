using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Top-down animated agent for Automation mode. Reuses the Manual jeepney
/// sprite, moves it along the top-down road, and rotates the body to face the
/// current grid direction.
/// </summary>
public class TopDownAgentView : MonoBehaviour, IPathAgentView
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

    // Full needle deflection lines up with Manual's topSpeed (4 world units/sec).
    const float TopSpeed = 4f;

    public float CurrentSpeed => _cruiseSpeed;
    public float CurrentSpeed01 => TopSpeed > 0f ? Mathf.Clamp01(_cruiseSpeed / TopSpeed) : 0f;

    // -------------------------------------------------------------------------

    public void Init(IGridSpace space, Vector2Int cell, int facing)
    {
        _space = space;
        _stopView = space as IStopView;
        SnapTo(cell, facing);
    }

    public void SnapTo(Vector2Int cell, int facing)
    {
        transform.position = _space.CellToWorld(cell);
        SetSortOrder(cell);
        SetBodyFacing(facing);

        // A teleport breaks motion continuity: drop cruise momentum and re-pin the
        // lagged heading to the snapped facing so the next drive eases in cleanly.
        _cruiseSpeed = 0f;
        _speedVel    = 0f;
        _bodyRot     = body != null ? body.transform.localRotation : Quaternion.identity;
        _hasBodyRot  = true;
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
                    yield return MoveTo(result.From, result.To, duration);
                break;

            case "turnLeft":
            case "turnRight":
                yield return Turn(result.FacingBefore, result.FacingAfter, duration);
                break;

            case "moveLeft":
                // Slide sideways without rotating — MoveTo leaves the body facing untouched,
                // so the jeepney drifts into the lane while still pointing ahead.
                if (result.Blocked)
                    yield return Bump((result.FacingBefore + 3) % 4, duration);
                else
                    yield return MoveTo(result.From, result.To, duration);
                break;

            case "moveRight":
                if (result.Blocked)
                    yield return Bump((result.FacingBefore + 1) % 4, duration);
                else
                    yield return MoveTo(result.From, result.To, duration);
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
            if (result.Action == "moveForward" && !result.Blocked)
                hasForwardMove = true;

        if (hasForwardMove)
        {
            yield return PlayContinuousPath(moves, secondsPerStep);
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

    IEnumerator PlayContinuousPath(IReadOnlyList<AgentActionResult> moves, float secondsPerStep)
    {
        // Stitch the forward moves into one world-space polyline so the jeepney drives the
        // whole stretch as a single heavy motion — accelerate, cruise, settle — instead of
        // easing to a near-stop at every cell. Rotation lags the travel direction so corners
        // feel weighty, like a real loaded jeepney leaning into the turn.
        var pts     = new List<Vector3>();
        var toCells = new List<Vector2Int>();
        foreach (AgentActionResult result in moves)
        {
            if (result.Action != "moveForward" || result.Blocked) continue;
            if (pts.Count == 0) pts.Add(_space.CellToWorld(result.From));
            pts.Add(_space.CellToWorld(result.To));
            toCells.Add(result.To);
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

            Vector3 dir = pts[s + 1] - pts[s];
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

    IEnumerator MoveTo(Vector2Int from, Vector2Int to, float duration)
    {
        Vector3 a = _space.CellToWorld(from);
        Vector3 b = _space.CellToWorld(to);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            transform.position = Vector3.Lerp(a, b, t);
            if (t > 0.5f) SetSortOrder(to);
            yield return null;
        }

        transform.position = b;
        SetSortOrder(to);
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

    IEnumerator Turn(int fromFacing, int toFacing, float duration)
    {
        Quaternion a = BodyRotation(fromFacing);
        Quaternion b = BodyRotation(toFacing);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            body.transform.localRotation = Quaternion.Slerp(a, b, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        body.transform.localRotation = b;
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
