using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the end-of-leg overlay used by ManualDrive and AutomationDrive/CodeDrive:
/// a centered "LEVEL COMPLETE" congratulations card (Keep exploring / Finish &amp;
/// leave) plus a small persistent "Finish leg" button. The controller lives on an
/// always-active container so its Start() wires the buttons even though the panels
/// below begin hidden.
/// </summary>
public static class LegCompletionOverlayBuilder
{
    public static LegCompletionController Build(Transform parent)
    {
        // Always-active container — holds the controller so it initialises at load.
        var container = UIFactory.CreateRect(parent, "LegCompletion",
                                             Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // Toggled root holds both faces (the card and the small finish button).
        var root = UIFactory.CreateRect(container, "Root",
                                        Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // --- Centered congratulations card (dim backdrop + card) -----------------
        var completePanel = UIFactory.CreatePanel(root, "CompletePanel",
                                                  Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.72f));
        completePanel.offsetMin = Vector2.zero;
        completePanel.offsetMax = Vector2.zero;

        var card = UIFactory.CreatePanel(completePanel, "Card",
                                         new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), UIFactory.PanelDark);
        UIFactory.Place(card, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640f, 360f));

        var title = UIFactory.CreateText(card, "Title", "LEVEL COMPLETE", 34f, UIFactory.Accent);
        UIFactory.Place(title, new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(580f, 48f));

        var message = UIFactory.CreateText(card, "Message", "", 22f,
                                           UIFactory.TextBright, TextAlignmentOptions.Top);
        UIFactory.Place(message, new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(560f, 150f));
        message.enableWordWrapping = true;

        var explore = UIFactory.CreateButton(card, "ExploreButton", "Keep exploring",
                                             new Vector2(260f, 56f), 22f);
        UIFactory.Place(explore, new Vector2(0.5f, 0f), new Vector2(-140f, 36f), new Vector2(260f, 56f));
        explore.image.color = new Color(0.30f, 0.45f, 0.75f);

        var leave = UIFactory.CreateButton(card, "LeaveButton", "Finish & leave",
                                           new Vector2(260f, 56f), 22f);
        UIFactory.Place(leave, new Vector2(0.5f, 0f), new Vector2(140f, 36f), new Vector2(260f, 56f));
        leave.image.color = new Color(0.85f, 0.55f, 0.12f);

        // --- Small persistent "Finish leg" button (top-center) -------------------
        var finishRoot = UIFactory.CreateRect(root, "FinishButtonRoot",
                                              new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        UIFactory.Place(finishRoot, new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(260f, 52f));

        var bg = finishRoot.gameObject.AddComponent<Image>();
        bg.sprite = UIFactory.BuiltinSprite("UISprite.psd");
        bg.type   = Image.Type.Sliced;
        bg.color  = new Color(0.12f, 0.16f, 0.22f, 0.92f);

        var finishBtn = UIFactory.CreateButton(finishRoot, "FinishButton",
                                               "Finish leg", new Vector2(240f, 44f), 22f);
        UIFactory.Place(finishBtn, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240f, 44f));
        finishBtn.image.color = new Color(0.85f, 0.55f, 0.12f);

        root.gameObject.SetActive(false);

        var ctrl = container.gameObject.AddComponent<LegCompletionController>();
        SceneBuilderUtil.Wire(ctrl, "root",             root.gameObject);
        SceneBuilderUtil.Wire(ctrl, "completePanel",    completePanel.gameObject);
        SceneBuilderUtil.Wire(ctrl, "titleLabel",       title);
        SceneBuilderUtil.Wire(ctrl, "messageLabel",     message);
        SceneBuilderUtil.Wire(ctrl, "exploreButton",    explore);
        SceneBuilderUtil.Wire(ctrl, "leaveButton",      leave);
        SceneBuilderUtil.Wire(ctrl, "finishButtonRoot", finishRoot.gameObject);
        SceneBuilderUtil.Wire(ctrl, "finishButton",     finishBtn);

        return ctrl;
    }
}
