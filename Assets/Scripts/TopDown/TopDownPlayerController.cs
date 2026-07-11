using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Top-down player movement for the overworld levels, tuned to feel like the
/// classic GBA/DS Pokémon games: crisp 4-directional walking with no diagonal
/// sliding. Movement starts and stops near-instantly (no floaty acceleration),
/// and when two directions are held the most-recently-pressed axis wins, so the
/// player snaps cleanly between cardinals instead of drifting on a diagonal.
///
/// Uses <see cref="Rigidbody2D"/> for physics-based collision against
/// TilemapCollider2D walls, matching the project's existing top-down pattern.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class TopDownPlayerController : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Tuning
    // -------------------------------------------------------------------------

    [Header("Movement")]
    [Tooltip("Walk speed in world units (tiles) per second. Constant — no ramp.")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("References")]
    [SerializeField] private SpriteRenderer bodySprite;
    private Animator _bodyAnimator;

    private static readonly int IdleHash = Animator.StringToHash("Idle");
    private static readonly int HorizontalHash = Animator.StringToHash("HorizontalDirection");
    private static readonly int VerticalHash = Animator.StringToHash("VerticallDirection");

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    // One entry per cardinal in CardinalDirs order (Up, Down, Left, Right).
    // Tracks the moment each direction key transitioned to "pressed" so the
    // most-recently-pressed held direction can win when several are down.
    private static readonly Vector2[] CardinalDirs =
    {
        Vector2.up, Vector2.down, Vector2.left, Vector2.right
    };
    private readonly float[] _pressTime = new float[4];
    private readonly bool[]  _wasDown   = new bool[4];

    private Vector2 _moveDir;            // resolved cardinal move direction (or zero)
    private Rigidbody2D _rb;
    private bool _inputLocked;

    /// <summary>
    /// When true, all player input is ignored (dialogue, cutscenes, menus).
    /// Physics still runs so the body doesn't freeze mid-air.
    /// </summary>
    public bool InputLocked
    {
        get => _inputLocked;
        set
        {
            _inputLocked = value;
            if (value)
            {
                _moveDir = Vector2.zero;
                for (int i = 0; i < _wasDown.Length; i++) _wasDown[i] = false;
            }
        }
    }

    /// <summary>True while the player is actively walking.</summary>
    public bool IsMoving => _moveDir.sqrMagnitude > 0.01f;

    /// <summary>Current world-space facing direction (unit cardinal vector).</summary>
    public Vector2 FacingDirection { get; private set; } = Vector2.down;

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

    void Update()
    {
        // The universal Settings overlay is a modal: while it's up, gameplay
        // input must not continue (keyboard walk isn't stopped by UI raycasts).
        bool modalOpen = UniversalSettingsManager.IsAnyOpen;

        if (!_inputLocked && !modalOpen)
            ReadInput();
        else if (modalOpen)
            _moveDir = Vector2.zero;

        // Snap the body to face the cardinal we're moving toward, instantly —
        // never rotate smoothly through in-between angles.
        if (_moveDir.sqrMagnitude > 0.01f)
            FacingDirection = _moveDir;

        UpdateBodyVisual();
    }

    void FixedUpdate()
    {
        // Constant-speed step: instant start, instant stop (no glide).
        Vector2 newPos = _rb.position + _moveDir * (moveSpeed * Time.fixedDeltaTime);
        _rb.MovePosition(newPos);
    }

    // -------------------------------------------------------------------------
    // Input
    // -------------------------------------------------------------------------

    void ReadInput()
    {
        var kb = Keyboard.current;
        if (kb == null) { _moveDir = Vector2.zero; return; }

        // Held state per cardinal (Up, Down, Left, Right).
        bool up    = kb.upArrowKey.isPressed    || kb.wKey.isPressed;
        bool down  = kb.downArrowKey.isPressed  || kb.sKey.isPressed;
        bool left  = kb.leftArrowKey.isPressed  || kb.aKey.isPressed;
        bool right = kb.rightArrowKey.isPressed || kb.dKey.isPressed;
        bool[] held = { up, down, left, right };

        // Record the timestamp when each key first goes down so the most recent
        // press can take priority while it stays held.
        for (int i = 0; i < held.Length; i++)
        {
            if (held[i] && !_wasDown[i]) _pressTime[i] = Time.time;
            _wasDown[i] = held[i];
        }

        // Pick the held cardinal with the latest press time — last input wins.
        int best = -1;
        float bestTime = float.NegativeInfinity;
        for (int i = 0; i < held.Length; i++)
        {
            if (held[i] && _pressTime[i] >= bestTime)
            {
                bestTime = _pressTime[i];
                best = i;
            }
        }

        _moveDir = best >= 0 ? CardinalDirs[best] : Vector2.zero;
    }

    // -------------------------------------------------------------------------
    // Visual facing
    // -------------------------------------------------------------------------

    public void SetVisualController(RuntimeAnimatorController controller)
    {
        if (bodySprite == null || controller == null) return;

        bodySprite.transform.localEulerAngles = Vector3.zero;
        _bodyAnimator = bodySprite.GetComponent<Animator>();
        if (_bodyAnimator == null)
            _bodyAnimator = bodySprite.gameObject.AddComponent<Animator>();

        _bodyAnimator.runtimeAnimatorController = controller;
        UpdateBodyVisual();
    }

    void UpdateBodyVisual()
    {
        if (_bodyAnimator != null)
        {
            bodySprite.transform.localEulerAngles = Vector3.zero;
            _bodyAnimator.SetBool(IdleHash, !IsMoving);
            _bodyAnimator.SetFloat(HorizontalHash, FacingDirection.x);
            _bodyAnimator.SetFloat(VerticalHash, FacingDirection.y);
            return;
        }

        if (IsMoving)
            UpdateSpriteFacing(_moveDir);
    }

    void UpdateSpriteFacing(Vector2 dir)
    {
        // Snap the body sprite to the cardinal it's heading toward. With a
        // single placeholder sprite we rotate it; once 4-direction sprites exist
        // this is the hook to swap them instead.
        // In top-down: up = -90°, right = 0°, down = 90°, left = 180°.
        if (bodySprite == null) return;

        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        bodySprite.transform.localEulerAngles = new Vector3(0f, 0f, targetAngle);
    }
}
