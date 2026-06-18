using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Code-based breakdown repair, reframed as a maze you escape by writing an
/// algorithm — the placeholder for the "code" branch of the breakdown roll.
/// A small perfect maze is generated per seed; the player solves it in the
/// <b>code editor or the block canvas</b> depending on the Block/Code setting,
/// then presses RUN. The shared Automation pipeline
/// (<see cref="AgentSim"/> + <see cref="ExecutionController"/> + interpreter)
/// drives the agent through the grid; reaching the exit (D) repairs the jeepney.
///
/// Drop-in replacement for CodeFixMinigame: same
/// <c>Show(BreakdownFault, seed, Action&lt;MinigameResult&gt;)</c> signature, and
/// like every minigame the soft timer only dents the score — the run always
/// continues (PRD §5.4).
/// </summary>
public class MazeRepairMinigame : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Labels")]
    [SerializeField] private TMP_Text titleLabel;
    [SerializeField] private TMP_Text goalLabel;
    [SerializeField] private TMP_Text feedbackLabel;
    [SerializeField] private TMP_Text timerLabel;

    [Header("Visual maze (iso grid rendered to a RenderTexture)")]
    [SerializeField] private GridWorldView    worldView;
    [SerializeField] private JeepneyAgentView agentView;
    [SerializeField] private Camera           mazeCamera;
    [SerializeField] private RawImage         mazeImage;

    [Header("Editors (swapped by the Block/Code setting)")]
    [SerializeField] private GameObject blockPanel;
    [SerializeField] private GameObject codePanel;
    [SerializeField] private BlockCanvasController  blockCanvas;
    [SerializeField] private BlockPaletteController palette;
    [SerializeField] private CodeEditorController   codeEditor;

    [Header("Execution")]
    [SerializeField] private ExecutionController exec;

    [Header("Controls")]
    [SerializeField] private Button runButton;
    [SerializeField] private Button resetButton;

    [Header("Tuning")]
    [Tooltip("Maze size in cells (square). Kept small so a wall-follower finishes fast.")]
    [SerializeField] private int mazeCells = 4;
    [Tooltip("Soft timer; on expiry the puzzle ends with a score penalty (run continues).")]
    [SerializeField] private float softTimerSeconds = 90f;

    Action<MinigameResult> _onDone;
    AutomationPuzzleDefinition _def;
    AgentSim _sim;
    RenderTexture _rt;
    bool  _active;
    bool  _codeActive;
    int   _attempts;
    float _timeLeft;

    void Awake()
    {
        if (runButton   != null) runButton.onClick.AddListener(OnRun);
        if (resetButton != null) resetButton.onClick.AddListener(OnReset);

        if (exec != null)
        {
            exec.OnFinished     += HandleFinished;
            exec.OnRuntimeError += HandleRuntimeError;
        }
    }

    // Deactivate in Start (not Awake) so the embedded editor components on child
    // objects finish their own Awake/initialization on scene load first.
    void Start()
    {
        if (root != null) root.SetActive(false);
    }

    void OnDestroy()
    {
        if (runButton   != null) runButton.onClick.RemoveListener(OnRun);
        if (resetButton != null) resetButton.onClick.RemoveListener(OnReset);

        if (exec != null)
        {
            exec.OnFinished     -= HandleFinished;
            exec.OnRuntimeError -= HandleRuntimeError;
        }

        if (mazeCamera != null) mazeCamera.targetTexture = null;
        if (_rt != null) { _rt.Release(); Destroy(_rt); _rt = null; }
    }

    // -------------------------------------------------------------------------

    /// <summary>Opens the maze repair puzzle. <paramref name="fault"/> is flavor only.</summary>
    public void Show(BreakdownFault fault, int seed, Action<MinigameResult> onDone)
    {
        _onDone   = onDone;
        _attempts = 0;
        _timeLeft = softTimerSeconds;

        // Generate a fresh perfect maze; it is always solvable by a wall-follower.
        _def = MazeGenerator.Generate(mazeCells, mazeCells, seed);

        GridModel grid = GridModel.Parse(_def.gridMap, out _);
        _sim = new AgentSim(grid, new FareTable(), _def.startFacing);

        // Render the maze graphically: build the iso tiles and let the agent view
        // animate the jeepney cell-to-cell (driven by ExecutionController).
        if (worldView != null) worldView.Build(grid);
        if (exec != null)
            exec.Init(grid, _sim, agentView, worldView, worldView, _def, _def.startFacing);
        EnsureRenderTexture();
        if (worldView != null) worldView.FrameCamera(mazeCamera);
        if (mazeCamera != null) mazeCamera.enabled = true;

        // Prime both editors; the active one is chosen by the setting below.
        if (blockCanvas != null) blockCanvas.Init(_def.allowedQueries, null);
        if (palette     != null) palette.Init(_def.allowedBlocks, blockCanvas);
        if (codeEditor  != null) codeEditor.SetScaffold(_def.codeScaffold);

        bool blockMode = SaveSystem.Current != null && SaveSystem.Current.settings.blockMode;
        _codeActive = !blockMode;
        if (blockPanel != null) blockPanel.SetActive(blockMode);
        if (codePanel  != null) codePanel.SetActive(_codeActive);

        if (titleLabel != null)
            titleLabel.text = fault == BreakdownFault.Fuel
                ? "FUEL LINE JAMMED — reroute the flow!"
                : "ENGINE SEIZED — trace the circuit!";
        if (goalLabel != null) goalLabel.text = _def.goalText;
        if (feedbackLabel != null)
            feedbackLabel.text = _codeActive
                ? "Write an algorithm, then press RUN to drive the route."
                : "Snap blocks together, then press RUN to drive the route.";

        _active = true;
        if (root != null) root.SetActive(true);
    }

    /// <summary>Creates the maze RenderTexture once and points the camera + UI at it.</summary>
    void EnsureRenderTexture()
    {
        if (mazeCamera == null) return;
        if (_rt == null)
        {
            _rt = new RenderTexture(560, 384, 16) { name = "MazeRT" };
            _rt.Create();
        }
        mazeCamera.targetTexture = _rt;
        if (mazeImage != null) mazeImage.texture = _rt;
    }

    void Update()
    {
        if (!_active) return;

        _timeLeft -= Time.deltaTime;
        if (timerLabel != null)
            timerLabel.text = $"⏱ {Mathf.CeilToInt(Mathf.Max(0f, _timeLeft))}s";

        if (_timeLeft <= 0f)
            Finish(timedOut: true);   // soft escape hatch — the drive carries on
    }

    // -------------------------------------------------------------------------
    // Controls

    void OnRun()
    {
        if (!_active || exec == null) return;

        if (exec.State == ExecutionController.ExecState.Paused)
        {
            exec.TogglePause();
            return;
        }
        if (exec.State == ExecutionController.ExecState.Running)
            return;

        List<LangError> errors;
        ProgramNode program = _codeActive
            ? (codeEditor  != null ? codeEditor.BuildProgram(out errors)  : EmptyProgram(out errors))
            : (blockCanvas != null ? blockCanvas.BuildProgram(out errors) : EmptyProgram(out errors));

        if (errors != null && errors.Count > 0)
        {
            if (feedbackLabel != null) feedbackLabel.text = errors[0].ToString();
            return;
        }
        if (program == null || program.Statements.Count == 0)
        {
            if (feedbackLabel != null) feedbackLabel.text = "The program is empty — add some commands first.";
            return;
        }

        _attempts++;
        if (feedbackLabel != null) feedbackLabel.text = "Driving…";
        exec.Run(program);
    }

    void OnReset()
    {
        if (!_active || exec == null) return;
        exec.ResetWorld();   // snaps the agent view back to the start
        if (feedbackLabel != null) feedbackLabel.text = "World reset — edit and RUN again.";
    }

    static ProgramNode EmptyProgram(out List<LangError> errors)
    {
        errors = new List<LangError>();
        return new ProgramNode();
    }

    // -------------------------------------------------------------------------
    // Execution events

    void HandleRuntimeError(LangError error)
    {
        if (feedbackLabel != null) feedbackLabel.text = error.ToString();
    }

    void HandleFinished(bool win)
    {
        if (!_active) return;

        if (win)
        {
            Finish(timedOut: false);
            return;
        }

        if (feedbackLabel != null)
            feedbackLabel.text = "The jeepney didn't reach the exit. Edit your program and RUN again.";
    }

    // -------------------------------------------------------------------------

    void Finish(bool timedOut)
    {
        if (!_active) return;
        _active = false;

        if (exec != null) exec.ResetWorld();
        if (mazeCamera != null) mazeCamera.enabled = false;   // stop rendering the RT
        if (root != null) root.SetActive(false);

        int penalty = timedOut ? 40 : 0;
        int retries = Mathf.Max(0, _attempts - 1);
        var result = new MinigameResult
        {
            TimedOut = timedOut,
            Mistakes = retries,
            Score    = Mathf.Max(10, 100 - penalty - 10 * retries),
        };

        Action<MinigameResult> cb = _onDone;
        _onDone = null;
        cb?.Invoke(result);
    }
}
