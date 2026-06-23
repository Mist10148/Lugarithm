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
    [SerializeField] private Button exitButton;

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
                case EntityType.PlayerStart:
                    // Handled by PositionPlayer
                    break;
            }
        }
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
        trigger.Init(EntityType.Npc, entity.displayName, "Press E to talk");

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
        }
    }

    void HandleNpcInteraction(InteractionTrigger trigger)
    {
        // For now, show a placeholder dialogue. Later: wire into DialogueSystem.
        Debug.Log($"[TopDownLevel] Talking to NPC: {trigger.PromptLabel}");

        // Lock input during dialogue (placeholder — unlock after a short delay)
        if (playerController != null)
        {
            playerController.InputLocked = true;
            // Auto-unlock after 1 second as placeholder
            Invoke(nameof(UnlockInput), 1f);
        }
    }

    void HandleJeepStopInteraction(InteractionTrigger trigger)
    {
        // Future: launch the jeep minigame scene and return here after.
        Debug.Log($"[TopDownLevel] Boarding jeep at: {trigger.PromptLabel}");

        // Placeholder: show a message
        if (playerController != null)
        {
            playerController.InputLocked = true;
            Invoke(nameof(UnlockInput), 1f);
        }
    }

    void HandleExitInteraction(InteractionTrigger trigger)
    {
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
        // For now, only the tutorial has a map. Future levels add cases here
        // or pull from a ScriptableObject registry.
        switch (levelIndex)
        {
            default: return OverworldMapLibrary.TutorialMap();
        }
    }
}
