using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>
/// Builds TopDownLevel.unity — the top-down overworld scene with a tilemap
/// floor, wall collision layer, player character with WASD movement, camera
/// follow, and a minimal HUD.
///
/// Menu: Lugarithm > Build TopDown Level Scene
/// </summary>
public static class TopDownSceneBuilder
{
    // Tile asset creation path (persistent across rebuilds)
    const string TileAssetDir = "Assets/Resources/Tiles";
    const string TutorialArtDir = "Assets/Art/Tutorial";

    // -------------------------------------------------------------------------
    // Public entry point
    // -------------------------------------------------------------------------

    /// <summary>Build and save the TopDownLevel scene.</summary>
    public static void Build()
    {
        var scene = SceneBuilderUtil.NewScene();

        // --- Camera & infrastructure -----------------------------------------------

        Camera cam = SceneBuilderUtil.CreateCamera2D(
            "Main Camera",
            new Color(0.28f, 0.40f, 0.25f),  // dark green background
            6f);

        var follow = cam.gameObject.AddComponent<CameraFollow2D>();

        SceneBuilderUtil.CreateGlobalLight2D();
        SceneBuilderUtil.CreateEventSystem();

        // --- Tilemap layers --------------------------------------------------------

        // Parent for all tilemap layers
        var tilemapRoot = new GameObject("Tilemaps");

        // Grid (1 unit per cell — matches the map data)
        var grid = tilemapRoot.AddComponent<Grid>();
        grid.cellSize = new Vector3(1f, 1f, 1f);

        // Ground tilemap (visual only, no collider)
        var groundGo = new GameObject("Ground");
        groundGo.transform.SetParent(tilemapRoot.transform, false);
        var groundTilemap = groundGo.AddComponent<Tilemap>();
        groundTilemap.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
        var groundRenderer = groundGo.AddComponent<TilemapRenderer>();
        groundRenderer.sortingOrder = -10;

        // Wall/collision tilemap (has TilemapCollider2D — blocks player)
        var wallGo = new GameObject("Walls");
        wallGo.transform.SetParent(tilemapRoot.transform, false);
        var wallTilemap = wallGo.AddComponent<Tilemap>();
        wallTilemap.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
        var wallRenderer = wallGo.AddComponent<TilemapRenderer>();
        wallRenderer.sortingOrder = -5;
        wallGo.AddComponent<TilemapCollider2D>();

        GameObject tutorialEnvironmentRoot = BuildTutorialEnvironment();

        // --- Create Tile assets from placeholder sprites ---------------------------

        Tile tGrass   = CreateOrLoadTile("grass");
        Tile tPath    = CreateOrLoadTile("path");
        Tile tWall    = CreateOrLoadTile("wall");
        Tile tWater   = CreateOrLoadTile("water");
        Tile tJeepStop = CreateOrLoadTile("jeep_stop");

        // --- Player -----------------------------------------------------------------

        var playerGo = new GameObject("Player");

        var rb = playerGo.AddComponent<Rigidbody2D>();
        rb.gravityScale   = 0f;
        rb.linearDamping  = 0f;
        rb.angularDamping = 0f;
        rb.freezeRotation = true;
        rb.interpolation  = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Player physics collider (small box)
        var box = playerGo.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.5f, 0.5f);
        box.offset = new Vector2(0f, 0f);

        // Player body sprite (child so it can rotate independently)
        var bodyGo = new GameObject("Body");
        bodyGo.transform.SetParent(playerGo.transform, false);
        var bodySr = bodyGo.AddComponent<SpriteRenderer>();
        bodySr.sprite       = SceneBuilderUtil.LoadPlaceholder("td_player");
        bodySr.sortingOrder = 5;

        var player = playerGo.AddComponent<TopDownPlayerController>();

