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
    [SerializeField] private CodeFixMinigame      codeFixMinigame;      // code · either

    [Header("Town gate (non-code, required to advance)")]
    [SerializeField] private FlowConnectMinigame  flowPuzzle;
    [SerializeField] private CrateStackMinigame   cratePuzzle;

    // -------------------------------------------------------------------------

    LevelDefinition   _def;
    int               _levelIndex;
    RouteContext      _ctx;
    DriveScoreTracker _tracker;
    PassengerManager  _passengers;
    BreakdownController _breakdown;
    float _startTime;
    float _legElapsed;
    bool  _finished;

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

        // World
        _ctx = RouteVisualBuilder.Build(worldRoot != null ? worldRoot : transform, _def.manual);

        Vector2 start     = _def.manual.waypoints[0];
        Vector2 direction = RouteMath.DirectionAt(_def.manual.waypoints, 0.1f);
        float angle = Vector2.SignedAngle(Vector2.up, direction);
        jeepney.TeleportTo(start, angle);

        if (cameraFollow != null)
            cameraFollow.SnapToWorld(IsoProjection.Project(start));

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
        }

        _breakdown = GetComponent<BreakdownController>();
        if (_breakdown != null)
            _breakdown.Init(jeepney, engineRepairMinigame, refuelMinigame, codeFixMinigame,
                            toast, _tracker,
                            _ctx.TotalLength, _def.manual.breakdownAtRouteFraction);

        _startTime = Time.time;

        if (toast != null && _ctx.DestinationZone != null)
            toast.Show($"{_def.displayName}:  drive to {_ctx.DestinationZone.StopName} — stop at the signs for passengers");
    }

    // -------------------------------------------------------------------------

    void Update()
    {
        if (_finished || _ctx == null) return;

        // Route progress drives off-road detection and the breakdown trigger.
        float offRoute;
        float along = RouteMath.NearestDistanceAlong(_ctx.Waypoints, jeepney.transform.position, out offRoute);
        jeepney.OffRoad = offRoute > _def.manual.roadHalfWidth + 0.8f;

        if (_breakdown != null)
            _breakdown.Tick(along);

        // Leg ends once the destination was serviced and every fare is settled.
        bool drawerBusy = coinDrawer != null && coinDrawer.Busy;
        bool servicing  = _passengers != null && _passengers.IsServicing;
        bool arrived    = _passengers != null && _passengers.ArrivedAtDestination;

        if (arrived && !drawerBusy && !servicing)
            Finish();
    }

    void Finish()
    {
        _finished = true;
        jeepney.InputLocked = true;
        _legElapsed = Time.time - _startTime;   // freeze drive time before the gate

        // The town gate (non-code Mini-Game 2) must be solved before results.
        bool shown = ShowTownGate(2000 + _levelIndex, result =>
        {
            _tracker.AddSatisfaction(result.Score);   // fold the gate score into the leg
            ShowResults();
        });

        if (shown)
        {
            if (toast != null)
                toast.Show($"Arrived at {_def.displayName} — clear the gate to finish the leg.");
        }
        else
        {
            ShowResults();
        }
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
                    LoadScene("LevelSelect");
                },
                onReplay: () => LoadScene("ManualDrive"));
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
