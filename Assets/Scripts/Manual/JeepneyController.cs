using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Real-time jeepney driving for Manual Mode: WASD / arrows via the Input
/// System. Tuned for a heavy, underpowered vintage jeepney: slow acceleration,
/// a modest top speed, strong coasting friction, wide steering, and high
/// lateral grip so it follows its front wheels without drifting.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class JeepneyController : MonoBehaviour
{
    [Header("Driving — tune the heavy feel here")]
    [Tooltip("Forward acceleration when holding W / Up (low = sluggish start).")]
    [SerializeField] private float acceleration = 7f;

    [Tooltip("Reverse acceleration when holding S / Down.")]
    [SerializeField] private float reverseAcceleration = 4f;

    [Tooltip("Maximum forward speed on the road.")]
    [SerializeField] private float topSpeed = 6.5f;

    [Tooltip("Passive deceleration when no throttle is applied (high = heavy stop).")]
    [SerializeField] private float brakingForce = 6f;

    [Header("Steering")]
    [Tooltip("Maximum turn rate in degrees per second (low = wide turning radius).")]
    [SerializeField] private float steeringSpeed = 75f;

    [Tooltip("How quickly the steering wheel catches up to input (lower = heavier delay).")]
    [SerializeField] private float steeringResponse = 3.5f;

    [Tooltip("Forward speed required before steering reaches full effectiveness.")]
    [SerializeField] private float minSteerSpeed = 2.5f;

    [Header("Grip")]
    [Tooltip("How much sideways velocity survives each physics tick (1 = zero sideways slip).")]
    [Range(0f, 1f)]
    [SerializeField] private float lateralGrip = 0.995f;

    [Header("Inertia")]
    [Tooltip("How quickly throttle input reaches the engine (lower = lazier response).")]
    [SerializeField] private float throttleResponse = 4f;

    [Header("Off-road")]
    [SerializeField] private float offRoadSpeedFactor = 0.45f;

    [Header("Fuel")]
    [SerializeField] private float fuelDrainPerSecond = 0.004f;

    private Rigidbody2D _rb;
    private float _fuel = 1f;
    private float _steerInput;    // smoothed steering value
    private float _throttleInput; // smoothed throttle value

    // -------------------------------------------------------------------------
    // Public state

    /// <summary>Blocks player input (breakdowns, results) without freezing physics.</summary>
    public bool InputLocked { get; set; }

    /// <summary>True while the jeepney is off the road (set by the drive controller).</summary>
    public bool OffRoad { get; set; }

    public float CurrentSpeed   => _rb != null ? _rb.linearVelocity.magnitude : 0f;
    public float CurrentSpeed01 => topSpeed > 0f ? Mathf.Clamp01(CurrentSpeed / topSpeed) : 0f;
    public float Fuel01         => _fuel;

    // -------------------------------------------------------------------------

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        // Coasting drag is handled manually by brakingForce so the stop is
        // controllable and heavy. Disable built-in linear damping to avoid
        // double-damping.
        _rb.linearDamping = 0f;
    }

    void FixedUpdate()
    {
        float rawThrottle = 0f;
        float rawSteer    = 0f;

        if (!InputLocked && Keyboard.current != null)
        {
            var kb = Keyboard.current;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    rawThrottle += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  rawThrottle -= 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  rawSteer    += 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) rawSteer    -= 1f;
        }

        // Smooth inputs for a heavy, delayed response.
        _throttleInput = Mathf.MoveTowards(_throttleInput, rawThrottle,
                                           throttleResponse * Time.fixedDeltaTime);
        _steerInput    = Mathf.MoveTowards(_steerInput, rawSteer,
                                           steeringResponse * Time.fixedDeltaTime);

        // Fuel only burns while throttling; an empty tank limps along.
        if (rawThrottle != 0f)
            _fuel = Mathf.Max(0f, _fuel - fuelDrainPerSecond * Time.fixedDeltaTime *
                                  Mathf.Abs(rawThrottle));
        float fuelFactor = _fuel > 0f ? 1f : 0.5f;

        // Throttle along the jeepney's nose.
        if (_throttleInput > 0f)
            _rb.AddForce(transform.up * (acceleration * _throttleInput * fuelFactor));
        else if (_throttleInput < 0f)
            _rb.AddForce(transform.up * (reverseAcceleration * _throttleInput * fuelFactor));

        // Heavy friction: apply a braking force whenever the driver is not
        // pressing the throttle, so the jeepney coasts to a stop but not
        // instantly.
        if (Mathf.Abs(_throttleInput) < 0.01f && _rb.linearVelocity.magnitude > 0.01f)
        {
            Vector2 brakeDir = -_rb.linearVelocity.normalized;
            _rb.AddForce(brakeDir * brakingForce);
        }

        // No-drift grip: resolve velocity into forward and lateral components
        // relative to the jeepney's facing, then bleed almost all sideways slip.
        // This makes the vehicle follow its front wheels rather than slide.
        Vector2 forward     = transform.up;
        float   forwardSpeed = Vector2.Dot(_rb.linearVelocity, forward);
        Vector2 lateral     = _rb.linearVelocity - forward * forwardSpeed;
        _rb.linearVelocity  = forward * forwardSpeed + lateral * lateralGrip;

        // Heavy, wide steering: turn rate is limited and scales with forward
        // speed. Reversing flips the steering direction like a real car.
        float speedFactor = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / minSteerSpeed);
        float turn = _steerInput * steeringSpeed * Mathf.Sign(forwardSpeed) *
                     speedFactor * Time.fixedDeltaTime;
        _rb.MoveRotation(_rb.rotation + turn);

        // Speed cap (tighter off-road or out of fuel).
        float cap = topSpeed * (OffRoad ? offRoadSpeedFactor : 1f) * fuelFactor;
        if (_rb.linearVelocity.magnitude > cap)
            _rb.linearVelocity = _rb.linearVelocity.normalized * cap;
    }

    // -------------------------------------------------------------------------

    /// <summary>Places the jeepney on the route start, facing along it.</summary>
    public void TeleportTo(Vector2 position, float rotationDegrees)
    {
        if (_rb == null) _rb = GetComponent<Rigidbody2D>();

        _rb.position       = position;
        _rb.rotation       = rotationDegrees;
        _rb.linearVelocity  = Vector2.zero;
        _rb.angularVelocity = 0f;

        _steerInput    = 0f;
        _throttleInput = 0f;

        transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, rotationDegrees));
    }
}
