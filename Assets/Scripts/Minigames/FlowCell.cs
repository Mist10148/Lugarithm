using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// One cell of the <see cref="FlowConnectMinigame"/> grid. Forwards pointer down
/// and pointer-enter (drag-over) events to its owner so paths can be drawn by
/// dragging. The grid coordinates and the two images are set once by the scene
/// builder; <see cref="owner"/> is bound at runtime when the puzzle opens.
/// </summary>
public class FlowCell : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler
{
    public int   x;
    public int   y;
    public Image background;   // raycast target — receives the pointer events
    public Image dot;          // hub marker (hidden on non-hub cells)

    [System.NonSerialized] public FlowConnectMinigame owner;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (owner != null) owner.CellDown(x, y);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (owner != null) owner.CellEnter(x, y);
    }
}
