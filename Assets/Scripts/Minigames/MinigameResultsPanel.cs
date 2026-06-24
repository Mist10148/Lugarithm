using System;
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
    [SerializeField] private Button     continueButton;

    Action _onContinue;

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

        if (root != null) root.SetActive(false);
    }

    /// <summary>Shows the card. <paramref name="analysis"/> non-null reveals the code
    /// analysis block (code-based drills); pass null for non-code minigames.</summary>
    public void Show(string category, string title, MinigameResult result,
                     CodeAnalysis analysis, Action onContinue)
    {
        _onContinue = onContinue;

        if (categoryLabel != null) categoryLabel.text = category;
        if (titleLabel    != null) titleLabel.text    = title;
        if (statsLabel    != null) statsLabel.text    = BuildStats(result);

        bool hasAnalysis = analysis != null;
        if (analysisGroup != null) analysisGroup.SetActive(hasAnalysis);
        if (hasAnalysis && analysisLabel != null) analysisLabel.text = BuildAnalysis(analysis);

        if (root != null) root.SetActive(true);
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

    public void Hide()
    {
        if (root != null) root.SetActive(false);
    }
}
