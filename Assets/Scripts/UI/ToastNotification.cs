using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Transient on-screen notification ("Journal page recovered", etc.).
/// Fades in, holds, fades out, then deactivates itself.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class ToastNotification : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text   messageLabel;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Timing (seconds)")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float holdDuration = 2.0f;

    private Coroutine _routine;

    // -------------------------------------------------------------------------

    void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    // -------------------------------------------------------------------------

    /// <summary>Displays a message that auto-dismisses after the hold duration.</summary>
    public void Show(string message)
    {
        if (messageLabel != null) messageLabel.text = message;

        gameObject.SetActive(true);

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(ShowRoutine());
    }

    IEnumerator ShowRoutine()
    {
        yield return Fade(0f, 1f);
        yield return new WaitForSecondsRealtime(holdDuration);
        yield return Fade(1f, 0f);
        gameObject.SetActive(false);
    }

    IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        canvasGroup.alpha = from;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = to;
    }
}
