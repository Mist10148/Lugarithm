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
    readonly List<string> _history = new List<string>();
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

        string lockMsg = KnowledgeRagService.TryGetLockedTownMessage(text, SaveSystem.Current, out string locked)
            ? locked : null;
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

        if (!HeritageOracleService.TryBuildRequest(input, SaveSystem.Current, _history,
                out AiRequest request, out IReadOnlyList<KnowledgeHit> hits, out string local))
        {
            UpdateBubble(typingBubble, local);
            Remember(input, local);
            SetInputEnabled(true);
            chatInput.ActivateInputField();
            yield break;
        }

        AiResult result = null;
        int packets = 0;
        yield return GeminiClient.Stream(request, _ =>
        {
            packets++;
            UpdateBubble(typingBubble, "Consulting recovered records" + new string('.', 1 + packets % 3));
        }, completed => result = completed);

        string response = null;
        if (result != null && result.Success &&
            HeritageOracleService.TryParseAndValidate(result.Text, hits, out OracleResponse parsed))
            response = parsed.answer;
        response ??= HeritageOracleService.FallbackResponse(SaveSystem.Current.currentLevelIndex);
        yield return RevealBubble(typingBubble, response);
        Remember(input, response);

        SetInputEnabled(true);
        chatInput.ActivateInputField();
    }

    // -------------------------------------------------------------------------

    void Remember(string question, string answer)
    {
        _history.Add("Player: " + question);
        _history.Add("Oracle: " + answer);
        while (_history.Count > 8) _history.RemoveAt(0);
    }

    IEnumerator RevealBubble(TMP_Text bubble, string response)
    {
        string[] words = response.Split(' ');
        string visible = "";
        for (int i = 0; i < words.Length; i++)
        {
            visible += (i == 0 ? "" : " ") + words[i];
            UpdateBubble(bubble, visible);
            if (i % 4 == 3) yield return null;
        }
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
