using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds MainMenu.unity with a Sprout Lands hero layout:
/// title, one dominant New Game action, and secondary action tiles below.
/// </summary>
public static class MainMenuSceneBuilder
{
    static readonly Color MenuInk   = new Color(0.32f, 0.23f, 0.15f, 1f);

    public static void Build()
    {
        var scene = SceneBuilderUtil.NewScene();

        SceneBuilderUtil.CreateCamera2D("Main Camera", new Color(0.05f, 0.06f, 0.09f), 5f);
        SceneBuilderUtil.CreateGlobalLight2D();
        SceneBuilderUtil.CreateEventSystem();

        var canvas = UIFactory.CreateCanvas("MenuCanvas");

        // Background
        UIFactory.CreatePanel(canvas.transform, "Background",
                              Vector2.zero, Vector2.one, new Color(0.04f, 0.05f, 0.08f, 1f));
        var backgroundImage = canvas.transform.Find("Background")?.GetComponent<Image>();
        if (backgroundImage != null)
            backgroundImage.raycastTarget = false;
        var horizon = UIFactory.CreatePanel(canvas.transform, "HorizonGlow",
                                            new Vector2(0f, 0.52f), new Vector2(1f, 0.86f),
                                            new Color(1f, 1f, 1f, 0.03f));
        horizon.offsetMin = new Vector2(0f, -220f);
        horizon.offsetMax = new Vector2(0f, 180f);
        var horizonImage = horizon.GetComponent<Image>();
        if (horizonImage != null)
            horizonImage.raycastTarget = false;

        var stripe = UIFactory.CreatePanel(canvas.transform, "RoadStripe",
                                           new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                                           new Color(1f, 1f, 1f, 0.025f));
        stripe.offsetMin = new Vector2(0f, -250f);
        stripe.offsetMax = new Vector2(0f, 250f);
        var stripeImage = stripe.GetComponent<Image>();
        if (stripeImage != null)
            stripeImage.raycastTarget = false;

        Button newGame = null;
        Button cont = null;
        Button settings = null;
        Button journal = null;
        Button quit = null;

        TMP_FontAsset previousFontOverride = UIFactory.FontOverride;
        UIFactory.FontOverride = SproutLandsMenuFont.EnsureFontAsset();
        try
        {
            // Title block
            var titleShadow = UIFactory.CreateText(canvas.transform, "TitleShadow",
                                                   "LUGARITHM", 104f, new Color(0f, 0f, 0f, 0.45f));
            UIFactory.Place(titleShadow, new Vector2(0.5f, 1f), new Vector2(2f, -128f), new Vector2(1440f, 96f));

            var title = UIFactory.CreateText(canvas.transform, "Title", "LUGARITHM", 104f, UIFactory.Accent);
            UIFactory.Place(title, new Vector2(0.5f, 1f), new Vector2(0f, -132f), new Vector2(1440f, 96f));

            var subtitle = UIFactory.CreateText(canvas.transform, "Subtitle",
                                                "Drive the coast. Recover the pages. Learn the history.",
                                                20f, UIFactory.TextDim);
            UIFactory.Place(subtitle, new Vector2(0.5f, 1f), new Vector2(0f, -238f), new Vector2(1360f, 30f));

            // Hero card
            var heroShadow = UIFactory.CreatePanel(canvas.transform, "HeroShadow",
                                                   new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                                   new Color(0f, 0f, 0f, 0.22f));
            UIFactory.Place(heroShadow, new Vector2(0.5f, 0.5f), new Vector2(0f, -28f), new Vector2(960f, 650f));
            var heroShadowImage = heroShadow.GetComponent<Image>();
            if (heroShadowImage != null)
                heroShadowImage.raycastTarget = false;

            var heroCard = UIFactory.CreateRect(canvas.transform, "HeroCard",
                                                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            UIFactory.Place(heroCard, new Vector2(0.5f, 0.5f), new Vector2(0f, -34f), new Vector2(920f, 610f));
            var heroCardImage = heroCard.gameObject.AddComponent<Image>();
            heroCardImage.sprite = SproutLandsUiLibrary.MenuCardBlank;
            heroCardImage.type = Image.Type.Simple;
            heroCardImage.color = new Color(1f, 1f, 1f, 0.98f);
            heroCardImage.raycastTarget = false;

            var heroContent = UIFactory.CreateRect(heroCard, "Content",
                                                   Vector2.zero, Vector2.one,
                                                   new Vector2(44f, 36f), new Vector2(-44f, -36f));

            var sectionTitle = UIFactory.CreateText(heroContent, "SectionTitle", "START YOUR RUN",
                                                    22f, MenuInk, TextAlignmentOptions.Center);
            UIFactory.Place(sectionTitle, new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(340f, 28f));

            var sectionTag = UIFactory.CreateText(heroContent, "SectionTag",
                                                  "A bright start, then the coast awaits.",
                                                  15f, UIFactory.TextDim, TextAlignmentOptions.Center);
            UIFactory.Place(sectionTag, new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(460f, 22f));

            newGame = UIFactory.CreateArtButton(heroContent, "NewGameButton", "NEW GAME",
                                                new Vector2(392f, 128f),
                                                SproutLandsUiLibrary.BigPlayBlank,
                                                28f, MenuInk);
            newGame.image.preserveAspect = true;
            UIFactory.Place(newGame, new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(392f, 128f));

            cont = UIFactory.CreateArtButton(heroContent, "ContinueButton", "CONTINUE",
                                             new Vector2(304f, 96f),
                                             SproutLandsUiLibrary.BigPlayDark,
                                             24f, MenuInk);
            cont.image.preserveAspect = true;
            UIFactory.Place(cont, new Vector2(0.5f, 1f), new Vector2(0f, -294f), new Vector2(304f, 96f));

            var secondaryRow = UIFactory.CreateRect(heroContent, "SecondaryRow",
                                                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            UIFactory.Place(secondaryRow, new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(624f, 132f));

            settings = UIFactory.CreateIconButton(secondaryRow, "SettingsButton", "SETTINGS",
                                                   new Vector2(100f, 108f),
                                                   SproutLandsUiLibrary.SmallSquareButton,
                                                   SproutLandsUiLibrary.MenuIconSettings,
                                                   13f, MenuInk, 20f);
            UIFactory.Place(settings, new Vector2(0.5f, 0f), new Vector2(-168f, 10f), new Vector2(100f, 108f));

            journal = UIFactory.CreateIconButton(secondaryRow, "JournalButton", "JOURNAL",
                                                  new Vector2(100f, 108f),
                                                  SproutLandsUiLibrary.SmallSquareButton,
                                                  SproutLandsUiLibrary.MenuIconBook,
                                                  13f, MenuInk, 20f);
            UIFactory.Place(journal, new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(100f, 108f));

            quit = UIFactory.CreateIconButton(secondaryRow, "QuitButton", "QUIT",
                                              new Vector2(100f, 108f),
                                              SproutLandsUiLibrary.SmallSquareButton,
                                              SproutLandsUiLibrary.MenuIconQuit,
                                              13f, MenuInk, 20f);
            UIFactory.Place(quit, new Vector2(0.5f, 0f), new Vector2(168f, 10f), new Vector2(100f, 108f));

            // Version tag
            var version = UIFactory.CreateText(canvas.transform, "Version",
                                               "v0.1-dev  ·  Cyfer", 12f, UIFactory.TextDim,
                                               TextAlignmentOptions.BottomLeft);
            UIFactory.Place(version, new Vector2(0f, 0f), new Vector2(24f, 16f), new Vector2(400f, 28f));
        }
        finally
        {
            UIFactory.FontOverride = previousFontOverride;
        }

        // Settings panel + manager
        SettingsPanel settingsPanel = SettingsPanelBuilder.Build(canvas.transform);

        var manager = canvas.gameObject.AddComponent<MainMenuManager>();
        SceneBuilderUtil.Wire(manager, "newGameButton",  newGame);
        SceneBuilderUtil.Wire(manager, "continueButton", cont);
        SceneBuilderUtil.Wire(manager, "settingsButton", settings);
        SceneBuilderUtil.Wire(manager, "journalButton",  journal);
        SceneBuilderUtil.Wire(manager, "quitButton",     quit);
        SceneBuilderUtil.Wire(manager, "settingsPanel",  settingsPanel);
        SceneBuilderUtil.Wire(manager, "levelSelectSceneName", "LevelSelect");

        UIFactory.AddPressFlash(newGame);
        UIFactory.AddPressFlash(cont);
        UIFactory.AddPressFlash(settings);
        UIFactory.AddPressFlash(journal);
        UIFactory.AddPressFlash(quit);

        SceneBuilderUtil.SaveScene(scene, "MainMenu");
    }
}
