using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// Presentation-only animation for the shared top-down jeepney. It reads movement
/// from the transform and never changes physics, routing, input, colliders, or sim state.
/// </summary>
public sealed class VehicleSpriteAnimator : MonoBehaviour
{
    [SerializeField] SpriteRenderer body;
    [SerializeField] float framesPerSecond = 8f;
    [SerializeField] float movingThreshold = 0.04f;

    Sprite[] _idle = Array.Empty<Sprite>();
    Sprite[] _drive = Array.Empty<Sprite>();
    Sprite[] _accelerate = Array.Empty<Sprite>();
    Sprite[] _brake = Array.Empty<Sprite>();
    Sprite[] _left = Array.Empty<Sprite>();
    Sprite[] _right = Array.Empty<Sprite>();
    Sprite[] _smoke = Array.Empty<Sprite>();
    SpriteRenderer _smokeRenderer;
    Vector3 _lastPosition;
    float _lastAngle;
    float _lastSpeed;
    float _frameClock;
    int _frame;

    void Awake()
    {
        if (body == null) body = GetComponent<SpriteRenderer>();
        Sprite[] all = Resources.LoadAll<Sprite>("Vehicles/player_jeepney_sheet");
        _idle = Named(all, "idle_");
        _drive = Named(all, "drive_");
        _accelerate = Named(all, "accelerate_");
        _brake = Named(all, "brake_");
        _left = Named(all, "turn_left_");
        _right = Named(all, "turn_right_");
        _smoke = Resources.LoadAll<Sprite>("Vehicles/jeepney_smoke_sheet")
                          .OrderBy(s => s.name).ToArray();

        var smokeGo = new GameObject("ExhaustSmoke");
        smokeGo.transform.SetParent(transform, false);
        smokeGo.transform.localPosition = new Vector3(0.34f, -1.02f, 0f);
        _smokeRenderer = smokeGo.AddComponent<SpriteRenderer>();
        _smokeRenderer.sortingOrder = body != null ? body.sortingOrder - 1 : 9;
        _smokeRenderer.enabled = false;
        _lastPosition = transform.position;
        _lastAngle = transform.eulerAngles.z;
        if (body != null && _idle.Length > 0) body.sprite = _idle[0];
    }

    void LateUpdate()
    {
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        float speed = Vector3.Distance(transform.position, _lastPosition) / dt;
        float turn = Mathf.DeltaAngle(_lastAngle, transform.eulerAngles.z) / dt;
        float acceleration = (speed - _lastSpeed) / dt;
        Sprite[] state = Mathf.Abs(turn) > 12f ? (turn > 0f ? _left : _right)
                       : speed <= movingThreshold ? _idle
                       : acceleration < -0.7f ? _brake
                       : acceleration > 0.7f ? _accelerate : _drive;
        if (state.Length == 0) state = _drive.Length > 0 ? _drive : _idle;

        _frameClock += dt * framesPerSecond;
        int next = Mathf.FloorToInt(_frameClock);
        if (next != _frame)
        {
            _frame = next;
            if (body != null && state.Length > 0) body.sprite = state[_frame % state.Length];
        }

        if (_smokeRenderer != null)
        {
            bool show = speed > movingThreshold && _smoke.Length > 0;
            _smokeRenderer.enabled = show;
            if (show) _smokeRenderer.sprite = _smoke[_frame % _smoke.Length];
        }
        _lastPosition = transform.position;
        _lastAngle = transform.eulerAngles.z;
        _lastSpeed = speed;
    }

    static Sprite[] Named(Sprite[] sprites, string prefix)
    {
        return sprites.Where(s => s.name.StartsWith(prefix, StringComparison.Ordinal))
                      .OrderBy(s => s.name).ToArray();
    }
}
