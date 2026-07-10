using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared overlay for Maze, Block Fill, Pattern Memory, and the distinct
/// rotate-the-routes Color Connect puzzle. Layouts are generated once per level
/// visit; Reset restores that layout rather than rerolling it.
/// </summary>
public class GridPuzzleMinigame : MonoBehaviour
{
    public const int Grid = 8;

    [Header("References")]
    [SerializeField] GameObject root;
    [SerializeField] TMP_Text titleLabel;
    [SerializeField] TMP_Text instructionLabel;
    [SerializeField] TMP_Text feedbackLabel;
    [SerializeField] Image[] cellImages;
    [SerializeField] Button[] cellButtons;
    [SerializeField] GridPuzzleCell[] pointerCells;
    [SerializeField] TMP_Text[] cellLabels;
    [SerializeField] Image[] northArms;
    [SerializeField] Image[] eastArms;
    [SerializeField] Image[] southArms;
    [SerializeField] Image[] westArms;
    [SerializeField] Image[] endpointDots;
    [SerializeField] GridLayoutGroup gridLayout;
    [SerializeField] Button resetButton;
    [SerializeField] Button replayButton;
    [SerializeField] Button quitButton;
    [SerializeField] Image timerFill;

    static readonly Color Hidden = new Color(0f, 0f, 0f, 0f);
    static readonly Color Wall = new Color(0.12f, 0.13f, 0.16f);
    static readonly Color Open = new Color(0.24f, 0.26f, 0.32f);
    static readonly Color Token = new Color(0.30f, 0.74f, 0.42f);
    static readonly Color Goal = new Color(0.95f, 0.65f, 0.15f);
    static readonly Color Visited = new Color(0.22f, 0.70f, 0.74f);
    static readonly Color Flash = new Color(0.96f, 0.90f, 0.42f);
    static readonly Color Bad = new Color(0.92f, 0.35f, 0.32f);
    static readonly Color[] RouteColors =
    {
        new Color(0.90f, 0.30f, 0.25f), new Color(0.25f, 0.55f, 0.90f),
        new Color(0.40f, 0.80f, 0.40f), new Color(0.95f, 0.78f, 0.25f),
        new Color(0.75f, 0.45f, 0.85f),
    };

    MinigameStationDef _def;
    MinigamePuzzleKind _kind;
    int _levelIndex;
    int _seed;
    int _w, _h;
    Action _onSolved, _onQuit;
    bool _busy, _running, _timedOut;
    bool _dragging, _dragMoved, _ignoreNextClick;
    int _mistakes;
    float _timer, _timerStart;

    MazeLayout _mazeLayout;
    Vector2Int _token;
    FillLayout _fillLayout;
    readonly List<Vector2Int> _fillPath = new List<Vector2Int>();
    RouteRotationLayout _routeLayout;
    RouteRotationBoard _routeBoard;
    readonly List<int> _sequence = new List<int>();
    int _inputIndex;
    float _cueSeconds;

    void Awake()
    {
        if (cellButtons != null)
            for (int i = 0; i < cellButtons.Length; i++)
            {
                int index = i;
                if (cellButtons[i] != null) cellButtons[i].onClick.AddListener(() => OnCell(index));
            }
        if (pointerCells != null)
            for (int i = 0; i < pointerCells.Length; i++)
                if (pointerCells[i] != null)
                {
                    pointerCells[i].index = i;
                    pointerCells[i].owner = this;
                }
        if (resetButton != null) resetButton.onClick.AddListener(Restart);
        if (replayButton != null) replayButton.onClick.AddListener(ReplayPattern);
        if (quitButton != null) quitButton.onClick.AddListener(QuitOut);
        if (root != null) root.SetActive(false);
    }

    void Update()
    {
        if (!_running || _timedOut || (_kind == MinigamePuzzleKind.PatternMatch && _busy)) return;
        _timer -= Time.unscaledDeltaTime;
        if (timerFill != null) timerFill.fillAmount = Mathf.Clamp01(_timer / _timerStart);
        if (_timer > 0f) return;
        _timer = 0f;
        _timedOut = true;
        if (feedbackLabel != null)
            feedbackLabel.text = "Time bonus expired — keep going; your progress is safe.";
    }

