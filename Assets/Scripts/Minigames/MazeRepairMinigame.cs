using System;
using System.Collections;
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
    [SerializeField] private MinigameResultsPanel resultsPanel;

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
    [SerializeField] private VibeCodingController   vibeCtrl;
    [SerializeField] private GhostTextController    ghost;   // inline next-line completion (optional)

    [Header("Execution")]
    [SerializeField] private ExecutionController exec;

    [Header("Controls")]
    [SerializeField] private Button runButton;
    [SerializeField] private Button resetButton;
    [Tooltip("Loads a known-good maze solver into the editor/blocks and runs it — for testing.")]
    [SerializeField] private Button autopilotButton;

    [Header("Co-Pilot hint (optional)")]
    [SerializeField] private Button   hintButton;
    [SerializeField] private TMP_Text hintLabel;

    [Header("Tuning")]
    [Tooltip("Maze size in cells (square). Kept small so a wall-follower finishes fast.")]
    [SerializeField] private int mazeCells = 4;
    [Tooltip("Soft timer; on expiry the puzzle ends with a score penalty (run continues).")]
    [SerializeField] private float softTimerSeconds = 90f;
    [Tooltip("Execution speed multiplier for this maze only (the maze owns its own " +
             "ExecutionController, so this never affects the main Automation drive). " +
             ">1 finishes the maze faster.")]
    [SerializeField] private float runSpeed = 2.5f;

    Action<MinigameResult> _onDone;
    AutomationPuzzleDefinition _def;
    AgentSim _sim;
    RenderTexture _rt;
    bool  _active;
    bool  _codeActive;
    int   _attempts;
    float _timeLeft;

    // Co-pilot hint state (mirrors AutomationDriveController's tiered, struggle-aware flow).
    int  _hintTier;
    int  _failCount;
    int  _runAttempts;
    bool _struggleNudged;
    int  _bestDelivered;

    void Awake()
    {
        if (runButton   != null) runButton.onClick.AddListener(OnRun);
        if (resetButton != null) resetButton.onClick.AddListener(OnReset);
        if (autopilotButton != null) autopilotButton.onClick.AddListener(OnAutopilot);
        if (hintButton  != null)
        {
            hintButton.gameObject.SetActive(false);
            hintButton.onClick.AddListener(OnHintRequested);
        }
        else Debug.LogWarning("[MazeRepairMinigame] hintButton is not wired — hint UI will never appear.");

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
        if (autopilotButton != null) autopilotButton.onClick.RemoveListener(OnAutopilot);
        if (hintButton  != null) hintButton.onClick.RemoveListener(OnHintRequested);

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

        _hintTier = 0;
        _failCount = 0;
        _runAttempts = 0;
        _struggleNudged = false;
        _bestDelivered = 0;
        if (hintButton != null) hintButton.gameObject.SetActive(false);
        if (hintLabel  != null) hintLabel.text = "";

        // Generate a fresh perfect maze; it is always solvable by a wall-follower.
        _def = MazeGenerator.Generate(mazeCells, mazeCells, seed);

        GridModel grid = GridModel.Parse(_def.gridMap, out _);
        _sim = new AgentSim(grid, new FareTable(), _def.startFacing);

        // Render the maze graphically: build the iso tiles and let the agent view
        // animate the jeepney cell-to-cell (driven by ExecutionController).
        if (worldView != null) worldView.Build(grid);
        if (exec != null)
        {
            exec.Init(grid, _sim, agentView, worldView, worldView, _def, _def.startFacing);
            exec.SetSpeed(runSpeed);   // maze-only: snappier than the weighty open-road cadence
        }
        EnsureRenderTexture();
        if (worldView != null) worldView.FrameCamera(mazeCamera);
        if (mazeCamera != null) mazeCamera.enabled = true;

        // Prime both editors; the active one is chosen by the setting below.
        if (blockCanvas != null) blockCanvas.Init(_def.allowedQueries, null);
        if (palette     != null) palette.Init(_def.allowedBlocks, blockCanvas);
        if (codeEditor  != null) codeEditor.SetScaffold(_def.codeScaffold);

        // Give the in-editor AI agent the level vocabulary + live maze/jeepney state.
        if (vibeCtrl != null)
        {
            vibeCtrl.Init(_def.allowedBlocks, _def.allowedQueries, codeEditor, blockCanvas);
            vibeCtrl.SetWorldContext(grid, _sim, _def);
        }
        if (ghost != null) ghost.Bind(_def);

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
        _runAttempts++;
        Debug.Log($"[MazeRepairMinigame] RUN pressed — attempts={_attempts}, runAttempts={_runAttempts}, failCount={_failCount}");
        if (feedbackLabel != null) feedbackLabel.text = "Driving…";
        exec.SetSpeed(runSpeed);   // re-assert in case a reset left it stale
        exec.Run(program);
        RevealHintAfterStruggle();
    }

    void OnReset()
    {
        if (!_active || exec == null) return;
        exec.ResetWorld();   // snaps the agent view back to the start
        if (feedbackLabel != null) feedbackLabel.text = "World reset — edit and RUN again.";
    }

    /// <summary>
    /// Testing aid: drops a known-good maze solver (the right-hand wall-follower) into the
    /// active surface — block canvas in block mode, code editor otherwise — and runs it, so a
    /// tester can confirm the whole code→blocks→drive pipeline solves the maze in one click.
    /// Mirrors <see cref="AutomationDriveController.LoadAutopilotProgramForCurrentEditor"/>.
    /// </summary>
    void OnAutopilot()
    {
        if (!_active || exec == null || _def == null) return;
        if (exec.State == ExecutionController.ExecState.Running) return;

        string source = !string.IsNullOrWhiteSpace(_def.optimalSolutionText)
            ? _def.optimalSolutionText
            : MazeContent.WallFollower;

        ProgramNode program = Parser.Compile(source, out List<LangError> errors);
        if (errors != null && errors.Count > 0)
        {
            if (feedbackLabel != null) feedbackLabel.text = errors[0].ToString();
            return;
        }

        bool loaded;
        if (_codeActive)
        {
            loaded = codeEditor != null;
            if (loaded) codeEditor.SetSource(source);
        }
        else
        {
            loaded = blockCanvas != null && blockCanvas.LoadProgram(program);
            if (!loaded && codeEditor != null)
            {
                // The solver uses code this canvas can't show as blocks — run it as code instead.
                codeEditor.SetSource(source);
                if (codePanel  != null) codePanel.SetActive(true);
                if (blockPanel != null) blockPanel.SetActive(false);
                _codeActive = true;
                loaded = true;
            }
        }

        if (!loaded)
        {
            if (feedbackLabel != null) feedbackLabel.text = "Autopilot couldn't load the solver.";
            return;
        }

        // Load only — the player presses RUN themselves so they can review, re-run,
        // and Reset cleanly (auto-running here conflicted with the redo flow).
        if (feedbackLabel != null) feedbackLabel.text = "Autopilot loaded the wall-follower — press RUN to drive.";
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

        // Struggle nudge: after a couple of failed runs, surface the on-demand hint button.
        _failCount++;
        int delivered = _sim != null ? _sim.PassengersDelivered : 0;
        if (delivered > _bestDelivered)
        {
            _bestDelivered = delivered;
            _hintTier = Mathf.Max(0, _hintTier - 1);
        }
        RevealHintAfterStruggle();
    }

    // -------------------------------------------------------------------------
    // Co-Pilot hint (shared flow, minigame fallback voice)

    void RevealHintAfterStruggle()
    {
        Debug.Log($"[MazeRepairMinigame] RevealHintAfterStruggle — runAttempts={_runAttempts}, failCount={_failCount}, hintButton={(hintButton != null ? "wired" : "NULL")}");
        if (hintButton == null) return;
        if (_failCount < 2 && _runAttempts < 3) return;

        hintButton.gameObject.SetActive(true);
        if (!_struggleNudged)
        {
            _struggleNudged = true;
            if (hintLabel != null)
                hintLabel.text = "Stuck? Tap Hint — I'll look at your code and nudge you, no spoilers.";
        }
    }

    public void OnHintRequested()
    {
        int tier = Mathf.Min(_hintTier, MinigameHintLibrary.MazeHints.Length - 1);
        _hintTier = Mathf.Min(_hintTier + 1, MinigameHintLibrary.MazeHints.Length - 1);
        StartCoroutine(FetchHint(tier));
    }

    IEnumerator FetchHint(int tier)
    {
        // The hint now lives exclusively in the AI vibe-coding chat; clear any legacy
        // fallback label so the same text doesn't show in two places.
        if (hintLabel != null) hintLabel.text = "";

        string source = _codeActive
            ? (codeEditor  != null ? codeEditor.Source : "")
            : (blockCanvas != null ? blockCanvas.ToSourceText() : "");
        string authored = MinigameHintLibrary.MazeHints[tier];
        AiRequest request = CopilotHintFlow.BuildRequest(source, _sim, _def, authored,
            MinigameHintLibrary.Mechanic, tier, MinigameHintLibrary.MazeConcept);

        AiResult result = null;
        string streamed = "";

        // Open the AI chat and show a pending hint bubble there too.
        TMP_Text vibeHintBubble = vibeCtrl != null
            ? vibeCtrl.AddHintBubble(MinigameHintLibrary.Mechanic.speakerName,
                                     MinigameHintLibrary.Mechanic.role)
            : null;

        yield return GeminiClient.Stream(request, delta =>
        {
            streamed += delta;
            if (vibeHintBubble != null)
                vibeCtrl.SetHintBubbleText(vibeHintBubble, streamed,
                    MinigameHintLibrary.Mechanic.speakerName,
                    MinigameHintLibrary.Mechanic.role);
        }, completed => result = completed);

        string final = result != null && result.Success ? result.Text : authored;
        if (vibeHintBubble != null)
            vibeCtrl.SetHintBubbleText(vibeHintBubble, final,
                MinigameHintLibrary.Mechanic.speakerName,
                MinigameHintLibrary.Mechanic.role);
    }

    // -------------------------------------------------------------------------

    void Finish(bool timedOut)
    {
        if (!_active) return;

        if (timedOut)
        {
            _failCount++;
            Debug.Log($"[MazeRepairMinigame] timed out — failCount={_failCount}");
            RevealHintAfterStruggle();
        }

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

        if (resultsPanel != null)
        {
            // Code-based drill → show the same kind of code analysis as the main
            // Automation panel (deterministic; no per-drill AI call).
            string playerSource = _codeActive
                ? (codeEditor  != null ? codeEditor.Source : "")
                : (blockCanvas != null ? blockCanvas.ToSourceText() : "");
            float elapsed = Mathf.Max(0f, softTimerSeconds - _timeLeft);
            CodeAnalysis analysis = CodeAnalyticsService.Analyze(
                playerSource, _def != null ? _def.optimalSolutionText : "",
                _sim != null ? _sim.StepsUsed : 0, _def != null ? _def.parSteps : 1,
                retries, elapsed, softTimerSeconds, exec != null ? exec.LineHits : null);

            resultsPanel.Show("MINIGAME · Code", "MAZE REPAIR", result, analysis, () => cb?.Invoke(result));
        }
        else
        {
            cb?.Invoke(result);
        }
    }
}
