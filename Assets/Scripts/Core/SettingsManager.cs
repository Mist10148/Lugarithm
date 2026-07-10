using System;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Central access point for player settings. Backed by
/// <c>SaveSystem.Current.settings</c> (single source of truth), applies changes
/// at runtime, and raises <see cref="OnSettingsChanged"/> so any open screen can
/// refresh. Replaces the scattered PlayerPrefs that the menu used before.
/// </summary>
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Audio (optional)")]
    [Tooltip("If assigned, volumes drive these exposed AudioMixer params. " +
             "Otherwise music volume falls back to AudioListener.volume.")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string musicVolumeParam = "MusicVolume";
    [SerializeField] private string sfxVolumeParam   = "SFXVolume";

    /// <summary>Raised whenever any setting changes (or after the initial apply).</summary>
    public event Action OnSettingsChanged;

    // Shorthand for the settings block inside the loaded save.
    private static GameSettings S => SaveSystem.Current.settings;

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
    }

    void Start()
    {
        // GameManager.Awake loads the save, and Awake runs before any Start,
        // so Current.settings is populated by the time we apply here.
        ApplyAll();
    }

    // -------------------------------------------------------------------------
    // Typed properties (get from save, set persists + applies)

    public bool ManualMode
    {
        get => S.manualMode;
        set { S.manualMode = value; Persist(); }
    }

    public bool BlockMode
    {
        get => S.blockMode;
        set { S.blockMode = value; Persist(); }
    }

    public float MusicVolume
    {
        get => S.musicVolume;
        set { S.musicVolume = Mathf.Clamp01(value); ApplyAudio(); Persist(); }
    }

    public float SfxVolume
    {
        get => S.sfxVolume;
        set { S.sfxVolume = Mathf.Clamp01(value); ApplyAudio(); Persist(); }
    }

    public DialogueSpeed DialogueSpeed
    {
        get => (DialogueSpeed)S.dialogueSpeed;
        set { S.dialogueSpeed = (int)value; Persist(); }
    }

    public bool Subtitles
    {
        get => S.subtitles;
        set { S.subtitles = value; Persist(); }
    }

    public BrakeMode BrakeMode
    {
        get => (BrakeMode)S.brakeMode;
        set { S.brakeMode = (int)value; Persist(); }
    }

    public GameLanguage Language
    {
        get => (GameLanguage)S.language;
        set
        {
            if (S.language == (int)value) return;
            S.language = (int)value;
            Persist();   // LocalizationManager listens on OnSettingsChanged and re-renders.
        }
    }

    public int CodeThemeId
    {
        get => S.codeThemeId;
        set
        {
            if (!CodeThemeLibrary.Exists(value)) return;
            S.codeThemeId = value;
            Persist();
        }
    }

    /// <summary>
    /// Attempts to purchase a theme. Returns true when already unlocked or
    /// successfully bought; false if the player can't afford it.
    /// </summary>
    public bool TryBuyTheme(int themeId)
    {
        if (SaveSystem.Current.HasTheme(themeId)) return true;

        CodeTheme theme = CodeThemeLibrary.Get(themeId);
        if (SaveSystem.Current.currency < theme.cost) return false;

        SaveSystem.Current.currency -= theme.cost;
        SaveSystem.Current.UnlockTheme(themeId);
        SaveSystem.Save();
        return true;
    }

    // -------------------------------------------------------------------------
    // Apply / persist

    /// <summary>Re-applies every setting to the live game (call after load).</summary>
    public void ApplyAll()
    {
        ApplyAudio();
        OnSettingsChanged?.Invoke();
    }

    private void ApplyAudio()
    {
        if (audioMixer != null)
        {
            audioMixer.SetFloat(musicVolumeParam, LinearToDb(S.musicVolume));
            audioMixer.SetFloat(sfxVolumeParam,   LinearToDb(S.sfxVolume));
        }
        else if (AudioManager.Instance != null)
        {
            // Route music through the dedicated BGM source so it acts independently
            // of SFX (unlike the global AudioListener stopgap below).
            AudioManager.Instance.SetMusicVolume(S.musicVolume);
        }
        else
        {
            // Last-ditch fallback until an AudioMixer or AudioManager exists.
            AudioListener.volume = S.musicVolume;
        }
    }

    private void Persist()
    {
        SaveSystem.Save();
        OnSettingsChanged?.Invoke();
    }

    /// <summary>Converts a 0..1 linear volume to decibels for an AudioMixer.</summary>
    private static float LinearToDb(float linear)
    {
        return linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;
    }
}
