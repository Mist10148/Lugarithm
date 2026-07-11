using UnityEngine;

/// <summary>
/// Loops an audible artifact cue whose volume rises as the player approaches.
/// Uses a 2D source so distance only controls loudness rather than Unity's 3D
/// rolloff, which keeps the tracker predictable in the top-down map.
/// </summary>
public class ArtifactProximityAudio : MonoBehaviour
{
    private Transform _listener;
    private AudioSource _source;
    private float _nearDistance;
    private float _farDistance;
    private float _maxVolume;

    public void Configure(
        Transform listener,
        AudioClip clip,
        float nearDistance = 1.5f,
        float farDistance = 24f,
        float maxVolume = 1f)
    {
        _listener = listener;
        _nearDistance = Mathf.Max(0f, nearDistance);
        _farDistance = Mathf.Max(_nearDistance + 0.01f, farDistance);
        _maxVolume = Mathf.Clamp01(maxVolume);

        _source = GetComponent<AudioSource>();
        if (_source == null) _source = gameObject.AddComponent<AudioSource>();
        _source.clip = clip;
        _source.loop = true;
        _source.playOnAwake = false;
        _source.spatialBlend = 0f;
        _source.volume = 0f;

        if (clip != null) _source.Play();
    }

    void Update()
    {
        if (_source == null || _listener == null) return;

        float distance = Vector2.Distance(transform.position, _listener.position);
        float sfxVolume = SettingsManager.Instance != null
            ? SettingsManager.Instance.SfxVolume
            : SaveSystem.Current != null
                ? SaveSystem.Current.settings.sfxVolume
                : 1f;
        _source.volume = EvaluateVolume(
            distance, _nearDistance, _farDistance, _maxVolume * sfxVolume);
    }

    public static float EvaluateVolume(
        float distance,
        float nearDistance,
        float farDistance,
        float maxVolume = 1f)
    {
        if (farDistance <= nearDistance) return distance <= nearDistance
            ? Mathf.Clamp01(maxVolume)
            : 0f;

        float proximity = 1f - Mathf.InverseLerp(nearDistance, farDistance, distance);
        proximity = proximity * proximity * (3f - (2f * proximity));
        return Mathf.Clamp01(maxVolume) * proximity;
    }
}
