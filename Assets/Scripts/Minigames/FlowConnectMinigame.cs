using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// The Molo town gate (non-code Mini-Game 2): connect each pair of colored
/// transit hubs by dragging a path, with no two paths crossing. Must be solved
/// to advance — retries are free and the soft timer only dents the score
/// (reconciling the user's "needed to complete" with PRD's "fail = penalty").
/// Reuses the shared <see cref="MinigameResult"/> contract.
/// </summary>
public class FlowConnectMinigame : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject root;
    [SerializeField] private MinigameResultsPanel resultsPanel;
    [SerializeField] private TMP_Text   titleLabel;
    [SerializeField] private TMP_Text   feedbackLabel;
    [SerializeField] private FlowCell[] cells;        // Size×Size, row-major (y*Size + x)
    [SerializeField] private Button     resetButton;
    [SerializeField] private Image      timerFill;

    [Header("Timing")]
    [SerializeField] private float softTimerSeconds = 60f;

    public const int Size = 5;

    static readonly Color[] Palette =
    {
        new Color(0.90f, 0.30f, 0.25f),  // red
        new Color(0.25f, 0.55f, 0.90f),  // blue
        new Color(0.40f, 0.80f, 0.40f),  // green
        new Color(0.95f, 0.78f, 0.25f),  // gold
        new Color(0.75f, 0.45f, 0.85f),  // violet
        new Color(0.90f, 0.55f, 0.20f),  // orange
    };
    static readonly Color EmptyCell  = new Color(0.16f, 0.18f, 0.22f);
    static readonly Color GoodColor  = new Color(0.45f, 0.85f, 0.45f);
    static readonly Color WarnColor  = new Color(0.92f, 0.45f, 0.40f);
    static readonly Color NeutralCol = new Color(0.85f, 0.86f, 0.82f);

    Action<MinigameResult> _onDone;
    FlowConnectBoard _board;
    int   _active = -1;
    bool  _drawing;
    float _timer;
    float _timerStart;
    bool  _running;
    bool  _timedOut;

    // -------------------------------------------------------------------------

    void Awake()
    {
        if (resetButton != null) resetButton.onClick.AddListener(ResetBoard);
        BindCells();
        if (root != null) root.SetActive(false);
    }

    void Update()
    {
        // A drag ends when the pointer is released anywhere.
        if (_drawing && (Pointer.current == null || !Pointer.current.press.isPressed))
            _drawing = false;

        if (!_running || _timedOut) return;

        _timer -= Time.deltaTime;
        if (timerFill != null)
            timerFill.fillAmount = Mathf.Clamp01(_timer / _timerStart);

        if (_timer <= 0f)
        {
            _timedOut = true;
            if (feedbackLabel != null)
            {
                feedbackLabel.text  = "Out of time — finish the links to continue (score dented).";
                feedbackLabel.color = WarnColor;
            }
        }
    }

    // -------------------------------------------------------------------------

    /// <summary>Opens the puzzle; <paramref name="onDone"/> fires once it's solved.</summary>
    public void Show(int seed, Action<MinigameResult> onDone)
        => Show(null, 1, seed, onDone);

    public void Show(MinigameStationDef station, int levelIndex, int seed,
                     Action<MinigameResult> onDone)
    {
        _onDone   = onDone;
        _running  = true;
        _timedOut = false;
        _active   = -1;
        _drawing  = false;

        FlowConnectLayout layout = FlowConnectLayouts.Generate(levelIndex, seed);
        _board = new FlowConnectBoard(layout.Width, layout.Height, layout.Pairs);
        int expectedMoves = 0;
        foreach (Vector2Int[] path in layout.Solution) expectedMoves += path.Length;
        _timerStart = OverworldPuzzleTuning.SoftTimerSeconds(expectedMoves);
        _timer = _timerStart;

        BindCells();
        if (titleLabel != null) titleLabel.text = station != null ? station.title : "FLOW CONNECT";
        if (feedbackLabel != null)
        {
            feedbackLabel.text  = "Drag from a hub to its matching hub.";
            feedbackLabel.color = NeutralCol;
        }

        Refresh();
        if (root != null) root.SetActive(true);
    }

    // -------------------------------------------------------------------------
    // Pointer routing (from FlowCell)

    public void CellDown(int x, int y)
    {
        if (!_running || _board == null) return;

        var cell = new Vector2Int(x, y);
        int ep = _board.EndpointColor(cell);
        if (ep >= 0)
        {
            _active  = ep;
            _drawing = true;
            _board.Start(ep, cell);
            Refresh();
        }
        else
        {
            _active  = -1;
            _drawing = false;
        }
    }

    public void CellEnter(int x, int y)
    {
        if (!_running || !_drawing || _active < 0 || _board == null) return;

        if (_board.Extend(_active, new Vector2Int(x, y)))
        {
            Refresh();
            if (_board.IsSolved())
                Finish();
        }
    }

    // -------------------------------------------------------------------------

    void ResetBoard()
    {
        if (!_running || _board == null) return;
        _board.ClearAll();
        _active  = -1;
        _drawing = false;
        Refresh();
        if (feedbackLabel != null)
        {
            feedbackLabel.text  = "Cleared — start again from any hub.";
            feedbackLabel.color = NeutralCol;
        }
    }

    void BindCells()
    {
        if (cells == null) return;
        foreach (FlowCell c in cells)
            if (c != null) c.owner = this;
    }

    void Refresh()
    {
        if (cells == null || _board == null) return;

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                int i = y * Size + x;
                if (i >= cells.Length || cells[i] == null) continue;

                var cell  = new Vector2Int(x, y);
                int owner = _board.Owner(cell);
                int ep    = _board.EndpointColor(cell);

                if (cells[i].background != null)
                    cells[i].background.color = owner >= 0 ? Dim(Palette[owner % Palette.Length]) : EmptyCell;

                if (cells[i].dot != null)
                {
                    bool isHub = ep >= 0;
                    cells[i].dot.enabled = isHub;
                    if (isHub) cells[i].dot.color = Palette[ep % Palette.Length];
                }
            }
        }
    }

    static Color Dim(Color c) => new Color(c.r * 0.55f, c.g * 0.55f, c.b * 0.55f, 1f);

    void Finish()
    {
        _running = false;
        if (feedbackLabel != null) { feedbackLabel.text = "All hubs linked!"; feedbackLabel.color = GoodColor; }

        var result = new MinigameResult
        {
            TimedOut = _timedOut,
            Mistakes = 0,
            Score    = _timedOut ? 60 : 100,
        };

        StartCoroutine(FinishAfter(result));
    }

    System.Collections.IEnumerator FinishAfter(MinigameResult result)
    {
        yield return new WaitForSecondsRealtime(0.7f);
        if (root != null) root.SetActive(false);
        Action<MinigameResult> done = _onDone;
        _onDone = null;
        done?.Invoke(result);
    }
}
