using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Collapsible terminal docked to the bottom of the code window. Hosts the
/// execution <see cref="ConsoleController"/> (action log, warnings, errors, and
/// <c>print()</c> output) and reserves space for itself by shrinking the editor
/// body when open. A toolbar button toggles it; the header ✕ closes it. Starts
/// closed so the editor fills the window by default.
/// </summary>
public class TerminalPanelController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform panelRoot;   // the terminal panel itself
    [SerializeField] private RectTransform editorBody;  // shrunk from the bottom when open
    [SerializeField] private ConsoleController console;  // the scrolling log
    [SerializeField] private Button toggleButton;        // opens/toggles (in the run toolbar / window)
    [SerializeField] private Button closeButton;         // ✕ in the terminal header

    [Header("Layout")]
    [SerializeField] private float terminalHeight = 190f;

    public bool IsOpen { get; private set; }
    public ConsoleController Console => console;

    void Awake()
    {
        if (toggleButton != null) toggleButton.onClick.AddListener(Toggle);
        if (closeButton  != null) closeButton.onClick.AddListener(Close);
        Apply(false);
    }

    public void Toggle() => Apply(!IsOpen);
    public void Open()   => Apply(true);
    public void Close()  => Apply(false);

    void Apply(bool open)
    {
        IsOpen = open;

        if (panelRoot != null && panelRoot.gameObject.activeSelf != open)
            panelRoot.gameObject.SetActive(open);

        // Reserve / reclaim the bottom strip so the editor and terminal never overlap.
        if (editorBody != null)
        {
            Vector2 min = editorBody.offsetMin;
            min.y = open ? terminalHeight : 0f;
            editorBody.offsetMin = min;
        }
    }
}