    public void Begin(MinigameStationDef def, int levelIndex, int seed, Action onSolved, Action onQuit)
    {
        _def = def;
        _kind = def.kind;
        _levelIndex = Mathf.Clamp(levelIndex, 0, 5);
        _seed = seed;
        _onSolved = onSolved;
        _onQuit = onQuit;
        _mistakes = 0;
        _timedOut = false;
        _running = true;
        if (titleLabel != null) titleLabel.text = def.title;

        GenerateLayout();
        Restart();
        _timerStart = OverworldPuzzleTuning.SoftTimerSeconds(ExpectedMoves());
        _timer = _timerStart;
        if (timerFill != null) timerFill.fillAmount = 1f;
        if (root != null) root.SetActive(true);
    }

    void GenerateLayout()
    {
        switch (_kind)
        {
            case MinigamePuzzleKind.BlockFill:
                _fillLayout = OverworldPuzzleGenerator.GenerateFill(_levelIndex, _seed);
                _w = _fillLayout.width; _h = _fillLayout.height;
                break;
            case MinigamePuzzleKind.PatternMatch:
                _w = 3; _h = 3;
                var random = new System.Random(_seed);
                _sequence.Clear();
                for (int i = 0; i < OverworldPuzzleTuning.PatternLength(_levelIndex); i++)
                    _sequence.Add(random.Next(9));
                _cueSeconds = OverworldPuzzleTuning.PatternCueSeconds(_levelIndex);
                break;
            case MinigamePuzzleKind.ColorConnect:
                _routeLayout = RouteRotationGenerator.Generate(_levelIndex, _seed);
                _w = _routeLayout.width; _h = _routeLayout.height;
                break;
            default:
                _mazeLayout = OverworldPuzzleGenerator.GenerateMaze(_levelIndex, _seed);
                _w = _mazeLayout.width; _h = _mazeLayout.height;
                break;
        }
        ConfigureGrid();
    }

    void ConfigureGrid()
    {
        if (gridLayout == null) return;
        int size = Mathf.Max(_w, _h);
        gridLayout.constraintCount = size;
        float spacing = size >= 8 ? 4f : 6f;
        float cell = (540f - spacing * (size - 1)) / size;
        gridLayout.spacing = new Vector2(spacing, spacing);
        gridLayout.cellSize = new Vector2(cell, cell);
    }

    void Restart()
    {
        if (!_running) return;
        StopAllCoroutines();
        _busy = false;
        _dragging = false;
        _dragMoved = false;
        _ignoreNextClick = false;
        if (feedbackLabel != null) feedbackLabel.text = "";
        if (replayButton != null) replayButton.gameObject.SetActive(_kind == MinigamePuzzleKind.PatternMatch);

        switch (_kind)
        {
            case MinigamePuzzleKind.BlockFill: SetupFill(); break;
            case MinigamePuzzleKind.PatternMatch: SetupPattern(); break;
            case MinigamePuzzleKind.ColorConnect: SetupRotation(); break;
            default: SetupMaze(); break;
        }
    }

    void SetupMaze()
    {
        _token = _mazeLayout.start;
        if (instructionLabel != null)
            instructionLabel.text = "Drag from the green tile through open neighbours, or click one step at a time.";
        RenderMaze();
    }

    void RenderMaze()
    {
        ClearRouteVisuals();
        for (int y = 0; y < Grid; y++)
            for (int x = 0; x < Grid; x++)
            {
                int i = y * Grid + x;
                bool visible = x < _w && y < _h;
                bool wall = visible && _mazeLayout.wall[y, x];
                SetCell(i, visible, visible && !wall);
                if (!visible) continue;
                Vector2Int p = new Vector2Int(x, y);
                cellImages[i].color = wall ? Wall : p == _token ? Token : p == _mazeLayout.goal ? Goal : Open;
                SetLabel(i, p == _mazeLayout.start ? "S" : p == _mazeLayout.goal ? "G" : "");
            }
    }

