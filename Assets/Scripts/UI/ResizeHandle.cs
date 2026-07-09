using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Bottom-right corner grip for a floating window: drag to resize the target,
/// clamped to a minimum size. Pairs with <see cref="EditorWindowController"/> and
/// <see cref="DragWindowHandle"/> to make the editor windows behave like real
/// desktop windows.
/// </summary>
public class ResizeHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [SerializeField] private RectTransform target;
    [SerializeField] private Vector2 minSize = new Vector2(360f, 220f);

    RectTransform _canvas;

    void Awake()
    {
        Canvas c = GetComponentInParent<Canvas>();
        if (c != null) _canvas = (RectTransform)c.transform;
    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (target != null) target.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData e)
    {
        if (target == null) return;

        float scale = _canvas != null ? _canvas.localScale.x : 1f;
        Vector2 d = e.delta / Mathf.Max(0.0001f, scale);

        Vector2 size = target.sizeDelta;
        size.x = Mathf.Max(minSize.x, size.x + d.x);
        size.y = Mathf.Max(minSize.y, size.y - d.y);   // dragging down (−y) grows height
        target.sizeDelta = size;
        ClampToCanvas();
    }

    void ClampToCanvas()
    {
        if (target == null || _canvas == null) return;

        var win = new Vector3[4];
        var can = new Vector3[4];
        target.GetWorldCorners(win);
        _canvas.GetWorldCorners(can);

        float scale = Mathf.Max(0.0001f, _canvas.localScale.x);
        Vector2 size = target.sizeDelta;

        if (win[2].x > can[2].x)
            size.x -= (win[2].x - can[2].x) / scale;
        if (win[0].y < can[0].y)
            size.y -= (can[0].y - win[0].y) / scale;

        size.x = Mathf.Max(minSize.x, size.x);
        size.y = Mathf.Max(minSize.y, size.y);
        target.sizeDelta = size;

        // Growing a window can also shove it past an edge (pivot-dependent) —
        // run the shared position clamp so all four edges stay recoverable.
        WindowClampUtil.Clamp(target, _canvas);
    }
}
