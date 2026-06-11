using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trigger area around a passenger stop. Tracks whether the jeepney is inside
/// and owns the placeholder peeps waiting at the sign. Spawned at runtime by
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

    /// <summary>
    /// Spawns placeholder peeps lined up beside the stop sign. The offsets are
    /// computed on the flat logical plane (relative to this zone), then iso-
    /// projected; peeps are top-level so the rotated zone doesn't tilt them.
    /// </summary>
    public void SpawnWaitingPeeps(int count, Vector2 startLocal, Vector2 stepDirection)
    {
        Sprite peepSprite = Resources.Load<Sprite>("Placeholders/peep");

        for (int i = 0; i < count; i++)
        {
            Vector2 local        = startLocal + stepDirection * (0.75f * i);
            Vector2 logicalWorld = transform.TransformPoint(local);

            var peep = new GameObject($"Peep_{i}");
            peep.transform.position = IsoProjection.Project(logicalWorld);

            var sr = peep.AddComponent<SpriteRenderer>();
            sr.sprite = peepSprite;
            sr.sortingOrder = IsoProjection.SortOrder(logicalWorld) + 1;
            sr.color = PeepColor(StopIndex * 7 + i);

            _waitingPeeps.Add(peep);
        }
    }

    /// <summary>Removes and returns one waiting peep (boards the jeepney).</summary>
    public GameObject TakeWaitingPeep()
    {
        if (_waitingPeeps.Count == 0) return null;

        GameObject peep = _waitingPeeps[_waitingPeeps.Count - 1];
        _waitingPeeps.RemoveAt(_waitingPeeps.Count - 1);
        return peep;
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
