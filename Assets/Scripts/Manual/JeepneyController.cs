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
    [Tooltip("Automatic forward acceleration (low = sluggish start).")]
    [SerializeField] private float acceleration = 4.5f;

    [Tooltip("Maximum forward speed on the road.")]
    [SerializeField] private float topSpeed = 4f;

    [Tooltip("Deceleration while holding Space or while input is locked.")]
    [SerializeField] private float brakingForce = 16f;

    [Header("Lane Assist")]
    [Tooltip("Meters between lane centers.")]
    [SerializeField] private float laneWidth = 1.35f;

    [Tooltip("How many lanes the jeepney can move away from the center lane.")]
    [SerializeField] private int laneStepsEachSide = 1;

    [Tooltip("How quickly the jeepney slides into the selected lane.")]
    [SerializeField] private float laneChangeSpeed = 4f;

    [Tooltip("How quickly the jeepney visually turns to match the road.")]
    [SerializeField] private float rotationFollowSpeed = 8f;

    [Header("Off-road")]
    [SerializeField] private float offRoadSpeedFactor = 0.45f;

    [Header("Fuel")]
    [SerializeField] private float fuelDrainPerSecond = 0.004f;

    private Rigidbody2D _rb;
    private float _fuel = 1f;
    private Vector2[] _driveLine;
    private float _routeDistance;
    private float _routeLength;
    private float _routeSpeed;
    private float _laneOffset;
    private int   _targetLane;
    private bool  _aHeld;
    private bool  _dHeld;

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

            if (kb.spaceKey.isPressed) braking = true;
        }
        else
        {
            _aHeld = false;
            _dHeld = false;
        }

        bool atRouteEnd = _routeDistance >= _routeLength - 0.01f;
        bool driving = !InputLocked && !braking && !atRouteEnd;

        // Fuel only burns while the jeepney is driving itself forward; an
        // empty tank limps along.
        if (driving)
            _fuel = Mathf.Max(0f, _fuel - fuelDrainPerSecond * Time.fixedDeltaTime);
        float fuelFactor = _fuel > 0f ? 1f : 0.5f;

        float cap = topSpeed * (OffRoad ? offRoadSpeedFactor : 1f) * fuelFactor;
        float targetSpeed = driving ? cap : 0f;
        float speedRate = driving && _routeSpeed < targetSpeed ? acceleration : brakingForce;
        _routeSpeed = Mathf.MoveTowards(_routeSpeed, targetSpeed, speedRate * Time.fixedDeltaTime);

        _routeDistance = Mathf.Min(_routeDistance + _routeSpeed * Time.fixedDeltaTime, _routeLength);
        if (_routeDistance >= _routeLength - 0.001f)
            _routeSpeed = 0f;

        _laneOffset = Mathf.MoveTowards(_laneOffset, _targetLane * laneWidth,
                                        laneChangeSpeed * Time.fixedDeltaTime);

        Vector2 center = RouteMath.PointAt(_driveLine, _routeDistance);
        Vector2 direction = RouteMath.DirectionAt(_driveLine, _routeDistance + 0.1f);
        Vector2 left = new Vector2(-direction.y, direction.x);
        Vector2 targetPosition = center + left * _laneOffset;

        Vector2 previous = _rb.position;
        _rb.MovePosition(targetPosition);
        _rb.linearVelocity = Time.fixedDeltaTime > 0f
            ? (targetPosition - previous) / Time.fixedDeltaTime
            : Vector2.zero;

        float targetAngle = Vector2.SignedAngle(Vector2.up, direction);
        float angle = Mathf.LerpAngle(_rb.rotation, targetAngle,
                                      rotationFollowSpeed * Time.fixedDeltaTime);
        _rb.MoveRotation(angle);
    }

    void FixedUpdateFallback()
    {
        bool braking = false;

        if (!InputLocked && Keyboard.current != null)
            braking = Keyboard.current.spaceKey.isPressed;

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
        _routeDistance = _routeLength > 0f
            ? RouteMath.NearestDistanceAlong(waypoints, _rb.position, out _)
            : 0f;

        if (!preserveLane)
        {
            _targetLane = 0;
            _laneOffset = 0f;
            _routeSpeed = 0f;
        }
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

        transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, rotationDegrees));
    }
}
