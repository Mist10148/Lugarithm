using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds AutomationDrive.unity — the Automation Mode scene: world camera on
/// the left 40% (iso grid spawned at runtime), code/block workspace on the
/// right 60%, execution control bar top-center, console + monitor at the
/// bottom of the workspace, and the results overlay.
/// </summary>
public static class AutomationDriveSceneBuilder
{
    public static void Build()
    {
        var scene = SceneBuilderUtil.NewScene();

        // --- World (full-screen top-down; iso fallback still present) ---------------

        Camera worldCam = SceneBuilderUtil.CreateCamera2D("World Camera",
                                                          new Color(0.07f, 0.09f, 0.12f), 5f);
        var cameraFollow = worldCam.gameObject.AddComponent<CameraFollow2D>();
        SceneBuilderUtil.CreateGlobalLight2D();
        SceneBuilderUtil.CreateEventSystem();

        // Iso fallback for authored mazes (Oton, tests).
        var gridRoot = new GameObject("GridRoot");
        var worldView = gridRoot.AddComponent<GridWorldView>();

        var jeepneyGo = new GameObject("AgentJeepney");
        var isoBody = jeepneyGo.AddComponent<SpriteRenderer>();
        isoBody.sprite = SceneBuilderUtil.LoadPlaceholder("iso_jeepney");
        isoBody.sortingOrder = 100;

        var arrowGo = new GameObject("Arrow");
        arrowGo.transform.SetParent(jeepneyGo.transform, false);
        arrowGo.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        arrowGo.transform.localScale = Vector3.one * 0.8f;
        var arrow = arrowGo.AddComponent<SpriteRenderer>();
        arrow.sprite = SceneBuilderUtil.LoadPlaceholder("triangle");
        arrow.color = Color.white;
        arrow.sortingOrder = 101;

        var agentView = jeepneyGo.AddComponent<JeepneyAgentView>();
        SceneBuilderUtil.Wire(agentView, "body",  isoBody);
        SceneBuilderUtil.Wire(agentView, "arrow", arrow);

        // Top-down procedural world root + agent.
        var topDownWorldRoot = new GameObject("TopDownWorldRoot").transform;

        var topDownAgentGo = new GameObject("TopDownAgent");
        var tdBody = topDownAgentGo.AddComponent<SpriteRenderer>();
        tdBody.sprite = SceneBuilderUtil.LoadPlaceholder("jeepney_top");
        tdBody.sortingOrder = 10;
        var topDownAgent = topDownAgentGo.AddComponent<TopDownAgentView>();
        topDownAgent.body = tdBody;

        // --- Canvas ---------------------------------------------------------------------

        Canvas canvas = UIFactory.CreateCanvas("WorkspaceCanvas");

        // Workspace backdrop first (right 60%, full height) so everything else
        // draws above it — uncovered screen areas outside the world camera's
        // viewport would otherwise show garbage.
        var workspace = UIFactory.CreatePanel(canvas.transform, "Workspace",
                                              new Vector2(0.4f, 0f), new Vector2(1f, 1f),
                                              UIFactory.PanelDarker);
        workspace.offsetMin = Vector2.zero;
        workspace.offsetMax = Vector2.zero;

        // Goal banner (top-left, over the world view)
        var goalBanner = UIFactory.CreatePanel(canvas.transform, "GoalBanner",
                                               new Vector2(0f, 1f), new Vector2(0f, 1f),
                                               new Color(0.06f, 0.07f, 0.10f, 0.85f));
        UIFactory.Place(goalBanner, new Vector2(0f, 1f), new Vector2(10f, -10f), new Vector2(742f, 92f));
        var goalText = UIFactory.CreateText(goalBanner, "GoalText", "", 20f,
                                            UIFactory.TextBright, TextAlignmentOptions.TopLeft);
        goalText.rectTransform.offsetMin = new Vector2(12f, 6f);
        goalText.rectTransform.offsetMax = new Vector2(-12f, -6f);

        // Control bar (top-center)
        var controlBar = UIFactory.CreatePanel(canvas.transform, "ControlBar",
                                               new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                               UIFactory.PanelDark);
        UIFactory.Place(controlBar, new Vector2(0.5f, 1f), new Vector2(120f, 0f), new Vector2(700f, 56f));
        UIFactory.AddHorizontalLayout(controlBar, 8f, new RectOffset(10, 10, 6, 6), TextAnchor.MiddleCenter);

        Button run    = MakeBarButton(controlBar, "RunButton",   "▶ RUN",  120f);
        run.image.color = new Color(0.20f, 0.55f, 0.25f);
        Button pause  = MakeBarButton(controlBar, "PauseButton", "❚❚",      70f);
        Button reset  = MakeBarButton(controlBar, "ResetButton", "↺ Reset", 110f);
        Button speed1 = MakeBarButton(controlBar, "Speed1",      "1×",      64f);
        Button speed2 = MakeBarButton(controlBar, "Speed2",      "2×",      64f);
        Button speed5 = MakeBarButton(controlBar, "Speed5",      "5×",      64f);
        Button step   = MakeBarButton(controlBar, "StepButton",  "Step",    80f);

        Slider speedSlider = UIFactory.CreateSlider(controlBar, "SpeedSlider", new Vector2(180f, 36f));
        speedSlider.minValue = 0.2f;
        speedSlider.maxValue = 8f;
        speedSlider.value = 1f;
        var sliderLe = speedSlider.gameObject.GetComponent<LayoutElement>();
        if (sliderLe == null) sliderLe = speedSlider.gameObject.AddComponent<LayoutElement>();
        sliderLe.preferredWidth = 180f;
        sliderLe.preferredHeight = 36f;

        TMP_Text speedLabel = UIFactory.CreateText(controlBar, "SpeedLabel", "×1.0", 20f, UIFactory.TextBright);
        var labelLe = speedLabel.gameObject.GetComponent<LayoutElement>();
        if (labelLe == null) labelLe = speedLabel.gameObject.AddComponent<LayoutElement>();
        labelLe.preferredWidth = 56f;
        labelLe.preferredHeight = 36f;

        Button autopilot = MakeBarButton(controlBar, "Autopilot", "🤖 Auto", 120f);
        autopilot.image.color = new Color(0.30f, 0.45f, 0.75f);

        // Exit (top-right corner)
        Button exit = UIFactory.CreateButton(canvas.transform, "ExitButton", "Exit", new Vector2(110f, 42f));
        UIFactory.Place(exit, new Vector2(1f, 1f), new Vector2(-10f, -8f), new Vector2(110f, 42f));
        var link = exit.gameObject.AddComponent<SceneLink>();
        SceneBuilderUtil.Wire(link, "button",    exit);
        SceneBuilderUtil.Wire(link, "sceneName", "LevelSelect");

        // --- Floating editor windows (Block / Code) ---------------------------------

        var editorArea = UIFactory.CreateRect(canvas.transform, "EditorArea",
                                              Vector2.zero, Vector2.one,
                                              Vector2.zero, Vector2.zero);

        RectTransform blockPanel = BuildBlockWindow(
            editorArea, (RectTransform)canvas.transform, out BlockPaletteController paletteCtrl, out BlockCanvasController blockCanvas);
        UIFactory.Place(blockPanel, new Vector2(0.62f, 0.5f), Vector2.zero, new Vector2(760f, 640f));

        RectTransform codePanel = BuildCodeWindow(
            editorArea, out CodeEditorController codeEditor, out Button codeChatButton);
        UIFactory.Place(codePanel, new Vector2(0.62f, 0.5f), Vector2.zero, new Vector2(760f, 640f));

        // Co-Pilot hint button + label (bottom-right of workspace)
        Button hintBtn = UIFactory.CreateButton(workspace, "HintButton",
                                                  "💡 Ask for a hint", new Vector2(200f, 40f), 20f);
        UIFactory.Place(hintBtn, new Vector2(1f, 0f), new Vector2(-120f, 60f), new Vector2(200f, 40f));
        hintBtn.image.color = UIFactory.Accent;
        hintBtn.gameObject.SetActive(false);

        TMP_Text hintLbl = UIFactory.CreateText(workspace, "HintLabel", "", 20f, UIFactory.TextDim);
        UIFactory.Place(hintLbl, new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(600f, 60f));
        hintLbl.enableWordWrapping = true;

        // Monitor + console (bottom of workspace)
        var monitorLine = UIFactory.CreatePanel(workspace, "Monitor",
                                                new Vector2(0f, 0f), new Vector2(1f, 0f),
                                                UIFactory.PanelDark);
        UIFactory.Place(monitorLine, new Vector2(0.5f, 0f), new Vector2(0f, 222f), new Vector2(0f, 30f));
        monitorLine.anchorMin = new Vector2(0f, 0f);
        monitorLine.anchorMax = new Vector2(1f, 0f);
        monitorLine.offsetMin = new Vector2(14f, 222f);
        monitorLine.offsetMax = new Vector2(-14f, 252f);

        var monitorText = UIFactory.CreateText(monitorLine, "Text", "", 17f,
                                               UIFactory.Accent, TextAlignmentOptions.MidlineLeft);
        monitorText.rectTransform.offsetMin = new Vector2(10f, 0f);
        monitorText.rectTransform.offsetMax = new Vector2(-10f, 0f);

        var monitor = monitorLine.gameObject.AddComponent<StateMonitorController>();
        SceneBuilderUtil.Wire(monitor, "label", monitorText);

        ConsoleController console = BuildConsole(workspace);

        // Results overlay (full screen)
        AutomationResultsPanel results = BuildResults(canvas);

        // Town gates (non-code, required to advance) — the level picks one.
        FlowConnectMinigame flowPuzzle  = MinigameOverlayBuilder.BuildFlowConnect(canvas.transform);
        CrateStackMinigame  cratePuzzle = MinigameOverlayBuilder.BuildCrateStack(canvas.transform);
        // Tutorial repair drills: code-based maze escape + non-code refuel.
        MazeRepairMinigame  mazeRepair  = MinigameOverlayBuilder.BuildMazeRepair(canvas.transform);
        RefuelMinigame      refuel      = MinigameOverlayBuilder.BuildRefuel(canvas.transform);
        DialogueController  dialogue    = DialogueOverlayBuilder.BuildDriveDialogue(canvas.transform);
        LegCompletionController legCompletion = LegCompletionOverlayBuilder.Build(canvas.transform);

        // Vibe Coding / Autopilot floating chat window.
        VibeCodingController vibeCtrl = BuildVibeCodingWindow((RectTransform)canvas.transform);

        // --- Orchestrator -------------------------------------------------------------------

        var controllerGo = new GameObject("AutomationController");
        var exec = controllerGo.AddComponent<ExecutionController>();
        var selfDrive = controllerGo.AddComponent<SelfDriveAgent>();
        var controller = controllerGo.AddComponent<AutomationDriveController>();

        SceneBuilderUtil.Wire(controller, "worldCamera",    worldCam);
        SceneBuilderUtil.Wire(controller, "worldView",      worldView);
        SceneBuilderUtil.Wire(controller, "agentView",      agentView);
        SceneBuilderUtil.Wire(controller, "topDownWorldRoot", topDownWorldRoot);
        SceneBuilderUtil.Wire(controller, "topDownAgentView", topDownAgent);
        SceneBuilderUtil.Wire(controller, "cameraFollow",   cameraFollow);
        SceneBuilderUtil.Wire(controller, "exec",           exec);
        SceneBuilderUtil.Wire(controller, "goalLabel",      goalText);
        SceneBuilderUtil.Wire(controller, "blockPanel",     blockPanel.gameObject);
        SceneBuilderUtil.Wire(controller, "codePanel",      codePanel.gameObject);
        SceneBuilderUtil.Wire(controller, "blockCanvas",    blockCanvas);
        SceneBuilderUtil.Wire(controller, "palette",        paletteCtrl);
        SceneBuilderUtil.Wire(controller, "codeEditor",     codeEditor);
        SceneBuilderUtil.Wire(controller, "codeChatButton", codeChatButton);
        SceneBuilderUtil.Wire(controller, "runButton",      run);
        SceneBuilderUtil.Wire(controller, "pauseButton",    pause);
        SceneBuilderUtil.Wire(controller, "resetButton",    reset);
        SceneBuilderUtil.Wire(controller, "speed1Button",   speed1);
        SceneBuilderUtil.Wire(controller, "speed2Button",   speed2);
        SceneBuilderUtil.Wire(controller, "speed5Button",   speed5);
        SceneBuilderUtil.Wire(controller, "speedSlider",    speedSlider);
        SceneBuilderUtil.Wire(controller, "speedLabel",     speedLabel);
        SceneBuilderUtil.Wire(controller, "stepButton",     step);
        SceneBuilderUtil.Wire(controller, "autopilotButton", autopilot);
        SceneBuilderUtil.Wire(controller, "selfDrive",      selfDrive);
        SceneBuilderUtil.Wire(controller, "console",        console);
        SceneBuilderUtil.Wire(controller, "monitor",        monitor);
        SceneBuilderUtil.Wire(controller, "results",        results);
        SceneBuilderUtil.Wire(controller, "flowPuzzle",     flowPuzzle);
        SceneBuilderUtil.Wire(controller, "cratePuzzle",    cratePuzzle);
        SceneBuilderUtil.Wire(controller, "mazeRepairMinigame", mazeRepair);
        SceneBuilderUtil.Wire(controller, "refuelMinigame",     refuel);
        SceneBuilderUtil.Wire(controller, "dialogue",       dialogue);
        SceneBuilderUtil.Wire(controller, "hintButton",     hintBtn);
        SceneBuilderUtil.Wire(controller, "hintLabel",      hintLbl);
        SceneBuilderUtil.Wire(controller, "vibeCtrl",       vibeCtrl);
        SceneBuilderUtil.Wire(controller, "legCompletion",  legCompletion);

        SceneBuilderUtil.SaveScene(scene, "AutomationDrive");
    }

