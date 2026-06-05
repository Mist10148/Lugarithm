using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Manages the splash screen sequence:
/// fade in logo → hold → fade out → load Main Menu.
/// Any key skips to the Main Menu immediately.
/// </summary>
public class SplashScreenManager : MonoBehaviour
{
    [Header("Canvas Groups")]
    [Tooltip("CanvasGroup on the logo/title panel")]
    [SerializeField] private CanvasGroup logoGroup;

    [Tooltip("CanvasGroup on the team name panel (e.g. 'A CYFER GAME')")]
    [SerializeField] private CanvasGroup teamNameGroup;

    [Header("Timing (seconds)")]
    [SerializeField] private float fadeInDuration  = 1.5f;
    [SerializeField] private float holdDuration    = 2.5f;
    [SerializeField] private float fadeOutDuration = 1.0f;

    [Header("Next Scene")]
    [SerializeField] private string nextSceneName = "MainMenu";

    private bool _skipped = false;

    // -------------------------------------------------------------------------

    void Start()
    {
        // Make sure both groups start invisible
        SetAlpha(logoGroup,     0f);
        SetAlpha(teamNameGroup, 0f);

        StartCoroutine(PlaySplash());
    }

    void Update()
    {
        // Any key / mouse click skips the splash
        if (!_skipped && AnySkipPressed())
        {
            _skipped = true;
            StopAllCoroutines();
            GoToMainMenu();
        }
    }

    /// <summary>
    /// True on the frame the player presses any key, the left mouse button, or a
    /// gamepad button. Uses the Input System package (the project's active input
    /// handler) — the legacy UnityEngine.Input class throws under that backend.
    /// </summary>
    static bool AnySkipPressed()
    {
        return (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            || (Mouse.current    != null && Mouse.current.leftButton.wasPressedThisFrame)
            || (Gamepad.current  != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
    }

    // -------------------------------------------------------------------------

    IEnumerator PlaySplash()
    {
        // 1. Fade in logo
        yield return StartCoroutine(Fade(logoGroup, 0f, 1f, fadeInDuration));

        // 2. Fade in team name slightly after (shorter fade)
        yield return StartCoroutine(Fade(teamNameGroup, 0f, 1f, fadeInDuration * 0.6f));

        // 3. Hold
        yield return new WaitForSeconds(holdDuration);

        // 4. Fade both out simultaneously
        StartCoroutine(Fade(teamNameGroup, 1f, 0f, fadeOutDuration));
        yield return StartCoroutine(Fade(logoGroup, 1f, 0f, fadeOutDuration));

        // 5. Load next scene
        GoToMainMenu();
    }

    IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null) yield break;

        float elapsed = 0f;
        group.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        group.alpha = to;
    }

    void SetAlpha(CanvasGroup group, float alpha)
    {
        if (group != null) group.alpha = alpha;
    }

    void GoToMainMenu()
    {
        // Route through the transition manager when present so the scene swap
        // matches every other transition; fall back to a hard load otherwise.
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.TransitionTo(nextSceneName);
        else
            SceneManager.LoadScene(nextSceneName);
    }
}
