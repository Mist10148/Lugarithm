using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds MainMenu.unity — title screen with New Game / Continue / Settings /
/// Quit and the shared Settings panel.
/// </summary>
public static class MainMenuSceneBuilder
{
    public static void Build()
    {
        var scene = SceneBuilderUtil.NewScene();

        SceneBuilderUtil.CreateCamera2D("Main Camera", new Color(0.05f, 0.06f, 0.09f), 5f);
        SceneBuilderUtil.CreateGlobalLight2D();
        SceneBuilderUtil.CreateEventSystem();

        var canvas = UIFactory.CreateCanvas("MenuCanvas");

        // Background with a faint road stripe for flavor.
        UIFactory.CreatePanel(canvas.transform, "Background",
                              Vector2.zero, Vector2.one, new Color(0.05f, 0.06f, 0.09f, 1f));
        var stripe = UIFactory.CreatePanel(canvas.transform, "RoadStripe",
                                           new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                                           new Color(1f, 1f, 1f, 0.03f));
        var stripeRt = stripe;
        stripeRt.offsetMin = new Vector2(0f, -260f);
        stripeRt.offsetMax = new Vector2(0f, 260f);

        // --- Title ----------------------------------------------------------------

        var title = UIFactory.CreateText(canvas.transform, "Title", "LUGARITHM", 110f, UIFactory.Accent);
        UIFactory.Place(title, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(1300f, 140f));
        title.fontStyle = TMPro.FontStyles.Bold;

        var subtitle = UIFactory.CreateText(canvas.transform, "Subtitle",
                                            "“Drive the coast. Recover the pages. Learn the history.”",
                                            28f, UIFactory.TextDim);
        UIFactory.Place(subtitle, new Vector2(0.5f, 1f), new Vector2(0f, -250f), new Vector2(1300f, 50f));

        // --- Buttons ----------------------------------------------------------------

        var column = UIFactory.CreateRect(canvas.transform, "Buttons",
                                          new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        UIFactory.Place(column, new Vector2(0.5f, 0.5f), new Vector2(0f, -110f), new Vector2(340f, 408f));
        UIFactory.AddVerticalLayout(column, 16f, align: TextAnchor.MiddleCenter);

        Button newGame  = UIFactory.CreateButton(column, "NewGameButton",  "New Game", new Vector2(320f, 62f));
        Button cont     = UIFactory.CreateButton(column, "ContinueButton", "Continue", new Vector2(320f, 62f));
        Button settings = UIFactory.CreateButton(column, "SettingsButton", "Settings", new Vector2(320f, 62f));
        Button journal  = UIFactory.CreateButton(column, "JournalButton",  "Journal",  new Vector2(320f, 62f));
        Button quit     = UIFactory.CreateButton(column, "QuitButton",     "Quit",     new Vector2(320f, 62f));

        // --- Version tag -------------------------------------------------------------

        var version = UIFactory.CreateText(canvas.transform, "Version",
                                           "v0.1-dev  ·  Cyfer", 20f, UIFactory.TextDim,
                                           TMPro.TextAlignmentOptions.BottomLeft);
        UIFactory.Place(version, new Vector2(0f, 0f), new Vector2(24f, 16f), new Vector2(400f, 36f));

        // --- Settings panel + manager ---------------------------------------------------

        SettingsPanel settingsPanel = SettingsPanelBuilder.Build(canvas.transform);

        var manager = canvas.gameObject.AddComponent<MainMenuManager>();
        SceneBuilderUtil.Wire(manager, "newGameButton",        newGame);
        SceneBuilderUtil.Wire(manager, "continueButton",       cont);
        SceneBuilderUtil.Wire(manager, "settingsButton",       settings);
        SceneBuilderUtil.Wire(manager, "journalButton",        journal);
        SceneBuilderUtil.Wire(manager, "quitButton",           quit);
        SceneBuilderUtil.Wire(manager, "settingsPanel",        settingsPanel);
        SceneBuilderUtil.Wire(manager, "levelSelectSceneName", "LevelSelect");

        SceneBuilderUtil.SaveScene(scene, "MainMenu");
    }
}
