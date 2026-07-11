using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the persistent Almanac / Journal / Oracle overlay as a child of
/// Bootstrap.unity. The root GameObject receives an <see cref="AlmanacManager"/>
/// whose Awake calls DontDestroyOnLoad, so the book survives scene changes.
/// </summary>
public static class AlmanacOverlayBuilder
{
    // Parchment-friendly ink palette — the book pages are light, so all chrome
    // text must be dark to read. Mirrored in AlmanacController for runtime states.
    static readonly Color32 Ink    = new Color32(66, 42, 30, 255);
    static readonly Color32 InkDim = new Color32(120, 92, 68, 255);
    static readonly Color32 Amber  = new Color32(155, 90, 12, 255);
    static readonly Color32 CardNormal = new Color32(214, 190, 150, 255);

    public static AlmanacManager Build(Transform parent)
    {
        var root = new GameObject("AlmanacManager");
        if (parent != null)
            root.transform.SetParent(parent, false);

        var manager = root.AddComponent<AlmanacManager>();

        // Canvas (sort order 200 — above scene UI, below transition fade at 1000)
        Canvas canvas = UIFactory.CreateCanvas("AlmanacCanvas", 200);
        canvas.transform.SetParent(root.transform, false);

        // ---- Backdrop (the toggled root) -------------------------------------
        var backdrop = UIFactory.CreatePanel(canvas.transform, "Backdrop",
                                             Vector2.zero, Vector2.one,
                                             new Color(0f, 0f, 0f, 0.7f));
        backdrop.GetComponent<Image>().raycastTarget = true;

        // ---- Book border + panel ---------------------------------------------
        var bookBorder = UIFactory.CreatePanel(backdrop, "BookBorder",
                                               new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                               Color.clear);
        UIFactory.Place(bookBorder, new Vector2(0.5f, 0.5f), Vector2.zero,
                        new Vector2(1696f, 870f));

        var bookPanel = UIFactory.CreatePanel(bookBorder, "BookPanel",
                                              new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                              Color.clear);
        UIFactory.Place(bookPanel, new Vector2(0.5f, 0.5f), Vector2.zero,
                        new Vector2(1690f, 864f));
        var bookImage = bookPanel.GetComponent<Image>();
        bookImage.sprite = LugarithmUiSkin.JournalBook;
        bookImage.type = Image.Type.Simple;
        bookImage.preserveAspect = true;
        bookImage.color = Color.white;

        // ---- Tab bar (top, full width): Heritage / Coding / Oracle -----------
        var tabBar = UIFactory.CreateRect(bookPanel, "TabBar",
                                          new Vector2(0f, 1f), new Vector2(1f, 1f),
                                          new Vector2(150f, -145f), new Vector2(-790f, 28f));

        Button heritageTab = UIFactory.CreateButton(tabBar, "HeritageTab", "Heritage Pages",
                                                    new Vector2(190f, 120f), 22f);
        UIFactory.Place(heritageTab, new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(190f, 120f));
        SetLabelColor(heritageTab, UIFactory.Accent);

        Button codingTab = UIFactory.CreateButton(tabBar, "CodingTab", "Coding Reference",
                                                  new Vector2(190f, 120f), 20f);
        UIFactory.Place(codingTab, new Vector2(0f, 0.5f), new Vector2(205f, 0f), new Vector2(190f, 120f));
        SetLabelColor(codingTab, InkDim);

        Button oracleTab = UIFactory.CreateButton(tabBar, "OracleTab", "Oracle",
                                                  new Vector2(170f, 120f), 22f);
        UIFactory.Place(oracleTab, new Vector2(0f, 0.5f), new Vector2(402f, 0f), new Vector2(170f, 120f));
        SetLabelColor(oracleTab, InkDim);
        heritageTab.image.sprite = null;
        codingTab.image.sprite = null;
        oracleTab.image.sprite = null;
        heritageTab.image.color = Color.clear;
        codingTab.image.color = Color.clear;
        oracleTab.image.color = Color.clear;

        // ==== Detail pane (PvZ two-pane: thumbnail grid + entry detail) =======
        var detailPane = UIFactory.CreateRect(bookPanel, "DetailPane",
                                              new Vector2(0f, 0f), new Vector2(1f, 1f),
                                              new Vector2(135f, 88f), new Vector2(-135f, -155f));

        // Left: scrollable grid of entry thumbnails.
        ScrollRect sidebarScroll = UIFactory.CreateScrollView(detailPane, "EntryGrid",
                                                              new Vector2(0f, 0f), new Vector2(0.4f, 1f),
                                                              out RectTransform sidebarContent);
        ClearScrollChrome(sidebarScroll);
        ((RectTransform)sidebarScroll.transform).offsetMax = new Vector2(-4f, 0f);
        UIFactory.AddVerticalScrollbar(sidebarScroll);

        // Swap the default vertical list for a PvZ-style grid of cards.
        var defaultLayout = sidebarContent.GetComponent<VerticalLayoutGroup>();
        if (defaultLayout != null) Object.DestroyImmediate(defaultLayout);
        var grid = sidebarContent.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize        = new Vector2(270f, 130f);
        grid.spacing         = new Vector2(14f, 14f);
        grid.padding         = new RectOffset(8, 8, 8, 8);
        grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        grid.childAlignment  = TextAnchor.UpperCenter;

        // Thumbnail card template.
        // Flat clean card — a tinted builtin 9-slice; the controller tints per
        // state (normal / selected / locked) instead of swapping sprites.
        Button sidebarEntryTemplate = UIFactory.CreateButton(sidebarContent,
                                                             "SidebarEntryTemplate",
                                                             "Town", new Vector2(270f, 130f), 18f);
        sidebarEntryTemplate.image.sprite = UIFactory.BuiltinSprite("UISprite.psd");
        sidebarEntryTemplate.image.type = Image.Type.Sliced;
        sidebarEntryTemplate.image.color = CardNormal;
        var entryTemplLabel = sidebarEntryTemplate.GetComponentInChildren<TMP_Text>();
        if (entryTemplLabel != null)
        {
            entryTemplLabel.alignment = TextAlignmentOptions.Center;
            entryTemplLabel.textWrappingMode = TextWrappingModes.Normal;
            entryTemplLabel.color = Ink;
            entryTemplLabel.margin = new Vector4(12f, 10f, 12f, 10f);
        }
        var entryIcon = AddSprite(sidebarEntryTemplate.transform, "EntryIcon", null,
                                  new Vector2(0f, 0.5f), new Vector2(38f, 0f), new Vector2(44f, 44f));
        entryIcon.GetComponent<Image>().raycastTarget = false;
        sidebarEntryTemplate.gameObject.SetActive(false);

        // Right: entry detail — art banner + title + body.
        var detailArea = UIFactory.CreateRect(detailPane, "DetailArea",
                                              new Vector2(0.4f, 0f), new Vector2(1f, 1f),
                                              new Vector2(42f, 0f), new Vector2(0f, 0f));

        var titleRibbon = AddSprite(detailArea, "TitleRibbon", LugarithmUiSkin.JournalTitleRibbon,
                                    new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(560f, 74f));

        var entryArtFrame = UIFactory.CreatePanel(detailArea, "EntryArt",
                                                  new Vector2(0f, 1f), new Vector2(1f, 1f),
                                                  Color.clear);
        entryArtFrame.offsetMin = new Vector2(54f, -360f);
        entryArtFrame.offsetMax = new Vector2(-54f, -92f);
        var entryArt = entryArtFrame.GetComponent<Image>();
        var entryArtLabel = UIFactory.CreateText(entryArtFrame, "ArtInitials", "", 42f,
                                                 UIFactory.TextBright, TextAlignmentOptions.Center);
        entryArtLabel.rectTransform.offsetMin = Vector2.zero;
        entryArtLabel.rectTransform.offsetMax = Vector2.zero;
        entryArtLabel.fontStyle = FontStyles.Bold;

        var contentTitle = UIFactory.CreateText(detailArea, "ContentTitle", "", 28f,
                                                new Color32(239, 169, 18, 255), TextAlignmentOptions.Center);
        contentTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
        contentTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
        contentTitle.rectTransform.offsetMin = new Vector2(36f, -80f);
        contentTitle.rectTransform.offsetMax = new Vector2(-36f, -18f);

        ScrollRect contentScroll = UIFactory.CreateScrollView(detailArea, "ContentScroll",
                                                              new Vector2(0f, 0f), new Vector2(1f, 1f),
                                                              out RectTransform contentBodyRect);
        ClearScrollChrome(contentScroll);
        ((RectTransform)contentScroll.transform).offsetMin = new Vector2(44f, 70f);
        ((RectTransform)contentScroll.transform).offsetMax = new Vector2(-44f, -382f);
        UIFactory.AddVerticalScrollbar(contentScroll);

        var contentBody = UIFactory.CreateText(contentBodyRect, "Body", "", 19f,
                                               Ink, TextAlignmentOptions.TopLeft);
        contentBody.margin = new Vector4(8f, 8f, 22f, 8f);
        contentBody.textWrappingMode = TextWrappingModes.Normal;
        // Size the body to its actual text so long reference entries scroll instead of
        // clipping at a fixed height (the scroll content fitter grows to match).
        var bodyFitter = contentBody.gameObject.AddComponent<ContentSizeFitter>();
        bodyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Button previousButton = CreateJournalNavButton(detailArea, "PreviousButton", "nav_prev", new Vector2(70f, 24f));
        Button nextButton = CreateJournalNavButton(detailArea, "NextButton", "nav_next", new Vector2(-70f, 24f));
        var pageIndicator = UIFactory.CreateText(detailArea, "PageIndicator", "1 / 1", 18f,
                                                 new Color32(66, 42, 30, 255), TextAlignmentOptions.Center);
        UIFactory.Place(pageIndicator.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(150f, 40f));

        // ==== Oracle pane (its own tab) =======================================
        var oraclePane = UIFactory.CreateRect(bookPanel, "OraclePane",
                                              new Vector2(0f, 0f), new Vector2(1f, 1f),
                                              new Vector2(135f, 88f), new Vector2(-135f, -155f));

        // Left page: quick-ask topics — flat buttons that drop a starter question
        // into the chat (wired to ChatController below).
        var oracleTopics = UIFactory.CreateRect(oraclePane, "OracleTopics",
                                                new Vector2(0f, 0f), new Vector2(0.39f, 1f),
                                                new Vector2(18f, 38f), new Vector2(-22f, -38f));
        var topicsHeader = UIFactory.CreateText(oracleTopics, "TopicsHeader", "Ask the Oracle", 22f,
                                                Amber, TextAlignmentOptions.MidlineLeft);
        topicsHeader.fontStyle = FontStyles.Bold;
        topicsHeader.rectTransform.anchorMin = new Vector2(0f, 1f);
        topicsHeader.rectTransform.anchorMax = new Vector2(1f, 1f);
        topicsHeader.rectTransform.offsetMin = new Vector2(8f, -44f);
        topicsHeader.rectTransform.offsetMax = new Vector2(-8f, -6f);

        string[] topicTitles = { "Town History", "Landmarks", "Culture", "Coding Help", "Commands", "Tips" };
        string[] topicSubtitles = { "Discover our past", "Explore key places", "Traditions & stories", "Get coding guidance", "In-game commands", "Helpful advice" };
        string[] topicPrompts =
        {
            "Tell me about this town's history.",
            "What landmarks should I know about here?",
            "What traditions and festivals is this town known for?",
            "Can you explain how coding the jeepney works?",
            "What commands can I use in the game?",
            "Any tips for my current level?"
        };
        string inkDimHex = ColorUtility.ToHtmlStringRGB(InkDim);
        var topicButtons = new Button[topicTitles.Length];
        for (int i = 0; i < topicTitles.Length; i++)
        {
            Button row = UIFactory.CreateButton(oracleTopics, $"Topic_{i}", "", new Vector2(0f, 76f), 19f);
            row.image.sprite = UIFactory.BuiltinSprite("UISprite.psd");
            row.image.type = Image.Type.Sliced;
            row.image.color = CardNormal;
            var rowRt = (RectTransform)row.transform;
            rowRt.anchorMin = Vector2.up;
            rowRt.anchorMax = Vector2.one;
            rowRt.offsetMin = new Vector2(0f, -130f - i * 82f);
            rowRt.offsetMax = new Vector2(0f, -54f - i * 82f);
            var label = row.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = "<b>" + topicTitles[i] + "</b>\n<size=75%><color=#" + inkDimHex + ">"
                             + topicSubtitles[i] + "</color></size>";
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.color = Ink;
                label.margin = new Vector4(16f, 6f, 12f, 6f);
            }
            topicButtons[i] = row;
        }