        // Wire camera follow. The overworld wants a Pokémon-style POV: stay
        // centered on the player with only a touch of softness and NO velocity
        // lead (the lead is for the drive scenes, where seeing road ahead helps).
        SceneBuilderUtil.Wire(follow, "target",       playerGo.transform);
        SceneBuilderUtil.Wire(follow, "smoothTime",   0.08f);
        SceneBuilderUtil.Wire(follow, "velocityLead", 0f);
        SceneBuilderUtil.Wire(follow, "useBounds",    true);
        SceneBuilderUtil.Wire(follow, "minBounds",    new Vector2(0f, 0f));
        SceneBuilderUtil.Wire(follow, "maxBounds",    new Vector2(24f, 36f));
        // leadBody intentionally left unwired so there is no directional lead.

        // --- HUD --------------------------------------------------------------------

        Canvas canvas = UIFactory.CreateCanvas("HudCanvas");
        TMP_FontAsset previousFont = UIFactory.FontOverride;
        UIFactory.FontOverride = SproutLandsMenuFont.EnsureFontAsset();

        var statusBackdrop = UIFactory.CreatePanel(canvas.transform, "StatusBackdrop",
                                                   new Vector2(0f, 1f), new Vector2(0f, 1f),
                                                   UIFactory.TutorialPlum);
        UIFactory.Place(statusBackdrop, new Vector2(0f, 1f), new Vector2(16f, -16f), new Vector2(252f, 92f));
        statusBackdrop.GetComponent<Image>().raycastTarget = false;

        // Level name (top-left)
        var levelName = UIFactory.CreateText(canvas.transform, "LevelName", "Tutorial",
                                             28f, UIFactory.Accent, TextAlignmentOptions.MidlineLeft);
        UIFactory.Place(levelName, new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(300f, 40f));

        // Interaction prompt (bottom-center) — hidden until player approaches an entity
        var promptBg = UIFactory.CreatePanel(canvas.transform, "PromptBg",
                                              new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                                              UIFactory.PanelDarker);
        UIFactory.Place(promptBg, new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(400f, 50f));
        promptBg.gameObject.SetActive(false);

