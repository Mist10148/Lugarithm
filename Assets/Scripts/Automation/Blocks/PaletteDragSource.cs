using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Makes a palette button a drag source: dragging it onto the canvas creates a
/// new block at the drop slot. Clicking it still appends (see
/// <see cref="BlockPaletteController"/>).
/// </summary>
public class PaletteDragSource : MonoBehaviour,
                                 IBeginDragHandler, IDragHandler, IEndDragHandler
{
    BlockCanvasController _canvas;
    BlockType             _type;

    public void Setup(BlockCanvasController canvas, BlockType type)
    {
        _canvas = canvas;
        _type   = type;
    }

    public void OnBeginDrag(PointerEventData e) { if (_canvas != null) _canvas.BeginPaletteDrag(_type, e); }
    public void OnDrag(PointerEventData e)      { if (_canvas != null) _canvas.UpdateDrag(e); }
    public void OnEndDrag(PointerEventData e)   { if (_canvas != null) _canvas.EndDrag(e); }
}
