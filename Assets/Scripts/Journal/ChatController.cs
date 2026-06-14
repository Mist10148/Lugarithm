using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Oracle chat controller for the right-hand Almanac page.
/// Player messages are forwarded to <see cref="GeminiClient"/> via
/// <see cref="HeritageOracleService"/>; a fallback string is used when the
/// API is unavailable.  Lock-checks run before any API call so spoilers
/// never reach Gemini's context.
/// </summary>
public class ChatController : MonoBehaviour
{
    [SerializeField] private RectTransform  chatContent;
    [SerializeField] private TMP_Text       bubbleTemplate;
    [SerializeField] private TMP_InputField chatInput;
    [SerializeField] private Button         sendButton;

    static readonly Color TextBright = new Color(0.93f, 0.93f, 0.88f, 1f);
    static readonly Color TextDim    = new Color(0.62f, 0.64f, 0.66f, 1f);

    readonly List<TMP_Text> _bubbles = new List<TMP_Text>();
    bool _bound;

    // -------------------------------------------------------------------------

    void Start() => Bind();

    void Bind()
    {
        if (_bound) return;
        _bound = true;
        if (sendButton != null) sendButton.onClick.AddListener(OnSend);
        if (chatInput  != null) chatInput.onSubmit.AddListener(_ => OnSend());
    }

    void OnSend()
    {
        if (chatInput == null) return;
        string text = chatInput.text;
        if (string.IsNullOrWhiteSpace(text)) return;

        text = text.Trim();
        AddBubble(text, player: true);
        chatInput.text = "";

        string lockMsg = GetLockMessage(text);
        if (lockMsg != null)
        {
            AddBubble(lockMsg, player: false);
            chatInput.ActivateInputField();
            return;
        }

        SetInputEnabled(false);
        StartCoroutine(AskOracle(text));
    }

    IEnumerator AskOracle(string input)
    {
        var typingBubble = AddBubble("...", player: false);

        string prompt   = HeritageOracleService.BuildPrompt(input, SaveSystem.Current.currentLevelIndex);
        string response = null;
        yield return GeminiClient.Ask(prompt, r => response = r);

        UpdateBubble(typingBubble, response
            ?? HeritageOracleService.FallbackResponse(SaveSystem.Current.currentLevelIndex));

        SetInputEnabled(true);
        chatInput.ActivateInputField();
    }

    // -------------------------------------------------------------------------

    static string GetLockMessage(string input)
    {
        string lower = input.ToLowerInvariant();
        for (int i = 0; i < LevelLibrary.Count; i++)
        {
            string town = LevelLibrary.Names[i].ToLowerInvariant();
            if (lower.Contains(town) && !ProgressionRules.IsUnlocked(SaveSystem.Current, i))
                return "My records on that region are still locked. Explore further.";
        }
        return null;
    }

    void SetInputEnabled(bool enabled)
    {
        if (chatInput  != null) chatInput.interactable  = enabled;
        if (sendButton != null) sendButton.interactable = enabled;
    }

    TMP_Text AddBubble(string text, bool player)
    {
        if (bubbleTemplate == null || chatContent == null) return null;

        var bubble = Instantiate(bubbleTemplate, chatContent);
        bubble.gameObject.SetActive(true);
        bubble.text      = text;
        bubble.color     = player ? TextBright : TextDim;
        bubble.alignment = player ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;

        var le = bubble.GetComponent<LayoutElement>();
        if (le == null) le = bubble.gameObject.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;

        _bubbles.Add(bubble);
        RebuildBubbleHeights();
        ScrollToBottom();
        return bubble;
    }

    void UpdateBubble(TMP_Text bubble, string text)
    {
        if (bubble == null) return;
        bubble.text = text;
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
}
