using System;
using UnityEngine;

/// <summary>
/// Persistent singleton that holds the active UI language and notifies listeners
/// when it changes. Created in Bootstrap (DontDestroyOnLoad) alongside the other
/// managers. The Settings Language selector writes
/// <see cref="SettingsManager.Language"/>, which persists and fires
/// <see cref="SettingsManager.OnSettingsChanged"/>; this manager picks that up and
/// raises <see cref="OnLanguageChanged"/> so every visible
/// <see cref="LocalizedLabel"/> re-renders live.
/// </summary>
public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    public event Action OnLanguageChanged;

    GameLanguage _language = GameLanguage.English;
    bool _hooked;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _language = FromSave();
    }

    // SettingsManager may finish its own Awake after us, so try to hook on both
    // OnEnable and Start; the guard makes it idempotent.
    void OnEnable() => Hook();
    void Start()    => Hook();

    void Hook()
    {
        if (_hooked || SettingsManager.Instance == null) return;
        SettingsManager.Instance.OnSettingsChanged += OnSettingsChanged;
        _hooked = true;
        OnSettingsChanged();   // sync in case the saved language differs from our default
    }

    void OnDestroy()
    {
        if (_hooked && SettingsManager.Instance != null)
            SettingsManager.Instance.OnSettingsChanged -= OnSettingsChanged;
    }

    void OnSettingsChanged()
    {
        GameLanguage now = FromSave();
        if (now == _language) return;
        _language = now;
        OnLanguageChanged?.Invoke();
    }

    public GameLanguage Language => _language;

    /// <summary>Localized text for a key in the active language.</summary>
    public string T(string key) => LocalizationTable.Get(key, _language);

    /// <summary>Static convenience that works even before the manager exists
    /// (e.g. at scene-build time), falling back to the saved language.</summary>
    public static string Translate(string key)
        => LocalizationTable.Get(key, Instance != null ? Instance._language : FromSave());

    static GameLanguage FromSave()
        => SaveSystem.Current != null && SaveSystem.Current.settings != null
            ? (GameLanguage)SaveSystem.Current.settings.language
            : GameLanguage.English;
}
