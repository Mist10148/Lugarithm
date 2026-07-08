using System.Collections.Generic;
using TMPro;
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
    [SerializeField] private RoadTrafficController traffic;

    [Header("UI")]
    [SerializeField] private ManualHudController  hud;
    [SerializeField] private CoinDrawerController coinDrawer;
    [SerializeField] private DriveResultsPanel    resultsPanel;
    [SerializeField] private ToastNotification    toast;

    [Header("Breakdown minigames (randomized)")]
    [SerializeField] private PatternMatchMinigame engineRepairMinigame; // non-code · engine
    [SerializeField] private RefuelMinigame       refuelMinigame;       // non-code · fuel
    [SerializeField] private MazeRepairMinigame   mazeRepairMinigame;   // code · either (escape a maze)

    [Header("Dialogue")]
    [SerializeField] private DialogueController dialogue;

    [Header("Front-seat story passenger")]
    [SerializeField] private GameObject frontSeatCard;   // small HUD card: who's riding up front
    [SerializeField] private TMP_Text   frontSeatLabel;

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
    readonly HashSet<int> _peepsSpawned = new HashSet<int>();   // stop ordinals that already have waiting peeps
    DriveScoreTracker _tracker;
    PassengerManager  _passengers;
    BreakdownController _breakdown;
    StreamingTown     _streaming;
    int               _maxChunks;        // int.MaxValue = endless streaming for free-roam
    int               _chunksAppended;
    int               _chunkGenerationId;
    readonly List<StreamedChunkView> _streamedChunkViews = new List<StreamedChunkView>();
    float _startTime;
    float _legElapsed;
    bool  _finished;
    bool  _storyComplete;   // latched true on first delivery; "Finish leg" then shows permanently
    bool  _revealPlayed;    // the heritage reveal plays once, on delivery (not after the gate)
    bool  _tutorialComplete; // tutorial is dialogue-driven: ending the story completes the leg
    bool  _conversationDone; // story passenger's chat finished — a completion gate (with arrival)
    bool  _destinationFinalized; // chat ended → capped streaming so a real drop-off terminal exists
    bool  _arrivedNudged;    // showed the "finish your chat" nudge once on early arrival
    bool  _boardingPlayed;   // boarding/story dialogue has played this leg (never replays)
    bool  _freeRoamStoryConsumed; // story/reveal finished; free-roam must never restart dialogue
    bool    _storyDropoffArmed;   // a drop-off marker has been placed (dialogue ended)
    Vector2 _storyDropoffWorld;   // fixed world position of the story drop-off
    GameObject _storyMarker;      // distinct on-road marker at the drop-off
    string _storyPassengerName = "Your passenger";

    const float StoryDropoffBufferUnits   = 14f;   // how far ahead the drop-off is placed
    const float StoryDropoffRadius        = 3.5f;  // how close counts as "arrived" at it
    const float StoryDropoffBufferSeconds = 2.5f;  // beat of driving after the chat before it shows

    const float StreamLookAhead = 45f;
    const int ActiveChunksBehind = 2;
    const int ActiveChunksAhead = 6;

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
            _streaming.EndlessNoTerminal = true;   // no "Destination" end sign — the road never ends
            _maxChunks = int.MaxValue;   // endless streaming; the leg ends at the story drop-off, not a terminal
            TownLayout layout = _streaming.Layout;
            // Demote the authored terminal so no end-sign shows; the story passenger is dropped
            // at a marked stop along the way instead (armed when the dialogue ends).
            TownNode initialDest = layout.Node(layout.destNodeId);
            if (initialDest != null) initialDest.kind = NodeKind.Junction;
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
        if (traffic != null)
            traffic.InitManual(_ctx, root, jeepney.transform, jeepney);

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
                            _ctx.TotalLength, _def.manual.breakdownAtRouteFraction,
                            automaticInterruptionsEnabled: _levelIndex != 0);

        _startTime = Time.time;

        if (toast != null && _ctx.DestinationZone != null)
            toast.Show($"{_def.displayName}:  drive to {_ctx.DestinationZone.StopName} — stop at the signs for passengers");

        if (legCompletion != null)
        {
            legCompletion.OnFinishPressed += Finish;
            legCompletion.OnKeepExploring += OnKeepExploring;
        }

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
            _peepsSpawned.Add(kv.Key);
        }
    }

    /// <summary>
    /// Adds the new chunk's committed rides to the pending boarding plan and
    /// spawns tinted waiting peeps at the new stops.
    /// </summary>
    void MergeProceduralPassengers(TownChunk chunk)
    {
        if (_pendingBoarding == null) _pendingBoarding = new Dictionary<int, List<PassengerManager.PendingBoard>>();
        if (_ctx == null || _ctx.ZoneByNode == null) return;

        // AppendProcedural has already spawned this chunk's zones, so resolve each
        // ride's town nodes to their *global* stop ordinal (index into _ctx.Zones,
        // == StopZone.StopIndex). The old code used the chunk-local delta.stops
        // index, which doesn't line up with the zone array — peeps and dulog
        // targets would land on the wrong stops.
        var newOrigins = new HashSet<int>();
        foreach (PassengerRequest req in chunk.requests)
        {
            if (!_ctx.ZoneByNode.TryGetValue(req.originNodeId, out StopZone originZone)) continue;
            if (!_ctx.ZoneByNode.TryGetValue(req.destNodeId,   out StopZone destZone))   continue;

            int o = originZone.StopIndex;
            if (!_pendingBoarding.TryGetValue(o, out List<PassengerManager.PendingBoard> lst))
            {
                lst = new List<PassengerManager.PendingBoard>();
                _pendingBoarding[o] = lst;
            }
            lst.Add(new PassengerManager.PendingBoard
            {
                color       = req.color,
                destOrdinal = destZone.StopIndex,
                destName    = destZone.StopName,
                fare        = req.fare,
                tender      = req.tender,
            });
            newOrigins.Add(o);
        }

        // Spawn waiting peeps only for the stops this chunk introduced. Looping
        // over all of _pendingBoarding (as before) re-spawned a fresh duplicate
        // set on every earlier stop each append, inflating WaitingCount and
        // stacking sprites. The _peepsSpawned guard makes this idempotent.
        foreach (int o in newOrigins)
        {
            if (o < 0 || o >= _ctx.Zones.Length) continue;
            if (!_peepsSpawned.Add(o)) continue;
            StopZone zone = _ctx.Zones[o];
            var colors = new List<Color>();
            foreach (PassengerManager.PendingBoard b in _pendingBoarding[o]) colors.Add(b.color);
            zone.SpawnWaitingPeeps(colors,
                new Vector2(_def.manual.roadHalfWidth + 2.1f, -0.8f), Vector2.right);
        }

        if (_passengers != null)
            _passengers.ConfigureProcedural(_pendingBoarding);
    }

    void PlayBoardingDialogue()
    {
        // Play the story conversation exactly once per leg — free-roam and streaming must
        // never replay it.
        if (_boardingPlayed || _freeRoamStoryConsumed)
        {
            Debug.LogWarning("[Manual] PlayBoardingDialogue re-entry blocked (dialogue already played).");
            return;
        }
        _boardingPlayed = true;

        // No conversation for this leg → nothing to finish talking about; let arrival
        // alone complete the leg (don't deadlock the conversation gate).
        if (dialogue == null) { _conversationDone = true; return; }

        DialogueConversation convo = DialogueLibrary.ForLevel(_levelIndex, manualMode: true);
        if (convo == null) { _conversationDone = true; return; }

        // The front-seat story passenger we're carrying + conversing with this leg.
        PassengerDefinition pax = PassengerLibrary.Get(convo.passengerId);
        if (pax != null && !string.IsNullOrEmpty(pax.displayName)) _storyPassengerName = pax.displayName;
        ShowFrontSeatCard(pax);

        dialogue.OnEvent += HandleDialogueEvent;
        dialogue.Play(convo, () =>
        {
            dialogue.OnEvent -= HandleDialogueEvent;

            // Conversation reached its end ("…we're almost there"): the player is done
            // talking. For story levels this is one of two completion gates — the other
            // is delivering the passenger (ArrivedAtDestination), checked in Update().
            _conversationDone = true;

            // Now the chat's over, lock in a concrete drop-off terminal so the
            // endless procedural stream stops receding and the leg can actually end.
            FinalizeStoryDestination();

            // On the endless procedural road (every level + tutorial) the leg ends only
            // after driving the story passenger to their drop-off (handled by the
            // arrival gate in Update) — finishing the chat alone must not end it. Only a
            // non-procedural tutorial (legacy/safety) completes on its chat alone.
            if (_tutorialComplete && !_proceduralActive && !_storyComplete)
            {
                _storyComplete = true;
                OnStoryComplete();
            }
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
    /// Falls back gracefully — and always resumes — if a panel is missing.
    /// </summary>
    void ShowTutorialMinigame(bool repair)
    {
        if (jeepney != null) jeepney.InputLocked = true;
        int seed = Random.Range(0, 99999);

        System.Action<MinigameResult> onDone = _ =>
        {
            if (!repair && jeepney != null) jeepney.Refuel();
            if (jeepney != null) jeepney.InputLocked = false;
            if (dialogue != null) dialogue.ResumeAfterEvent();
        };

        if (repair && mazeRepairMinigame != null)
            mazeRepairMinigame.ShowSimpleRepair(BreakdownFault.Engine, seed, onDone);
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

    /// <summary>Shows the persistent front-seat card naming the story passenger we're
    /// carrying + conversing with this leg.</summary>
    void ShowFrontSeatCard(PassengerDefinition pax)
    {
        if (frontSeatCard == null) return;
        if (pax == null) { frontSeatCard.SetActive(false); return; }

        if (frontSeatLabel != null)
            frontSeatLabel.text = $"<size=70%>FRONT SEAT</size>\n{_storyPassengerName}";
        frontSeatCard.SetActive(true);
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

        // The story passenger alights at their marked drop-off (placed when the chat ended),
        // like a normal NPC: once the jeepney reaches the marker AND the chat is finished, the
        // front-seat card disappears and the leg completes. The road keeps streaming endlessly.
        bool drawerBusy = coinDrawer != null && coinDrawer.Busy;
        bool servicing  = _passengers != null && _passengers.IsServicing;
        if (_storyDropoffArmed && !_storyComplete && !drawerBusy && !servicing)
        {
            bool atDrop = Vector2.Distance((Vector2)jeepney.transform.position, _storyDropoffWorld)
                          <= StoryDropoffRadius;
            if (atDrop && _conversationDone)
            {
                _storyComplete = true;
                ShowFrontSeatCard(null);                                  // passenger has alighted
                if (_storyMarker != null) _storyMarker.SetActive(false);
                OnStoryComplete();
            }
            else if (atDrop && !_conversationDone && !_arrivedNudged)
            {
                _arrivedNudged = true;
                if (toast != null)
                    toast.Show($"{_storyPassengerName} still has more to say — finish your chat.");
            }
        }

        // Once the story is complete the Finish button stays up permanently,
        // even while free-roaming past the delivered terminal.
        if (_storyComplete && _revealPlayed && legCompletion != null && !legCompletion.IsVisible)
            legCompletion.Show();

        // Stream more road ahead when the jeepney approaches the current frontier — but not
        // while a minigame is up (the whole drive freezes behind the modal, jeepney included).
        bool minigameActive = _breakdown != null && _breakdown.InProgress;
        if (_proceduralActive && _streaming != null && !_finished && !minigameActive)
        {
            float distToEnd = Vector2.Distance(jeepney.transform.position, _streaming.TrunkEndPos);
            if (distToEnd < StreamLookAhead)
                AppendChunk();
        }

        if (_proceduralActive)
            RefreshChunkWindow();
    }

    void AppendChunk()
    {
        if (_streaming == null || _ctx == null) return;
        if (_chunksAppended >= _maxChunks) return;   // cap reached → destination is final, leg can finish
        _chunksAppended++;

        TownChunk chunk = StreamingTownGenerator.AppendChunk(_streaming);
        if (chunk == null || chunk.nodes.Count == 0) return;
        StreamedChunkView chunkView = CreateChunkView(chunk);

        Transform root = worldRoot != null ? worldRoot : transform;
        ManualLayoutResult delta = ManualLayoutProjector.ProjectChunk(_streaming.Layout, chunk);
        RouteVisualBuilder.AppendProcedural(root, _ctx, delta, _def.manual.roadHalfWidth,
                                            chunkView != null ? chunkView.root : null);
        if (chunkView != null)
        {
            chunkView.SetActive(true);
            _streamedChunkViews.Add(chunkView);
        }
        _segments = _ctx.Segments;
        if (jeepney != null)
            jeepney.SetDriveLine(_ctx.Waypoints, preserveLane: true);
        if (traffic != null)
            traffic.RebindRoute(_ctx);

        MergeProceduralPassengers(chunk);

        // The destination moved, so the passenger manager can keep going.
        if (_passengers != null)
            _passengers.ResetDestinationArrival();

        if (toast != null && _ctx.DestinationZone != null)
            toast.Show($"Keep driving to {_ctx.DestinationZone.StopName}");
        RefreshChunkWindow();
    }

    StreamedChunkView CreateChunkView(TownChunk chunk)
    {
        if (chunk == null) return null;
        Transform rootParent = worldRoot != null ? worldRoot : transform;
        var root = new GameObject($"StreamedChunk_{chunk.chunkIndex:000}");
        root.transform.SetParent(rootParent, false);

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
        if (_streamedChunkViews.Count == 0 || jeepney == null || _ctx == null || _ctx.Waypoints == null)
            return;

        float currentAlong = RouteMath.NearestDistanceAlong(_ctx.Waypoints, jeepney.transform.position, out _);
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
            if (!inWindow && HasPendingRideInChunk(view))
                inWindow = true;
            if (view.active != inWindow)
                view.SetActive(inWindow);
        }
    }

    bool HasPendingRideInChunk(StreamedChunkView view)
    {
        if (view == null || _pendingBoarding == null || _ctx == null || _ctx.ZoneByNode == null)
            return false;
        foreach (KeyValuePair<int, StopZone> pair in _ctx.ZoneByNode)
        {
            if (!view.nodeIds.Contains(pair.Key)) continue;
            StopZone zone = pair.Value;
            if (zone != null && _pendingBoarding.TryGetValue(zone.StopIndex, out List<PassengerManager.PendingBoard> pending) &&
                pending != null && pending.Count > 0)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Called once the front-seat character's chat ends. Marks the story passenger's drop-off
    /// a short distance ahead on the road and pins it to a fixed world position — the road keeps
    /// streaming forever (no cap, no end terminal). The passenger alights there like any NPC
    /// (Update's proximity check), which hides the front-seat card and completes the leg.
    /// </summary>
    void FinalizeStoryDestination()
    {
        if (_destinationFinalized) return;
        _destinationFinalized = true;

        if (_proceduralActive && _ctx != null && _ctx.Waypoints != null && jeepney != null)
        {
            // Buffer: keep driving a beat after the chat, THEN show the drop-off ahead.
            if (toast != null)
                toast.Show($"“Para!”  {_storyPassengerName}'s stop is coming up…");
            StartCoroutine(ArmStoryDropoffAfterBuffer());
        }
    }

    /// <summary>Waits a short buffer after the dialogue, then places the story drop-off marker
    /// ahead of the jeepney's CURRENT position (so it's always ahead) and shows it.</summary>
    System.Collections.IEnumerator ArmStoryDropoffAfterBuffer()
    {
        yield return new WaitForSeconds(StoryDropoffBufferSeconds);
        if (_storyComplete || _ctx == null || _ctx.Waypoints == null || jeepney == null) yield break;

        float along = RouteMath.NearestDistanceAlong(_ctx.Waypoints, jeepney.transform.position, out _);
        _storyDropoffWorld = RouteMath.PointAt(_ctx.Waypoints, along + StoryDropoffBufferUnits);
        _storyDropoffArmed = true;
        SpawnStoryMarker(_storyDropoffWorld);
        if (toast != null)
            toast.Show($"Drop {_storyPassengerName} at the marked stop ahead.");
    }

    /// <summary>Spawns (or repositions) the distinct on-road marker at the story drop-off.</summary>
    void SpawnStoryMarker(Vector2 world)
    {
        Transform root = worldRoot != null ? worldRoot : transform;
        if (_storyMarker == null)
        {
            _storyMarker = new GameObject("StoryDropoffMarker");
            _storyMarker.transform.SetParent(root, false);
            var sr = _storyMarker.AddComponent<SpriteRenderer>();
            var tex = Texture2D.whiteTexture;
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                      new Vector2(0.5f, 0.5f), tex.width);
            sr.color = new Color(0.30f, 0.85f, 0.95f, 1f);   // bright cyan beacon
            sr.sortingOrder = 8;
            _storyMarker.transform.localScale = new Vector3(2f, 2f, 1f);
        }
        _storyMarker.transform.position = new Vector3(world.x, world.y, 0f);
        _storyMarker.SetActive(true);
    }

    /// <summary>
    /// The story spine just ended (player delivered to the terminal). Play the
    /// heritage reveal inline, then hand control back for optional free-roam —
    /// the latched "Finish leg" button lets them wrap up whenever they want.
    /// </summary>
    void OnStoryComplete()
    {
        System.Action onDone = () =>
        {
            _revealPlayed = true;
            _freeRoamStoryConsumed = true;
            // Congratulate, then let the player choose: keep free-roaming the
            // endless procedural town, or finish the leg. Input stays locked under
            // the card (PassengerManager locks it at the destination) and is only
            // released by the "Keep exploring" choice (OnKeepExploring).
            if (legCompletion != null)
                legCompletion.ShowComplete(
                    $"LEVEL COMPLETE — {_def.displayName}",
                    "Nice driving! You delivered the story.\nKeep exploring the town and pick up more passengers, or finish the leg to bank your run.",
                    allowExplore: true);
            else if (jeepney != null)
                jeepney.InputLocked = false;   // no panel wired — fall back to free-roam
        };

        if (dialogue == null) { onDone(); return; }

        DialogueConversation convo = DialogueLibrary.ForLevel(_levelIndex, manualMode: true);
        if (convo == null || convo.journalPageId < 0 || convo.journalPageId >= JournalPageLibrary.Pages.Count)
        {
            onDone();
            return;
        }

        JournalPageDefinition page = JournalPageLibrary.Pages[convo.journalPageId];
        dialogue.PlayReveal(convo, page, onDone);
    }

    /// <summary>Player chose to keep driving the free-roam town from the completion
    /// card — release the controls (kept locked under the card) and leave the small
    /// "Finish leg" button up for whenever they're done.</summary>
    void OnKeepExploring()
    {
        if (jeepney != null) jeepney.InputLocked = false;
        _freeRoamStoryConsumed = true;
        if (dialogue != null) dialogue.StopAndHide();

        // Resume endless streaming for free-roam (the story drop-off capped it). Junction
        // frontiers from here on, so no fresh "Destination" terminals appear past the drop-off.
        _maxChunks = int.MaxValue;
        if (_streaming != null) _streaming.EndlessNoTerminal = true;

        if (toast != null)
            toast.Show("Keep exploring — press Finish leg whenever you're ready.");
    }

    void Finish()
    {
        if (_finished) return;
        _finished = true;
        jeepney.InputLocked = true;
        if (legCompletion != null) legCompletion.Hide();
        _legElapsed = Time.time - _startTime;   // freeze drive time

        PlayRevealThenResults();
    }

    void PlayRevealThenResults()
    {
        // The reveal already played inline on delivery (OnStoryComplete) — the
        // gate just leads straight to the results now. The dialogue path below
        // is only a fallback for an edge case where delivery never triggered it.
        if (_revealPlayed || dialogue == null)
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

    void ShowResults()
    {
        int score = _tracker.ComputeScore(_legElapsed, _def.manual.parTimeSeconds);
        int bonus = ScoreCalculator.CurrencyFor(score);

        if (GameManager.Instance != null)
            GameManager.Instance.EarnCurrency(bonus);

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
                    MarkComplete(score);
                    if (resultsPanel != null) resultsPanel.Hide();
                    if (BadgeUnlockManager.Instance != null)
                        BadgeUnlockManager.Instance.Show(_levelIndex, () => LoadScene("LevelSelect"));
                    else
                        LoadScene("LevelSelect");
                },
                onReplay: () => LoadScene("ManualDrive"));
        }
        else
        {
            // No results panel wired — still record completion so the next level
            // unlocks, then bail to the menu.
            MarkComplete(score);
            LoadScene("LevelSelect");
        }
    }

    /// <summary>Records the leg as complete (unlocks the next level, earns the badge,
    /// saves). Safe to call once on the finish path.</summary>
    void MarkComplete(int score)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.CompleteLevel(_levelIndex, score);
    }

    void OnDestroy()
    {
        if (legCompletion != null)
        {
            legCompletion.OnFinishPressed -= Finish;
            legCompletion.OnKeepExploring -= OnKeepExploring;
        }

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
