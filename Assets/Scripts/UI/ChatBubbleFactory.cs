using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds Messenger-style chat bubbles at runtime, shared by the Oracle chat and the
/// in-editor vibe-coding chat. Each message becomes its own row in a vertical layout —
/// the player's on the right, the assistant's on the left — with a coloured bubble that
/// hugs short text and wraps long text at a fraction of the viewport width.
/// </summary>
public static class ChatBubbleFactory
{
    public const float WidthFraction = 0.7f;
    public const float PadX = 14f, PadY = 9f;

    /// <summary>Creates a bubble row under <paramref name="content"/> and returns the
    /// inner text component (so callers can stream updates into it). The owning row
    /// GameObject is returned via <paramref name="row"/> for later destruction.</summary>
    public static TMP_Text Add(RectTransform content, TMP_Text template, string text, bool rightAligned,
                               Color bubbleColor, Color textColor, out GameObject row)
        => Add(content, template, text, rightAligned, bubbleColor, textColor, null, out row);

    public static TMP_Text Add(RectTransform content, TMP_Text template, string text, bool rightAligned,
                               Color bubbleColor, Color textColor, Sprite frameSprite, out GameObject row)
    {
        row = null;
        if (content == null || template == null) return null;

        // Row: full width; aligns its single bubble child left or right.
        row = new GameObject(rightAligned ? "PlayerRow" : "AssistantRow", typeof(RectTransform));
        var rowRt = (RectTransform)row.transform;
        rowRt.SetParent(content, false);
        var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.childControlWidth      = true;
        rowLayout.childControlHeight     = true;
        rowLayout.childForceExpandWidth  = false;
        rowLayout.childForceExpandHeight = false;
        rowLayout.childAlignment = rightAligned ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
        var rowFitter = row.AddComponent<ContentSizeFitter>();
        rowFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        rowFitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        // Bubble: a coloured panel that hugs the text up to a max width.
        var bubble = new GameObject("Bubble", typeof(RectTransform));
        var bubbleRt = (RectTransform)bubble.transform;
        bubbleRt.SetParent(rowRt, false);
        var bubbleImg = bubble.AddComponent<Image>();
        bubbleImg.sprite = frameSprite;
        bubbleImg.type = frameSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        bubbleImg.color = bubbleColor;
        var bubbleLayout = bubble.AddComponent<HorizontalLayoutGroup>();
        bubbleLayout.padding = new RectOffset((int)PadX, (int)PadX, (int)PadY, (int)PadY);
        bubbleLayout.childControlWidth      = true;
        bubbleLayout.childControlHeight     = true;
        bubbleLayout.childForceExpandWidth  = false;
        bubbleLayout.childForceExpandHeight = false;
        var bubbleFitter = bubble.AddComponent<ContentSizeFitter>();
        bubbleFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        bubbleFitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        // Inner text reuses the template so it keeps the builder's font/size.
        var label = Object.Instantiate(template, bubbleRt);
        label.gameObject.SetActive(true);
        label.alignment        = TextAlignmentOptions.TopLeft;
        label.color            = textColor;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget    = false;
        var le = label.GetComponent<LayoutElement>();
        if (le == null) le = label.gameObject.AddComponent<LayoutElement>();
        le.flexibleWidth = 0f;

        SetText(label, text, content);
        return label;
    }

    /// <summary>Updates a bubble's text and re-sizes it.</summary>
    public static void SetText(TMP_Text label, string text, RectTransform content)
    {
        if (label == null) return;
        label.text = text;

        var le = label.GetComponent<LayoutElement>();
        if (le != null)
        {
            float viewport = content != null && content.rect.width > 1f ? content.rect.width : 640f;
            float maxText = Mathf.Max(160f, viewport * WidthFraction) - PadX * 2f;
            Vector2 pref = label.GetPreferredValues(text ?? "", maxText, 0f);
            le.preferredWidth  = Mathf.Min(pref.x, maxText);
            le.preferredHeight = pref.y;
        }

        if (content != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }

    /// <summary>Configures a scroll content's vertical layout for stacked message rows.</summary>
    public static void PrepareContent(RectTransform content)
    {
        if (content == null) return;
        var vlg = content.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) return;
        vlg.childControlHeight     = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth      = true;
        vlg.childForceExpandWidth  = true;
        vlg.spacing                = 10f;
    }

    /// <summary>Snaps a scroll view (found from the content's parents) to the latest row.</summary>
    public static void ScrollToBottom(RectTransform content)
    {
        if (content == null) return;
        var scroll = content.GetComponentInParent<ScrollRect>();
        if (scroll != null)
        {
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 0f;
        }
    }
}