        // Right page: minimal header — title, one-line subtitle, thin rule.
        var oracleRight = UIFactory.CreateRect(oraclePane, "OracleRight",
                                               new Vector2(0.41f, 0f), Vector2.one,
                                               new Vector2(20f, 0f), Vector2.zero);
        var oracleTitle = UIFactory.CreateText(oracleRight, "OracleTitle", "Oracle", 26f,
                                               Amber, TextAlignmentOptions.Center);
        oracleTitle.fontStyle = FontStyles.Bold;
        UIFactory.Place(oracleTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(400f, 40f));
        var oracleSubtitle = UIFactory.CreateText(oracleRight, "OracleSubtitle",
                                                  "Ask about the towns, culture, or coding.", 17f,
                                                  InkDim, TextAlignmentOptions.Center);
        UIFactory.Place(oracleSubtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(560f, 26f));
        var headerRule = UIFactory.CreatePanel(oracleRight, "HeaderRule",
                                               new Vector2(0f, 1f), new Vector2(1f, 1f),
                                               new Color32(120, 92, 68, 110));
        headerRule.offsetMin = new Vector2(40f, -82f);
        headerRule.offsetMax = new Vector2(-40f, -80f);
        headerRule.GetComponent<Image>().raycastTarget = false;

        var oracleFlavour = UIFactory.CreateText(oraclePane, "OracleFlavour",
                                                 "Ask the Oracle about any town or coding concept.",
                                                 18f, UIFactory.TextDim, TextAlignmentOptions.TopLeft);
        oracleFlavour.gameObject.SetActive(false);

