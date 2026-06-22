using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Top-down player movement for the overworld levels: 4-directional WASD /
/// Arrow-key walk with acceleration and smooth deceleration. Uses
/// <see cref="Rigidbody2D"/> for physics-based collision against TilemapCollider2D
/// walls, matching the project's existing top-down physics pattern.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class TopDownPlayerController : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Tuning
    // -------------------------------------------------------------------------

    [Header("Movement")]
    [Tooltip("Maximum walk speed in world units per second.")]
    [SerializeField] private float maxSpeed = 5f;

    [Tooltip("How quickly the player reaches max speed (higher = snappier).")]
    [SerializeField] private float accelTime = 0.15f;

    [Tooltip("How quickly the player stops when no input is held.")]
    [SerializeField] private float decelTime = 0.1f;

    [Header("Facing")]
    [Tooltip("Degrees per second the sprite rotates toward the movement direction.")]
    [SerializeField] private float turnSpeed = 720f;

    [Header("References")]
    [SerializeField] private SpriteRenderer bodySprite;

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private Vector2 _inputDir;         // normalised input this frame
    private Vector2 _velocity;          // current smoothed velocity
    private float _speedSmoothVel;      // for SmoothDamp magnitude tracking
    private Rigidbody2D _rb;
    private bool _inputLocked;

    /// <summary>
    /// When true, all player input is ignored (dialogue, cutscenes, menus).
    /// Physics still runs so the body doesn't freeze mid-air.
    /// </summary>
    public bool InputLocked
    {
        get => _inputLocked;
        set => _inputLocked = value;
    }

    /// <summary>True while the player is actively providing movement input.</summary>
    public bool IsMoving => _inputDir.sqrMagnitude > 0.01f;

    /// <summary>Current world-space facing direction (unit vector).</summary>
    public Vector2 FacingDirection { get; private set; } = Vector2.up;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        // Top-down physics: no gravity, no rotation from physics, interpolation.
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;
        _rb.linearDamping = 0f;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void FixedUpdate()
    {
        if (_inputLocked)
        {
            _inputDir = Vector2.zero;
        }
        else
        {
            ReadInput();
        }

        float targetMag = _inputDir.sqrMagnitude > 0.01f ? maxSpeed : 0f;
        float smoothT = targetMag > _velocity.magnitude ? accelTime : decelTime;
        float mag = Mathf.SmoothDamp(_velocity.magnitude, targetMag, ref _speedSmoothVel, smoothT);

        if (mag < 0.01f)
        {
            _velocity = Vector2.zero;
            _rb.MovePosition(_rb.position);
            return;
        }

        Vector2 dir = _inputDir.sqrMagnitude > 0.01f ? _inputDir : _velocity.normalized;
        _velocity = dir * mag;

        // Clamp diagonal speed to maxSpeed
        if (_velocity.sqrMagnitude > maxSpeed * maxSpeed)
            _velocity = _velocity.normalized * maxSpeed;

        Vector2 newPos = _rb.position + _velocity * Time.fixedDeltaTime;
        _rb.MovePosition(newPos);
    }

    void Update()
    {
        // Update facing direction toward movement direction
        if (_velocity.sqrMagnitude > 0.1f)
        {
            Vector2 targetFacing = _velocity.normalized;
            FacingDirection = targetFacing;
            UpdateSpriteFacing(targetFacing);
        }
    }

    // -------------------------------------------------------------------------
    // Input
    // -------------------------------------------------------------------------

    void ReadInput()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        float x = 0f, y = 0f;

        if (kb.leftArrowKey.isPressed  || kb.aKey.isPressed) x -= 1f;
        if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) x += 1f;
        if (kb.downArrowKey.isPressed  || kb.sKey.isPressed) y -= 1f;
        if (kb.upArrowKey.isPressed    || kb.wKey.isPressed) y += 1f;

        // Normalise diagonal so it's not √2 faster
        if (x != 0f || y != 0f)
        {
            float len = Mathf.Sqrt(x * x + y * y);
            _inputDir = new Vector2(x / len, y / len);
        }
        else
        {
            _inputDir = Vector2.zero;
        }
    }

    // -------------------------------------------------------------------------
    // Visual facing
    // -------------------------------------------------------------------------

    void UpdateSpriteFacing(Vector2 dir)
    {
        // Rotate the body sprite to face the movement direction.
        // In top-down, "up" = -90°, "right" = 0°, "down" = 90°, "left" = 180°.
        if (bodySprite == null) return;

        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        float currentAngle = bodySprite.transform.localEulerAngles.z;
        float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, turnSpeed * Time.deltaTime);
        bodySprite.transform.localEulerAngles = new Vector3(0f, 0f, newAngle);
    }
}
