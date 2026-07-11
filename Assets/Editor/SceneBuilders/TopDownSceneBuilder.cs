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
        // Sensible default for the tutorial map (24x36); TopDownLevelController
        // overrides these per-level at load via CameraFollow2D.SetBounds.
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
        UIFactory.Place(statusBackdrop, new Vector2(0f, 1f), new Vector2(22f, -24f), new Vector2(360f, 220f));
        var statusImage = statusBackdrop.GetComponent<Image>();
        statusImage.sprite = LugarithmUiSkin.TutorialObjective;
        statusImage.type = Image.Type.Simple;
        statusImage.preserveAspect = true;
        statusImage.color = Color.white;
        statusImage.raycastTarget = false;

        // Blueprint location ribbon (top-center).
        var locationBanner = UIFactory.CreatePanel(canvas.transform, "LocationBanner",
                                                   new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Color.white);
        var locationImage = locationBanner.GetComponent<Image>();
        locationImage.sprite = LugarithmUiSkin.TutorialBanner;
        locationImage.type = Image.Type.Simple;
        locationImage.color = Color.white;
        UIFactory.Place(locationBanner, new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(570f, 90f));
        locationImage.preserveAspect = true;
        var levelName = UIFactory.CreateText(locationBanner, "LevelName", "Tutorial",
                                             30f, UIFactory.Accent, TextAlignmentOptions.Center);
        levelName.rectTransform.offsetMin = new Vector2(36f, 8f);
        levelName.rectTransform.offsetMax = new Vector2(-36f, -8f);

        var objectiveTitle = UIFactory.CreateText(statusBackdrop, "ObjectiveTitle", "TUTORIAL",
                                                  27f, UIFactory.Accent, TextAlignmentOptions.MidlineLeft);
        UIFactory.Place(objectiveTitle, new Vector2(0f, 1f), new Vector2(74f, -20f), new Vector2(250f, 38f));

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
        objectives.transform.SetParent(statusBackdrop, false);
        UIFactory.Place(objectives, new Vector2(0f, 1f), new Vector2(74f, -62f), new Vector2(250f, 30f));

        // Main quest label (top-left, under the side-objectives counter, gold + bigger)
        var mainQuest = UIFactory.CreateText(canvas.transform, "MainQuest", "",
                                             22f, UIFactory.TutorialGold, TextAlignmentOptions.MidlineLeft);
        mainQuest.transform.SetParent(statusBackdrop, false);
        mainQuest.enableWordWrapping = true;
        UIFactory.Place(mainQuest, new Vector2(0f, 1f), new Vector2(34f, -116f), new Vector2(292f, 100f));

        // Optional artifact tracker. It stays hidden until all five side objectives
        // and the main objective are complete, then reads X until the pickup is found.
        var artifactStatus = UIFactory.CreatePanel(canvas.transform, "ArtifactStatus",
                                                    new Vector2(1f, 1f), new Vector2(1f, 1f),
                                                    UIFactory.TutorialPlum);
        UIFactory.Place(artifactStatus, new Vector2(1f, 1f), new Vector2(-18f, -74f),
                        new Vector2(300f, 48f));
        artifactStatus.GetComponent<Image>().raycastTarget = false;

        var artifactCaption = UIFactory.CreateLocalizedText(
            artifactStatus, "ArtifactCaption", "hud.artifactfound", 19f,
            UIFactory.TutorialCream, TextAlignmentOptions.MidlineLeft);
        UIFactory.Place(artifactCaption, new Vector2(0f, 0.5f), new Vector2(14f, 0f),
                        new Vector2(235f, 36f));

        var artifactMark = UIFactory.CreateText(
            artifactStatus, "ArtifactMark", "X", 25f,
            UIFactory.TutorialGold, TextAlignmentOptions.Center);
        artifactMark.fontStyle = FontStyles.Bold;
        UIFactory.Place(artifactMark, new Vector2(1f, 0.5f), new Vector2(-14f, 0f),
                        new Vector2(42f, 36f));

        // The pixel font has no check glyph, so draw a crisp check from two bars.
        var artifactCheck = UIFactory.CreateFixedRect(
            artifactStatus, "ArtifactCheck", new Vector2(1f, 0.5f),
            new Vector2(-14f, 0f), new Vector2(42f, 36f));
        var checkShort = UIFactory.CreateFixedRect(
            artifactCheck, "ShortStroke", new Vector2(0.5f, 0.5f),
            new Vector2(-6f, -3f), new Vector2(15f, 5f));
        UIFactory.AddImage(checkShort, UIFactory.TutorialGold).raycastTarget = false;
        checkShort.localEulerAngles = new Vector3(0f, 0f, -45f);
        var checkLong = UIFactory.CreateFixedRect(
            artifactCheck, "LongStroke", new Vector2(0.5f, 0.5f),
            new Vector2(5f, 1f), new Vector2(25f, 5f));
        UIFactory.AddImage(checkLong, UIFactory.TutorialGold).raycastTarget = false;
        checkLong.localEulerAngles = new Vector3(0f, 0f, 45f);
        artifactCheck.gameObject.SetActive(false);

        // Controls hint (bottom-left)
        var hintBackdrop = UIFactory.CreatePanel(canvas.transform, "HintBackdrop",
                                                 new Vector2(0f, 0f), new Vector2(0f, 0f),
                                                 UIFactory.TutorialPlum);
        UIFactory.Place(hintBackdrop, new Vector2(0f, 0f), new Vector2(22f, 20f), new Vector2(625f, 96f));
        var hintImage = hintBackdrop.GetComponent<Image>();
        hintImage.sprite = LugarithmUiSkin.TutorialFooter;
        hintImage.type = Image.Type.Simple;
        hintImage.preserveAspect = true;
        hintImage.color = Color.white;
        hintImage.raycastTarget = false;

        var hint = UIFactory.CreateText(canvas.transform, "Hint", "WASD / Arrows: Move  |  E: Interact",
                                        16f, UIFactory.TextDim);
        hint.transform.SetParent(hintBackdrop, false);
        UIFactory.Place(hint, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 70f));

        var modeCard = UIFactory.CreatePanel(canvas.transform, "ModeCard",
                                             new Vector2(1f, 0f), new Vector2(1f, 0f), UIFactory.PanelDark);
        UIFactory.Place(modeCard, new Vector2(1f, 0f), new Vector2(-24f, 20f), new Vector2(350f, 100f));
        var modeImage = modeCard.GetComponent<Image>();
        modeImage.sprite = LugarithmUiSkin.TutorialModeCard;
        modeImage.type = Image.Type.Simple;
        modeImage.preserveAspect = true;
        modeImage.color = Color.white;
        var modeText = UIFactory.CreateText(modeCard, "ModeText",
                                            "DRIVE MODE     |     CODE MODE\nManual                 Blocks",
                                            20f, UIFactory.TextBright, TextAlignmentOptions.Center);
        modeText.rectTransform.offsetMin = new Vector2(18f, 12f);
        modeText.rectTransform.offsetMax = new Vector2(-18f, -12f);

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

        SettingsPanel settingsPanel = SettingsPanelBuilder.Build(canvas.transform);

        // Blueprint action rail (top-right).
        Button settingsButton = CreateTutorialRailButton(canvas.transform, "SettingsButton", "SETTINGS",
                                                          LugarithmUiSkin.TutorialRailSettings);
        UIFactory.Place(settingsButton, new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(132f, 98f));
        var openSettings = settingsButton.gameObject.AddComponent<SettingsPanelOpenButton>();
        SceneBuilderUtil.Wire(openSettings, "button", settingsButton);
        SceneBuilderUtil.Wire(openSettings, "settingsPanel", settingsPanel);

        Button codeButton = CreateTutorialRailButton(canvas.transform, "CodeButton", "CODE",
                                                      LugarithmUiSkin.TutorialRailCode);
        UIFactory.Place(codeButton, new Vector2(1f, 1f), new Vector2(-24f, -132f), new Vector2(132f, 98f));

        Button oracleButton = CreateTutorialRailButton(canvas.transform, "OracleButton", "ORACLE",
                                                        LugarithmUiSkin.TutorialRailOracle);
        UIFactory.Place(oracleButton, new Vector2(1f, 1f), new Vector2(-24f, -240f), new Vector2(132f, 98f));
        oracleButton.gameObject.AddComponent<AlmanacToggleButton>();

        Button journalButton = CreateTutorialRailButton(canvas.transform, "JournalButton", "JOURNAL",
                                                         LugarithmUiSkin.TutorialRailJournal);
        UIFactory.Place(journalButton, new Vector2(1f, 1f), new Vector2(-24f, -348f), new Vector2(132f, 98f));
        journalButton.gameObject.AddComponent<AlmanacToggleButton>();

        // Preserve the existing exit callback while matching the fifth reference rail slot.
        Button exitButton = CreateTutorialRailButton(canvas.transform, "ExitButton", "EXIT",
                                                      LugarithmUiSkin.TutorialRailPause);
        UIFactory.Place(exitButton, new Vector2(1f, 1f), new Vector2(-24f, -456f), new Vector2(132f, 98f));

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
        SceneBuilderUtil.Wire(controller, "artifactStatusRoot", artifactStatus.gameObject);
        SceneBuilderUtil.Wire(controller, "artifactStatusMark", artifactMark);
        SceneBuilderUtil.Wire(controller, "artifactStatusCheck", artifactCheck.gameObject);
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
        SceneBuilderUtil.Wire(controller, "artifactSprite", SproutLandsUiLibrary.MenuIconBook);
        SceneBuilderUtil.Wire(controller, "artifactProximityClip",
            AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Audio/SFX/audio cue/Cultural_Heritage_Artifact_Beat_Loop.wav"));
        SceneBuilderUtil.Wire(controller, "puzzleStationSprite",
                               SceneBuilderUtil.LoadPlaceholder("td_puzzle"));
        SceneBuilderUtil.Wire(controller, "codeStationSprite",
                               SceneBuilderUtil.LoadPlaceholder("td_code"));

        // Wire the body sprite on the player controller
        SceneBuilderUtil.Wire(player, "bodySprite", bodySr);

        artifactStatus.gameObject.SetActive(false);

        // --- Save -------------------------------------------------------------------

        UIFactory.ApplyBlueprintSkin(canvas.transform);
        SceneBuilderUtil.SaveScene(scene, "TopDownLevel");
    }

    static Button CreateTutorialRailButton(Transform parent, string name, string label, Sprite sprite)
    {
        var rt = UIFactory.CreateRect(parent, name, new Vector2(1f, 1f), new Vector2(1f, 1f));
        rt.sizeDelta = new Vector2(132f, 98f);
        var image = rt.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = Color.white;
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.12f, 1.08f, 1.02f, 1f);
        colors.pressedColor = new Color(0.82f, 0.76f, 0.88f, 1f);
        button.colors = colors;
        var text = UIFactory.CreateText(rt, "Label", label, 14f, UIFactory.TutorialCream,
                                        TextAlignmentOptions.Center);
        text.rectTransform.anchorMin = new Vector2(0f, 0f);
        text.rectTransform.anchorMax = new Vector2(1f, 0f);
        text.rectTransform.pivot = new Vector2(0.5f, 0f);
        text.rectTransform.anchoredPosition = new Vector2(0f, 8f);
        text.rectTransform.sizeDelta = new Vector2(-12f, 24f);
        text.enableAutoSizing = true;
        text.fontSizeMin = 10f;
        text.fontSizeMax = 14f;
        return button;
    }

    static GameObject BuildTutorialEnvironment()
    {
        var root = new GameObject("TutorialEnvironment");

        var background = new GameObject("HeritagePlazaBackground");
        background.transform.SetParent(root.transform, false);
        background.transform.position = new Vector3(12f, 18f, 0f);
        var backgroundRenderer = background.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = LoadTutorialSprite("TutorialHeritagePlaza.png", false, 16f);
        backgroundRenderer.sortingOrder = -20;

        // No collider here: the plaza PNG is fully opaque, so a PolygonCollider2D
        // would auto-generate a solid rectangle over the whole map and trap the
        // player at spawn. Containment comes from the wall-tilemap collider.

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
