using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trigger area around a passenger stop. Tracks whether the jeepney is inside
/// and owns the townsfolk figures waiting at the sign. Spawned at runtime by
/// <see cref="RouteVisualBuilder"/>.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class StopZone : MonoBehaviour
{
    public int    StopIndex     { get; set; }
    public string StopName      { get; set; }
    public bool   IsDestination { get; set; }

    /// <summary>Raised with (zone, entered) when the jeepney crosses the trigger.</summary>
    public event Action<StopZone, bool> OnJeepneyCrossed;

    public bool JeepneyInside { get; private set; }

    readonly List<GameObject> _waitingPeeps = new List<GameObject>();

    public int WaitingCount => _waitingPeeps.Count;

    // -------------------------------------------------------------------------

    /// <summary>Spawns waiting townsfolk lined up beside the stop sign.</summary>
    public void SpawnWaitingPeeps(int count, Vector2 startLocal, Vector2 stepDirection)
    {
        for (int i = 0; i < count; i++)
            AddPeep(i, PeepColor(StopIndex * 7 + i), startLocal, stepDirection);
    }

    /// <summary>
    /// Spawns waiting townsfolk tinted by the committed rider colors (procedural
    /// town), so a waiting passenger, their ribbon chip, and their dulog marker
    /// all share a color. Peeps are taken last-in-first-out, so the colors are
    /// laid out in order and popped from the end to stay matched.
    /// </summary>
    public void SpawnWaitingPeeps(IReadOnlyList<Color> colors, Vector2 startLocal, Vector2 stepDirection)
    {
        for (int i = 0; i < colors.Count; i++)
            AddPeep(i, colors[i], startLocal, stepDirection);
    }

    /// <summary>
    /// One waiting person: a real top-down townsperson figure (reused from the
    /// town NPC art) standing on a small color-coded ground dot. The root keeps a
    /// tinted SpriteRenderer so the dot both marks the passenger's color (matching
    /// their ribbon chip / dulog marker) and remains the tint that
    /// <see cref="PassengerManager"/> reads when they board.
    /// </summary>
    void AddPeep(int i, Color tint, Vector2 startLocal, Vector2 stepDirection)
    {
        var peep = new GameObject($"Peep_{i}");
        peep.transform.SetParent(transform, false);
        peep.transform.localPosition = (Vector3)(startLocal + stepDirection * (0.75f * i));

        // Ground dot: carries the color code and the boarding tint.
        var sr = peep.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("Placeholders/circle");
        sr.sortingOrder = 4;
        sr.color = tint;

        // Reused town NPC figure standing on the dot, facing the camera.
        TownNpcVisuals.BuildIdleFigure(peep.transform, StopIndex * 7 + i, sortingOrder: 6);

        _waitingPeeps.Add(peep);
    }

    /// <summary>Removes and returns one waiting peep (boards the jeepney).</summary>
    public GameObject TakeWaitingPeep()
    {
        if (_waitingPeeps.Count == 0) return null;

        GameObject peep = _waitingPeeps[_waitingPeeps.Count - 1];
        _waitingPeeps.RemoveAt(_waitingPeeps.Count - 1);
        return peep;
    }

    public void ClearWaitingPeeps()
    {
        foreach (GameObject peep in _waitingPeeps)
        {
            if (peep == null) continue;
            // Destroy logs an error under EditMode tests (nothing is playing); fall back to
            // DestroyImmediate there so cleanup stays test-safe.
            if (Application.isPlaying) Destroy(peep);
            else DestroyImmediate(peep);
        }
        _waitingPeeps.Clear();
    }

    /// <summary>Deterministic placeholder tint per peep.</summary>
    public static Color PeepColor(int seed)
    {
        float hue = Mathf.Repeat(seed * 0.173f, 1f);
        return Color.HSVToRGB(hue, 0.55f, 0.95f);
    }

    // -------------------------------------------------------------------------

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<JeepneyController>() == null) return;
        JeepneyInside = true;
        OnJeepneyCrossed?.Invoke(this, true);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<JeepneyController>() == null) return;
        JeepneyInside = false;
        OnJeepneyCrossed?.Invoke(this, false);
    }
}
