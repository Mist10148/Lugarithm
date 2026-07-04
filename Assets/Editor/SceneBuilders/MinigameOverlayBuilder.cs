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
    /// <summary>Lifts a minigame overlay above all gameplay/HUD/journal canvases so it always
    /// renders in front while it's up (its own sorting canvas, above Almanac 200 / badge 300,
    /// below the screen-fade transition 1000).</summary>
    static void LiftToFront(RectTransform overlay)
    {
        if (overlay == null) return;
        var canvas = overlay.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 500;
        overlay.gameObject.AddComponent<GraphicRaycaster>();
    }

    /// <summary>
    /// Compact post-minigame results card, parented alongside a minigame overlay.
    /// Non-code drills show outcome + score; code drills also reveal a CODE ANALYSIS
    /// block (hidden by default, toggled on by <see cref="MinigameResultsPanel"/>).
    /// </summary>
    internal static MinigameResultsPanel BuildResultsCard(Transform parent)
    {
        var overlay = UIFactory.CreatePanel(parent, "MinigameResultsOverlay",
                                            Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.8f));
        LiftToFront(overlay);

        var window = UIFactory.CreatePanel(overlay, "Window",
                                           new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), UIFactory.PanelDark);
        UIFactory.Place(window, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1240f, 850f));

        var category = UIFactory.CreateText(window, "Category", "MINIGAME", 18f,
                                            UIFactory.TextDim, TextAlignmentOptions.Center);
        UIFactory.Place(category, new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(1100f, 22f));

        var title = UIFactory.CreateText(window, "Title", "", 32f, UIFactory.Accent, TextAlignmentOptions.Center);
        UIFactory.Place(title, new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(1100f, 48f));

        var stats = UIFactory.CreateText(window, "Stats", "", 24f, UIFactory.TextBright, TextAlignmentOptions.Top);
        UIFactory.Place(stats, new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(860f, 120f));
        stats.lineSpacing = 8f;

        var codeStats = UIFactory.CreateText(window, "CodeStats", "", 24f,
                                             UIFactory.Accent, TextAlignmentOptions.Center);
        UIFactory.Place(codeStats, new Vector2(0.5f, 0f), new Vector2(0f, 174f), new Vector2(1160f, 34f));
        codeStats.gameObject.SetActive(false);

        // CODE ANALYSIS row — hidden for non-code drills.
        var analysisGroup = UIFactory.CreateRect(window, "AnalysisGroup",
                                                 new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        UIFactory.Place(analysisGroup, new Vector2(0.5f, 0f), new Vector2(0f, 142f), new Vector2(1160f, 30f));
        var analysis = UIFactory.CreateText(analysisGroup, "Body", "", 19f,
                                            UIFactory.Accent, TextAlignmentOptions.Center);
        analysis.rectTransform.offsetMin = Vector2.zero;
        analysis.rectTransform.offsetMax = Vector2.zero;
        analysis.textWrappingMode = TextWrappingModes.NoWrap;
        analysis.overflowMode = TextOverflowModes.Ellipsis;
        analysisGroup.gameObject.SetActive(false);

        TMP_Dropdown attempts = UIFactory.CreateDropdown(window, "AttemptDropdown", new Vector2(320f, 34f), 16f);
        UIFactory.Place(attempts, new Vector2(0.5f, 1f), new Vector2(-390f, -92f), new Vector2(320f, 34f));
        var attemptStatus = UIFactory.CreateText(window, "AttemptStatus", "", 17f,
                                                 UIFactory.TextDim, TextAlignmentOptions.MidlineLeft);
        UIFactory.Place(attemptStatus, new Vector2(0.5f, 1f), new Vector2(120f, -92f), new Vector2(660f, 34f));

        var codeGroup = UIFactory.CreateRect(window, "CodeCompareGroup",
                                             new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        UIFactory.Place(codeGroup, new Vector2(0.5f, 1f), new Vector2(0f, -124f), new Vector2(1160f, 430f));

        var leftColumn = UIFactory.CreateRect(codeGroup, "LeftColumn",
                                              Vector2.zero, new Vector2(0.5f, 1f),
                                              Vector2.zero, new Vector2(-12f, 0f));
        var rightColumn = UIFactory.CreateRect(codeGroup, "RightColumn",
                                               new Vector2(0.5f, 0f), Vector2.one,
                                               new Vector2(12f, 0f), Vector2.zero);
        var divider = UIFactory.CreateRect(codeGroup, "Divider",
                                           new Vector2(0.5f, 0f), new Vector2(0.5f, 1f),
                                           new Vector2(-2f, 48f), new Vector2(2f, -6f));
        var dividerImage = divider.gameObject.AddComponent<Image>();
        dividerImage.color = new Color(0.18f, 0.12f, 0.07f, 0.9f);
        dividerImage.raycastTarget = false;

        var playerHeader = UIFactory.CreateText(leftColumn, "PlayerHeader", "YOUR SOLUTION", 22f,
                                                UIFactory.TextDim, TextAlignmentOptions.Center);
        UIFactory.Place(playerHeader, new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(520f, 30f));
        var referenceHeader = UIFactory.CreateText(rightColumn, "ReferenceHeader", "INTENDED SOLUTION", 22f,
                                                   new Color(0.55f, 0.78f, 1f), TextAlignmentOptions.Center);
        UIFactory.Place(referenceHeader, new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(520f, 30f));

        ScrollRect playerScroll = UIFactory.CreateScrollView(leftColumn, "PlayerScroll",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), out RectTransform playerContent);
        UIFactory.Place((RectTransform)playerScroll.transform, new Vector2(0.5f, 1f),
                        new Vector2(0f, -54f), new Vector2(536f, 400f));
        UIFactory.AddVerticalScrollbar(playerScroll, permanent: true);
        var playerText = UIFactory.CreateText(playerContent, "Text", "", 19f,
                                              UIFactory.TextBright, TextAlignmentOptions.TopLeft);
        playerText.enableWordWrapping = true;
        var playerFit = playerText.gameObject.AddComponent<ContentSizeFitter>();
        playerFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect referenceScroll = UIFactory.CreateScrollView(rightColumn, "ReferenceScroll",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), out RectTransform referenceContent);
        UIFactory.Place((RectTransform)referenceScroll.transform, new Vector2(0.5f, 1f),
                        new Vector2(0f, -54f), new Vector2(536f, 400f));
        UIFactory.AddVerticalScrollbar(referenceScroll, permanent: true);
        var referenceText = UIFactory.CreateText(referenceContent, "Text", "", 19f,
                                                 UIFactory.TextBright, TextAlignmentOptions.TopLeft);
        referenceText.enableWordWrapping = true;
        var referenceFit = referenceText.gameObject.AddComponent<ContentSizeFitter>();
        referenceFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        codeGroup.gameObject.SetActive(false);

        var mentor = UIFactory.CreateText(window, "MentorLabel", "", 18f,
                                          UIFactory.TextDim, TextAlignmentOptions.TopLeft);
        UIFactory.Place(mentor, new Vector2(0.5f, 0f), new Vector2(0f, 76f), new Vector2(1160f, 52f));
        mentor.enableWordWrapping = true;

        Button cont = UIFactory.CreateButton(window, "ContinueButton", "Continue", new Vector2(240f, 58f));
        UIFactory.LocalizeButton(cont, "common.continue");
        UIFactory.Place(cont, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(240f, 58f));
        cont.image.color = new Color(0.85f, 0.55f, 0.12f);

        var panel = overlay.gameObject.AddComponent<MinigameResultsPanel>();
        SceneBuilderUtil.Wire(panel, "root",           overlay.gameObject);
        SceneBuilderUtil.Wire(panel, "categoryLabel",  category);
        SceneBuilderUtil.Wire(panel, "titleLabel",     title);
        SceneBuilderUtil.Wire(panel, "statsLabel",     stats);
        SceneBuilderUtil.Wire(panel, "codeStatsLabel", codeStats);
        SceneBuilderUtil.Wire(panel, "analysisGroup",  analysisGroup.gameObject);
        SceneBuilderUtil.Wire(panel, "analysisLabel",  analysis);
        SceneBuilderUtil.Wire(panel, "attemptDropdown", attempts);
        SceneBuilderUtil.Wire(panel, "attemptStatusLabel", attemptStatus);
        SceneBuilderUtil.Wire(panel, "codeCompareGroup", codeGroup.gameObject);
        SceneBuilderUtil.Wire(panel, "playerSourceLabel", playerText);
        SceneBuilderUtil.Wire(panel, "referenceSourceLabel", referenceText);
        SceneBuilderUtil.Wire(panel, "mentorLabel", mentor);
        SceneBuilderUtil.Wire(panel, "continueButton", cont);
        return panel;
    }

    // -------------------------------------------------------------------------
    // Overworld minigame-station access card (placeholder). Pops when the player
    // interacts with a puzzle/code station; describes the (not-yet-wired) game and
    // lets them "start" it (counts it as solved for the three-objectives loop).

    public static MinigamePlaceholderPanel BuildMinigamePlaceholder(Transform parent)
    {
        var overlay = UIFactory.CreatePanel(parent, "MinigamePlaceholderOverlay",
                                            Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.78f));
        LiftToFront(overlay);

        var window = UIFactory.CreatePanel(overlay, "Window",
                                           new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                           UIFactory.PanelDark);
        UIFactory.Place(window, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 470f));

        // Accent strip across the top, tinted to the station marker colour at runtime.
        var accentRt = UIFactory.CreateRect(window, "AccentBar", new Vector2(0f, 1f), new Vector2(1f, 1f),
                                            new Vector2(0f, -8f), new Vector2(0f, 0f));
        accentRt.sizeDelta = new Vector2(0f, 8f);
        var accentBar = accentRt.gameObject.AddComponent<Image>();
        accentBar.sprite = SceneBuilderUtil.LoadPlaceholder("white_box");
        accentBar.color  = UIFactory.Accent;

        var category = UIFactory.CreateText(window, "Category", "PUZZLE", 18f,
                                            UIFactory.TextDim, TextAlignmentOptions.Center);
        UIFactory.Place(category, new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(560f, 22f));

        var title = UIFactory.CreateText(window, "Title", "", 32f, UIFactory.Accent, TextAlignmentOptions.Center);
        UIFactory.Place(title, new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(560f, 44f));

        var description = UIFactory.CreateText(window, "Description", "", 22f,
                                               UIFactory.TextBright, TextAlignmentOptions.Top);
        UIFactory.Place(description, new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(540f, 150f));
        description.enableWordWrapping = true;

        var concept = UIFactory.CreateText(window, "Concept", "", 20f,
                                           UIFactory.Accent, TextAlignmentOptions.Center);
        UIFactory.Place(concept, new Vector2(0.5f, 0f), new Vector2(0f, 196f), new Vector2(540f, 28f));

        var note = UIFactory.CreateText(window, "PlaceholderNote", "", 16f,
                                        UIFactory.TextDim, TextAlignmentOptions.Top);
        UIFactory.Place(note, new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(540f, 80f));
        note.enableWordWrapping = true;

        Button leave = UIFactory.CreateButton(window, "LeaveButton", "Leave", new Vector2(220f, 56f));
        UIFactory.Place(leave, new Vector2(0.5f, 0f), new Vector2(-130f, 28f), new Vector2(220f, 56f));

        Button start = UIFactory.CreateButton(window, "StartButton", "Start Puzzle", new Vector2(240f, 56f));
        UIFactory.Place(start, new Vector2(0.5f, 0f), new Vector2(130f, 28f), new Vector2(240f, 56f));
        start.image.color = new Color(0.20f, 0.55f, 0.25f);

        var panel = overlay.gameObject.AddComponent<MinigamePlaceholderPanel>();
        SceneBuilderUtil.Wire(panel, "root",             overlay.gameObject);
        SceneBuilderUtil.Wire(panel, "accentBar",        accentBar);
        SceneBuilderUtil.Wire(panel, "categoryLabel",    category);
        SceneBuilderUtil.Wire(panel, "titleLabel",       title);
        SceneBuilderUtil.Wire(panel, "descriptionLabel", description);
        SceneBuilderUtil.Wire(panel, "conceptLabel",     concept);
        SceneBuilderUtil.Wire(panel, "placeholderNote",  note);
        SceneBuilderUtil.Wire(panel, "startButton",      start);
        SceneBuilderUtil.Wire(panel, "leaveButton",      leave);
        SceneBuilderUtil.Wire(panel, "startButtonLabel", start.GetComponentInChildren<TMP_Text>(true));

        return panel;
    }

    // -------------------------------------------------------------------------
    // Overworld grid puzzle (Maze / BlockFill / PatternMatch) — a 6×6 board of
    // clickable cells reused across the three non-code station kinds.

    public static GridPuzzleMinigame BuildGridPuzzle(Transform parent)
    {
        var overlay = UIFactory.CreatePanel(parent, "GridPuzzleOverlay",
                                            Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.82f));
        LiftToFront(overlay);

        var window = UIFactory.CreatePanel(overlay, "Window",
                                           new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                           UIFactory.PanelDark);
        UIFactory.Place(window, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 760f));

        var title = UIFactory.CreateText(window, "Title", "", 28f, UIFactory.Accent, TextAlignmentOptions.Center);
        UIFactory.Place(title, new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(640f, 40f));

        var instruction = UIFactory.CreateText(window, "Instruction", "", 19f,
                                               UIFactory.TextDim, TextAlignmentOptions.Center);
        UIFactory.Place(instruction, new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(640f, 48f));
        instruction.enableWordWrapping = true;

        int n = GridPuzzleMinigame.Grid;
        var grid = UIFactory.CreateRect(window, "Grid", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        UIFactory.Place(grid, new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(540f, 540f));
        var gl = grid.gameObject.AddComponent<GridLayoutGroup>();
        gl.cellSize        = new Vector2(84f, 84f);
        gl.spacing         = new Vector2(6f, 6f);
        gl.startCorner     = GridLayoutGroup.Corner.UpperLeft;
        gl.startAxis       = GridLayoutGroup.Axis.Horizontal;
        gl.childAlignment  = TextAnchor.MiddleCenter;
        gl.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        gl.constraintCount = n;

        var images  = new Image[n * n];
        var buttons = new Button[n * n];
        for (int i = 0; i < n * n; i++)
        {
            var cellRt = UIFactory.CreateRect(grid, $"Cell_{i}",
                                              new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            cellRt.sizeDelta = new Vector2(84f, 84f);
            var img = cellRt.gameObject.AddComponent<Image>();
            img.sprite = UIFactory.BuiltinSprite("UISprite.psd");
            img.type   = Image.Type.Sliced;
            img.color  = new Color(0.24f, 0.26f, 0.32f);
            var btn = cellRt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            // The game controls cell colour directly, so don't let the button's
            // tint transition fight it.
            btn.transition = Selectable.Transition.None;
            images[i]  = img;
            buttons[i] = btn;
        }

        var feedback = UIFactory.CreateText(window, "Feedback", "", 22f, UIFactory.TextBright, TextAlignmentOptions.Center);
        UIFactory.Place(feedback, new Vector2(0.5f, 0f), new Vector2(0f, 96f), new Vector2(640f, 32f));

        Button reset = UIFactory.CreateButton(window, "ResetButton", "Reset", new Vector2(200f, 56f));
        UIFactory.Place(reset, new Vector2(0.5f, 0f), new Vector2(-120f, 28f), new Vector2(200f, 56f));

        Button quit = UIFactory.CreateButton(window, "QuitButton", "Leave", new Vector2(200f, 56f));
        UIFactory.Place(quit, new Vector2(0.5f, 0f), new Vector2(120f, 28f), new Vector2(200f, 56f));

        var game = overlay.gameObject.AddComponent<GridPuzzleMinigame>();
        SceneBuilderUtil.Wire(game, "root",             overlay.gameObject);
        SceneBuilderUtil.Wire(game, "titleLabel",       title);
        SceneBuilderUtil.Wire(game, "instructionLabel", instruction);
        SceneBuilderUtil.Wire(game, "feedbackLabel",    feedback);
        SceneBuilderUtil.WireArray(game, "cellImages",  images);
        SceneBuilderUtil.WireArray(game, "cellButtons", buttons);
        SceneBuilderUtil.Wire(game, "resetButton",      reset);
        SceneBuilderUtil.Wire(game, "quitButton",       quit);
        return game;
    }

    // -------------------------------------------------------------------------
    // Overworld coding challenge — reorder shuffled program lines (concept-tied).

    public static CodeOrderMinigame BuildCodeOrder(Transform parent)
    {
        var overlay = UIFactory.CreatePanel(parent, "CodeOrderOverlay",
                                            Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.82f));
        LiftToFront(overlay);

        var window = UIFactory.CreatePanel(overlay, "Window",
                                           new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                           UIFactory.PanelDark);
        UIFactory.Place(window, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 700f));

        var title = UIFactory.CreateText(window, "Title", "", 26f, UIFactory.Accent, TextAlignmentOptions.Center);
        UIFactory.Place(title, new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(680f, 40f));

        var goal = UIFactory.CreateText(window, "Goal", "", 19f, UIFactory.TextDim, TextAlignmentOptions.Center);
        UIFactory.Place(goal, new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(660f, 44f));
        goal.enableWordWrapping = true;

        var list = UIFactory.CreateRect(window, "Cards", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        UIFactory.Place(list, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(620f, 396f));
        UIFactory.AddVerticalLayout(list, 8f, align: TextAnchor.UpperCenter);

        int max = CodeOrderMinigame.MaxLines;
        var labels = new TMP_Text[max];
        var bgs    = new Image[max];
        var ups    = new Button[max];
        var downs  = new Button[max];
        for (int i = 0; i < max; i++)
        {
            var row = UIFactory.CreateRect(list, $"Card_{i}",
                                           new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            row.sizeDelta = new Vector2(600f, 56f);
            UIFactory.SetLayoutSize(row, 600f, 56f);
            var bg = row.gameObject.AddComponent<Image>();
            bg.sprite = UIFactory.BuiltinSprite("UISprite.psd");
            bg.type   = Image.Type.Sliced;
            bg.color  = new Color(0.22f, 0.30f, 0.42f);

            var label = UIFactory.CreateText(row, "Label", "", 22f, UIFactory.TextBright,
                                             TextAlignmentOptions.MidlineLeft);
            UIFactory.Place(label, new Vector2(0f, 0.5f), new Vector2(232f, 0f), new Vector2(420f, 48f));

            Button up   = UIFactory.CreateButton(row, "Up",   "▲", new Vector2(62f, 46f), 22f);
            UIFactory.Place(up,   new Vector2(1f, 0.5f), new Vector2(-84f, 0f), new Vector2(62f, 46f));
            Button down = UIFactory.CreateButton(row, "Down", "▼", new Vector2(62f, 46f), 22f);
            UIFactory.Place(down, new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(62f, 46f));

            labels[i] = label; bgs[i] = bg; ups[i] = up; downs[i] = down;
        }

        var feedback = UIFactory.CreateText(window, "Feedback", "", 22f, UIFactory.TextBright, TextAlignmentOptions.Center);
        UIFactory.Place(feedback, new Vector2(0.5f, 0f), new Vector2(0f, 160f), new Vector2(660f, 32f));

        Button hint = UIFactory.CreateButton(window, "HintButton", "Hint", new Vector2(150f, 46f), 18f);
        UIFactory.Place(hint, new Vector2(0.5f, 0f), new Vector2(0f, 76f), new Vector2(150f, 46f));
        hint.image.color = UIFactory.Accent;
        hint.gameObject.SetActive(false);

        var hintText = UIFactory.CreateText(window, "HintLabel", "", 17f, UIFactory.TextDim,
                                            TextAlignmentOptions.Center);
        UIFactory.Place(hintText, new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(660f, 42f));
        hintText.enableWordWrapping = true;

        Button run = UIFactory.CreateButton(window, "RunButton", "▶ RUN", new Vector2(200f, 56f));
        UIFactory.Place(run, new Vector2(0.5f, 0f), new Vector2(-120f, 28f), new Vector2(200f, 56f));
        run.image.color = new Color(0.20f, 0.55f, 0.25f);

        Button quit = UIFactory.CreateButton(window, "QuitButton", "Leave", new Vector2(200f, 56f));
        UIFactory.Place(quit, new Vector2(0.5f, 0f), new Vector2(120f, 28f), new Vector2(200f, 56f));

        var game = overlay.gameObject.AddComponent<CodeOrderMinigame>();
        SceneBuilderUtil.Wire(game, "resultsPanel", BuildResultsCard(parent));
        SceneBuilderUtil.Wire(game, "root",          overlay.gameObject);
        SceneBuilderUtil.Wire(game, "titleLabel",    title);
        SceneBuilderUtil.Wire(game, "goalLabel",     goal);
        SceneBuilderUtil.Wire(game, "feedbackLabel", feedback);
        SceneBuilderUtil.WireArray(game, "cardLabels",      labels);
        SceneBuilderUtil.WireArray(game, "cardBackgrounds", bgs);
        SceneBuilderUtil.WireArray(game, "upButtons",       ups);
        SceneBuilderUtil.WireArray(game, "downButtons",     downs);
        SceneBuilderUtil.Wire(game, "runButton",  run);
        SceneBuilderUtil.Wire(game, "quitButton", quit);
        SceneBuilderUtil.Wire(game, "hintButton", hint);
        SceneBuilderUtil.Wire(game, "hintLabel",  hintText);
        return game;
    }

    public static FlowConnectMinigame BuildFlowConnect(Transform parent)
    {
        var overlay = UIFactory.CreatePanel(parent, "FlowConnectOverlay",
                                            Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.80f));
        LiftToFront(overlay);

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
        SceneBuilderUtil.Wire(game, "resultsPanel", BuildResultsCard(parent));
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
        LiftToFront(overlay);

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
        SceneBuilderUtil.Wire(game, "resultsPanel", BuildResultsCard(parent));
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
        LiftToFront(overlay);

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
        SceneBuilderUtil.Wire(minigame, "resultsPanel", BuildResultsCard(parent));
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
        LiftToFront(overlay);

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
        SceneBuilderUtil.Wire(game, "resultsPanel", BuildResultsCard(parent));
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
        LiftToFront(overlay);

        var window = UIFactory.CreatePanel(overlay, "Window",
                                           new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                           UIFactory.PanelDark);
        UIFactory.Place(window, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 660f));

        var title = UIFactory.CreateText(window, "Title", "", 26f, UIFactory.Accent);
        UIFactory.Place(title, new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(680f, 44f));

        Button hint = UIFactory.CreateButton(window, "HintButton", "Hint", new Vector2(140f, 38f), 17f);
        UIFactory.Place(hint, new Vector2(1f, 1f), new Vector2(-92f, -18f), new Vector2(140f, 38f));
        hint.image.color = UIFactory.Accent;
        hint.gameObject.SetActive(false);

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

        var hintText = UIFactory.CreateText(window, "HintLabel", "", 17f, UIFactory.TextDim,
                                            TextAlignmentOptions.Center);
        UIFactory.Place(hintText, new Vector2(0.5f, 0f), new Vector2(0f, 132f), new Vector2(660f, 48f));
        hintText.enableWordWrapping = true;

        var game = overlay.gameObject.AddComponent<CodeFixMinigame>();
        SceneBuilderUtil.Wire(game, "resultsPanel", BuildResultsCard(parent));
        SceneBuilderUtil.Wire(game, "root",          overlay.gameObject);
        SceneBuilderUtil.Wire(game, "titleLabel",    title);
        SceneBuilderUtil.Wire(game, "feedbackLabel", feedback);
        SceneBuilderUtil.WireArray(game, "cardLabels",      labels);
        SceneBuilderUtil.WireArray(game, "cardBackgrounds", bgs);
        SceneBuilderUtil.WireArray(game, "upButtons",       ups);
        SceneBuilderUtil.WireArray(game, "downButtons",     downs);
        SceneBuilderUtil.Wire(game, "runButton", run);
        SceneBuilderUtil.Wire(game, "hintButton", hint);
        SceneBuilderUtil.Wire(game, "hintLabel",  hintText);
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
        LiftToFront(overlay);

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

        Button run = UIFactory.CreateButton(left, "RunButton", "▶ RUN", new Vector2(160f, 56f));
        UIFactory.Place(run, new Vector2(0.5f, 0f), new Vector2(-170f, 16f), new Vector2(160f, 56f));
        run.image.color = new Color(0.20f, 0.55f, 0.25f);

        Button reset = UIFactory.CreateButton(left, "ResetButton", "↺ Reset", new Vector2(160f, 56f));
        UIFactory.Place(reset, new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(160f, 56f));

        // Autopilot (testing): loads a known-good solver into the active surface and runs it.
        Button autopilot = UIFactory.CreateButton(left, "AutopilotButton", "🤖 Autopilot", new Vector2(160f, 56f), 18f);
        UIFactory.Place(autopilot, new Vector2(0.5f, 0f), new Vector2(170f, 16f), new Vector2(160f, 56f));
        autopilot.image.color = new Color(0.30f, 0.45f, 0.75f);

        // Co-Pilot hint (shared flow, minigame voice) — hidden until the player struggles.
        Button hintBtn = UIFactory.CreateButton(left, "HintButton", "💡 Hint", new Vector2(150f, 44f), 18f);
        UIFactory.Place(hintBtn, new Vector2(1f, 0f), new Vector2(-10f, 78f), new Vector2(150f, 44f));
        hintBtn.image.color = UIFactory.Accent;
        hintBtn.gameObject.SetActive(false);

        TMP_Text hintLbl = UIFactory.CreateText(left, "HintLabel", "", 16f, UIFactory.TextDim,
                                                TextAlignmentOptions.TopLeft);
        UIFactory.Place(hintLbl, new Vector2(0.5f, 0f), new Vector2(0f, 112f), new Vector2(520f, 36f));
        hintLbl.enableWordWrapping = true;

        // --- Right column: the two editor windows (one shown per the setting) -----
        var editorArea = UIFactory.CreateRect(window, "EditorArea",
                                              new Vector2(0f, 0f), new Vector2(1f, 1f),
                                              new Vector2(580f, 20f), new Vector2(-20f, -56f));

        // This overlay has its own left-column Run/Pause/Reset/Step/Speed/Autopilot
        // controls (above), so the windows' embedded toolbar is skipped here.
        RectTransform blockPanel = AutomationDriveSceneBuilder.BuildBlockWindow(
            editorArea, (RectTransform)overlay.transform,
            out BlockPaletteController palette, out BlockCanvasController blockCanvas,
            out _, out _, out _, out _, out _, out _, out _, embedToolbar: false);
        RectTransform codePanel = AutomationDriveSceneBuilder.BuildCodeWindow(
            editorArea, out CodeEditorController codeEditor, out VibeCodingController mazeVibe,
            out _, out _, out _, out _, out _, out _, out _,
            out _, out _, embedToolbar: false);

        // Execution engine (drives the shared AgentSim through the maze grid).
        var exec = overlay.gameObject.AddComponent<ExecutionController>();

        var game = overlay.gameObject.AddComponent<MazeRepairMinigame>();
        SceneBuilderUtil.Wire(game, "resultsPanel", BuildResultsCard(parent));
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
        SceneBuilderUtil.Wire(game, "vibeCtrl",      mazeVibe);
        SceneBuilderUtil.Wire(game, "ghost",         codeEditor.GetComponent<GhostTextController>());
        SceneBuilderUtil.Wire(game, "exec",          exec);
        SceneBuilderUtil.Wire(game, "runButton",     run);
        SceneBuilderUtil.Wire(game, "resetButton",   reset);
        SceneBuilderUtil.Wire(game, "autopilotButton", autopilot);
        SceneBuilderUtil.Wire(game, "hintButton",    hintBtn);
        SceneBuilderUtil.Wire(game, "hintLabel",     hintLbl);

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
