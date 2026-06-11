using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// End-of-leg results overlay for Manual Mode: line-item breakdown, score,
/// currency earned, and Continue / Replay.
/// </summary>
public class DriveResultsPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text   titleLabel;
    [SerializeField] private TMP_Text   breakdownLabel;
    [SerializeField] private TMP_Text   scoreLabel;
    [SerializeField] private Button     continueButton;
    [SerializeField] private Button     replayButton;

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

    public void Show(string title, string breakdown, int score, int currency,
                     Action onContinue, Action onReplay)
    {
        _onContinue = onContinue;
        _onReplay   = onReplay;

        if (titleLabel     != null) titleLabel.text     = title;
        if (breakdownLabel != null) breakdownLabel.text = breakdown;
        if (scoreLabel     != null) scoreLabel.text     = $"SCORE  {score}      EARNED  ₱{currency}";
        if (root           != null) root.SetActive(true);
    }
}
