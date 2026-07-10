using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Defensive fallback panel for future station kinds that do not yet have a
/// playable controller. Every current six-objective town routes to a real game.
/// </summary>
public class MinigamePlaceholderPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject root;
    [SerializeField] private Image      accentBar;       // tinted to the station marker colour
    [SerializeField] private TMP_Text   categoryLabel;   // PUZZLE · CODING CHALLENGE
    [SerializeField] private TMP_Text   titleLabel;
    [SerializeField] private TMP_Text   descriptionLabel;
    [SerializeField] private TMP_Text   conceptLabel;
    [SerializeField] private TMP_Text   placeholderNote;
    [SerializeField] private Button     startButton;
    [SerializeField] private Button     leaveButton;
    [SerializeField] private TMP_Text   startButtonLabel;

    Action _onComplete;
    Action _onCancel;

    public bool IsOpen => root != null && root.activeSelf;

    void Awake()
    {
        if (startButton != null)
            startButton.onClick.AddListener(() =>
            {
                Action cb = _onComplete;
                _onComplete = null;
                _onCancel = null;
                Hide();
                cb?.Invoke();
            });

        if (leaveButton != null)
            leaveButton.onClick.AddListener(() =>
            {
                Action cb = _onCancel;
                _onComplete = null;
                _onCancel = null;
                Hide();
                cb?.Invoke();
            });

        if (root != null) root.SetActive(false);
    }

    /// <summary>
    /// Shows the access card for a station. <paramref name="alreadySolved"/> swaps
    /// the Start button into a disabled "Solved" state so re-entry just reviews it.
    /// </summary>
    public void Show(MinigameStationDef def, bool alreadySolved,
                     Action onComplete, Action onCancel)
    {
        if (def == null) { onCancel?.Invoke(); return; }

        _onComplete = onComplete;
        _onCancel   = onCancel;

        if (accentBar != null) accentBar.color = def.markerColor;

        if (categoryLabel != null)
            categoryLabel.text = def.IsCoding ? "CODING CHALLENGE" : "PUZZLE";

        if (titleLabel != null) titleLabel.text = def.title;
        if (descriptionLabel != null) descriptionLabel.text = def.description;

        if (conceptLabel != null)
        {
            string prefix = def.IsCoding ? "Concept: " : "Heritage: ";
            conceptLabel.text = string.IsNullOrEmpty(def.concept) ? "" : prefix + def.concept;
        }

        if (placeholderNote != null)
            placeholderNote.text = def.IsCoding
                ? "Placeholder — the coding challenge isn't built yet. Starting it counts it as cleared so you can move on."
                : "Placeholder — this puzzle isn't built yet. Starting it counts it as solved.";

        if (startButton != null) startButton.interactable = !alreadySolved;
        if (startButtonLabel != null)
            startButtonLabel.text = alreadySolved ? "Solved" : (def.IsCoding ? "Start Challenge" : "Start Puzzle");

        if (root != null) root.SetActive(true);
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);
    }
}
