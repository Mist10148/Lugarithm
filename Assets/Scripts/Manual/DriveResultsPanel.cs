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
    [SerializeField] private TMP_Text   categoryLabel;   // "MAIN GAMEPLAY · Manual"
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
                     Action onContinue, Action onReplay, string category = "MAIN GAMEPLAY · Manual")
    {
        _onContinue = onContinue;
        _onReplay   = onReplay;

        if (categoryLabel  != null) categoryLabel.text  = category;
        if (titleLabel     != null) titleLabel.text     = title;
        if (breakdownLabel != null) breakdownLabel.text = breakdown;
        if (scoreLabel     != null) scoreLabel.text     = $"SCORE  <b>{score}</b>      EARNED  <b>₱{currency}</b>";
        if (root           != null) root.SetActive(true);
    }

    /// <summary>Hides the overlay. Called on the Continue handoff so no stale
    /// raycaster competes with the badge unlock panel layered above it.</summary>
    public void Hide()
    {
        if (root != null) root.SetActive(false);
    }
}
