using UnityEngine;
using UnityEngine.EventSystems;

public interface IVerticalReorderTarget
{
    void MoveCard(int fromIndex, int toIndex);
}

/// <summary>Reusable vertical drag handle; arrow buttons remain independent fallbacks.</summary>
public sealed class VerticalReorderHandle : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int index;
    public float rowStep = 64f;
    [System.NonSerialized] public IVerticalReorderTarget owner;

    RectTransform _rect;
    Vector2 _start;
    float _dragDelta;

    void Awake() => _rect = transform as RectTransform;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_rect == null) _rect = transform as RectTransform;
        if (_rect != null) _start = _rect.anchoredPosition;
        _dragDelta = 0f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        _dragDelta += eventData.delta.y;
        if (_rect != null) _rect.anchoredPosition = _start + new Vector2(0f, _dragDelta);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_rect == null) return;
        float delta = _dragDelta;
        _rect.anchoredPosition = _start;
        if (Mathf.Abs(delta) < rowStep * 0.35f) return;
        int offset = Mathf.RoundToInt(-delta / rowStep);
        if (offset == 0) offset = delta > 0f ? -1 : 1;
        owner?.MoveCard(index, index + offset);
    }
}
