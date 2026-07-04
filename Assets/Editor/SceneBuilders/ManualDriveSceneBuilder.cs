using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds ManualDrive.unity — the Manual Mode scene: jeepney + camera follow
/// in the world, and the full HUD (dashboard, passenger ribbon, coin drawer,
/// breakdown minigame, results, toast). The route itself is spawned at
/// runtime from the selected level's definition.
/// </summary>
public static class ManualDriveSceneBuilder
{
    public static void Build()
    {
        var scene = SceneBuilderUtil.NewScene();

        // --- World -----------------------------------------------------------------

        Camera cam = SceneBuilderUtil.CreateCamera2D("Main Camera", new Color(0.13f, 0.22f, 0.14f), 9f);
        var follow = cam.gameObject.AddComponent<CameraFollow2D>();

        SceneBuilderUtil.CreateGlobalLight2D();
        SceneBuilderUtil.CreateEventSystem();

        // Big tiled grass ground under everything. Follows the camera so endless
        // free-roam never reaches the edge of the grass.
        var ground = new GameObject("Ground");
        var groundSr = ground.AddComponent<SpriteRenderer>();
        groundSr.sprite       = SceneBuilderUtil.LoadPlaceholder("grass_tile");
        groundSr.drawMode     = SpriteDrawMode.Tiled;
        groundSr.size         = new Vector2(700f, 700f);
        groundSr.sortingOrder = -100;
        ground.transform.position = new Vector3(10f, 75f, 0f);
        ground.AddComponent<GroundFollow>();

        var worldRoot = new GameObject("WorldRoot");

        // Jeepney — physics body + top-down sprite in one GameObject.
        var jeepneyGo = new GameObject("Jeepney");

        var rb = jeepneyGo.AddComponent<Rigidbody2D>();
        rb.gravityScale   = 0f;
        rb.linearDamping  = 0.6f;
        rb.angularDamping = 5f;
        rb.interpolation  = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var box = jeepneyGo.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.95f, 1.9f);

        var jeepneySr = jeepneyGo.AddComponent<SpriteRenderer>();
        jeepneySr.sprite       = SceneBuilderUtil.LoadPlaceholder("jeepney_top");
        jeepneySr.sortingOrder = 10;

        var jeepney = jeepneyGo.AddComponent<JeepneyController>();
        SceneBuilderUtil.Wire(jeepney, "fuelDrainPerSecond", RefuelMath.FuelDrainPerSecond);

        SceneBuilderUtil.Wire(follow, "target",   jeepneyGo.transform);
        SceneBuilderUtil.Wire(follow, "leadBody", rb);

        // --- HUD --------------------------------------------------------------------

        Canvas canvas = UIFactory.CreateCanvas("HudCanvas");

        ManualHudController hud  = BuildDashboardAndRibbon(canvas);
        CoinDrawerController drawer = BuildCoinDrawer(canvas);
        PatternMatchMinigame engineRepair = MinigameOverlayBuilder.BuildEngineRepair(canvas.transform);
        RefuelMinigame       refuel       = MinigameOverlayBuilder.BuildRefuel(canvas.transform);
        MazeRepairMinigame   mazeRepair   = MinigameOverlayBuilder.BuildMazeRepair(canvas.transform);
        DriveResultsPanel results = BuildResults(canvas);
        ToastNotification toast = BuildToast(canvas);
        // Compact dialogue card pinned bottom-right so it clears the bottom-center
        // dashboard (speedometer + fuel) and the right-side choice pills. The wide
        // default card overlaps the dashboard in ManualDrive only.
        DialogueController  dialogue    = DialogueOverlayBuilder.BuildDriveDialogue(
                                              canvas.transform,
                                              boxSize: new Vector2(680f, 200f),
                                              boxAnchoredPos: new Vector2(-24f, 24f));
        LegCompletionController legCompletion = LegCompletionOverlayBuilder.Build(canvas.transform);

