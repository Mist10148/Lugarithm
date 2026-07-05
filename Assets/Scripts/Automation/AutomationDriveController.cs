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
    [SerializeField] private Button speedButton;
    [SerializeField] private TMP_Text speedLabel;
    [SerializeField] private Button stepButton;
    [SerializeField] private Button editorModeToggle;

    [Header("Control Bar — Code window's own copy (duplicated toolbar)")]
    [SerializeField] private Button codeRunButton;
    [SerializeField] private Button codePauseButton;
    [SerializeField] private Button codeResetButton;
    [SerializeField] private Button codeStepButton;
    [SerializeField] private Button codeSpeedButton;
    [SerializeField] private TMP_Text codeSpeedLabel;
    [SerializeField] private Button codeAutopilotButton;

    [Header("Readouts")]
    [SerializeField] private ConsoleController      console;
    [SerializeField] private TerminalPanelController terminal;
    [SerializeField] private StateMonitorController monitor;
    [SerializeField] private TMP_Text               walletLabel;
    [SerializeField] private Image                  automationFuelFill;
    [SerializeField] private Image                  gaugeFuelFill;
    [SerializeField] private TMP_Text               gaugeSpeedLabel;
    [SerializeField] private RectTransform          gaugeSpeedNeedle;
    [SerializeField] private PassengerRibbonController passengerRibbon;
    [SerializeField] private AutomationDulogMarkerController dulogMarkers;
    [SerializeField] private AutomationResultsPanel results;

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

    // Speed is a single cycle button now: tap to step through these presets.
    static readonly float[] SpeedPresets = { 0.25f, 0.5f, 1f, 2f, 4f };
    public static IReadOnlyList<float> SpeedPresetValues => SpeedPresets;
    int _speedIndex = 1;   // start at ×1.0

    public float CurrentSpeedPreset => SpeedPresets[_speedIndex];

    LevelDefinition _level;
    AutomationPuzzleDefinition _def;
    GridModel       _grid;
    List<GridRide>  _rides;
    IAgentView      _activeAgent;
    TopDownGridSpace _topDownSpace;
    StreamingTown   _streamingTown;
    bool _proceduralTopDown;
    bool _optionalFreeRoam;
    int  _maxChunks;
    int  _chunksAppended;
    int  _chunkGenerationId;
    readonly List<StreamedChunkView> _streamedChunkViews = new List<StreamedChunkView>();
    bool _destinationFinalized;
    int  _startFacing;
    int  _levelIndex;
    bool _codeTabActive;
    bool _lastRunWasCode;
    int  _runCount;
    int  _failCount;
    readonly CodeRunHistory _runHistory = new CodeRunHistory();
    CodeRunAttempt _activeRunAttempt;
    bool _struggleNudged;
    int  _hintTier;
    int  _bestDelivered;   // best passengers-delivered across runs; eases the hint tier on progress
    int  _lastExecutedLine;
    int  _outputCursor;   // how many print() lines we've already pushed to the terminal this run
    float _startTime;
    bool  _won;
    bool  _revealPlayed;   // heritage reveal plays once, on reaching the goal (not after the gate)
    bool  _tutorialComplete; // tutorial is dialogue-driven: ending the story completes the leg
    bool  _conversationDone; // story passenger's chat finished — a completion gate (with the win)
    bool  _storyLegShown;    // the reveal + LEVEL COMPLETE card have been shown (guard)
    bool  _solvedNudged;     // showed the "finish your chat" nudge once after an early solve
    bool  _endlessWinFired;  // endless leg already detected its story-drop-off win mid-run (guard)
    bool  _boardingPlayed;   // boarding/story dialogue has played this leg (never replays)
    bool  _freeRoamStoryConsumed; // story/reveal finished; free-roam must never restart dialogue
    bool  _storyDropHandled; // front-seat card already torn down on the story drop-off (guard)
    Vector3 _storyDropoffWorld;  // fixed world position of the story drop-off (stable across streaming)
    bool  _storyDropoffArmed;
    GameObject _storyMarker;     // the distinct on-road marker at the drop-off
    string _storyPassengerName = "Your passenger";
    DriveInterruptionScheduler _interruptionScheduler;

    // How far ahead (in grid cells) the story drop-off marker is placed, and how long after the
    // dialogue ends before it appears (a beat of normal driving, then the destination shows).
    const int   StoryDropoffBufferCells   = 8;
    const float StoryDropoffBufferSeconds = 2.5f;

    // Lookahead distance (world units) at which the procedural town streams the next
    // chunk ahead of the driving program — matches Manual's StreamLookAhead.
    const float StreamLookAhead = 70f;   // spawn the next chunk well off-camera (cam half-width ~21 + lead) so it doesn't pop in at the edge
    const int ActiveChunksBehind = 2;
    const int ActiveChunksAhead = 6;

    public float AutomationFuel01 => _autoFuel;
    public int AutomationRefuelSpent => _autoRefuelSpent;

    void Start()
    {
        _speedIndex = 2;

        // Resolve level (Tutorial fallback for direct editor play).
        _levelIndex = GameManager.Instance != null ? GameManager.Instance.SelectedLevelIndex : 0;
        _level = LevelLibrary.Get(_levelIndex);
        if (!_level.hasContent)
        {
            _levelIndex = 0;
            _level = LevelLibrary.Get(0);
        }
        _def = _level.auto;
        _interruptionScheduler = new DriveInterruptionScheduler(5000 + _levelIndex);

        // Procedural town: build the automation grid + rides from a generated
        // layout so the self-driving agent has real passengers to tend. The
        // authored maze stays the fallback (and its keystone test still uses it).
        _rides = null;
        bool useProceduralTopDown = _level.procedural != null && _level.procedural.enabled;
        _proceduralTopDown = useProceduralTopDown;
        TownLayout proceduralLayout = null;
        if (useProceduralTopDown)
        {
            int seed = System.Guid.NewGuid().GetHashCode() & 0x7fffffff;
            _streamingTown = StreamingTownGenerator.Begin(_level.procedural, _level.fares, seed);
            _streamingTown.EndlessNoTerminal = true;   // no "Destination" end sign — the road never stops
            _maxChunks = int.MaxValue;   // stream endlessly; the leg completes on the story drop-off, not a terminal

            // The authored trunk is already long enough to fill the screen, so instead of
            // pre-streaming chunks (which spiked startup with hundreds of GameObjects), just
            // demote the authored terminal so no "Destination" end-sign shows. The road then
            // extends continuously as the program drives (TickProceduralStreaming).
            TownNode initialDest = _streamingTown.Layout.Node(_streamingTown.Layout.destNodeId);
            if (initialDest != null) initialDest.kind = NodeKind.Junction;

            proceduralLayout = _streamingTown.Layout;
            _def = SelfDrivePlanner.BuildPuzzle(proceduralLayout, _level.procedural.gen.gridCellSize,
                                                _levelIndex, out _rides, out _);
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

        // Endless procedural road: completion is the story drop-off (armed when the dialogue
        // ends), not "all riders delivered". StoryLegMode keeps the leg from finishing early
        // off filler riders while the road streams.
        sim.EndlessRoute = useProceduralTopDown;
        sim.StoryLegMode = useProceduralTopDown;

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
            _topDownSpace = new TopDownGridSpace(proceduralLayout, _level.procedural.gen.gridCellSize,
                                                 roadHalfWidth, topDownWorldRoot);
            activeSpace = _topDownSpace;
            activeStopView = _topDownSpace;

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
            // NOTE: the camera is snapped to the jeepney AFTER exec.Init positions the agent
            // at the start cell (below) — snapping here would point it at world origin first.

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

        // Now that exec.Init has placed the agent at its start cell, lock the camera onto
        // the real jeepney position (snapping earlier would frame the empty world origin).
        if (_proceduralTopDown && cameraFollow != null && topDownAgentView != null)
            cameraFollow.SnapTo(topDownAgentView.transform);

        // Surface each onboard passenger's drop-off target (pulsing pin + off-screen compass).
        if (dulogMarkers != null)
        {
            Transform agentT = _proceduralTopDown && topDownAgentView != null
                ? topDownAgentView.transform
                : (agentView != null ? agentView.transform : null);
            dulogMarkers.Init(exec, activeSpace, agentT, worldCamera);
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
            if (codeEditor  != null)
            {
                codeEditor.ConfigureAutocomplete(_def.allowedBlocks, _def.allowedQueries, _def.allowedReporters);
                codeEditor.SetScaffold(_def.codeScaffold);
            }
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

        // Control bar (Block window's copy — the Code window has its own identical
        // copy wired below, kept in sync since both windows can be open at once).
        if (runButton    != null) runButton.onClick.AddListener(OnRun);
        if (pauseButton  != null) pauseButton.onClick.AddListener(OnPause);
        if (resetButton  != null) resetButton.onClick.AddListener(OnReset);

        // Both windows' speed buttons cycle the same shared preset index, so a tap on
        // either advances the speed and both faces (plus the gauge) stay in sync.
        if (speedButton != null) speedButton.onClick.AddListener(CycleSpeed);

        if (gaugeSpeedLabel != null) gaugeSpeedLabel.text = "x1.0";

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

        // Code window's own toolbar copy — identical wiring, kept in sync via the
        // speed-slider listeners above/below so switching tabs mid-run stays consistent.
        if (codeRunButton    != null) codeRunButton.onClick.AddListener(OnRun);
        if (codePauseButton  != null) codePauseButton.onClick.AddListener(OnPause);
        if (codeResetButton  != null) codeResetButton.onClick.AddListener(OnReset);
        if (codeStepButton   != null) codeStepButton.onClick.AddListener(() => exec?.StepOnce());
        if (codeAutopilotButton != null)
        {
            codeAutopilotButton.gameObject.SetActive(true);
            codeAutopilotButton.onClick.AddListener(OnAutopilot);
        }

        if (codeSpeedButton != null) codeSpeedButton.onClick.AddListener(CycleSpeed);

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
        if (passengerRibbon != null) passengerRibbon.Init();
        RefreshAutomationHud();

        _startTime = Time.time;

        PlayBoardingDialogue();
    }

    void Update()
    {
        if (_proceduralTopDown)
            UpdateProceduralGroundPosition();
        TickProceduralStreaming();
        TickEndlessWinCheck();
        if (_proceduralTopDown)
            RefreshChunkWindow();
        RefreshAutomationHud();
        UpdateSpeedNeedle();

        // NOTE: we intentionally do NOT reset the camera's chase velocity at every batch
        // boundary anymore. The agent now cruises continuously across batches
        // (TopDownAgentView carries speed + heading between PlayContinuousPath calls), so
        // there's no per-step hard stop to overshoot — SmoothDamp self-settles when the
        // jeepney genuinely stops. Zeroing it each batch only produced a camera hitch synced
        // to the 4-cell cadence. ResetVelocity() is still used on real teleports (spawn snap).
    }

    /// <summary>
    /// On the endless road a program can run forever (<c>while True: keepDriving()</c>),
    /// so the leg can't wait for the program to finish to detect the win. The moment the
    /// required story rider is delivered we complete the leg — but leave the program and
    /// the road running (free-roam) so the player keeps cruising/testing.
    /// </summary>
    void TickEndlessWinCheck()
    {
        if (!_proceduralTopDown || _won || _endlessWinFired || _storyLegShown) return;
        if (exec == null || exec.Sim == null || _def == null || !_def.endlessRoute) return;
        if (exec.State != ExecutionController.ExecState.Running) return;
        if (!exec.Sim.IsWin(_def)) return;

        _endlessWinFired = true;
        _won = true;
        _optionalFreeRoam = true;   // keep streaming the road after the win
        OnStoryDropped();
        if (!_conversationDone && !_tutorialComplete && !_solvedNudged)
        {
            _solvedNudged = true;
            if (console != null)
                console.Info($"Delivered! Finish your chat with {_storyPassengerName} to wrap up — keep driving as long as you like.");
        }
        TryShowStoryComplete($"LEVEL COMPLETE — {_level.displayName}");
    }

    /// <summary>The front-seat story passenger just alighted at their marked drop-off — tear
    /// down the "[name] is in the front seat" card (they're no longer aboard). Fires once.</summary>
    void OnStoryDropped()
    {
        if (_storyDropHandled) return;
        _storyDropHandled = true;
        _storyDropoffArmed = false;             // stop re-pinning; the drop already happened
        if (_storyMarker != null) _storyMarker.SetActive(false);   // marker served its purpose
        ShowFrontSeatCard(null);                // hide the front-seat card
        if (console != null) console.Info($"{_storyPassengerName} hopped off — salamat!");
    }

    /// <summary>
    /// Streams the next chunk ahead of the driving program, mirroring Manual's lookahead,
    /// so the procedural town is continuously generated from the start (not only after a
    /// win). Only appends at a static-world boundary — program running, nothing animating,
    /// no queued moves — so re-rasterizing the grid can never teleport an in-flight path.
    /// </summary>
    void TickProceduralStreaming()
    {
        if (!_proceduralTopDown || _streamingTown == null) return;
        if (_storyLegShown && !_optionalFreeRoam) return;   // resume streaming for free-roam
        if (_won && !_optionalFreeRoam) return;
        if (exec == null || exec.Sim == null || topDownAgentView == null) return;
        if (_chunksAppended >= _maxChunks) return;
        if (exec.State != ExecutionController.ExecState.Running) return;
        if (exec.Busy) return;   // wait for a static-world boundary between visual batches

        float distToEnd = Vector2.Distance(
            (Vector2)topDownAgentView.transform.position, _streamingTown.TrunkEndPos);
        if (distToEnd >= StreamLookAhead) return;

        AppendProceduralRoute(keepProgramRunning: true);
    }

    void PlayBoardingDialogue()
    {
        // Play the story conversation exactly once per leg — free-roam, streaming, and
        // program re-runs must never replay it.
        if (_boardingPlayed || _freeRoamStoryConsumed)
        {
            Debug.LogWarning("[Automation] PlayBoardingDialogue re-entry blocked (dialogue already played).");
            return;
        }
        _boardingPlayed = true;

        // No conversation → nothing to finish talking about; let the puzzle win alone
        // complete the leg (don't deadlock the conversation gate).
        if (dialogue == null) { _conversationDone = true; FinalizeStoryDestination(); return; }

        DialogueConversation convo = DialogueLibrary.ForLevel(_levelIndex, manualMode: false,
            blockMode: SaveSystem.Current.settings.blockMode);
        if (convo == null) { _conversationDone = true; FinalizeStoryDestination(); return; }

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
            FinalizeStoryDestination();
            TryShowStoryComplete(_tutorialComplete
                ? $"TUTORIAL COMPLETE — {_level.displayName}"
                : $"LEVEL COMPLETE — {_level.displayName}");
        });
    }

    /// <summary>
    /// Called when the story conversation ends. Arms the story passenger's drop-off a short
    /// buffer ahead on the existing road and freezes streaming so that cell stays valid while
    /// the jeepney drives there — reaching it ends the leg. Streaming resumes (endless) only if
    /// the player chooses to keep exploring. Mirrors <c>ManualDriveController</c>'s buffer-cap.
    /// </summary>
    void FinalizeStoryDestination()
    {
        if (_destinationFinalized) return;
        _destinationFinalized = true;

        if (_proceduralTopDown && exec != null && exec.Sim != null && _topDownSpace != null)
        {
            // Buffer: drive on a beat after the chat, THEN show the drop-off ahead.
            if (console != null)
                console.Info($"\"Para!\" {_storyPassengerName}'s stop is coming up…");
            StartCoroutine(ArmStoryDropoffAfterBuffer());
        }
    }

    /// <summary>Waits a short buffer after the dialogue, then marks the story drop-off ahead of
    /// the jeepney's CURRENT position and shows the marker — pinned to a fixed world spot so the
    /// road can keep streaming forever without it drifting.</summary>
    System.Collections.IEnumerator ArmStoryDropoffAfterBuffer()
    {
        yield return new WaitForSeconds(StoryDropoffBufferSeconds);
        if (exec == null || exec.Sim == null || _topDownSpace == null || _storyDropHandled) yield break;

        Vector2Int cell = exec.Sim.CellAhead(StoryDropoffBufferCells);
        _storyDropoffWorld = _topDownSpace.CellToWorld(cell);
        _storyDropoffArmed = true;
        exec.Sim.ArmStoryDropoff(cell);
        SpawnStoryMarker(_storyDropoffWorld);
        if (console != null)
            console.Info($"Drop {_storyPassengerName} at the marked stop ahead.");
    }

    /// <summary>Spawns (or repositions) the distinct on-road marker at the story drop-off.</summary>
    void SpawnStoryMarker(Vector3 world)
    {
        if (_storyMarker == null)
        {
            _storyMarker = new GameObject("StoryDropoffMarker");
            if (topDownWorldRoot != null) _storyMarker.transform.SetParent(topDownWorldRoot, false);
            var sr = _storyMarker.AddComponent<SpriteRenderer>();
            var tex = Texture2D.whiteTexture;
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                      new Vector2(0.5f, 0.5f), tex.width);
            sr.color = new Color(0.30f, 0.85f, 0.95f, 1f);   // bright cyan beacon
            sr.sortingOrder = 8;                              // above road, below the jeepney (10)
            _storyMarker.transform.localScale = new Vector3(2f, 2f, 1f);
        }
        _storyMarker.transform.position = new Vector3(world.x, world.y, 0f);
        _storyMarker.SetActive(true);
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

        // Freeze the main program/agent (and thus streaming, gated on exec.State==Running)
        // while the drill is on screen, so nothing keeps driving behind the minigame.
        if (exec != null) exec.SetPaused(true);

        System.Action<MinigameResult> onDone = _ =>
        {
            if (!repair)
            {
                _autoFuel = 1f;
                RefreshAutomationHud();
            }
            if (exec != null) exec.SetPaused(false);
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
        if ((_won && !_optionalFreeRoam) || exec == null) return;

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
        string runSource = _lastRunWasCode
            ? (codeEditor != null ? codeEditor.Source : "")
            : (blockCanvas != null ? blockCanvas.ToSourceText() : "");
        _activeRunAttempt = _runHistory.RecordStarted(
            runSource,
            _lastRunWasCode ? "Automation (Code)" : "Automation (Blocks)");

        if (_runCount >= 3 && hintButton != null)
            hintButton.gameObject.SetActive(true);

        // Fresh terminal for this run: clear the log, reset the print cursor, and
        // pop the terminal open so debug output is visible while the program runs.
        _outputCursor = 0;
        if (terminal != null) terminal.Open();
        if (console != null)
        {
            console.Clear();
            console.Info($"run #{_runCount} started…");
        }

        // RUN never snaps the world back to the start — the jeepney keeps its
        // current cell, passengers and fares, and only the program is re-loaded and
        // run from the top. This is what lets a short routine be run again and again
        // to service the route step by step. Use the RESET button to start over.
        exec.Run(program, resetWorld: false);
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

    // Speedometer needle sweep: rest angle matches the static gauge art (35°); full speed
    // swings the needle down to the high end of the dial. Mirrors ManualHudController's needle.
    const float NeedleRestAngle = 35f;
    const float NeedleFullAngle = -120f;
    float _needleAngle = NeedleRestAngle;

    /// <summary>Drives the bottom-left gauge needle from the top-down jeepney's real cruise speed
    /// (world units/sec). It rises as the agent accelerates, holds while cruising, and falls back to
    /// rest when stopped or idle — matching Manual mode's speedometer behavior.</summary>
    void UpdateSpeedNeedle()
    {
        if (gaugeSpeedNeedle == null) return;

        float t = topDownAgentView != null ? topDownAgentView.CurrentSpeed01 : 0f;
        float target = Mathf.Lerp(NeedleRestAngle, NeedleFullAngle, t);

        // Frame-rate-independent ease so the needle settles smoothly rather than snapping.
        _needleAngle = Mathf.Lerp(_needleAngle, target, 1f - Mathf.Exp(-8f * Time.deltaTime));
        gaugeSpeedNeedle.localRotation = Quaternion.Euler(0f, 0f, _needleAngle);
    }

    /// <summary>Advances the speed cycle button to the next preset and updates every
    /// readout (both windows' buttons + the on-screen gauge).</summary>
    void CycleSpeed()
    {
        _speedIndex = (_speedIndex + 1) % SpeedPresets.Length;
        float v = SpeedPresets[_speedIndex];
        SetSpeed(v);

        string text = $"×{v:0.0}";
        text = $"x{v:0.##}";
        if (speedLabel != null)      speedLabel.text      = text;
        if (codeSpeedLabel != null)  codeSpeedLabel.text  = text;
        if (gaugeSpeedLabel != null) gaugeSpeedLabel.text = text;
    }

    // -------------------------------------------------------------------------
    // Self-driving autopilot

    void OnAutopilot()
    {
        if (_won || exec == null) return;

        // Editor-first autopilot: place the reference program where the player can
        // see and review it. The player presses RUN themselves (auto-running here
        // caused redo/complete conflicts), so we only load it.
        if (!LoadAutopilotProgramForCurrentEditor())
            return;
        if (console != null) console.Info("autopilot loaded the route program — press RUN to drive.");
    }

    public bool LoadAutopilotProgramForCurrentEditor()
    {
        string source = !string.IsNullOrWhiteSpace(_def != null ? _def.optimalSolutionText : null)
            ? _def.optimalSolutionText
            : SelfDrivePlanner.ReferenceSolution;

        ProgramNode program = Parser.Compile(source, out List<LangError> errors);
        if (errors.Count > 0)
        {
            if (console != null)
                foreach (LangError error in errors)
                    console.Error(error.ToString());
            return false;
        }

        if (_codeTabActive)
        {
            if (codeEditor == null) return false;
            codeEditor.SetSource(source);
            _lastRunWasCode = true;
            return true;
        }

        if (blockCanvas == null) return false;
        if (!blockCanvas.LoadProgram(program))
        {
            if (console != null)
                console.Warn("autopilot program uses code-only features; loading it in Code Mode instead.");
            if (codeEditor != null) codeEditor.SetSource(source);
            SetTab(true);
            RefreshEditorModeLabel();
            _lastRunWasCode = true;
            return codeEditor != null;
        }

        _lastRunWasCode = false;
        return true;
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
        int authoredCount = conv != null && conv.assistHints != null ? conv.assistHints.Length : 0;

        int idx  = Mathf.Min(_hintTier, Mathf.Max(0, authoredCount - 1));
        string authored = authoredCount > 0
            ? conv.assistHints[idx].text
            : "You're stuck — that's normal. Look at where the jeepney stops and compare it to what your code says.";

        var pax = conv != null ? PassengerLibrary.Get(conv.passengerId) : null;
        if (pax == null) pax = PassengerLibrary.Get("gemma");

        _hintTier = Mathf.Min(_hintTier + 1, Mathf.Max(0, authoredCount - 1));
        StartCoroutine(FetchHint(authored, pax, idx));
    }

    IEnumerator FetchHint(string authoredText, PassengerDefinition pax, int tier)
    {
        // The hint now lives exclusively in the AI vibe-coding chat; clear the legacy
        // fallback label so the same text doesn't appear in two places.
        if (hintLabel != null) hintLabel.text = "";

        if (vibeCtrl == null)
            Debug.LogWarning("[AutomationDrive] Hint requested but vibeCtrl is not wired — hint will not appear in chat.");
        if (pax == null)
            Debug.LogWarning("[AutomationDrive] Hint requested but passenger definition is null — hint will not appear in chat.");

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

        // Open the AI chat and show a pending hint bubble there too.
        TMP_Text vibeHintBubble = vibeCtrl != null && pax != null
            ? vibeCtrl.AddHintBubble(pax.speakerName, pax.role)
            : null;

        yield return GeminiClient.Stream(request, delta =>
        {
            streamed += delta;
            if (vibeHintBubble != null && pax != null)
                vibeCtrl.SetHintBubbleText(vibeHintBubble, streamed, pax.speakerName, pax.role);
        }, completed => result = completed);

        string final = result != null && result.Success ? result.Text : authoredText;
        if (vibeHintBubble != null && pax != null)
            vibeCtrl.SetHintBubbleText(vibeHintBubble, final, pax.speakerName, pax.role);
    }

    // -------------------------------------------------------------------------
    // Execution events

    // Grass under the procedural road so Automation's world matches Manual. Keep
    // it as one simple camera-sized sprite; a tiled sprite grown to the whole
    // endless layout eventually exceeds Unity's generated mesh limits.
    void AddProceduralGround(Transform worldRoot, TownLayout layout)
    {
        if (worldRoot == null)
            return;

        Transform existing = worldRoot.Find("ProceduralGround");
        GameObject go = existing != null ? existing.gameObject : new GameObject("ProceduralGround");
        go.transform.SetParent(worldRoot, false);

        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = Resources.Load<Sprite>("Placeholders/grass_tile");
        sr.drawMode     = SpriteDrawMode.Simple;
        sr.sortingOrder = -100;

        UpdateProceduralGroundPosition();
    }

    void UpdateProceduralGroundPosition()
    {
        if (topDownWorldRoot == null) return;
        Transform ground = topDownWorldRoot.Find("ProceduralGround");
        if (ground == null) return;

        Vector3 center = topDownAgentView != null
            ? topDownAgentView.transform.position
            : worldCamera != null ? worldCamera.transform.position : Vector3.zero;
        ground.position = new Vector3(center.x, center.y, 0f);

        SpriteRenderer sr = ground.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;

        float height = worldCamera != null ? worldCamera.orthographicSize * 2f : 48f;
        float width = worldCamera != null ? height * Mathf.Max(1f, worldCamera.aspect) : 80f;
        float size = Mathf.Max(width, height) + 120f;
        Vector2 spriteSize = sr.sprite.bounds.size;
        float sx = spriteSize.x > 0.01f ? size / spriteSize.x : size;
        float sy = spriteSize.y > 0.01f ? size / spriteSize.y : size;
        ground.localScale = new Vector3(sx, sy, 1f);
    }

    // Automation drains fuel and can break down mid-run, like Manual — the
    // self-driving jeepney pauses for the refuel/repair mini-game, then resumes.
    float _autoFuel = 1f;
    int   _autoRefuelSpent;
    bool  _autoBreakdownActive;

    void AutoFuelTick()
    {
        if (_autoBreakdownActive) return;
        if (refuelMinigame == null && mazeRepairMinigame == null) return;
        if (_interruptionScheduler == null)
            _interruptionScheduler = new DriveInterruptionScheduler(5000 + _levelIndex);

        // Assumes ExecutionController.baseStepSeconds == 1.0 (one step == one simulated
        // second), so this drains at the same per-second rate as Manual Mode regardless
        // of the playback Speed slider (Speed only stretches real-world animation time).
        _autoFuel = Mathf.Max(0f, _autoFuel - RefuelMath.FuelDrainPerSecond);
        RefreshAutomationHud();

        if (_interruptionScheduler.ShouldRefuel(_autoFuel))
            StartCoroutine(AutoBreakdown(fuel: true));
        else if (_interruptionScheduler.TryStartRepair(AutomationProgress01(), UnityEngine.Random.value))
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
                _autoRefuelSpent += spent;
                if (console != null)
                    console.Info($"refuel cost: PHP {spent}");
                RefreshAutomationHud();
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
        RefreshAutomationHud();
    }

    float AutomationProgress01()
    {
        int steps = exec != null && exec.Sim != null ? exec.Sim.StepsUsed : 0;
        int par = _def != null ? Mathf.Max(1, _def.parSteps) : 60;
        return steps / (float)par;
    }

    void RefreshAutomationHud()
    {
        if (automationFuelFill != null)
        {
            automationFuelFill.fillAmount = Mathf.Clamp01(_autoFuel);
            automationFuelFill.color = _autoFuel > 0.25f
                ? new Color(0.95f, 0.65f, 0.15f)
                : new Color(0.9f, 0.2f, 0.15f);
        }

        if (gaugeFuelFill != null)
        {
            gaugeFuelFill.fillAmount = Mathf.Clamp01(_autoFuel);
            gaugeFuelFill.color = _autoFuel > 0.25f
                ? new Color(0.95f, 0.65f, 0.15f)
                : new Color(0.9f, 0.2f, 0.15f);
        }

        if (walletLabel == null) return;

        int saved = SaveSystem.Current != null ? SaveSystem.Current.currency : 0;
        int pending = GameManager.Instance != null ? GameManager.Instance.PendingCurrency : 0;
        int debt = SaveSystem.Current != null ? SaveSystem.Current.debt : 0;
        int wallet = saved + pending;
        walletLabel.text = debt > 0 ? $"₱ {wallet}  debt -{debt}" : $"₱ {wallet}";
    }

    // Drains print() output produced since the last flush into the terminal. The
    // interpreter appends to exec.Output as it runs and clears it on (re)load, so a
    // simple cursor tracks what we've already shown.
    void FlushOutput()
    {
        if (exec == null || console == null) return;
        IReadOnlyList<string> output = exec.Output;
        if (output == null) return;
        for (; _outputCursor < output.Count; _outputCursor++)
            console.Print(output[_outputCursor]);
    }

    void HandleStepDone(AgentActionResult result, StepResult step)
    {
        // Surface any print() output that ran up to this action, in order.
        FlushOutput();

        _lastExecutedLine = step.Node != null ? step.Node.Line : 0;

        if (console != null)
        {
            string prefix = _lastExecutedLine > 0 ? $"line {_lastExecutedLine}: " : "";
            console.Info(prefix + result.Action + "()");
            if (!string.IsNullOrEmpty(result.Warning))
                console.Warn(result.Warning);
            if (result.FareCollected > 0)
                console.Info($"   fare collected: ₱{result.FareCollected}");
            if (result.ChangeGiven > 0)
                console.Info($"   change given: ₱{result.ChangeGiven}");
        }

        if (passengerRibbon != null)
        {
            if (result.BoardedRideIds != null && exec != null && exec.Sim != null && exec.Sim.Rides != null)
            {
                for (int i = 0; i < result.BoardedRideIds.Count; i++)
                {
                    int rideId = result.BoardedRideIds[i];
                    string label = result.BoardedDestLabels[i];
                    Color tint = Color.white;
                    foreach (GridRide ride in exec.Sim.Rides)
                        if (ride.id == rideId) { tint = ride.color; break; }
                    passengerRibbon.Claim(rideId, label, tint);
                }
            }
            if (result.DeliveredRideIds != null)
                foreach (int rideId in result.DeliveredRideIds)
                    passengerRibbon.Release(rideId);
        }

        if (result.FareCollected > 0 && GameManager.Instance != null)
            GameManager.Instance.EarnCurrency(result.FareCollected);

        if (result.FareCollected > 0 || result.ChangeGiven > 0)
            RefreshAutomationHud();

        if (monitor != null && exec != null)
            monitor.Refresh(exec.Sim, _lastExecutedLine);

        if (_codeTabActive && codeEditor != null && step.Node != null)
            codeEditor.HighlightExecutingLine(step.Node.Line);

        if (!_codeTabActive && blockCanvas != null && step.Node != null)
            blockCanvas.HighlightExecuting(step.Node.SourceRef);

        if (codeEditor != null && exec != null)
            codeEditor.SetHeat(exec.LineHits);

        AutoFuelTick();
    }

    void HandleHotLine(int line)
    {
        if (console != null)
            console.Warn($"this looks like an infinite loop near line {line}.");
    }

    void HandleRuntimeError(LangError error)
    {
        FlushOutput();   // show any print() output that preceded the error
        if (codeEditor != null) codeEditor.ClearExecutionHighlight();
        if (blockCanvas != null) blockCanvas.ClearExecutionHighlight();
        if (terminal != null) terminal.Open();
        if (console != null) console.Error(error.ToString());
    }

    void HandleWorldReset()
    {
        _outputCursor = 0;   // exec clears its Output on reload; keep our cursor in step
        if (monitor != null) monitor.ShowIdle();
        if (codeEditor != null) codeEditor.ClearExecutionHighlight();
        if (codeEditor != null) codeEditor.ClearHeat();
        if (blockCanvas != null) blockCanvas.ClearExecutionHighlight();
        if (passengerRibbon != null) passengerRibbon.ReleaseAll();
        if (dulogMarkers != null) dulogMarkers.ClearAll();
        _autoFuel = 1f;
        _autoBreakdownActive = false;
        RefreshAutomationHud();
    }

    void HandleFinished(bool win)
    {
        FlushOutput();   // a print()-only program never hits HandleStepDone, so flush here
        if (codeEditor != null) codeEditor.ClearExecutionHighlight();
        if (blockCanvas != null) blockCanvas.ClearExecutionHighlight();
        string gapForAttempt = null;
        if (!win && exec != null && exec.Sim != null)
            gapForAttempt = exec.Sim.DescribeGoalGap(_def);
        if (_activeRunAttempt != null)
        {
            int steps = exec != null && exec.Sim != null ? exec.Sim.StepsUsed : 0;
            _runHistory.Complete(
                _activeRunAttempt,
                win,
                win ? "Solved" : "Stopped",
                steps,
                win ? $"Solved in {steps} steps." : (gapForAttempt ?? "The program ended without reaching the goal."));
            _activeRunAttempt = null;
        }

        if (win)
        {
            if (_proceduralTopDown && _storyLegShown)
            {
                AppendProceduralRoute(keepProgramRunning: false);
                _won = false;
                if (console != null)
                    console.Info("route complete - another stretch is ready.");
                return;
            }

            _won = true;
            if (_proceduralTopDown) OnStoryDropped();   // a program that ends on routeComplete
            // The leg completes only when the puzzle is solved AND the story passenger's
            // chat is finished. If the chat is still going, hold and nudge once.
            if (!_conversationDone && !_tutorialComplete && !_solvedNudged)
            {
                _solvedNudged = true;
                if (console != null)
                    console.Info($"Solved! Finish your chat with {_storyPassengerName} to wrap up the leg.");
            }
            TryShowStoryComplete($"LEVEL COMPLETE — {_level.displayName}");
            return;
        }

        if (console != null && exec != null)
        {
            string gap = gapForAttempt ?? exec.Sim.DescribeGoalGap(_def);
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
    /// are met. On the endless procedural road (every level + tutorial) completion needs
    /// BOTH the conversation finished AND the jeepney driven to the story drop-off
    /// (<see cref="_won"/>) — so the leg never ends the instant the chat does. Only a
    /// non-procedural tutorial (legacy/safety) completes on its chat alone.
    /// </summary>
    void TryShowStoryComplete(string title)
    {
        if (_storyLegShown) return;
        bool ready = (_tutorialComplete && !_proceduralTopDown)
            ? _conversationDone
            : (_won && _conversationDone);
        if (!ready) return;

        _storyLegShown = true;
        PlayRevealOnReach(() =>
        {
            _freeRoamStoryConsumed = true;
            if (legCompletion != null)
                legCompletion.ShowComplete(
                    title,
                    "Nice driving! You delivered the story.\nKeep exploring the town and pick up more passengers, or finish the leg to bank your run.",
                    allowExplore: _proceduralTopDown);
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

    /// <summary>Keep exploring after the story drop-off: un-freeze the road (it streams
    /// endlessly again), disarm the story drop-off so the leg can't instantly "re-win", and
    /// let the player keep driving/coding. The story dialogue never replays.</summary>
    void OnKeepExploring()
    {
        if (!_proceduralTopDown)
            return;

        _optionalFreeRoam = true;
        _maxChunks = int.MaxValue;                          // resume endless streaming
        _freeRoamStoryConsumed = true;
        if (dialogue != null) dialogue.StopAndHide();
        if (exec != null && exec.Sim != null)
            exec.Sim.StoryDropoffArmed = false;             // leg already done; don't re-complete
        _won = false;
        if (console != null)
            console.Info("keep cruising the endless road - or press Finish leg when you're done.");
    }

    /// <summary>
    /// Appends the next streamed chunk and rebinds the world. When
    /// <paramref name="keepProgramRunning"/> is true (continuous lookahead streaming) the
    /// running program is preserved so the autopilot drives straight into the new stretch;
    /// when false (post-win continue / keep-exploring) the world is reset for a fresh run.
    /// The agent is always re-pinned by its WORLD position, so the grid-origin shift from a
    /// grown layout never teleports it.
    /// </summary>
    void AppendProceduralRoute(bool keepProgramRunning)
    {
        if (_streamingTown == null || exec == null || exec.Sim == null)
            return;
        if (_chunksAppended >= _maxChunks)
            return;

        // Capture the agent's true world position (stable across the grid re-rasterize).
        Vector3 currentWorld = topDownAgentView != null
            ? topDownAgentView.transform.position
            : _topDownSpace != null ? _topDownSpace.CellToWorld(exec.Sim.Position) : Vector3.zero;
        int currentFacing = exec.Sim.Facing;
        IReadOnlyList<GridRide> oldRides = exec.Sim.Rides;

        TownChunk chunk = StreamingTownGenerator.AppendChunk(_streamingTown);
        if (chunk == null || chunk.nodes.Count == 0)
            return;
        _chunksAppended++;
        StreamedChunkView chunkView = CreateChunkView(chunk);

        _def = SelfDrivePlanner.BuildPuzzle(_streamingTown.Layout, _streamingTown.CellSize,
                                            _levelIndex, out List<GridRide> remappedRides, out int newStartFacing);
        TransferRideState(oldRides, remappedRides);
        _rides = remappedRides;
        _startFacing = newStartFacing;

        _grid = GridModel.Parse(_def.gridMap, out List<string> mapErrors);
        foreach (string problem in mapErrors)
            if (console != null) console.Error("map: " + problem);

        AppendProceduralTopDownWorld(chunk, chunkView != null ? chunkView.root : null);
        if (chunkView != null)
        {
            chunkView.SetActive(true);
            _streamedChunkViews.Add(chunkView);
        }

        Vector2Int currentCell = _topDownSpace != null
            ? _topDownSpace.WorldToCell(currentWorld)
            : _grid.DestPos;
        currentCell = NearestWalkable(_grid, currentCell);

        exec.Sim.RebindGrid(_grid, currentCell, currentFacing, _rides);
        if (keepProgramRunning)
            exec.RebindStreamingWorld(_grid, _topDownSpace, _topDownSpace, _def, _startFacing);
        else
            exec.RebindWorld(_grid, _topDownSpace, _topDownSpace, _def, _startFacing);

        // Re-pin the story drop-off cell to its fixed world position (the grid origin shifted),
        // so the marker stays put even though the road kept streaming (no cap needed).
        if (_storyDropoffArmed && !exec.Sim.StoryDelivered && _topDownSpace != null)
            exec.Sim.StoryDropoffCell = NearestWalkable(_grid, _topDownSpace.WorldToCell(_storyDropoffWorld));

        if (goalLabel != null) goalLabel.text = _def.goalText;
        if (vibeCtrl != null) vibeCtrl.SetWorldContext(_grid, exec.Sim, _def);
        if (ghost != null) ghost.Bind(_def);
        if (monitor != null) monitor.Refresh(exec.Sim, _lastExecutedLine);
        RefreshChunkWindow();
    }

    /// <summary>
    /// Incrementally dresses only the freshly streamed chunk's road/buildings (keeping all
    /// existing world objects in place — no destroy-all rebuild, so streaming never lags),
    /// then refreshes the grid space's stop/peep mapping from the grown layout. Falls back
    /// to a full build only if there is no space yet.
    /// </summary>
    void AppendProceduralTopDownWorld(TownChunk chunk, Transform chunkRoot)
    {
        if (topDownWorldRoot == null || _streamingTown == null || _streamingTown.Layout == null)
            return;

        AddProceduralGround(topDownWorldRoot, _streamingTown.Layout);

        float roadHalfWidth = _level.manual != null ? _level.manual.roadHalfWidth : 3f;
        if (_topDownSpace == null || _topDownSpace.RouteContext == null)
        {
            _topDownSpace = new TopDownGridSpace(_streamingTown.Layout, _streamingTown.CellSize,
                                                 roadHalfWidth, topDownWorldRoot);
            return;
        }

        ManualLayoutResult delta = ManualLayoutProjector.ProjectChunk(_streamingTown.Layout, chunk);
        RouteVisualBuilder.AppendProcedural(topDownWorldRoot, _topDownSpace.RouteContext,
                                            delta, roadHalfWidth, chunkRoot);
        _topDownSpace.RefreshFromLayout(_streamingTown.Layout, _rides);
    }

    StreamedChunkView CreateChunkView(TownChunk chunk)
    {
        if (chunk == null || topDownWorldRoot == null) return null;

        var root = new GameObject($"StreamedChunk_{chunk.chunkIndex:000}");
        root.transform.SetParent(topDownWorldRoot, false);

        var view = new StreamedChunkView
        {
            chunkIndex = chunk.chunkIndex,
            generationId = ++_chunkGenerationId,
            root = root.transform,
            minAlong = float.PositiveInfinity,
            maxAlong = float.NegativeInfinity,
        };

        foreach (TownNode node in chunk.nodes)
        {
            if (node == null) continue;
            view.nodeIds.Add(node.id);
            view.minAlong = Mathf.Min(view.minAlong, node.alongTrunk);
            view.maxAlong = Mathf.Max(view.maxAlong, node.alongTrunk);
        }

        if (float.IsInfinity(view.minAlong))
        {
            view.minAlong = 0f;
            view.maxAlong = 0f;
        }
        return view;
    }

    void RefreshChunkWindow()
    {
        if (_streamedChunkViews.Count == 0 || topDownAgentView == null ||
            _topDownSpace == null || _topDownSpace.RouteContext == null ||
            _topDownSpace.RouteContext.Waypoints == null)
            return;

        float currentAlong = RouteMath.NearestDistanceAlong(
            _topDownSpace.RouteContext.Waypoints, topDownAgentView.transform.position, out _);

        int currentChunkIndex = _streamedChunkViews[0].chunkIndex;
        foreach (StreamedChunkView view in _streamedChunkViews)
        {
            if (currentAlong >= view.minAlong)
                currentChunkIndex = view.chunkIndex;
            if (currentAlong <= view.maxAlong)
                break;
        }

        int minChunk = currentChunkIndex - ActiveChunksBehind;
        int maxChunk = currentChunkIndex + ActiveChunksAhead;
        foreach (StreamedChunkView view in _streamedChunkViews)
        {
            bool inWindow = view.chunkIndex >= minChunk && view.chunkIndex <= maxChunk;
            if (!inWindow && HasUnresolvedRideInChunk(view))
                inWindow = true;
            if (view.active != inWindow)
                view.SetActive(inWindow);
        }
    }

    bool HasUnresolvedRideInChunk(StreamedChunkView view)
    {
        if (view == null || _rides == null) return false;
        foreach (GridRide ride in _rides)
        {
            if (ride == null || ride.delivered) continue;
            if (view.nodeIds.Contains(ride.originNodeId) || view.nodeIds.Contains(ride.destNodeId))
                return true;
        }
        return false;
    }

    static void TransferRideState(IReadOnlyList<GridRide> oldRides, List<GridRide> newRides)
    {
        if (oldRides == null || newRides == null) return;

        var oldById = new Dictionary<int, GridRide>();
        foreach (GridRide ride in oldRides)
            oldById[ride.id] = ride;

        foreach (GridRide ride in newRides)
            if (oldById.TryGetValue(ride.id, out GridRide old))
            {
                ride.aboard    = old.aboard;
                ride.delivered = old.delivered;
                ride.fareCollected = old.fareCollected;
                ride.changeSettled = old.changeSettled;
                ride.paid      = old.paid;
                ride.tender    = old.tender;
            }
    }

    static Vector2Int NearestWalkable(GridModel grid, Vector2Int preferred)
    {
        if (grid == null) return preferred;
        if (grid.IsWalkable(preferred)) return preferred;

        int maxRadius = Mathf.Max(grid.Width, grid.Height);
        for (int radius = 1; radius <= maxRadius; radius++)
        {
            for (int y = preferred.y - radius; y <= preferred.y + radius; y++)
            for (int x = preferred.x - radius; x <= preferred.x + radius; x++)
            {
                var p = new Vector2Int(x, y);
                if (grid.IsWalkable(p)) return p;
            }
        }
        return grid.StartPos;
    }

    // -------------------------------------------------------------------------
    // Results

    void BeginResults()
    {
        PlayRevealThenResults();
    }

    /// <summary>Plays the heritage reveal once, on reaching the goal, then invokes onDone.</summary>
    void PlayRevealOnReach(System.Action onDone)
    {
        if (_revealPlayed || dialogue == null) { _revealPlayed = true; onDone(); return; }

        DialogueConversation convo = DialogueLibrary.ForLevel(_levelIndex, manualMode: false,
            blockMode: SaveSystem.Current.settings.blockMode);
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

        DialogueConversation convo = DialogueLibrary.ForLevel(_levelIndex, manualMode: false,
            blockMode: SaveSystem.Current.settings.blockMode);
        if (convo == null || convo.journalPageId < 0 || convo.journalPageId >= JournalPageLibrary.Pages.Count)
        {
            ShowResults();
            return;
        }

        JournalPageDefinition page = JournalPageLibrary.Pages[convo.journalPageId];
        dialogue.PlayReveal(convo, page, ShowResults);
    }

    void ShowResults()
    {
        AgentSim sim = exec.Sim;
        float elapsed = Time.time - _startTime;
        int attemptCount = Mathf.Max(1, _runHistory.Count);
        int retries = Mathf.Max(0, attemptCount - 1);

        int score = ScoreCalculator.AutomationScore(
            sim.StepsUsed, _def.parSteps, elapsed, _def.softTimerSeconds, retries, _lastRunWasCode);

        int bonus = ScoreCalculator.CurrencyFor(score);
        if (GameManager.Instance != null)
            GameManager.Instance.EarnCurrency(bonus);
        RefreshAutomationHud();

        int earned = sim.FaresCollected + bonus;   // display-only; fares already credited per-step

        string playerSolution = _lastRunWasCode
            ? codeEditor.Source
            : blockCanvas.ToSourceText();

        int minutes = (int)(elapsed / 60f);
        int seconds = (int)(elapsed % 60f);
        string stats = $"SCORE {score}   ·   steps {sim.StepsUsed} (par {_def.parSteps})   ·   " +
                       $"time {minutes:0}:{seconds:00}   ·   runs {attemptCount}   ·   retries {retries}   ·   earned ₱{earned}" +
                       (_lastRunWasCode ? "   ·   CODE ×1.5" : "");

        if (console != null) console.Info("goal complete!");

        if (results != null)
        {
            CodeAnalysis analysis = CodeAnalyticsService.Analyze(
                playerSolution, _def.optimalSolutionText, sim.StepsUsed, _def.parSteps,
                retries, elapsed, _def.softTimerSeconds, exec.LineHits, attemptCount);
            results.Show(
                $"LEG COMPLETE  —  {_level.displayName}",
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
                                          : "MAIN GAMEPLAY · Automation (Blocks)",
                attempts: _runHistory.Attempts);

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
            _def.allowedBlocks, _def.allowedQueries, _runHistory.Attempts);
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