    // -------------------------------------------------------------------------
    // Dedicated editor windows (Block / Code) — shared by CodeDrive + Maze so a
    // run shows exactly one editor in its own titled, floating panel, chosen by
    // the Block/Code setting. The palette lives *inside* the block window, so
    // Code mode shows no block UI at all.

    /// <summary>A titled floating panel; <paramref name="content"/> is the body below the title bar.</summary>
    internal static RectTransform BuildWindow(RectTransform parent, string name, string title,
                                              out RectTransform content, out RectTransform titleBar)
    {
        var window = UIFactory.CreatePanel(parent, name, Vector2.zero, Vector2.one, UIFactory.PanelDark);
        window.offsetMin = Vector2.zero;
        window.offsetMax = Vector2.zero;

        titleBar = UIFactory.CreatePanel(window, "TitleBar", new Vector2(0f, 1f), new Vector2(1f, 1f),
                                         new Color(0.06f, 0.07f, 0.10f, 1f));
        titleBar.offsetMin = new Vector2(0f, -34f);
        titleBar.offsetMax = Vector2.zero;
        var titleText = UIFactory.CreateText(titleBar, "Title", title, 18f, UIFactory.Accent,
                                              TextAlignmentOptions.MidlineLeft);
        titleText.rectTransform.offsetMin = new Vector2(14f, 0f);
        titleText.rectTransform.offsetMax = new Vector2(-90f, 0f);

        content = UIFactory.CreateRect(window, "Content", Vector2.zero, Vector2.one,
                                       Vector2.zero, new Vector2(0f, -34f));
        return window;
    }

