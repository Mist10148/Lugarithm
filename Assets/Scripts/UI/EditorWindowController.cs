using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// A floating editor window (GoCars / The Farmer Was Replaced style): click
/// anywhere to focus (bring to front), a title-bar button to minimize/restore
/// (collapse to just the title bar), and a close button that hides it. Title-bar
/// dragging is handled by <see cref="DragWindowHandle"/> and corner resizing by
/// <see cref="ResizeHandle"/>; this owns focus, minimize, and open/close so a
/// <see cref="WindowDock"/> can reopen a closed or minimized window.
/// </summary>
[DisallowMultipleComponent]
public class EditorWindowController : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private RectTransform window;       // the moved/resized root (defaults to self)
    [SerializeField] private RectTransform content;      // hidden while minimized
    [SerializeField] private Button        minimizeButton;
    [SerializeField] private Button        closeButton;
    [SerializeField] private TMP_Text      minimizeLabel;
    [SerializeField] private TMP_Text      titleLabel;

    public string Title => titleLabel != null ? titleLabel.text : name;
    public bool   IsOpen => window != null && window.gameObject.activeSelf;
    public bool   IsMinimized { get; private set; }

    const float TitleBarHeight = 34f;
    float _restoreHeight;

    void Awake()
    {
        if (window == null) window = (RectTransform)transform;
        _restoreHeight = window.sizeDelta.y;
        if (minimizeButton != null) minimizeButton.onClick.AddListener(ToggleMinimize);
        if (closeButton    != null) closeButton.onClick.AddListener(Close);
    }

    // Any pointer-down on the window background/title brings it to the front.
    public void OnPointerDown(PointerEventData e) => BringToFront();

    public void BringToFront()
    {
        if (window != null) window.SetAsLastSibling();
    }

    public void Open()
    {
        if (window == null) return;
        window.gameObject.SetActive(true);
        if (IsMinimized) ToggleMinimize();   // un-minimize on reopen
        BringToFront();
    }

    public void Close()
    {
        if (window != null) window.gameObject.SetActive(false);
    }

    public void ToggleMinimize()
    {
        IsMinimized = !IsMinimized;
        if (content != null) content.gameObject.SetActive(!IsMinimized);

        if (window != null)
        {
            Vector2 s = window.sizeDelta;
            if (IsMinimized) { _restoreHeight = s.y; s.y = TitleBarHeight; }
            else             { s.y = _restoreHeight; }
            window.sizeDelta = s;
        }

        if (minimizeLabel != null) minimizeLabel.text = IsMinimized ? "+" : "_";
        BringToFront();
    }
}