        // Clear button (top-right) — wipes the transcript on demand.
        Button clearChatButton = UIFactory.CreateButton(oracleRight, "ClearChatButton", "Clear",
                                                        new Vector2(90f, 30f), 16f);
        clearChatButton.image.color = Color.clear;
        UIFactory.Place(clearChatButton, new Vector2(1f, 1f), new Vector2(-18f, -30f), new Vector2(76f, 28f));
        SetLabelColor(clearChatButton, InkDim);

        ScrollRect chatScroll = UIFactory.CreateScrollView(oracleRight, "ChatScroll",
                                                           new Vector2(0f, 0f), new Vector2(1f, 1f),
                                                           out RectTransform chatContent);
        var chatScrollRt = (RectTransform)chatScroll.transform;
        ClearScrollChrome(chatScroll);
        chatScrollRt.offsetMin = new Vector2(20f, 92f);
        chatScrollRt.offsetMax = new Vector2(-20f, -100f);
        UIFactory.AddVerticalScrollbar(chatScroll, permanent: true);

        var bubbleTemplate = UIFactory.CreateText(chatContent, "BubbleTemplate", "", 16f,
                                                  new Color32(66, 42, 30, 255), TextAlignmentOptions.TopLeft);
        bubbleTemplate.textWrappingMode = TextWrappingModes.Normal;
        var bubbleLe = bubbleTemplate.gameObject.AddComponent<LayoutElement>();
        bubbleLe.preferredHeight = 40f;
        bubbleLe.flexibleWidth = 1f;
        bubbleTemplate.gameObject.SetActive(false);

