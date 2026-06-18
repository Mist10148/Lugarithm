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
        ClampToCanvas();
    }

    // Keep at least a grabbable strip of the window inside the canvas.
    void ClampToCanvas()
    {
        var win = new Vector3[4];
        var can = new Vector3[4];
        windowRoot.GetWorldCorners(win);   // 0=BL 1=TL 2=TR 3=BR
        _canvas.GetWorldCorners(can);
        float margin = 48f * _canvas.localScale.x;

        Vector2 push = Vector2.zero;
        if (win[0].x > can[3].x - margin) push.x = (can[3].x - margin) - win[0].x; // off right
        if (win[3].x < can[0].x + margin) push.x = (can[0].x + margin) - win[3].x; // off left
        if (win[0].y > can[1].y - margin) push.y = (can[1].y - margin) - win[0].y; // off top
        if (win[1].y < can[0].y + margin) push.y = (can[0].y + margin) - win[1].y; // off bottom

        if (push != Vector2.zero)
            windowRoot.anchoredPosition += push / _canvas.localScale.x;
    }
}
