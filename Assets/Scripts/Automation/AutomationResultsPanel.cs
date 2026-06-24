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
    [SerializeField] private RectTransform annotationContainer;
    [SerializeField] private Button annotationTemplate;
    [SerializeField] private GameObject tooltipRoot;
    [SerializeField] private TMP_Text tooltipLabel;
    [SerializeField] private Button   continueButton;
    [SerializeField] private Button   replayButton;

    Action _onContinue;
    Action _onReplay;
    string _playerSource;
    string _optimalSource;
    readonly List<Button> _annotationButtons = new List<Button>();

    // -------------------------------------------------------------------------

    void Awake()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(() => _onContinue?.Invoke());
        if (replayButton != null)
            replayButton.onClick.AddListener(() => _onReplay?.Invoke());

        if (root != null) root.SetActive(false);
    }

    public void Show(string title, string playerSolution, string optimalSolution,
                     string stats, Action onContinue, Action onReplay, CodeAnalysis analysis = null,
                     string category = "MAIN GAMEPLAY · Automation")
    {
        _onContinue = onContinue;
        _onReplay   = onReplay;
        _playerSource = playerSolution ?? "";
        _optimalSource = optimalSolution ?? "";

        if (categoryLabel        != null) categoryLabel.text        = category;
        if (titleLabel           != null) titleLabel.text           = title;
        if (playerSolutionLabel  != null) playerSolutionLabel.text  = playerSolution;
        if (optimalSolutionLabel != null) optimalSolutionLabel.text = optimalSolution;
        if (statsLabel           != null) statsLabel.text           = stats;
        if (mentorLabel          != null) mentorLabel.text          = "...";
        if (efficiencyLabel != null && analysis != null)
            efficiencyLabel.text = $"EFFICIENCY {analysis.EfficiencyScore}/100   ·   {analysis.ComplexityClass}   ·   {analysis.Summary}";
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
        BuildAnnotations(review.annotations ?? new CodeReviewAnnotation[0]);
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

        if (playerSolutionLabel != null)
            playerSolutionLabel.text = HighlightLines(_playerSource, annotations, "player");
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
