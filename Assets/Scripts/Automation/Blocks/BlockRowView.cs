using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// One block card on the Scratch-style canvas. The whole card is a drag handle:
/// press and drag it (with its nested children) to a new slot, or onto the
/// trash to delete it. Containers show a condition chip (click to cycle the
/// query) and a "not" toggle. Dumb view — <see cref="BlockCanvasController"/>
/// owns the tree and the drag logic.
/// </summary>
public class BlockRowView : MonoBehaviour,
                            IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("References")]
    [SerializeField] private Image          background;
    [SerializeField] private LayoutElement  indentSpacer;
    [SerializeField] private TMP_Text       label;
    [SerializeField] private Button         conditionButton;
    [SerializeField] private TMP_Text       conditionLabel;
    [SerializeField] private Button         notButton;
    [SerializeField] private Image          notFace;
    [SerializeField] private Button         deleteButton;

    const float IndentWidth = 24f;

    BlockCanvasController _owner;
    BlockNode             _node;
    bool                  _draggable;

    Color     _baseColor;
    Coroutine _pulse;

    public BlockNode Node => _node;

    // -------------------------------------------------------------------------

    public void Configure(BlockCanvasController owner, BlockNode node, string text, int indent,
                          Color color, bool showCondition, bool showNot, bool negateOn,
                          string conditionText, bool draggable)
    {
        _owner     = owner;
        _node      = node;
        _draggable = draggable;

        if (label != null) label.text = text;
        if (indentSpacer != null) indentSpacer.preferredWidth = indent * IndentWidth;

        _baseColor = color;
        if (background != null) background.color = color;

        if (conditionButton != null) conditionButton.gameObject.SetActive(showCondition);
        if (notButton       != null) notButton.gameObject.SetActive(showNot);
        if (conditionLabel  != null && showCondition) conditionLabel.text = conditionText;
        if (notFace         != null) notFace.color = negateOn
            ? new Color(0.95f, 0.65f, 0.15f)
            : new Color(0.25f, 0.28f, 0.34f);

        if (deleteButton != null) deleteButton.gameObject.SetActive(draggable);
    }

    public void Bind(Action onCycleCondition, Action onToggleNot, Action onDelete)
    {
        Rebind(conditionButton, onCycleCondition);
        Rebind(notButton,       onToggleNot);
        Rebind(deleteButton,    onDelete);
    }

    static void Rebind(Button button, Action action)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        if (action != null)
            button.onClick.AddListener(() => action());
    }

    // -------------------------------------------------------------------------
    // Drag (whole card)

    public void OnBeginDrag(PointerEventData e)
    {
        if (_draggable && _owner != null && _node != null) _owner.BeginDrag(_node, e);
    }

    public void OnDrag(PointerEventData e)
    {
        if (_draggable && _owner != null) _owner.UpdateDrag(e);
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (_draggable && _owner != null) _owner.EndDrag(e);
    }

    // -------------------------------------------------------------------------
    // Highlights

    /// <summary>Brief execution-highlight pulse (current block).</summary>
    public void PulseExecuting() => StartPulse(new Color(0.95f, 0.65f, 0.15f, 1f), 0.30f);

    /// <summary>Validation-error flash (empty container, etc.).</summary>
    public void PulseError() => StartPulse(new Color(0.90f, 0.25f, 0.20f, 1f), 0.8f);

    public void ClearPulse()
    {
        if (_pulse != null)
        {
            StopCoroutine(_pulse);
            _pulse = null;
        }
        if (background != null)
            background.color = _baseColor;
    }

    void StartPulse(Color color, float seconds)
    {
        if (background == null || !isActiveAndEnabled) return;
        if (_pulse != null) StopCoroutine(_pulse);
        _pulse = StartCoroutine(Pulse(color, seconds));
    }

    IEnumerator Pulse(Color color, float seconds)
    {
        background.color = color;
        yield return new WaitForSeconds(seconds);
        background.color = _baseColor;
        _pulse = null;
    }
}