    bool MazeStep(int x, int y)
    {
        Vector2Int next = new Vector2Int(x, y);
        bool valid = x >= 0 && y >= 0 && x < _w && y < _h && !_mazeLayout.wall[y, x] &&
                     Manhattan(next, _token) == 1;
        if (!valid)
        {
            _mistakes++;
            StartCoroutine(FlashBad(Index(x, y)));
            return false;
        }
        _token = next;
        RenderMaze();
        if (_token == _mazeLayout.goal) Win("Route cleared!");
        return true;
    }

    void SetupFill()
    {
        _fillPath.Clear();
        _fillPath.Add(_fillLayout.solution[0]);
        if (instructionLabel != null)
            instructionLabel.text = "Drag one continuous path through every lit tile. Drag backward to undo.";
        RenderFill();
    }

    void RenderFill()
    {
        ClearRouteVisuals();
        var visited = new HashSet<Vector2Int>(_fillPath);
        Vector2Int head = _fillPath[_fillPath.Count - 1];
        for (int y = 0; y < Grid; y++)
            for (int x = 0; x < Grid; x++)
            {
                int i = y * Grid + x;
                bool inBoard = x < _w && y < _h;
                bool active = inBoard && _fillLayout.active[y, x];
                SetCell(i, inBoard, active);
                if (!inBoard) continue;
                if (!active)
                {
                    cellImages[i].color = Wall;
                    SetLabel(i, "");
                    continue;
                }
                Vector2Int p = new Vector2Int(x, y);
                cellImages[i].color = p == head ? Token : visited.Contains(p) ? Visited : Open;
                SetLabel(i, p == _fillLayout.solution[0] ? "S" : "");
            }
    }

    bool FillStep(int x, int y)
    {
        if (x < 0 || y < 0 || x >= _w || y >= _h || !_fillLayout.active[y, x]) return false;
        Vector2Int next = new Vector2Int(x, y);
        Vector2Int head = _fillPath[_fillPath.Count - 1];
        if (_fillPath.Count >= 2 && next == _fillPath[_fillPath.Count - 2])
        {
            _fillPath.RemoveAt(_fillPath.Count - 1);
            RenderFill();
            return true;
        }
        if (Manhattan(next, head) != 1 || _fillPath.Contains(next))
        {
            _mistakes++;
            StartCoroutine(FlashBad(Index(x, y)));
            return false;
        }
        _fillPath.Add(next);
        RenderFill();
        if (_fillPath.Count == _fillLayout.solution.Length) Win("Every tile filled!");
        return true;
    }

    void SetupRotation()
    {
        _routeBoard = new RouteRotationBoard(_routeLayout);
        if (instructionLabel != null)
            instructionLabel.text = "Click route tiles to rotate them. Right-click rotates backward; connect every matching symbol.";
        RenderRotation();
    }

    void RenderRotation()
    {
        for (int y = 0; y < Grid; y++)
            for (int x = 0; x < Grid; x++)
            {
                int i = y * Grid + x;
                bool inBoard = x < _w && y < _h;
                bool visible = inBoard && _routeBoard.ColorAt(x, y) >= 0;
                SetCell(i, inBoard, visible);
                SetLabel(i, "");
                if (!inBoard) { SetArms(i, 0, Color.clear, false); continue; }
                if (!visible)
                {
                    cellImages[i].color = Wall;
                    SetArms(i, 0, Color.clear, false);
                    continue;
                }
                int color = _routeBoard.ColorAt(x, y);
                Color tint = RouteColors[color % RouteColors.Length];
                cellImages[i].color = new Color(tint.r * 0.28f, tint.g * 0.28f, tint.b * 0.28f, 1f);
                SetArms(i, _routeBoard.MaskAt(x, y), tint, _routeBoard.IsEndpoint(x, y));
                if (_routeBoard.IsEndpoint(x, y)) SetLabel(i, ((char)('A' + color)).ToString());
            }
    }

    void RotateCell(int x, int y, int delta)
    {
        if (x < 0 || y < 0 || x >= _w || y >= _h || _routeBoard.ColorAt(x, y) < 0) return;
        _routeBoard.Rotate(x, y, delta);
        RenderRotation();
        if (_routeBoard.IsSolved()) Win("Every route connected!");
    }

