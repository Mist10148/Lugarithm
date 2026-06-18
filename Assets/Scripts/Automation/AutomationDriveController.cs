using System.Collections;
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

    [Header("Top-down world (procedural levels)")]
    [SerializeField] private Transform        topDownWorldRoot;
    [SerializeField] private TopDownAgentView topDownAgentView;
    [SerializeField] private CameraFollow2D   cameraFollow;

    [Header("Execution")]
    [SerializeField] private ExecutionController exec;

    [Header("Workspace")]
    [SerializeField] private TMP_Text   goalLabel;
    [SerializeField] private GameObject blockPanel;
    [SerializeField] private GameObject codePanel;
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
    [SerializeField] private Slider speedSlider;
    [SerializeField] private TMP_Text speedLabel;
    [SerializeField] private Button stepButton;

    [Header("Readouts")]
    [SerializeField] private ConsoleController      console;
    [SerializeField] private StateMonitorController monitor;
    [SerializeField] private AutomationResultsPanel results;

    [Header("Town gate (non-code, required to advance)")]
    [SerializeField] private FlowConnectMinigame flowPuzzle;
    [SerializeField] private CrateStackMinigame  cratePuzzle;

    [Header("Tutorial repair drills")]
    [SerializeField] private MazeRepairMinigame mazeRepairMinigame;  // code · repair (escape a maze)
    [SerializeField] private RefuelMinigame     refuelMinigame;      // non-code · fuel

    [Header("Dialogue")]
    [SerializeField] private DialogueController dialogue;

    [Header("Co-Pilot Hints")]
    [SerializeField] private Button   hintButton;
    [SerializeField] private TMP_Text hintLabel;

    [Header("Vibe Coding")]
    [SerializeField] private VibeCodingController vibeCtrl;
    [SerializeField] private Button               codeChatButton;

    [Header("Leg completion")]
    [SerializeField] private LegCompletionController legCompletion;

    [Header("Self-driving (procedural town)")]
    [SerializeField] private SelfDriveAgent selfDrive;
    [SerializeField] private Button         autopilotButton;

    // -------------------------------------------------------------------------

    LevelDefinition _level;
    AutomationPuzzleDefinition _def;
    GridModel       _grid;
    List<GridRide>  _rides;
    IAgentView      _activeAgent;
    int  _startFacing;
    int  _levelIndex;
    bool _codeTabActive;
    bool _lastRunWasCode;
    int  _runCount;
    int  _hintTier;
    int  _lastExecutedLine;
    int  _townPuzzleBonus;
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

        // Procedural town: build the automation grid + rides from a generated
        // layout so the self-driving agent has real passengers to tend. The
        // authored maze stays the fallback (and its keystone test still uses it).
        _rides = null;
        bool useProceduralTopDown = _level.procedural != null && _level.procedural.enabled;
        TownLayout proceduralLayout = null;
        if (useProceduralTopDown)
        {
            int seed = System.Guid.NewGuid().GetHashCode() & 0x7fffffff;
            proceduralLayout = TownLayoutGenerator.Generate(_level.procedural, _level.fares, seed);
            _def = SelfDrivePlanner.BuildPuzzle(proceduralLayout, _level.procedural.gen.gridCellSize,
                                                out _rides, out _);
        }

        if (GameManager.Instance != null)
            GameManager.Instance.PendingCurrency = 0;

        // World — either the authored puzzle grid, or one derived from the
        // manual route so this scene mirrors the manual drive on tiles.
        string[] gridMap   = _def.gridMap;
        int      startFacing = _def.startFacing;
        if (!useProceduralTopDown && deriveGridFromRoute && !_def.useAuthoredGrid &&
            _level.manual != null && _level.manual.waypoints != null &&
            _level.manual.waypoints.Length >= 2)
        {
            RouteToGrid.Result derived = RouteToGrid.FromManualRoute(_level.manual);
            gridMap     = derived.Map;
            startFacing = derived.StartFacing;
        }

        GridModel grid = GridModel.Parse(gridMap, out List<string> mapErrors);
        foreach (string problem in mapErrors)
            if (console != null) console.Error("map: " + problem);

        var sim = new AgentSim(grid, _level.fares, startFacing);
        _grid = grid;
        _startFacing = startFacing;
        if (_rides != null) sim.LoadRides(_rides);

        IAgentView activeAgent = null;
        IGridSpace activeSpace = null;
        IStopView  activeStopView = null;

        if (useProceduralTopDown)
        {
            // Retire iso for procedural levels: render the same top-down road as
            // Manual mode and drive a top-down jeepney sprite over it.
            if (topDownWorldRoot == null)
            {
                var rootGo = new GameObject("TopDownWorldRoot");
                topDownWorldRoot = rootGo.transform;
            }

            float roadHalfWidth = _level.manual != null ? _level.manual.roadHalfWidth : 3f;
            var tdSpace = new TopDownGridSpace(proceduralLayout, _level.procedural.gen.gridCellSize,
                                               roadHalfWidth, topDownWorldRoot);
            activeSpace = tdSpace;
            activeStopView = tdSpace;

            if (topDownAgentView == null)
            {
                var go = new GameObject("TopDownAgent");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = Resources.Load<Sprite>("Placeholders/jeepney_top");
                sr.sortingOrder = 10;
                topDownAgentView = go.AddComponent<TopDownAgentView>();
                topDownAgentView.body = sr;
            }
            activeAgent = topDownAgentView;

            if (cameraFollow == null && worldCamera != null)
                cameraFollow = worldCamera.gameObject.AddComponent<CameraFollow2D>();
            if (cameraFollow != null)
                cameraFollow.SnapTo(topDownAgentView.transform);

            if (worldCamera != null)
            {
                worldCamera.rect = new Rect(0f, 0f, 1f, 1f);
                worldCamera.orthographicSize = 12f;
            }
        }
        else
        {
            // Iso fallback for authored mazes (Oton and tests).
            if (worldView != null)
            {
                worldView.Build(grid);
                if (_rides != null) ColorRideStops();
                worldView.FrameCamera(worldCamera);
            }
            if (agentView != null)
                agentView.Init(worldView, grid.StartPos, sim.Facing);

            activeAgent = agentView;
            activeSpace = worldView;
            activeStopView = worldView;
        }

        if (exec != null)
        {
            _activeAgent = activeAgent;
            exec.Init(grid, sim, activeAgent, activeSpace, activeStopView, _def, startFacing);
            exec.OnStepDone     += HandleStepDone;
            exec.OnRuntimeError += HandleRuntimeError;
            exec.OnFinished     += HandleFinished;
            exec.OnWorldReset   += HandleWorldReset;
            exec.OnHotLine      += HandleHotLine;
        }

        // Workspace
        if (goalLabel != null) goalLabel.text = _def.goalText;

        if (blockCanvas != null) blockCanvas.Init(_def.allowedQueries, console);
        if (palette     != null) palette.Init(_def.allowedBlocks, blockCanvas);
        if (codeEditor  != null) codeEditor.SetScaffold(_def.codeScaffold);

        if (vibeCtrl != null && codeEditor != null)
        {
            vibeCtrl.Init(_def.allowedBlocks, _def.allowedQueries, codeEditor);
            vibeCtrl.gameObject.SetActive(false);
        }

        if (codeChatButton != null)
            codeChatButton.onClick.AddListener(ToggleVibeWindow);

        if (hintButton != null)
        {
            hintButton.gameObject.SetActive(false);
            hintButton.onClick.AddListener(OnHintRequested);
        }

        bool blockMode = SaveSystem.Current.settings.blockMode;
        SetTab(codeActive: !blockMode);

        // The Block/Code setting is the source of truth — switch the active editor
        // window live when it changes (no in-scene tabs needed).
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnSettingsChanged += ApplyEditorModeFromSettings;

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

        if (speedSlider != null)
        {
            speedSlider.minValue = 0.2f;
            speedSlider.maxValue = 8f;
            speedSlider.value = 1f;
            speedSlider.onValueChanged.AddListener(v =>
            {
                SetSpeed(v);
                if (speedLabel != null) speedLabel.text = $"×{v:0.0}";
            });
        }

        if (stepButton != null)
            stepButton.onClick.AddListener(() => exec?.StepOnce());

        // Autopilot only makes sense when there are rides to tend.
        if (autopilotButton != null)
        {
            autopilotButton.gameObject.SetActive(_rides != null);
            autopilotButton.onClick.AddListener(OnAutopilot);
        }

        if (legCompletion != null)
            legCompletion.OnFinishPressed += OnFinishLeg;

        if (console != null)
        {
            console.Info($"{_level.displayName} — {(_def.allowedQueries.Length > 0 ? "conditionals unlocked" : "sequencing")}");
            console.Info("write a program, then press RUN");
        }
        if (monitor != null) monitor.ShowIdle();

        _startTime = Time.time;

        PlayBoardingDialogue();
    }

    void PlayBoardingDialogue()
    {
        if (dialogue == null) return;

        DialogueConversation convo = DialogueLibrary.ForLevel(_levelIndex, manualMode: false);
        if (convo == null) return;

        dialogue.OnEvent += HandleDialogueEvent;
        dialogue.Play(convo, () =>
        {
            dialogue.OnEvent -= HandleDialogueEvent;
        });
    }

    void HandleDialogueEvent(DialogueEventKind kind, string payload)
    {
        switch (kind)
        {
            // Scripted tutorial drills: launch the matching minigame and only
            // resume the conversation once the player has finished it.
            case DialogueEventKind.TutorialRepair:
                ShowTutorialMinigame(repair: true);
                break;
            case DialogueEventKind.TutorialRefuel:
                ShowTutorialMinigame(repair: false);
                break;

            case DialogueEventKind.DrivingTutorial:
            case DialogueEventKind.FareTutorial:
            case DialogueEventKind.Breakdown:
            case DialogueEventKind.Maintenance:
                StartCoroutine(ResumeDialogueAfter(1.5f));
                break;

            case DialogueEventKind.Arrive:
            case DialogueEventKind.Advance:
            case DialogueEventKind.TutorialComplete:
            case DialogueEventKind.Continue:
            default:
                StartCoroutine(ResumeDialogueAfter(0.1f));
                break;
        }
    }

    /// <summary>
    /// Opens a tutorial repair drill: the code-based MazeRepairMinigame (engine fault)
    /// or the non-code RefuelMinigame. Dialogue resumes once the minigame finishes.
    /// The modal overlays sit on top of the workspace and never touch the sim.
    /// </summary>
    void ShowTutorialMinigame(bool repair)
    {
        int seed = UnityEngine.Random.Range(0, 99999);

        System.Action<MinigameResult> onDone = _ =>
        {
            if (dialogue != null) dialogue.ResumeAfterEvent();
        };

        if (repair && mazeRepairMinigame != null)
            mazeRepairMinigame.Show(BreakdownFault.Engine, seed, onDone);
        else if (!repair && refuelMinigame != null)
            refuelMinigame.Show(seed, onDone);
        else
            onDone(null);
    }

    System.Collections.IEnumerator ResumeDialogueAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (dialogue != null)
            dialogue.ResumeAfterEvent();
    }

    // -------------------------------------------------------------------------
    // Tabs

    void SetTab(bool codeActive)
    {
        _codeTabActive = codeActive;

        if (blockPanel != null) blockPanel.SetActive(!codeActive);
        if (codePanel  != null) codePanel.SetActive(codeActive);
    }

    void ApplyEditorModeFromSettings()
    {
        SetTab(codeActive: !SaveSystem.Current.settings.blockMode);
    }

    void OnDestroy()
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnSettingsChanged -= ApplyEditorModeFromSettings;

        if (codeChatButton != null)
            codeChatButton.onClick.RemoveListener(ToggleVibeWindow);

        if (legCompletion != null)
            legCompletion.OnFinishPressed -= OnFinishLeg;

        // Bank any collected fares on any exit — Continue already flushed them,
        // so a second call after completion is a harmless no-op.
        if (GameManager.Instance != null)
            GameManager.Instance.SaveProgress();
    }

    void ToggleVibeWindow()
    {
        if (vibeCtrl == null) return;
        vibeCtrl.gameObject.SetActive(!vibeCtrl.gameObject.activeSelf);
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

        if (_runCount >= 3 && hintButton != null)
            hintButton.gameObject.SetActive(true);

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
    // Self-driving autopilot

    void OnAutopilot()
    {
        if (_won || selfDrive == null || exec == null || _rides == null) return;
        if (selfDrive.IsDriving) return;

        exec.ResetWorld();
        _lastRunWasCode = false;
        if (console != null) console.Info("autopilot engaged — self-driving the route…");

        StartCoroutine(selfDrive.Drive(_grid, exec.Sim, _activeAgent, _rides, _startFacing,
                                       0.3f, _def, HandleFinished));
    }

    /// <summary>Tint each waiting peep by its committed rider color (matches dulog beacons).</summary>
    void ColorRideStops()
    {
        if (worldView == null || _rides == null) return;
        var colors = new List<KeyValuePair<Vector2Int, Color>>();
        foreach (GridRide ride in _rides)
            colors.Add(new KeyValuePair<Vector2Int, Color>(ride.origin, ride.color));
        worldView.ColorStops(colors);
    }

    // -------------------------------------------------------------------------
    // Co-Pilot hints

    public void OnHintRequested()
    {
        DialogueConversation conv = DialogueLibrary.Get(_levelIndex);
        if (conv == null || conv.assistHints.Length == 0) return;

        int idx  = Mathf.Min(_hintTier, conv.assistHints.Length - 1);
        var hint = conv.assistHints[idx];
        var pax  = PassengerLibrary.Get(conv.passengerId);
        if (pax == null) return;

        _hintTier = Mathf.Min(_hintTier + 1, conv.assistHints.Length - 1);
        StartCoroutine(FetchHint(hint.text, pax, idx));
    }

    IEnumerator FetchHint(string authoredText, PassengerDefinition pax, int tier)
    {
        if (hintLabel != null) hintLabel.text = "...";

        string prompt = CopilotHintService.BuildPrompt(authoredText, pax, tier);
        string result = null;
        yield return GeminiClient.Ask(prompt, r => result = r);

        if (hintLabel != null)
            hintLabel.text = result ?? authoredText;
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

        if (_codeTabActive && codeEditor != null && step.Node != null)
            codeEditor.HighlightExecutingLine(step.Node.Line);

        if (!_codeTabActive && blockCanvas != null && step.Node != null)
            blockCanvas.HighlightExecuting(step.Node.SourceRef);

        if (codeEditor != null && exec != null)
            codeEditor.SetHeat(exec.LineHits);
    }

    void HandleHotLine(int line)
    {
        if (console != null)
            console.Warn($"this looks like an infinite loop near line {line}.");
    }

    void HandleRuntimeError(LangError error)
    {
        if (console != null) console.Error(error.ToString());
    }

    void HandleWorldReset()
    {
        if (monitor != null) monitor.ShowIdle();
        if (codeEditor != null) codeEditor.ClearExecutionHighlight();
        if (codeEditor != null) codeEditor.ClearHeat();
    }

    void HandleFinished(bool win)
    {
        if (codeEditor != null) codeEditor.ClearExecutionHighlight();

        if (win)
        {
            _won = true;
            if (legCompletion != null)
                legCompletion.Show();
            else
                BeginResults();
            return;
        }

        if (console != null && exec != null)
        {
            string gap = exec.Sim.DescribeGoalGap(_def);
            console.Warn(gap ?? "the program ended without reaching the goal.");
            console.Info("edit your program and press RUN to try again.");
        }
    }

    void OnFinishLeg()
    {
        if (legCompletion != null) legCompletion.Hide();
        BeginResults();
    }

    // -------------------------------------------------------------------------
    // Results

    /// <summary>Runs the required non-code town gate (if any) before results.</summary>
    void BeginResults()
    {
        bool shown = ShowTownGate(2000 + _levelIndex, result =>
        {
            _townPuzzleBonus = result.Score;
            PlayRevealThenResults();
        });

        if (!shown) PlayRevealThenResults();
        else if (console != null) console.Info("puzzle solved — now clear the town gate to finish the leg.");
    }

    void PlayRevealThenResults()
    {
        if (dialogue == null)
        {
            ShowResults();
            return;
        }

        DialogueConversation convo = DialogueLibrary.ForLevel(_levelIndex, manualMode: false);
        if (convo == null || convo.journalPageId < 0 || convo.journalPageId >= JournalPageLibrary.Pages.Count)
        {
            ShowResults();
            return;
        }

        JournalPageDefinition page = JournalPageLibrary.Pages[convo.journalPageId];
        dialogue.PlayReveal(convo, page, ShowResults);
    }

    /// <summary>Shows the level's required non-code town puzzle. False when there is none.</summary>
    bool ShowTownGate(int seed, System.Action<MinigameResult> onDone)
    {
        switch (_level.townPuzzle)
        {
            case TownPuzzleKind.FlowConnect:
                if (flowPuzzle  != null) { flowPuzzle.Show(seed, onDone);  return true; }
                break;
            case TownPuzzleKind.CrateStack:
                if (cratePuzzle != null) { cratePuzzle.Show(seed, onDone); return true; }
                break;
        }
        return false;
    }

    void ShowResults()
    {
        AgentSim sim = exec.Sim;
        float elapsed = Time.time - _startTime;
        int retries = Mathf.Max(0, _runCount - 1);

        int score = ScoreCalculator.AutomationScore(
            sim.StepsUsed, _def.parSteps, elapsed, _def.softTimerSeconds, retries, _lastRunWasCode)
            + _townPuzzleBonus;

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
                    if (BadgeUnlockManager.Instance != null)
                        BadgeUnlockManager.Instance.Show(_levelIndex, () => LoadScene("LevelSelect"));
                    else
                        LoadScene("LevelSelect");
                },
                onReplay: () => LoadScene(SceneManager.GetActiveScene().name));

            StartCoroutine(FetchMentorFeedback(playerSolution, sim.StepsUsed, retries));
        }
        else
        {
            LoadScene("LevelSelect");
        }
    }

    IEnumerator FetchMentorFeedback(string playerSol, int steps, int retries)
    {
        string concept = JournalPageLibrary.Pages[_levelIndex].codingConceptName;
        string prompt  = CodingMentorService.BuildPrompt(
            _level.displayName, concept, steps, _def.parSteps,
            retries, _lastRunWasCode, playerSol, _def.optimalSolutionText);

        string response = null;
        yield return GeminiClient.Ask(prompt, r => response = r);

        if (results != null)
            results.SetMentorResponse(response
                ?? "Keep experimenting — every attempt teaches you something new!");
    }

    void LoadScene(string sceneName)
    {
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.TransitionTo(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }
}
