using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds CodeDrive.unity — the routed Automation scene: a full-screen
/// isometric tile world (its grid derived from the level's manual route) that
/// the jeepney drives by executing the player's program. The code/blocks
/// workspace is a toggleable overlay; which editor it shows is chosen solely in
/// Settings (no in-scene tabs). A Commands panel documents the language.
/// Reuses the workspace builders from <see cref="AutomationDriveSceneBuilder"/>.
/// </summary>
public static class CodeDriveSceneBuilder
{
    public static void Build()
    {
        var scene = SceneBuilderUtil.NewScene();

        // --- World (full screen; iso fallback + top-down procedural) ----------------

        Camera worldCam = SceneBuilderUtil.CreateCamera2D("World Camera",
                                                          new Color(0.13f, 0.22f, 0.14f), 5f);
        var cameraFollow = worldCam.gameObject.AddComponent<CameraFollow2D>();
        SceneBuilderUtil.CreateGlobalLight2D();
        SceneBuilderUtil.CreateEventSystem();

        // Iso fallback for authored mazes.
        var gridRoot = new GameObject("GridRoot");
        var worldView = gridRoot.AddComponent<GridWorldView>();

        var jeepneyGo = new GameObject("AgentJeepney");
        var isoBody = jeepneyGo.AddComponent<SpriteRenderer>();
        isoBody.sprite = SceneBuilderUtil.LoadPlaceholder("iso_jeepney");
        isoBody.sortingOrder = 1000;

        var arrowGo = new GameObject("Arrow");
        arrowGo.transform.SetParent(jeepneyGo.transform, false);
        arrowGo.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        arrowGo.transform.localScale = Vector3.one * 0.8f;
        var arrow = arrowGo.AddComponent<SpriteRenderer>();
        arrow.sprite = SceneBuilderUtil.LoadPlaceholder("triangle");
        arrow.color = Color.white;
        arrow.sortingOrder = 1001;

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

        // --- Canvas -----------------------------------------------------------------

        Canvas canvas = UIFactory.CreateCanvas("WorkspaceCanvas");
        var canvasRoot = (RectTransform)canvas.transform;

        // Goal banner (top-left, over the world)
        var goalBanner = UIFactory.CreatePanel(canvas.transform, "GoalBanner",
                                               new Vector2(0f, 1f), new Vector2(0f, 1f),
                                               new Color(0.06f, 0.07f, 0.10f, 0.85f));
        UIFactory.Place(goalBanner, new Vector2(0f, 1f), new Vector2(16f, -12f), new Vector2(640f, 82f));
        var goalText = UIFactory.CreateText(goalBanner, "GoalText", "", 20f,
                                            UIFactory.TextBright, TextAlignmentOptions.TopLeft);
        goalText.rectTransform.offsetMin = new Vector2(12f, 6f);
        goalText.rectTransform.offsetMax = new Vector2(-12f, -6f);
        goalText.enableWordWrapping = true;

        // Compact control bar below the goal. Keeping the coding HUD in one
        // left-hand rail leaves the road and jeepney visible on the right.
        var controlBar = UIFactory.CreatePanel(canvas.transform, "ControlBar",
                                               new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                               UIFactory.PanelDark);
        UIFactory.Place(controlBar, new Vector2(0f, 1f), new Vector2(16f, -108f), new Vector2(640f, 48f));
        UIFactory.AddHorizontalLayout(controlBar, 6f, new RectOffset(8, 8, 5, 5), TextAnchor.MiddleCenter);

        Button run    = AutomationDriveSceneBuilder.MakeBarButton(controlBar, "RunButton",   "RUN",   88f);
        run.image.color = new Color(0.20f, 0.55f, 0.25f);
        Button pause  = AutomationDriveSceneBuilder.MakeBarButton(controlBar, "PauseButton", "Pause",  76f);
        Button reset  = AutomationDriveSceneBuilder.MakeBarButton(controlBar, "ResetButton", "Reset",  76f);
        Button step   = AutomationDriveSceneBuilder.MakeBarButton(controlBar, "StepButton",  "Step",    68f);

        // Speed is a single cycle button (tap to step ×0.5 → ×1 → ×2 → ×4 → …); its
        // own face is the readout, so speedLabel points at the button's child label.
        Button speedButton = AutomationDriveSceneBuilder.MakeBarButton(controlBar, "SpeedButton", "×1.0", 96f);
        TMP_Text speedLabel = speedButton.GetComponentInChildren<TMP_Text>();

        // Top-right buttons: Exit, workspace toggle, Commands.
        Button exit = UIFactory.CreateButton(canvas.transform, "ExitButton", "Exit", new Vector2(110f, 42f));
        UIFactory.Place(exit, new Vector2(1f, 1f), new Vector2(-24f, -84f), new Vector2(130f, 44f));
        var link = exit.gameObject.AddComponent<SceneLink>();
        SceneBuilderUtil.Wire(link, "button",    exit);
        SceneBuilderUtil.Wire(link, "sceneName", "LevelSelect");

        // Editor switch sits with the other global actions instead of over the road.
        // Editor switch grouped with the other top-right actions (below Journal),
        // instead of stacked under the goal banner on the left.
        Button editorModeToggle = UIFactory.CreateButton(canvas.transform, "EditorModeToggle",
                                                         "Editor: Blocks", new Vector2(170f, 42f), 18f);
        UIFactory.Place(editorModeToggle, new Vector2(1f, 1f), new Vector2(-24f, -240f), new Vector2(170f, 42f));
        editorModeToggle.image.color = new Color(0.30f, 0.45f, 0.75f);

        Button workspaceToggle = UIFactory.CreateButton(canvas.transform, "WorkspaceToggle",
                                                        "▤ Workspace", new Vector2(170f, 42f), 20f);
        UIFactory.Place(workspaceToggle, new Vector2(1f, 1f), new Vector2(-24f, -136f), new Vector2(170f, 42f));

        Button journalToggle = UIFactory.CreateButton(canvas.transform, "JournalToggle",
                                                      "Journal", new Vector2(170f, 42f), 20f);
        UIFactory.Place(journalToggle, new Vector2(1f, 1f), new Vector2(-24f, -188f), new Vector2(170f, 42f));
        journalToggle.gameObject.AddComponent<AlmanacToggleButton>();

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

        RectTransform runStatus = AutomationDriveSceneBuilder.BuildRunStatusHud(
            canvas.transform, out TMP_Text walletLabel, out Image autoFuelFill);
        UIFactory.Place(runStatus, new Vector2(1f, 1f), new Vector2(-24f, -22f), new Vector2(260f, 50f));

        RectTransform gaugePanel = AutomationDriveSceneBuilder.BuildAutomationGaugePanel(
            canvas.transform, out Image gaugeFuelFill, out TMP_Text gaugeSpeedLabel,
            out RectTransform gaugeSpeedNeedle);
        UIFactory.Place(gaugePanel, new Vector2(0f, 0f), new Vector2(18f, 12f), new Vector2(470f, 150f));

        Button commands = UIFactory.CreateButton(canvas.transform, "CommandsButton",
                                                 "Commands ?", new Vector2(160f, 42f), 20f);
        UIFactory.Place(commands, new Vector2(1f, 1f), new Vector2(-204f, -136f), new Vector2(160f, 42f));

        // --- Workspace overlay (compact left dock, toggleable) ----------------------

        var workspace = UIFactory.CreateRect(canvas.transform, "Workspace",
                                             Vector2.zero, Vector2.one,
                                             Vector2.zero, Vector2.zero);

        // Editor windows: Block and Code each in their own titled floating panel,
        // stacked in the same area. The active editor is chosen by the Block/Code
        // setting (the controller shows exactly one), so Code mode shows no blocks.
        var editorArea = UIFactory.CreateRect(workspace, "EditorArea",
                                              Vector2.zero, Vector2.one,
                                              Vector2.zero, Vector2.zero);
        // This scene has its own standalone controlBar (above) for Run/Pause/Reset/Step/
        // Speed, so the windows' embedded toolbar (used by AutomationDrive) is skipped here.
        RectTransform blockPanel = AutomationDriveSceneBuilder.BuildBlockWindow(
            editorArea, canvasRoot, out BlockPaletteController paletteCtrl, out BlockCanvasController blockCanvas,
            out _, out _, out _, out _, out _, out _, out _, embedToolbar: false);
        AutomationDriveSceneBuilder.PlaceFloatingEditorWindow(
            blockPanel, new Vector2(520f, -170f), new Vector2(760f, 780f));

        RectTransform codePanel = AutomationDriveSceneBuilder.BuildCodeWindow(
            editorArea, out CodeEditorController codeEditor, out VibeCodingController vibeCtrl,
            out _, out _, out _, out _, out _, out _, out _, embedToolbar: false);
        AutomationDriveSceneBuilder.PlaceFloatingEditorWindow(
            codePanel, new Vector2(520f, -170f), new Vector2(760f, 780f));

        // (The old bottom "terminal" band — a state-monitor line + console log — was
        // removed; the editor now fills that reclaimed space and the in-window AI
        // chat replaces the old separate readouts.)

        // --- Commands / README panel (hidden until opened) --------------------------

        GameObject readmePanel = BuildReadmePanel(canvas, out Button readmeClose);

        // --- Results overlay --------------------------------------------------------

        AutomationResultsPanel results = AutomationDriveSceneBuilder.BuildResults(canvas);

        // Town gates (non-code, required to advance) — the level picks one.
        FlowConnectMinigame flowPuzzle  = MinigameOverlayBuilder.BuildFlowConnect(canvas.transform);
        CrateStackMinigame  cratePuzzle = MinigameOverlayBuilder.BuildCrateStack(canvas.transform);
        // Tutorial repair drills: code-based maze escape + non-code refuel (dialogue-driven).
        MazeRepairMinigame  mazeRepair  = MinigameOverlayBuilder.BuildMazeRepair(canvas.transform);
        RefuelMinigame      refuel      = MinigameOverlayBuilder.BuildRefuel(canvas.transform);
        DialogueController  dialogue    = DialogueOverlayBuilder.BuildDriveDialogue(canvas.transform);
        LegCompletionController legCompletion = LegCompletionOverlayBuilder.Build(canvas.transform);

        // --- Orchestrator -----------------------------------------------------------

        var controllerGo = new GameObject("CodeDriveController");
        var exec = controllerGo.AddComponent<ExecutionController>();
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
        SceneBuilderUtil.Wire(controller, "vibeCtrl",       vibeCtrl);
        SceneBuilderUtil.Wire(controller, "deriveGridFromRoute",   true);
        SceneBuilderUtil.Wire(controller, "workspaceToggleButton", workspaceToggle);
        SceneBuilderUtil.Wire(controller, "workspaceRoot",         workspace.gameObject);
        SceneBuilderUtil.Wire(controller, "readmeButton",          commands);
        SceneBuilderUtil.Wire(controller, "readmePanel",           readmePanel);
        SceneBuilderUtil.Wire(controller, "readmeCloseButton",     readmeClose);
        SceneBuilderUtil.Wire(controller, "runButton",    run);
        SceneBuilderUtil.Wire(controller, "pauseButton",  pause);
        SceneBuilderUtil.Wire(controller, "resetButton",  reset);
        SceneBuilderUtil.Wire(controller, "speedButton",  speedButton);
        SceneBuilderUtil.Wire(controller, "speedLabel",   speedLabel);
        SceneBuilderUtil.Wire(controller, "stepButton",   step);
        SceneBuilderUtil.Wire(controller, "editorModeToggle", editorModeToggle);
        SceneBuilderUtil.Wire(controller, "results",      results);
        SceneBuilderUtil.Wire(controller, "walletLabel",  walletLabel);
        SceneBuilderUtil.Wire(controller, "automationFuelFill", autoFuelFill);
        SceneBuilderUtil.Wire(controller, "gaugeFuelFill",  gaugeFuelFill);
        SceneBuilderUtil.Wire(controller, "gaugeSpeedLabel", gaugeSpeedLabel);
        SceneBuilderUtil.Wire(controller, "gaugeSpeedNeedle", gaugeSpeedNeedle);

        // Drop-off dulog markers (world pin per onboard passenger + off-screen compass arrow).
        var dulogEdgeLayer = UIFactory.CreateRect(canvas.transform, "DulogEdgeLayer",
                                                  Vector2.zero, Vector2.one);
        dulogEdgeLayer.offsetMin = Vector2.zero;
        dulogEdgeLayer.offsetMax = Vector2.zero;
        var dulogGo = new GameObject("DulogMarkers");
        var dulogMarkers = dulogGo.AddComponent<AutomationDulogMarkerController>();
        SceneBuilderUtil.Wire(dulogMarkers, "edgeArrowParent", dulogEdgeLayer);
        SceneBuilderUtil.Wire(controller, "dulogMarkers", dulogMarkers);

        SceneBuilderUtil.Wire(controller, "flowPuzzle",   flowPuzzle);
        SceneBuilderUtil.Wire(controller, "cratePuzzle",  cratePuzzle);
        SceneBuilderUtil.Wire(controller, "mazeRepairMinigame", mazeRepair);
        SceneBuilderUtil.Wire(controller, "refuelMinigame",     refuel);
        SceneBuilderUtil.Wire(controller, "dialogue",     dialogue);
        SceneBuilderUtil.Wire(controller, "legCompletion", legCompletion);
        SceneBuilderUtil.Wire(controller, "frontSeatCard",  frontSeat.gameObject);
        SceneBuilderUtil.Wire(controller, "frontSeatLabel", frontSeatLabel);

        SceneBuilderUtil.SaveScene(scene, "CodeDrive");
    }

