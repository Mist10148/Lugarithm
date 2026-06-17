using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Orchestrates a Manual Mode leg: builds the route world for the selected
/// level, spawns the jeepney, runs the drive (passengers, fares, breakdown),
/// and shows results when the destination stop has been serviced.
/// Runs standalone in the editor too — managers are null-guarded.
/// </summary>
public class ManualDriveController : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector References

    [Header("World")]
    [SerializeField] private JeepneyController jeepney;
    [SerializeField] private CameraFollow2D    cameraFollow;
    [SerializeField] private Transform         worldRoot;

    [Header("UI")]
    [SerializeField] private ManualHudController  hud;
    [SerializeField] private CoinDrawerController coinDrawer;
    [SerializeField] private DriveResultsPanel    resultsPanel;
    [SerializeField] private ToastNotification    toast;

    [Header("Breakdown minigames (randomized)")]
    [SerializeField] private PatternMatchMinigame engineRepairMinigame; // non-code · engine
    [SerializeField] private RefuelMinigame       refuelMinigame;       // non-code · fuel
    [SerializeField] private MazeRepairMinigame   mazeRepairMinigame;   // code · either (escape a maze)

    [Header("Town gate (non-code, required to advance)")]
    [SerializeField] private FlowConnectMinigame  flowPuzzle;
    [SerializeField] private CrateStackMinigame   cratePuzzle;

    [Header("Dialogue")]
    [SerializeField] private DialogueController dialogue;

    [Header("Leg completion")]
    [SerializeField] private LegCompletionController legCompletion;

    [Header("Procedural town")]
    [SerializeField] private DulogMarkerController dulogMarkers;
    [Tooltip("Seed for the per-run town; negative = fresh random each play.")]
    [SerializeField] private int proceduralSeed = -1;

    // -------------------------------------------------------------------------

    LevelDefinition   _def;
    int               _levelIndex;
    RouteContext      _ctx;
    bool              _proceduralActive;
    List<RoadSegment> _segments;
    Dictionary<int, List<PassengerManager.PendingBoard>> _pendingBoarding;
    DriveScoreTracker _tracker;
    PassengerManager  _passengers;
    BreakdownController _breakdown;
    StreamingTown     _streaming;
    float _startTime;
    float _legElapsed;
    bool  _finished;

    const float StreamLookAhead = 45f;

    void Start()
    {
        // Resolve the selected level (Tutorial fallback keeps the scene
        // playable when launched directly in the editor).
        _levelIndex = GameManager.Instance != null ? GameManager.Instance.SelectedLevelIndex : 0;
        _def = LevelLibrary.Get(_levelIndex);
        if (!_def.hasContent)
        {
            _levelIndex = 0;
            _def = LevelLibrary.Get(0);
        }

        if (GameManager.Instance != null)
            GameManager.Instance.PendingCurrency = 0;   // fresh leg ledger

        // World — procedural town when the level opts in, else the authored route.
        Transform root = worldRoot != null ? worldRoot : transform;
        _proceduralActive = _def.procedural != null && _def.procedural.enabled;

        Vector2[] driveLine;
        if (_proceduralActive)
        {
            int seed = proceduralSeed >= 0
                ? proceduralSeed
                : (System.Guid.NewGuid().GetHashCode() & 0x7fffffff);
            _streaming = StreamingTownGenerator.Begin(_def.procedural, _def.fares, seed);
            TownLayout layout = _streaming.Layout;
            ManualLayoutResult projected = ManualLayoutProjector.Project(layout);

            _ctx       = RouteVisualBuilder.BuildProcedural(root, projected, _def.manual.roadHalfWidth);
            _segments  = _ctx.Segments;
            driveLine  = projected.trunk;

            BuildProceduralPassengers(layout, projected);
        }
        else
        {
            _ctx      = RouteVisualBuilder.Build(root, _def.manual);
            driveLine = _def.manual.waypoints;
        }

        Vector2 start     = driveLine[0];
        Vector2 direction = RouteMath.DirectionAt(driveLine, 0.1f);
        float angle = Vector2.SignedAngle(Vector2.up, direction);
        jeepney.TeleportTo(start, angle);
        jeepney.SetDriveLine(driveLine);

        if (cameraFollow != null)
            cameraFollow.SnapTo(jeepney.transform);

        // Systems
        _tracker = new DriveScoreTracker();

        if (hud != null) hud.Init(jeepney);
        if (coinDrawer != null) coinDrawer.Init(_tracker, toast);

        _passengers = GetComponent<PassengerManager>();
        if (_passengers != null)
        {
            _passengers.SetFareTable(_def.fares);
            _passengers.Init(jeepney, coinDrawer, hud, toast, _tracker, _def.manual, _ctx,
                             seed: 1000 + _levelIndex);
            if (_proceduralActive)
                _passengers.ConfigureProcedural(_pendingBoarding);
        }

        _breakdown = GetComponent<BreakdownController>();
        if (_breakdown != null)
            _breakdown.Init(jeepney, engineRepairMinigame, refuelMinigame, mazeRepairMinigame,
                            toast, _tracker,
                            _ctx.TotalLength, _def.manual.breakdownAtRouteFraction);

        _startTime = Time.time;

        if (toast != null && _ctx.DestinationZone != null)
            toast.Show($"{_def.displayName}:  drive to {_ctx.DestinationZone.StopName} — stop at the signs for passengers");

        if (legCompletion != null)
            legCompletion.OnFinishPressed += Finish;

        PlayBoardingDialogue();
    }

    /// <summary>
    /// Turns the layout's committed rides into a per-stop boarding plan and spawns
    /// waiting peeps tinted to match each rider (peep ↔ ribbon chip ↔ dulog marker).
    /// </summary>
    void BuildProceduralPassengers(TownLayout layout, ManualLayoutResult projected)
    {
        _pendingBoarding = new Dictionary<int, List<PassengerManager.PendingBoard>>();

        var ordinalOf = new Dictionary<int, int>();
        for (int i = 0; i < projected.stops.Count; i++)
            ordinalOf[projected.stops[i].id] = i;

        foreach (PassengerRequest req in layout.requests)
        {
            if (!ordinalOf.TryGetValue(req.originNodeId, out int o)) continue;
            if (!ordinalOf.TryGetValue(req.destNodeId, out int d)) continue;

            if (!_pendingBoarding.TryGetValue(o, out List<PassengerManager.PendingBoard> lst))
            {
                lst = new List<PassengerManager.PendingBoard>();
                _pendingBoarding[o] = lst;
            }
            lst.Add(new PassengerManager.PendingBoard
            {
                color       = req.color,
                destOrdinal = d,
                destName    = projected.stops[d].name,
                fare        = req.fare,
                tender      = req.tender,
            });
        }

        foreach (var kv in _pendingBoarding)
        {
            StopZone zone = _ctx.Zones[kv.Key];
            var colors = new List<Color>();
            foreach (PassengerManager.PendingBoard b in kv.Value) colors.Add(b.color);
            zone.SpawnWaitingPeeps(colors,
                new Vector2(_def.manual.roadHalfWidth + 2.1f, -0.8f), Vector2.right);
        }
    }

    /// <summary>
    /// Adds the new chunk's committed rides to the pending boarding plan and
    /// spawns tinted waiting peeps at the new stops.
    /// </summary>
    void MergeProceduralPassengers(TownLayout layout, ManualLayoutResult delta, TownChunk chunk)
    {
        if (_pendingBoarding == null) _pendingBoarding = new Dictionary<int, List<PassengerManager.PendingBoard>>();

        var ordinalOf = new Dictionary<int, int>();
        for (int i = 0; i < delta.stops.Count; i++)
            ordinalOf[delta.stops[i].id] = i;

        foreach (PassengerRequest req in chunk.requests)
        {
            if (!ordinalOf.TryGetValue(req.originNodeId, out int o)) continue;
            if (!ordinalOf.TryGetValue(req.destNodeId, out int d)) continue;

            if (!_pendingBoarding.TryGetValue(o, out List<PassengerManager.PendingBoard> lst))
            {
                lst = new List<PassengerManager.PendingBoard>();
                _pendingBoarding[o] = lst;
            }
            lst.Add(new PassengerManager.PendingBoard
            {
                color       = req.color,
                destOrdinal = d,
                destName    = delta.stops[d].name,
                fare        = req.fare,
                tender      = req.tender,
            });
        }

        foreach (var kv in _pendingBoarding)
        {
            if (kv.Key >= _ctx.Zones.Length) continue;
            StopZone zone = _ctx.Zones[kv.Key];
            var colors = new List<Color>();
            foreach (PassengerManager.PendingBoard b in kv.Value) colors.Add(b.color);
            zone.SpawnWaitingPeeps(colors,
                new Vector2(_def.manual.roadHalfWidth + 2.1f, -0.8f), Vector2.right);
        }

        if (_passengers != null)
            _passengers.ConfigureProcedural(_pendingBoarding);
    }

    void PlayBoardingDialogue()
    {
        if (dialogue == null) return;

        DialogueConversation convo = DialogueLibrary.ForLevel(_levelIndex, manualMode: true);
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

            // For the working seam, other gameplay events are acknowledged and the
            // dialogue resumes after a short pause. The full wiring (driving/fare
            // tutorials, breakdown pauses) is left open for the next pass.
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
    /// Falls back gracefully — and always resumes — if a panel is missing.
    /// </summary>
    void ShowTutorialMinigame(bool repair)
    {
        if (jeepney != null) jeepney.InputLocked = true;
        int seed = Random.Range(0, 99999);

        System.Action<MinigameResult> onDone = _ =>
        {
            if (jeepney != null) jeepney.InputLocked = false;
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

    void Update()
    {
        if (_finished || _ctx == null) return;

        // Route progress drives off-road detection and the breakdown trigger.
        // Progress (and the breakdown point) is always measured along the trunk;
        // off-road, for the procedural town, is distance to the nearest road
        // segment so a short detour onto a branch stub still counts as on-road.
        float offRoute;
        float along = RouteMath.NearestDistanceAlong(_ctx.Waypoints, jeepney.transform.position, out offRoute);
        if (_proceduralActive && _segments != null)
            offRoute = RouteMath.NearestDistanceToGraph(_segments, jeepney.transform.position);
        jeepney.OffRoad = offRoute > _def.manual.roadHalfWidth + 0.8f;

        if (_breakdown != null)
            _breakdown.Tick(along);

        // Leg ends when the player presses "Finish leg" after the destination
        // has been serviced and every fare is settled.
        bool drawerBusy = coinDrawer != null && coinDrawer.Busy;
        bool servicing  = _passengers != null && _passengers.IsServicing;
        bool arrived    = _passengers != null && _passengers.ArrivedAtDestination;

        if (arrived && !drawerBusy && !servicing)
        {
            if (legCompletion != null && !legCompletion.IsVisible)
                legCompletion.Show();
        }

        // Stream more road ahead when the jeepney approaches the current frontier.
        if (_proceduralActive && _streaming != null && !_finished)
        {
            float distToEnd = Vector2.Distance(jeepney.transform.position, _streaming.TrunkEndPos);
            if (distToEnd < StreamLookAhead)
                AppendChunk();
        }
    }

    void AppendChunk()
    {
        if (_streaming == null || _ctx == null) return;

        TownChunk chunk = StreamingTownGenerator.AppendChunk(_streaming);
        if (chunk == null || chunk.nodes.Count == 0) return;

        Transform root = worldRoot != null ? worldRoot : transform;
        ManualLayoutResult delta = ManualLayoutProjector.ProjectChunk(_streaming.Layout, chunk);
        RouteVisualBuilder.AppendProcedural(root, _ctx, delta, _def.manual.roadHalfWidth);
        _segments = _ctx.Segments;
        if (jeepney != null)
            jeepney.SetDriveLine(_ctx.Waypoints, preserveLane: true);

        MergeProceduralPassengers(_streaming.Layout, delta, chunk);

        // The destination moved, so the passenger manager can keep going.
        if (_passengers != null)
            _passengers.ResetDestinationArrival();

        if (toast != null && _ctx.DestinationZone != null)
            toast.Show($"Keep driving to {_ctx.DestinationZone.StopName}");
    }

    void Finish()
    {
        if (_finished) return;
        _finished = true;
        jeepney.InputLocked = true;
        if (legCompletion != null) legCompletion.Hide();
        _legElapsed = Time.time - _startTime;   // freeze drive time before the gate

        // The town gate (non-code Mini-Game 2) must be solved before results.
        bool shown = ShowTownGate(2000 + _levelIndex, result =>
        {
            _tracker.AddSatisfaction(result.Score);   // fold the gate score into the leg
            PlayRevealThenResults();
        });

        if (shown)
        {
            if (toast != null)
                toast.Show($"Arrived at {_def.displayName} — clear the gate to finish the leg.");
        }
        else
        {
            PlayRevealThenResults();
        }
    }

    void PlayRevealThenResults()
    {
        if (dialogue == null)
        {
            ShowResults();
            return;
        }

        DialogueConversation convo = DialogueLibrary.ForLevel(_levelIndex, manualMode: true);
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
        switch (_def.townPuzzle)
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
        int score = _tracker.ComputeScore(_legElapsed, _def.manual.parTimeSeconds);
        int bonus = ScoreCalculator.CurrencyFor(score);

        if (GameManager.Instance != null)
            GameManager.Instance.PendingCurrency += bonus;

        int earnedTotal = GameManager.Instance != null
            ? GameManager.Instance.PendingCurrency
            : bonus;

        if (resultsPanel != null)
        {
            resultsPanel.Show(
                $"LEG COMPLETE  —  {_def.displayName}",
                _tracker.BuildBreakdownText(_legElapsed),
                score, earnedTotal,
                onContinue: () =>
                {
                    if (GameManager.Instance != null)
                        GameManager.Instance.CompleteLevel(_levelIndex, score);
                    if (BadgeUnlockManager.Instance != null)
                        BadgeUnlockManager.Instance.Show(_levelIndex, () => LoadScene("LevelSelect"));
                    else
                        LoadScene("LevelSelect");
                },
                onReplay: () => LoadScene("ManualDrive"));
        }
        else
        {
            LoadScene("LevelSelect");
        }
    }

    void OnDestroy()
    {
        if (legCompletion != null)
            legCompletion.OnFinishPressed -= Finish;

        // Bank any collected fares on any exit — Continue already flushed them,
        // so a second call after completion is a harmless no-op.
        if (GameManager.Instance != null)
            GameManager.Instance.SaveProgress();
    }

    void LoadScene(string sceneName)
    {
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.TransitionTo(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }
}
