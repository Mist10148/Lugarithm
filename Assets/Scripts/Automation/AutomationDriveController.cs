using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Orchestrates an Automation Mode puzzle: builds the iso grid world for the
/// selected level, runs the Block/Code workspace state machine
/// (Editing → Running → Result), and routes execution events to the console,
/// monitor, and highlights. Runs standalone in the editor too.
/// </summary>
public class AutomationDriveController : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector References

    [Header("World")]
    [SerializeField] private Camera           worldCamera;
    [SerializeField] private GridWorldView    worldView;
    [SerializeField] private JeepneyAgentView agentView;

    [Header("Execution")]
    [SerializeField] private ExecutionController exec;

    [Header("Workspace")]
    [SerializeField] private TMP_Text   goalLabel;
    [SerializeField] private GameObject blockPanel;
    [SerializeField] private GameObject codePanel;
    [SerializeField] private Button     blocksTabButton;
    [SerializeField] private Button     codeTabButton;
    [SerializeField] private BlockCanvasController  blockCanvas;
    [SerializeField] private BlockPaletteController palette;
    [SerializeField] private CodeEditorController   codeEditor;

    [Header("CodeDrive overlay (optional)")]
    [Tooltip("Derive the tile grid from the level's manual route instead of auto.gridMap.")]
    [SerializeField] private bool       deriveGridFromRoute;
    [SerializeField] private Button     workspaceToggleButton;
    [SerializeField] private GameObject workspaceRoot;
    [SerializeField] private Button     readmeButton;
    [SerializeField] private GameObject readmePanel;
    [SerializeField] private Button     readmeCloseButton;

    [Header("Control Bar")]
    [SerializeField] private Button runButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button speed1Button;
    [SerializeField] private Button speed2Button;
    [SerializeField] private Button speed5Button;

    [Header("Readouts")]
    [SerializeField] private ConsoleController      console;
    [SerializeField] private StateMonitorController monitor;
    [SerializeField] private AutomationResultsPanel results;

    // -------------------------------------------------------------------------

    LevelDefinition _level;
    AutomationPuzzleDefinition _def;
    int  _levelIndex;
    bool _codeTabActive;
    bool _lastRunWasCode;
    int  _runCount;
    int  _lastExecutedLine;
    float _startTime;
    bool  _won;

    void Start()
    {
        // Resolve level (Tutorial fallback for direct editor play).
        _levelIndex = GameManager.Instance != null ? GameManager.Instance.SelectedLevelIndex : 0;
        _level = LevelLibrary.Get(_levelIndex);
        if (!_level.hasContent)
        {
            _levelIndex = 0;
            _level = LevelLibrary.Get(0);
        }
        _def = _level.auto;

        if (GameManager.Instance != null)
            GameManager.Instance.PendingCurrency = 0;

        // World — either the authored puzzle grid, or one derived from the
        // manual route so this scene mirrors the manual drive on tiles.
        string[] gridMap   = _def.gridMap;
        int      startFacing = _def.startFacing;
        if (deriveGridFromRoute && _level.manual != null &&
            _level.manual.waypoints != null && _level.manual.waypoints.Length >= 2)
        {
            RouteToGrid.Result derived = RouteToGrid.FromManualRoute(_level.manual);
            gridMap     = derived.Map;
            startFacing = derived.StartFacing;
        }

        GridModel grid = GridModel.Parse(gridMap, out List<string> mapErrors);
        foreach (string problem in mapErrors)
            if (console != null) console.Error("map: " + problem);

        var sim = new AgentSim(grid, _level.fares, startFacing);

        if (worldView != null)
        {
            worldView.Build(grid);
            worldView.FrameCamera(worldCamera);
        }
        if (agentView != null)
            agentView.Init(worldView, grid.StartPos, sim.Facing);

        if (exec != null)
        {
            exec.Init(grid, sim, agentView, worldView, _def, startFacing);
            exec.OnStepDone     += HandleStepDone;
            exec.OnRuntimeError += HandleRuntimeError;
            exec.OnFinished     += HandleFinished;
            exec.OnWorldReset   += HandleWorldReset;
        }

        // Workspace
        if (goalLabel != null) goalLabel.text = _def.goalText;

        if (blockCanvas != null) blockCanvas.Init(_def.allowedQueries, console);
        if (palette     != null) palette.Init(_def.allowedBlocks, blockCanvas);
        if (codeEditor  != null) codeEditor.SetScaffold(_def.codeScaffold);

        bool blockMode = SaveSystem.Current.settings.blockMode;
        SetTab(codeActive: !blockMode);

        if (blocksTabButton != null) blocksTabButton.onClick.AddListener(() => SetTab(false));
        if (codeTabButton   != null) codeTabButton.onClick.AddListener(() => SetTab(true));

        // CodeDrive overlays: the workspace toggles visibility (it never switches
        // editor type — that's settings-only); the README panel opens on demand.
        if (workspaceToggleButton != null && workspaceRoot != null)
            workspaceToggleButton.onClick.AddListener(() =>
                workspaceRoot.SetActive(!workspaceRoot.activeSelf));

        if (readmePanel != null) readmePanel.SetActive(false);
        if (readmeButton != null && readmePanel != null)
            readmeButton.onClick.AddListener(() =>
                readmePanel.SetActive(!readmePanel.activeSelf));
        if (readmeCloseButton != null && readmePanel != null)
            readmeCloseButton.onClick.AddListener(() => readmePanel.SetActive(false));

        // Control bar
        if (runButton    != null) runButton.onClick.AddListener(OnRun);
        if (pauseButton  != null) pauseButton.onClick.AddListener(OnPause);
        if (resetButton  != null) resetButton.onClick.AddListener(OnReset);
        if (speed1Button != null) speed1Button.onClick.AddListener(() => SetSpeed(1f));
        if (speed2Button != null) speed2Button.onClick.AddListener(() => SetSpeed(2f));
        if (speed5Button != null) speed5Button.onClick.AddListener(() => SetSpeed(5f));

        if (console != null)
        {
            console.Info($"{_level.displayName} — {(_def.allowedQueries.Length > 0 ? "conditionals unlocked" : "sequencing")}");
            console.Info("write a program, then press RUN");
        }
        if (monitor != null) monitor.ShowIdle();

        _startTime = Time.time;
    }

    // -------------------------------------------------------------------------
    // Tabs

    void SetTab(bool codeActive)
    {
        _codeTabActive = codeActive;

        if (blockPanel != null) blockPanel.SetActive(!codeActive);
        if (codePanel  != null) codePanel.SetActive(codeActive);

        // Active tab gets the accent tint.
        TintTab(blocksTabButton, !codeActive);
        TintTab(codeTabButton,   codeActive);
    }

    static void TintTab(Button button, bool active)
    {
        if (button == null) return;
        var face = button.targetGraphic as Image;
        if (face != null)
            face.color = active
                ? new Color(0.85f, 0.55f, 0.12f)
                : new Color(0.18f, 0.22f, 0.30f);
    }

    // -------------------------------------------------------------------------
    // Control bar

    void OnRun()
    {
        if (_won || exec == null) return;

        // RUN while paused = resume.
        if (exec.State == ExecutionController.ExecState.Paused)
        {
            exec.TogglePause();
            return;
        }

        List<LangError> errors;
        ProgramNode program = _codeTabActive
            ? codeEditor.BuildProgram(out errors)
            : blockCanvas.BuildProgram(out errors);

        if (errors.Count > 0)
        {
            if (console != null)
                foreach (LangError error in errors)
                    console.Error(error.ToString());
            return;
        }

        if (program.Statements.Count == 0)
        {
            if (console != null) console.Warn("the program is empty — add some commands first.");
            return;
        }

        _lastRunWasCode = _codeTabActive;
        _runCount++;
        _lastExecutedLine = 0;

        if (console != null)
        {
            console.Clear();
            console.Info($"run #{_runCount} started…");
        }

        exec.Run(program);
    }

    void OnPause()
    {
        if (exec == null) return;
        exec.TogglePause();
        if (console != null && exec.State == ExecutionController.ExecState.Paused)
            console.Info("paused.");
    }

    void OnReset()
    {
        if (exec == null || _won) return;
        exec.ResetWorld();
        if (console != null) console.Info("world reset.");
    }

    void SetSpeed(float speed)
    {
        if (exec != null) exec.SetSpeed(speed);
        if (console != null) console.Info($"speed ×{speed:0}");
    }

    // -------------------------------------------------------------------------
    // Execution events

    void HandleStepDone(AgentActionResult result, StepResult step)
    {
        _lastExecutedLine = step.Node != null ? step.Node.Line : 0;

        if (console != null)
        {
            string prefix = _lastExecutedLine > 0 ? $"line {_lastExecutedLine}: " : "";
            console.Info(prefix + result.Action + "()");
            if (!string.IsNullOrEmpty(result.Warning))
                console.Warn(result.Warning);
            if (result.FareCollected > 0)
                console.Info($"   fare collected: ₱{result.FareCollected}");
        }

        if (monitor != null && exec != null)
            monitor.Refresh(exec.Sim, _lastExecutedLine);

        if (!_codeTabActive && blockCanvas != null && step.Node != null)
            blockCanvas.HighlightExecuting(step.Node.SourceRef);
    }

    void HandleRuntimeError(LangError error)
    {
        if (console != null) console.Error(error.ToString());
    }

    void HandleWorldReset()
    {
        if (monitor != null) monitor.ShowIdle();
    }

    void HandleFinished(bool win)
    {
        if (win)
        {
            _won = true;
            ShowResults();
            return;
        }

        if (console != null && exec != null)
        {
            string gap = exec.Sim.DescribeGoalGap(_def);
            console.Warn(gap ?? "the program ended without reaching the goal.");
            console.Info("edit your program and press RUN to try again.");
        }
    }

    // -------------------------------------------------------------------------
    // Results

    void ShowResults()
    {
        AgentSim sim = exec.Sim;
        float elapsed = Time.time - _startTime;
        int retries = Mathf.Max(0, _runCount - 1);

        int score = ScoreCalculator.AutomationScore(
            sim.StepsUsed, _def.parSteps, elapsed, _def.softTimerSeconds, retries, _lastRunWasCode);

        int earned = sim.FaresCollected + ScoreCalculator.CurrencyFor(score);
        if (GameManager.Instance != null)
            GameManager.Instance.PendingCurrency += earned;

        string playerSolution = _lastRunWasCode
            ? codeEditor.Source
            : blockCanvas.ToSourceText();

        int minutes = (int)(elapsed / 60f);
        int seconds = (int)(elapsed % 60f);
        string stats = $"SCORE {score}   ·   steps {sim.StepsUsed} (par {_def.parSteps})   ·   " +
                       $"time {minutes:0}:{seconds:00}   ·   retries {retries}   ·   earned ₱{earned}" +
                       (_lastRunWasCode ? "   ·   CODE ×1.5" : "");

        if (console != null) console.Info("goal complete!");

        if (results != null)
        {
            results.Show(
                $"PUZZLE SOLVED  —  {_level.displayName}",
                playerSolution, _def.optimalSolutionText, stats,
                onContinue: () =>
                {
                    if (GameManager.Instance != null)
                        GameManager.Instance.CompleteLevel(_levelIndex, score);
                    LoadScene("LevelSelect");
                },
                onReplay: () => LoadScene(SceneManager.GetActiveScene().name));
        }
        else
        {
            LoadScene("LevelSelect");
        }
    }

    void LoadScene(string sceneName)
    {
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.TransitionTo(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }
}
