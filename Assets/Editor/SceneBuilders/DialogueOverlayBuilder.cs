using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the branching dialogue overlay (dialogue bar + choice list + reveal/cutscene panel)
/// for ManualDrive, AutomationDrive, and CodeDrive scenes.
/// </summary>
public static class DialogueOverlayBuilder
{
    public static DialogueController BuildDriveDialogue(Transform parent)
    {
        // --- Reveal / cutscene panel (full screen, hidden) ----------------------

        var revealRoot = UIFactory.CreatePanel(parent, "RevealRoot",
                                               Vector2.zero, Vector2.one,
                                               new Color(0.04f, 0.05f, 0.07f, 0.92f));
        revealRoot.gameObject.SetActive(false);

        var revealCard = UIFactory.CreatePanel(revealRoot, "JournalCard",
                                               new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                               UIFactory.PanelDark);
        UIFactory.Place(revealCard, new Vector2(0.5f, 0.5f), new Vector2(0f, 24f), new Vector2(720f, 640f));

        var revealTitle = UIFactory.CreateText(revealCard, "Title", "Recovered Page", 32f, UIFactory.Accent);
        UIFactory.Place(revealTitle, new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(660f, 44f));

        var revealScroll = UIFactory.CreateScrollView(revealCard, "Scroll",
                                                      new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                                      out RectTransform revealContent);
        UIFactory.Place((RectTransform)revealScroll.transform, new Vector2(0.5f, 0.5f),
                        new Vector2(0f, -20f), new Vector2(680f, 540f));

        var journalCard = UIFactory.CreateText(revealContent, "JournalCardText", "", 22f,
                                               UIFactory.TextBright, TextAlignmentOptions.TopLeft);
        journalCard.textWrappingMode = TextWrappingModes.Normal;

        var revealBody = UIFactory.CreateText(revealRoot, "RevealBody", "", 24f,
                                              UIFactory.TextBright, TextAlignmentOptions.Top);
        UIFactory.Place(revealBody, new Vector2(0.5f, 0f), new Vector2(0f, 180f), new Vector2(1400f, 120f));

        // --- Choice list (HoYo: vertical rounded pills down the right side) -----

        var choiceRoot = UIFactory.CreateRect(parent, "ChoiceRoot",
                                              new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                                              new Vector2(-580f, -280f), new Vector2(-40f, 280f));
        UIFactory.AddVerticalLayout(choiceRoot, 12f, align: TextAnchor.MiddleRight);

        var choiceTemplate = UIFactory.CreateButton(choiceRoot, "ChoiceTemplate", "Choice", new Vector2(520f, 56f), 22f);
        choiceTemplate.image.color = new Color(0.12f, 0.14f, 0.20f, 0.96f);
        var choiceTemplLabel = choiceTemplate.GetComponentInChildren<TMP_Text>();
        if (choiceTemplLabel != null)
        {
            choiceTemplLabel.alignment = TextAlignmentOptions.MidlineLeft;
            choiceTemplLabel.rectTransform.offsetMin = new Vector2(22f, 0f);
        }
        // Accent left edge so each option reads like a HoYo selectable bubble.
        var choiceEdge = UIFactory.CreatePanel(choiceTemplate.transform, "Edge",
                                               new Vector2(0f, 0f), new Vector2(0f, 1f), UIFactory.Accent);
        choiceEdge.offsetMin = new Vector2(0f, 8f);
        choiceEdge.offsetMax = new Vector2(7f, -8f);
        choiceEdge.GetComponent<Image>().raycastTarget = false;
        choiceTemplate.gameObject.SetActive(false);

        // --- Dialogue box (HoYo: wide, bottom-center, accented name plate) ------

        var root = UIFactory.CreatePanel(parent, "DialogueBar",
                                         new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                                         new Color(0.06f, 0.07f, 0.10f, 0.92f));
        UIFactory.Place(root, new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(1500f, 220f));

        // Accent bar across the top edge of the box.
        var accentBar = UIFactory.CreatePanel(root, "AccentBar",
                                              new Vector2(0f, 1f), new Vector2(1f, 1f), UIFactory.Accent);
        accentBar.offsetMin = new Vector2(0f, -4f);
        accentBar.offsetMax = new Vector2(0f, 0f);
        accentBar.GetComponent<Image>().raycastTarget = false;

        // Speaker portrait placeholder (tinted plate + initials) at the top-left.
        var portraitFrame = UIFactory.CreatePanel(root, "PortraitFrame",
                                                  new Vector2(0f, 1f), new Vector2(0f, 1f),
                                                  new Color(0.40f, 0.45f, 0.55f, 1f));
        UIFactory.Place(portraitFrame, new Vector2(0f, 1f), new Vector2(20f, -16f), new Vector2(76f, 76f));
        var portraitImg = portraitFrame.GetComponent<Image>();
        var speakerInitials = UIFactory.CreateText(portraitFrame, "Initials", "", 30f,
                                                   UIFactory.TextBright, TextAlignmentOptions.Center);
        speakerInitials.rectTransform.offsetMin = Vector2.zero;
        speakerInitials.rectTransform.offsetMax = Vector2.zero;
        speakerInitials.fontStyle = FontStyles.Bold;

        // Speaker name (accent) to the right of the portrait, with an underline.
        var speakerLabel = UIFactory.CreateText(root, "SpeakerLabel", "", 24f, UIFactory.Accent,
                                                TextAlignmentOptions.MidlineLeft);
        UIFactory.Place(speakerLabel, new Vector2(0f, 1f), new Vector2(108f, -20f), new Vector2(420f, 36f));
        speakerLabel.fontStyle = FontStyles.Bold;

        var nameUnderline = UIFactory.CreatePanel(root, "NameUnderline",
                                                  new Vector2(0f, 1f), new Vector2(0f, 1f), UIFactory.Accent);
        UIFactory.Place(nameUnderline, new Vector2(0f, 1f), new Vector2(108f, -58f), new Vector2(280f, 3f));
        nameUnderline.GetComponent<Image>().raycastTarget = false;

        // Body text fills the box below the name, right of the portrait.
        var bodyLabel = UIFactory.CreateText(root, "BodyLabel", "", 24f,
                                             UIFactory.TextBright, TextAlignmentOptions.TopLeft);
        bodyLabel.rectTransform.anchorMin = new Vector2(0f, 0f);
        bodyLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
        bodyLabel.rectTransform.offsetMin = new Vector2(108f, 20f);
        bodyLabel.rectTransform.offsetMax = new Vector2(-40f, -70f);
        bodyLabel.enableWordWrapping = true;

        var continueIndicator = UIFactory.CreateText(root, "ContinueIndicator", "▼", 28f, UIFactory.Accent);
        UIFactory.Place(continueIndicator, new Vector2(1f, 0f), new Vector2(-22f, 16f), new Vector2(40f, 40f));

        // --- Skip (top-right) + Next (bottom-right) -----------------------------

        var skipBtn = UIFactory.CreateButton(root, "SkipButton", "Skip", new Vector2(120f, 40f), 20f);
        UIFactory.Place(skipBtn, new Vector2(1f, 1f), new Vector2(-16f, -12f), new Vector2(120f, 40f));

        var nextBtn = UIFactory.CreateButton(root, "NextButton", "Next ▶", new Vector2(120f, 40f), 20f);
        UIFactory.Place(nextBtn, new Vector2(1f, 0f), new Vector2(-150f, 16f), new Vector2(120f, 40f));

        // DialogBox drives the typewriter reveal on the bar.
        var dialogBox = root.gameObject.AddComponent<DialogBox>();
        SceneBuilderUtil.Wire(dialogBox, "root",             root.gameObject);
        SceneBuilderUtil.Wire(dialogBox, "speakerLabel",     speakerLabel);
        SceneBuilderUtil.Wire(dialogBox, "bodyLabel",        bodyLabel);
        SceneBuilderUtil.Wire(dialogBox, "continueIndicator", continueIndicator);

        // --- Dialogue controller ------------------------------------------------

        var controllerGo = new GameObject("DialogueController");
        controllerGo.transform.SetParent(parent, false);
        var controller = controllerGo.AddComponent<DialogueController>();

        SceneBuilderUtil.Wire(controller, "root",                  root.gameObject);
        SceneBuilderUtil.Wire(controller, "dialogBox",             dialogBox);
        SceneBuilderUtil.Wire(controller, "continueIndicator",     continueIndicator.gameObject);
        SceneBuilderUtil.Wire(controller, "nextButton",            nextBtn);
        SceneBuilderUtil.Wire(controller, "skipButton",            skipBtn);
        SceneBuilderUtil.Wire(controller, "choiceContainer",       choiceRoot);
        SceneBuilderUtil.Wire(controller, "choiceButtonTemplate",  choiceTemplate);
        SceneBuilderUtil.Wire(controller, "revealRoot",            revealRoot.gameObject);
        SceneBuilderUtil.Wire(controller, "revealBody",            revealBody);
        SceneBuilderUtil.Wire(controller, "journalCard",           journalCard);
        SceneBuilderUtil.Wire(controller, "speakerPortrait",       portraitImg);
        SceneBuilderUtil.Wire(controller, "speakerInitials",       speakerInitials);

        // Hide by default until a conversation starts.
        root.gameObject.SetActive(false);
        choiceRoot.gameObject.SetActive(false);
        revealRoot.gameObject.SetActive(false);

        return controller;
    }
}