        // Exit (top-right, under currency)
        Button exit = UIFactory.CreateButton(canvas.transform, "ExitButton", "Exit", new Vector2(130f, 44f));
        UIFactory.LocalizeButton(exit, "hud.exit");
        UIFactory.Place(exit, new Vector2(1f, 1f), new Vector2(-24f, -84f), new Vector2(130f, 44f));
        var link = exit.gameObject.AddComponent<SceneLink>();
        SceneBuilderUtil.Wire(link, "button",    exit);
        SceneBuilderUtil.Wire(link, "sceneName", "LevelSelect");

        // Journal toggle (below Exit)
        Button journalToggle = UIFactory.CreateButton(canvas.transform, "JournalToggle",
                                                      "Journal", new Vector2(130f, 44f), 20f);
        UIFactory.LocalizeButton(journalToggle, "hud.journal");
        UIFactory.Place(journalToggle, new Vector2(1f, 1f), new Vector2(-24f, -136f),
                        new Vector2(130f, 44f));
        journalToggle.gameObject.AddComponent<AlmanacToggleButton>();

        // Front-seat story-passenger card (top-left): who you're carrying + talking to.
        var frontSeat = UIFactory.CreatePanel(canvas.transform, "FrontSeatCard",
                                              new Vector2(0f, 1f), new Vector2(0f, 1f),
                                              new Color(0.10f, 0.12f, 0.16f, 0.92f));
        UIFactory.Place(frontSeat, new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(320f, 58f));
        TMP_Text frontSeatLabel = UIFactory.CreateText(frontSeat, "Label", "", 22f,
                                                       UIFactory.TextBright, TextAlignmentOptions.MidlineLeft);
        frontSeatLabel.rectTransform.offsetMin = new Vector2(16f, 0f);
        frontSeatLabel.rectTransform.offsetMax = new Vector2(-12f, 0f);
        frontSeat.gameObject.SetActive(false);

        // --- Orchestrator ------------------------------------------------------------

        var controllerGo = new GameObject("DriveController");
        var controller = controllerGo.AddComponent<ManualDriveController>();
        var passengerMgr = controllerGo.AddComponent<PassengerManager>();
        controllerGo.AddComponent<BreakdownController>();

        // Dulog highlighting (color-coded markers + edge-of-screen arrows).
        var edgeParent = UIFactory.CreateRect(canvas.transform, "DulogEdgeLayer",
                                              Vector2.zero, Vector2.one);
        edgeParent.offsetMin = Vector2.zero;
        edgeParent.offsetMax = Vector2.zero;

        var dulogGo = new GameObject("DulogMarkers");
        var dulog = dulogGo.AddComponent<DulogMarkerController>();
        SceneBuilderUtil.Wire(dulog, "cam",             cam);
        SceneBuilderUtil.Wire(dulog, "jeepney",         jeepneyGo.transform);
        SceneBuilderUtil.Wire(dulog, "passengers",      passengerMgr);
        SceneBuilderUtil.Wire(dulog, "edgeArrowParent", edgeParent);

        SceneBuilderUtil.Wire(controller, "jeepney",      jeepney);
        SceneBuilderUtil.Wire(controller, "cameraFollow", follow);
        SceneBuilderUtil.Wire(controller, "worldRoot",    worldRoot.transform);
        SceneBuilderUtil.Wire(controller, "hud",          hud);
        SceneBuilderUtil.Wire(controller, "coinDrawer",   drawer);
        SceneBuilderUtil.Wire(controller, "engineRepairMinigame", engineRepair);
        SceneBuilderUtil.Wire(controller, "refuelMinigame",       refuel);
        SceneBuilderUtil.Wire(controller, "mazeRepairMinigame",   mazeRepair);
        SceneBuilderUtil.Wire(controller, "resultsPanel", results);
        SceneBuilderUtil.Wire(controller, "toast",        toast);
        SceneBuilderUtil.Wire(controller, "dialogue",     dialogue);
        SceneBuilderUtil.Wire(controller, "legCompletion", legCompletion);
        SceneBuilderUtil.Wire(controller, "dulogMarkers", dulog);
        SceneBuilderUtil.Wire(controller, "frontSeatCard",  frontSeat.gameObject);
        SceneBuilderUtil.Wire(controller, "frontSeatLabel", frontSeatLabel);

