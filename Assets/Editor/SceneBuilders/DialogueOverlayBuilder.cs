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

        // --- Choice list (vertical, above the dialogue bar) ---------------------

        var choiceRoot = UIFactory.CreateRect(parent, "ChoiceRoot",
                                              new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                                              new Vector2(-420f, 190f), new Vector2(420f, 500f));
        UIFactory.AddVerticalLayout(choiceRoot, 8f, align: TextAnchor.LowerCenter);

        var choiceTemplate = UIFactory.CreateButton(choiceRoot, "ChoiceTemplate", "Choice", new Vector2(520f, 54f), 22f);
        choiceTemplate.gameObject.SetActive(false);

        // --- Dialogue bar (bottom center) ---------------------------------------

        var root = UIFactory.CreatePanel(parent, "DialogueBar",
                                         new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                                         UIFactory.PanelDark);
        UIFactory.Place(root, new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(1200f, 170f));

        var speakerPlate = UIFactory.CreatePanel(root, "SpeakerPlate",
                                                 new Vector2(0f, 1f), new Vector2(0f, 1f),
                                                 UIFactory.PanelDarker);
        UIFactory.Place(speakerPlate, new Vector2(0f, 1f), new Vector2(12f, -12f), new Vector2(220f, 44f));

        var speakerLabel = UIFactory.CreateText(speakerPlate, "SpeakerLabel", "", 22f, UIFactory.Accent);
        speakerLabel.rectTransform.offsetMin = new Vector2(12f, 0f);
        speakerLabel.rectTransform.offsetMax = new Vector2(-12f, 0f);

        var bodyLabel = UIFactory.CreateText(root, "BodyLabel", "", 24f,
                                             UIFactory.TextBright, TextAlignmentOptions.TopLeft);
        UIFactory.Place(bodyLabel, new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(1160f, 110f));
        bodyLabel.enableWordWrapping = true;

        var continueIndicator = UIFactory.CreateText(root, "ContinueIndicator", "▼", 28f, UIFactory.Accent);
        UIFactory.Place(continueIndicator, new Vector2(1f, 0f), new Vector2(-18f, 18f), new Vector2(40f, 40f));

        // --- Navigation buttons (bottom-right) ----------------------------------

        var nextBtn = UIFactory.CreateButton(root, "NextButton", "Next ▶", new Vector2(110f, 42f), 22f);
        UIFactory.Place(nextBtn, new Vector2(1f, 0f), new Vector2(-140f, 18f), new Vector2(110f, 42f));

        var skipBtn = UIFactory.CreateButton(root, "SkipButton", "Skip ⏭", new Vector2(110f, 42f), 22f);
        UIFactory.Place(skipBtn, new Vector2(1f, 0f), new Vector2(-262f, 18f), new Vector2(110f, 42f));

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

        // Hide by default until a conversation starts.
        root.gameObject.SetActive(false);
        choiceRoot.gameObject.SetActive(false);
        revealRoot.gameObject.SetActive(false);

        return controller;
    }
}
