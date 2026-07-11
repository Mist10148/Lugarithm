using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Persistent, Bootstrap-owned controller for the single universal Settings
/// overlay. Mirrors the <see cref="SettingsManager"/> singleton pattern (one
/// instance, <c>DontDestroyOnLoad</c>, duplicates self-destruct). The overlay
/// itself is a prefab at <c>Resources/UI/SettingsOverlay</c>, so the
/// Bootstrap-owned instance and the direct-scene fallback both come from one
/// source and can never diverge.
///
/// Every scene's Settings button routes here via <see cref="Ensure"/>, so there
/// is exactly one Settings panel across the whole game. This controller owns
/// only presentation/lifecycle; the authoritative settings state still lives in
/// <see cref="SettingsManager"/> / SaveSystem via the wrapped <see cref="SettingsPanel"/>.
/// </summary>
public class UniversalSettingsManager : MonoBehaviour
{
    public static UniversalSettingsManager Instance { get; private set; }

    const string PrefabResourcePath = "UI/SettingsOverlay";

    [SerializeField] private SettingsPanel panel;

    private GameObject _restoreFocus;
    private bool _wasOpen;

    /// <summary>True while the overlay is showing.</summary>
    public bool IsOpen => panel != null && panel.IsOpen;

    /// <summary>Convenience for gating gameplay input from anywhere while the modal is up.</summary>
    public static bool IsAnyOpen => Instance != null && Instance.IsOpen;

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

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Returns the live manager, creating it from the Resources prefab when no
    /// Bootstrap-owned instance exists yet (e.g. a scene played directly in the
    /// editor). Never throws; returns null only if the prefab is missing, so
    /// callers can safely no-op with <c>Ensure()?.Open()</c>.
    /// </summary>
    public static UniversalSettingsManager Ensure()
    {
        if (Instance != null) return Instance;

        var prefab = Resources.Load<GameObject>(PrefabResourcePath);
        if (prefab == null)
        {
            Debug.LogWarning($"[Lugarithm] Settings overlay prefab missing at Resources/{PrefabResourcePath}.");
            return null;
        }

        // Instantiating runs Awake, which assigns Instance and marks it persistent.
        var go = Instantiate(prefab);
        go.name = prefab.name;
        return Instance;
    }

    // -------------------------------------------------------------------------
    // Public API

    public void Open()
    {
        if (panel == null) return;
        CaptureFocus();
        panel.Open();
        SelectFirstControl();
        _wasOpen = true;
    }

    public void Close()
    {
        if (panel == null) return;
        panel.Close();
        RestoreFocus();
        _wasOpen = false;
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    // -------------------------------------------------------------------------

    void Update()
    {
        bool open = IsOpen;

        // Escape closes the overlay (the project runs the Input System only, so
        // legacy Input.GetKeyDown would throw — read the device directly).
        if (open)
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                Close();
                return;
            }
        }

        // Catch a close driven by the panel's own X/Close button (which calls
        // SettingsPanel.Close directly) so focus is still restored.
        if (_wasOpen && !open)
        {
            RestoreFocus();
            _wasOpen = false;
        }
        else if (!_wasOpen && open)
        {
            _wasOpen = true;
        }
    }

    // -------------------------------------------------------------------------
    // Focus / keyboard navigation

    void CaptureFocus()
    {
        EventSystem es = EventSystem.current;
        _restoreFocus = es != null ? es.currentSelectedGameObject : null;
    }

    void SelectFirstControl()
    {
        EventSystem es = EventSystem.current;
        if (es == null || panel == null) return;

        Selectable first = panel.GetComponentInChildren<Selectable>(includeInactive: false);
        es.SetSelectedGameObject(first != null ? first.gameObject : null);
    }

    void RestoreFocus()
    {
        EventSystem es = EventSystem.current;
        if (es == null) return;

        bool valid = _restoreFocus != null && _restoreFocus.activeInHierarchy;
        es.SetSelectedGameObject(valid ? _restoreFocus : null);
        _restoreFocus = null;
    }
}
