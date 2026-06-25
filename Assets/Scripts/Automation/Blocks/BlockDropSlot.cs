using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A connection point between blocks. The canvas spawns one at every position in
/// every list (root sequence, container body, else body) so a dragged block has a
/// target everywhere a block could legally go. At rest the slot is a hairline gap
/// that keeps the stack reading as connected puzzle pieces; while dragging it
/// shows a faint hint, and the nearest usable slot OPENS a block-sized gap (the
/// Scratch "this is where it lands" preview). The slot is indented to its nesting
/// level so moving the cursor right snaps a block into a deeper C-block.
/// </summary>
public class BlockDropSlot : MonoBehaviour
{
    [SerializeField] private Image         bar;
    [SerializeField] private LayoutElement sizer;        // root height (gap size)
    [SerializeField] private LayoutElement indentSpacer; // nesting indent

    const float IndentWidth = 24f;
    const float RestHeight   = 4f;   // hairline so the stack reads as connected
    const float OpenHeight   = 44f;  // block-sized landing lane while dragging

    static readonly Color Hint      = new Color(0.55f, 0.60f, 0.68f, 0.40f);
    static readonly Color Highlight = new Color(0.95f, 0.65f, 0.15f, 0.95f);

    /// <summary>The list this slot inserts into, and the index within it.</summary>
    public List<BlockNode> List  { get; private set; }
    public int             Index { get; private set; }

    /// <summary>False while dragging a block onto its own subtree (illegal).</summary>
    public bool Usable { get; set; } = true;

    public RectTransform Rect => (RectTransform)transform;

    public void Setup(List<BlockNode> list, int index, int indent)
    {
        List  = list;
        Index = index;
        if (indentSpacer != null) indentSpacer.preferredWidth = indent * IndentWidth;
    }

    /// <summary>
    /// Hairline at rest (false); a full open landing lane while dragging (true).
    /// Every usable slot opens to the same height so activating one only recolors
    /// it — the stack doesn't reflow under the cursor, so the gap never jumps.
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (visible) SetState(OpenHeight, Hint, true);
        else         SetState(RestHeight, Hint, false);
    }

    /// <summary>Glow as the active drop target (true) or dim back to a hint (false).</summary>
    public void SetHighlight(bool on)
    {
        SetState(OpenHeight, on ? Highlight : Hint, true);
    }

    void SetState(float height, Color color, bool barOn)
    {
        if (sizer != null) sizer.preferredHeight = height;
        if (bar != null) { bar.enabled = barOn; bar.color = color; }
    }
}
