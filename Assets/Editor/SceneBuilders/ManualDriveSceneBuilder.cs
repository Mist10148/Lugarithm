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

        // Big tiled grass ground under everything.
        var ground = new GameObject("Ground");
        var groundSr = ground.AddComponent<SpriteRenderer>();
        groundSr.sprite       = SceneBuilderUtil.LoadPlaceholder("grass_tile");
        groundSr.drawMode     = SpriteDrawMode.Tiled;
        groundSr.size         = new Vector2(500f, 500f);
        groundSr.sortingOrder = -100;
        ground.transform.position = new Vector3(10f, 75f, 0f);

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

        SceneBuilderUtil.Wire(follow, "target",   jeepneyGo.transform);
        SceneBuilderUtil.Wire(follow, "leadBody", rb);

        // --- HUD --------------------------------------------------------------------

        Canvas canvas = UIFactory.CreateCanvas("HudCanvas");

        ManualHudController hud  = BuildDashboardAndRibbon(canvas);
        CoinDrawerController drawer = BuildCoinDrawer(canvas);
        PatternMatchMinigame engineRepair = BuildEngineRepair(canvas);
        RefuelMinigame       refuel       = BuildRefuel(canvas);
        CodeFixMinigame      codeFix      = BuildCodeFix(canvas);
        DriveResultsPanel results = BuildResults(canvas);
        ToastNotification toast = BuildToast(canvas);
        FlowConnectMinigame flowPuzzle  = MinigameOverlayBuilder.BuildFlowConnect(canvas.transform);
        CrateStackMinigame  cratePuzzle = MinigameOverlayBuilder.BuildCrateStack(canvas.transform);

        // Exit (top-right, under currency)
        Button exit = UIFactory.CreateButton(canvas.transform, "ExitButton", "Exit", new Vector2(130f, 44f));
        UIFactory.Place(exit, new Vector2(1f, 1f), new Vector2(-24f, -84f), new Vector2(130f, 44f));
        var link = exit.gameObject.AddComponent<SceneLink>();
        SceneBuilderUtil.Wire(link, "button",    exit);
        SceneBuilderUtil.Wire(link, "sceneName", "LevelSelect");

        // Journal toggle (below Exit)
        Button journalToggle = UIFactory.CreateButton(canvas.transform, "JournalToggle",
                                                      "Journal", new Vector2(130f, 44f), 20f);
        UIFactory.Place(journalToggle, new Vector2(1f, 1f), new Vector2(-24f, -136f),
                        new Vector2(130f, 44f));
        journalToggle.gameObject.AddComponent<AlmanacToggleButton>();

        // --- Orchestrator ------------------------------------------------------------

        var controllerGo = new GameObject("DriveController");
        var controller = controllerGo.AddComponent<ManualDriveController>();
        controllerGo.AddComponent<PassengerManager>();
        controllerGo.AddComponent<BreakdownController>();

        SceneBuilderUtil.Wire(controller, "jeepney",      jeepney);
        SceneBuilderUtil.Wire(controller, "cameraFollow", follow);
        SceneBuilderUtil.Wire(controller, "worldRoot",    worldRoot.transform);
        SceneBuilderUtil.Wire(controller, "hud",          hud);
        SceneBuilderUtil.Wire(controller, "coinDrawer",   drawer);
        SceneBuilderUtil.Wire(controller, "engineRepairMinigame", engineRepair);
        SceneBuilderUtil.Wire(controller, "refuelMinigame",       refuel);
        SceneBuilderUtil.Wire(controller, "codeFixMinigame",      codeFix);
        SceneBuilderUtil.Wire(controller, "resultsPanel", results);
        SceneBuilderUtil.Wire(controller, "toast",        toast);
        SceneBuilderUtil.Wire(controller, "flowPuzzle",   flowPuzzle);
        SceneBuilderUtil.Wire(controller, "cratePuzzle",  cratePuzzle);

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

        var speedCaption = UIFactory.CreateText(dash, "SpeedCaption", "SPEED", 16f, UIFactory.TextDim);
        UIFactory.Place(speedCaption, new Vector2(0f, 0f), new Vector2(40f, 6f), new Vector2(90f, 22f));

        // Fuel bar
        var fuelCaption = UIFactory.CreateText(dash, "FuelCaption", "FUEL", 18f, UIFactory.TextDim,
                                               TextAlignmentOptions.MidlineLeft);
        UIFactory.Place(fuelCaption, new Vector2(0f, 1f), new Vector2(170f, -28f), new Vector2(70f, 30f));

        var fuelBg = UIFactory.CreatePanel(dash, "FuelBg",
                                           new Vector2(0f, 1f), new Vector2(0f, 1f), UIFactory.PanelDarker);
        UIFactory.Place(fuelBg, new Vector2(0f, 1f), new Vector2(240f, -28f), new Vector2(200f, 26f));

        Image fuelFill = MakeFillBar(fuelBg, new Color(0.95f, 0.65f, 0.15f));

        var hint = UIFactory.CreateText(dash, "Hint", "WASD / arrows to drive — stop at signs", 16f, UIFactory.TextDim);
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

    static PassengerChip BuildChip(RectTransform parent, int index)
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
        var window = UIFactory.CreatePanel(canvas.transform, "CoinDrawer",
                                           new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                                           UIFactory.PanelDark);
        UIFactory.Place(window, new Vector2(1f, 0.5f), new Vector2(-18f, 60f), new Vector2(430f, 540f));

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
    // Breakdown minigame overlays (engine non-code / fuel non-code / code fix)

    static PatternMatchMinigame BuildEngineRepair(Canvas canvas)
    {
        var overlay = UIFactory.CreatePanel(canvas.transform, "BreakdownOverlay",
                                            Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.7f));

        var window = UIFactory.CreatePanel(overlay, "Window",
                                           new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                           UIFactory.PanelDark);
        UIFactory.Place(window, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 560f));

        var title = UIFactory.CreateText(window, "Title", "", 30f, UIFactory.Accent);
        UIFactory.Place(title, new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(660f, 44f));

        // Target strip
        var strip = UIFactory.CreateRect(window, "TargetStrip", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        UIFactory.Place(strip, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(420f, 72f));
        var stripLayout = UIFactory.AddHorizontalLayout(strip, 12f, align: TextAnchor.MiddleCenter);
        stripLayout.childAlignment = TextAnchor.MiddleCenter;

        var slots = new Image[5];
        for (int i = 0; i < 5; i++)
        {
            var slot = UIFactory.CreateRect(strip, $"Slot_{i}", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            slot.sizeDelta = new Vector2(64f, 64f);
            UIFactory.SetLayoutSize(slot, 64f, 64f);
            slots[i] = UIFactory.AddImage(slot, Color.white, SceneBuilderUtil.LoadPlaceholder("circle"));
        }

        // 3×3 parts grid
        var grid = UIFactory.CreateRect(window, "PartsGrid", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        UIFactory.Place(grid, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(330f, 330f));
        var gridLayout = grid.gameObject.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(102f, 102f);
        gridLayout.spacing  = new Vector2(10f, 10f);
        gridLayout.childAlignment = TextAnchor.MiddleCenter;

        var buttons = new Button[9];
        for (int i = 0; i < 9; i++)
            buttons[i] = UIFactory.CreateButton(grid, $"Part_{i}", "", new Vector2(102f, 102f));

        // Timer + feedback
        var timerBg = UIFactory.CreatePanel(window, "TimerBg",
                                            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), UIFactory.PanelDarker);
        UIFactory.Place(timerBg, new Vector2(0.5f, 0f), new Vector2(0f, 64f), new Vector2(560f, 18f));
        Image timerFill = MakeFillBar(timerBg, new Color(0.95f, 0.65f, 0.15f));

        var feedback = UIFactory.CreateText(window, "Feedback", "", 24f, UIFactory.TextBright);
        UIFactory.Place(feedback, new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(560f, 36f));

        var minigame = overlay.gameObject.AddComponent<PatternMatchMinigame>();
        SceneBuilderUtil.Wire(minigame, "root",          overlay.gameObject);
        SceneBuilderUtil.Wire(minigame, "titleLabel",    title);
        SceneBuilderUtil.Wire(minigame, "feedbackLabel", feedback);
        SceneBuilderUtil.WireArray(minigame, "targetSlots", slots);
        SceneBuilderUtil.WireArray(minigame, "gridButtons", buttons);
        SceneBuilderUtil.Wire(minigame, "timerFill",     timerFill);

        // Root must start active so Awake wires the buttons, then hides itself.
        return minigame;
    }

    // -------------------------------------------------------------------------
    // Refuel minigame overlay (non-code · fuel fault)

    static RefuelMinigame BuildRefuel(Canvas canvas)
    {
        var overlay = UIFactory.CreatePanel(canvas.transform, "RefuelOverlay",
                                            Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.7f));

        var window = UIFactory.CreatePanel(overlay, "Window",
                                           new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                           UIFactory.PanelDark);
        UIFactory.Place(window, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 600f));

        var title = UIFactory.CreateText(window, "Title", "", 26f, UIFactory.Accent);
        UIFactory.Place(title, new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(580f, 64f));

        // Vertical tank gauge
        var tankBg = UIFactory.CreatePanel(window, "TankBg",
                                           new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                           UIFactory.PanelDarker);
        UIFactory.Place(tankBg, new Vector2(0.5f, 0.5f), new Vector2(0f, 16f), new Vector2(150f, 360f));

        var fillRt = UIFactory.CreateRect(tankBg, "TankFill", Vector2.zero, Vector2.one,
                                          new Vector2(6f, 6f), new Vector2(-6f, -6f));
        var tankFill = fillRt.gameObject.AddComponent<Image>();
        tankFill.sprite        = SceneBuilderUtil.LoadPlaceholder("white_box");
        tankFill.color         = new Color(0.95f, 0.65f, 0.15f);
        tankFill.type          = Image.Type.Filled;
        tankFill.fillMethod    = Image.FillMethod.Vertical;
        tankFill.fillOrigin    = (int)Image.OriginVertical.Bottom;
        tankFill.fillAmount    = 0f;
        tankFill.raycastTarget = false;

        // Target band — anchors overridden at runtime to [lo, hi].
        var band = UIFactory.CreateRect(tankBg, "BandZone", new Vector2(0f, 0.5f), new Vector2(1f, 0.7f),
                                        new Vector2(2f, 0f), new Vector2(-2f, 0f));
        var bandImg = band.gameObject.AddComponent<Image>();
        bandImg.color         = new Color(0.35f, 0.85f, 0.35f, 0.45f);
        bandImg.raycastTarget = false;

        Button pump = UIFactory.CreateButton(window, "PumpButton", "PUMP", new Vector2(200f, 64f));
        UIFactory.Place(pump, new Vector2(0.5f, 0f), new Vector2(-115f, 100f), new Vector2(200f, 64f));
        pump.image.color = new Color(0.85f, 0.55f, 0.12f);

        Button done = UIFactory.CreateButton(window, "DoneButton", "DONE", new Vector2(200f, 64f));
        UIFactory.Place(done, new Vector2(0.5f, 0f), new Vector2(115f, 100f), new Vector2(200f, 64f));
        done.image.color = new Color(0.20f, 0.55f, 0.25f);

        var timerBg = UIFactory.CreatePanel(window, "TimerBg",
                                            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), UIFactory.PanelDarker);
        UIFactory.Place(timerBg, new Vector2(0.5f, 0f), new Vector2(0f, 66f), new Vector2(480f, 18f));
        Image timerFill = MakeFillBar(timerBg, new Color(0.95f, 0.65f, 0.15f));

        var feedback = UIFactory.CreateText(window, "Feedback", "", 22f, UIFactory.TextBright);
        UIFactory.Place(feedback, new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(560f, 32f));

        var game = overlay.gameObject.AddComponent<RefuelMinigame>();
        SceneBuilderUtil.Wire(game, "root",          overlay.gameObject);
        SceneBuilderUtil.Wire(game, "titleLabel",    title);
        SceneBuilderUtil.Wire(game, "feedbackLabel", feedback);
        SceneBuilderUtil.Wire(game, "tankFill",      tankFill);
        SceneBuilderUtil.Wire(game, "bandZone",      band);
        SceneBuilderUtil.Wire(game, "pumpButton",    pump);
        SceneBuilderUtil.Wire(game, "doneButton",    done);
        SceneBuilderUtil.Wire(game, "timerFill",     timerFill);

        return game;
    }

    // -------------------------------------------------------------------------
    // Code-fix minigame overlay (code · arrange the repair steps in order)

    static CodeFixMinigame BuildCodeFix(Canvas canvas)
    {
        var overlay = UIFactory.CreatePanel(canvas.transform, "CodeFixOverlay",
                                            Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.7f));

        var window = UIFactory.CreatePanel(overlay, "Window",
                                           new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                           UIFactory.PanelDark);
        UIFactory.Place(window, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 660f));

        var title = UIFactory.CreateText(window, "Title", "", 26f, UIFactory.Accent);
        UIFactory.Place(title, new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(680f, 44f));

        var list = UIFactory.CreateRect(window, "Cards", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        UIFactory.Place(list, new Vector2(0.5f, 1f), new Vector2(0f, -68f), new Vector2(620f, 416f));
        UIFactory.AddVerticalLayout(list, 8f, align: TextAnchor.UpperCenter);

        int max = CodeFixMinigame.MaxSteps;
        var labels = new TMP_Text[max];
        var bgs    = new Image[max];
        var ups    = new Button[max];
        var downs  = new Button[max];
        for (int i = 0; i < max; i++)
        {
            var row = UIFactory.CreateRect(list, $"Card_{i}",
                                           new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            row.sizeDelta = new Vector2(600f, 58f);
            UIFactory.SetLayoutSize(row, 600f, 58f);
            var bg = row.gameObject.AddComponent<Image>();
            bg.sprite = UIFactory.BuiltinSprite("UISprite.psd");
            bg.type   = Image.Type.Sliced;
            bg.color  = new Color(0.22f, 0.30f, 0.42f);

            var label = UIFactory.CreateText(row, "Label", "", 24f, UIFactory.TextBright,
                                             TextAlignmentOptions.MidlineLeft);
            UIFactory.Place(label, new Vector2(0f, 0.5f), new Vector2(232f, 0f), new Vector2(420f, 50f));

            Button up   = UIFactory.CreateButton(row, "Up",   "▲", new Vector2(62f, 48f), 22f);
            UIFactory.Place(up,   new Vector2(1f, 0.5f), new Vector2(-84f, 0f), new Vector2(62f, 48f));
            Button down = UIFactory.CreateButton(row, "Down", "▼", new Vector2(62f, 48f), 22f);
            UIFactory.Place(down, new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(62f, 48f));

            labels[i] = label;
            bgs[i]    = bg;
            ups[i]    = up;
            downs[i]  = down;
        }

        Button run = UIFactory.CreateButton(window, "RunButton", "▶ RUN", new Vector2(220f, 60f));
        UIFactory.Place(run, new Vector2(0.5f, 0f), new Vector2(0f, 96f), new Vector2(220f, 60f));
        run.image.color = new Color(0.20f, 0.55f, 0.25f);

        var timerBg = UIFactory.CreatePanel(window, "TimerBg",
                                            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), UIFactory.PanelDarker);
        UIFactory.Place(timerBg, new Vector2(0.5f, 0f), new Vector2(0f, 64f), new Vector2(560f, 18f));
        Image timerFill = MakeFillBar(timerBg, new Color(0.95f, 0.65f, 0.15f));

        var feedback = UIFactory.CreateText(window, "Feedback", "", 22f, UIFactory.TextBright);
        UIFactory.Place(feedback, new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(660f, 32f));

        var game = overlay.gameObject.AddComponent<CodeFixMinigame>();
        SceneBuilderUtil.Wire(game, "root",          overlay.gameObject);
        SceneBuilderUtil.Wire(game, "titleLabel",    title);
        SceneBuilderUtil.Wire(game, "feedbackLabel", feedback);
        SceneBuilderUtil.WireArray(game, "cardLabels",      labels);
        SceneBuilderUtil.WireArray(game, "cardBackgrounds", bgs);
        SceneBuilderUtil.WireArray(game, "upButtons",       ups);
        SceneBuilderUtil.WireArray(game, "downButtons",     downs);
        SceneBuilderUtil.Wire(game, "runButton", run);
        SceneBuilderUtil.Wire(game, "timerFill", timerFill);

        return game;
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
        UIFactory.Place(window, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(780f, 640f));

        var title = UIFactory.CreateText(window, "Title", "LEG COMPLETE", 40f, UIFactory.Accent);
        UIFactory.Place(title, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(720f, 56f));

        var breakdown = UIFactory.CreateText(window, "Breakdown", "", 26f, UIFactory.TextBright,
                                             TextAlignmentOptions.TopLeft);
        UIFactory.Place(breakdown, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(640f, 360f));

        var score = UIFactory.CreateText(window, "Score", "", 34f, UIFactory.Accent);
        UIFactory.Place(score, new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(700f, 50f));

        Button cont = UIFactory.CreateButton(window, "ContinueButton", "Continue", new Vector2(240f, 60f));
        UIFactory.Place(cont, new Vector2(0.5f, 0f), new Vector2(130f, 28f), new Vector2(240f, 60f));
        cont.image.color = new Color(0.85f, 0.55f, 0.12f);

        Button replay = UIFactory.CreateButton(window, "ReplayButton", "Replay Leg", new Vector2(240f, 60f));
        UIFactory.Place(replay, new Vector2(0.5f, 0f), new Vector2(-130f, 28f), new Vector2(240f, 60f));

        var panel = overlay.gameObject.AddComponent<DriveResultsPanel>();
        SceneBuilderUtil.Wire(panel, "root",           overlay.gameObject);
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
