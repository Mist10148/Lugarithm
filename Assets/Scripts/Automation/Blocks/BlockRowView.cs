using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One rendered row on the block canvas (a block header, body line, or the
/// "else:" separator). Dumb view — <see cref="BlockCanvasController"/>
/// instantiates rows from a template, configures them, and binds callbacks.
/// </summary>
public class BlockRowView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image  background;
    [SerializeField] private LayoutElement indentSpacer;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Button selectButton;
    [SerializeField] private Button conditionButton;
    [SerializeField] private Button notButton;
    [SerializeField] private Image  notFace;
    [SerializeField] private Button addInsideButton;
    [SerializeField] private Button addElseButton;
    [SerializeField] private Button upButton;
    [SerializeField] private Button downButton;
    [SerializeField] private Button deleteButton;

    Color _baseColor;
    Coroutine _pulse;

    const float IndentWidth = 28f;

    // -------------------------------------------------------------------------

    public void Configure(string text, int indent, Color color,
                          bool showConditionControls, bool negateOn,
                          bool showAddInside, bool showAddElse, bool interactive)
    {
        if (label != null) label.text = text;
        if (indentSpacer != null) indentSpacer.preferredWidth = indent * IndentWidth;

        _baseColor = color;
        if (background != null) background.color = color;

        if (conditionButton != null) conditionButton.gameObject.SetActive(showConditionControls);
        if (notButton       != null) notButton.gameObject.SetActive(showConditionControls);
        if (notFace         != null) notFace.color = negateOn
            ? new Color(0.95f, 0.65f, 0.15f)
            : new Color(0.25f, 0.28f, 0.34f);

        if (addInsideButton != null) addInsideButton.gameObject.SetActive(showAddInside);
        if (addElseButton   != null) addElseButton.gameObject.SetActive(showAddElse);
        if (upButton        != null) upButton.gameObject.SetActive(interactive);
        if (downButton      != null) downButton.gameObject.SetActive(interactive);
        if (deleteButton    != null) deleteButton.gameObject.SetActive(interactive);
        if (selectButton    != null) selectButton.interactable = interactive;
    }

    public void Bind(Action onSelect, Action onCycleCondition, Action onToggleNot,
                     Action onAddInside, Action onAddElse,
                     Action onUp, Action onDown, Action onDelete)
    {
        Rebind(selectButton,    onSelect);
        Rebind(conditionButton, onCycleCondition);
        Rebind(notButton,       onToggleNot);
        Rebind(addInsideButton, onAddInside);
        Rebind(addElseButton,   onAddElse);
        Rebind(upButton,        onUp);
        Rebind(downButton,      onDown);
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
    // Highlights

    /// <summary>Brief execution-highlight pulse (current block).</summary>
    public void PulseExecuting()
    {
        StartPulse(new Color(0.95f, 0.65f, 0.15f, 1f), 0.30f);
    }

    /// <summary>Validation-error flash (empty container, etc.).</summary>
    public void PulseError()
    {
        StartPulse(new Color(0.90f, 0.25f, 0.20f, 1f), 0.8f);
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
