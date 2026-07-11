using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Oracle chat controller for the right-hand Almanac page. Renders a
/// Messenger-style transcript — the player's messages as amber bubbles on the
/// right, the Oracle's as grey bubbles on the left, one row per message. Player
/// messages are forwarded to <see cref="GeminiClient"/> via
/// <see cref="HeritageOracleService"/>; greetings and out-of-domain questions are
/// answered locally (no API spend), and a fallback string is used when the API is
/// unavailable. Lock-checks run before any API call so spoilers never reach Gemini.
/// </summary>
public class ChatController : MonoBehaviour
{
    [SerializeField] private RectTransform  chatContent;
    [SerializeField] private TMP_Text       bubbleTemplate;
    [SerializeField] private TMP_InputField chatInput;
    [SerializeField] private Button         sendButton;
    [SerializeField] private Button         clearButton;
    [SerializeField] private Sprite         playerBubbleSprite;
    [SerializeField] private Sprite         oracleBubbleSprite;

    // Messenger-style palette: warm amber for the player, neutral grey for the Oracle.
    static readonly Color PlayerBubble = Color.white;
    static readonly Color PlayerText   = new Color32(66, 42, 30, 255);
    static readonly Color OracleBubble = Color.white;
    static readonly Color OracleText   = new Color32(66, 42, 30, 255);

    readonly List<GameObject> _rows    = new List<GameObject>();
    readonly List<string> _history = new List<string>();
    bool _bound;

    // -------------------------------------------------------------------------

    void Start() => Bind();

    void Bind()
    {
        if (_bound) return;
        _bound = true;
        if (sendButton  != null) sendButton.onClick.AddListener(OnSend);
        if (clearButton != null) clearButton.onClick.AddListener(ClearChat);
        if (chatInput   != null) chatInput.onSubmit.AddListener(_ => OnSend());

        ChatBubbleFactory.PrepareContent(chatContent);
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
            Remember(text, lockMsg);
            chatInput.ActivateInputField();
            return;
        }

        SetInputEnabled(false);
        StartCoroutine(AskOracle(text));
    }

    IEnumerator AskOracle(string input)
    {
        // Greetings, small talk and out-of-domain questions resolve locally — no API
        // call — so the Oracle still feels chatty without spending tokens.
        if (!HeritageOracleService.TryBuildRequest(input, SaveSystem.Current, _history,
                out AiRequest request, out IReadOnlyList<KnowledgeHit> hits, out string local))
        {
            var localBubble = AddBubble("", player: false);
            yield return RevealBubble(localBubble, local);
            Remember(input, local);
            SetInputEnabled(true);
            chatInput.ActivateInputField();
            yield break;
        }

        // Cache hit: this question against the same unlocked records was already answered —
        // replay it instantly without spending a token.
        string cacheKey = HeritageOracleService.CacheKey(input, hits);
        if (AiResponseCache.Oracle.TryGet(cacheKey, out string cachedAnswer))
        {
            var cachedBubble = AddBubble("", player: false);
            yield return RevealBubble(cachedBubble, cachedAnswer);
            Remember(input, cachedAnswer);
            SetInputEnabled(true);
            chatInput.ActivateInputField();
            yield break;
        }

        var typingBubble = AddBubble("…", player: false);

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
        {
            response = parsed.answer;
            AiResponseCache.Oracle.Put(cacheKey, response);   // only validated answers are cached
        }
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
        UpdateBubble(bubble, response);
    }

    void SetInputEnabled(bool enabled)
    {
        if (chatInput  != null) chatInput.interactable  = enabled;
        if (sendButton != null) sendButton.interactable = enabled;
    }

    /// <summary>Empties the transcript. Called by the Clear button and automatically
    /// when the Almanac closes, so each visit starts fresh.</summary>
    public void ClearChat()
    {
        foreach (GameObject row in _rows)
            if (row != null) Destroy(row);
        _rows.Clear();
        _history.Clear();
    }

    // -------------------------------------------------------------------------
    // Bubble construction (delegated to the shared ChatBubbleFactory)

    TMP_Text AddBubble(string text, bool player)
    {
        TMP_Text label = ChatBubbleFactory.Add(chatContent, bubbleTemplate, text, player,
            player ? PlayerBubble : OracleBubble, player ? PlayerText : OracleText,
            player ? playerBubbleSprite : oracleBubbleSprite, out GameObject row);
        if (row != null) _rows.Add(row);
        ChatBubbleFactory.ScrollToBottom(chatContent);
        return label;
    }

    void UpdateBubble(TMP_Text bubble, string text)
    {
        ChatBubbleFactory.SetText(bubble, text, chatContent);
        ChatBubbleFactory.ScrollToBottom(chatContent);
    }
}