    /// <summary>Backward-compatible <see cref="BuildWindow"/> without title-bar out.</summary>
    internal static RectTransform BuildWindow(RectTransform parent, string name, string title,
                                              out RectTransform content)
    {
        return BuildWindow(parent, name, title, out content, out _);
    }

    /// <summary>Scratch-style block window: palette column + drag-and-drop canvas.</summary>
    internal static RectTransform BuildBlockWindow(RectTransform parent, RectTransform dragLayer,
                                                   out BlockPaletteController palette,
                                                   out BlockCanvasController canvas)
    {
        RectTransform window = BuildWindow(parent, "BlockWindow", "BLOCKS — drag to build",
                                           out RectTransform content, out RectTransform titleBar);

        // Make the title bar draggable.
        if (titleBar != null)
        {
            var drag = titleBar.gameObject.AddComponent<DragWindowHandle>();
            SceneBuilderUtil.Wire(drag, "windowRoot", window.gameObject);
        }

        var paletteFrame = UIFactory.CreatePanel(content, "Palette",
                                                 new Vector2(0f, 0f), new Vector2(0f, 1f),
                                                 UIFactory.PanelDarker);
        paletteFrame.offsetMin = new Vector2(8f, 8f);
        paletteFrame.offsetMax = new Vector2(222f, -8f);

        var paletteHeader = UIFactory.CreateText(paletteFrame, "Header", "PALETTE", 18f, UIFactory.TextDim);
        UIFactory.Place(paletteHeader, new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(190f, 26f));

        var paletteContent = UIFactory.CreateRect(paletteFrame, "Content",
                                                  Vector2.zero, Vector2.one,
                                                  new Vector2(8f, 8f), new Vector2(-8f, -36f));
        UIFactory.AddVerticalLayout(paletteContent, 8f, align: TextAnchor.UpperCenter);

        Button paletteTemplate = UIFactory.CreateButton(paletteContent, "PaletteButtonTemplate",
                                                        "block", new Vector2(190f, 46f), 21f);
        paletteTemplate.gameObject.SetActive(false);

        palette = paletteFrame.gameObject.AddComponent<BlockPaletteController>();
        SceneBuilderUtil.Wire(palette, "content",        paletteContent);
        SceneBuilderUtil.Wire(palette, "buttonTemplate", paletteTemplate);

        var canvasArea = UIFactory.CreateRect(content, "BlockCanvasArea",
                                              new Vector2(0f, 0f), new Vector2(1f, 1f),
                                              new Vector2(230f, 8f), new Vector2(-8f, -8f));
        canvas = BuildBlockCanvas(canvasArea, dragLayer);

        return window;
    }

