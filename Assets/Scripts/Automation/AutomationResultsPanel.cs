using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Post-puzzle analytics stub (full AI mentor lands in Phase 5): the player's
/// solution and the optimal one side-by-side, score/time/currency, and
/// Continue / Replay.
/// </summary>
public class AutomationResultsPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleLabel;
    [SerializeField] private TMP_Text playerSolutionLabel;
    [SerializeField] private TMP_Text optimalSolutionLabel;
    [SerializeField] private TMP_Text statsLabel;
    [SerializeField] private Button   continueButton;
    [SerializeField] private Button   replayButton;

    Action _onContinue;
    Action _onReplay;

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
                     string stats, Action onContinue, Action onReplay)
    {
        _onContinue = onContinue;
        _onReplay   = onReplay;

        if (titleLabel           != null) titleLabel.text           = title;
        if (playerSolutionLabel  != null) playerSolutionLabel.text  = playerSolution;
        if (optimalSolutionLabel != null) optimalSolutionLabel.text = optimalSolution;
        if (statsLabel           != null) statsLabel.text           = stats;
        if (root                 != null) root.SetActive(true);
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);
    }
}