    // -------------------------------------------------------------------------

    static GameObject BuildReadmePanel(Canvas canvas, out Button closeButton)
    {
        var overlay = UIFactory.CreatePanel(canvas.transform, "CommandsOverlay",
                                            Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.6f));

        var window = UIFactory.CreatePanel(overlay, "Window",
                                           new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                           UIFactory.PanelDark);
        UIFactory.Place(window, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(960f, 800f));

        var title = UIFactory.CreateText(window, "Title", "COMMANDS", 34f, UIFactory.Accent);
        UIFactory.Place(title, new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(900f, 48f));

        ScrollRect scroll = UIFactory.CreateScrollView(window, "Scroll",
                                                       Vector2.zero, Vector2.one, out RectTransform content);
        var scrollRt = (RectTransform)scroll.transform;
        scrollRt.offsetMin = new Vector2(16f, 74f);
        scrollRt.offsetMax = new Vector2(-16f, -70f);

        var body = UIFactory.CreateText(content, "Body", ReadmeText, 19f,
                                        UIFactory.TextBright, TextAlignmentOptions.TopLeft);
        body.enableWordWrapping = true;
        var le = body.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 1500f;

        closeButton = UIFactory.CreateButton(window, "CloseButton", "Close", new Vector2(200f, 52f));
        UIFactory.Place(closeButton, new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(200f, 52f));
        closeButton.image.color = new Color(0.85f, 0.55f, 0.12f);