        var inputRow = UIFactory.CreatePanel(oracleRight, "InputRow",
                                             new Vector2(0f, 0f), new Vector2(1f, 0f),
                                             Color.clear);
        inputRow.offsetMin = new Vector2(18f, 12f);
        inputRow.offsetMax = new Vector2(-18f, 82f);

        TMP_InputField chatInput = CreateSinglelineInput(inputRow, "ChatInput", "Ask the Oracle…");
        var inputRt = chatInput.GetComponent<RectTransform>();
        inputRt.anchorMin = new Vector2(0f, 0f);
        chatInput.GetComponent<Image>().color = new Color32(240, 226, 194, 255);
        inputRt.anchorMax = new Vector2(0.82f, 1f);
        inputRt.offsetMin = new Vector2(4f, 4f);
        inputRt.offsetMax = new Vector2(-4f, -4f);

        Button sendButton = UIFactory.CreateButton(inputRow, "SendButton", "Send",
                                                   new Vector2(0f, 52f), 22f);
        sendButton.image.sprite = UIFactory.BuiltinSprite("UISprite.psd");
        sendButton.image.type = Image.Type.Sliced;
        sendButton.image.color = LugarithmUiSkin.Gold;
        SetLabelColor(sendButton, LugarithmUiSkin.PlumDeep);
        var sendRt = sendButton.GetComponent<RectTransform>();
        sendRt.anchorMin = new Vector2(0.83f, 0f);
        sendRt.anchorMax = new Vector2(1f, 1f);
        sendRt.offsetMin = new Vector2(4f, 4f);
        sendRt.offsetMax = new Vector2(-4f, -4f);

        oraclePane.gameObject.SetActive(false);

