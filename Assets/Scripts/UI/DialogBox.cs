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

    private Coroutine _revealRoutine;
    private string    _fullText = "";
    private bool      _isRevealing;

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
        if (root != null) root.SetActive(true);
        if (speakerLabel != null) speakerLabel.text = speaker;

        _fullText = text ?? "";

        if (useTypewriter && charsPerSecond > 0f)
        {
            if (_revealRoutine != null) StopCoroutine(_revealRoutine);
            _revealRoutine = StartCoroutine(Reveal());
        }
        else
        {
            SetBody(_fullText);
            ShowContinuePrompt();
        }
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
        if (continueIndicator != null) continueIndicator.SetActive(false);
        if (root != null) root.SetActive(false);
    }

    // -------------------------------------------------------------------------

    IEnumerator Reveal()
    {
        _isRevealing = true;
        if (continueIndicator != null) continueIndicator.SetActive(false);
        SetBody("");

        float shown = 0f;
        while (shown < _fullText.Length)
        {
            shown += charsPerSecond * Time.deltaTime;
            int count = Mathf.Clamp(Mathf.FloorToInt(shown), 0, _fullText.Length);
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
