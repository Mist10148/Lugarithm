using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// The Scratch-style block canvas: renders the block tree as nested, indented
/// cards separated by insertion slots. Drag a card (with its children) onto a
/// slot to move it, onto the trash to delete it; drag a palette block onto a
/// slot to create one. Compiles straight to the shared AST — no syntax errors
/// are possible by construction.
/// </summary>
public class BlockCanvasController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform content;
    [SerializeField] private ScrollRect    scrollRect;
    [SerializeField] private BlockRowView   rowTemplate;
    [SerializeField] private BlockDropSlot  slotTemplate;
    [SerializeField] private RectTransform  trashZone;
    [SerializeField] private RectTransform  dragLayer;

    static readonly Color ElseColor = new Color(0.78f, 0.55f, 0.20f, 1f);

    /// <summary>Scratch-style category color for a block, shared with the palette.</summary>
    public static Color CategoryColor(BlockType type)
    {
        switch (type)
        {
            case BlockType.MoveForward:
            case BlockType.TurnLeft:
            case BlockType.TurnRight:   return new Color(0.26f, 0.45f, 0.85f, 1f);  // motion (blue)
            case BlockType.PickUp:
            case BlockType.DropOff:
            case BlockType.CollectFare: return new Color(0.30f, 0.66f, 0.42f, 1f);  // passengers (green)
            default:                    return new Color(0.90f, 0.60f, 0.16f, 1f);  // control (gold)
        }
    }

    readonly List<BlockNode>  _roots   = new List<BlockNode>();
    readonly List<GameObject> _spawned = new List<GameObject>();
    readonly List<BlockDropSlot> _slots = new List<BlockDropSlot>();
    readonly Dictionary<BlockNode, BlockRowView> _rowMap = new Dictionary<BlockNode, BlockRowView>();

    string[] _allowedQueries = { "frontIsClear" };
    ConsoleController _console;

    // Drag state
    BlockNode      _dragNode;     // moving an existing block
    BlockType?     _paletteType;  // inserting a new block from the palette
    GameObject     _ghost;
    BlockDropSlot  _activeSlot;

    public IReadOnlyList<BlockNode> Roots => _roots;

    // -------------------------------------------------------------------------

    public void Init(string[] allowedQueries, ConsoleController console)
    {
        if (allowedQueries != null && allowedQueries.Length > 0)
            _allowedQueries = allowedQueries;
        _console = console;
        Rebuild();
    }

    // -------------------------------------------------------------------------
    // Public API

    /// <summary>Palette click fallback: appends a new block to the end.</summary>
    public void InsertBlock(BlockType type)
    {
        InsertNewAt(type, _roots, _roots.Count);
        Rebuild();
    }

    /// <summary>Compiles the canvas; flashes offending blocks on problems.</summary>
    public ProgramNode BuildProgram(out List<LangError> errors)
    {
        ProgramNode program = BlockProgram.ToAst(_roots, out errors, out List<BlockNode> offenders);

        foreach (BlockNode offender in offenders)
            if (_rowMap.TryGetValue(offender, out BlockRowView row) && row != null)
                row.PulseError();

        return program;
    }

    /// <summary>Canonical text of the current canvas (results screen).</summary>
    public string ToSourceText()
    {
        ProgramNode program = BlockProgram.ToAst(_roots, out _, out _);
        return AstPrinter.Print(program);
    }

    /// <summary>Execution highlight: pulse the card that produced the action.</summary>
    public void HighlightExecuting(object sourceRef)
    {
        if (sourceRef is BlockNode node &&
            _rowMap.TryGetValue(node, out BlockRowView row) && row != null)
        {
            row.PulseExecuting();
        }
    }

    public void ClearAll()
    {
        _roots.Clear();
        Rebuild();
    }

    // -------------------------------------------------------------------------
    // Rendering

    void Rebuild()
    {
        foreach (GameObject go in _spawned)
            if (go != null) Destroy(go);
        _spawned.Clear();
        _slots.Clear();
        _rowMap.Clear();

        RenderList(_roots, 0);

        foreach (BlockDropSlot slot in _slots) slot.SetVisible(false);
    }

    void RenderList(List<BlockNode> list, int indent)
    {
        for (int i = 0; i <= list.Count; i++)
        {
            SpawnSlot(list, i);

            if (i == list.Count) break;

            BlockNode node = list[i];
            SpawnRow(node, indent);

            if (node.IsContainer)
            {
                RenderList(node.Body, indent + 1);

                if (node.HasElse)
                {
                    SpawnElseHeader(indent);
                    RenderList(node.ElseBody, indent + 1);
                }
            }
        }
    }

    void SpawnSlot(List<BlockNode> list, int index)
    {
        BlockDropSlot slot = Instantiate(slotTemplate, content);
        slot.gameObject.SetActive(true);
        slot.Setup(list, index);
        slot.SetVisible(false);
        _spawned.Add(slot.gameObject);
        _slots.Add(slot);
    }

    void SpawnRow(BlockNode node, int indent)
    {
        BlockRowView row = Instantiate(rowTemplate, content);
        row.gameObject.SetActive(true);
        _spawned.Add(row.gameObject);
        _rowMap[node] = row;

        string headerText  = node.IsContainer
            ? (node.Type == BlockType.While ? "while" : "if")
            : BlockProgram.ActionName(node.Type) + "()";
        string conditionText = (node.Negate ? "not " : "") + node.Query + "()";

        row.Configure(this, node, headerText, indent,
                      CategoryColor(node.Type),
                      isContainer: node.IsContainer,
                      negateOn: node.Negate,
                      conditionText: conditionText,
                      draggable: true);

        row.Bind(
            onCycleCondition: () => { CycleQuery(node); Rebuild(); },
            onToggleNot:      () => { node.Negate = !node.Negate; Rebuild(); },
            onDelete:         () => { RemoveNode(node); Rebuild(); });
    }

    void SpawnElseHeader(int indent)
    {
        BlockRowView row = Instantiate(rowTemplate, content);
        row.gameObject.SetActive(true);
        _spawned.Add(row.gameObject);

        row.Configure(this, null, "else:", indent, ElseColor,
                      isContainer: false, negateOn: false, conditionText: "",
                      draggable: false);
        row.Bind(null, null, null);
    }

    // -------------------------------------------------------------------------
    // Drag — existing blocks

    public void BeginDrag(BlockNode node, PointerEventData e)
    {
        _dragNode    = node;
        _paletteType = null;
        CreateGhost(BlockProgram.Label(node));
        ShowSlots(excludeSubtreeOf: node);
        UpdateDrag(e);
    }

    public void BeginPaletteDrag(BlockType type, PointerEventData e)
    {
        _paletteType = type;
        _dragNode    = null;
        CreateGhost(PaletteGhostLabel(type));
        ShowSlots(excludeSubtreeOf: null);
        UpdateDrag(e);
    }

    public void UpdateDrag(PointerEventData e)
    {
        if (_dragNode == null && _paletteType == null) return;

        MoveGhost(e.position);

        bool overTrash = OverTrash(e.position);
        _activeSlot = overTrash ? null : NearestSlot(e.position);

        foreach (BlockDropSlot slot in _slots)
            slot.SetHighlight(slot == _activeSlot);
    }

    public void EndDrag(PointerEventData e)
    {
        bool overTrash = OverTrash(e.position);

        if (_dragNode != null)
        {
            if (overTrash) RemoveNode(_dragNode);
            else if (_activeSlot != null) MoveNode(_dragNode, _activeSlot.List, _activeSlot.Index);
        }
        else if (_paletteType != null && !overTrash)
        {
            if (_activeSlot != null) InsertNewAt(_paletteType.Value, _activeSlot.List, _activeSlot.Index);
            else                     InsertNewAt(_paletteType.Value, _roots, _roots.Count);
        }

        ClearDrag();
        Rebuild();
    }

    // -------------------------------------------------------------------------
    // Drag helpers

    void ShowSlots(BlockNode excludeSubtreeOf)
    {
        foreach (BlockDropSlot slot in _slots)
        {
            bool usable = excludeSubtreeOf == null || !IsListInSubtree(slot.List, excludeSubtreeOf);
            slot.Usable = usable;
            slot.SetVisible(usable);
        }
    }

    BlockDropSlot NearestSlot(Vector2 screenPos)
    {
        BlockDropSlot best = null;
        float bestSqr = float.MaxValue;

        foreach (BlockDropSlot slot in _slots)
        {
            if (!slot.Usable) continue;
            Vector2 sp = RectTransformUtility.WorldToScreenPoint(null, slot.Rect.position);
            float sqr = (sp - screenPos).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; best = slot; }
        }

        return best;
    }

    bool OverTrash(Vector2 screenPos)
    {
        return trashZone != null &&
               RectTransformUtility.RectangleContainsScreenPoint(trashZone, screenPos, null);
    }

    void CreateGhost(string text)
    {
        DestroyGhost();
        if (dragLayer == null) return;

        var go = new GameObject("BlockGhost", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(dragLayer, false);
        rt.sizeDelta = new Vector2(210f, 42f);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.95f, 0.65f, 0.15f, 0.85f);

        var labelGo = new GameObject("Text", typeof(RectTransform));
        var labelRt = (RectTransform)labelGo.transform;
        labelRt.SetParent(rt, false);
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(10f, 0f);
        labelRt.offsetMax = new Vector2(-10f, 0f);
        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 20f;
        tmp.color = new Color(0.06f, 0.07f, 0.10f);
        tmp.alignment = TextAlignmentOptions.MidlineLeft;

        var group = go.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable   = false;

        rt.SetAsLastSibling();
        _ghost = go;
    }

    void MoveGhost(Vector2 screenPos)
    {
        if (_ghost == null || dragLayer == null) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragLayer, screenPos, null, out Vector2 local))
        {
            ((RectTransform)_ghost.transform).anchoredPosition = local + new Vector2(105f, 0f);
        }
    }

    void DestroyGhost()
    {
        if (_ghost != null) Destroy(_ghost);
        _ghost = null;
    }

    void ClearDrag()
    {
        DestroyGhost();
        _dragNode    = null;
        _paletteType = null;
        _activeSlot  = null;
    }

    // -------------------------------------------------------------------------
    // Tree edits

    void InsertNewAt(BlockType type, List<BlockNode> list, int index)
    {
        var node = new BlockNode(type);
        if (node.IsContainer) node.Query = _allowedQueries[0];
        list.Insert(Mathf.Clamp(index, 0, list.Count), node);
    }

    void MoveNode(BlockNode node, List<BlockNode> targetList, int targetIndex)
    {
        if (!FindParent(_roots, node, out List<BlockNode> srcList, out int srcIndex)) return;

        srcList.RemoveAt(srcIndex);
        if (srcList == targetList && srcIndex < targetIndex) targetIndex--;
        targetList.Insert(Mathf.Clamp(targetIndex, 0, targetList.Count), node);
    }

    void RemoveNode(BlockNode node)
    {
        if (FindParent(_roots, node, out List<BlockNode> srcList, out int srcIndex))
            srcList.RemoveAt(srcIndex);
    }

    void CycleQuery(BlockNode node)
    {
        int current = Array.IndexOf(_allowedQueries, node.Query);
        node.Query = _allowedQueries[(current + 1 + _allowedQueries.Length) % _allowedQueries.Length];
    }

    // -------------------------------------------------------------------------
    // Tree queries

    static bool FindParent(List<BlockNode> list, BlockNode target,
                           out List<BlockNode> parent, out int index)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == target) { parent = list; index = i; return true; }
            if (FindParent(list[i].Body, target, out parent, out index)) return true;
            if (FindParent(list[i].ElseBody, target, out parent, out index)) return true;
        }
        parent = null;
        index  = -1;
        return false;
    }

    static bool IsListInSubtree(List<BlockNode> list, BlockNode root)
    {
        if (list == root.Body || list == root.ElseBody) return true;
        foreach (BlockNode child in root.Body)
            if (IsListInSubtree(list, child)) return true;
        foreach (BlockNode child in root.ElseBody)
            if (IsListInSubtree(list, child)) return true;
        return false;
    }

    static string PaletteGhostLabel(BlockType type)
    {
        switch (type)
        {
            case BlockType.While:  return "while …:";
            case BlockType.If:     return "if …:";
            case BlockType.IfElse: return "if …: else:";
            default:               return BlockProgram.ActionName(type) + "()";
        }
    }
}
