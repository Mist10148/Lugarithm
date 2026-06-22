using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the end-of-leg UI: a centered "LEVEL COMPLETE" congratulations card with
/// a choice to keep free-roaming or finish, plus a small persistent "Finish leg"
/// button shown afterwards so the player can wrap up whenever they like.
///
/// Events: <see cref="OnFinishPressed"/> fires when the player chooses to finish &amp;
/// leave (the card's leave button or the small finish button); <see cref="OnKeepExploring"/>
/// fires when they choose to keep driving the free-roam town.
/// </summary>
public class LegCompletionController : MonoBehaviour
{
    [SerializeField] private GameObject root;             // overall container
    [Header("Congratulations card")]
    [SerializeField] private GameObject completePanel;    // dimmed full-screen + centered card
    [SerializeField] private TMP_Text   titleLabel;
    [SerializeField] private TMP_Text   messageLabel;
    [SerializeField] private Button      exploreButton;   // "Keep exploring" (free-roam)
    [SerializeField] private Button      leaveButton;     // "Finish & leave"
    [Header("Persistent finish button")]
    [SerializeField] private GameObject finishButtonRoot; // small top-center button
    [SerializeField] private Button     finishButton;     // "Finish leg"

    public bool IsVisible => root != null && root.activeSelf;

    public event Action OnFinishPressed;
    public event Action OnKeepExploring;

    void Start()
    {
        HideAll();
        if (exploreButton != null) exploreButton.onClick.AddListener(KeepExploring);
        if (leaveButton   != null) leaveButton.onClick.AddListener(() => OnFinishPressed?.Invoke());
        if (finishButton  != null) finishButton.onClick.AddListener(() => OnFinishPressed?.Invoke());
    }

    void OnDestroy()
    {
        if (exploreButton != null) exploreButton.onClick.RemoveAllListeners();
        if (leaveButton   != null) leaveButton.onClick.RemoveAllListeners();
        if (finishButton  != null) finishButton.onClick.RemoveAllListeners();
    }

    /// <summary>Shows the centered congratulations card. <paramref name="allowExplore"/>
    /// hides the "Keep exploring" option where free-roam doesn't apply (Automation).</summary>
    public void ShowComplete(string title, string message, bool allowExplore)
    {
        if (root != null) root.SetActive(true);
        if (completePanel != null) completePanel.SetActive(true);
        if (finishButtonRoot != null) finishButtonRoot.SetActive(false);
        if (titleLabel   != null) titleLabel.text   = title;
        if (messageLabel != null) messageLabel.text = message;
        if (exploreButton != null) exploreButton.gameObject.SetActive(allowExplore);
    }

    /// <summary>Dismiss the card and leave just the small "Finish leg" button up.</summary>
    void KeepExploring()
    {
        ShowFinishButton();
        OnKeepExploring?.Invoke();
    }

    /// <summary>Shows only the small persistent "Finish leg" button (kept for the
    /// existing callers that latch it after the story beat).</summary>
    public void Show() => ShowFinishButton();

    void ShowFinishButton()
    {
        if (root != null) root.SetActive(true);
        if (completePanel != null) completePanel.SetActive(false);
        if (finishButtonRoot != null) finishButtonRoot.SetActive(true);
    }

    public void Hide() => HideAll();

    void HideAll()
    {
        if (completePanel    != null) completePanel.SetActive(false);
        if (finishButtonRoot != null) finishButtonRoot.SetActive(false);
        if (root != null) root.SetActive(false);
    }
}