    /// <summary>"The Farmer Was Replaced"-style code window: gutter + input + lint.</summary>
    internal static RectTransform BuildCodeWindow(RectTransform parent, out CodeEditorController editor,
                                                  out Button chatButton)
    {
        RectTransform window = BuildWindow(parent, "CodeWindow", "CODE — type to program",
                                           out RectTransform content, out RectTransform titleBar);

        chatButton = null;
        if (titleBar != null)
        {
            // Draggable title bar.
            var drag = titleBar.gameObject.AddComponent<DragWindowHandle>();
            SceneBuilderUtil.Wire(drag, "windowRoot", window.gameObject);

            // AI chat toggle in the title bar.
            chatButton = UIFactory.CreateButton(titleBar, "ChatButton", "✦ AI", new Vector2(80f, 26f), 16f);
            UIFactory.Place(chatButton, new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(80f, 26f));
        }

        editor = BuildCodeEditor(content);
        return window;
    }

    /// <summary>VS Code Copilot-style floating chat that generates automation code.</summary>
    internal static VibeCodingController BuildVibeCodingWindow(RectTransform parent)
    {
        RectTransform content;
        RectTransform windowRoot = BuildWindow(parent, "VibeCodingWindow", "✦ AI COPILOT", out content);
        UIFactory.Place(windowRoot, new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(420f, 500f));

        // Make the title bar draggable.
        var titleBar = windowRoot.Find("TitleBar") as RectTransform;
        if (titleBar != null)
        {
            var drag = titleBar.gameObject.AddComponent<DragWindowHandle>();
            SceneBuilderUtil.Wire(drag, "windowRoot", windowRoot.gameObject);
        }

        // Chat history (scrollable, fills top of window).
        ScrollRect scroll = UIFactory.CreateScrollView(content, "ChatHistory",
                                                       Vector2.zero, Vector2.one,
                                                       out RectTransform chatContent);
        UIFactory.Place(scroll, new Vector2(0f, 1f), new Vector2(0f, -8f), new Vector2(0f, -80f));
        scroll.vertical = true;

        TMP_Text historyLabel = UIFactory.CreateText(chatContent, "History", "", 18f, UIFactory.TextDim,
                                                     TextAlignmentOptions.TopLeft);
        historyLabel.enableWordWrapping = true;

        // Input row at the bottom.
        TMP_InputField inputField = UIFactory.CreateMultilineInput(content, "ChatInput",
                                                                   new Vector2(0f, 0f), new Vector2(1f, 0f), 20f);
        inputField.lineType = TMP_InputField.LineType.SingleLine;
        UIFactory.Place(inputField.GetComponent<RectTransform>(),
                        new Vector2(0f, 0f), new Vector2(45f, 10f), new Vector2(-90f, 44f));
        inputField.placeholder.GetComponent<TMP_Text>().text = "Describe what you want the jeepney to do...";

        Button sendBtn = UIFactory.CreateButton(content, "SendBtn", "▶", new Vector2(70f, 44f), 24f);
        UIFactory.Place(sendBtn.GetComponent<RectTransform>(),
                        new Vector2(1f, 0f), new Vector2(-10f, 10f), new Vector2(70f, 44f));
        sendBtn.image.color = UIFactory.Accent;

        // Wire controller.
        var vibeCtrl = windowRoot.gameObject.AddComponent<VibeCodingController>();
        SceneBuilderUtil.Wire(vibeCtrl, "chatInput",    inputField.gameObject);
        SceneBuilderUtil.Wire(vibeCtrl, "historyLabel", historyLabel.gameObject);
        SceneBuilderUtil.Wire(vibeCtrl, "sendButton",   sendBtn.gameObject);

        return vibeCtrl;
    }

