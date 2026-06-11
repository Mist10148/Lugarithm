using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One slot in the Passenger Status Ribbon (top-left HUD): passenger name,
/// destination, and a patience bar while their fare is being settled.
/// A fixed pool of these is built into the scene; <see cref="ManualHudController"/>
/// claims and releases them.
/// </summary>
public class PassengerChip : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image    background;
    [SerializeField] private Image    portrait;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Image    patienceFill;

    public bool InUse { get; private set; }

    Coroutine _flashRoutine;

    // -------------------------------------------------------------------------

    public void Show(string text, Color tint)
    {
        InUse = true;
        gameObject.SetActive(true);

        if (label    != null) label.text     = text;
        if (portrait != null) portrait.color = tint;
        SetPatience(-1f);
    }

    /// <summary>0..1 fills the bar; negative hides it (fare settled / not due).</summary>
    public void SetPatience(float fraction)
    {
        if (patienceFill == null) return;

        bool visible = fraction >= 0f;
        patienceFill.transform.parent.gameObject.SetActive(visible);
        if (visible)
        {
            patienceFill.fillAmount = Mathf.Clamp01(fraction);
            patienceFill.color = Color.Lerp(new Color(0.9f, 0.25f, 0.2f),
                                            new Color(0.35f, 0.85f, 0.35f),
                                            Mathf.Clamp01(fraction));
        }
    }

    public void Flash()
    {
        if (_flashRoutine != null) StopCoroutine(_flashRoutine);
        if (isActiveAndEnabled)
            _flashRoutine = StartCoroutine(FlashRoutine());
    }

    public void Hide()
    {
        InUse = false;
        if (_flashRoutine != null) StopCoroutine(_flashRoutine);
        _flashRoutine = null;
        gameObject.SetActive(false);
    }

    // -------------------------------------------------------------------------

    IEnumerator FlashRoutine()
    {
        if (background == null) yield break;

        Color baseColor = background.color;
        Color hot = new Color(0.95f, 0.65f, 0.15f, baseColor.a);

        for (int i = 0; i < 4; i++)
        {
            background.color = hot;
            yield return new WaitForSeconds(0.18f);
            background.color = baseColor;
            yield return new WaitForSeconds(0.14f);
        }
    }
}
