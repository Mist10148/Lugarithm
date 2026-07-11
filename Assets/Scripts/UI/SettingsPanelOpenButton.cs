using UnityEngine;
using UnityEngine.UI;

/// <summary>Binds an in-game HUD button to the shared settings overlay.</summary>
public class SettingsPanelOpenButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private SettingsPanel settingsPanel;

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
        if (settingsPanel != null) settingsPanel.Open();
    }
}
