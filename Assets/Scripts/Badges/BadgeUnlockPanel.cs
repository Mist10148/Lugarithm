using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime controller for the badge unlock overlay. All references are wired
/// by <see cref="BadgeUnlockBuilder"/>; this script never touches Editor-only
/// UI factory code.
///
/// History: an earlier version hid the root in <see cref="Awake"/>, which
/// instantly re-hid the panel that <see cref="Show"/> was opening and stranded
/// the player on the results screen ("yellow box, can't get out"). The current
/// version makes dismissal bulletproof: the panel can be closed by the Continue
/// button, by clicking the dimmed backdrop, or by pressing Enter/Space/Esc, and
/// the fade can only ever raise alpha so an interrupted coroutine can never
/// leave the panel invisible-but-blocking.
/// </summary>
public class BadgeUnlockPanel : MonoBehaviour
{
    // Wired by builder
    [SerializeField] private GameObject  root;            // the backdrop — toggled on/off
    [SerializeField] private TMP_Text    badgeNameLabel;
    [SerializeField] private TMP_Text    townNameLabel;
    [SerializeField] private TMP_Text    descriptionLabel;
    [SerializeField] private Image       badgePlaceholder; // amber rect (placeholder art)
    [SerializeField] private Button      continueButton;
    [SerializeField] private Button      backdropButton;   // full-screen click-to-dismiss
    [SerializeField] private CanvasGroup canvasGroup;

    Action _onDone;
    bool   _showing;   // gates keyboard dismiss + guards double-invoke
    bool   _wired;

    void Awake()
    {
        WireButtons();
        // Do NOT SetActive(false) here. The overlay is built INACTIVE in Bootstrap,
        // so Awake first runs *inside* Show()'s root.SetActive(true) — hiding the
        // root here would instantly re-hide the panel we're opening. The builder
        // already sets the initial hidden state.
    }

    /// <summary>Idempotent — safe to call from both Awake and Show() because the
    /// component's GameObject may still be inactive when Show() first runs.</summary>
    void WireButtons()
    {
        if (_wired) return;
        _wired = true;
        if (continueButton != null) continueButton.onClick.AddListener(Dismiss);
        if (backdropButton != null) backdropButton.onClick.AddListener(Dismiss);
    }

    public void Show(BadgeDefinition badge, Action onDone)
    {
        WireButtons();   // defensive: Awake may not have run yet (object was inactive)
        _onDone  = onDone;
        _showing = true;

        if (badgeNameLabel   != null) badgeNameLabel.text   = badge != null ? badge.badgeName : "";
        if (townNameLabel    != null) townNameLabel.text    = badge != null && badge.townName != null
                                                              ? badge.townName.ToUpperInvariant() : "";
        if (descriptionLabel != null) descriptionLabel.text = badge != null ? badge.description : "";

        if (root != null) root.SetActive(true);

        // Show fully visible immediately, then run the fade as pure polish. The
        // coroutine only raises alpha toward 1, so even if a scene change aborts
        // it the panel is never left transparent-but-click-blocking.
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            StartCoroutine(FadeIn());
        }
    }

    void Update()
    {
        if (!_showing) return;
        if (Input.GetKeyDown(KeyCode.Return)  || Input.GetKeyDown(KeyCode.KeypadEnter) ||
            Input.GetKeyDown(KeyCode.Space)   || Input.GetKeyDown(KeyCode.Escape))
            Dismiss();
    }

    void Dismiss()
    {
        if (!_showing) return;   // button + key can both fire in one frame
        _showing = false;

        if (root != null) root.SetActive(false);

        Action done = _onDone;
        _onDone = null;
        done?.Invoke();
    }

    System.Collections.IEnumerator FadeIn()
    {
        // Polish only: ease in from a slight dim. Never drops below what Show() set.
        float t = 0.55f;
        if (canvasGroup != null) canvasGroup.alpha = t;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.3f;
            if (canvasGroup != null) canvasGroup.alpha = Mathf.Clamp01(t);
            yield return null;
        }
        if (canvasGroup != null) canvasGroup.alpha = 1f;
    }
}
