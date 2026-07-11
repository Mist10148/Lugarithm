using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds MainMenu.unity with a scenic coastal background and a right-aligned
/// title/menu composition.
/// </summary>
public static class MainMenuSceneBuilder
{
    static readonly Color ButtonText = new Color(0.98f, 0.96f, 1f, 1f);

    public static void Build()
    {
        var scene = SceneBuilderUtil.NewScene();

        SceneBuilderUtil.CreateCamera2D("Main Camera", new Color(0.05f, 0.06f, 0.09f), 5f);
        SceneBuilderUtil.CreateGlobalLight2D();
        SceneBuilderUtil.CreateEventSystem();

        var canvas = UIFactory.CreateCanvas("MenuCanvas");

        var background = UIFactory.CreatePanel(canvas.transform, "Background",
                                               Vector2.zero, Vector2.one, Color.white);
        var backgroundImage = background.GetComponent<Image>();
        if (backgroundImage != null)
        {
            backgroundImage.sprite = SproutLandsUiLibrary.MainMenuBackground;
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.preserveAspect = false;
            backgroundImage.raycastTarget = false;
        }

        AddReadabilityWash(canvas.transform);

        Button newGame = null;
        Button cont = null;
        Button settings = null;
        Button journal = null;
        Button quit = null;

        TMP_FontAsset previousFontOverride = UIFactory.FontOverride;
        UIFactory.FontOverride = SproutLandsMenuFont.EnsureFontAsset();
        try
        {
            var titleArt = UIFactory.CreateRect(canvas.transform, "TitleArt",
                                                new Vector2(1f, 1f), new Vector2(1f, 1f));
            titleArt.pivot = new Vector2(1f, 1f);
            titleArt.anchoredPosition = new Vector2(-102f, -28f);
            titleArt.sizeDelta = new Vector2(820f, 292f);
            var titleImage = titleArt.gameObject.AddComponent<Image>();
            titleImage.sprite = SproutLandsUiLibrary.MainMenuTitle;
            titleImage.type = Image.Type.Simple;
            titleImage.preserveAspect = true;
            titleImage.raycastTarget = false;

            var menuStack = UIFactory.CreateRect(canvas.transform, "MenuStack",
                                                 new Vector2(1f, 0f), new Vector2(1f, 0f));
            menuStack.pivot = new Vector2(1f, 0f);
            menuStack.anchoredPosition = new Vector2(-250f, 140f);
            menuStack.sizeDelta = new Vector2(462f, 486f);

            newGame = CreateMenuRowButton(menuStack, "NewGameButton", "JEEP JOURNEY", "menu.newgame",
                                          SproutLandsUiLibrary.MenuIconJeep, 0f, false);
            cont = CreateMenuRowButton(menuStack, "ContinueButton", "CONTINUE", "menu.continue",
                                       SproutLandsUiLibrary.MenuIconRoute, -92f, true);
            journal = CreateMenuRowButton(menuStack, "JournalButton", "JOURNAL", "menu.journal",
                                          SproutLandsUiLibrary.MenuIconBook, -184f, false);
            settings = CreateMenuRowButton(menuStack, "SettingsButton", "SETTINGS", "menu.settings",
                                           SproutLandsUiLibrary.MenuIconSettings, -276f, false);
            quit = CreateMenuRowButton(menuStack, "QuitButton", "EXIT", "menu.quit",
                                       SproutLandsUiLibrary.MenuIconQuit, -368f, false);

            var version = UIFactory.CreateText(canvas.transform, "Version",
                                               "v0.1-dev  ·  Cyfer", 12f,
                                               new Color(0.86f, 0.84f, 0.90f, 0.78f),
                                               TextAlignmentOptions.BottomLeft);
            UIFactory.Place(version, new Vector2(0f, 0f), new Vector2(24f, 16f), new Vector2(400f, 28f));
        }
        finally
        {
            UIFactory.FontOverride = previousFontOverride;
        }

        // Settings is the single universal overlay (owned by Bootstrap, or lazily
        // loaded from Resources when MainMenu is opened directly). No per-scene
        // Settings panel is baked here anymore — MainMenuManager.OnOpenSettings
        // routes to UniversalSettingsManager.
        var manager = canvas.gameObject.AddComponent<MainMenuManager>();
        SceneBuilderUtil.Wire(manager, "newGameButton", newGame);
        SceneBuilderUtil.Wire(manager, "continueButton", cont);
        SceneBuilderUtil.Wire(manager, "settingsButton", settings);
        SceneBuilderUtil.Wire(manager, "journalButton", journal);
        SceneBuilderUtil.Wire(manager, "quitButton", quit);
        SceneBuilderUtil.Wire(manager, "levelSelectSceneName", "LevelSelect");

        UIFactory.AddPressFlash(newGame);
        UIFactory.AddPressFlash(cont);
        UIFactory.AddPressFlash(settings);
        UIFactory.AddPressFlash(journal);
        UIFactory.AddPressFlash(quit);

        // MainMenu is also a supported direct-entry scene in the editor.  Bootstrap
        // normally supplies the persistent AlmanacManager, but without this local
        // fallback the Journal button silently did nothing when MainMenu was opened
        // directly. AlmanacManager's singleton guard destroys this copy when the
        // Bootstrap instance already exists, so normal scene routing is unchanged.
        AlmanacOverlayBuilder.Build(null);

        SceneBuilderUtil.SaveScene(scene, "MainMenu");
    }

