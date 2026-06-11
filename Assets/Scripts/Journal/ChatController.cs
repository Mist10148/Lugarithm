using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Stub Oracle chat controller for the right-hand Almanac page.
/// Phase 5 will replace <see cref="GenerateOracleResponse(string)"/> with a
/// real LLM call; for now it answers with a placeholder or a lock warning.
/// </summary>
public class ChatController : MonoBehaviour
{
    [SerializeField] private RectTransform chatContent;
    [SerializeField] private TMP_Text      bubbleTemplate;
    [SerializeField] private TMP_InputField chatInput;
    [SerializeField] private Button        sendButton;

    // Local palette copy — runtime scripts must not reference editor UIFactory.
    static readonly Color TextBright = new Color(0.93f, 0.93f, 0.88f, 1f);
    static readonly Color TextDim    = new Color(0.62f, 0.64f, 0.66f, 1f);

    readonly List<TMP_Text> _bubbles = new List<TMP_Text>();
    bool _bound;

    // -------------------------------------------------------------------------

    void Start()
    {
        Bind();
    }

    // -------------------------------------------------------------------------

    void Bind()
    {
        if (_bound) return;
        _bound = true;

        if (sendButton != null)
            sendButton.onClick.AddListener(OnSend);

        if (chatInput != null)
            chatInput.onSubmit.AddListener(_ => OnSend());
    }

    void OnSend()
    {
        if (chatInput == null) return;

        string text = chatInput.text;
        if (string.IsNullOrWhiteSpace(text)) return;

        AddBubble(text.Trim(), player: true);
        string response = GenerateOracleResponse(text);
        AddBubble(response, player: false);

        chatInput.text = "";
        chatInput.ActivateInputField();
    }

    void AddBubble(string text, bool player)
    {
        if (bubbleTemplate == null || chatContent == null) return;

        var bubble = Instantiate(bubbleTemplate, chatContent);
        bubble.gameObject.SetActive(true);
        bubble.text = text;
        bubble.color = player ? TextBright : TextDim;
        bubble.alignment = player ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;

        var le = bubble.GetComponent<LayoutElement>();
        if (le == null) le = bubble.gameObject.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;

        _bubbles.Add(bubble);
        RebuildBubbleHeights();
        ScrollToBottom();
    }

    void RebuildBubbleHeights()
    {
        Canvas.ForceUpdateCanvases();
        foreach (var bubble in _bubbles)
        {
            if (bubble == null) continue;
            var le = bubble.GetComponent<LayoutElement>();
            if (le != null)
                le.preferredHeight = Mathf.Max(bubble.preferredHeight + 12f, 36f);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent);
    }

    void ScrollToBottom()
    {
        var scroll = chatContent.GetComponentInParent<ScrollRect>();
        if (scroll != null)
        {
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 0f;
        }
    }

    // -------------------------------------------------------------------------

    string GenerateOracleResponse(string input)
    {
        string lower = input.ToLowerInvariant();

        for (int i = 0; i < LevelLibrary.Count; i++)
        {
            string town = LevelLibrary.Names[i].ToLowerInvariant();
            if (lower.Contains(town) && !ProgressionRules.IsUnlocked(SaveSystem.Current, i))
                return "My records on that region are still locked. Explore further.";
        }

        return "The Oracle is still awakening. [PLACEHOLDER — Phase 5]";
    }
}
