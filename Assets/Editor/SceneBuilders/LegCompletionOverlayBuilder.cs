using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the persistent "Finish leg" button overlay used by both ManualDrive
/// and AutomationDrive/CodeDrive scenes.
/// </summary>
public static class LegCompletionOverlayBuilder
{
    public static LegCompletionController Build(Transform parent)
    {
        var root = UIFactory.CreateRect(parent, "LegCompletionRoot",
                                        new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        UIFactory.Place(root, new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(260f, 52f));
        root.gameObject.SetActive(false);

        var bg = root.gameObject.AddComponent<Image>();
        bg.sprite = UIFactory.BuiltinSprite("UISprite.psd");
        bg.type   = Image.Type.Sliced;
        bg.color  = new Color(0.12f, 0.16f, 0.22f, 0.92f);

        var finishBtn = UIFactory.CreateButton(root, "FinishButton",
                                               "Finish leg", new Vector2(240f, 44f), 22f);
        UIFactory.Place(finishBtn, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240f, 44f));
        finishBtn.image.color = new Color(0.85f, 0.55f, 0.12f);

        var label = finishBtn.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = "Finish leg";

        var ctrl = root.gameObject.AddComponent<LegCompletionController>();
        SceneBuilderUtil.Wire(ctrl, "root", root.gameObject);
        SceneBuilderUtil.Wire(ctrl, "finishButton", finishBtn);

        return ctrl;
    }
}