        SceneBuilderUtil.SaveScene(scene, "ManualDrive");
    }

    // -------------------------------------------------------------------------
    // Dashboard + passenger ribbon

    static ManualHudController BuildDashboardAndRibbon(Canvas canvas)
    {
        // Dashboard (bottom center)
        var dash = UIFactory.CreatePanel(canvas.transform, "Dashboard",
                                         new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                                         UIFactory.PanelDark);
        UIFactory.Place(dash, new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(470f, 150f));

        // Speedometer dial + needle
        var dial = UIFactory.CreateRect(dash, "Dial", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
        UIFactory.Place(dial, new Vector2(0f, 0.5f), new Vector2(18f, -4f), new Vector2(126f, 126f));
        UIFactory.AddImage(dial, Color.white, SceneBuilderUtil.LoadPlaceholder("dial"));

        var needle = UIFactory.CreateRect(dial, "Needle", new Vector2(0.5f, 0.18f), new Vector2(0.5f, 0.18f));
        var needleRt = needle;
        needleRt.pivot = new Vector2(0.5f, 0.08f);
        needleRt.anchoredPosition = Vector2.zero;
        needleRt.sizeDelta = new Vector2(6f, 56f);
        UIFactory.AddImage(needle, new Color(0.95f, 0.25f, 0.2f), SceneBuilderUtil.LoadPlaceholder("white_box"));

        var speedCaption = UIFactory.CreateLocalizedText(dash, "SpeedCaption", "hud.speed", 16f, UIFactory.TextDim);
        UIFactory.Place(speedCaption, new Vector2(0f, 0f), new Vector2(40f, 6f), new Vector2(90f, 22f));

        // Fuel bar
        var fuelCaption = UIFactory.CreateLocalizedText(dash, "FuelCaption", "hud.fuel", 18f, UIFactory.TextDim,
                                                        TextAlignmentOptions.MidlineLeft);
        UIFactory.Place(fuelCaption, new Vector2(0f, 1f), new Vector2(170f, -28f), new Vector2(70f, 30f));

        var fuelBg = UIFactory.CreatePanel(dash, "FuelBg",
                                           new Vector2(0f, 1f), new Vector2(0f, 1f), UIFactory.PanelDarker);
        UIFactory.Place(fuelBg, new Vector2(0f, 1f), new Vector2(240f, -28f), new Vector2(200f, 26f));

        Image fuelFill = MakeFillBar(fuelBg, new Color(0.95f, 0.65f, 0.15f));

        var hint = UIFactory.CreateLocalizedText(dash, "Hint", "hud.manualhint", 16f, UIFactory.TextDim);
        UIFactory.Place(hint, new Vector2(0.5f, 0f), new Vector2(60f, 34f), new Vector2(290f, 40f));

        // Currency (top right)
        var currency = UIFactory.CreateText(canvas.transform, "Currency", "₱ 0", 40f,
                                            UIFactory.Accent, TextAlignmentOptions.MidlineRight);
        UIFactory.Place(currency, new Vector2(1f, 1f), new Vector2(-24f, -22f), new Vector2(260f, 50f));

        // Passenger ribbon (top left)
        var ribbon = UIFactory.CreateRect(canvas.transform, "PassengerRibbon",
                                          new Vector2(0f, 1f), new Vector2(0f, 1f));
        UIFactory.Place(ribbon, new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(300f, 540f));
        var layout = UIFactory.AddVerticalLayout(ribbon, 8f, align: TextAnchor.UpperLeft);
        layout.childAlignment = TextAnchor.UpperLeft;

        var chips = new PassengerChip[8];
        for (int i = 0; i < chips.Length; i++)
            chips[i] = BuildChip(ribbon, i);

        var hud = canvas.gameObject.AddComponent<ManualHudController>();
        SceneBuilderUtil.Wire(hud, "speedNeedle",   needleRt);
        SceneBuilderUtil.Wire(hud, "fuelFill",      fuelFill);
        SceneBuilderUtil.Wire(hud, "currencyLabel", currency);
        SceneBuilderUtil.WireArray(hud, "chips",    chips);

        return hud;
    }

    internal static PassengerChip BuildChip(RectTransform parent, int index)
    {
        var chip = UIFactory.CreateRect(parent, $"Chip_{index}",
                                        new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        chip.sizeDelta = new Vector2(290f, 56f);
        UIFactory.SetLayoutSize(chip, 290f, 56f);

        var bg = chip.gameObject.AddComponent<Image>();
        bg.sprite = UIFactory.BuiltinSprite("UISprite.psd");
        bg.type   = Image.Type.Sliced;
        bg.color  = UIFactory.PanelDark;
        bg.raycastTarget = false;

        var portrait = UIFactory.CreateRect(chip, "Portrait", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
        UIFactory.Place(portrait, new Vector2(0f, 0.5f), new Vector2(8f, 2f), new Vector2(26f, 38f));
        var portraitImage = UIFactory.AddImage(portrait, Color.white, SceneBuilderUtil.LoadPlaceholder("peep"));
        portraitImage.raycastTarget = false;

        var label = UIFactory.CreateText(chip, "Label", "", 19f, UIFactory.TextBright,
                                         TextAlignmentOptions.MidlineLeft);
        UIFactory.Place(label, new Vector2(0f, 0.5f), new Vector2(44f, 6f), new Vector2(240f, 36f));

        var patienceBg = UIFactory.CreatePanel(chip, "PatienceBg",
                                               new Vector2(0f, 0f), new Vector2(0f, 0f), UIFactory.PanelDarker);
        UIFactory.Place(patienceBg, new Vector2(0f, 0f), new Vector2(44f, 6f), new Vector2(236f, 8f));
        patienceBg.GetComponent<Image>().raycastTarget = false;

        Image patienceFill = MakeFillBar(patienceBg, new Color(0.35f, 0.85f, 0.35f));

        var component = chip.gameObject.AddComponent<PassengerChip>();
        SceneBuilderUtil.Wire(component, "background",   bg);
        SceneBuilderUtil.Wire(component, "portrait",     portraitImage);
        SceneBuilderUtil.Wire(component, "label",        label);
        SceneBuilderUtil.Wire(component, "patienceFill", patienceFill);

        return component;
    }

    // -------------------------------------------------------------------------
    // Coin drawer

    static CoinDrawerController BuildCoinDrawer(Canvas canvas)
    {
        // Anchored LEFT so the right-side dialogue choice pills never cover the drawer.
        var window = UIFactory.CreatePanel(canvas.transform, "CoinDrawer",
                                           new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                                           UIFactory.PanelDark);
        UIFactory.Place(window, new Vector2(0f, 0.5f), new Vector2(18f, 60f), new Vector2(430f, 540f));

        var title = UIFactory.CreateText(window, "Title", "COIN DRAWER", 28f, UIFactory.Accent);
        UIFactory.Place(title, new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(380f, 40f));

        var header = UIFactory.CreateText(window, "Header", "", 23f, UIFactory.TextBright);
        UIFactory.Place(header, new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(390f, 80f));

        var patienceBg = UIFactory.CreatePanel(window, "PatienceBg",
                                               new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), UIFactory.PanelDarker);
        UIFactory.Place(patienceBg, new Vector2(0.5f, 1f), new Vector2(0f, -148f), new Vector2(380f, 18f));
        Image patienceFill = MakeFillBar(patienceBg, new Color(0.35f, 0.85f, 0.35f));

        // Denomination grid: 4 coins + 3 bills.
        var grid = UIFactory.CreateRect(window, "Denominations",
                                        new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        UIFactory.Place(grid, new Vector2(0.5f, 1f), new Vector2(0f, -180f), new Vector2(396f, 180f));
        var gridLayout = grid.gameObject.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize    = new Vector2(94f, 76f);
        gridLayout.spacing     = new Vector2(6f, 6f);
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.childAlignment = TextAnchor.UpperCenter;

        string[] labels = { "₱1", "₱5", "₱10", "₱20", "₱20 bill", "₱50", "₱100" };
        var buttons = new Button[labels.Length];
        for (int i = 0; i < labels.Length; i++)
        {
            buttons[i] = UIFactory.CreateButton(grid, $"Denom_{i}", labels[i], new Vector2(94f, 76f), 24f);
            Image face = buttons[i].image;
            face.color = i < 4 ? new Color(0.55f, 0.45f, 0.18f) : new Color(0.28f, 0.42f, 0.28f);
        }

        var selected = UIFactory.CreateText(window, "Selected", "Selected: ₱0", 26f, UIFactory.TextBright);
        UIFactory.Place(selected, new Vector2(0.5f, 0f), new Vector2(0f, 86f), new Vector2(380f, 40f));

        Button clear = UIFactory.CreateButton(window, "ClearButton", "Clear", new Vector2(150f, 56f));
        UIFactory.Place(clear, new Vector2(0.5f, 0f), new Vector2(-90f, 18f), new Vector2(150f, 56f));

        Button give = UIFactory.CreateButton(window, "GiveButton", "GIVE", new Vector2(190f, 56f));
        UIFactory.Place(give, new Vector2(0.5f, 0f), new Vector2(95f, 18f), new Vector2(190f, 56f));
        give.image.color = new Color(0.85f, 0.55f, 0.12f);

        var drawer = canvas.gameObject.AddComponent<CoinDrawerController>();
        SceneBuilderUtil.Wire(drawer, "root",          window.gameObject);
        SceneBuilderUtil.Wire(drawer, "headerLabel",   header);
        SceneBuilderUtil.Wire(drawer, "selectedLabel", selected);
        SceneBuilderUtil.Wire(drawer, "patienceFill",  patienceFill);
        SceneBuilderUtil.WireArray(drawer, "denominationButtons", buttons);
        SceneBuilderUtil.Wire(drawer, "clearButton",   clear);
        SceneBuilderUtil.Wire(drawer, "giveButton",    give);
        SceneBuilderUtil.Wire(drawer, "window",        window);

        return drawer;
    }

    // -------------------------------------------------------------------------
    // Results overlay

    static DriveResultsPanel BuildResults(Canvas canvas)
    {
        var overlay = UIFactory.CreatePanel(canvas.transform, "ResultsOverlay",
                                            Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.75f));

        var window = UIFactory.CreatePanel(overlay, "Window",
                                           new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                           UIFactory.PanelDark);
        UIFactory.Place(window, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 660f));

        // Category chip (top) — labels which analytics surface this is.
        var category = UIFactory.CreateText(window, "Category", "MAIN GAMEPLAY · Manual", 18f,
                                            UIFactory.TextDim, TextAlignmentOptions.Center);
        UIFactory.Place(category, new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(700f, 24f));

        var title = UIFactory.CreateText(window, "Title", "LEG COMPLETE", 40f, UIFactory.Accent);
        UIFactory.Place(title, new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(700f, 52f));

        // A thin divider under the header keeps the breakdown zone visually separate.
        var rule = UIFactory.CreatePanel(window, "Rule", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                         new Color(1f, 1f, 1f, 0.08f));
        UIFactory.Place(rule, new Vector2(0.5f, 1f), new Vector2(0f, -116f), new Vector2(680f, 2f));

        // Breakdown rows — the leg's line-items, generously spaced.
        var breakdown = UIFactory.CreateText(window, "Breakdown", "", 25f, UIFactory.TextBright,
                                             TextAlignmentOptions.TopLeft);
        UIFactory.Place(breakdown, new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(640f, 340f));
        breakdown.lineSpacing = 12f;

        // Score hero band near the bottom.
        var scoreBg = UIFactory.CreatePanel(window, "ScoreBand", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                                            new Color(0.06f, 0.07f, 0.10f, 1f));
        UIFactory.Place(scoreBg, new Vector2(0.5f, 0f), new Vector2(0f, 104f), new Vector2(700f, 64f));
        var score = UIFactory.CreateText(scoreBg, "Score", "", 32f, UIFactory.Accent);
        score.rectTransform.offsetMin = Vector2.zero;
        score.rectTransform.offsetMax = Vector2.zero;

        Button cont = UIFactory.CreateButton(window, "ContinueButton", "Continue", new Vector2(240f, 60f));
        UIFactory.LocalizeButton(cont, "common.continue");
        UIFactory.Place(cont, new Vector2(0.5f, 0f), new Vector2(130f, 28f), new Vector2(240f, 60f));
        cont.image.color = new Color(0.85f, 0.55f, 0.12f);

        Button replay = UIFactory.CreateButton(window, "ReplayButton", "Replay Leg", new Vector2(240f, 60f));
        UIFactory.LocalizeButton(replay, "results.replay");
        UIFactory.Place(replay, new Vector2(0.5f, 0f), new Vector2(-130f, 28f), new Vector2(240f, 60f));

        var panel = overlay.gameObject.AddComponent<DriveResultsPanel>();
        SceneBuilderUtil.Wire(panel, "root",           overlay.gameObject);
        SceneBuilderUtil.Wire(panel, "categoryLabel",  category);
        SceneBuilderUtil.Wire(panel, "titleLabel",     title);
        SceneBuilderUtil.Wire(panel, "breakdownLabel", breakdown);
        SceneBuilderUtil.Wire(panel, "scoreLabel",     score);
        SceneBuilderUtil.Wire(panel, "continueButton", cont);
        SceneBuilderUtil.Wire(panel, "replayButton",   replay);

        return panel;
    }

    // -------------------------------------------------------------------------
    // Toast

    static ToastNotification BuildToast(Canvas canvas)
    {
        var toastPanel = UIFactory.CreatePanel(canvas.transform, "Toast",
                                               new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                               UIFactory.PanelDarker);
        UIFactory.Place(toastPanel, new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(760f, 56f));

        var message = UIFactory.CreateText(toastPanel, "Message", "", 24f, UIFactory.TextBright);

        var group = toastPanel.gameObject.AddComponent<CanvasGroup>();
        var toast = toastPanel.gameObject.AddComponent<ToastNotification>();
        SceneBuilderUtil.Wire(toast, "messageLabel", message);
        SceneBuilderUtil.Wire(toast, "canvasGroup",  group);

        return toast;
    }

    // -------------------------------------------------------------------------

    /// <summary>Horizontal fill bar inside a background panel.</summary>
    static Image MakeFillBar(RectTransform background, Color color)
    {
        var fill = UIFactory.CreateRect(background, "Fill", Vector2.zero, Vector2.one,
                                        new Vector2(2f, 2f), new Vector2(-2f, -2f));
        var image = fill.gameObject.AddComponent<Image>();
        image.sprite        = SceneBuilderUtil.LoadPlaceholder("white_box");
        image.color         = color;
        image.type          = Image.Type.Filled;
        image.fillMethod    = Image.FillMethod.Horizontal;
        image.fillAmount    = 1f;
        image.raycastTarget = false;
        return image;
    }
}
