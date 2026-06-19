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
    }
}
