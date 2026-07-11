using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Binds a scene's Settings button to the single universal Settings overlay
/// (<see cref="UniversalSettingsManager"/>). Every entry point uses this same
/// component so there is one authoritative panel; the manager is created from
/// its Resources prefab on demand when a scene is played directly in the editor
/// without Bootstrap.
/// </summary>
public class SettingsPanelOpenButton : MonoBehaviour
{
    [SerializeField] private Button button;

    void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(Open);
    }

    void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(Open);
    }

    void Open()
    {
        UniversalSettingsManager.Ensure()?.Toggle();
    }
}
