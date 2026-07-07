using UnityEngine;

/// <summary>
/// Attaches to a main quest station marker to make it pulse gently (scale
/// oscillation + soft alpha breathing), so the main quest stands out from
/// the regular side-objective markers at a glance.
/// </summary>
public class MainQuestPulse : MonoBehaviour
{
    SpriteRenderer _sr;
    Color _baseColor;
    Vector3 _baseScale;

    float _pulseSpeed = 1.8f;
    float _scaleAmp   = 0.08f;
    float _alphaMin   = 0.75f;

    public void Init(SpriteRenderer sr)
    {
        _sr = sr;
        _baseColor = sr.color;
        _baseScale = sr.transform.localScale;
    }

    void Update()
    {
        if (_sr == null) return;

        float t = Mathf.Sin(Time.time * _pulseSpeed) * 0.5f + 0.5f; // 0..1

        // Gentle scale oscillation
        float scale = 1f + t * _scaleAmp;
        _sr.transform.localScale = _baseScale * scale;

        // Soft alpha breathing
        float alpha = Mathf.Lerp(_alphaMin, 1f, t);
        _sr.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, alpha);
    }
}