    void SetupPattern()
    {
        _inputIndex = 0;
        if (instructionLabel != null)
            instructionLabel.text = "Watch the numbered tiles, then repeat the route. Replay is always available.";
        RenderPatternBase();
        StartCoroutine(FlashSequence());
    }

    void RenderPatternBase()
    {
        ClearRouteVisuals();
        for (int y = 0; y < Grid; y++)
            for (int x = 0; x < Grid; x++)
            {
                int i = y * Grid + x;
                bool visible = x < 3 && y < 3;
                SetCell(i, visible, visible);
                if (!visible) continue;
                cellImages[i].color = Open;
                SetLabel(i, PatternCellText(y * 3 + x));
            }
    }

    IEnumerator FlashSequence()
    {
        _busy = true;
        if (feedbackLabel != null) feedbackLabel.text = "Watch…";
        yield return new WaitForSecondsRealtime(0.35f);
        foreach (int logical in _sequence)
        {
            int i = LogicalToCell(logical);
            cellImages[i].color = Flash;
            yield return new WaitForSecondsRealtime(_cueSeconds);
            cellImages[i].color = Open;
            yield return new WaitForSecondsRealtime(0.12f);
        }
        _inputIndex = 0;
        if (feedbackLabel != null) feedbackLabel.text = "Your turn!";
        _busy = false;
    }

    void ReplayPattern()
    {
        if (_kind != MinigamePuzzleKind.PatternMatch || !_running) return;
        StopAllCoroutines();
        RenderPatternBase();
        StartCoroutine(FlashSequence());
    }

    void PatternClick(int x, int y)
    {
        if (_busy || x >= 3 || y >= 3) return;
        int logical = y * 3 + x;
        int i = y * Grid + x;
        if (logical == _sequence[_inputIndex])
        {
            StartCoroutine(Blip(i, true));
            _inputIndex++;
            if (_inputIndex >= _sequence.Count) Win("Sequence matched!");
        }
        else
        {
            _mistakes++;
            if (feedbackLabel != null) feedbackLabel.text = "That tile was out of sequence — watch again.";
            StartCoroutine(Blip(i, false));
            ReplayPattern();
        }
    }

    IEnumerator Blip(int index, bool good)
    {
        cellImages[index].color = good ? Token : Bad;
        yield return new WaitForSecondsRealtime(0.15f);
        if (index >= 0 && index < cellImages.Length && cellImages[index] != null) cellImages[index].color = Open;
    }

    public void CellPointerDown(int index)
    {
        if (_busy || !_running) return;
        int x = index % Grid, y = index / Grid;
        bool atHead = _kind == MinigamePuzzleKind.Maze
            ? new Vector2Int(x, y) == _token
            : _kind == MinigamePuzzleKind.BlockFill && new Vector2Int(x, y) == _fillPath[_fillPath.Count - 1];
        _dragging = atHead;
        _dragMoved = false;
    }

    public void CellPointerEnter(int index)
    {
        if (!_dragging || _busy || !_running) return;
        int x = index % Grid, y = index / Grid;
        bool changed = _kind == MinigamePuzzleKind.Maze ? MazeStep(x, y) : FillStep(x, y);
        _dragMoved |= changed;
    }

    public void CellPointerUp(int index)
    {
        if (_dragMoved) _ignoreNextClick = true;
        _dragging = false;
    }

    public void CellSecondaryClick(int index)
    {
        if (_kind != MinigamePuzzleKind.ColorConnect || _busy) return;
        RotateCell(index % Grid, index / Grid, -1);
    }

    void OnCell(int index)
    {
        if (_ignoreNextClick) { _ignoreNextClick = false; return; }
        if (_busy || !_running) return;
        int x = index % Grid, y = index / Grid;
        switch (_kind)
        {
            case MinigamePuzzleKind.BlockFill: FillStep(x, y); break;
            case MinigamePuzzleKind.PatternMatch: PatternClick(x, y); break;
            case MinigamePuzzleKind.ColorConnect: RotateCell(x, y, 1); break;
            default: MazeStep(x, y); break;
        }
    }