        overlay.gameObject.SetActive(false);
        return overlay.gameObject;
    }

    const string ReadmeText =
@"Drive the jeepney by programming it. Build a program (blocks or code — set in
Settings), then press ▶ RUN. One action runs per tick.

<b>ACTIONS</b>  (do something; one tick each)
  moveForward()   move one tile in the way you're facing (bumps on a wall)
  turnLeft()      rotate 90° left      turnRight()  rotate 90° right
  driveToNextStop()  path to the next rider stop or terminal
  driveToTerminal()  path to the current route terminal
  pickUp()        board the passenger on this stop
  collectFare()   collect fare and record tender
  giveChange(amount)  give exact sukli; use giveChange(changeOwed())
  dropOff()       let settled passengers off at their requested stop

<b>CONDITIONS</b>  (ask a yes/no question; used by if / while)
  frontIsClear()   leftIsClear()   rightIsClear()
  atStop()         passengerWaiting()   atRequestedStop()
  routeComplete()  hasPassengerAboard()

<b>REPORTERS</b>  (return values)
  fareOwed()       cashTendered()       changeOwed()
  seatsLeft()      passengerCount()

<b>CONTROL FLOW</b>
  if CONDITION:           run the indented block once if true
  if CONDITION: else:     ... otherwise run the else block
  while CONDITION:        repeat the block while true
  not CONDITION           flip a condition (e.g. while not routeComplete():)

In the Code Editor end if/else/while with a colon ':' and indent the body 4
spaces; '#' starts a comment. In Blocks, drop blocks inside the C-shaped
if/while and click the condition chip to choose the question.

<b>EXAMPLE</b> — follow the road to the destination:
  while not routeComplete():
      driveToNextStop()
      if passengerWaiting():
          pickUp()
          collectFare()
          giveChange(changeOwed())
      if atRequestedStop():
          dropOff()

atDestination() remains for maze/minigame content; procedural routes use routeComplete().";
}