        var promptText = UIFactory.CreateText(promptBg, "PromptText", "",
                                               22f, UIFactory.TextBright);
        UIFactory.Place(promptText, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380f, 40f));

        // Side-objectives counter (top-left, under the level name)
        var objectives = UIFactory.CreateText(canvas.transform, "Objectives", "",
                                              20f, UIFactory.TextBright, TextAlignmentOptions.MidlineLeft);
        UIFactory.Place(objectives, new Vector2(0f, 1f), new Vector2(18f, -58f), new Vector2(300f, 30f));

        // Main quest label (top-left, under the side-objectives counter, gold + bigger)
        var mainQuest = UIFactory.CreateText(canvas.transform, "MainQuest", "",
                                             22f, UIFactory.TutorialGold, TextAlignmentOptions.MidlineLeft);
        UIFactory.Place(mainQuest, new Vector2(0f, 1f), new Vector2(18f, -86f), new Vector2(400f, 30f));

        // Controls hint (bottom-left)
        var hintBackdrop = UIFactory.CreatePanel(canvas.transform, "HintBackdrop",
                                                 new Vector2(0f, 0f), new Vector2(0f, 0f),
                                                 UIFactory.TutorialPlum);
        UIFactory.Place(hintBackdrop, new Vector2(0f, 0f), new Vector2(16f, 14f), new Vector2(500f, 38f));
        hintBackdrop.GetComponent<Image>().raycastTarget = false;

        var hint = UIFactory.CreateText(canvas.transform, "Hint", "WASD / Arrows: Move  |  E: Interact",
                                        16f, UIFactory.TextDim);
        UIFactory.Place(hint, new Vector2(0f, 0f), new Vector2(18f, 18f), new Vector2(460f, 30f));

        // Minigame-station access card (placeholder, for kinds with no game yet).
        MinigamePlaceholderPanel minigamePanel =
            MinigameOverlayBuilder.BuildMinigamePlaceholder(canvas.transform, true);

        // Playable lightweight station games: a shared grid puzzle (maze / block-fill
        // / pattern) and the concept-tied coding line-ordering challenge.
        GridPuzzleMinigame gridPuzzle = MinigameOverlayBuilder.BuildGridPuzzle(canvas.transform, true);
        CodeOrderMinigame  codeOrder  = MinigameOverlayBuilder.BuildCodeOrder(canvas.transform, true);
        FlowConnectMinigame flowPuzzle = MinigameOverlayBuilder.BuildFlowConnect(canvas.transform, true);
        CrateStackMinigame  cratePuzzle = MinigameOverlayBuilder.BuildCrateStack(canvas.transform, true);
        MazeRepairMinigame  codingMaze = MinigameOverlayBuilder.BuildMazeRepair(canvas.transform, true);

        // Exit button (top-right)
        Button exitButton = UIFactory.CreateButton(canvas.transform, "ExitButton", "Exit",
                                                    new Vector2(120f, 44f));
        UIFactory.Place(exitButton, new Vector2(1f, 1f), new Vector2(-18f, -18f), new Vector2(120f, 44f));

        // Branching dialogue overlay for talking to town NPCs (reuses the same
        // controller as the drive scenes). Compact card pinned bottom-right so it
        // clears the bottom-center interaction prompt.
        DialogueController dialogue = DialogueOverlayBuilder.BuildDriveDialogue(
                                          canvas.transform,
                                          boxSize: new Vector2(920f, 250f),
                                          boxAnchoredPos: new Vector2(0f, 32f),
                                          tutorialPixelTheme: true);

        UIFactory.ApplyTutorialPixelTheme(canvas.transform);
        levelName.color = UIFactory.TutorialGold;
        objectives.color = UIFactory.TutorialCream;
        mainQuest.color = UIFactory.TutorialGold;
        hint.color = UIFactory.TutorialMuted;
        UIFactory.FontOverride = previousFont;

        // --- Orchestrator -----------------------------------------------------------

        var controllerGo = new GameObject("LevelController");
        var controller = controllerGo.AddComponent<TopDownLevelController>();

        SceneBuilderUtil.Wire(controller, "groundTilemap", groundTilemap);
        SceneBuilderUtil.Wire(controller, "wallTilemap",   wallTilemap);
        SceneBuilderUtil.Wire(controller, "tutorialEnvironmentRoot", tutorialEnvironmentRoot);
        SceneBuilderUtil.Wire(controller, "playerController", player);
        SceneBuilderUtil.Wire(controller, "cameraFollow",  follow);
        SceneBuilderUtil.Wire(controller, "promptRoot",    promptBg.gameObject);
        SceneBuilderUtil.Wire(controller, "levelNameLabel", levelName);
        SceneBuilderUtil.Wire(controller, "promptLabel",   promptText);
        SceneBuilderUtil.Wire(controller, "objectivesLabel", objectives);
        SceneBuilderUtil.Wire(controller, "mainQuestLabel", mainQuest);
        SceneBuilderUtil.Wire(controller, "exitButton",    exitButton);
        SceneBuilderUtil.Wire(controller, "dialogue",      dialogue);
        SceneBuilderUtil.Wire(controller, "minigamePanel", minigamePanel);
        SceneBuilderUtil.Wire(controller, "gridPuzzle",    gridPuzzle);
        SceneBuilderUtil.Wire(controller, "codeOrder",     codeOrder);
        SceneBuilderUtil.Wire(controller, "flowPuzzle",    flowPuzzle);
        SceneBuilderUtil.Wire(controller, "cratePuzzle",   cratePuzzle);
        SceneBuilderUtil.Wire(controller, "codingMaze",    codingMaze);
        SceneBuilderUtil.Wire(controller, "grassTile",     tGrass);
        SceneBuilderUtil.Wire(controller, "pathTile",      tPath);
        SceneBuilderUtil.Wire(controller, "wallTile",      tWall);
        SceneBuilderUtil.Wire(controller, "waterTile",     tWater);
        SceneBuilderUtil.Wire(controller, "jeepStopTile",  tJeepStop);
        SceneBuilderUtil.Wire(controller, "playerSprite",       SceneBuilderUtil.LoadPlaceholder("td_player"));
        SceneBuilderUtil.Wire(controller, "npcSprite",          SceneBuilderUtil.LoadPlaceholder("td_npc"));
        SceneBuilderUtil.Wire(controller, "interactionIndicatorSprite",
                               SceneBuilderUtil.LoadPlaceholder("td_interaction"));
        SceneBuilderUtil.Wire(controller, "puzzleStationSprite",
                               SceneBuilderUtil.LoadPlaceholder("td_puzzle"));
        SceneBuilderUtil.Wire(controller, "codeStationSprite",
                               SceneBuilderUtil.LoadPlaceholder("td_code"));

        // Wire the body sprite on the player controller
        SceneBuilderUtil.Wire(player, "bodySprite", bodySr);

        // --- Save -------------------------------------------------------------------

        SceneBuilderUtil.SaveScene(scene, "TopDownLevel");
    }

    static GameObject BuildTutorialEnvironment()
    {
        var root = new GameObject("TutorialEnvironment");

        var background = new GameObject("HeritagePlazaBackground");
        background.transform.SetParent(root.transform, false);
        background.transform.position = new Vector3(12f, 18f, 0f);
        var backgroundRenderer = background.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = LoadTutorialSprite("TutorialHeritagePlaza.png", false, 2048f / 24f);
        backgroundRenderer.sortingOrder = -20;

        root.SetActive(false);
        return root;
    }

    static void CreateTutorialTree(
        Transform parent,
        string name,
        Sprite sprite,
        Vector2 position,
        float scale)
    {
        if (sprite == null) return;

        var tree = new GameObject(name);
        tree.transform.SetParent(parent, false);
        tree.transform.position = new Vector3(position.x, position.y, 0f);
        tree.transform.localScale = Vector3.one * scale;

        var renderer = tree.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 6;

        Bounds bounds = sprite.bounds;
        var canopyTrigger = tree.AddComponent<BoxCollider2D>();
        canopyTrigger.isTrigger = true;
        canopyTrigger.size = new Vector2(bounds.size.x * 0.76f, bounds.size.y * 0.62f);
        canopyTrigger.offset = new Vector2(0f, bounds.size.y * 0.62f);

        var occluder = tree.AddComponent<TutorialTreeOccluder>();
        occluder.Configure(renderer);
    }

    static Sprite LoadTutorialSprite(string fileName, bool bottomPivot, float pixelsPerUnit)
    {
        string path = $"{TutorialArtDir}/{fileName}";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = fileName == "TutorialHeritagePlaza.png" ? 4096 : 2048;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = bottomPivot
                ? (int)SpriteAlignment.BottomCenter
                : (int)SpriteAlignment.Center;
            settings.spritePivot = bottomPivot
                ? new Vector2(0.5f, 0f)
                : new Vector2(0.5f, 0.5f);
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // -------------------------------------------------------------------------
    // Tile asset creation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates a Unity <see cref="Tile"/> asset from the matching placeholder
    /// sprite, saved to <c>Resources/Tiles/</c> so the scene builder can
    /// reference them across rebuilds without GUID drift.
    /// </summary>
    static Tile CreateOrLoadTile(string name)
    {
        Directory.CreateDirectory(TileAssetDir);
        string path = $"{TileAssetDir}/{name}.asset";

        // Try to load existing tile asset first
        var existing = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (existing != null)
        {
            // Ensure the sprite is up-to-date (in case placeholders were regenerated)
            Sprite sprite = SceneBuilderUtil.LoadPlaceholder($"td_{name}");
            if (existing.sprite != sprite)
                existing.sprite = sprite;
            EditorUtility.SetDirty(existing);
            return existing;
        }

        // Create new tile asset
        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.name = name;

        // Load the corresponding placeholder sprite
        Sprite spriteForTile = SceneBuilderUtil.LoadPlaceholder($"td_{name}");
        if (spriteForTile == null)
        {
            // Fallback: try generic names
            spriteForTile = SceneBuilderUtil.LoadPlaceholder(name);
        }

        tile.sprite = spriteForTile;

        AssetDatabase.CreateAsset(tile, path);
        AssetDatabase.SaveAssets();

        return tile;
    }
}