    void SetCell(int index, bool visible, bool interactable)
    {
        if (index < 0 || cellImages == null || index >= cellImages.Length) return;
        if (cellImages[index] != null)
        {
            cellImages[index].gameObject.SetActive(visible);
            cellImages[index].enabled = visible;
            if (!visible) cellImages[index].color = Hidden;
        }
        if (cellButtons != null && index < cellButtons.Length && cellButtons[index] != null)
            cellButtons[index].interactable = interactable;
    }

    void SetLabel(int index, string text)
    {
        if (cellLabels == null || index < 0 || index >= cellLabels.Length || cellLabels[index] == null) return;
        cellLabels[index].text = text;
    }

    void ClearRouteVisuals()
    {
        for (int i = 0; i < Grid * Grid; i++) SetArms(i, 0, Color.clear, false);
    }

    void SetArms(int index, int mask, Color color, bool endpoint)
    {
        SetArm(northArms, index, (mask & RouteRotationBoard.North) != 0, color);
        SetArm(eastArms, index, (mask & RouteRotationBoard.East) != 0, color);
        SetArm(southArms, index, (mask & RouteRotationBoard.South) != 0, color);
        SetArm(westArms, index, (mask & RouteRotationBoard.West) != 0, color);
        if (endpointDots != null && index >= 0 && index < endpointDots.Length && endpointDots[index] != null)
        {
            endpointDots[index].gameObject.SetActive(endpoint);
            endpointDots[index].color = color;
        }
    }

    static void SetArm(Image[] arms, int index, bool active, Color color)
    {
        if (arms == null || index < 0 || index >= arms.Length || arms[index] == null) return;
        arms[index].gameObject.SetActive(active);
        arms[index].color = color;
    }

    IEnumerator FlashBad(int index)
    {
        if (index < 0 || cellImages == null || index >= cellImages.Length || cellImages[index] == null) yield break;
        Color before = cellImages[index].color;
        cellImages[index].color = Bad;
        yield return new WaitForSecondsRealtime(0.12f);
        if (cellImages[index] != null) cellImages[index].color = before;
    }

    void Win(string message)
    {
        if (_busy || !_running) return;
        _busy = true;
        int score = Mathf.Max(10, 100 - _mistakes * 5 - (_timedOut ? 25 : 0));
        if (feedbackLabel != null) feedbackLabel.text = $"<color=#7CFC72>{message}  Score {score}</color>";
        StartCoroutine(WinAfter());
    }

    IEnumerator WinAfter()
    {
        yield return new WaitForSecondsRealtime(0.7f);
        Action callback = _onSolved;
        Cleanup();
        callback?.Invoke();
    }

    void QuitOut()
    {
        Action callback = _onQuit;
        Cleanup();
        callback?.Invoke();
    }

    void Cleanup()
    {
        StopAllCoroutines();
        _busy = false;
        _running = false;
        _onSolved = null;
        _onQuit = null;
        if (root != null) root.SetActive(false);
    }

    int ExpectedMoves()
    {
        switch (_kind)
        {
            case MinigamePuzzleKind.BlockFill: return _fillLayout.solution.Length;
            case MinigamePuzzleKind.PatternMatch: return _sequence.Count * 2;
            case MinigamePuzzleKind.ColorConnect: return _w * OverworldPuzzleTuning.FlowPairs(_levelIndex);
            default: return _w * 2;
        }
    }

    string PatternCellText(int logical)
    {
        string[] words = _def != null && _def.id != null && _def.id.StartsWith("sj_")
            ? new[] { "SUN", "SHIP", "WALL", "HORSE", "FLAG", "DRUM", "SWORD", "GATE", "STONE" }
            : new[] { "BOAT", "GOLD", "POT", "IRON", "RICE", "NET", "ROPE", "FISH", "OAR" };
        return $"{logical + 1}\n{words[logical]}";
    }

    int Index(int x, int y) => x >= 0 && y >= 0 && x < Grid && y < Grid ? y * Grid + x : -1;
    static int Manhattan(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    static int LogicalToCell(int logical) => (logical / 3) * Grid + logical % 3;
}
