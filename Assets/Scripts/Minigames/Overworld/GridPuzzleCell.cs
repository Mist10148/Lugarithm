using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Forwards continuous pointer gestures from one shared puzzle cell.</summary>
public sealed class GridPuzzleCell : MonoBehaviour,
    IPointerDownHandler, IPointerEnterHandler, IPointerUpHandler, IPointerClickHandler
{
    public int index;
    [System.NonSerialized] public GridPuzzleMinigame owner;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            owner?.CellPointerDown(index);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        owner?.CellPointerEnter(index);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            owner?.CellPointerUp(index);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
            owner?.CellSecondaryClick(index);
    }
}
