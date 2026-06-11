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

        // ---- Left page --------------------------------------------------------
        var leftPage = UIFactory.CreateRect(bookPanel, "LeftPage",
                                            new Vector2(0f, 0f), new Vector2(0.5f, 1f),
                                            new Vector2(8f, 8f), new Vector2(-4f, -8f));

        // Tab bar
        var tabBar = UIFactory.CreateRect(leftPage, "TabBar",
                                          new Vector2(0f, 1f), new Vector2(1f, 1f),
                                          new Vector2(0f, -48f), new Vector2(0f, 0f));

        Button heritageTab = UIFactory.CreateButton(tabBar, "HeritageTab", "Heritage Pages",
                                                    new Vector2(180f, 40f), 20f);
        UIFactory.Place(heritageTab, new Vector2(0f, 1f), new Vector2(8f, -6f),
                        new Vector2(180f, 40f));
        SetLabelColor(heritageTab, UIFactory.Accent);

        Button codingTab = UIFactory.CreateButton(tabBar, "CodingTab", "Coding Reference",
                                                  new Vector2(180f, 40f), 20f);
        UIFactory.Place(codingTab, new Vector2(0f, 1f), new Vector2(196f, -6f),
                        new Vector2(180f, 40f));
        SetLabelColor(codingTab, UIFactory.TextDim);

        // Sidebar scroll
        ScrollRect sidebarScroll = UIFactory.CreateScrollView(leftPage, "SidebarScroll",
                                                              new Vector2(0f, 0f),
                                                              new Vector2(0.3f, 1f),
                                                              out RectTransform sidebarContent);
        var sidebarScrollRt = (RectTransform)sidebarScroll.transform;
        sidebarScrollRt.offsetMin = new Vector2(0f, 0f);
        sidebarScrollRt.offsetMax = new Vector2(0f, -52f);

        // Sidebar entry template
        Button sidebarEntryTemplate = UIFactory.CreateButton(sidebarContent,
                                                             "SidebarEntryTemplate",
                                                             "Town", new Vector2(0f, 52f), 22f);
        var entryLe = sidebarEntryTemplate.gameObject.AddComponent<LayoutElement>();
        entryLe.preferredHeight = 52f;
        entryLe.flexibleWidth = 1f;
        sidebarEntryTemplate.gameObject.SetActive(false);

        // Content area (right 70% of left page)
        var contentArea = UIFactory.CreateRect(leftPage, "ContentArea",
                                               new Vector2(0.3f, 0f), new Vector2(1f, 1f),
                                               new Vector2(8f, 0f), new Vector2(-8f, -52f));

        // Content title
        var contentTitle = UIFactory.CreateText(contentArea, "ContentTitle", "",
                                                36f, UIFactory.Accent,
                                                TextAlignmentOptions.TopLeft);
        contentTitle.rectTransform.offsetMin = new Vector2(0f, 0f);
        contentTitle.rectTransform.offsetMax = new Vector2(0f, -64f);

        // Content scroll
        ScrollRect contentScroll = UIFactory.CreateScrollView(contentArea, "ContentScroll",
                                                              new Vector2(0f, 0f),
                                                              new Vector2(1f, 1f),
                                                              out RectTransform contentBodyRect);
        var contentScrollRt = (RectTransform)contentScroll.transform;
        contentScrollRt.offsetMin = new Vector2(0f, 0f);
        contentScrollRt.offsetMax = new Vector2(0f, -72f);

        var contentBody = UIFactory.CreateText(contentBodyRect, "Body", "",
                                               20f, UIFactory.TextBright,
                                               TextAlignmentOptions.TopLeft);
        contentBody.textWrappingMode = TextWrappingModes.Normal;
        var bodyLe = contentBody.gameObject.AddComponent<LayoutElement>();
        bodyLe.preferredHeight = 1000f;
        bodyLe.flexibleWidth = 1f;

        // ---- Right page -------------------------------------------------------
        var rightPage = UIFactory.CreateRect(bookPanel, "RightPage",
                                             new Vector2(0.5f, 0f), new Vector2(1f, 1f),
                                             new Vector2(4f, 4f), new Vector2(-8f, -8f));

        // Oracle header
        var oracleHeader = UIFactory.CreateText(rightPage, "OracleHeader", "Oracle",
                                                32f, UIFactory.Accent,
                                                TextAlignmentOptions.TopLeft);
        oracleHeader.rectTransform.offsetMin = new Vector2(0f, 0f);
        oracleHeader.rectTransform.offsetMax = new Vector2(0f, -40f);

        // Oracle flavour line
        var oracleFlavour = UIFactory.CreateText(rightPage, "OracleFlavour",
                                                 "Ask about any town or coding concept.",
                                                 18f, UIFactory.TextDim,
                                                 TextAlignmentOptions.TopLeft);
        oracleFlavour.rectTransform.offsetMin = new Vector2(0f, -44f);
        oracleFlavour.rectTransform.offsetMax = new Vector2(0f, -74f);

        // Chat scroll
        ScrollRect chatScroll = UIFactory.CreateScrollView(rightPage, "ChatScroll",
                                                           new Vector2(0f, 0f),
                                                           new Vector2(1f, 1f),
                                                           out RectTransform chatContent);
        var chatScrollRt = (RectTransform)chatScroll.transform;
        chatScrollRt.offsetMin = new Vector2(0f, 72f);
        chatScrollRt.offsetMax = new Vector2(0f, -102f);

        // Chat bubble template
        var bubbleTemplate = UIFactory.CreateText(chatContent, "BubbleTemplate", "",
                                                  18f, UIFactory.TextBright,
                                                  TextAlignmentOptions.TopLeft);
        bubbleTemplate.textWrappingMode = TextWrappingModes.Normal;
        var bubbleLe = bubbleTemplate.gameObject.AddComponent<LayoutElement>();
        bubbleLe.preferredHeight = 40f;
        bubbleLe.flexibleWidth = 1f;
        bubbleTemplate.gameObject.SetActive(false);

        // Input row
        var inputRow = UIFactory.CreatePanel(rightPage, "InputRow",
                                             new Vector2(0f, 0f), new Vector2(1f, 0f),
                                             UIFactory.PanelDarker);
        inputRow.offsetMin = new Vector2(0f, 0f);
        inputRow.offsetMax = new Vector2(0f, 60f);

        TMP_InputField chatInput = CreateSinglelineInput(inputRow, "ChatInput",
                                                         "Ask the Oracle…");
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

        // ---- Divider + close --------------------------------------------------
        var divider = UIFactory.CreatePanel(bookPanel, "Divider",
                                            new Vector2(0.5f, 0.05f),
                                            new Vector2(0.5f, 0.95f),
                                            UIFactory.Accent);
        divider.offsetMin = new Vector2(-1f, 0f);
        divider.offsetMax = new Vector2(1f, 0f);

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

        SceneBuilderUtil.Wire(controller, "bookRoot",               backdrop.gameObject);
        SceneBuilderUtil.Wire(controller, "heritageTabButton",      heritageTab);
        SceneBuilderUtil.Wire(controller, "codingTabButton",        codingTab);
        SceneBuilderUtil.Wire(controller, "sidebarContent",         sidebarContent);
        SceneBuilderUtil.Wire(controller, "sidebarEntryTemplate",   sidebarEntryTemplate);
        SceneBuilderUtil.Wire(controller, "contentTitle",           contentTitle);
        SceneBuilderUtil.Wire(controller, "contentBody",            contentBody);
        SceneBuilderUtil.Wire(controller, "chatController",         chatController);
        SceneBuilderUtil.Wire(controller, "closeButton",            closeButton);

        SceneBuilderUtil.Wire(chatController, "chatContent",    chatContent);
        SceneBuilderUtil.Wire(chatController, "bubbleTemplate", bubbleTemplate);
        SceneBuilderUtil.Wire(chatController, "chatInput",      chatInput);
        SceneBuilderUtil.Wire(chatController, "sendButton",     sendButton);

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
