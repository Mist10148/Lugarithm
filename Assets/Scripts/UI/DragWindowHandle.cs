using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attach to a title bar RectTransform. Dragging the title bar moves the
/// parent window around the canvas, clamped inside the canvas bounds.
/// </summary>
public class DragWindowHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [SerializeField] RectTransform windowRoot;

    RectTransform _canvas;

    void Awake()
    {
        Canvas c = GetComponentInParent<Canvas>();
        if (c != null) _canvas = c.GetComponent<RectTransform>();
    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (windowRoot != null) windowRoot.SetAsLastSibling();   // bring window to front
    }

    public void OnDrag(PointerEventData e)
    {
        if (windowRoot == null || _canvas == null) return;
        windowRoot.anchoredPosition += e.delta / _canvas.localScale.x;
        WindowClampUtil.Clamp(windowRoot, _canvas);
    }
}