    // -------------------------------------------------------------------------
    // Block canvas

    internal static BlockCanvasController BuildBlockCanvas(RectTransform parent, RectTransform dragLayer)
    {
        ScrollRect scroll = UIFactory.CreateScrollView(parent, "CanvasScroll",
                                                       Vector2.zero, Vector2.one,
                                                       out RectTransform content);
        // Leave a strip at the bottom for the trash zone.
        ((RectTransform)scroll.transform).offsetMin = new Vector2(0f, 44f);

        // Trash zone (drag a block here to delete it).
        var trash = UIFactory.CreatePanel(parent, "TrashZone",
                                          new Vector2(0f, 0f), new Vector2(1f, 0f),
                                          new Color(0.30f, 0.12f, 0.12f, 0.92f));
        trash.offsetMin = new Vector2(0f, 4f);
        trash.offsetMax = new Vector2(0f, 40f);
        var trashLabel = UIFactory.CreateText(trash, "Label", "🗑  drag a block here to delete",
                                              18f, new Color(0.92f, 0.6f, 0.55f),
                                              TextAlignmentOptions.Center);
        trashLabel.rectTransform.offsetMin = Vector2.zero;
        trashLabel.rectTransform.offsetMax = Vector2.zero;

        // Templates live inactive under the panel root.
        BlockRowView  rowTemplate  = BuildBlockRowTemplate(parent);
        BlockDropSlot slotTemplate = BuildSlotTemplate(parent);

        var canvasCtrl = parent.gameObject.AddComponent<BlockCanvasController>();
        SceneBuilderUtil.Wire(canvasCtrl, "content",      content);
        SceneBuilderUtil.Wire(canvasCtrl, "scrollRect",   scroll);
        SceneBuilderUtil.Wire(canvasCtrl, "rowTemplate",  rowTemplate);
        SceneBuilderUtil.Wire(canvasCtrl, "slotTemplate", slotTemplate);
        SceneBuilderUtil.Wire(canvasCtrl, "trashZone",    trash);
        SceneBuilderUtil.Wire(canvasCtrl, "dragLayer",    dragLayer);

        return canvasCtrl;
    }

