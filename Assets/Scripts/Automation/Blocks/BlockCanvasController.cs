using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Block Mode canvas: renders the block tree as indented rows with an
/// insertion cursor. Tapping a row moves the cursor after it; container rows
/// offer "add inside" / "add else"; ▲▼✕ reorder and delete. The palette
/// inserts new blocks at the cursor. Compiles straight to the shared AST —
/// no syntax errors are possible by construction.
/// </summary>
public class BlockCanvasController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform content;
    [SerializeField] private ScrollRect    scrollRect;
    [SerializeField] private BlockRowView  rowTemplate;
    [SerializeField] private RectTransform cursorTemplate;

    static readonly Color ActionColor    = new Color(0.22f, 0.30f, 0.42f, 1f);
    static readonly Color ContainerColor = new Color(0.34f, 0.26f, 0.46f, 1f);
    static readonly Color ElseColor      = new Color(0.28f, 0.24f, 0.38f, 1f);

    readonly List<BlockNode> _roots = new List<BlockNode>();
    readonly List<GameObject> _spawned = new List<GameObject>();
    readonly Dictionary<BlockNode, BlockRowView> _rowMap = new Dictionary<BlockNode, BlockRowView>();

    List<BlockNode> _cursorList;
    int             _cursorIndex;

    string[] _allowedQueries = { "frontIsClear" };
    ConsoleController _console;

    public IReadOnlyList<BlockNode> Roots => _roots;

    // -------------------------------------------------------------------------

    public void Init(string[] allowedQueries, ConsoleController console)
    {
        if (allowedQueries != null && allowedQueries.Length > 0)
            _allowedQueries = allowedQueries;
        _console = console;

        _cursorList  = _roots;
        _cursorIndex = 0;
        Rebuild();
    }

    // -------------------------------------------------------------------------
    // Public API

    /// <summary>Inserts a new block at the cursor (palette click).</summary>
    public void InsertBlock(BlockType type)
    {
        var node = new BlockNode(type);
        if (node.IsContainer)
            node.Query = _allowedQueries[0];

        if (_cursorList == null) { _cursorList = _roots; _cursorIndex = _roots.Count; }
        _cursorIndex = Mathf.Clamp(_cursorIndex, 0, _cursorList.Count);
        _cursorList.Insert(_cursorIndex, node);
        _cursorIndex++;

        // Adding a container drops the cursor inside it — the natural next step.
        if (node.IsContainer)
        {
            _cursorList  = node.Body;
            _cursorIndex = 0;
        }

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

    /// <summary>Execution highlight: pulse the row that produced the action.</summary>
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
        _cursorList  = _roots;
        _cursorIndex = 0;
        Rebuild();
    }

    // -------------------------------------------------------------------------
    // Rendering

    void Rebuild()
    {
        foreach (GameObject go in _spawned)
            if (go != null) Destroy(go);
        _spawned.Clear();
        _rowMap.Clear();

        if (!CursorStillExists())
        {
            _cursorList  = _roots;
            _cursorIndex = _roots.Count;
        }

        RenderList(_roots, 0);
    }

    void RenderList(List<BlockNode> list, int indent)
    {
        for (int i = 0; i <= list.Count; i++)
        {
            if (list == _cursorList && i == _cursorIndex)
                SpawnCursor(indent);

            if (i == list.Count) break;

            BlockNode node = list[i];
            SpawnRow(node, list, i, indent);

            if (node.IsContainer)
            {
                RenderList(node.Body, indent + 1);

                if (node.HasElse)
                {
                    SpawnElseSeparator(node, indent);
                    RenderList(node.ElseBody, indent + 1);
                }
            }
        }
    }

    void SpawnRow(BlockNode node, List<BlockNode> list, int index, int indent)
    {
        BlockRowView row = Instantiate(rowTemplate, content);
        row.gameObject.SetActive(true);
        _spawned.Add(row.gameObject);
        _rowMap[node] = row;

        row.Configure(BlockProgram.Label(node), indent,
                      node.IsContainer ? ContainerColor : ActionColor,
                      showConditionControls: node.IsContainer,
                      negateOn: node.Negate,
                      showAddInside: node.IsContainer,
                      showAddElse: node.HasElse,
                      interactive: true);

        row.Bind(
            onSelect:         () => { SetCursor(list, index + 1); Rebuild(); },
            onCycleCondition: () => { CycleQuery(node); Rebuild(); },
            onToggleNot:      () => { node.Negate = !node.Negate; Rebuild(); },
            onAddInside:      () => { SetCursor(node.Body, node.Body.Count); Rebuild(); },
            onAddElse:        () => { SetCursor(node.ElseBody, node.ElseBody.Count); Rebuild(); },
            onUp:             () => { Swap(list, index, index - 1); Rebuild(); },
            onDown:           () => { Swap(list, index, index + 1); Rebuild(); },
            onDelete:         () => { list.RemoveAt(index); Rebuild(); });
    }

    void SpawnElseSeparator(BlockNode node, int indent)
    {
        BlockRowView row = Instantiate(rowTemplate, content);
        row.gameObject.SetActive(true);
        _spawned.Add(row.gameObject);

        row.Configure("else:", indent, ElseColor,
                      showConditionControls: false, negateOn: false,
                      showAddInside: false, showAddElse: false, interactive: false);
        row.Bind(null, null, null, null, null, null, null, null);
    }

    void SpawnCursor(int indent)
    {
        RectTransform cursor = Instantiate(cursorTemplate, content);
        cursor.gameObject.SetActive(true);
        _spawned.Add(cursor.gameObject);

        var layout = cursor.GetComponent<LayoutElement>();
        if (layout == null) layout = cursor.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 14f;
    }

    // -------------------------------------------------------------------------
    // Cursor / edits

    void SetCursor(List<BlockNode> list, int index)
    {
        _cursorList  = list;
        _cursorIndex = Mathf.Clamp(index, 0, list.Count);
    }

    void CycleQuery(BlockNode node)
    {
        int current = System.Array.IndexOf(_allowedQueries, node.Query);
        node.Query = _allowedQueries[(current + 1 + _allowedQueries.Length) % _allowedQueries.Length];
    }

    static void Swap(List<BlockNode> list, int a, int b)
    {
        if (a < 0 || b < 0 || a >= list.Count || b >= list.Count) return;
        (list[a], list[b]) = (list[b], list[a]);
    }

    bool CursorStillExists()
    {
        if (_cursorList == _roots) return true;
        return ListReachable(_roots);

        bool ListReachable(List<BlockNode> list)
        {
            foreach (BlockNode node in list)
            {
                if (node.Body == _cursorList || node.ElseBody == _cursorList) return true;
                if (ListReachable(node.Body)) return true;
                if (ListReachable(node.ElseBody)) return true;
            }
            return false;
        }
    }
}