    static void AddReadabilityWash(Transform canvas)
    {
        var fullWash = UIFactory.CreatePanel(canvas, "BackgroundWash",
                                             Vector2.zero, Vector2.one,
                                             new Color(0.02f, 0.015f, 0.04f, 0.16f));
        SetNonInteractive(fullWash);

        var rightWash = UIFactory.CreatePanel(canvas, "RightReadabilityWash",
                                              new Vector2(0.54f, 0f), Vector2.one,
                                              new Color(0.08f, 0.04f, 0.12f, 0.30f));
        SetNonInteractive(rightWash);

        var lowerWash = UIFactory.CreatePanel(canvas, "LowerReadabilityWash",
                                              new Vector2(0f, 0f), new Vector2(1f, 0.46f),
                                              new Color(0.02f, 0.015f, 0.04f, 0.20f));
        SetNonInteractive(lowerWash);
    }

    static Button CreateMenuRowButton(RectTransform parent, string name, string label, string labelKey,
                                      Sprite iconSprite, float y, bool disabledTint)
    {
        var row = UIFactory.CreateRect(parent, name, new Vector2(1f, 1f), new Vector2(1f, 1f));
        row.pivot = new Vector2(1f, 1f);
        row.anchoredPosition = new Vector2(0f, y);
        row.sizeDelta = new Vector2(450f, 76f);

        var image = row.gameObject.AddComponent<Image>();
        image.sprite = disabledTint ? SproutLandsUiLibrary.MainMenuButtonDisabled : SproutLandsUiLibrary.MainMenuButton;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = Color.white;
        image.raycastTarget = true;

        var button = row.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.13f, 1.10f, 1.18f, 1f);
        colors.pressedColor = new Color(0.82f, 0.78f, 0.90f, 1f);
        colors.selectedColor = new Color(1.04f, 1.02f, 1.08f, 1f);
        colors.disabledColor = new Color(0.70f, 0.68f, 0.74f, 0.68f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        var iconCell = UIFactory.CreatePanel(row, "IconCell",
                                             new Vector2(0f, 0f), new Vector2(0f, 1f),
                                             new Color(0.18f, 0.14f, 0.26f, 0.54f));
        iconCell.pivot = new Vector2(0f, 0.5f);
        iconCell.sizeDelta = new Vector2(78f, 0f);
        SetNonInteractive(iconCell);

        var divider = UIFactory.CreatePanel(row, "IconDivider",
                                            new Vector2(0f, 0f), new Vector2(0f, 1f),
                                            new Color(0.96f, 0.92f, 1f, 0.72f));
        divider.pivot = new Vector2(0f, 0.5f);
        divider.anchoredPosition = new Vector2(78f, 0f);
        divider.sizeDelta = new Vector2(2f, 0f);
        SetNonInteractive(divider);

        var icon = UIFactory.CreateRect(row, "Icon",
                                        new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
        icon.pivot = new Vector2(0.5f, 0.5f);
        icon.anchoredPosition = new Vector2(39f, 0f);
        icon.sizeDelta = new Vector2(32f, 32f);
        var iconImage = icon.gameObject.AddComponent<Image>();
        iconImage.sprite = iconSprite;
        iconImage.type = Image.Type.Simple;
        iconImage.color = ButtonText;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        var text = UIFactory.CreateText(row, "Label", label, 24f, ButtonText,
                                        TextAlignmentOptions.MidlineLeft);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.fontStyle = FontStyles.Bold;
        text.rectTransform.anchorMin = new Vector2(0f, 0f);
        text.rectTransform.anchorMax = new Vector2(1f, 1f);
        text.rectTransform.offsetMin = new Vector2(106f, 8f);
        text.rectTransform.offsetMax = new Vector2(-22f, -8f);
        UIFactory.Localize(text, labelKey);

        return button;
    }

    static void SetNonInteractive(RectTransform rect)
    {
        var image = rect.GetComponent<Image>();
        if (image != null)
            image.raycastTarget = false;
    }
}
