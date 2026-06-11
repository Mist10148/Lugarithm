using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Real-time jeepney driving for Manual Mode: WASD / arrows via the Input
/// System, with momentum retention and low lateral grip so the jeepney
/// drifts on the "slippery" coastal road (PRD §5.2).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class JeepneyController : MonoBehaviour
{
    [Header("Driving")]
    [SerializeField] private float accelForce    = 26f;
    [SerializeField] private float reverseForce  = 14f;
    [SerializeField] private float maxSpeed      = 9f;
    [SerializeField] private float steerDegPerSec = 170f;

    [Header("Drift")]
    [Tooltip("How much forward momentum survives each physics tick (≈1 = icy).")]
    [SerializeField] private float forwardKeep = 0.995f;
    [Tooltip("How much sideways velocity survives each tick (lower = grippier).")]
    [SerializeField] private float lateralGrip = 0.88f;

    [Header("Off-road")]
    [SerializeField] private float offRoadSpeedFactor = 0.45f;

    [Header("Fuel")]
    [SerializeField] private float fuelDrainPerSecond = 0.004f;

    private Rigidbody2D _rb;
    private float _fuel = 1f;

    // -------------------------------------------------------------------------
    // Public state

    /// <summary>Blocks player input (breakdowns, results) without freezing physics.</summary>
    public bool InputLocked { get; set; }

    /// <summary>True while the jeepney is off the road (set by the drive controller).</summary>
    public bool OffRoad { get; set; }

    public float CurrentSpeed   => _rb != null ? _rb.linearVelocity.magnitude : 0f;
    public float CurrentSpeed01 => maxSpeed > 0f ? Mathf.Clamp01(CurrentSpeed / maxSpeed) : 0f;
    public float Fuel01         => _fuel;

    // -------------------------------------------------------------------------

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        float throttle = 0f;
        float steer    = 0f;

        if (!InputLocked && Keyboard.current != null)
        {
            var kb = Keyboard.current;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    throttle += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  throttle -= 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  steer    += 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) steer    -= 1f;
        }

        // Fuel only burns while throttling; an empty tank limps along.
        if (throttle != 0f)
            _fuel = Mathf.Max(0f, _fuel - fuelDrainPerSecond * Time.fixedDeltaTime *
                                  (Mathf.Abs(throttle)));
        float fuelFactor = _fuel > 0f ? 1f : 0.5f;

        // Throttle along the jeepney's nose.
        if (throttle > 0f)
            _rb.AddForce(transform.up * (accelForce * throttle * fuelFactor));
        else if (throttle < 0f)
            _rb.AddForce(transform.up * (reverseForce * throttle * fuelFactor));

        // Steering scales with speed so the jeepney can't pivot in place,
        // and flips when reversing (like a real vehicle).
        float forwardSpeed = Vector2.Dot(_rb.linearVelocity, transform.up);
        float steerFactor  = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / 2.5f) * Mathf.Sign(forwardSpeed);
        _rb.MoveRotation(_rb.rotation + steer * steerDegPerSec * steerFactor * Time.fixedDeltaTime);

        // Drift core: keep most forward momentum, bleed sideways slip.
        Vector2 forward = (Vector2)transform.up * forwardSpeed;
        Vector2 lateral = _rb.linearVelocity - forward;
        _rb.linearVelocity = forward * forwardKeep + lateral * lateralGrip;

        // Speed cap (tighter off-road or out of fuel).
        float cap = maxSpeed * (OffRoad ? offRoadSpeedFactor : 1f) * fuelFactor;
        if (_rb.linearVelocity.magnitude > cap)
            _rb.linearVelocity = _rb.linearVelocity.normalized * cap;
    }

    // -------------------------------------------------------------------------

    /// <summary>Places the jeepney on the route start, facing along it.</summary>
    public void TeleportTo(Vector2 position, float rotationDegrees)
    {
        if (_rb == null) _rb = GetComponent<Rigidbody2D>();
        _rb.position = position;
        _rb.rotation = rotationDegrees;
        _rb.linearVelocity  = Vector2.zero;
        _rb.angularVelocity = 0f;
        transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, rotationDegrees));
    }
}
