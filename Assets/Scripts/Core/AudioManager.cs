using UnityEngine;

/// <summary>
/// Persistent owner of the background-music playback. Holds a looping
/// <see cref="AudioSource"/> (the clip is wired in by the Bootstrap scene
/// builder) and exposes its volume so the player's Music Volume setting can
/// drive it live. <see cref="SettingsManager.ApplyAudio"/> pushes the current
/// value here on startup and on every slider change. Persists across scenes so
/// the loop is seamless; place one in the first-loaded (Bootstrap) scene.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Tooltip("Looping background-music source. Clip is assigned by the scene builder.")]
    [SerializeField] private AudioSource musicSource;

    // -------------------------------------------------------------------------

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicSource == null) musicSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        // SettingsManager.Awake runs before any Start, so Instance is available;
        // pull the persisted volume, then begin the loop.
        if (SettingsManager.Instance != null)
            SetMusicVolume(SettingsManager.Instance.MusicVolume);
        else if (SaveSystem.Current != null)
            SetMusicVolume(SaveSystem.Current.settings.musicVolume);

        if (musicSource != null && musicSource.clip != null && !musicSource.isPlaying)
            musicSource.Play();
    }

    // -------------------------------------------------------------------------

    /// <summary>Sets the background-music volume (0..1). Called by SettingsManager.</summary>
    public void SetMusicVolume(float v)
    {
        if (musicSource != null) musicSource.volume = Mathf.Clamp01(v);
    }
}
