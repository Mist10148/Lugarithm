using UnityEngine;

/// <summary>
/// Gentle vertical bob for the always-visible "!" beacon floating above an
/// unsolved minigame station, so open objectives are spottable from across the
/// town hub. Mirrors the <see cref="MainQuestPulse"/> pattern: Init captures the
/// rest pose, Update oscillates around it.
/// </summary>
public class StationBeacon : MonoBehaviour
{
    SpriteRenderer _sr;
    Vector3 _basePosition;

    const float BobSpeed  = 2f;
    const float BobHeight = 0.08f;

    public void Init(SpriteRenderer sr)
    {
        _sr = sr;
        _basePosition = transform.localPosition;
    }

    void Update()
    {
        if (_sr == null) return;
        transform.localPosition =
            _basePosition + Vector3.up * (Mathf.Sin(Time.time * BobSpeed) * BobHeight);
    }
}
