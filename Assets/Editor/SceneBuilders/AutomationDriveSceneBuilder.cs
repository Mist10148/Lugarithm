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
                                                          new Color(0.13f, 0.22f, 0.14f), 5f);
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

        // Workspace backdrop first (compact left rail) so the road remains visible
        // draws above it — uncovered screen areas outside the world camera's
        // viewport would otherwise show garbage.
        var workspace = UIFactory.CreatePanel(canvas.transform, "Workspace",
                                              new Vector2(0f, 0f), new Vector2(0.365f, 1f),
                                              UIFactory.PanelDarker);
        workspace.offsetMin = new Vector2(16f, 16f);
        workspace.offsetMax = new Vector2(-4f, -204f);

        // Goal banner (top-left, over the world view). Width capped so long
        // goal text wraps inside the banner instead of spilling into the road.
        var goalBanner = UIFactory.CreatePanel(canvas.transform, "GoalBanner",
                                               new Vector2(0f, 1f), new Vector2(0f, 1f),
                                               new Color(0.06f, 0.07f, 0.10f, 0.85f));
        UIFactory.Place(goalBanner, new Vector2(0f, 1f), new Vector2(16f, -12f), new Vector2(640f, 82f));
        var goalText = UIFactory.CreateText(goalBanner, "GoalText", "", 20f,
                                            UIFactory.TextBright, TextAlignmentOptions.TopLeft);
        goalText.rectTransform.offsetMin = new Vector2(12f, 6f);
        goalText.rectTransform.offsetMax = new Vector2(-12f, -6f);
        goalText.enableWordWrapping = true;

        // Control bar in its own band below the goal banner with a clear gutter,
        // not flush against it. Stays in the left UI rail; never crosses the road.
        var controlBar = UIFactory.CreatePanel(canvas.transform, "ControlBar",
                                               new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                               UIFactory.PanelDark);
        UIFactory.Place(controlBar, new Vector2(0f, 1f), new Vector2(16f, -108f), new Vector2(640f, 48f));
        UIFactory.AddHorizontalLayout(controlBar, 5f, new RectOffset(7, 7, 5, 5), TextAnchor.MiddleCenter);

        Button run    = MakeBarButton(controlBar, "RunButton",   "RUN",    78f);
        run.image.color = new Color(0.20f, 0.55f, 0.25f);
        Button pause  = MakeBarButton(controlBar, "PauseButton", "Pause",  66f);
        Button reset  = MakeBarButton(controlBar, "ResetButton", "Reset",  66f);
        Button step   = MakeBarButton(controlBar, "StepButton",  "Step",    58f);

        Slider speedSlider = UIFactory.CreateSlider(controlBar, "SpeedSlider", new Vector2(128f, 34f));
        speedSlider.minValue = 0.2f;
        speedSlider.maxValue = 8f;
        speedSlider.value = 1f;
        var sliderLe = speedSlider.gameObject.GetComponent<LayoutElement>();
        if (sliderLe == null) sliderLe = speedSlider.gameObject.AddComponent<LayoutElement>();
        sliderLe.preferredWidth = 128f;
        sliderLe.preferredHeight = 34f;

        TMP_Text speedLabel = UIFactory.CreateText(controlBar, "SpeedLabel", "×1.0", 20f, UIFactory.TextBright);
        var labelLe = speedLabel.gameObject.GetComponent<LayoutElement>();
        if (labelLe == null) labelLe = speedLabel.gameObject.AddComponent<LayoutElement>();
        labelLe.preferredWidth = 56f;
        labelLe.preferredHeight = 36f;

        Button autopilot = MakeBarButton(controlBar, "Autopilot", "Auto", 72f);
        autopilot.image.color = new Color(0.30f, 0.45f, 0.75f);

        // Exit (top-right corner)
        Button exit = UIFactory.CreateButton(canvas.transform, "ExitButton", "Exit", new Vector2(110f, 42f));
        UIFactory.Place(exit, new Vector2(1f, 1f), new Vector2(-10f, -8f), new Vector2(110f, 42f));
        var link = exit.gameObject.AddComponent<SceneLink>();
        SceneBuilderUtil.Wire(link, "button",    exit);
        SceneBuilderUtil.Wire(link, "sceneName", "LevelSelect");

        // In-editor Block/Code switch, grouped with the other top-right actions
        // (below Exit) instead of stacked under the goal banner on the left.
        Button editorModeToggle = UIFactory.CreateButton(canvas.transform, "EditorModeToggle",
                                                         "Editor: Blocks", new Vector2(190f, 40f), 18f);
        UIFactory.Place(editorModeToggle, new Vector2(1f, 1f), new Vector2(-10f, -58f), new Vector2(190f, 40f));
        editorModeToggle.image.color = new Color(0.30f, 0.45f, 0.75f);

        // Front-seat story-passenger card (top-center): who you're coding for + talking to.
        var frontSeat = UIFactory.CreatePanel(canvas.transform, "FrontSeatCard",
                                              new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                              new Color(0.10f, 0.12f, 0.16f, 0.92f));
        UIFactory.Place(frontSeat, new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(320f, 52f));
        TMP_Text frontSeatLabel = UIFactory.CreateText(frontSeat, "Label", "", 22f,
                                                       UIFactory.TextBright, TextAlignmentOptions.MidlineLeft);
        frontSeatLabel.rectTransform.offsetMin = new Vector2(16f, 0f);
        frontSeatLabel.rectTransform.offsetMax = new Vector2(-12f, 0f);
        frontSeat.gameObject.SetActive(false);

        // --- Floating editor windows (Block / Code) ---------------------------------

        var editorArea = UIFactory.CreateRect(canvas.transform, "EditorArea",
                                              Vector2.zero, Vector2.one,
                                              Vector2.zero, Vector2.zero);

        RectTransform blockPanel = BuildBlockWindow(
            editorArea, (RectTransform)canvas.transform, out BlockPaletteController paletteCtrl, out BlockCanvasController blockCanvas);
        blockPanel.anchorMin = new Vector2(0f, 0f);
        blockPanel.anchorMax = new Vector2(0.365f, 1f);
        blockPanel.offsetMin = new Vector2(24f, 24f);
        blockPanel.offsetMax = new Vector2(-12f, -212f);

        RectTransform codePanel = BuildCodeWindow(
            editorArea, out CodeEditorController codeEditor, out VibeCodingController vibeCtrl);
        codePanel.anchorMin = new Vector2(0f, 0f);
        codePanel.anchorMax = new Vector2(0.365f, 1f);
        codePanel.offsetMin = new Vector2(24f, 24f);
        codePanel.offsetMax = new Vector2(-12f, -212f);

        // Co-Pilot hint button + label (bottom-right of workspace)
        Button hintBtn = UIFactory.CreateButton(workspace, "HintButton",
                                                  "💡 Ask for a hint", new Vector2(200f, 40f), 20f);
        UIFactory.Place(hintBtn, new Vector2(1f, 0f), new Vector2(-120f, 60f), new Vector2(200f, 40f));
        hintBtn.image.color = UIFactory.Accent;
        hintBtn.gameObject.SetActive(false);

        TMP_Text hintLbl = UIFactory.CreateText(workspace, "HintLabel", "", 20f, UIFactory.TextDim);
        UIFactory.Place(hintLbl, new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(600f, 60f));
        hintLbl.enableWordWrapping = true;

        // (The old bottom "terminal" band — a state-monitor line + console log —
        // was removed; the editor now fills that reclaimed space.)

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
        SceneBuilderUtil.Wire(controller, "ghost",          codeEditor.GetComponent<GhostTextController>());
        SceneBuilderUtil.Wire(controller, "runButton",      run);
        SceneBuilderUtil.Wire(controller, "pauseButton",    pause);
        SceneBuilderUtil.Wire(controller, "resetButton",    reset);
        SceneBuilderUtil.Wire(controller, "speedSlider",    speedSlider);
        SceneBuilderUtil.Wire(controller, "speedLabel",     speedLabel);
        SceneBuilderUtil.Wire(controller, "stepButton",     step);
        SceneBuilderUtil.Wire(controller, "editorModeToggle", editorModeToggle);
        SceneBuilderUtil.Wire(controller, "autopilotButton", autopilot);
        SceneBuilderUtil.Wire(controller, "selfDrive",      selfDrive);
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
        SceneBuilderUtil.Wire(controller, "frontSeatCard",  frontSeat.gameObject);
        SceneBuilderUtil.Wire(controller, "frontSeatLabel", frontSeatLabel);

        SceneBuilderUtil.SaveScene(scene, "AutomationDrive");
    }

    // -------------------------------------------------------------------------
    // Dedicated editor windows (Block / Code) — shared by CodeDrive + Maze so a
    // run shows exactly one editor in its own titled, floating panel, chosen by
    // the Block/Code setting. The palette lives *inside* the block window, so
    // Code mode shows no block UI at all.

    /// <summary>
    /// A titled floating window with full chrome: title-bar drag, click-to-focus,
    /// minimize/restore, close, and a bottom-right resize grip. <paramref name="content"/>
    /// is the body below the title bar; <paramref name="windowCtrl"/> drives the
    /// window and can be registered with a <see cref="WindowDock"/>.
    /// </summary>
    internal static RectTransform BuildWindow(RectTransform parent, string name, string title,
                                              out RectTransform content, out RectTransform titleBar,
                                              out EditorWindowController windowCtrl, bool closeable = true)
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
        titleText.rectTransform.offsetMax = new Vector2(-150f, 0f);   // room for the window buttons

        // Window buttons (top-right of the title bar): minimize, and optionally close.
        float minX = closeable ? -44f : -12f;
        Button minBtn = UIFactory.CreateButton(titleBar, "MinimizeButton", "_", new Vector2(28f, 24f), 18f);
        UIFactory.Place(minBtn, new Vector2(1f, 0.5f), new Vector2(minX, 0f), new Vector2(28f, 24f));
        Button closeBtn = null;
        if (closeable)
        {
            closeBtn = UIFactory.CreateButton(titleBar, "CloseButton", "x", new Vector2(28f, 24f), 18f);
            UIFactory.Place(closeBtn, new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(28f, 24f));
            closeBtn.image.color = new Color(0.55f, 0.20f, 0.20f, 1f);
        }

        content = UIFactory.CreateRect(window, "Content", Vector2.zero, Vector2.one,
                                       Vector2.zero, new Vector2(0f, -34f));

        // Bottom-right resize grip.
        var grip = UIFactory.CreatePanel(window, "ResizeGrip", new Vector2(1f, 0f), new Vector2(1f, 0f),
                                         new Color(0.50f, 0.55f, 0.62f, 0.55f));
        UIFactory.Place(grip, new Vector2(1f, 0f), new Vector2(-2f, 2f), new Vector2(18f, 18f));
        var resize = grip.gameObject.AddComponent<ResizeHandle>();
        SceneBuilderUtil.Wire(resize, "target", window);

        // Title-bar drag + window controller (focus / minimize / close).
        var drag = titleBar.gameObject.AddComponent<DragWindowHandle>();
        SceneBuilderUtil.Wire(drag, "windowRoot", window.gameObject);

        windowCtrl = window.gameObject.AddComponent<EditorWindowController>();
        SceneBuilderUtil.Wire(windowCtrl, "window",         window);
        SceneBuilderUtil.Wire(windowCtrl, "content",        content);
        SceneBuilderUtil.Wire(windowCtrl, "minimizeButton", minBtn);
        if (closeBtn != null) SceneBuilderUtil.Wire(windowCtrl, "closeButton", closeBtn);
        SceneBuilderUtil.Wire(windowCtrl, "minimizeLabel",  minBtn.GetComponentInChildren<TMP_Text>());
        SceneBuilderUtil.Wire(windowCtrl, "titleLabel",     titleText);

        return window;
    }

    /// <summary><see cref="BuildWindow"/> without the controller out.</summary>
    internal static RectTransform BuildWindow(RectTransform parent, string name, string title,
                                              out RectTransform content, out RectTransform titleBar)
    {
        return BuildWindow(parent, name, title, out content, out titleBar, out _);
    }

    /// <summary>Backward-compatible <see cref="BuildWindow"/> without title-bar out.</summary>
    internal static RectTransform BuildWindow(RectTransform parent, string name, string title,
                                              out RectTransform content)
    {
        return BuildWindow(parent, name, title, out content, out _, out _);
    }

    /// <summary>Scratch-style block window: palette column + drag-and-drop canvas.</summary>
    internal static RectTransform BuildBlockWindow(RectTransform parent, RectTransform dragLayer,
                                                   out BlockPaletteController palette,
                                                   out BlockCanvasController canvas)
    {
        RectTransform window = BuildWindow(parent, "BlockWindow", "BLOCKS — drag to build",
                                           out RectTransform content);

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

    /// <summary>
    /// "The Farmer Was Replaced"-style code window: gutter + input + lint, with an
    /// in-window AI chat that the title-bar "AI"/"Code" buttons swap to and from.
    /// The window has no close button (the editor is always available); the chat is
    /// driven by the returned <see cref="VibeCodingController"/>.
    /// </summary>
    internal static RectTransform BuildCodeWindow(RectTransform parent, out CodeEditorController editor,
                                                  out VibeCodingController chat)
    {
        RectTransform window = BuildWindow(parent, "CodeWindow", "CODE — type to program",
                                           out RectTransform content, out RectTransform titleBar,
                                           out _, closeable: false);

        // Editor body and chat body share the content area; only one shows at a time.
        var editorBody = UIFactory.CreateRect(content, "EditorBody", Vector2.zero, Vector2.one,
                                              Vector2.zero, Vector2.zero);
        editor = BuildCodeEditor(editorBody);

        var chatBody = UIFactory.CreateRect(content, "ChatBody", Vector2.zero, Vector2.one,
                                            Vector2.zero, Vector2.zero);

        // Mode bar (Ask / Plan / Agent) pinned to the top of the chat body.
        var modeBar = UIFactory.CreateRect(chatBody, "ModeBar", new Vector2(0f, 1f), new Vector2(1f, 1f),
                                           new Vector2(6f, -34f), new Vector2(-6f, -4f));
        UIFactory.AddHorizontalLayout(modeBar, 6f, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleLeft);
        Button autoBtn     = UIFactory.CreateButton(modeBar, "AutoMode",     "Auto",     new Vector2(70f, 26f), 15f);
        Button askBtn      = UIFactory.CreateButton(modeBar, "AskMode",      "Ask",      new Vector2(64f, 26f), 15f);
        Button planBtn     = UIFactory.CreateButton(modeBar, "PlanMode",     "Plan",     new Vector2(64f, 26f), 15f);
        Button agentBtn    = UIFactory.CreateButton(modeBar, "AgentMode",    "Agent",    new Vector2(74f, 26f), 15f);
        Button refactorBtn = UIFactory.CreateButton(modeBar, "RefactorMode", "Refactor", new Vector2(96f, 26f), 15f);
        foreach (Button b in new[] { autoBtn, askBtn, planBtn, agentBtn, refactorBtn })
        {
            var le = b.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth  = ((RectTransform)b.transform).sizeDelta.x;
            le.preferredHeight = 26f;
        }

        // Chat history (scroll) fills the middle; the input row is pinned to the bottom.
        ScrollRect scroll = UIFactory.CreateScrollView(chatBody, "ChatHistory",
                                                       Vector2.zero, Vector2.one,
                                                       out RectTransform chatContent);
        var scrollRt = (RectTransform)scroll.transform;
        scrollRt.offsetMin = new Vector2(6f, 50f);
        scrollRt.offsetMax = new Vector2(-6f, -40f);
        scroll.vertical = true;

        TMP_Text historyLabel = UIFactory.CreateText(chatContent, "History",
            "Ask me about your code, ask me to plan an approach, or switch to Agent and tell me what the jeepney should do.",
            18f, UIFactory.TextDim, TextAlignmentOptions.TopLeft);
        historyLabel.enableWordWrapping = true;

        // Inactive bubble template for the Messenger-style transcript.
        TMP_Text vibeBubbleTemplate = UIFactory.CreateText(chatContent, "BubbleTemplate", "", 18f,
                                                           UIFactory.TextBright, TextAlignmentOptions.TopLeft);
        vibeBubbleTemplate.textWrappingMode = TextWrappingModes.Normal;
        var vibeBubbleLe = vibeBubbleTemplate.gameObject.AddComponent<LayoutElement>();
        vibeBubbleLe.preferredHeight = 30f;
        vibeBubbleTemplate.gameObject.SetActive(false);

        TMP_InputField inputField = UIFactory.CreateMultilineInput(chatBody, "ChatInput",
                                                                   new Vector2(0f, 0f), new Vector2(1f, 0f), 18f);
        inputField.lineType = TMP_InputField.LineType.SingleLine;
        var inRt = (RectTransform)inputField.transform;
        inRt.offsetMin = new Vector2(6f, 6f);
        inRt.offsetMax = new Vector2(-62f, 44f);
        if (inputField.placeholder is TMP_Text ph) ph.text = "Ask a question, or say what to do…";

        Button sendBtn = UIFactory.CreateButton(chatBody, "SendBtn", "▶", new Vector2(50f, 38f), 20f);
        var sendRt = (RectTransform)sendBtn.transform;
        sendRt.anchorMin = new Vector2(1f, 0f);
        sendRt.anchorMax = new Vector2(1f, 0f);
        sendRt.pivot     = new Vector2(1f, 0f);
        sendRt.anchoredPosition = new Vector2(-6f, 6f);
        sendRt.sizeDelta = new Vector2(50f, 38f);
        sendBtn.image.color = UIFactory.Accent;

        chatBody.gameObject.SetActive(false);

        // Title-bar swap buttons (left of minimize): AI shows the chat, Code shows
        // the editor.
        Button aiBtn = null, codeBtn = null;
        if (titleBar != null)
        {
            aiBtn = UIFactory.CreateButton(titleBar, "ChatButton", "AI", new Vector2(48f, 24f), 16f);
            UIFactory.Place(aiBtn, new Vector2(1f, 0.5f), new Vector2(-118f, 0f), new Vector2(48f, 24f));
            aiBtn.image.color = new Color(0.30f, 0.45f, 0.75f, 1f);

            codeBtn = UIFactory.CreateButton(titleBar, "EditorButton", "Code", new Vector2(56f, 24f), 16f);
            UIFactory.Place(codeBtn, new Vector2(1f, 0.5f), new Vector2(-60f, 0f), new Vector2(56f, 24f));
        }

        chat = window.gameObject.AddComponent<VibeCodingController>();
        SceneBuilderUtil.Wire(chat, "chatInput",      inputField);
        SceneBuilderUtil.Wire(chat, "historyLabel",   historyLabel);
        SceneBuilderUtil.Wire(chat, "chatContent",    chatContent);
        SceneBuilderUtil.Wire(chat, "bubbleTemplate", vibeBubbleTemplate);
        SceneBuilderUtil.Wire(chat, "sendButton",     sendBtn);
        SceneBuilderUtil.Wire(chat, "codeEditor",     editor);
        SceneBuilderUtil.Wire(chat, "editorBody",     editorBody.gameObject);
        SceneBuilderUtil.Wire(chat, "chatBody",       chatBody.gameObject);
        SceneBuilderUtil.Wire(chat, "autoButton",     autoBtn);
        SceneBuilderUtil.Wire(chat, "askButton",      askBtn);
        SceneBuilderUtil.Wire(chat, "planButton",     planBtn);
        SceneBuilderUtil.Wire(chat, "agentButton",    agentBtn);
        SceneBuilderUtil.Wire(chat, "refactorButton", refactorBtn);
        if (aiBtn   != null) SceneBuilderUtil.Wire(chat, "aiButton",   aiBtn);
        if (codeBtn != null) SceneBuilderUtil.Wire(chat, "codeButton", codeBtn);

        return window;
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
        var trashLabel = UIFactory.CreateText(trash, "Label", "drag a block here to delete",
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

        // Inline ghost-text overlay (Copilot-style next-line suggestion). Faint, non-interactive,
        // glyph-aligned over the input like the highlight layer; the controller lives on the same
        // GameObject as the editor so hosts can fetch it with GetComponent<GhostTextController>().
        var ghostText = UIFactory.CreateText(input.textViewport, "GhostText", "", 22f,
                                             UIFactory.TextDim, TextAlignmentOptions.TopLeft);
        ghostText.rectTransform.anchorMin = Vector2.zero;
        ghostText.rectTransform.anchorMax = Vector2.one;
        ghostText.rectTransform.offsetMin = Vector2.zero;
        ghostText.rectTransform.offsetMax = Vector2.zero;
        ghostText.enableWordWrapping = false;
        ghostText.raycastTarget = false;
        ghostText.richText = true;
        if (mono != null) ghostText.font = mono;
        ghostText.gameObject.SetActive(false);

        var ghost = parent.gameObject.AddComponent<GhostTextController>();
        SceneBuilderUtil.Wire(ghost, "editor",     editor);
        SceneBuilderUtil.Wire(ghost, "ghostLabel", ghostText);

        return editor;
    }

    internal static CodeAutocompleteController BuildAutocompleteDropdown(RectTransform parent)
    {
        // Float on the top-level canvas (not inside the editor window) so the
        // dropdown is never clipped and PositionNearCaret's canvas-space math is
        // correct. Anchored to the canvas centre with a top-left pivot so it drops
        // down-right from the caret.
        Canvas canvas = parent.GetComponentInParent<Canvas>();
        RectTransform host = canvas != null ? (RectTransform)canvas.transform : parent;

        var window = UIFactory.CreatePanel(host, "AutocompleteDropdown",
                                           new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                           new Color(0.08f, 0.09f, 0.12f, 0.98f));
        window.pivot = new Vector2(0f, 1f);
        window.sizeDelta = new Vector2(240f, 220f);
        window.SetAsLastSibling();
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
        // Top edge lowered to leave a clear gap before the monitor strip above.
        var frame = UIFactory.CreateRect(workspace, "Console",
                                         new Vector2(0f, 0f), new Vector2(1f, 0f),
                                         new Vector2(14f, 10f), new Vector2(-14f, 184f));

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
        UIFactory.Place(window, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1240f, 850f));

        var category = UIFactory.CreateText(window, "Category", "MAIN GAMEPLAY · Automation", 18f,
                                            UIFactory.TextDim, TextAlignmentOptions.Center);
        UIFactory.Place(category, new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(1100f, 22f));

        var title = UIFactory.CreateText(window, "Title", "PUZZLE SOLVED", 36f, UIFactory.Accent);
        UIFactory.Place(title, new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(1100f, 48f));

        var playerHeader = UIFactory.CreateText(window, "PlayerHeader", "YOUR SOLUTION", 22f, UIFactory.TextDim);
        UIFactory.Place(playerHeader, new Vector2(0.5f, 1f), new Vector2(-285f, -94f), new Vector2(520f, 30f));

        var optimalHeader = UIFactory.CreateText(window, "OptimalHeader", "AI'S BETTER VERSION", 22f,
                                                 new Color(0.55f, 0.78f, 1f), TextAlignmentOptions.Center);
        UIFactory.Place(optimalHeader, new Vector2(0.5f, 1f), new Vector2(285f, -94f), new Vector2(520f, 30f));

        var playerPanel = UIFactory.CreatePanel(window, "PlayerPanel",
                                                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                                UIFactory.PanelDarker);
        UIFactory.Place(playerPanel, new Vector2(0.5f, 1f), new Vector2(-295f, -116f), new Vector2(560f, 440f));
        var playerText = UIFactory.CreateText(playerPanel, "Text", "", 19f,
                                              UIFactory.TextBright, TextAlignmentOptions.TopLeft);
        playerText.rectTransform.offsetMin = new Vector2(12f, 8f);
        playerText.rectTransform.offsetMax = new Vector2(-12f, -8f);

        var optimalPanel = UIFactory.CreatePanel(window, "OptimalPanel",
                                                 new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                                 UIFactory.PanelDarker);
        UIFactory.Place(optimalPanel, new Vector2(0.5f, 1f), new Vector2(295f, -116f), new Vector2(560f, 440f));
        var optimalText = UIFactory.CreateText(optimalPanel, "Text", "", 19f,
                                               UIFactory.TextBright, TextAlignmentOptions.TopLeft);
        optimalText.rectTransform.offsetMin = new Vector2(12f, 8f);
        optimalText.rectTransform.offsetMax = new Vector2(-12f, -8f);

        var stats = UIFactory.CreateText(window, "Stats", "", 24f, UIFactory.Accent);
        UIFactory.Place(stats, new Vector2(0.5f, 0f), new Vector2(0f, 174f), new Vector2(1160f, 34f));

        var efficiency = UIFactory.CreateText(window, "EfficiencyLabel", "EFFICIENCY —", 21f, UIFactory.Accent);
        UIFactory.Place(efficiency, new Vector2(0.5f, 0f), new Vector2(0f, 142f), new Vector2(1160f, 30f));

        RectTransform annotations = UIFactory.CreateRect(window, "Annotations",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        UIFactory.Place(annotations, new Vector2(0.5f, 0f), new Vector2(0f, 108f), new Vector2(1160f, 30f));
        var annotationLayout = annotations.gameObject.AddComponent<HorizontalLayoutGroup>();
        annotationLayout.spacing = 6f;
        annotationLayout.childForceExpandHeight = true;
        annotationLayout.childForceExpandWidth = false;
        annotationLayout.childAlignment = TextAnchor.MiddleCenter;
        Button annotationTemplate = UIFactory.CreateButton(annotations, "AnnotationTemplate", "Line note", new Vector2(190f, 28f), 14f);
        UIFactory.SetLayoutSize(annotationTemplate, 190f, 28f);
        annotationTemplate.gameObject.SetActive(false);

        var mentor = UIFactory.CreateText(window, "MentorLabel", "...", 20f, UIFactory.TextDim);
        UIFactory.Place(mentor, new Vector2(0.5f, 0f), new Vector2(0f, 76f), new Vector2(1160f, 52f));
        mentor.enableWordWrapping = true;

        var tooltip = UIFactory.CreatePanel(window, "AnnotationTooltip",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Color(0.05f, 0.06f, 0.08f, 0.98f));
        UIFactory.Place(tooltip, new Vector2(0.5f, 0.5f), new Vector2(0f, -100f), new Vector2(780f, 120f));
        var tooltipText = UIFactory.CreateText(tooltip, "Text", "", 19f, UIFactory.TextBright, TextAlignmentOptions.TopLeft);
        tooltipText.rectTransform.offsetMin = new Vector2(16f, 12f);
        tooltipText.rectTransform.offsetMax = new Vector2(-16f, -12f);
        tooltip.gameObject.SetActive(false);

        Button cont = UIFactory.CreateButton(window, "ContinueButton", "Continue", new Vector2(240f, 58f));
        UIFactory.Place(cont, new Vector2(0.5f, 0f), new Vector2(130f, 30f), new Vector2(240f, 58f));
        cont.image.color = new Color(0.85f, 0.55f, 0.12f);

        Button replay = UIFactory.CreateButton(window, "ReplayButton", "Replay Puzzle", new Vector2(240f, 58f));
        UIFactory.Place(replay, new Vector2(0.5f, 0f), new Vector2(-130f, 30f), new Vector2(240f, 58f));

        var panel = overlay.gameObject.AddComponent<AutomationResultsPanel>();
        SceneBuilderUtil.Wire(panel, "root",                 overlay.gameObject);
        SceneBuilderUtil.Wire(panel, "categoryLabel",        category);
        SceneBuilderUtil.Wire(panel, "titleLabel",           title);
        SceneBuilderUtil.Wire(panel, "playerSolutionLabel",  playerText);
        SceneBuilderUtil.Wire(panel, "optimalSolutionLabel", optimalText);
        SceneBuilderUtil.Wire(panel, "statsLabel",           stats);
        SceneBuilderUtil.Wire(panel, "mentorLabel",          mentor);
        SceneBuilderUtil.Wire(panel, "efficiencyLabel",      efficiency);
        SceneBuilderUtil.Wire(panel, "annotationContainer",  annotations);
        SceneBuilderUtil.Wire(panel, "annotationTemplate",   annotationTemplate);
        SceneBuilderUtil.Wire(panel, "tooltipRoot",          tooltip.gameObject);
        SceneBuilderUtil.Wire(panel, "tooltipLabel",         tooltipText);
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
