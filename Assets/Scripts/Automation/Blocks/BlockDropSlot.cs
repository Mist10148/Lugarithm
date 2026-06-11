using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// An insertion gap between blocks. The canvas spawns one at every position in
/// every list (root sequence, container body, else body) so a dragged block
/// has a target everywhere a block could legally go. Hidden until a drag
/// starts; the nearest usable slot highlights as the drop target.
/// </summary>
public class BlockDropSlot : MonoBehaviour
{
    [SerializeField] private Image bar;

    static readonly Color Idle      = new Color(0.50f, 0.55f, 0.62f, 0.45f);
    static readonly Color Highlight = new Color(0.95f, 0.65f, 0.15f, 0.95f);

    /// <summary>The list this slot inserts into, and the index within it.</summary>
    public List<BlockNode> List  { get; private set; }
    public int             Index { get; private set; }

    /// <summary>False while dragging a block onto its own subtree (illegal).</summary>
    public bool Usable { get; set; } = true;

    public RectTransform Rect => (RectTransform)transform;

    public void Setup(List<BlockNode> list, int index)
    {
        List  = list;
        Index = index;
    }

    public void SetVisible(bool visible)
    {
        if (bar != null) { bar.enabled = visible; bar.color = Idle; }
    }

    public void SetHighlight(bool on)
    {
        if (bar != null) bar.color = on ? Highlight : Idle;
    }
}
