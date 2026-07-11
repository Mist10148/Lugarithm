using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A row of mutually-exclusive "pill" buttons — a clearer alternative to a toggle
/// for either/or (and 3–4 way) settings, because every option's label stays
/// visible and the active one is highlighted. Built by
/// <c>UIFactory.CreateSegmentedSelector</c>; the segment buttons are this object's
/// direct children, discovered in creation order so the button index is the option
/// index. The hosting screen reads <see cref="Value"/> and listens on
/// <see cref="OnValueChanged"/>.
/// </summary>
public class SegmentedSelector : MonoBehaviour
{
    static readonly Color Active = new Color(0.30f, 0.45f, 0.75f, 1f);
    static readonly Color Idle   = new Color(0.18f, 0.22f, 0.30f, 1f);

    Button[] _segments;
    bool _wired;
    [SerializeField] Sprite activeSprite;
    [SerializeField] Sprite idleSprite;

    /// <summary>Index of the selected option.</summary>
    public int Value { get; private set; }

    /// <summary>Fires only on user clicks (not on <see cref="SetValueWithoutNotify"/>).</summary>
    public event Action<int> OnValueChanged;

    public int Count { get { EnsureWired(); return _segments != null ? _segments.Length : 0; } }

    void Awake() => EnsureWired();

    void EnsureWired()
    {
        if (_wired) return;
        _wired = true;

        // Direct children are the segment buttons, in option order. (The container
        // has no Button of its own, and each button's label is a TMP child, not a
        // Button, so the traversal order matches the order they were created.)
        _segments = GetComponentsInChildren<Button>(includeInactive: true);
        for (int i = 0; i < _segments.Length; i++)
        {
            int index = i;
            _segments[i].onClick.AddListener(() => Select(index, notify: true));
        }
        Paint();
    }

    /// <summary>Sets the active segment for display without firing the event
    /// (used when syncing the UI from saved settings).</summary>
    public void SetValueWithoutNotify(int index)
    {
        EnsureWired();
        Value = _segments.Length > 0 ? Mathf.Clamp(index, 0, _segments.Length - 1) : 0;
        Paint();
    }

    void Select(int index, bool notify)
    {
        Value = index;
        Paint();
        if (notify) OnValueChanged?.Invoke(index);
    }

    void Paint()
    {
        if (_segments == null) return;
        for (int i = 0; i < _segments.Length; i++)
            if (_segments[i] != null && _segments[i].image != null)
            {
                if (activeSprite != null && idleSprite != null)
                {
                    _segments[i].image.sprite = i == Value ? activeSprite : idleSprite;
                    _segments[i].image.color = Color.white;
                }
                else
                    _segments[i].image.color = i == Value ? Active : Idle;
            }
    }
}
