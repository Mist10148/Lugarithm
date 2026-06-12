using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime controller for the badge unlock overlay. All references are wired
/// by <see cref="BadgeUnlockBuilder"/>; this script never touches Editor-only
/// UI factory code.
/// </summary>
public class BadgeUnlockPanel : MonoBehaviour
{
    // Wired by builder
    [SerializeField] private GameObject  root;          // the backdrop — toggled on/off
    [SerializeField] private TMP_Text    badgeNameLabel;
    [SerializeField] private TMP_Text    townNameLabel;
    [SerializeField] private TMP_Text    descriptionLabel;
    [SerializeField] private Image       badgePlaceholder; // amber rect
    [SerializeField] private Button      continueButton;
    [SerializeField] private CanvasGroup canvasGroup;

    // Palette (never reference UIFactory from runtime scripts)
    static readonly Color Accent     = new Color(0.95f, 0.65f, 0.15f, 1f);
    static readonly Color TextBright = new Color(0.93f, 0.93f, 0.88f, 1f);
    static readonly Color TextDim    = new Color(0.62f, 0.64f, 0.66f, 1f);

    Action _onDone;

    void Awake()
    {
        continueButton?.onClick.AddListener(OnContinue);
        if (root != null) root.SetActive(false);
    }

    public void Show(BadgeDefinition badge, Action onDone)
    {
        _onDone = onDone;

        if (badgeNameLabel    != null) badgeNameLabel.text    = badge.badgeName;
        if (townNameLabel     != null) townNameLabel.text     = badge.townName.ToUpperInvariant();
        if (descriptionLabel  != null) descriptionLabel.text  = badge.description;

        if (root != null) root.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            StartCoroutine(FadeIn());
        }
    }

    void OnContinue()
    {
        if (root != null) root.SetActive(false);
        _onDone?.Invoke();
        _onDone = null;
    }

    System.Collections.IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.4f;
            if (canvasGroup != null) canvasGroup.alpha = Mathf.Clamp01(t);
            yield return null;
        }
    }
}