    static BlockRowView BuildBlockRowTemplate(RectTransform parent)
    {
        var row = UIFactory.CreateRect(parent, "BlockRowTemplate",
                                       new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        row.sizeDelta = new Vector2(620f, 46f);
        UIFactory.SetLayoutSize(row, -1f, 46f);

        var bg = row.gameObject.AddComponent<Image>();
        bg.sprite = UIFactory.BuiltinSprite("UISprite.psd");
        bg.type   = Image.Type.Sliced;
        bg.color  = new Color(0.22f, 0.30f, 0.42f, 1f);

        var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 4f;
        layout.padding = new RectOffset(6, 6, 4, 4);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        // Indent spacer
        var spacer = UIFactory.CreateRect(row, "Indent", Vector2.zero, Vector2.zero);
        var spacerLayout = spacer.gameObject.AddComponent<LayoutElement>();
        spacerLayout.preferredWidth = 0f;

        // Label (the keyword / action name)
        var label = UIFactory.CreateText(row, "Label", "moveForward()", 20f,
                                         UIFactory.TextBright, TextAlignmentOptions.MidlineLeft);
        var labelLayout = label.gameObject.AddComponent<LayoutElement>();
        labelLayout.preferredWidth  = 150f;
        labelLayout.flexibleWidth   = 1f;
        labelLayout.preferredHeight = 36f;

        // Condition chip (containers only) — click to cycle the query
        Button cond = MakeRowButton(row, "CondButton", "frontIsClear()", 168f);
        var condLabel = cond.GetComponentInChildren<TMP_Text>();
        cond.image.color = new Color(0.20f, 0.24f, 0.34f, 1f);

        Button not = MakeRowButton(row, "NotButton", "not", 50f);
        Button del = MakeRowButton(row, "DeleteButton", "✕", 36f);

        var view = row.gameObject.AddComponent<BlockRowView>();
        SceneBuilderUtil.Wire(view, "background",      bg);
        SceneBuilderUtil.Wire(view, "indentSpacer",    spacerLayout);
        SceneBuilderUtil.Wire(view, "label",           label);
        SceneBuilderUtil.Wire(view, "conditionButton", cond);
        SceneBuilderUtil.Wire(view, "conditionLabel",  condLabel);
        SceneBuilderUtil.Wire(view, "notButton",       not);
        SceneBuilderUtil.Wire(view, "notFace",         not.image);
        SceneBuilderUtil.Wire(view, "deleteButton",    del);

        row.gameObject.SetActive(false);
        return view;
    }

    static BlockDropSlot BuildSlotTemplate(RectTransform parent)
    {
        var slot = UIFactory.CreateRect(parent, "SlotTemplate",
                                        new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        slot.sizeDelta = new Vector2(620f, 12f);

        var layout = slot.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 12f;

        var bar = UIFactory.CreatePanel(slot, "Bar", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                                        new Color(0.50f, 0.55f, 0.62f, 0.45f));
        bar.offsetMin = new Vector2(6f, -3f);
        bar.offsetMax = new Vector2(-6f, 3f);
        bar.GetComponent<Image>().raycastTarget = false;

        var view = slot.gameObject.AddComponent<BlockDropSlot>();
        SceneBuilderUtil.Wire(view, "bar", bar.GetComponent<Image>());

        slot.gameObject.SetActive(false);
        return view;
    }

    // -------------------------------------------------------------------------
    // Code editor

    internal static CodeEditorController BuildCodeEditor(RectTransform parent)
    {
        // Gutter background (line-number column)
        var gutter = UIFactory.CreatePanel(parent, "Gutter",
                                           new Vector2(0f, 0f), new Vector2(0f, 1f),
                                           new Color(0.05f, 0.06f, 0.08f, 1f));
        gutter.offsetMin = new Vector2(0f, 36f);
        gutter.offsetMax = new Vector2(46f, 0f);

        // Container for gutter icons + fold arrows (drawn over the gutter background).
        var gutterRoot = UIFactory.CreateRect(gutter, "GutterIcons",
                                              Vector2.zero, Vector2.one,
                                              Vector2.zero, Vector2.zero);

        var lineNumbers = UIFactory.CreateText(parent, "LineNumbers", "1", 22f,
                                               UIFactory.TextDim, TextAlignmentOptions.TopRight);
        lineNumbers.rectTransform.anchorMin = new Vector2(0f, 0f);
        lineNumbers.rectTransform.anchorMax = new Vector2(0f, 1f);
        lineNumbers.rectTransform.offsetMin = new Vector2(0f, 44f);
        lineNumbers.rectTransform.offsetMax = new Vector2(40f, -8f);

        TMP_InputField input = UIFactory.CreateMultilineInput(parent, "CodeInput",
                                                              Vector2.zero, Vector2.one, 22f);
        var inputRt = (RectTransform)input.transform;
        inputRt.offsetMin = new Vector2(52f, 36f);
        inputRt.offsetMax = new Vector2(0f, 0f);
        var inputBg = input.GetComponent<Image>();
        if (inputBg != null) inputBg.color = new Color(0.07f, 0.08f, 0.11f, 1f);

        // Syntax-highlight overlay, glyph-aligned over the input's text.
        var highlight = UIFactory.CreateText(input.textViewport, "Highlight", "", 22f,
                                             UIFactory.TextBright, TextAlignmentOptions.TopLeft);
        highlight.rectTransform.anchorMin = Vector2.zero;
        highlight.rectTransform.anchorMax = Vector2.one;
        highlight.rectTransform.offsetMin = Vector2.zero;
        highlight.rectTransform.offsetMax = Vector2.zero;
        highlight.enableWordWrapping = false;
        highlight.raycastTarget = false;
        highlight.richText = true;
        highlight.transform.SetAsFirstSibling();

        // Execution line highlight bar (behind the text, inside the viewport).
        var execBarRt = UIFactory.CreatePanel(input.textViewport, "ExecLineBar",
                                              Vector2.zero, Vector2.one,
                                              new Color(0.18f, 0.36f, 0.58f, 0.45f));
        execBarRt.offsetMin = Vector2.zero;
        execBarRt.offsetMax = Vector2.zero;
        var execBar = execBarRt.GetComponent<Image>();
        execBar.raycastTarget = false;
        execBarRt.gameObject.SetActive(false);
        execBarRt.SetAsFirstSibling();

        // Squiggle underlines for lint errors.
        var squigglesRoot = UIFactory.CreateRect(input.textViewport, "Squiggles",
                                                 Vector2.zero, Vector2.one,
                                                 Vector2.zero, Vector2.zero);

        var lint = UIFactory.CreateText(parent, "LintLabel", "", 17f,
                                        UIFactory.TextDim, TextAlignmentOptions.MidlineLeft);
        UIFactory.Place(lint, new Vector2(0f, 0f), new Vector2(8f, 4f), new Vector2(800f, 28f));
        lint.richText = true;

        // Autocomplete dropdown (high sort order so it floats above everything).
        CodeAutocompleteController autocomplete = BuildAutocompleteDropdown(parent);

        // Monospace font (The Farmer Was Replaced look). Optional — falls back to
        // the default font when the asset hasn't been generated.
        var mono = Resources.Load<TMP_FontAsset>("Fonts/CodeMono");
        if (mono != null)
        {
            if (input.textComponent != null) input.textComponent.font = mono;
            highlight.font   = mono;
            lineNumbers.font = mono;
            if (input.placeholder is TMP_Text placeholder) placeholder.font = mono;
        }

        var editor = parent.gameObject.AddComponent<CodeEditorController>();
        SceneBuilderUtil.Wire(editor, "input",       input);
        SceneBuilderUtil.Wire(editor, "lineNumbers", lineNumbers);
        SceneBuilderUtil.Wire(editor, "highlight",   highlight);
        SceneBuilderUtil.Wire(editor, "lintLabel",   lint);
        SceneBuilderUtil.Wire(editor, "execLineBar", execBar);
        SceneBuilderUtil.Wire(editor, "gutterRoot",  gutterRoot);
        SceneBuilderUtil.Wire(editor, "squigglesRoot", squigglesRoot);
        SceneBuilderUtil.Wire(editor, "autocomplete", autocomplete);

        autocomplete.input = input;
        autocomplete.highlight = highlight;

        return editor;
    }

    internal static CodeAutocompleteController BuildAutocompleteDropdown(RectTransform parent)
    {
        var window = UIFactory.CreatePanel(parent, "AutocompleteDropdown",
                                           Vector2.zero, Vector2.zero,
                                           new Color(0.08f, 0.09f, 0.12f, 0.98f));
        UIFactory.Place(window, Vector2.zero, Vector2.zero, new Vector2(240f, 220f));
        window.gameObject.SetActive(false);

        ScrollRect scroll = UIFactory.CreateScrollView(window, "Scroll",
                                                       Vector2.zero, Vector2.one,
                                                       out RectTransform content);
        ((RectTransform)scroll.transform).offsetMin = new Vector2(2f, 2f);
        ((RectTransform)scroll.transform).offsetMax = new Vector2(-2f, -2f);

        // Row template.
        var row = UIFactory.CreateButton(content, "RowTemplate", "moveForward  action", new Vector2(220f, 30f), 18f);
        row.image.color = new Color(0.08f, 0.09f, 0.12f, 0.95f);
        var rowTxt = row.GetComponentInChildren<TMP_Text>();
        rowTxt.alignment = TextAlignmentOptions.MidlineLeft;
        rowTxt.rectTransform.offsetMin = new Vector2(6f, 0f);
        row.gameObject.SetActive(false);

        var ctrl = window.gameObject.AddComponent<CodeAutocompleteController>();
        SceneBuilderUtil.Wire(ctrl, "root", window.gameObject);
        SceneBuilderUtil.Wire(ctrl, "content", content);
        SceneBuilderUtil.Wire(ctrl, "rowTemplate", row.gameObject);
        return ctrl;
    }

    // -------------------------------------------------------------------------
    // Console

    internal static ConsoleController BuildConsole(RectTransform workspace)
    {
        var frame = UIFactory.CreateRect(workspace, "Console",
                                         new Vector2(0f, 0f), new Vector2(1f, 0f),
                                         new Vector2(14f, 10f), new Vector2(-14f, 214f));

        ScrollRect scroll = UIFactory.CreateScrollView(frame, "ConsoleScroll",
                                                       Vector2.zero, Vector2.one,
                                                       out RectTransform content);

        var template = UIFactory.CreateText(frame, "LineTemplate", "console line", 17f,
                                            UIFactory.TextBright, TextAlignmentOptions.MidlineLeft);
        var templateLayout = template.gameObject.AddComponent<LayoutElement>();
        templateLayout.preferredHeight = 24f;
        template.gameObject.SetActive(false);

        var console = frame.gameObject.AddComponent<ConsoleController>();
        SceneBuilderUtil.Wire(console, "scrollRect",   scroll);
        SceneBuilderUtil.Wire(console, "content",      content);
        SceneBuilderUtil.Wire(console, "lineTemplate", template);

        return console;
    }

    // -------------------------------------------------------------------------
    // Results overlay

    internal static AutomationResultsPanel BuildResults(Canvas canvas)
    {
        var overlay = UIFactory.CreatePanel(canvas.transform, "ResultsOverlay",
                                            Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.78f));

        var window = UIFactory.CreatePanel(overlay, "Window",
                                           new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                           UIFactory.PanelDark);
        UIFactory.Place(window, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180f, 760f));

        var title = UIFactory.CreateText(window, "Title", "PUZZLE SOLVED", 38f, UIFactory.Accent);
        UIFactory.Place(title, new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(1100f, 52f));

        var playerHeader = UIFactory.CreateText(window, "PlayerHeader", "YOUR SOLUTION", 22f, UIFactory.TextDim);
        UIFactory.Place(playerHeader, new Vector2(0.5f, 1f), new Vector2(-285f, -82f), new Vector2(520f, 30f));

        var optimalHeader = UIFactory.CreateText(window, "OptimalHeader", "OPTIMAL SOLUTION", 22f, UIFactory.TextDim);
        UIFactory.Place(optimalHeader, new Vector2(0.5f, 1f), new Vector2(285f, -82f), new Vector2(520f, 30f));

        var playerPanel = UIFactory.CreatePanel(window, "PlayerPanel",
                                                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                                UIFactory.PanelDarker);
        UIFactory.Place(playerPanel, new Vector2(0.5f, 1f), new Vector2(-285f, -116f), new Vector2(530f, 430f));
        var playerText = UIFactory.CreateText(playerPanel, "Text", "", 19f,
                                              UIFactory.TextBright, TextAlignmentOptions.TopLeft);
        playerText.rectTransform.offsetMin = new Vector2(12f, 8f);
        playerText.rectTransform.offsetMax = new Vector2(-12f, -8f);

        var optimalPanel = UIFactory.CreatePanel(window, "OptimalPanel",
                                                 new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                                 UIFactory.PanelDarker);
        UIFactory.Place(optimalPanel, new Vector2(0.5f, 1f), new Vector2(285f, -116f), new Vector2(530f, 430f));
        var optimalText = UIFactory.CreateText(optimalPanel, "Text", "", 19f,
                                               UIFactory.TextBright, TextAlignmentOptions.TopLeft);
        optimalText.rectTransform.offsetMin = new Vector2(12f, 8f);
        optimalText.rectTransform.offsetMax = new Vector2(-12f, -8f);

        var stats = UIFactory.CreateText(window, "Stats", "", 24f, UIFactory.Accent);
        UIFactory.Place(stats, new Vector2(0.5f, 0f), new Vector2(0f, 140f), new Vector2(1100f, 40f));

        var mentor = UIFactory.CreateText(window, "MentorLabel", "...", 20f, UIFactory.TextDim);
        UIFactory.Place(mentor, new Vector2(0.5f, 0f), new Vector2(0f, 93f), new Vector2(1100f, 48f));
        mentor.enableWordWrapping = true;

        Button cont = UIFactory.CreateButton(window, "ContinueButton", "Continue", new Vector2(240f, 58f));
        UIFactory.Place(cont, new Vector2(0.5f, 0f), new Vector2(130f, 30f), new Vector2(240f, 58f));
        cont.image.color = new Color(0.85f, 0.55f, 0.12f);

        Button replay = UIFactory.CreateButton(window, "ReplayButton", "Replay Puzzle", new Vector2(240f, 58f));
        UIFactory.Place(replay, new Vector2(0.5f, 0f), new Vector2(-130f, 30f), new Vector2(240f, 58f));

        var panel = overlay.gameObject.AddComponent<AutomationResultsPanel>();
        SceneBuilderUtil.Wire(panel, "root",                 overlay.gameObject);
        SceneBuilderUtil.Wire(panel, "titleLabel",           title);
        SceneBuilderUtil.Wire(panel, "playerSolutionLabel",  playerText);
        SceneBuilderUtil.Wire(panel, "optimalSolutionLabel", optimalText);
        SceneBuilderUtil.Wire(panel, "statsLabel",           stats);
        SceneBuilderUtil.Wire(panel, "mentorLabel",          mentor);
        SceneBuilderUtil.Wire(panel, "continueButton",       cont);
        SceneBuilderUtil.Wire(panel, "replayButton",         replay);

        return panel;
    }

    // -------------------------------------------------------------------------

    internal static Button MakeBarButton(RectTransform bar, string name, string label, float width)
    {
        Button button = UIFactory.CreateButton(bar, name, label, new Vector2(width, 44f), 22f);
        UIFactory.SetLayoutSize(button, width, 44f);
        return button;
    }

    static Button MakeRowButton(RectTransform row, string name, string label, float width)
    {
        Button button = UIFactory.CreateButton(row, name, label, new Vector2(width, 36f), 16f);
        var layout = button.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth  = width;
        layout.preferredHeight = 36f;
        return button;
    }
}
