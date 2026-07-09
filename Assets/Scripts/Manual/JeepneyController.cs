using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Real-time jeepney driving for Manual Mode: the jeepney rolls forward
/// automatically along the route, A/D change lanes, and Space brakes. Route
/// turns are handled by driver assist so the first player verb is lateral road
/// positioning for overtaking.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class JeepneyController : MonoBehaviour
{
    [Header("Driving — tune the heavy feel here")]
    [Tooltip("Automatic forward acceleration (used by the fallback physics path).")]
    [SerializeField] private float acceleration = 4.5f;

    [Tooltip("Maximum forward speed on the road.")]
    [SerializeField] private float topSpeed = 4f;

    [Tooltip("Deceleration while holding Space or while input is locked (fallback path).")]
    [SerializeField] private float brakingForce = 16f;

    [Header("Heavy feel — momentum smoothing (route follow)")]
    [Tooltip("Seconds to ease up toward top speed. Higher = heavier, more sluggish start.")]
    [SerializeField] private float accelSmoothTime = 1.2f;

    [Tooltip("Seconds to bleed off speed when braking / stopping. Higher = longer coast.")]
    [SerializeField] private float brakeSmoothTime = 0.55f;

    [Tooltip("Seconds to drift into the selected lane. Higher = heavier, lazier lane changes.")]
    [SerializeField] private float laneSmoothTime = 0.28f;

    [Tooltip("Within this many meters of a 90° corner, the jeep eases to the lane " +
             "center so it tracks the turn cleanly instead of cutting/reversing.")]
    [SerializeField] private float cornerEaseDistance = 2.5f;

    [Header("Lane Assist")]
    [Tooltip("Meters between lane centers.")]
    [SerializeField] private float laneWidth = 1.35f;

    [Tooltip("How many lanes the jeepney can move away from the center lane.")]
    [SerializeField] private int laneStepsEachSide = 1;

    [Tooltip("How quickly the jeepney visually turns to match the road (low = laggy, heavy body).")]
    [SerializeField] private float rotationFollowSpeed = 4f;

    [Header("Off-road")]
    [SerializeField] private float offRoadSpeedFactor = 0.45f;

    [Header("Fuel")]
    [SerializeField] private float fuelDrainPerSecond = RefuelMath.FuelDrainPerSecond;

    private Rigidbody2D _rb;
    private float _fuel = 1f;
    private Vector2[] _driveLine;
    private float _routeDistance;
    private float _routeLength;
    private float _routeSpeed;
    private float _speedVel;     // SmoothDamp velocity for forward speed momentum
    private float _laneOffset;
    private float _laneVel;      // SmoothDamp velocity for lane-change drift
    private int   _targetLane;
    private bool  _aHeld;
    private bool  _dHeld;
    private bool  _spaceHeld;      // previous-frame Space state (FixedUpdate-safe edge detect)
    private bool  _brakeToggled;   // latched brake state when Brake Mode = Toggle
    private float _trafficSlowUntil;
    private float _trafficLaneNudge;
    private float _trafficNudgeUntil;
    private float _trafficFollowLimit = float.PositiveInfinity;

    // -------------------------------------------------------------------------
    // Public state

    /// <summary>Blocks player input (breakdowns, results) without freezing physics.</summary>
    public bool InputLocked { get; set; }

    /// <summary>True while the jeepney is off the road (set by the drive controller).</summary>
    public bool OffRoad { get; set; }

    public float CurrentSpeed   => _driveLine != null ? _routeSpeed :
                                   (_rb != null ? _rb.linearVelocity.magnitude : 0f);
    public float CurrentSpeed01 => topSpeed > 0f ? Mathf.Clamp01(CurrentSpeed / topSpeed) : 0f;
    public float Fuel01         => _fuel;
    public void  Refuel()       => _fuel = 1f;   // tank back to full (after the refuel mini-game)

    // -------------------------------------------------------------------------

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        // Motion is driven by the route follower, so built-in damping would
        // fight the lane-change positioning.
        _rb.linearDamping = 0f;
    }

    void FixedUpdate()
    {
        if (_driveLine != null && _driveLine.Length >= 2)
        {
            FixedUpdateRouteFollow();
            return;
        }

        FixedUpdateFallback();
    }

    void FixedUpdateRouteFollow()
    {
        bool braking  = false;

        if (!InputLocked && Keyboard.current != null)
        {
            var kb = Keyboard.current;
            bool aPressed = kb.aKey.isPressed;
            bool dPressed = kb.dKey.isPressed;

            if (aPressed && !_aHeld)
                _targetLane = Mathf.Clamp(_targetLane + 1, -laneStepsEachSide, laneStepsEachSide);
            if (dPressed && !_dHeld)
                _targetLane = Mathf.Clamp(_targetLane - 1, -laneStepsEachSide, laneStepsEachSide);

            _aHeld = aPressed;
            _dHeld = dPressed;

            braking = ReadBrake(kb);
        }
        else
        {
            _aHeld = false;
            _dHeld = false;
            _spaceHeld    = false;
            _brakeToggled = false;
        }

        bool atRouteEnd = _routeDistance >= _routeLength - 0.01f;
        bool driving = !InputLocked && !braking && !atRouteEnd;

        // Fuel only burns while the jeepney is driving itself forward; an
        // empty tank limps along.
        if (driving)
            _fuel = Mathf.Max(0f, _fuel - fuelDrainPerSecond * Time.fixedDeltaTime);
        float fuelFactor = _fuel > 0f ? 1f : 0.5f;

        // Forward speed eases in/out like a heavy vintage jeepney: a sluggish
        // build toward top speed and a long coast when braking or stopping.
        float cap = topSpeed * (OffRoad ? offRoadSpeedFactor : 1f) * fuelFactor;
        if (Time.time < _trafficSlowUntil)
            cap *= 0.42f;
        float targetSpeed = driving ? cap : 0f;
        float smoothTime = targetSpeed >= _routeSpeed ? accelSmoothTime : brakeSmoothTime;
        _routeSpeed = Mathf.SmoothDamp(_routeSpeed, targetSpeed, ref _speedVel,
                                       smoothTime, Mathf.Infinity, Time.fixedDeltaTime);
        if (_routeSpeed < 0f) _routeSpeed = 0f;

        float proposedDistance = Mathf.Min(_routeDistance + _routeSpeed * Time.fixedDeltaTime, _routeLength);
        if (proposedDistance > _trafficFollowLimit)
        {
            // A car ahead in this lane is a wall, not a ghost: bump into its tail and
            // hold there (never yanked backward if the limit dips behind us — e.g. a
            // car merging in beside the jeep). A/D still changes lanes to overtake.
            proposedDistance = Mathf.Max(_routeDistance, Mathf.Max(0f, _trafficFollowLimit));
            _routeSpeed = 0f;
            _speedVel   = 0f;
        }
        _routeDistance = proposedDistance;
        if (_routeDistance >= _routeLength - 0.001f)
        {
            _routeSpeed = 0f;
            _speedVel   = 0f;
        }

        // Lane changes drift in rather than snapping. Near a 90° corner the lane
        // target eases to the centerline so the jeep tracks the turn cleanly — the
        // perpendicular lane offset flips basis at the vertex, and holding an offset
        // there throws the jeep sideways/backward.
        float distToCorner = RouteMath.DistanceToNearestCorner(_driveLine, _routeDistance);
        float cornerFactor = cornerEaseDistance > 0f
            ? Mathf.Clamp01(distToCorner / cornerEaseDistance)
            : 1f;
        float laneTarget = _targetLane * laneWidth * cornerFactor;
        if (Time.time < _trafficNudgeUntil)
            laneTarget += _trafficLaneNudge * cornerFactor;
        _laneOffset = Mathf.SmoothDamp(_laneOffset, laneTarget, ref _laneVel,
                                       laneSmoothTime, Mathf.Infinity, Time.fixedDeltaTime);

        Vector2 center = RouteMath.PointAt(_driveLine, _routeDistance);
        Vector2 direction = RouteMath.DirectionAt(_driveLine, _routeDistance + 0.1f);
        Vector2 left = new Vector2(-direction.y, direction.x);
        Vector2 targetPosition = center + left * _laneOffset;

        Vector2 previous = _rb.position;
        _rb.MovePosition(targetPosition);
        Vector2 motion = targetPosition - previous;
        _rb.linearVelocity = Time.fixedDeltaTime > 0f
            ? motion / Time.fixedDeltaTime
            : Vector2.zero;

        // Face the way we actually move, not the raw segment lookahead. The lane
        // basis flips at 90° vertices and a streamed fold can momentarily point a
        // segment backward, which would swing the body ~180° ("driving backward").
        // Route distance only ever advances, so the movement vector is always the
        // true heading. While essentially stopped (or drifting purely sideways at
        // a stop) keep the last heading so the body doesn't spin or face sideways.
        float targetAngle = _rb.rotation;
        if (_routeSpeed > 0.02f && motion.sqrMagnitude > 1e-8f)
            targetAngle = Vector2.SignedAngle(Vector2.up, motion);
        float angle = Mathf.LerpAngle(_rb.rotation, targetAngle,
                                      rotationFollowSpeed * Time.fixedDeltaTime);
        _rb.MoveRotation(angle);
    }

    /// <summary>
    /// Reads the brake per the Brake Mode setting. Hold: brake while Space is
    /// held. Toggle: tap Space to latch/unlatch braking. Edge detection compares
    /// against the previous frame (not <c>wasPressedThisFrame</c>) so it stays
    /// correct in FixedUpdate. Falls back to Hold when no settings are loaded.
    /// </summary>
    bool ReadBrake(Keyboard kb)
    {
        bool spaceNow  = kb.spaceKey.isPressed;
        bool spaceEdge = spaceNow && !_spaceHeld;
        _spaceHeld = spaceNow;

        BrakeMode mode = SettingsManager.Instance != null
            ? SettingsManager.Instance.BrakeMode
            : (SaveSystem.Current != null
                ? (BrakeMode)SaveSystem.Current.settings.brakeMode
                : BrakeMode.Hold);

        if (mode == BrakeMode.Toggle)
        {
            if (spaceEdge) _brakeToggled = !_brakeToggled;
            return _brakeToggled;
        }

        _brakeToggled = false;   // keep the latch clear so switching modes is clean
        return spaceNow;
    }

    void FixedUpdateFallback()
    {
        bool braking = false;

        if (!InputLocked && Keyboard.current != null)
            braking = ReadBrake(Keyboard.current);
        else
        {
            _spaceHeld    = false;
            _brakeToggled = false;
        }

        if (!InputLocked && !braking)
            _rb.AddForce(transform.up * acceleration);

        if ((braking || InputLocked) && _rb.linearVelocity.magnitude > 0.01f)
        {
            Vector2 brakeDir = -_rb.linearVelocity.normalized;
            _rb.AddForce(brakeDir * brakingForce);
            if (_rb.linearVelocity.magnitude < 0.35f)
                _rb.linearVelocity = Vector2.zero;
        }

        float cap = topSpeed * (OffRoad ? offRoadSpeedFactor : 1f) * (_fuel > 0f ? 1f : 0.5f);
        if (_rb.linearVelocity.magnitude > cap)
            _rb.linearVelocity = _rb.linearVelocity.normalized * cap;
    }

    // -------------------------------------------------------------------------

    /// <summary>Sets the route the jeepney follows automatically.</summary>
    public void SetDriveLine(Vector2[] waypoints, bool preserveLane = false)
    {
        _driveLine = waypoints;
        _routeLength = waypoints != null && waypoints.Length >= 2
            ? RouteMath.TotalLength(waypoints)
            : 0f;

        if (_rb == null) _rb = GetComponent<Rigidbody2D>();

        if (preserveLane)
        {
            // Streaming only appends to the polyline's tail, so the existing
            // arc-length still points to the same place — keep it. Re-projecting
            // here can snap backward when Manhattan roads fold near themselves.
            _routeDistance = Mathf.Min(_routeDistance, _routeLength);
        }
        else
        {
            _routeDistance = _routeLength > 0f
                ? RouteMath.NearestDistanceAlong(waypoints, _rb.position, out _)
                : 0f;
            _targetLane = 0;
            _laneOffset = 0f;
            _laneVel    = 0f;
            _routeSpeed = 0f;
            _speedVel   = 0f;
        }
        _trafficFollowLimit = float.PositiveInfinity;
    }

    /// <summary>Places the jeepney on the route start, facing along it.</summary>
    public void TeleportTo(Vector2 position, float rotationDegrees)
    {
        if (_rb == null) _rb = GetComponent<Rigidbody2D>();

        _rb.position       = position;
        _rb.rotation       = rotationDegrees;
        _rb.linearVelocity  = Vector2.zero;
        _rb.angularVelocity = 0f;

        _routeSpeed   = 0f;
        _targetLane   = 0;
        _laneOffset   = 0f;
        _aHeld        = false;
        _dHeld        = false;
        _spaceHeld    = false;
        _brakeToggled = false;

        transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, rotationDegrees));
    }

    /// <summary>Route-distance ceiling imposed by the nearest same-lane car ahead,
    /// pushed by <see cref="RoadTrafficController"/> every tick. PositiveInfinity
    /// when the lane is clear.</summary>
    public void SetTrafficFollowLimit(float limit)
    {
        _trafficFollowLimit = limit;
    }

    /// <summary>
    /// Manual traffic contact bleeds speed and leans the lane assist away for a
    /// moment — the "bump" feel on top of the hard follow-limit that stops the
    /// jeepney from passing through the car.
    /// </summary>
    public void ApplySoftTrafficContact(Vector2 vehiclePosition)
    {
        _trafficSlowUntil = Time.time + 0.45f;
        if (_driveLine != null && _driveLine.Length >= 2)
        {
            Vector2 dir = RouteMath.DirectionAt(_driveLine, _routeDistance + 0.1f);
            Vector2 left = new Vector2(-dir.y, dir.x);
            float side = Vector2.Dot((Vector2)transform.position - vehiclePosition, left);
            _trafficLaneNudge = Mathf.Sign(Mathf.Abs(side) < 0.01f ? 1f : side) * laneWidth * 0.35f;
            _trafficNudgeUntil = Time.time + 0.35f;
        }
        _routeSpeed *= 0.55f;
        _speedVel = 0f;
    }
}
