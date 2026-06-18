using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// Builds minigame overlays shared by more than one scene. Right now that's the
/// FlowConnect town gate, dropped into both the Manual drive and the
/// Automation/CodeDrive scenes so a Molo run always includes the non-code
/// connections puzzle regardless of mode. Kept here to keep the scene builders
/// DRY.
/// </summary>
public static class MinigameOverlayBuilder
{
    public static FlowConnectMinigame BuildFlowConnect(Transform parent)
    {
        var overlay = UIFactory.CreatePanel(parent, "FlowConnectOverlay",
                                            Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.80f));

        var window = UIFactory.CreatePanel(overlay, "Window",
                                           new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                           UIFactory.PanelDark);
        UIFactory.Place(window, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 780f));

        var title = UIFactory.CreateText(window, "Title", "", 26f, UIFactory.Accent);
        UIFactory.Place(title, new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(680f, 44f));

        // 5×5 hub grid. GridLayoutGroup fills row-major from the upper-left, so
        // child (y*n + x) lands at column x, row y — matching board coordinates
        // (y = 0 is the top row, as in the automation grids).
        int n = FlowConnectMinigame.Size;
        var grid = UIFactory.CreateRect(window, "Grid", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        UIFactory.Place(grid, new Vector2(0.5f, 0.5f), new Vector2(0f, 28f), new Vector2(540f, 540f));
        var gl = grid.gameObject.AddComponent<GridLayoutGroup>();
        gl.cellSize        = new Vector2(96f, 96f);
        gl.spacing         = new Vector2(8f, 8f);
        gl.startCorner     = GridLayoutGroup.Corner.UpperLeft;
        gl.startAxis       = GridLayoutGroup.Axis.Horizontal;
        gl.childAlignment  = TextAnchor.MiddleCenter;
        gl.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        gl.constraintCount = n;

        var cells = new FlowCell[n * n];
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                var cellRt = UIFactory.CreateRect(grid, $"Cell_{x}_{y}",
                                                  new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                cellRt.sizeDelta = new Vector2(96f, 96f);

                var bg = cellRt.gameObject.AddComponent<Image>();
                bg.sprite        = UIFactory.BuiltinSprite("UISprite.psd");
                bg.type          = Image.Type.Sliced;
                bg.color         = new Color(0.16f, 0.18f, 0.22f);
                bg.raycastTarget = true;

                var dotRt = UIFactory.CreateRect(cellRt, "Dot",
                                                 new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                UIFactory.Place(dotRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(58f, 58f));
                var dot = dotRt.gameObject.AddComponent<Image>();
                dot.sprite        = SceneBuilderUtil.LoadPlaceholder("circle");
                dot.raycastTarget = false;
                dot.enabled       = false;

                var cell = cellRt.gameObject.AddComponent<FlowCell>();
                cell.x = x;
                cell.y = y;
                cell.background = bg;
                cell.dot = dot;
                cells[y * n + x] = cell;
            }
        }

        Button reset = UIFactory.CreateButton(window, "ResetButton", "Reset", new Vector2(200f, 56f));
        UIFactory.Place(reset, new Vector2(0.5f, 0f), new Vector2(0f, 96f), new Vector2(200f, 56f));

        var timerBg = UIFactory.CreatePanel(window, "TimerBg",
                                            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), UIFactory.PanelDarker);
        UIFactory.Place(timerBg, new Vector2(0.5f, 0f), new Vector2(0f, 64f), new Vector2(560f, 18f));
        Image timerFill = MakeFillBar(timerBg, new Color(0.95f, 0.65f, 0.15f));

        var feedback = UIFactory.CreateText(window, "Feedback", "", 22f, UIFactory.TextBright);
        UIFactory.Place(feedback, new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(660f, 32f));

        var game = overlay.gameObject.AddComponent<FlowConnectMinigame>();
        SceneBuilderUtil.Wire(game, "root",          overlay.gameObject);
        SceneBuilderUtil.Wire(game, "titleLabel",    title);
        SceneBuilderUtil.Wire(game, "feedbackLabel", feedback);
        SceneBuilderUtil.WireArray(game, "cells",    cells);
        SceneBuilderUtil.Wire(game, "resetButton",   reset);
        SceneBuilderUtil.Wire(game, "timerFill",     timerFill);

        return game;
    }

    // -------------------------------------------------------------------------
    // Crate Stack town gate (Oton): reorder crates heaviest-at-bottom.

    public static CrateStackMinigame BuildCrateStack(Transform parent)
    {
        var overlay = UIFactory.CreatePanel(parent, "CrateStackOverlay",
                                            Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.80f));

        var window = UIFactory.CreatePanel(overlay, "Window",
                                           new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                           UIFactory.PanelDark);
        UIFactory.Place(window, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 700f));

        var title = UIFactory.CreateText(window, "Title", "", 26f, UIFactory.Accent);
        UIFactory.Place(title, new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(680f, 44f));

        var list = UIFactory.CreateRect(window, "Cards", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        UIFactory.Place(list, new Vector2(0.5f, 1f), new Vector2(0f, -68f), new Vector2(620f, 432f));
        UIFactory.AddVerticalLayout(list, 8f, align: TextAnchor.UpperCenter);

        int max = CrateStackMinigame.Slots;
        var labels = new TMP_Text[max];
        var bgs    = new Image[max];
        var ups    = new Button[max];
        var downs  = new Button[max];
        for (int i = 0; i < max; i++)
        {
            var row = UIFactory.CreateRect(list, $"Crate_{i}",
                                           new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            row.sizeDelta = new Vector2(600f, 60f);
            UIFactory.SetLayoutSize(row, 600f, 60f);
            var bg = row.gameObject.AddComponent<Image>();
            bg.sprite = UIFactory.BuiltinSprite("UISprite.psd");
            bg.type   = Image.Type.Sliced;
            bg.color  = new Color(0.45f, 0.33f, 0.20f);

            var label = UIFactory.CreateText(row, "Label", "", 24f, UIFactory.TextBright,
                                             TextAlignmentOptions.MidlineLeft);
            UIFactory.Place(label, new Vector2(0f, 0.5f), new Vector2(232f, 0f), new Vector2(420f, 52f));

            Button up   = UIFactory.CreateButton(row, "Up",   "▲", new Vector2(64f, 50f), 22f);
            UIFactory.Place(up,   new Vector2(1f, 0.5f), new Vector2(-86f, 0f), new Vector2(64f, 50f));
            Button down = UIFactory.CreateButton(row, "Down", "▼", new Vector2(64f, 50f), 22f);
            UIFactory.Place(down, new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(64f, 50f));

            labels[i] = label;
            bgs[i]    = bg;
            ups[i]    = up;
            downs[i]  = down;
        }

        var timerBg = UIFactory.CreatePanel(window, "TimerBg",
                                            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), UIFactory.PanelDarker);
        UIFactory.Place(timerBg, new Vector2(0.5f, 0f), new Vector2(0f, 64f), new Vector2(560f, 18f));
        Image timerFill = MakeFillBar(timerBg, new Color(0.95f, 0.65f, 0.15f));

        var feedback = UIFactory.CreateText(window, "Feedback", "", 22f, UIFactory.TextBright);
        UIFactory.Place(feedback, new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(660f, 32f));

        var game = overlay.gameObject.AddComponent<CrateStackMinigame>();
        SceneBuilderUtil.Wire(game, "root",          overlay.gameObject);
        SceneBuilderUtil.Wire(game, "titleLabel",    title);
        SceneBuilderUtil.Wire(game, "feedbackLabel", feedback);
        SceneBuilderUtil.WireArray(game, "cardLabels",      labels);
        SceneBuilderUtil.WireArray(game, "cardBackgrounds", bgs);
        SceneBuilderUtil.WireArray(game, "upButtons",       ups);
        SceneBuilderUtil.WireArray(game, "downButtons",     downs);
        SceneBuilderUtil.Wire(game, "timerFill", timerFill);

        return game;
    }

    // -------------------------------------------------------------------------
    // Breakdown repair overlays. Shared by the Manual drive (random mid-drive
    // breakdown) and the Automation/CodeDrive scene (scripted tutorial drills):
    // engine non-code (PatternMatch), fuel non-code (Refuel), and code-based
    // (CodeFix · either fault).

    public static PatternMatchMinigame BuildEngineRepair(Transform parent)
    {
        var overlay = UIFactory.CreatePanel(parent, "BreakdownOverlay",
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

    public static RefuelMinigame BuildRefuel(Transform parent)
    {
        var overlay = UIFactory.CreatePanel(parent, "RefuelOverlay",
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

    public static CodeFixMinigame BuildCodeFix(Transform parent)
    {
        var overlay = UIFactory.CreatePanel(parent, "CodeFixOverlay",
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
    // Maze repair minigame overlay (code · escape a maze with your algorithm).
    // Reuses the Automation block/code editor windows so the player solves it in
    // whichever editor the Block/Code setting selects.

    public static MazeRepairMinigame BuildMazeRepair(Transform parent)
    {
        var overlay = UIFactory.CreatePanel(parent, "MazeRepairOverlay",
                                            Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.82f));

        var window = UIFactory.CreatePanel(overlay, "Window",
                                           new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                           UIFactory.PanelDark);
        UIFactory.Place(window, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1340f, 780f));

        var title = UIFactory.CreateText(window, "Title", "", 26f, UIFactory.Accent);
        UIFactory.Place(title, new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(1280f, 40f));

        // --- Left column: goal, the maze view, feedback, timer, controls ---------
        var left = UIFactory.CreateRect(window, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f),
                                        new Vector2(20f, 20f), new Vector2(560f, -56f));

        var goal = UIFactory.CreateText(left, "Goal", "", 19f, UIFactory.TextDim,
                                        TextAlignmentOptions.TopLeft);
        UIFactory.Place(goal, new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(520f, 96f));
        goal.enableWordWrapping = true;

        var mazePanel = UIFactory.CreatePanel(left, "MazePanel",
                                              new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                              UIFactory.PanelDarker);
        UIFactory.Place(mazePanel, new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(520f, 360f));

        // The maze is rendered graphically: a dedicated camera draws the iso grid
        // + driving jeepney into a RenderTexture (created at runtime), shown here.
        var mazeImageRt = UIFactory.CreateRect(mazePanel, "MazeImage",
                                               Vector2.zero, Vector2.one,
                                               new Vector2(8f, 8f), new Vector2(-8f, -8f));
        var mazeImage = mazeImageRt.gameObject.AddComponent<RawImage>();
        mazeImage.color = Color.white;

        BuildMazeWorld(out GridWorldView worldView, out JeepneyAgentView agentView, out Camera mazeCamera);

        var feedback = UIFactory.CreateText(left, "Feedback", "", 19f, UIFactory.TextBright,
                                            TextAlignmentOptions.TopLeft);
        UIFactory.Place(feedback, new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(520f, 80f));
        feedback.enableWordWrapping = true;

        var timer = UIFactory.CreateText(left, "Timer", "", 22f, UIFactory.Accent,
                                         TextAlignmentOptions.MidlineLeft);
        UIFactory.Place(timer, new Vector2(0f, 0f), new Vector2(8f, 78f), new Vector2(180f, 32f));

        Button run = UIFactory.CreateButton(left, "RunButton", "▶ RUN", new Vector2(180f, 56f));
        UIFactory.Place(run, new Vector2(0.5f, 0f), new Vector2(-100f, 16f), new Vector2(180f, 56f));
        run.image.color = new Color(0.20f, 0.55f, 0.25f);

        Button reset = UIFactory.CreateButton(left, "ResetButton", "↺ Reset", new Vector2(180f, 56f));
        UIFactory.Place(reset, new Vector2(0.5f, 0f), new Vector2(100f, 16f), new Vector2(180f, 56f));

        // --- Right column: the two editor windows (one shown per the setting) -----
        var editorArea = UIFactory.CreateRect(window, "EditorArea",
                                              new Vector2(0f, 0f), new Vector2(1f, 1f),
                                              new Vector2(580f, 20f), new Vector2(-20f, -56f));

        RectTransform blockPanel = AutomationDriveSceneBuilder.BuildBlockWindow(
            editorArea, (RectTransform)overlay.transform,
            out BlockPaletteController palette, out BlockCanvasController blockCanvas);
        RectTransform codePanel = AutomationDriveSceneBuilder.BuildCodeWindow(
            editorArea, out CodeEditorController codeEditor, out _);

        // Execution engine (drives the shared AgentSim through the maze grid).
        var exec = overlay.gameObject.AddComponent<ExecutionController>();

        var game = overlay.gameObject.AddComponent<MazeRepairMinigame>();
        SceneBuilderUtil.Wire(game, "root",          overlay.gameObject);
        SceneBuilderUtil.Wire(game, "titleLabel",    title);
        SceneBuilderUtil.Wire(game, "goalLabel",     goal);
        SceneBuilderUtil.Wire(game, "mazeImage",     mazeImage);
        SceneBuilderUtil.Wire(game, "worldView",     worldView);
        SceneBuilderUtil.Wire(game, "agentView",     agentView);
        SceneBuilderUtil.Wire(game, "mazeCamera",    mazeCamera);
        SceneBuilderUtil.Wire(game, "feedbackLabel", feedback);
        SceneBuilderUtil.Wire(game, "timerLabel",    timer);
        SceneBuilderUtil.Wire(game, "blockPanel",    blockPanel.gameObject);
        SceneBuilderUtil.Wire(game, "codePanel",     codePanel.gameObject);
        SceneBuilderUtil.Wire(game, "blockCanvas",   blockCanvas);
        SceneBuilderUtil.Wire(game, "palette",       palette);
        SceneBuilderUtil.Wire(game, "codeEditor",    codeEditor);
        SceneBuilderUtil.Wire(game, "exec",          exec);
        SceneBuilderUtil.Wire(game, "runButton",     run);
        SceneBuilderUtil.Wire(game, "resetButton",   reset);

        return game;
    }

    /// <summary>
    /// Builds the off-screen maze world driven into a RenderTexture: an iso
    /// <see cref="GridWorldView"/>, a <see cref="JeepneyAgentView"/> agent, and a
    /// dedicated orthographic camera. Placed far from the scene's playfield so the
    /// camera only ever captures the maze (no extra layer needed). The camera is
    /// left disabled; <see cref="MazeRepairMinigame"/> enables it (and assigns the
    /// runtime RenderTexture) while the puzzle is on screen.
    /// </summary>
    static void BuildMazeWorld(out GridWorldView worldView, out JeepneyAgentView agentView, out Camera cam)
    {
        Vector3 origin = new Vector3(5000f, 5000f, 0f);

        var worldRoot = new GameObject("MazeWorldRoot");
        worldRoot.transform.position = origin;
        worldView = worldRoot.AddComponent<GridWorldView>();

        var agentGo = new GameObject("MazeAgent");
        agentGo.transform.position = origin;
        var body = agentGo.AddComponent<SpriteRenderer>();
        body.sprite = SceneBuilderUtil.LoadPlaceholder("iso_jeepney");
        body.sortingOrder = 100;

        var arrowGo = new GameObject("Arrow");
        arrowGo.transform.SetParent(agentGo.transform, false);
        arrowGo.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        arrowGo.transform.localScale = Vector3.one * 0.8f;
        var arrow = arrowGo.AddComponent<SpriteRenderer>();
        arrow.sprite = SceneBuilderUtil.LoadPlaceholder("triangle");
        arrow.color = Color.white;
        arrow.sortingOrder = 101;

        agentView = agentGo.AddComponent<JeepneyAgentView>();
        SceneBuilderUtil.Wire(agentView, "body",  body);
        SceneBuilderUtil.Wire(agentView, "arrow", arrow);

        var camGo = new GameObject("MazeCamera");
        camGo.transform.position = origin + new Vector3(0f, 0f, -10f);
        cam = camGo.AddComponent<Camera>();
        cam.orthographic     = true;
        cam.orthographicSize = 4f;
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = new Color(0.05f, 0.06f, 0.09f, 1f);
        cam.GetUniversalAdditionalCameraData();   // URP camera data
        cam.enabled = false;                       // rendered only while the maze is shown
    }

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
