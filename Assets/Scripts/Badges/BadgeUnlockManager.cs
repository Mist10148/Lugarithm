using System;
using UnityEngine;

/// <summary>
/// Persistent singleton that drives the badge unlock overlay. Created in
/// Bootstrap via <see cref="BadgeUnlockBuilder"/>.
/// </summary>
public class BadgeUnlockManager : MonoBehaviour
{
    public static BadgeUnlockManager Instance { get; private set; }

    [SerializeField] private BadgeUnlockPanel panel;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Show the badge reveal panel. onDone fires when the player dismisses it.
    /// If levelIndex has no badge definition, calls onDone immediately.
    /// </summary>
    public void Show(int levelIndex, Action onDone)
    {
        var badge = BadgeLibrary.Get(levelIndex);
        if (badge == null || panel == null) { onDone?.Invoke(); return; }
        panel.Show(badge, onDone);
    }
}
