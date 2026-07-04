using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Reusable dialogue box: shows a speaker name and body text with an optional
/// typewriter reveal. Built for the passenger dialogue system (Phase 2) but
/// usable by any character conversation.
/// </summary>
public class DialogBox : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject root;              // panel toggled on/off
    [SerializeField] private TMP_Text   speakerLabel;
    [SerializeField] private TMP_Text   bodyLabel;
    [SerializeField] private GameObject continueIndicator; // "▼" prompt, optional

    [Header("Typewriter")]
    [SerializeField] private bool  useTypewriter = true;
    [SerializeField] private float charsPerSecond = 40f;

    public bool UseTypewriter { get => useTypewriter; set => useTypewriter = value; }
    public float CharsPerSecond { get => charsPerSecond; set => charsPerSecond = value; }

    /// <summary>When false (Subtitles OFF) the line text is hidden while the bar,
    /// speaker name, and Next/Skip stay usable. The reveal still runs underneath, so
    /// turning subtitles back on mid-line shows the rest.</summary>
    public bool BodyVisible
    {
        set { if (bodyLabel != null) bodyLabel.enabled = value; }
    }

    private Coroutine _revealRoutine;
    private string    _fullText = "";
    private bool      _isRevealing;
    private int       _shownChars;

    /// <summary>True while the line is still being typed out.</summary>
    public bool IsRevealing => _isRevealing;

    // -------------------------------------------------------------------------

    void Awake()
    {
        Hide();
    }

    // -------------------------------------------------------------------------
    // Public API

    /// <summary>Shows the box with the given speaker and line.</summary>
    public void Show(string speaker, string text)
    {
        // A new line may arrive while the previous typewriter is still alive
        // (notably when Next and the global click handler fire together). Always
        // retire it before changing the backing text.
        if (_revealRoutine != null) StopCoroutine(_revealRoutine);
        _revealRoutine = null;
        _isRevealing = false;

        if (root != null) root.SetActive(true);
        if (speakerLabel != null) speakerLabel.text = speaker;

        _fullText = text ?? "";
        _shownChars = 0;

        if (useTypewriter && charsPerSecond > 0f)
        {
            _revealRoutine = StartCoroutine(Reveal());
        }
        else
        {
            SetBody(_fullText);
            ShowContinuePrompt();
        }
    }

    public void BeginStreaming(string speaker)
    {
        if (_revealRoutine != null) StopCoroutine(_revealRoutine);
        _revealRoutine = null;
        _isRevealing = true;
        _fullText = "";
        _shownChars = 0;
        if (root != null) root.SetActive(true);
        if (speakerLabel != null) speakerLabel.text = speaker;
        if (continueIndicator != null) continueIndicator.SetActive(false);
        SetBody("");
    }

    public void AppendStreaming(string text)
    {
        _fullText += text ?? "";
        _shownChars = _fullText.Length;
        SetBody(_fullText);
    }

    public void UpdateStreaming(string text)
    {
        _fullText = text ?? "";
        _shownChars = Mathf.Min(_shownChars, _fullText.Length);
        _isRevealing = true;
        if (continueIndicator != null) continueIndicator.SetActive(false);

        if (useTypewriter && charsPerSecond > 0f)
        {
            if (_revealRoutine != null) StopCoroutine(_revealRoutine);
            _revealRoutine = StartCoroutine(RevealFrom(_shownChars));
        }
        else
        {
            SetBody(_fullText);
            _shownChars = _fullText.Length;
        }
    }

    public void CompleteStreaming(string finalText)
    {
        _fullText = finalText ?? "";
        if (_revealRoutine != null) StopCoroutine(_revealRoutine);
        _revealRoutine = null;
        SetBody(_fullText);
        _shownChars = _fullText.Length;
        _isRevealing = false;
        ShowContinuePrompt();
    }

    /// <summary>
    /// Advances the box. If the line is still typing, completes it instantly and
    /// returns false. If it has finished, returns true so the caller knows it may
    /// move on to the next line.
    /// </summary>
    public bool Advance()
    {
        if (_isRevealing)
        {
            CompleteReveal();
            return false;
        }
        return true;
    }

    public void Hide()
    {
        if (_revealRoutine != null) StopCoroutine(_revealRoutine);
        _revealRoutine = null;
        _isRevealing = false;
        _shownChars = 0;
        if (continueIndicator != null) continueIndicator.SetActive(false);
        if (root != null) root.SetActive(false);
    }

    // -------------------------------------------------------------------------

    IEnumerator Reveal()
    {
        yield return RevealFrom(0);
    }

    IEnumerator RevealFrom(int startIndex)
    {
        _isRevealing = true;
        if (continueIndicator != null) continueIndicator.SetActive(false);

        float shown = startIndex;
        while (shown < _fullText.Length)
        {
            shown += charsPerSecond * Time.deltaTime;
            int count = Mathf.Clamp(Mathf.FloorToInt(shown), 0, _fullText.Length);
            _shownChars = count;
            SetBody(_fullText.Substring(0, count));
            yield return null;
        }

        CompleteReveal();
    }

    void CompleteReveal()
    {
        if (_revealRoutine != null) StopCoroutine(_revealRoutine);
        _revealRoutine = null;
        SetBody(_fullText);
        _shownChars = _fullText.Length;
        _isRevealing = false;
        ShowContinuePrompt();
    }

    void ShowContinuePrompt()
    {
        if (continueIndicator != null) continueIndicator.SetActive(true);
    }

    void SetBody(string text)
    {
        if (bodyLabel != null) bodyLabel.text = text;
    }
}