        var turnOverlay = UIFactory.CreateRect(bookPanel, "PageTurnOverlay",
                                               new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        UIFactory.Place(turnOverlay, new Vector2(0.5f, 0.5f), new Vector2(0f, -18f),
                        new Vector2(760f, 700f));
        var turnImage = turnOverlay.gameObject.AddComponent<RawImage>();
        turnImage.texture = LugarithmUiSkin.JournalPageTurns;
        turnImage.color = Color.white;
        turnImage.raycastTarget = false;
        turnOverlay.gameObject.SetActive(false);
        var turnAnimator = turnOverlay.gameObject.AddComponent<JournalPageTurnAnimator>();
        SceneBuilderUtil.Wire(turnAnimator, "overlay", turnImage);
        SceneBuilderUtil.Wire(turnAnimator, "heritageTab", heritageTab);
        SceneBuilderUtil.Wire(turnAnimator, "codingTab", codingTab);
        SceneBuilderUtil.Wire(turnAnimator, "oracleTab", oracleTab);

        // ---- Close button -----------------------------------------------------
        Button closeButton = UIFactory.CreateButton(bookPanel, "CloseButton", "✕",
                                                    new Vector2(48f, 48f), 28f);
        closeButton.image.sprite = LugarithmUiSkin.JournalClose;
        closeButton.image.color = Color.white;
        UIFactory.Place(closeButton, new Vector2(1f, 1f), new Vector2(-8f, -8f),
                        new Vector2(48f, 48f));
        SetLabelColor(closeButton, Color.clear);

        // ---- Runtime components + wiring --------------------------------------
        var controllerObj = new GameObject("AlmanacController");
        controllerObj.transform.SetParent(root.transform, false);
        var controller = controllerObj.AddComponent<AlmanacController>();
        var chatController = controllerObj.AddComponent<ChatController>();

        SceneBuilderUtil.Wire(manager, "controller", controller);

        SceneBuilderUtil.Wire(controller, "bookRoot",             backdrop.gameObject);
        SceneBuilderUtil.Wire(controller, "heritageTabButton",    heritageTab);
        SceneBuilderUtil.Wire(controller, "codingTabButton",      codingTab);
        SceneBuilderUtil.Wire(controller, "oracleTabButton",      oracleTab);
        SceneBuilderUtil.Wire(controller, "sidebarContent",       sidebarContent);
        SceneBuilderUtil.Wire(controller, "sidebarEntryTemplate", sidebarEntryTemplate);
        SceneBuilderUtil.Wire(controller, "contentTitle",         contentTitle);
        SceneBuilderUtil.Wire(controller, "contentBody",          contentBody);
        SceneBuilderUtil.Wire(controller, "entryArt",             entryArt);
        SceneBuilderUtil.Wire(controller, "entryArtLabel",        entryArtLabel);
        SceneBuilderUtil.WireArray(controller, "landmarkSprites", new UnityEngine.Object[]
        {
            LugarithmUiSkin.JournalPart("landmark_tutorial_jaro"), LugarithmUiSkin.JournalPart("landmark_iloilo_molo"),
            LugarithmUiSkin.JournalPart("landmark_oton"), LugarithmUiSkin.JournalPart("landmark_tigbauan"),
            LugarithmUiSkin.JournalPart("landmark_miagao"), LugarithmUiSkin.JournalPart("landmark_san_joaquin")
        });
        string[] conceptIcons = { "commands", "sensing", "sequencing", "variables", "operators", "conditionals", "loops", "lists", "functions", "nested_conditions", "comments", "indentation", "booleans", "stopping_loops", "endless_driving", "command_catalog", "errors", "autopilot" };
        var iconAssets = new UnityEngine.Object[conceptIcons.Length];
        for (int i = 0; i < conceptIcons.Length; i++) iconAssets[i] = LugarithmUiSkin.JournalPart("coding_icon_" + conceptIcons[i]);
        SceneBuilderUtil.WireArray(controller, "codingIconSprites", iconAssets);
        SceneBuilderUtil.Wire(controller, "detailPane",           detailPane.gameObject);
        SceneBuilderUtil.Wire(controller, "oraclePane",           oraclePane.gameObject);
        SceneBuilderUtil.Wire(controller, "chatController",       chatController);
        SceneBuilderUtil.Wire(controller, "closeButton",          closeButton);
        SceneBuilderUtil.Wire(controller, "previousButton",       previousButton);
        SceneBuilderUtil.Wire(controller, "nextButton",           nextButton);
        SceneBuilderUtil.Wire(controller, "pageIndicator",        pageIndicator);

        SceneBuilderUtil.Wire(chatController, "chatContent",    chatContent);
        SceneBuilderUtil.Wire(chatController, "bubbleTemplate", bubbleTemplate);
        SceneBuilderUtil.Wire(chatController, "chatInput",      chatInput);
        SceneBuilderUtil.Wire(chatController, "sendButton",     sendButton);
        SceneBuilderUtil.Wire(chatController, "clearButton",    clearChatButton);
        SceneBuilderUtil.Wire(chatController, "playerBubbleSprite", LugarithmUiSkin.JournalPlayerMessage);
        SceneBuilderUtil.Wire(chatController, "oracleBubbleSprite", LugarithmUiSkin.JournalOracleMessage);
        SceneBuilderUtil.WireArray(chatController, "quickTopicButtons", topicButtons);
        SceneBuilderUtil.WireStringArray(chatController, "quickTopicPrompts", topicPrompts);

        UIFactory.ApplyBlueprintSkin(backdrop);
        backdrop.gameObject.SetActive(false);
        return manager;
    }

