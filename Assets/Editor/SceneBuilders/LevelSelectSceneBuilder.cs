using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds LevelSelect.unity — six entry rows (Tutorial + Levels 1–5) with
/// lock states, best scores, Back, and the shared Settings panel.
/// </summary>
public static class LevelSelectSceneBuilder
{
    static readonly Color PanelBg = new Color(0.035f, 0.018f, 0.055f, 0.86f);
    static readonly Color PanelBorder = new Color(0.25f, 0.15f, 0.30f, 0.90f);
    static readonly Color TextBright = new Color(0.98f, 0.96f, 1f, 1f);
    static readonly Color TextMuted = new Color(0.77f, 0.67f, 0.82f, 1f);
    static readonly Color Gold = new Color(0.96f, 0.65f, 0.14f, 1f);
    static readonly Color LockedBg = new Color(0.10f, 0.07f, 0.13f, 0.82f);
    static readonly Color Road = new Color(0.15f, 0.14f, 0.18f, 1f);

    public static void Build()
    {
        var scene = SceneBuilderUtil.NewScene();

        SceneBuilderUtil.CreateCamera2D("Main Camera", new Color(0.05f, 0.06f, 0.09f), 5f);
        SceneBuilderUtil.CreateGlobalLight2D();
        SceneBuilderUtil.CreateEventSystem();

        var canvas = UIFactory.CreateCanvas("LevelSelectCanvas");
        var background = UIFactory.CreatePanel(canvas.transform, "Background",
                                               Vector2.zero, Vector2.one, Color.white);
        var bgImage = background.GetComponent<Image>();
        if (bgImage != null)
        {
            bgImage.sprite = SproutLandsUiLibrary.MainMenuBackground;
            bgImage.type = Image.Type.Simple;
            bgImage.preserveAspect = false;
            bgImage.raycastTarget = false;
        }

        AddBackgroundWashes(canvas.transform);

        TMP_FontAsset previousFontOverride = UIFactory.FontOverride;
        UIFactory.FontOverride = SproutLandsMenuFont.EnsureFontAsset();
        try
        {

        // --- Header -----------------------------------------------------------------

        var header = UIFactory.CreateLocalizedText(canvas.transform, "Header", "levelselect.title", 52f, Gold);
        UIFactory.Place(header, new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(900f, 72f));
        header.fontStyle = FontStyles.Bold;
        header.characterSpacing = 2f;

        var sub = UIFactory.CreateLocalizedText(canvas.transform, "SubHeader",
                                                "levelselect.subtitle", 17f, TextMuted);
        UIFactory.Place(sub, new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(1040f, 34f));

        // Wallet total (top-right of header area)
        var walletBadge = CreateSpritePanel(canvas.transform, "WalletBadge",
                                            new Vector2(1f, 1f), new Vector2(-248f, -32f),
                                            new Vector2(72f, 72f), SproutLandsUiLibrary.SquareButton);
        var wallet = UIFactory.CreateText(walletBadge, "WalletLabel",
                                          "P 0", 24f, Gold,
                                          TextAlignmentOptions.Center);
        wallet.enableAutoSizing = true;
        wallet.fontSizeMin = 13f;
        wallet.fontSizeMax = 24f;
        UIFactory.Place(wallet, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(60f, 44f));

        // --- Road strip -------------------------------------------------------------

        BuildRoadStrip(canvas.transform);

        // --- Entry rows -------------------------------------------------------------

        var column = UIFactory.CreateRect(canvas.transform, "Entries",
                                          new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        UIFactory.Place(column, new Vector2(0.5f, 0.5f), new Vector2(0f, -118f), new Vector2(970f, 560f));
        UIFactory.AddVerticalLayout(column, 12f, align: TextAnchor.UpperCenter);

        var entries = new LevelSelectEntry[LevelLibrary.Count];
        for (int i = 0; i < LevelLibrary.Count; i++)
            entries[i] = BuildEntryRow(column, i);

        // --- Corner buttons -----------------------------------------------------------

        Button back = CreatePixelButton(canvas.transform, "BackButton", "BACK", new Vector2(190f, 54f), false);
        UIFactory.Place(back, new Vector2(0f, 1f), new Vector2(32f, -32f), new Vector2(190f, 54f));

        Button settings = CreatePixelButton(canvas.transform, "SettingsButton", "SETTINGS", new Vector2(190f, 54f), false);
        UIFactory.Place(settings, new Vector2(1f, 1f), new Vector2(-34f, -32f), new Vector2(190f, 54f));

        // --- Settings panel + manager ----------------------------------------------------

        SettingsPanel settingsPanel = SettingsPanelBuilder.Build(canvas.transform);

        var manager = canvas.gameObject.AddComponent<LevelSelectManager>();
        SceneBuilderUtil.WireArray(manager, "entries", entries);
        SceneBuilderUtil.Wire(manager, "backButton",          back);
        SceneBuilderUtil.Wire(manager, "settingsButton",      settings);
        SceneBuilderUtil.Wire(manager, "settingsPanel",       settingsPanel);
        SceneBuilderUtil.Wire(manager, "walletLabel",         wallet);
        SceneBuilderUtil.Wire(manager, "mainMenuSceneName",   "MainMenu");
        SceneBuilderUtil.Wire(manager, "manualSceneName",     "ManualDrive");
        SceneBuilderUtil.Wire(manager, "automationSceneName", "CodeDrive");

        }
        finally
        {
            UIFactory.FontOverride = previousFontOverride;
        }

        SceneBuilderUtil.SaveScene(scene, "LevelSelect");
    }

    // -------------------------------------------------------------------------

    static LevelSelectEntry BuildEntryRow(RectTransform parent, int index)
    {
        var row = UIFactory.CreateRect(parent, $"Entry_{index}",
                                       new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        row.sizeDelta = new Vector2(900f, 84f);
        UIFactory.SetLayoutSize(row, 940f, index == 0 ? 92f : 78f);
        row.sizeDelta = new Vector2(940f, index == 0 ? 92f : 78f);

        var face = row.gameObject.AddComponent<Image>();
        face.sprite = index == 0 ? SproutLandsUiLibrary.MainMenuButton : SproutLandsUiLibrary.MainMenuButtonDisabled;
        face.type   = Image.Type.Simple;
        face.color  = Color.white;
        face.raycastTarget = true;

        var button = row.gameObject.AddComponent<Button>();
        button.targetGraphic = face;
        button.transition = Selectable.Transition.ColorTint;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.10f, 1.07f, 1.12f, 1f);
        colors.pressedColor = new Color(0.86f, 0.82f, 0.90f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.78f, 0.72f, 0.80f, 0.72f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        var badge = CreatePixelPanel(row, "NumberBadge",
                                     new Vector2(0f, 0.5f), new Vector2(48f, 0f),
                                     new Vector2(58f, 46f),
                                     index == 0 ? new Color(0.34f, 0.18f, 0.12f, 0.92f) : LockedBg,
                                     PanelBorder);
        var badgeText = UIFactory.CreateText(badge, "Text", (index + 1).ToString("00"), 17f,
                                             index == 0 ? Gold : TextMuted, TextAlignmentOptions.Center);
        UIFactory.Place(badgeText, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(48f, 24f));

        var name = UIFactory.CreateText(row, "Name", "", index == 0 ? 29f : 23f, TextBright,
                                        TextAlignmentOptions.MidlineLeft);
        name.textWrappingMode = TextWrappingModes.NoWrap;
        name.overflowMode = TextOverflowModes.Ellipsis;
        UIFactory.Place(name, new Vector2(0f, 0.5f), new Vector2(128f, index == 0 ? 12f : 9f), new Vector2(590f, 40f));

        var subtitle = UIFactory.CreateText(row, "Subtitle",
                                            LevelLibrary.Names[index].ToUpperInvariant(),
                                            12f,
                                            index == 0
                                                ? new Color(0.32f, 0.17f, 0.07f, 0.95f)
                                                : new Color(0.88f, 0.82f, 0.92f, 1f),
                                            TextAlignmentOptions.MidlineLeft);
        subtitle.textWrappingMode = TextWrappingModes.NoWrap;
        subtitle.overflowMode = TextOverflowModes.Ellipsis;
        UIFactory.Place(subtitle, new Vector2(0f, 0.5f), new Vector2(130f, index == 0 ? -24f : -20f), new Vector2(560f, 24f));

        var best = UIFactory.CreateText(row, "Best", "", 17f, UIFactory.TextDim,
                                        TextAlignmentOptions.MidlineRight);
        best.textWrappingMode = TextWrappingModes.NoWrap;
        best.overflowMode = TextOverflowModes.Ellipsis;
        UIFactory.Place(best, new Vector2(1f, 0.5f), new Vector2(-292f, -22f), new Vector2(230f, 28f));

        var status = UIFactory.CreateText(row, "Status", "", 18f, index == 0 ? Gold : TextMuted,
                                          TextAlignmentOptions.MidlineRight);
        status.textWrappingMode = TextWrappingModes.NoWrap;
        status.overflowMode = TextOverflowModes.Ellipsis;
        UIFactory.Place(status, new Vector2(1f, 0.5f), new Vector2(-42f, 2f), new Vector2(190f, 38f));

        var lockOverlay = UIFactory.CreatePanel(row, "LockOverlay",
                                                Vector2.zero, Vector2.one,
                                                new Color(0.02f, 0.01f, 0.03f, 0f));
        lockOverlay.GetComponent<Image>().raycastTarget = false;

        var entry = row.gameObject.AddComponent<LevelSelectEntry>();
        SceneBuilderUtil.Wire(entry, "nameLabel",   name);
        SceneBuilderUtil.Wire(entry, "statusLabel", status);
        SceneBuilderUtil.Wire(entry, "bestLabel",   best);
        SceneBuilderUtil.Wire(entry, "lockOverlay", lockOverlay.gameObject);
        SceneBuilderUtil.Wire(entry, "button",      button);

        return entry;
    }

    static void AddBackgroundWashes(Transform canvas)
    {
        var fullWash = UIFactory.CreatePanel(canvas, "BackgroundWash",
                                             Vector2.zero, Vector2.one,
                                             new Color(0.02f, 0.01f, 0.035f, 0.34f));
        SetNonInteractive(fullWash);

        var contentWash = UIFactory.CreatePanel(canvas, "ContentReadabilityWash",
                                                new Vector2(0f, 0f), Vector2.one,
                                                new Color(0.04f, 0.018f, 0.065f, 0.28f));
        SetNonInteractive(contentWash);
    }

    static void BuildRoadStrip(Transform parent)
    {
        var strip = CreateSpritePanel(parent, "RoadMap",
                                      new Vector2(0.5f, 1f), new Vector2(0f, -142f),
                                      new Vector2(920f, 84f), SproutLandsUiLibrary.MainMenuButtonDisabled);

        var road = UIFactory.CreatePanel(strip, "RoadLine",
                                         new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                         Road);
        road.pivot = new Vector2(0.5f, 0.5f);
        road.anchoredPosition = new Vector2(0f, 11f);
        road.sizeDelta = new Vector2(820f, 16f);
        SetNonInteractive(road);

        for (int d = 0; d < 13; d++)
        {
            var dash = UIFactory.CreatePanel(strip, $"RoadDash_{d}",
                                             new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                             new Color(0.86f, 0.70f, 0.30f, 0.90f));
            dash.pivot = new Vector2(0.5f, 0.5f);
            dash.anchoredPosition = new Vector2(-360f + d * 60f, 11f);
            dash.sizeDelta = new Vector2(26f, 3f);
            SetNonInteractive(dash);
        }

        string[] names = { "TUTORIAL", "MOLO", "OTON", "TIGBAUAN", "MIAG-AO", "SAN JOAQUIN" };
        for (int i = 0; i < names.Length; i++)
        {
            float x = -380f + i * 152f;
            var node = CreatePixelPanel(strip, $"Node_{i}",
                                        new Vector2(0.5f, 0.5f), new Vector2(x, 11f),
                                        new Vector2(i == 0 ? 54f : 44f, i == 0 ? 54f : 44f),
                                        i == 0 ? Gold : new Color(0.24f, 0.17f, 0.30f, 1f),
                                        i == 0 ? new Color(0.32f, 0.17f, 0.08f, 1f) : new Color(0.52f, 0.42f, 0.62f, 1f));
            var number = UIFactory.CreateText(node, "Number", (i + 1).ToString(), i == 0 ? 21f : 16f,
                                              i == 0 ? new Color(0.18f, 0.08f, 0.03f, 1f) : TextBright,
                                              TextAlignmentOptions.Center);
            UIFactory.Place(number, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(38f, 26f));

            var label = UIFactory.CreateText(strip, $"Node_{i}_Label", names[i], 12f,
                                             i == 0 ? Gold : new Color(0.90f, 0.85f, 0.93f, 1f), TextAlignmentOptions.Center);
            UIFactory.Place(label, new Vector2(0.5f, 0.5f), new Vector2(x, -29f), new Vector2(132f, 20f));
        }
    }

    static Button CreatePixelButton(Transform parent, string name, string label, Vector2 size, bool disabled)
    {
        var row = UIFactory.CreateRect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        row.sizeDelta = size;
        var image = row.gameObject.AddComponent<Image>();
        image.sprite = disabled ? SproutLandsUiLibrary.MainMenuButtonDisabled : SproutLandsUiLibrary.MainMenuButton;
        image.type = Image.Type.Simple;
        image.color = Color.white;
        image.raycastTarget = true;

        var button = row.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        var text = UIFactory.CreateText(row, "Label", label, 18f, TextBright, TextAlignmentOptions.Center);
        text.fontStyle = FontStyles.Bold;
        UIFactory.Place(text, new Vector2(0.5f, 0.5f), new Vector2(0f, -2f), new Vector2(size.x - 28f, size.y - 14f));
        return button;
    }

    static RectTransform CreatePixelPanel(Transform parent, string name, Vector2 anchor,
                                          Vector2 position, Vector2 size, Color fill, Color border)
    {
        var panel = UIFactory.CreatePanel(parent, name, anchor, anchor, fill);
        panel.pivot = anchor;
        panel.anchoredPosition = position;
        panel.sizeDelta = size;
        var outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = border;
        outline.effectDistance = new Vector2(3f, -3f);
        outline.useGraphicAlpha = true;
        SetNonInteractive(panel);
        return panel;
    }

    static RectTransform CreateSpritePanel(Transform parent, string name, Vector2 anchor,
                                           Vector2 position, Vector2 size, Sprite sprite)
    {
        var panel = UIFactory.CreatePanel(parent, name, anchor, anchor, sprite == null ? Color.clear : Color.white);
        panel.pivot = anchor;
        panel.anchoredPosition = position;
        panel.sizeDelta = size;
        var image = panel.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
        }
        return panel;
    }

    static void FlattenPanelOutline(RectTransform panel)
    {
        var outline = panel.GetComponent<Outline>();
        if (outline != null)
            outline.effectDistance = Vector2.zero;
    }

    static void SetNonInteractive(RectTransform rect)
    {
        var image = rect.GetComponent<Image>();
        if (image != null)
            image.raycastTarget = false;
    }
}
