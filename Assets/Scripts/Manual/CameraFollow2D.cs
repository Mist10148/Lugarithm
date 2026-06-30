using UnityEngine;

/// <summary>
/// Smooth camera follow with a small velocity lead so the player sees more
/// road in the direction of travel.
/// </summary>
public class CameraFollow2D : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Feel")]
    [SerializeField] private float smoothTime = 0.32f;
    [SerializeField] private float velocityLead = 0.28f;

    [Tooltip("Logical physics body used for velocity-lead.")]
    [SerializeField] private Rigidbody2D leadBody;

    private Vector3 _velocity;
    private Rigidbody2D _targetBody;

    // -------------------------------------------------------------------------

    void Start()
    {
        if (target != null)
            _targetBody = target.GetComponent<Rigidbody2D>();
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 lead = Vector3.zero;
        if (leadBody != null)
            lead = (Vector3)(leadBody.linearVelocity * velocityLead);
        else if (_targetBody != null)
            lead = (Vector3)(_targetBody.linearVelocity * velocityLead);

        Vector3 goal = target.position + lead;
        goal.z = transform.position.z;

        transform.position = Vector3.SmoothDamp(transform.position, goal, ref _velocity, smoothTime);
    }

    /// <summary>Lets the drive controller retarget (and snap) at spawn time.</summary>
    public void SnapTo(Transform newTarget)
    {
        target = newTarget;
        _targetBody = newTarget != null ? newTarget.GetComponent<Rigidbody2D>() : null;

        if (newTarget != null)
        {
            Vector3 p = newTarget.position;
            transform.position = new Vector3(p.x, p.y, transform.position.z);
        }

        ResetVelocity();   // a snap is a teleport — don't carry chase momentum across it
    }

    /// <summary>Snaps the camera to a world position.</summary>
    public void SnapToWorld(Vector3 worldPos)
    {
        transform.position = new Vector3(worldPos.x, worldPos.y, transform.position.z);
    }

    /// <summary>Zeroes the internally tracked chase velocity. Call this right when the
    /// followed target's motion comes to a hard discrete stop (e.g. Automation's per-step
    /// agent tween finishing) so SmoothDamp doesn't carry momentum built up while chasing
    /// a goal that just decelerated sharply — avoids a brief overshoot/snap-back at every
    /// step boundary. Manual Mode's continuous-physics jeepney never needs this.</summary>
    public void ResetVelocity()
    {
        _velocity = Vector3.zero;
    }
}
