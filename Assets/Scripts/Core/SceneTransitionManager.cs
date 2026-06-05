using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles scene changes with a fade in/out (and an optional loading screen).
/// Other scripts call <see cref="TransitionTo"/> instead of using SceneManager
/// directly so every transition looks consistent. Persists across scenes;
/// place one in the first-loaded (Bootstrap/Splash) scene.
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Overlay")]
    [Tooltip("Full-screen CanvasGroup used to fade to/from black.")]
    [SerializeField] private CanvasGroup fadeGroup;

    [Tooltip("Optional panel shown while the next scene loads.")]
    [SerializeField] private GameObject loadingPanel;

    [Header("Timing (seconds)")]
    [SerializeField] private float fadeDuration = 0.5f;

    private bool _isTransitioning;

    // -------------------------------------------------------------------------

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Start fully transparent and non-blocking.
        if (fadeGroup != null)
        {
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;
        }
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    // -------------------------------------------------------------------------
    // Public API

    /// <summary>Fades out, loads <paramref name="sceneName"/> async, fades back in.</summary>
    public void TransitionTo(string sceneName)
    {
        if (_isTransitioning) return;
        StartCoroutine(TransitionRoutine(sceneName));
    }

    // -------------------------------------------------------------------------

    IEnumerator TransitionRoutine(string sceneName)
    {
        _isTransitioning = true;

        if (fadeGroup != null)
            fadeGroup.blocksRaycasts = true;   // block input during the transition

        // 1. Fade to black.
        yield return StartCoroutine(Fade(0f, 1f));

        // 2. Show the loading panel and load the scene in the background.
        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        // Unity stalls async loads at 0.9 until activation is allowed.
        while (op.progress < 0.9f)
            yield return null;

        op.allowSceneActivation = true;
        yield return new WaitUntil(() => op.isDone);

        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        // 3. Fade back in.
        yield return StartCoroutine(Fade(1f, 0f));

        if (fadeGroup != null)
            fadeGroup.blocksRaycasts = false;
        _isTransitioning = false;
    }

    IEnumerator Fade(float from, float to)
    {
        if (fadeGroup == null) yield break;

        float elapsed = 0f;
        fadeGroup.alpha = from;

        // Unscaled so transitions still run if the game is paused (timeScale 0).
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }

        fadeGroup.alpha = to;
    }
}
