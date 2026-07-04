using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Draggable horizontal splitter for the code window terminal. Dragging upward
/// grows the terminal; dragging downward gives space back to the editor.
/// </summary>
public class TerminalResizeHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [SerializeField] private TerminalPanelController terminal;

    RectTransform _canvas;

    void Awake()
    {
        Canvas c = GetComponentInParent<Canvas>();
        if (c != null) _canvas = (RectTransform)c.transform;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (terminal != null) terminal.Open();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (terminal == null) return;

        float scale = _canvas != null ? Mathf.Max(0.0001f, _canvas.localScale.x) : 1f;
        terminal.SetHeight(terminal.TerminalHeight + eventData.delta.y / scale);
    }
}
