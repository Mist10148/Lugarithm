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
                                                          new Color(0.07f, 0.09f, 0.12f), 5f);
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
        UIFactory.Place(goalBanner, new Vector2(0f, 1f), new Vector2(10f, -10f), new Vector2(742f, 92f));
        var goalText = UIFactory.CreateText(goalBanner, "GoalText", "", 20f,
                                            UIFactory.TextBright, TextAlignmentOptions.TopLeft);
        goalText.rectTransform.offsetMin = new Vector2(12f, 6f);
        goalText.rectTransform.offsetMax = new Vector2(-12f, -6f);

        // Control bar (top-center)
        var controlBar = UIFactory.CreatePanel(canvas.transform, "ControlBar",
                                               new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                               UIFactory.PanelDark);
        UIFactory.Place(controlBar, new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(700f, 56f));
        UIFactory.AddHorizontalLayout(controlBar, 8f, new RectOffset(10, 10, 6, 6), TextAnchor.MiddleCenter);

        Button run    = AutomationDriveSceneBuilder.MakeBarButton(controlBar, "RunButton",   "▶ RUN",  120f);
        run.image.color = new Color(0.20f, 0.55f, 0.25f);
        Button pause  = AutomationDriveSceneBuilder.MakeBarButton(controlBar, "PauseButton", "❚❚",      70f);
        Button reset  = AutomationDriveSceneBuilder.MakeBarButton(controlBar, "ResetButton", "↺ Reset", 110f);
        Button speed1 = AutomationDriveSceneBuilder.MakeBarButton(controlBar, "Speed1",      "1×",      64f);
        Button speed2 = AutomationDriveSceneBuilder.MakeBarButton(controlBar, "Speed2",      "2×",      64f);
        Button speed5 = AutomationDriveSceneBuilder.MakeBarButton(controlBar, "Speed5",      "5×",      64f);
        Button step   = AutomationDriveSceneBuilder.MakeBarButton(controlBar, "StepButton",  "Step",    80f);

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

        // Top-right buttons: Exit, workspace toggle, Commands.
        Button exit = UIFactory.CreateButton(canvas.transform, "ExitButton", "Exit", new Vector2(110f, 42f));
        UIFactory.Place(exit, new Vector2(1f, 1f), new Vector2(-10f, -8f), new Vector2(110f, 42f));
        var link = exit.gameObject.AddComponent<SceneLink>();
        SceneBuilderUtil.Wire(link, "button",    exit);
        SceneBuilderUtil.Wire(link, "sceneName", "LevelSelect");

        Button workspaceToggle = UIFactory.CreateButton(canvas.transform, "WorkspaceToggle",
                                                        "▤ Workspace", new Vector2(170f, 42f), 20f);
        UIFactory.Place(workspaceToggle, new Vector2(1f, 1f), new Vector2(-10f, -58f), new Vector2(170f, 42f));

        Button journalToggle = UIFactory.CreateButton(canvas.transform, "JournalToggle",
                                                      "Journal", new Vector2(170f, 42f), 20f);
        UIFactory.Place(journalToggle, new Vector2(1f, 1f), new Vector2(-10f, -108f), new Vector2(170f, 42f));
        journalToggle.gameObject.AddComponent<AlmanacToggleButton>();

        Button commands = UIFactory.CreateButton(canvas.transform, "CommandsButton",
                                                 "Commands ?", new Vector2(160f, 42f), 20f);
        UIFactory.Place(commands, new Vector2(1f, 1f), new Vector2(-190f, -58f), new Vector2(160f, 42f));

        // --- Workspace overlay (right side, toggleable) -----------------------------

        var workspace = UIFactory.CreatePanel(canvas.transform, "Workspace",
                                              new Vector2(0.42f, 0f), new Vector2(1f, 1f),
                                              UIFactory.PanelDarker);
        workspace.offsetMin = new Vector2(0f, 0f);
        workspace.offsetMax = new Vector2(0f, -110f);

        // Editor windows: Block and Code each in their own titled floating panel,
        // stacked in the same area. The active editor is chosen by the Block/Code
        // setting (the controller shows exactly one), so Code mode shows no blocks.
        var editorArea = UIFactory.CreateRect(workspace, "EditorArea",
                                              new Vector2(0f, 0f), new Vector2(1f, 1f),
                                              new Vector2(8f, 258f), new Vector2(-8f, -8f));
        RectTransform blockPanel = AutomationDriveSceneBuilder.BuildBlockWindow(
            editorArea, canvasRoot, out BlockPaletteController paletteCtrl, out BlockCanvasController blockCanvas);
        UIFactory.Place(blockPanel, new Vector2(0.62f, 0.5f), Vector2.zero, new Vector2(760f, 640f));

        RectTransform codePanel = AutomationDriveSceneBuilder.BuildCodeWindow(
            editorArea, out CodeEditorController codeEditor, out Button codeChatButton);
        UIFactory.Place(codePanel, new Vector2(0.62f, 0.5f), Vector2.zero, new Vector2(760f, 640f));

        // Monitor + console
        var monitorLine = UIFactory.CreatePanel(workspace, "Monitor",
                                                new Vector2(0f, 0f), new Vector2(1f, 0f),
                                                UIFactory.PanelDark);
        monitorLine.offsetMin = new Vector2(14f, 222f);
        monitorLine.offsetMax = new Vector2(-14f, 252f);
        var monitorText = UIFactory.CreateText(monitorLine, "Text", "", 17f,
                                               UIFactory.Accent, TextAlignmentOptions.MidlineLeft);
        monitorText.rectTransform.offsetMin = new Vector2(10f, 0f);
        monitorText.rectTransform.offsetMax = new Vector2(-10f, 0f);
        var monitor = monitorLine.gameObject.AddComponent<StateMonitorController>();
        SceneBuilderUtil.Wire(monitor, "label", monitorText);

        ConsoleController console = AutomationDriveSceneBuilder.BuildConsole(workspace);

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
        SceneBuilderUtil.Wire(controller, "codeChatButton", codeChatButton);
        SceneBuilderUtil.Wire(controller, "deriveGridFromRoute",   true);
        SceneBuilderUtil.Wire(controller, "workspaceToggleButton", workspaceToggle);
        SceneBuilderUtil.Wire(controller, "workspaceRoot",         workspace.gameObject);
        SceneBuilderUtil.Wire(controller, "readmeButton",          commands);
        SceneBuilderUtil.Wire(controller, "readmePanel",           readmePanel);
        SceneBuilderUtil.Wire(controller, "readmeCloseButton",     readmeClose);
        SceneBuilderUtil.Wire(controller, "runButton",    run);
        SceneBuilderUtil.Wire(controller, "pauseButton",  pause);
        SceneBuilderUtil.Wire(controller, "resetButton",  reset);
        SceneBuilderUtil.Wire(controller, "speed1Button", speed1);
        SceneBuilderUtil.Wire(controller, "speed2Button", speed2);
        SceneBuilderUtil.Wire(controller, "speed5Button", speed5);
        SceneBuilderUtil.Wire(controller, "speedSlider",  speedSlider);
        SceneBuilderUtil.Wire(controller, "speedLabel",   speedLabel);
        SceneBuilderUtil.Wire(controller, "stepButton",   step);
        SceneBuilderUtil.Wire(controller, "console",      console);
        SceneBuilderUtil.Wire(controller, "monitor",      monitor);
        SceneBuilderUtil.Wire(controller, "results",      results);
        SceneBuilderUtil.Wire(controller, "flowPuzzle",   flowPuzzle);
        SceneBuilderUtil.Wire(controller, "cratePuzzle",  cratePuzzle);
        SceneBuilderUtil.Wire(controller, "mazeRepairMinigame", mazeRepair);
        SceneBuilderUtil.Wire(controller, "refuelMinigame",     refuel);
        SceneBuilderUtil.Wire(controller, "dialogue",     dialogue);
        SceneBuilderUtil.Wire(controller, "legCompletion", legCompletion);

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
  pickUp()        board the passenger on this stop
  dropOff()       let passengers off at the destination
  collectFare()   collect fare from everyone aboard

<b>CONDITIONS</b>  (ask a yes/no question; used by if / while)
  frontIsClear()   leftIsClear()   rightIsClear()
  atStop()         atDestination()

<b>CONTROL FLOW</b>
  if CONDITION:           run the indented block once if true
  if CONDITION: else:     ... otherwise run the else block
  while CONDITION:        repeat the block while true
  not CONDITION           flip a condition (e.g. while not atDestination():)

In the Code Editor end if/else/while with a colon ':' and indent the body 4
spaces; '#' starts a comment. In Blocks, drop blocks inside the C-shaped
if/while and click the condition chip to choose the question.

<b>EXAMPLE</b> — follow the road to the destination:
  while not atDestination():
      if frontIsClear():
          moveForward()
      else:
          turnLeft()";
}
