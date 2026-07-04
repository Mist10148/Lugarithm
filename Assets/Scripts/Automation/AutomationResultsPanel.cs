using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Post-puzzle results panel: the player's solution and the optimal one
/// side-by-side, score/time/currency, a Gemini-powered mentor explanation,
/// and Continue / Replay.
/// </summary>
public class AutomationResultsPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text categoryLabel;   // "MAIN GAMEPLAY · Automation (Code)"
    [SerializeField] private TMP_Text titleLabel;
    [SerializeField] private TMP_Text playerSolutionLabel;
    [SerializeField] private TMP_Text optimalSolutionLabel;
    [SerializeField] private TMP_Text statsLabel;
    [SerializeField] private TMP_Text mentorLabel;
    [SerializeField] private TMP_Text efficiencyLabel;
    [SerializeField] private TMP_Dropdown attemptDropdown;
    [SerializeField] private TMP_Text attemptStatusLabel;
    [SerializeField] private RectTransform annotationContainer;
    [SerializeField] private Button annotationTemplate;
    [SerializeField] private GameObject tooltipRoot;
    [SerializeField] private TMP_Text tooltipLabel;
    [SerializeField] private Button   continueButton;
    [SerializeField] private Button   replayButton;

    Action _onContinue;
    Action _onReplay;
    string _finalPlayerSource;
    string _optimalSource;
    CodeRunAttempt[] _attempts = Array.Empty<CodeRunAttempt>();
    CodeReviewAnnotation[] _annotations = Array.Empty<CodeReviewAnnotation>();
    readonly List<Button> _annotationButtons = new List<Button>();

    // -------------------------------------------------------------------------

    void Awake()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(() => _onContinue?.Invoke());
        if (replayButton != null)
            replayButton.onClick.AddListener(() => _onReplay?.Invoke());
        if (attemptDropdown != null)
            attemptDropdown.onValueChanged.AddListener(OnAttemptSelected);

        if (root != null) root.SetActive(false);
    }

    public void Show(string title, string playerSolution, string optimalSolution,
                     string stats, Action onContinue, Action onReplay, CodeAnalysis analysis = null,
                     string category = "MAIN GAMEPLAY · Automation",
                     IReadOnlyList<CodeRunAttempt> attempts = null)
    {
        _onContinue = onContinue;
        _onReplay   = onReplay;
        _finalPlayerSource = playerSolution ?? "";
        _optimalSource = optimalSolution ?? "";

        if (categoryLabel        != null) categoryLabel.text        = category;
        if (titleLabel           != null) titleLabel.text           = title;
        if (playerSolutionLabel  != null) playerSolutionLabel.text  = playerSolution;
        if (optimalSolutionLabel != null) optimalSolutionLabel.text = optimalSolution;
        if (statsLabel           != null) statsLabel.text           = stats;
        if (mentorLabel          != null) mentorLabel.text          = "...";
        if (efficiencyLabel != null && analysis != null)
            efficiencyLabel.text = $"EFFICIENCY {analysis.EfficiencyScore}/100   ·   {analysis.ComplexityClass}   ·   {analysis.Summary}";
        _annotations = Array.Empty<CodeReviewAnnotation>();
        ConfigureAttempts(attempts);
        ClearAnnotations();
        if (tooltipRoot != null) tooltipRoot.SetActive(false);
        if (root                 != null) root.SetActive(true);
    }

    public void SetMentorResponse(string text)
    {
        if (mentorLabel != null) mentorLabel.text = text;
    }

    public void SetMentorReview(MentorReview review)
    {
        if (review == null) return;
        if (mentorLabel != null) mentorLabel.text = review.summary;
        _optimalSource = review.optimizedCode ?? _optimalSource;
        if (optimalSolutionLabel != null) optimalSolutionLabel.text = _optimalSource;
        _annotations = review.annotations ?? Array.Empty<CodeReviewAnnotation>();
        BuildAnnotations(_annotations);
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
        bool finalAttempt = index == _attempts.Length - 1;

        if (attemptStatusLabel != null && attempt != null)
            attemptStatusLabel.text = string.IsNullOrWhiteSpace(attempt.summary)
                ? attempt.DisplayName
                : attempt.summary;

        if (playerSolutionLabel != null)
        {
            if (finalAttempt && _annotations.Length > 0)
                playerSolutionLabel.text = HighlightLines(_finalPlayerSource, _annotations, "player");
            else
                playerSolutionLabel.text = EscapeRichText(attempt != null ? attempt.source : _finalPlayerSource);
        }
    }

    void BuildAnnotations(CodeReviewAnnotation[] annotations)
    {
        ClearAnnotations();
        if (annotationContainer == null || annotationTemplate == null) return;
        foreach (CodeReviewAnnotation annotation in annotations)
        {
            Button button = Instantiate(annotationTemplate, annotationContainer);
            button.gameObject.SetActive(true);
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = $"{(annotation.side == "player" ? "YOU" : "AI")} L{annotation.startLine}" +
                             (annotation.endLine > annotation.startLine ? $"–{annotation.endLine}" : "") +
                             $": {annotation.title}";
            CodeAnnotationTooltip hover = button.gameObject.AddComponent<CodeAnnotationTooltip>();
            hover.Configure(tooltipRoot, tooltipLabel,
                $"<b>{annotation.title}</b>  ·  {annotation.category}\n{annotation.explanation}");
            _annotationButtons.Add(button);
        }

        bool finalAttemptSelected = attemptDropdown == null || _attempts.Length == 0 ||
                                    attemptDropdown.value == _attempts.Length - 1;
        if (playerSolutionLabel != null && finalAttemptSelected)
            playerSolutionLabel.text = HighlightLines(_finalPlayerSource, annotations, "player");
        if (optimalSolutionLabel != null)
            optimalSolutionLabel.text = HighlightLines(_optimalSource, annotations, "optimized");
    }

    static string HighlightLines(string source, CodeReviewAnnotation[] annotations, string side)
    {
        string[] lines = (source ?? "").Replace("\r", "").Split('\n');
        StringBuilder result = new StringBuilder();
        for (int i = 0; i < lines.Length; i++)
        {
            int line = i + 1;
            bool marked = System.Array.Exists(annotations, a => a.side == side && line >= a.startLine && line <= a.endLine);
            string escaped = lines[i].Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
            if (marked) result.Append("<mark=#D9922B44>").Append(escaped).Append("</mark>");
            else result.Append(escaped);
            if (i < lines.Length - 1) result.Append('\n');
        }
        return result.ToString();
    }

    static string EscapeRichText(string source)
    {
        return (source ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }

    void ClearAnnotations()
    {
        foreach (Button button in _annotationButtons) if (button != null) Destroy(button.gameObject);
        _annotationButtons.Clear();
    }

    public void Hide()
    {
        ClearAnnotations();
        if (root != null) root.SetActive(false);
    }
}
