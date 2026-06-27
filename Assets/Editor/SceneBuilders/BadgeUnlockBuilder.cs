using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the persistent badge unlock overlay in Bootstrap. The overlay is
/// created as a DontDestroyOnLoad singleton and shown by drive controllers on
/// leg completion.
/// </summary>
public static class BadgeUnlockBuilder
{
    public static BadgeUnlockManager Build()
    {
        var managerGo = new GameObject("BadgeUnlockManager");
        var manager   = managerGo.AddComponent<BadgeUnlockManager>();

        // Canvas — sort order 300 (above Almanac at 200, below transition at 1000)
        var canvas = UIFactory.CreateCanvas("BadgeCanvas", 300);
        canvas.transform.SetParent(managerGo.transform, false);

        // Backdrop (full-screen, blocks clicks while showing)
        var backdrop = UIFactory.CreatePanel(canvas.transform, "Backdrop",
                                             Vector2.zero, Vector2.one,
                                             new Color(0f, 0f, 0f, 0.80f));
        backdrop.GetComponent<Image>().raycastTarget = true;
        var backdropGroup = backdrop.gameObject.AddComponent<CanvasGroup>();

        // Click-anywhere-on-the-dim escape hatch. Transition None so hovering the
        // backdrop never tints the whole screen. The centered Window (added below)
        // sits on top and absorbs its own clicks, so only the dim area dismisses.
        var backdropButton = backdrop.gameObject.AddComponent<Button>();
        backdropButton.transition = Selectable.Transition.None;

        // Window (centered, 680×480)
        var window = UIFactory.CreatePanel(backdrop, "Window",
                                           new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                           UIFactory.PanelDark);
        UIFactory.Place(window, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(680f, 480f));

        // Amber border (slightly larger, drawn behind)
        var border = UIFactory.CreatePanel(backdrop, "Border",
                                           new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                           UIFactory.Accent);
        UIFactory.Place(border, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(686f, 486f));
        border.SetSiblingIndex(window.GetSiblingIndex() - 1);

        // "BADGE EARNED" header
        var header = UIFactory.CreateLocalizedText(window, "Header", "badge.earned",
                                                   22f, UIFactory.TextDim);
        UIFactory.Place(header, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(600f, 34f));

        // Placeholder badge art (amber square)
        var badgeRect = UIFactory.CreatePanel(window, "BadgePlaceholder",
                                              new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                              UIFactory.Accent);
        UIFactory.Place(badgeRect, new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(120f, 120f));
        var badgeImage = badgeRect.GetComponent<Image>();

        // Badge name (large, bold)
        var badgeName = UIFactory.CreateText(window, "BadgeName", "",
                                             52f, UIFactory.Accent);
        badgeName.fontStyle = TMPro.FontStyles.Bold;
        badgeName.enableWordWrapping = true;
        UIFactory.Place(badgeName, new Vector2(0.5f, 1f), new Vector2(0f, -220f), new Vector2(620f, 70f));

        // Town name (small caps style)
        var townName = UIFactory.CreateText(window, "TownName", "",
                                            20f, UIFactory.TextDim);
        UIFactory.Place(townName, new Vector2(0.5f, 1f), new Vector2(0f, -294f), new Vector2(620f, 30f));

        // Description
        var description = UIFactory.CreateText(window, "Description", "",
                                               20f, UIFactory.TextBright);
        description.enableWordWrapping = true;
        UIFactory.Place(description, new Vector2(0.5f, 1f), new Vector2(0f, -336f), new Vector2(580f, 60f));

        // Continue button
        Button continueBtn = UIFactory.CreateButton(window, "ContinueButton",
                                                    "Continue", new Vector2(260f, 58f));
        UIFactory.LocalizeButton(continueBtn, "common.continue");
        UIFactory.Place(continueBtn, new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(260f, 58f));
        continueBtn.image.color = UIFactory.Accent;

        // Wire BadgeUnlockPanel
        var panel = window.gameObject.AddComponent<BadgeUnlockPanel>();
        SceneBuilderUtil.Wire(panel, "root",             backdrop.gameObject);
        SceneBuilderUtil.Wire(panel, "badgeNameLabel",   badgeName);
        SceneBuilderUtil.Wire(panel, "townNameLabel",    townName);
        SceneBuilderUtil.Wire(panel, "descriptionLabel", description);
        SceneBuilderUtil.Wire(panel, "badgePlaceholder", badgeImage);
        SceneBuilderUtil.Wire(panel, "continueButton",   continueBtn);
        SceneBuilderUtil.Wire(panel, "backdropButton",   backdropButton);
        SceneBuilderUtil.Wire(panel, "canvasGroup",      backdropGroup);

        // Wire BadgeUnlockManager
        SceneBuilderUtil.Wire(manager, "panel", panel);

        // Start hidden
        backdrop.gameObject.SetActive(false);

        return manager;
    }
}
