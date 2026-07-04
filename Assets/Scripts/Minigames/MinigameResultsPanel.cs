using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Compact post-minigame results card, shared by every minigame. Shows the
/// category (MINIGAME · Code / Non-code), outcome + score stats, and — for the
/// code-based drills — a deterministic code-analysis block (efficiency, complexity,
/// structure) so the analytics mirror the main Automation panel. One Continue
/// button hands control back to the caller. Null-safe: a minigame with no panel
/// wired just invokes its callback immediately (unchanged behavior).
/// </summary>
public class MinigameResultsPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text   categoryLabel;
    [SerializeField] private TMP_Text   titleLabel;
    [SerializeField] private TMP_Text   statsLabel;
    [SerializeField] private GameObject analysisGroup;   // shown only for code-based drills
    [SerializeField] private TMP_Text   analysisLabel;
    [SerializeField] private TMP_Dropdown attemptDropdown;
    [SerializeField] private TMP_Text   attemptStatusLabel;
    [SerializeField] private GameObject codeCompareGroup;
    [SerializeField] private TMP_Text   playerSourceLabel;
    [SerializeField] private TMP_Text   referenceSourceLabel;
    [SerializeField] private TMP_Text   mentorLabel;
    [SerializeField] private Button     continueButton;

    Action _onContinue;
    CodeRunAttempt[] _attempts = Array.Empty<CodeRunAttempt>();
    string _finalPlayerSource = "";
    string _referenceSource = "";

    void Awake()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(() =>
            {
                Action cb = _onContinue;
                _onContinue = null;
                Hide();
                cb?.Invoke();
            });
        if (attemptDropdown != null)
            attemptDropdown.onValueChanged.AddListener(OnAttemptSelected);

        if (root != null) root.SetActive(false);
    }

    /// <summary>Shows the card. <paramref name="analysis"/> non-null reveals the code
    /// analysis block (code-based drills); pass null for non-code minigames.</summary>
    public void Show(string category, string title, MinigameResult result,
                     CodeAnalysis analysis, Action onContinue,
                     string playerSource = "", string referenceSource = "",
                     IReadOnlyList<CodeRunAttempt> attempts = null)
    {
        _onContinue = onContinue;
        _finalPlayerSource = playerSource ?? "";
        _referenceSource = referenceSource ?? "";

        if (categoryLabel != null) categoryLabel.text = category;
        if (titleLabel    != null) titleLabel.text    = title;
        if (statsLabel    != null) statsLabel.text    = BuildStats(result);

        bool hasAnalysis = analysis != null;
        if (analysisGroup != null) analysisGroup.SetActive(hasAnalysis);
        if (hasAnalysis && analysisLabel != null) analysisLabel.text = BuildAnalysis(analysis);
        if (codeCompareGroup != null) codeCompareGroup.SetActive(hasAnalysis);
        if (referenceSourceLabel != null) referenceSourceLabel.text = EscapeRichText(_referenceSource);
        if (mentorLabel != null) mentorLabel.text = hasAnalysis ? "..." : "";
        ConfigureAttempts(attempts);

        if (root != null) root.SetActive(true);
    }

    public void SetMentorReview(MentorReview review)
    {
        if (review == null) return;
        if (mentorLabel != null) mentorLabel.text = review.summary;
        _referenceSource = string.IsNullOrWhiteSpace(review.optimizedCode)
            ? _referenceSource
            : review.optimizedCode;
        if (referenceSourceLabel != null) referenceSourceLabel.text = EscapeRichText(_referenceSource);
    }

    static string BuildStats(MinigameResult r)
    {
        if (r == null) return "";
        string outcome = r.TimedOut
            ? "<color=#E0A030>Timed out — the run carries on</color>"
            : "<color=#7CFC72>Solved</color>";
        return $"{outcome}\n\n" +
               $"Score      <b>{r.Score}</b>\n" +
               $"Mistakes   <b>{r.Mistakes}</b>";
    }

    static string BuildAnalysis(CodeAnalysis a)
    {
        return $"<b>Efficiency {a.EfficiencyScore}/100</b>   ·   {a.ComplexityClass}\n" +
               $"{a.Summary}\n" +
               $"<color=#9EA0A2>statements {a.StatementCount}  ·  nesting {a.MaxNesting}  ·  loop depth {a.LoopDepth}</color>";
    }

    void ConfigureAttempts(IReadOnlyList<CodeRunAttempt> attempts)
    {
        _attempts = attempts != null ? ToArray(attempts) : Array.Empty<CodeRunAttempt>();
        if (attemptDropdown == null) return;

        attemptDropdown.ClearOptions();
        if (_attempts.Length == 0)
        {
            attemptDropdown.gameObject.SetActive(false);
            if (attemptStatusLabel != null) attemptStatusLabel.text = "";
            if (playerSourceLabel != null) playerSourceLabel.text = EscapeRichText(_finalPlayerSource);
            return;
        }

        var options = new List<string>();
        foreach (CodeRunAttempt attempt in _attempts)
            options.Add(attempt != null ? attempt.DisplayName : "Run");
        attemptDropdown.AddOptions(options);
        attemptDropdown.gameObject.SetActive(true);
        attemptDropdown.value = _attempts.Length - 1;
        attemptDropdown.RefreshShownValue();
        OnAttemptSelected(_attempts.Length - 1);
    }

    static CodeRunAttempt[] ToArray(IReadOnlyList<CodeRunAttempt> attempts)
    {
        var copy = new CodeRunAttempt[attempts.Count];
        for (int i = 0; i < attempts.Count; i++) copy[i] = attempts[i];
        return copy;
    }

    void OnAttemptSelected(int index)
    {
        if (_attempts == null || index < 0 || index >= _attempts.Length) return;
        CodeRunAttempt attempt = _attempts[index];
        if (playerSourceLabel != null)
            playerSourceLabel.text = EscapeRichText(attempt != null ? attempt.source : _finalPlayerSource);
        if (attemptStatusLabel != null && attempt != null)
            attemptStatusLabel.text = string.IsNullOrWhiteSpace(attempt.summary)
                ? attempt.DisplayName
                : attempt.summary;
    }

    static string EscapeRichText(string source)
    {
        return (source ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);
    }
}
