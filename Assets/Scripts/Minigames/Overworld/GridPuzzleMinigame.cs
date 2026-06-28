using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One lightweight, self-contained grid puzzle that covers three of the overworld
/// station kinds on a shared N×N board of clickable cells:
///
///   • Maze        — step a token from start to goal, around walls.
///   • BlockFill    — draw a single path that fills every tile (Hamiltonian).
///   • PatternMatch — watch a flashed sequence, then repeat it.
///
/// Click-based (no dragging) to stay robust. Reports success via the onSolved
/// callback; the player can also quit back out. Content comes from
/// <see cref="OverworldPuzzleLibrary"/>; mazes/fills are always solvable as built.
/// </summary>
public class GridPuzzleMinigame : MonoBehaviour
{
    /// <summary>Board side length. Layouts use the top-left w×h subregion.</summary>
    public const int Grid = 6;

    [Header("References")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text   titleLabel;
    [SerializeField] private TMP_Text   instructionLabel;
    [SerializeField] private TMP_Text   feedbackLabel;
    [SerializeField] private Image[]    cellImages;    // length Grid*Grid, row-major
    [SerializeField] private Button[]   cellButtons;   // length Grid*Grid, row-major
    [SerializeField] private Button     resetButton;
    [SerializeField] private Button     quitButton;

    // Palette
    static readonly Color Hidden  = new Color(0f, 0f, 0f, 0f);
    static readonly Color Wall    = new Color(0.12f, 0.13f, 0.16f);
    static readonly Color OpenCol = new Color(0.24f, 0.26f, 0.32f);
    static readonly Color Token   = new Color(0.30f, 0.74f, 0.42f);
    static readonly Color Goal    = new Color(0.95f, 0.65f, 0.15f);
    static readonly Color Visited = new Color(0.22f, 0.70f, 0.74f);
    static readonly Color FlashOn = new Color(0.96f, 0.90f, 0.42f);

    MinigamePuzzleKind _kind;
    int _w, _h;
    Action _onSolved, _onQuit;
    bool _busy;          // ignore input during flashes / solve animation

    // Maze
    bool[,] _wall;
    Vector2Int _start, _goal, _token;

    // BlockFill
    readonly List<Vector2Int> _path = new List<Vector2Int>();

    // PatternMatch
    readonly List<int> _sequence = new List<int>();
    int _inputIndex;

    void Awake()
    {
        if (cellButtons != null)
            for (int i = 0; i < cellButtons.Length; i++)
            {
                int idx = i;
                if (cellButtons[i] != null) cellButtons[i].onClick.AddListener(() => OnCell(idx));
            }

        if (resetButton != null) resetButton.onClick.AddListener(Restart);
        if (quitButton  != null) quitButton.onClick.AddListener(QuitOut);

        if (root != null) root.SetActive(false);
    }

    /// <summary>Opens the puzzle for a station. <paramref name="onSolved"/> fires on
    /// a win; <paramref name="onQuit"/> fires if the player backs out.</summary>
    public void Begin(MinigameStationDef def, Action onSolved, Action onQuit)
    {
        _kind = def.kind;
        _currentId = def.id;
        _onSolved = onSolved;
        _onQuit = onQuit;
        if (titleLabel != null) titleLabel.text = def.title;
        if (root != null) root.SetActive(true);
        Setup();
    }

    void Setup()
    {
        StopAllCoroutines();
        _busy = false;
        if (feedbackLabel != null) feedbackLabel.text = "";

        switch (_kind)
        {
            case MinigamePuzzleKind.BlockFill:    SetupFill();    break;
            case MinigamePuzzleKind.PatternMatch: SetupPattern(); break;
            default:                              SetupMaze();    break;
        }
    }

    void Restart() { if (!_busy || _kind != MinigamePuzzleKind.PatternMatch) Setup(); }

    void QuitOut()
    {
        Action cb = _onQuit;
        Cleanup();
        cb?.Invoke();
    }

    // -------------------------------------------------------------------------
    // Maze

    void SetupMaze()
    {
        MazeLayout m = OverworldPuzzleLibrary.GetMaze(_currentId);
        _w = Mathf.Min(m.width, Grid);
        _h = Mathf.Min(m.height, Grid);
        _wall = m.wall;
        _start = m.start;
        _goal = m.goal;
        _token = _start;
        if (instructionLabel != null)
            instructionLabel.text = "Click a neighbouring tile to move. Reach the orange goal.";
        RenderMaze();
    }

    void RenderMaze()
    {
        for (int y = 0; y < Grid; y++)
            for (int x = 0; x < Grid; x++)
            {
                int i = y * Grid + x;
                bool active = x < _w && y < _h;
                bool wall = active && _wall[y, x];
                SetCell(i, active, !wall && active);

                if (!active) continue;
                Color c = wall ? Wall : OpenCol;
                var p = new Vector2Int(x, y);
                if (p == _goal)  c = Goal;
                if (p == _token) c = Token;
                if (cellImages[i] != null) cellImages[i].color = c;
            }
    }

    void MazeClick(int x, int y)
    {
        if (x >= _w || y >= _h || _wall[y, x]) return;
        var p = new Vector2Int(x, y);
        if (Mathf.Abs(p.x - _token.x) + Mathf.Abs(p.y - _token.y) != 1) return; // must be adjacent
        _token = p;
        RenderMaze();
        if (_token == _goal) Win("Through the maze!");
    }

    // -------------------------------------------------------------------------
    // BlockFill

    void SetupFill()
    {
        Vector2Int size = OverworldPuzzleLibrary.GetFillSize(_currentId);
        _w = Mathf.Min(size.x, Grid);
        _h = Mathf.Min(size.y, Grid);
        _path.Clear();
        _path.Add(new Vector2Int(0, 0)); // start corner
        if (instructionLabel != null)
            instructionLabel.text = "Draw one path from the corner that fills every tile.";
        RenderFill();
    }

    void RenderFill()
    {
        var inPath = new HashSet<Vector2Int>(_path);
        Vector2Int head = _path[_path.Count - 1];
        for (int y = 0; y < Grid; y++)
            for (int x = 0; x < Grid; x++)
            {
                int i = y * Grid + x;
                bool active = x < _w && y < _h;
                SetCell(i, active, active);
                if (!active) continue;
                var p = new Vector2Int(x, y);
                Color c = OpenCol;
                if (inPath.Contains(p)) c = Visited;
                if (p == head) c = Token;
                if (cellImages[i] != null) cellImages[i].color = c;
            }
    }

    void FillClick(int x, int y)
    {
        if (x >= _w || y >= _h) return;
        var p = new Vector2Int(x, y);
        Vector2Int head = _path[_path.Count - 1];

        // Step back one if they click the previous cell (simple undo).
        if (_path.Count >= 2 && p == _path[_path.Count - 2])
        {
            _path.RemoveAt(_path.Count - 1);
            RenderFill();
            return;
        }

        bool adjacent = Mathf.Abs(p.x - head.x) + Mathf.Abs(p.y - head.y) == 1;
        if (!adjacent || _path.Contains(p)) return;

        _path.Add(p);
        RenderFill();
        if (_path.Count == _w * _h) Win("Every tile filled!");
    }

    // -------------------------------------------------------------------------
    // PatternMatch

    void SetupPattern()
    {
        _w = 3; _h = 3;
        _sequence.Clear();
        var rng = new System.Random();
        int len = 4;
        for (int i = 0; i < len; i++) _sequence.Add(rng.Next(_w * _h)); // index within 3×3 logical
        _inputIndex = 0;
        if (instructionLabel != null)
            instructionLabel.text = "Watch the sequence, then tap the tiles in the same order.";
        RenderPatternBase();
        StartCoroutine(FlashSequence());
    }

    void RenderPatternBase()
    {
        for (int y = 0; y < Grid; y++)
            for (int x = 0; x < Grid; x++)
            {
                int i = y * Grid + x;
                bool active = x < _w && y < _h;
                SetCell(i, active, active);
                if (active && cellImages[i] != null) cellImages[i].color = OpenCol;
            }
    }

    IEnumerator FlashSequence()
    {
        _busy = true;
        if (feedbackLabel != null) feedbackLabel.text = "Watch…";
        yield return new WaitForSeconds(0.5f);
        foreach (int logical in _sequence)
        {
            int i = LogicalToCell(logical);
            if (cellImages[i] != null) cellImages[i].color = FlashOn;
            yield return new WaitForSeconds(0.45f);
            if (cellImages[i] != null) cellImages[i].color = OpenCol;
            yield return new WaitForSeconds(0.18f);
        }
        if (feedbackLabel != null) feedbackLabel.text = "Your turn!";
        _inputIndex = 0;
        _busy = false;
    }

    void PatternClick(int x, int y)
    {
        if (_busy || x >= _w || y >= _h) return;
        int logical = y * _w + x;
        int i = y * Grid + x;

        if (logical == _sequence[_inputIndex])
        {
            StartCoroutine(Blip(i, true));
            _inputIndex++;
            if (_inputIndex >= _sequence.Count) Win("Sequence matched!");
        }
        else
        {
            if (feedbackLabel != null) feedbackLabel.text = "Not quite — watch again.";
            StartCoroutine(Blip(i, false));
            _inputIndex = 0;
            StartCoroutine(FlashSequence());
        }
    }

    IEnumerator Blip(int cell, bool good)
    {
        if (cellImages[cell] != null) cellImages[cell].color = good ? Token : Wall;
        yield return new WaitForSeconds(0.15f);
        if (cellImages[cell] != null) cellImages[cell].color = OpenCol;
    }

    static int LogicalToCell(int logical3x3)
    {
        int x = logical3x3 % 3, y = logical3x3 / 3;
        return y * Grid + x;
    }

    // -------------------------------------------------------------------------
    // Shared

    void OnCell(int idx)
    {
        if (_busy && _kind != MinigamePuzzleKind.PatternMatch) return;
        int x = idx % Grid, y = idx / Grid;
        switch (_kind)
        {
            case MinigamePuzzleKind.BlockFill:    FillClick(x, y);    break;
            case MinigamePuzzleKind.PatternMatch: PatternClick(x, y); break;
            default:                              MazeClick(x, y);    break;
        }
    }

    void SetCell(int i, bool visible, bool interactable)
    {
        if (cellImages[i] != null)
        {
            cellImages[i].enabled = visible;
            if (!visible) cellImages[i].color = Hidden;
        }
        if (cellButtons[i] != null) cellButtons[i].interactable = interactable;
    }

    void Win(string message)
    {
        _busy = true;
        if (feedbackLabel != null) feedbackLabel.text = "<color=#7CFC72>" + message + "</color>";
        StartCoroutine(WinAfter());
    }

    IEnumerator WinAfter()
    {
        yield return new WaitForSeconds(0.7f);
        Action cb = _onSolved;
        Cleanup();
        cb?.Invoke();
    }

    void Cleanup()
    {
        StopAllCoroutines();
        _busy = false;
        _onSolved = null;
        _onQuit = null;
        if (root != null) root.SetActive(false);
    }

    // Station id, used by the per-kind setups for library lookups.
    string _currentId;
}
