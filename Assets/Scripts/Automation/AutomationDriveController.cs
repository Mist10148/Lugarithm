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
    [SerializeField] private Slider speedSlider;
    [SerializeField] private TMP_Text speedLabel;
    [SerializeField] private Button stepButton;
    [SerializeField] private Button editorModeToggle;

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

    [Header("Front-seat story passenger")]
    [SerializeField] private GameObject frontSeatCard;
    [SerializeField] private TMP_Text   frontSeatLabel;

    [Header("Co-Pilot Hints")]
    [SerializeField] private Button   hintButton;
    [SerializeField] private TMP_Text hintLabel;

    [Header("AI helper (in-window chat)")]
    [SerializeField] private VibeCodingController vibeCtrl;
    [SerializeField] private GhostTextController  ghost;   // inline next-line completion (optional)

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
    int  _failCount;
    bool _struggleNudged;
    int  _hintTier;
    int  _bestDelivered;   // best passengers-delivered across runs; eases the hint tier on progress
    int  _lastExecutedLine;
    int  _townPuzzleBonus;
    float _startTime;
    bool  _won;
    bool  _revealPlayed;   // heritage reveal plays once, on reaching the goal (not after the gate)
    bool  _tutorialComplete; // tutorial is dialogue-driven: ending the story completes the leg
    bool  _conversationDone; // story passenger's chat finished — a completion gate (with the win)
    bool  _storyLegShown;    // the reveal + LEVEL COMPLETE card have been shown (guard)
    bool  _solvedNudged;     // showed the "finish your chat" nudge once after an early solve
    string _storyPassengerName = "Your passenger";

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

            // Grass ground under the road so the procedural world reads like Manual
            // (green grass + road), not a dark void.
            AddProceduralGround(topDownWorldRoot, proceduralLayout);

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

        // Bind the copilot's vocabulary + live world FIRST, in its own guard. If the
        // editor population below throws, the AI agent must still receive its world
        // context — without it every request builds a degenerate prompt and the
        // reply reads as "couldn't reach the AI". (The chat self-wires either way.)
        try
        {
            if (vibeCtrl != null)
            {
                vibeCtrl.Init(_def.allowedBlocks, _def.allowedQueries, codeEditor, blockCanvas);
                vibeCtrl.SetWorldContext(grid, sim, _def);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AutomationDrive] copilot init failed: {e}");
        }

        // Confirm the shared AI config is loaded for this scene (same static cache
        // the minigame copilot uses). 0 usable keys here is the real cause of a
        // persistent "couldn't reach the AI" — not a per-scene wiring fault.
        Debug.Log($"[AutomationDrive] AI copilot ready — usable keys: {GeminiClient.ConfiguredKeyCount}");

        // Populating the editors must never abort Start() — if it throws, the
        // control bar + editor toggle below would never wire (dead buttons, both
        // editors visible). Keep the UI usable regardless.
        try
        {
            if (blockCanvas != null) blockCanvas.Init(_def.allowedQueries, console);
            if (palette     != null) palette.Init(_def.allowedBlocks, blockCanvas);
            if (codeEditor  != null) codeEditor.SetScaffold(_def.codeScaffold);
            if (ghost != null) ghost.Bind(_def);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AutomationDrive] editor init failed (UI stays usable): {e}");
        }

        if (hintButton != null)
        {
            hintButton.gameObject.SetActive(false);
            hintButton.onClick.AddListener(OnHintRequested);
        }

        bool blockMode = SaveSystem.Current.settings.blockMode;
        SetTab(codeActive: !blockMode);
        RefreshEditorModeLabel();

        // The Block/Code setting is the source of truth — switch the active editor
        // window live when it changes (no in-scene tabs needed).
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnSettingsChanged += ApplyEditorModeFromSettings;

        // CodeDrive overlays: the workspace toggles visibility (it never switches
        // editor type — that's settings-only); the README panel opens on demand.
        // "Workspace" reopens/focuses the active floating editor window (the old
        // toggle just hid an otherwise-always-useful editor + terminal).
        if (workspaceToggleButton != null)
            workspaceToggleButton.onClick.AddListener(FocusActiveEditor);

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

        if (editorModeToggle != null)
            editorModeToggle.onClick.AddListener(ToggleEditorMode);

        // One-click autopilot is available on every automation level: procedural
        // towns drive their committed rides, authored levels synthesize rides from
        // the grid's stops (see OnAutopilot).
        if (autopilotButton != null)
        {
            autopilotButton.gameObject.SetActive(true);
            autopilotButton.onClick.AddListener(OnAutopilot);
        }

        if (legCompletion != null)
        {
            legCompletion.OnFinishPressed += OnFinishLeg;
            legCompletion.OnKeepExploring += OnKeepExploring;
        }

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
        // No conversation → nothing to finish talking about; let the puzzle win alone
        // complete the leg (don't deadlock the conversation gate).
        if (dialogue == null) { _conversationDone = true; return; }

        DialogueConversation convo = DialogueLibrary.ForLevel(_levelIndex, manualMode: false);
        if (convo == null) { _conversationDone = true; return; }

        PassengerDefinition pax = PassengerLibrary.Get(convo.passengerId);
        if (pax != null && !string.IsNullOrEmpty(pax.displayName)) _storyPassengerName = pax.displayName;
        ShowFrontSeatCard(pax);

        dialogue.OnEvent += HandleDialogueEvent;
        dialogue.Play(convo, () =>
        {
            dialogue.OnEvent -= HandleDialogueEvent;

            // Conversation finished — one of two completion gates. The tutorial has no
            // puzzle, so its chat alone completes; story levels also need the win.
            _conversationDone = true;
            TryShowStoryComplete(_tutorialComplete
                ? $"TUTORIAL COMPLETE — {_level.displayName}"
                : $"PUZZLE SOLVED — {_level.displayName}");
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

            // End of the tutorial story — latch it so the dialogue-finished callback
            // (PlayBoardingDialogue) completes the leg once the conversation closes.
            case DialogueEventKind.TutorialComplete:
                _tutorialComplete = true;
                StartCoroutine(ResumeDialogueAfter(0.1f));
                break;

            case DialogueEventKind.Arrive:
            case DialogueEventKind.Advance:
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
        RefreshEditorModeLabel();
    }

    /// <summary>Reopen + bring the currently-active editor window to the front
    /// (wired to the "Workspace" button — handy after the window's close button).</summary>
    void FocusActiveEditor()
    {
        GameObject active = _codeTabActive ? codePanel : blockPanel;
        if (active == null) return;
        active.SetActive(true);
        var win = active.GetComponent<EditorWindowController>();
        if (win != null) win.Open();
    }

    /// <summary>In-editor Block/Code switch — flips the setting, which fires
    /// OnSettingsChanged → ApplyEditorModeFromSettings → SetTab to swap editors.</summary>
    void ToggleEditorMode()
    {
        bool newBlock = !SaveSystem.Current.settings.blockMode;
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.BlockMode = newBlock;
        else
        {
            SaveSystem.Current.settings.blockMode = newBlock;
            ApplyEditorModeFromSettings();
        }
        RefreshEditorModeLabel();
    }

    void RefreshEditorModeLabel()
    {
        if (editorModeToggle == null) return;
        var label = editorModeToggle.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.text = SaveSystem.Current.settings.blockMode ? "Editor: Blocks" : "Editor: Code";
    }

    void OnDestroy()
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnSettingsChanged -= ApplyEditorModeFromSettings;

        if (legCompletion != null)
        {
            legCompletion.OnFinishPressed -= OnFinishLeg;
            legCompletion.OnKeepExploring -= OnKeepExploring;
        }

        // Bank any collected fares on any exit — Continue already flushed them,
        // so a second call after completion is a harmless no-op.
        if (GameManager.Instance != null)
            GameManager.Instance.SaveProgress();
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
        if (_won || selfDrive == null || exec == null) return;
        if (selfDrive.IsDriving) return;

        // Procedural towns carry committed rides; authored levels synthesize them
        // from the grid's 'P' stops (each bound for D) so the autopilot drives, tends
        // passengers, and collects fares everywhere — and just runs S→D if there are
        // no stops at all.
        List<GridRide> rides = _rides ?? SelfDrivePlanner.RidesFromGrid(_grid, _level.fares);

        exec.ResetWorld();
        _lastRunWasCode = false;
        if (console != null) console.Info("autopilot engaged — self-driving the route…");

        StartCoroutine(selfDrive.Drive(_grid, exec.Sim, _activeAgent, rides, _startFacing,
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

        string source = _codeTabActive && codeEditor != null
            ? codeEditor.Source
            : blockCanvas != null ? blockCanvas.ToSourceText() : "";
        string concept = _levelIndex >= 0 && _levelIndex < JournalPageLibrary.Pages.Count
            ? JournalPageLibrary.Pages[_levelIndex].codingConceptName : "program logic";

        // Shared builder: compiles, dry-runs for a fresh gap, and pre-analyzes the code into
        // concrete diagnostics before packaging the tier-aware request.
        AgentSim sim = exec != null ? exec.Sim : null;
        AiRequest request = CopilotHintFlow.BuildRequest(source, sim, _def, authoredText, pax, tier, concept);
        AiResult result = null;
        string streamed = "";
        yield return GeminiClient.Stream(request, delta =>
        {
            streamed += delta;
            if (hintLabel != null) hintLabel.text = streamed;
        }, completed => result = completed);

        if (hintLabel != null)
            hintLabel.text = result != null && result.Success ? result.Text : authoredText;
    }

    // -------------------------------------------------------------------------
    // Execution events

    // Big tiled grass under the procedural road so Automation's world matches
    // Manual's (which lays the same ground at sortingOrder -100). Sized to the
    // generated layout's bounds plus a margin so it always covers the view.
    void AddProceduralGround(Transform worldRoot, TownLayout layout)
    {
        if (worldRoot == null || layout == null || layout.nodes == null || layout.nodes.Count == 0)
            return;

        float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
        foreach (TownNode n in layout.nodes)
        {
            minX = Mathf.Min(minX, n.pos.x); maxX = Mathf.Max(maxX, n.pos.x);
            minY = Mathf.Min(minY, n.pos.y); maxY = Mathf.Max(maxY, n.pos.y);
        }

        const float margin = 80f;
        var go = new GameObject("ProceduralGround");
        go.transform.SetParent(worldRoot, false);
        go.transform.localPosition = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = Resources.Load<Sprite>("Placeholders/grass_tile");
        sr.drawMode     = SpriteDrawMode.Tiled;
        sr.size         = new Vector2((maxX - minX) + margin * 2f, (maxY - minY) + margin * 2f);
        sr.sortingOrder = -100;
    }

    // Automation drains fuel and can break down mid-run, like Manual — the
    // self-driving jeepney pauses for the refuel/repair mini-game, then resumes.
    float _autoFuel = 1f;
    bool  _autoBreakdownActive;

    // Progression gate (town puzzle) now pops mid-run, not after the win.
    bool  _progressionGateActive;
    bool  _progressionGateDone;

    void AutoFuelTick()
    {
        if (_autoBreakdownActive) return;
        if (refuelMinigame == null && mazeRepairMinigame == null) return;

        _autoFuel -= 0.03f;
        if (_autoFuel <= 0f)
            StartCoroutine(AutoBreakdown(fuel: true));
        else if (UnityEngine.Random.value < 0.012f)
            StartCoroutine(AutoBreakdown(fuel: false));
    }

    IEnumerator AutoBreakdown(bool fuel)
    {
        _autoBreakdownActive = true;
        if (exec != null) exec.SetPaused(true);

        int  seed = UnityEngine.Random.Range(0, 99999);
        bool done = false;
        System.Action<MinigameResult> onDone = result =>
        {
            if (fuel)
            {
                _autoFuel = 1f;
                int cost = RefuelMath.CostForScore(result != null ? result.Score : 0);
                int spent = GameManager.Instance != null
                    ? GameManager.Instance.SpendCurrency(cost)
                    : cost;
                if (console != null)
                    console.Info($"refuel cost: PHP {spent}");
            }
            done = true;
        };

        if (fuel && refuelMinigame != null)
            refuelMinigame.Show(seed, onDone);
        else if (mazeRepairMinigame != null)
            mazeRepairMinigame.Show(BreakdownFault.Engine, seed, onDone);
        else { if (fuel) _autoFuel = 1f; done = true; }

        yield return new WaitUntil(() => done);

        if (exec != null) exec.SetPaused(false);
        _autoBreakdownActive = false;
    }

    // Mid-run progression gate: at a random step during execution, the level's
    // town puzzle pops and must be solved before the run continues — same "during
    // gameplay" feel as a breakdown, but mandatory. BeginResults() is the fallback
    // if a short run never triggered it.
    void MaybeTriggerProgressionGate()
    {
        if (_progressionGateDone || _progressionGateActive || _autoBreakdownActive) return;
        if (_level == null || _level.townPuzzle == TownPuzzleKind.None) return;
        if (UnityEngine.Random.value < 0.02f)
            StartCoroutine(ProgressionGateRoutine());
    }

    IEnumerator ProgressionGateRoutine()
    {
        _progressionGateActive = true;
        if (exec != null) exec.SetPaused(true);

        bool done = false;
        bool shown = ShowSingleTownGate(3000 + _levelIndex, result =>
        {
            _townPuzzleBonus += result != null ? result.Score : 0;
            _progressionGateDone = true;
            done = true;
        });

        if (!shown)
        {
            if (exec != null) exec.SetPaused(false);
            _progressionGateActive = false;
            yield break;
        }

        if (console != null) console.Info("a town task popped up — clear it to keep driving.");
        yield return new WaitUntil(() => done);

        if (exec != null) exec.SetPaused(false);
        _progressionGateActive = false;
    }

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

        AutoFuelTick();
        MaybeTriggerProgressionGate();
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
        _autoFuel = 1f;
        _autoBreakdownActive = false;
    }

    void HandleFinished(bool win)
    {
        if (codeEditor != null) codeEditor.ClearExecutionHighlight();

        if (win)
        {
            _won = true;
            // The leg completes only when the puzzle is solved AND the story passenger's
            // chat is finished. If the chat is still going, hold and nudge once.
            if (!_conversationDone && !_tutorialComplete && !_solvedNudged)
            {
                _solvedNudged = true;
                if (console != null)
                    console.Info($"Solved! Finish your chat with {_storyPassengerName} to wrap up the leg.");
            }
            TryShowStoryComplete($"PUZZLE SOLVED — {_level.displayName}");
            return;
        }

        if (console != null && exec != null)
        {
            string gap = exec.Sim.DescribeGoalGap(_def);
            console.Warn(gap ?? "the program ended without reaching the goal.");
            console.Info("edit your program and press RUN to try again.");
        }

        // Struggle nudge: after a couple of failed runs, gently offer a hint. It stays
        // on-demand — we only surface the button and a one-time, dismissable nudge.
        _failCount++;

        // Ease the next hint's tier when the player makes real progress (one more passenger
        // delivered) so a small new mistake doesn't escalate them straight to pseudocode.
        int delivered = exec != null && exec.Sim != null ? exec.Sim.PassengersDelivered : 0;
        if (delivered > _bestDelivered)
        {
            _bestDelivered = delivered;
            _hintTier = Mathf.Max(0, _hintTier - 1);
        }

        if (_failCount >= 2 && hintButton != null)
        {
            hintButton.gameObject.SetActive(true);
            if (!_struggleNudged)
            {
                _struggleNudged = true;
                if (hintLabel != null)
                    hintLabel.text = "Stuck? Tap Hint — I'll look at what you wrote and nudge you " +
                                     "in the right direction, no spoilers.";
            }
        }
    }

    /// <summary>
    /// Shows the heritage reveal + LEVEL COMPLETE card once the leg's completion gates
    /// are met: the tutorial needs only its chat to finish; story levels need BOTH the
    /// puzzle solved (<see cref="_won"/>) and the conversation finished. Called from the
    /// win path and from the dialogue-finished callback, so whichever happens last fires it.
    /// </summary>
    void TryShowStoryComplete(string title)
    {
        if (_storyLegShown) return;
        bool ready = _tutorialComplete ? _conversationDone : (_won && _conversationDone);
        if (!ready) return;

        _storyLegShown = true;
        PlayRevealOnReach(() =>
        {
            if (legCompletion != null)
                legCompletion.ShowComplete(
                    title,
                    "Great work — your program reached the goal and the story's told.\nFinish the leg to bank your run and unlock what's next.",
                    allowExplore: false);   // fixed automation layout: no free-roam
            else
                BeginResults();
        });
    }

    /// <summary>Shows the persistent front-seat card naming the story passenger we're
    /// coding the route for + conversing with this leg.</summary>
    void ShowFrontSeatCard(PassengerDefinition pax)
    {
        if (frontSeatCard == null) return;
        if (pax == null) { frontSeatCard.SetActive(false); return; }

        if (frontSeatLabel != null)
            frontSeatLabel.text = $"<size=70%>FRONT SEAT</size>\n{_storyPassengerName}";
        frontSeatCard.SetActive(true);
    }

    void OnFinishLeg()
    {
        if (legCompletion != null) legCompletion.Hide();
        BeginResults();
    }

    /// <summary>Automation levels use a fixed layout, so there is no free-roam — the
    /// completion card hides "Keep exploring". This stays as a harmless hook in case
    /// a future level enables it; the controller already leaves the Finish button up.</summary>
    void OnKeepExploring() { }

    // -------------------------------------------------------------------------
    // Results

    /// <summary>The town gate now pops mid-run; this is the fallback for a short run
    /// that never triggered it, keeping the gate mandatory before results.</summary>
    void BeginResults()
    {
        if (!_progressionGateDone && _level.townPuzzle != TownPuzzleKind.None)
        {
            bool shown = ShowSingleTownGate(2000 + _levelIndex, result =>
            {
                _townPuzzleBonus += result != null ? result.Score : 0;
                _progressionGateDone = true;
                PlayRevealThenResults();
            });

            if (shown)
            {
                if (console != null) console.Info("clear the town gate to finish the leg.");
                return;
            }
        }

        PlayRevealThenResults();
    }

    /// <summary>Plays the heritage reveal once, on reaching the goal, then invokes onDone.</summary>
    void PlayRevealOnReach(System.Action onDone)
    {
        if (_revealPlayed || dialogue == null) { _revealPlayed = true; onDone(); return; }

        DialogueConversation convo = DialogueLibrary.ForLevel(_levelIndex, manualMode: false);
        if (convo == null || convo.journalPageId < 0 || convo.journalPageId >= JournalPageLibrary.Pages.Count)
        {
            _revealPlayed = true;
            onDone();
            return;
        }

        JournalPageDefinition page = JournalPageLibrary.Pages[convo.journalPageId];
        dialogue.PlayReveal(convo, page, () => { _revealPlayed = true; onDone(); });
    }

    void PlayRevealThenResults()
    {
        // The reveal already played on reaching the goal — the gate now leads
        // straight to results. The dialogue path below is only an edge-case fallback.
        if (_revealPlayed || dialogue == null)
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

    /// <summary>Shows the level's single town puzzle (matching its TownPuzzleKind).
    /// False when the level has none or its panel isn't wired.</summary>
    bool ShowSingleTownGate(int seed, System.Action<MinigameResult> onDone)
    {
        if (_level.townPuzzle == TownPuzzleKind.FlowConnect && flowPuzzle != null)
        {
            flowPuzzle.Show(seed, onDone);
            return true;
        }
        if (_level.townPuzzle == TownPuzzleKind.CrateStack && cratePuzzle != null)
        {
            cratePuzzle.Show(seed, onDone);
            return true;
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
            CodeAnalysis analysis = CodeAnalyticsService.Analyze(
                playerSolution, _def.optimalSolutionText, sim.StepsUsed, _def.parSteps,
                retries, elapsed, _def.softTimerSeconds, exec.LineHits);
            results.Show(
                $"PUZZLE SOLVED  —  {_level.displayName}",
                playerSolution, _def.optimalSolutionText, stats,
                onContinue: () =>
                {
                    if (GameManager.Instance != null)
                        GameManager.Instance.CompleteLevel(_levelIndex, score);
                    if (results != null) results.Hide();
                    if (BadgeUnlockManager.Instance != null)
                        BadgeUnlockManager.Instance.Show(_levelIndex, () => LoadScene("LevelSelect"));
                    else
                        LoadScene("LevelSelect");
                },
                onReplay: () => LoadScene(SceneManager.GetActiveScene().name),
                analysis: analysis,
                category: _lastRunWasCode ? "MAIN GAMEPLAY · Automation (Code)"
                                          : "MAIN GAMEPLAY · Automation (Blocks)");

            StartCoroutine(FetchMentorFeedback(playerSolution, analysis));
        }
        else
        {
            // No results panel wired — still record completion so the next level
            // unlocks, then bail to the menu.
            if (GameManager.Instance != null)
                GameManager.Instance.CompleteLevel(_levelIndex, score);
            LoadScene("LevelSelect");
        }
    }

    IEnumerator FetchMentorFeedback(string playerSol, CodeAnalysis analysis)
    {
        string concept = JournalPageLibrary.Pages[_levelIndex].codingConceptName;
        AiRequest request = CodingMentorService.BuildRequest(
            _level.displayName, concept, analysis, playerSol, _def.optimalSolutionText,
            _def.allowedBlocks, _def.allowedQueries);
        AiResult response = null;
        yield return GeminiClient.Stream(request, null, completed => response = completed);

        MentorReview review = null;
        int lineCount = string.IsNullOrEmpty(playerSol) ? 0 : playerSol.Replace("\r", "").Split('\n').Length;
        if (response == null || !response.Success ||
            !CodingMentorService.TryParseAndValidate(response.Text, _def.optimalSolutionText,
                _def.allowedBlocks, _def.allowedQueries, lineCount, out review))
            review = CodingMentorService.Fallback(_def.optimalSolutionText, analysis);
        if (results != null) results.SetMentorReview(review);
    }

    void LoadScene(string sceneName)
    {
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.TransitionTo(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }
}
