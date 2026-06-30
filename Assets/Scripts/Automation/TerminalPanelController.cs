using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Collapsible terminal docked to the bottom of the code window. Hosts the
/// execution <see cref="ConsoleController"/> and reserves space for itself by
/// shrinking the editor body when open.
/// </summary>
public class TerminalPanelController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private RectTransform editorBody;
    [SerializeField] private ConsoleController console;
    [SerializeField] private Button toggleButton;
    [SerializeField] private Button closeButton;

    [Header("Layout")]
    [SerializeField] private float terminalHeight = 190f;
    [SerializeField] private float minTerminalHeight = 130f;
    [SerializeField] private float maxTerminalHeight = 360f;

    public bool IsOpen { get; private set; }
    public ConsoleController Console => console;
    public float TerminalHeight => terminalHeight;

    void Awake()
    {
        if (toggleButton != null) toggleButton.onClick.AddListener(Toggle);
        if (closeButton  != null) closeButton.onClick.AddListener(Close);
        Apply(false);
    }

    public void Toggle() => Apply(!IsOpen);
    public void Open()   => Apply(true);
    public void Close()  => Apply(false);

    public void SetHeight(float height)
    {
        terminalHeight = Mathf.Clamp(height, minTerminalHeight, maxTerminalHeight);
        if (IsOpen) Apply(true);
    }

    void Apply(bool open)
    {
        IsOpen = open;
        terminalHeight = Mathf.Clamp(terminalHeight, minTerminalHeight, maxTerminalHeight);

        if (panelRoot != null && panelRoot.gameObject.activeSelf != open)
            panelRoot.gameObject.SetActive(open);

        if (panelRoot != null)
        {
            panelRoot.offsetMin = new Vector2(panelRoot.offsetMin.x, 0f);
            panelRoot.offsetMax = new Vector2(panelRoot.offsetMax.x, open ? terminalHeight : 0f);
        }

        if (editorBody != null)
        {
            Vector2 min = editorBody.offsetMin;
            min.y = open ? terminalHeight : 0f;
            editorBody.offsetMin = min;
        }
    }
}
