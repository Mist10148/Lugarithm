using TMPro;
using UnityEngine;
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
