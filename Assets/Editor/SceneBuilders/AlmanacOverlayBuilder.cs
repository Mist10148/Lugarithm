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
                                               UIFactory.Accent);
        UIFactory.Place(bookBorder, new Vector2(0.5f, 0.5f), Vector2.zero,
                        new Vector2(1696f, 870f));

        var bookPanel = UIFactory.CreatePanel(bookBorder, "BookPanel",
                                              new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                              UIFactory.PanelDark);
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
                                          new Vector2(145f, -125f), new Vector2(-780f, 18f));

        Button heritageTab = UIFactory.CreateButton(tabBar, "HeritageTab", "Heritage Pages",
                                                    new Vector2(210f, 104f), 20f);
        UIFactory.Place(heritageTab, new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(210f, 104f));
        SetLabelColor(heritageTab, UIFactory.Accent);

        Button codingTab = UIFactory.CreateButton(tabBar, "CodingTab", "Coding Reference",
                                                  new Vector2(210f, 104f), 20f);
        UIFactory.Place(codingTab, new Vector2(0f, 0.5f), new Vector2(226f, 0f), new Vector2(210f, 104f));
        SetLabelColor(codingTab, UIFactory.TextDim);

        Button oracleTab = UIFactory.CreateButton(tabBar, "OracleTab", "Oracle",
                                                  new Vector2(190f, 104f), 20f);
        UIFactory.Place(oracleTab, new Vector2(0f, 0.5f), new Vector2(444f, 0f), new Vector2(190f, 104f));
        SetLabelColor(oracleTab, UIFactory.TextDim);
        heritageTab.image.sprite = null;
        codingTab.image.sprite = null;
        oracleTab.image.sprite = null;
        heritageTab.image.color = Color.clear;
        codingTab.image.color = Color.clear;
        oracleTab.image.color = Color.clear;

        // ==== Detail pane (PvZ two-pane: thumbnail grid + entry detail) =======
        var detailPane = UIFactory.CreateRect(bookPanel, "DetailPane",
                                              new Vector2(0f, 0f), new Vector2(1f, 1f),
                                              new Vector2(110f, 80f), new Vector2(-110f, -115f));

        // Left: scrollable grid of entry thumbnails.
        ScrollRect sidebarScroll = UIFactory.CreateScrollView(detailPane, "EntryGrid",
                                                              new Vector2(0f, 0f), new Vector2(0.4f, 1f),
                                                              out RectTransform sidebarContent);
        ((RectTransform)sidebarScroll.transform).offsetMax = new Vector2(-4f, 0f);
        UIFactory.AddVerticalScrollbar(sidebarScroll);

        // Swap the default vertical list for a PvZ-style grid of cards.
        var defaultLayout = sidebarContent.GetComponent<VerticalLayoutGroup>();
        if (defaultLayout != null) Object.DestroyImmediate(defaultLayout);
        var grid = sidebarContent.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize        = new Vector2(150f, 80f);
        grid.spacing         = new Vector2(12f, 12f);
        grid.padding         = new RectOffset(12, 12, 12, 12);
        grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        grid.childAlignment  = TextAnchor.UpperCenter;

        // Thumbnail card template.
        Button sidebarEntryTemplate = UIFactory.CreateButton(sidebarContent,
                                                             "SidebarEntryTemplate",
                                                             "Town", new Vector2(150f, 80f), 18f);
        sidebarEntryTemplate.image.color = UIFactory.PanelDark;
        var entryTemplLabel = sidebarEntryTemplate.GetComponentInChildren<TMP_Text>();
        if (entryTemplLabel != null)
        {
            entryTemplLabel.alignment = TextAlignmentOptions.Center;
            entryTemplLabel.textWrappingMode = TextWrappingModes.Normal;
        }
        sidebarEntryTemplate.gameObject.SetActive(false);

        // Right: entry detail — art banner + title + body.
        var detailArea = UIFactory.CreateRect(detailPane, "DetailArea",
                                              new Vector2(0.4f, 0f), new Vector2(1f, 1f),
                                              new Vector2(12f, 0f), new Vector2(0f, 0f));

        var entryArtFrame = UIFactory.CreatePanel(detailArea, "EntryArt",
                                                  new Vector2(0f, 1f), new Vector2(1f, 1f),
                                                  new Color(0.30f, 0.34f, 0.42f, 1f));
        entryArtFrame.offsetMin = new Vector2(0f, -150f);
        entryArtFrame.offsetMax = new Vector2(0f, -8f);
        var entryArt = entryArtFrame.GetComponent<Image>();
        var entryArtLabel = UIFactory.CreateText(entryArtFrame, "ArtInitials", "", 56f,
                                                 UIFactory.TextBright, TextAlignmentOptions.Center);
        entryArtLabel.rectTransform.offsetMin = Vector2.zero;
        entryArtLabel.rectTransform.offsetMax = Vector2.zero;
        entryArtLabel.fontStyle = FontStyles.Bold;

        var contentTitle = UIFactory.CreateText(detailArea, "ContentTitle", "", 32f,
                                                UIFactory.Accent, TextAlignmentOptions.TopLeft);
        contentTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
        contentTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
        contentTitle.rectTransform.offsetMin = new Vector2(0f, -210f);
        contentTitle.rectTransform.offsetMax = new Vector2(0f, -158f);

        ScrollRect contentScroll = UIFactory.CreateScrollView(detailArea, "ContentScroll",
                                                              new Vector2(0f, 0f), new Vector2(1f, 1f),
                                                              out RectTransform contentBodyRect);
        ((RectTransform)contentScroll.transform).offsetMax = new Vector2(0f, -214f);
        UIFactory.AddVerticalScrollbar(contentScroll);

        var contentBody = UIFactory.CreateText(contentBodyRect, "Body", "", 20f,
                                               UIFactory.TextBright, TextAlignmentOptions.TopLeft);
        contentBody.textWrappingMode = TextWrappingModes.Normal;
        // Size the body to its actual text so long reference entries scroll instead of
        // clipping at a fixed height (the scroll content fitter grows to match).
        var bodyFitter = contentBody.gameObject.AddComponent<ContentSizeFitter>();
        bodyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ==== Oracle pane (its own tab) =======================================
        var oraclePane = UIFactory.CreateRect(bookPanel, "OraclePane",
                                              new Vector2(0f, 0f), new Vector2(1f, 1f),
                                              new Vector2(110f, 80f), new Vector2(-110f, -115f));

        var oracleFlavour = UIFactory.CreateText(oraclePane, "OracleFlavour",
                                                 "Ask the Oracle about any town or coding concept.",
                                                 18f, UIFactory.TextDim, TextAlignmentOptions.TopLeft);
        oracleFlavour.rectTransform.anchorMin = new Vector2(0f, 1f);
        oracleFlavour.rectTransform.anchorMax = new Vector2(1f, 1f);
        oracleFlavour.rectTransform.offsetMin = new Vector2(8f, -34f);
        oracleFlavour.rectTransform.offsetMax = new Vector2(-104f, -4f);

        // Clear button (top-right) — wipes the transcript on demand.
        Button clearChatButton = UIFactory.CreateButton(oraclePane, "ClearChatButton", "Clear",
                                                        new Vector2(90f, 30f), 16f);
        clearChatButton.image.color = UIFactory.PanelDarker;
        UIFactory.Place(clearChatButton, new Vector2(1f, 1f), new Vector2(-6f, -4f), new Vector2(90f, 30f));
        SetLabelColor(clearChatButton, UIFactory.TextDim);

        ScrollRect chatScroll = UIFactory.CreateScrollView(oraclePane, "ChatScroll",
                                                           new Vector2(0f, 0f), new Vector2(1f, 1f),
                                                           out RectTransform chatContent);
        var chatScrollRt = (RectTransform)chatScroll.transform;
        chatScrollRt.offsetMin = new Vector2(0f, 72f);
        chatScrollRt.offsetMax = new Vector2(0f, -40f);
        UIFactory.AddVerticalScrollbar(chatScroll, permanent: true);

        var bubbleTemplate = UIFactory.CreateText(chatContent, "BubbleTemplate", "", 18f,
                                                  UIFactory.TextBright, TextAlignmentOptions.TopLeft);
        bubbleTemplate.textWrappingMode = TextWrappingModes.Normal;
        var bubbleLe = bubbleTemplate.gameObject.AddComponent<LayoutElement>();
        bubbleLe.preferredHeight = 40f;
        bubbleLe.flexibleWidth = 1f;
        bubbleTemplate.gameObject.SetActive(false);

        var inputRow = UIFactory.CreatePanel(oraclePane, "InputRow",
                                             new Vector2(0f, 0f), new Vector2(1f, 0f),
                                             UIFactory.PanelDarker);
        inputRow.offsetMin = new Vector2(0f, 0f);
        inputRow.offsetMax = new Vector2(0f, 60f);

        TMP_InputField chatInput = CreateSinglelineInput(inputRow, "ChatInput", "Ask the Oracle…");
        var inputRt = chatInput.GetComponent<RectTransform>();
        inputRt.anchorMin = new Vector2(0f, 0f);
        inputRt.anchorMax = new Vector2(0.8f, 1f);
        inputRt.offsetMin = new Vector2(4f, 4f);
        inputRt.offsetMax = new Vector2(-4f, -4f);

        Button sendButton = UIFactory.CreateButton(inputRow, "SendButton", "Send",
                                                   new Vector2(0f, 52f), 22f);
        sendButton.image.color = UIFactory.Accent;
        var sendRt = sendButton.GetComponent<RectTransform>();
        sendRt.anchorMin = new Vector2(0.82f, 0f);
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
        closeButton.image.color = Color.clear;
        UIFactory.Place(closeButton, new Vector2(1f, 1f), new Vector2(-8f, -8f),
                        new Vector2(48f, 48f));
        SetLabelColor(closeButton, UIFactory.TextDim);

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
        SceneBuilderUtil.Wire(controller, "detailPane",           detailPane.gameObject);
        SceneBuilderUtil.Wire(controller, "oraclePane",           oraclePane.gameObject);
        SceneBuilderUtil.Wire(controller, "chatController",       chatController);
        SceneBuilderUtil.Wire(controller, "closeButton",          closeButton);

        SceneBuilderUtil.Wire(chatController, "chatContent",    chatContent);
        SceneBuilderUtil.Wire(chatController, "bubbleTemplate", bubbleTemplate);
        SceneBuilderUtil.Wire(chatController, "chatInput",      chatInput);
        SceneBuilderUtil.Wire(chatController, "sendButton",     sendButton);
        SceneBuilderUtil.Wire(chatController, "clearButton",    clearChatButton);

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
                                        UIFactory.TextBright,
                                        TextAlignmentOptions.TopLeft);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        text.enableWordWrapping = false;
        text.raycastTarget = false;

        var placeholder = UIFactory.CreateText(textArea, "Placeholder", placeholderText,
                                               18f, UIFactory.TextDim,
                                               TextAlignmentOptions.TopLeft);
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