    // -------------------------------------------------------------------------

    static void SetLabelColor(Button button, Color color)
    {
        if (button == null) return;
        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.color = color;
    }

    static RectTransform AddSprite(Transform parent, string name, Sprite sprite,
                                   Vector2 anchor, Vector2 position, Vector2 size)
    {
        var rt = UIFactory.CreateRect(parent, name, anchor, anchor);
        UIFactory.Place(rt, anchor, position, size);
        var image = rt.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return rt;
    }

    static void ClearScrollChrome(ScrollRect scroll)
    {
        if (scroll == null) return;
        foreach (Image image in scroll.GetComponentsInChildren<Image>(true))
            image.color = Color.clear;
    }

    static Button CreateJournalNavButton(Transform parent, string name, string prefix, Vector2 position)
    {
        Button button = UIFactory.CreateButton(parent, name, "", new Vector2(56f, 56f), 1f);
        UIFactory.Place(button, new Vector2(position.x > 0f ? 0f : 1f, 0f), position, new Vector2(56f, 56f));
        button.image.sprite = LugarithmUiSkin.JournalPart(prefix + "_normal");
        button.image.type = Image.Type.Simple;
        button.image.color = Color.white;
        var state = button.spriteState;
        state.highlightedSprite = LugarithmUiSkin.JournalPart(prefix + "_highlighted");
        state.pressedSprite = LugarithmUiSkin.JournalPart(prefix + "_pressed");
        state.disabledSprite = LugarithmUiSkin.JournalPart(prefix + "_disabled");
        button.spriteState = state;
        button.transition = Selectable.Transition.SpriteSwap;
        return button;
    }

    static TMP_InputField CreateSinglelineInput(Transform parent, string name,
                                                string placeholderText)
    {
        var rt = UIFactory.CreateRect(parent, name, Vector2.zero, Vector2.one);

        var image = rt.gameObject.AddComponent<Image>();
        image.sprite = UIFactory.BuiltinSprite("InputFieldBackground.psd");
        image.type = Image.Type.Sliced;
        image.color = UIFactory.PanelDarker;

        var input = rt.gameObject.AddComponent<TMP_InputField>();

        var textArea = UIFactory.CreateRect(rt, "Text Area", Vector2.zero, Vector2.one,
                                            new Vector2(10f, 8f), new Vector2(-10f, -8f));
        textArea.gameObject.AddComponent<RectMask2D>();

        var text = UIFactory.CreateText(textArea, "Text", "", 18f,
                                        Ink,
                                        TextAlignmentOptions.MidlineLeft);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        text.enableWordWrapping = false;
        text.raycastTarget = false;

        var placeholder = UIFactory.CreateText(textArea, "Placeholder", placeholderText,
                                               18f, InkDim,
                                               TextAlignmentOptions.MidlineLeft);
        placeholder.rectTransform.anchorMin = Vector2.zero;
        placeholder.rectTransform.anchorMax = Vector2.one;
        placeholder.rectTransform.offsetMin = Vector2.zero;
        placeholder.rectTransform.offsetMax = Vector2.zero;
        placeholder.fontStyle = FontStyles.Italic;
        placeholder.raycastTarget = false;

        input.textViewport = textArea;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.caretColor = UIFactory.Accent;
        input.customCaretColor = true;
        input.selectionColor = new Color(UIFactory.Accent.r, UIFactory.Accent.g,
                                         UIFactory.Accent.b, 0.35f);

        return input;
    }
}
