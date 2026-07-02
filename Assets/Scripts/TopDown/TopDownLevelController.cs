using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>
/// Orchestrator for the top-down overworld level. Mirrors the pattern of
/// <see cref="ManualDriveController"/>: builds the world from map data, spawns
/// the player, sets up the camera, and manages interaction triggers, HUD, and
/// scene lifecycle.
/// </summary>
public class TopDownLevelController : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector references (wired by TopDownSceneBuilder)
    // -------------------------------------------------------------------------

    [Header("World")]
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap wallTilemap;

    [Header("Player")]
    [SerializeField] private TopDownPlayerController playerController;

    [Header("Camera")]
    [SerializeField] private CameraFollow2D cameraFollow;

    [Header("HUD")]
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private TMP_Text levelNameLabel;
    [SerializeField] private TMP_Text promptLabel;
    [SerializeField] private TMP_Text objectivesLabel;
    [SerializeField] private Button exitButton;

    [Header("Dialogue")]
    [SerializeField] private DialogueController dialogue;

    [Header("Minigames")]
    [SerializeField] private MinigamePlaceholderPanel minigamePanel;
    [SerializeField] private GridPuzzleMinigame gridPuzzle;   // maze / block-fill / pattern
    [SerializeField] private CodeOrderMinigame  codeOrder;    // coding challenge
    [SerializeField] private FlowConnectMinigame flowPuzzle;   // transferred progression gate
    [SerializeField] private CrateStackMinigame  cratePuzzle;  // transferred progression gate
    [SerializeField] private Sprite puzzleStationSprite;
    [SerializeField] private Sprite codeStationSprite;

    [Header("Tile assets (wired by builder)")]
    [SerializeField] private Tile grassTile;
    [SerializeField] private Tile pathTile;
    [SerializeField] private Tile wallTile;
    [SerializeField] private Tile waterTile;
    [SerializeField] private Tile jeepStopTile;

    [Header("Sprites for entities (wired by builder)")]
    [SerializeField] private Sprite playerSprite;
    [SerializeField] private Sprite npcSprite;
    [SerializeField] private Sprite interactionIndicatorSprite;

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private OverworldMapData _mapData;
    private int _levelIndex;
    private InteractionTrigger _activeTrigger;
    private List<InteractionTrigger> _triggers = new List<InteractionTrigger>();
    private bool _dialogueActive;

    // Minigame stations: the def + body sprite per station trigger, which station
    // ids have been solved, and the coding station's id (its completion gates the exit).
    private readonly Dictionary<InteractionTrigger, MinigameStationDef> _stationDefs
        = new Dictionary<InteractionTrigger, MinigameStationDef>();
    private readonly Dictionary<InteractionTrigger, SpriteRenderer> _stationBodies
        = new Dictionary<InteractionTrigger, SpriteRenderer>();
    private readonly HashSet<string> _solvedStations = new HashSet<string>();
    private int _stationCount;
    private string _codingStationId;
    private bool _panelActive;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    void Start()
    {
        _levelIndex = GameManager.Instance != null ? GameManager.Instance.SelectedLevelIndex : 0;
        _mapData = GetMapData(_levelIndex);

        if (_mapData == null)
        {
            Debug.LogError($"[TopDownLevel] No map data for level {_levelIndex}. Falling back to tutorial.");
            _mapData = OverworldMapLibrary.TutorialMap();
        }

        BuildTilemap();
        SpawnEntities();
        PositionPlayer();
        SetupCamera();
        SetupHUD();

        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitClicked);
    }

    void OnDestroy()
    {
        // Clean up trigger subscriptions
        foreach (var t in _triggers)
        {
            t.OnInteracted -= HandleInteraction;
            t.OnPlayerEntered -= HandlePlayerEntered;
            t.OnPlayerExited -= HandlePlayerExited;
        }
    }

    // -------------------------------------------------------------------------
    // Tilemap painting
    // -------------------------------------------------------------------------

    void BuildTilemap()
    {
        if (groundTilemap == null || wallTilemap == null) return;

        for (int y = 0; y < _mapData.height; y++)
        {
            for (int x = 0; x < _mapData.width; x++)
            {
                TileType tile = _mapData.GetTile(x, y);
                Vector3Int pos = new Vector3Int(x, y, 0);

                // Everything goes on the ground tilemap; solid tiles also go on the wall layer
                Tile groundTileAsset = TileForType(tile);
                if (groundTileAsset != null)
                    groundTilemap.SetTile(pos, groundTileAsset);

                // Wall and water tiles go on a separate tilemap with a collider
                if (tile == TileType.Wall)
                {
                    if (wallTile != null)
                        wallTilemap.SetTile(pos, wallTile);
                }
                else if (tile == TileType.Water)
                {
                    // Water blocks movement but isn't a wall — we still use the wall collider layer
                    // For now, just the ground tile (no separate collider, but we could add one)
                    if (wallTile != null)
                        wallTilemap.SetTile(pos, wallTile); // uses wallTilemap collider as blocker
                }
            }
        }
    }

    Tile TileForType(TileType type)
    {
        switch (type)
        {
            case TileType.Path:     return pathTile;
            case TileType.Wall:     return grassTile; // visual grass under the wall overlay
            case TileType.Water:    return waterTile;
            default:                return grassTile;
        }
    }

    // -------------------------------------------------------------------------
    // Entity spawning
    // -------------------------------------------------------------------------

    void SpawnEntities()
    {
        // Bind the town's authored minigame defs to the map's Q/C stations in
        // row-major order, per station kind (two puzzles + one coding challenge).
        MinigameStationDef[] defs = TownMinigameLibrary.ForLevel(_levelIndex);
        var puzzleDefs = new List<MinigameStationDef>();
        MinigameStationDef codingDef = null;
        foreach (var d in defs)
        {
            if (d.IsCoding) codingDef ??= d;
            else puzzleDefs.Add(d);
        }
        int puzzleIndex = 0;

        foreach (var entity in _mapData.entities)
        {
            Vector3 worldPos = new Vector3(
                entity.gridX + 0.5f,
                entity.gridY + 0.5f,
                0f
            );

            switch (entity.type)
            {
                case EntityType.Npc:
                    SpawnNpc(entity, worldPos);
                    break;
                case EntityType.JeepStop:
                    SpawnJeepStop(entity, worldPos);
                    break;
                case EntityType.Exit:
                    SpawnExit(entity, worldPos);
                    break;
                case EntityType.PuzzleStation:
                    SpawnStation(entity, worldPos,
                        puzzleIndex < puzzleDefs.Count ? puzzleDefs[puzzleIndex++] : null,
                        puzzleStationSprite);
                    break;
                case EntityType.CodeChallenge:
                    SpawnStation(entity, worldPos, codingDef, codeStationSprite);
                    break;
                case EntityType.PlayerStart:
                    // Handled by PositionPlayer
                    break;
            }
        }

        UpdateObjectives();
    }

    /// <summary>
    /// Spawns one interactable minigame station: a tinted body marker (so the
    /// three objectives read as visibly distinct), an interaction zone, and a
    /// floating indicator. The station's <see cref="MinigameStationDef"/> drives
    /// the prompt label and the placeholder access panel.
    /// </summary>
    void SpawnStation(MapEntity entity, Vector3 pos, MinigameStationDef def, Sprite bodySprite)
    {
        if (def != null) entity.minigameId = def.id;

        var triggerGo = new GameObject($"Station_{def?.id ?? "unknown"}_{entity.gridX}_{entity.gridY}");
        triggerGo.transform.position = pos;

        var trigger = triggerGo.AddComponent<InteractionTrigger>();
        var col = triggerGo.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.8f;

        // Body marker — tinted to the station's marker colour for distinction.
        SpriteRenderer body = null;
        if (bodySprite != null)
        {
            var bodyGo = new GameObject("Marker");
            bodyGo.transform.SetParent(triggerGo.transform, false);
            bodyGo.transform.localPosition = Vector3.zero;
            body = bodyGo.AddComponent<SpriteRenderer>();
            body.sprite = bodySprite;
            body.color = def != null ? def.markerColor : Color.white;
            body.sortingOrder = 5;
        }

        if (interactionIndicatorSprite != null)
        {
            var indGo = new GameObject("Indicator");
            indGo.transform.SetParent(triggerGo.transform, false);
            indGo.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            var indicator = indGo.AddComponent<SpriteRenderer>();
            indicator.sprite = interactionIndicatorSprite;
            indicator.sortingOrder = 10;
            indicator.enabled = false;
        }

        string title = def != null ? def.title : "Minigame";
        string prompt = def != null && def.IsCoding
            ? $"Press E — {title} (coding)"
            : $"Press E — {title}";
        trigger.Init(entity.type, title, prompt);

        if (def != null)
        {
            _stationDefs[trigger] = def;
            if (body != null) _stationBodies[trigger] = body;
            _stationCount++;
            if (def.IsCoding) _codingStationId = def.id;
        }

        trigger.OnInteracted += HandleInteraction;
        trigger.OnPlayerEntered += HandlePlayerEntered;
        trigger.OnPlayerExited += HandlePlayerExited;
        _triggers.Add(trigger);
    }

    void SpawnNpc(MapEntity entity, Vector3 pos)
    {
        var triggerGo = new GameObject($"NPC_{entity.gridX}_{entity.gridY}");
        triggerGo.transform.position = pos;

        // Trigger zone (circle collider)
        var trigger = triggerGo.AddComponent<InteractionTrigger>();
        var col = triggerGo.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.8f;

        // NPC body sprite
        if (npcSprite != null)
        {
            var bodyGo = new GameObject("NPC_Body");
            bodyGo.transform.SetParent(triggerGo.transform, false);
            bodyGo.transform.localPosition = Vector3.zero;
            var sr = bodyGo.AddComponent<SpriteRenderer>();
            sr.sprite = npcSprite;
            sr.sortingOrder = 5;
        }

        // Interaction indicator
        SpriteRenderer indicator = null;
        if (interactionIndicatorSprite != null)
        {
            var indGo = new GameObject("Indicator");
            indGo.transform.SetParent(triggerGo.transform, false);
            indGo.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            indicator = indGo.AddComponent<SpriteRenderer>();
            indicator.sprite = interactionIndicatorSprite;
            indicator.sortingOrder = 10;
            indicator.enabled = false; // hidden until player approaches
        }

        // Configure via SerializedObject-style workaround: set fields directly
        // (the builder wires them; but we also set defaults here for runtime spawns)
        string label = string.IsNullOrEmpty(entity.displayName) ? "Talk" : entity.displayName;
        trigger.Init(EntityType.Npc, label, $"Press E to talk to {label}", entity.npcId);

        trigger.OnInteracted += HandleInteraction;
        trigger.OnPlayerEntered += HandlePlayerEntered;
        trigger.OnPlayerExited += HandlePlayerExited;
        _triggers.Add(trigger);
    }

    void SpawnJeepStop(MapEntity entity, Vector3 pos)
    {
        var triggerGo = new GameObject($"JeepStop_{entity.gridX}_{entity.gridY}");
        triggerGo.transform.position = pos;

        var trigger = triggerGo.AddComponent<InteractionTrigger>();
        var col = triggerGo.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.8f;

        trigger.Init(EntityType.JeepStop, "Jeep Stop", "Press E to board the jeepney");

        SpriteRenderer indicator = null;
        if (interactionIndicatorSprite != null)
        {
            var indGo = new GameObject("Indicator");
            indGo.transform.SetParent(triggerGo.transform, false);
            indGo.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            indicator = indGo.AddComponent<SpriteRenderer>();
            indicator.sprite = interactionIndicatorSprite;
            indicator.sortingOrder = 10;
            indicator.enabled = false;
        }

        trigger.OnInteracted += HandleInteraction;
        trigger.OnPlayerEntered += HandlePlayerEntered;
        trigger.OnPlayerExited += HandlePlayerExited;
        _triggers.Add(trigger);
    }

    void SpawnExit(MapEntity entity, Vector3 pos)
    {
        var triggerGo = new GameObject($"Exit_{entity.gridX}_{entity.gridY}");
        triggerGo.transform.position = pos;

        var trigger = triggerGo.AddComponent<InteractionTrigger>();
        var col = triggerGo.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.8f;

        trigger.Init(EntityType.Exit, "Exit", "Press E to leave");

        SpriteRenderer indicator = null;
        if (interactionIndicatorSprite != null)
        {
            var indGo = new GameObject("Indicator");
            indGo.transform.SetParent(triggerGo.transform, false);
            indGo.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            indicator = indGo.AddComponent<SpriteRenderer>();
            indicator.sprite = interactionIndicatorSprite;
            indicator.sortingOrder = 10;
            indicator.enabled = false;
        }

        trigger.OnInteracted += HandleInteraction;
        trigger.OnPlayerEntered += HandlePlayerEntered;
        trigger.OnPlayerExited += HandlePlayerExited;
        _triggers.Add(trigger);
    }

    // -------------------------------------------------------------------------
    // Player setup
    // -------------------------------------------------------------------------

    void PositionPlayer()
    {
        if (playerController == null) return;

        // Find the player start entity
        MapEntity start = null;
        foreach (var e in _mapData.entities)
        {
            if (e.type == EntityType.PlayerStart)
            {
                start = e;
                break;
            }
        }

        // Default to center-bottom if no start found
        float px = start != null ? start.gridX + 0.5f : _mapData.width * 0.5f;
        float py = start != null ? start.gridY + 0.5f : 1.5f;

        playerController.transform.position = new Vector3(px, py, 0f);
    }

    // -------------------------------------------------------------------------
    // Camera
    // -------------------------------------------------------------------------

    void SetupCamera()
    {
        if (cameraFollow != null && playerController != null)
        {
            cameraFollow.SnapTo(playerController.transform);
        }
    }

    // -------------------------------------------------------------------------
    // HUD
    // -------------------------------------------------------------------------

    void SetupHUD()
    {
        if (levelNameLabel != null)
        {
            string name = LevelLibrary.Names.Length > _levelIndex
                ? LevelLibrary.Names[_levelIndex]
                : "Unknown";
            levelNameLabel.text = name;
        }

        if (promptLabel != null)
            promptLabel.text = "";
    }

    // -------------------------------------------------------------------------
    // Interaction handling
    // -------------------------------------------------------------------------

    void HandleInteraction(InteractionTrigger trigger)
    {
        switch (trigger.EntityType)
        {
            case EntityType.Npc:
                HandleNpcInteraction(trigger);
                break;
            case EntityType.JeepStop:
                HandleJeepStopInteraction(trigger);
                break;
            case EntityType.Exit:
                HandleExitInteraction(trigger);
                break;
            case EntityType.PuzzleStation:
            case EntityType.CodeChallenge:
                HandleStationInteraction(trigger);
                break;
        }
    }

    void HandleStationInteraction(InteractionTrigger trigger)
    {
        // Don't open over a conversation or a panel that's already up.
        if (_dialogueActive || _panelActive) return;

        if (!_stationDefs.TryGetValue(trigger, out MinigameStationDef def) || def == null)
            return;

        // Shared outcome handlers: solving marks the objective, quitting just
        // releases input. Both clear the active flag and unlock the player.
        Action onSolved = () =>
        {
            _panelActive = false;
            MarkStationSolved(trigger, def);
            if (playerController != null) playerController.InputLocked = false;
        };
        Action onQuit = () =>
        {
            _panelActive = false;
            if (playerController != null) playerController.InputLocked = false;
        };

        // Pick the game for this station kind; fall back to the placeholder card
        // for kinds we haven't built a game for yet (e.g. ColorConnect).
        bool launched = false;
        if (def.IsCoding && codeOrder != null)
        {
            codeOrder.Begin(def, onSolved, onQuit);
            launched = true;
        }
        else if (gridPuzzle != null &&
                 (def.kind == MinigamePuzzleKind.Maze ||
                  def.kind == MinigamePuzzleKind.BlockFill ||
                  def.kind == MinigamePuzzleKind.PatternMatch))
        {
            gridPuzzle.Begin(def, onSolved, onQuit);
            launched = true;
        }
        else if (def.kind == MinigamePuzzleKind.FlowConnect && flowPuzzle != null)
        {
            flowPuzzle.Show(StationSeed(def), _ => onSolved());
            launched = true;
        }
        else if (def.kind == MinigamePuzzleKind.CrateStack && cratePuzzle != null)
        {
            cratePuzzle.Show(StationSeed(def), _ => onSolved());
            launched = true;
        }
        else if (minigamePanel != null)
        {
            bool alreadySolved = _solvedStations.Contains(def.id);
            minigamePanel.Show(def, alreadySolved, onSolved, onQuit);
            launched = true;
        }

        if (!launched)
        {
            // Nothing wired (e.g. launched directly in editor) — mark solved so
            // the objective loop still progresses, with a brief courtesy pause.
            MarkStationSolved(trigger, def);
            if (playerController != null)
            {
                playerController.InputLocked = true;
                Invoke(nameof(UnlockInput), 0.5f);
            }
            return;
        }

        _panelActive = true;
        if (playerController != null) playerController.InputLocked = true;
        UpdatePrompt("");
    }

    /// <summary>Records a station as solved, dims its marker, and refreshes the HUD.</summary>
    void MarkStationSolved(InteractionTrigger trigger, MinigameStationDef def)
    {
        if (def == null) return;
        bool isNew = _solvedStations.Add(def.id);

        // Dim the marker so cleared objectives read as done.
        if (_stationBodies.TryGetValue(trigger, out SpriteRenderer body) && body != null)
        {
            Color c = body.color;
            body.color = new Color(c.r, c.g, c.b, 0.4f);
        }

        if (isNew) UpdateObjectives();
    }

    int StationSeed(MinigameStationDef def)
    {
        unchecked
        {
            int seed = 7000 + (_levelIndex * 397);
            if (def != null && !string.IsNullOrEmpty(def.id))
                for (int i = 0; i < def.id.Length; i++)
                    seed = (seed * 31) + def.id[i];
            return seed & 0x7fffffff;
        }
    }

    void HandleNpcInteraction(InteractionTrigger trigger)
    {
        // Ignore re-presses while a conversation is already up.
        if (_dialogueActive) return;

        DialogueConversation convo = TownNpcDialogueLibrary.Get(_levelIndex, trigger.NpcId);

        // No conversation wired (or no dialogue overlay in the scene) → brief
        // courtesy pause so the interaction still reads as "talking".
        if (convo == null || dialogue == null)
        {
            if (playerController != null)
            {
                playerController.InputLocked = true;
                Invoke(nameof(UnlockInput), 1f);
            }
            return;
        }

        _dialogueActive = true;
        if (playerController != null) playerController.InputLocked = true;
        UpdatePrompt("");   // hide the "Press E" prompt under the dialogue bar

        dialogue.Play(convo, () =>
        {
            _dialogueActive = false;
            if (playerController != null) playerController.InputLocked = false;
        });
    }

    void HandleJeepStopInteraction(InteractionTrigger trigger)
    {
        // Don't board mid-conversation.
        if (_dialogueActive) return;

        // Locked levels have no jeepney route yet — boarding would just run the
        // tutorial fallback, so gate it behind a "coming soon" prompt instead.
        LevelDefinition def = LevelLibrary.Get(_levelIndex);
        if (def == null || !def.hasContent)
        {
            UpdatePrompt("This route isn't open yet — coming soon!");
            return;
        }

        // Board the jeepney: launch the drive in the player's chosen mode. The
        // selected level index is already set, so the drive scene runs this level.
        bool manual = SaveSystem.Current != null && SaveSystem.Current.settings != null
            ? SaveSystem.Current.settings.manualMode
            : true;
        LoadScene(manual ? "ManualDrive" : "AutomationDrive");
    }

    void HandleExitInteraction(InteractionTrigger trigger)
    {
        // The main coding challenge gates moving on — clear it before leaving.
        if (!string.IsNullOrEmpty(_codingStationId) && !_solvedStations.Contains(_codingStationId))
        {
            UpdatePrompt("Finish the coding challenge before you move on.");
            return;
        }

        // Complete the level and return to LevelSelect
        Debug.Log("[TopDownLevel] Player exited the level.");

        if (GameManager.Instance != null)
        {
            // For the tutorial, mark it as completed
            GameManager.Instance.CompleteLevel(_levelIndex, 100);
        }

        LoadScene("LevelSelect");
    }

    void HandlePlayerEntered(InteractionTrigger trigger)
    {
        _activeTrigger = trigger;
        UpdatePrompt(trigger.PromptText);
    }

    void HandlePlayerExited(InteractionTrigger trigger)
    {
        if (_activeTrigger == trigger)
        {
            _activeTrigger = null;
            UpdatePrompt("");
        }
    }

    void UpdatePrompt(string text)
    {
        if (promptRoot != null)
            promptRoot.SetActive(!string.IsNullOrEmpty(text));
        if (promptLabel != null)
            promptLabel.text = text;
    }

    void UnlockInput()
    {
        if (playerController != null)
            playerController.InputLocked = false;
    }

    /// <summary>Refreshes the HUD objective counter (e.g. "Objectives  1/3").</summary>
    void UpdateObjectives()
    {
        if (objectivesLabel == null) return;

        if (_stationCount == 0)
        {
            objectivesLabel.text = "";
            return;
        }

        objectivesLabel.text = $"Objectives  {_solvedStations.Count}/{_stationCount}";
    }

    // -------------------------------------------------------------------------
    // Navigation
    // -------------------------------------------------------------------------

    void OnExitClicked()
    {
        // Back to LevelSelect without completing
        LoadScene("LevelSelect");
    }

    void LoadScene(string sceneName)
    {
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.TransitionTo(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    // -------------------------------------------------------------------------
    // Map data resolution
    // -------------------------------------------------------------------------

    /// <summary>Returns the overworld map data for the given level index.</summary>
    static OverworldMapData GetMapData(int levelIndex)
    {
        return OverworldMapLibrary.ForLevel(levelIndex);
    }
}
