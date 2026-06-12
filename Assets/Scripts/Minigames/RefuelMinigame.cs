using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manual Mode breakdown repair (non-code, fuel fault): tap PUMP to raise the
/// fuel needle into the highlighted target band, then DONE. Over/under-filling
/// only dents the score (<see cref="RefuelMath"/>) — the run always continues.
/// The dispatcher reaches for this on a fuel fault when the random interface
/// roll lands on "non-code".
/// </summary>
public class RefuelMinigame : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject    root;
    [SerializeField] private TMP_Text      titleLabel;
    [SerializeField] private TMP_Text      feedbackLabel;
    [SerializeField] private Image         tankFill;   // vertical Filled image, 0..1
    [SerializeField] private RectTransform bandZone;   // child of the tank; Y anchors set to [lo,hi]
    [SerializeField] private Button        pumpButton;
    [SerializeField] private Button        doneButton;
    [SerializeField] private Image         timerFill;

    [Header("Timing")]
    [SerializeField] private float softTimerSeconds = 30f;

    static readonly Color FeedbackGood = new Color(0.45f, 0.85f, 0.45f);
    static readonly Color FeedbackBad  = new Color(0.92f, 0.45f, 0.40f);

    Action<MinigameResult> _onDone;
    System.Random _rng;
    float _fill;
    float _bandLo, _bandHi;
    float _tap;
    float _timer;
    bool  _running;

    // -------------------------------------------------------------------------

    void Awake()
    {
        if (pumpButton != null) pumpButton.onClick.AddListener(OnPump);
        if (doneButton != null) doneButton.onClick.AddListener(OnDone);
        if (root != null) root.SetActive(false);
    }

    void Update()
    {
        if (!_running) return;

        _timer -= Time.deltaTime;
        if (timerFill != null)
            timerFill.fillAmount = Mathf.Clamp01(_timer / softTimerSeconds);

        if (_timer <= 0f)
            Finish(timedOut: true);
    }

    // -------------------------------------------------------------------------

    /// <summary>Opens the minigame; <paramref name="onDone"/> fires exactly once.</summary>
    public void Show(int seed, Action<MinigameResult> onDone)
    {
        _onDone  = onDone;
        _rng     = new System.Random(seed);
        _timer   = softTimerSeconds;
        _running = true;

        RefuelMath.Target(_rng, out _bandLo, out _bandHi);
        _fill = RefuelMath.StartFill(_rng);
        _tap  = RefuelMath.TapAmount(_rng);

        if (bandZone != null)
        {
            bandZone.anchorMin = new Vector2(bandZone.anchorMin.x, _bandLo);
            bandZone.anchorMax = new Vector2(bandZone.anchorMax.x, _bandHi);
            bandZone.offsetMin = new Vector2(bandZone.offsetMin.x, 0f);
            bandZone.offsetMax = new Vector2(bandZone.offsetMax.x, 0f);
        }

        if (titleLabel != null) titleLabel.text = "OUT OF FUEL!  Pump up to the green band, then DONE:";
        if (feedbackLabel != null) { feedbackLabel.text = ""; feedbackLabel.color = FeedbackGood; }

        RefreshFill();
        if (root != null) root.SetActive(true);
    }

    // -------------------------------------------------------------------------

    void OnPump()
    {
        if (!_running) return;

        _fill = Mathf.Clamp01(_fill + _tap);
        _tap  = RefuelMath.TapAmount(_rng);   // re-roll so it can't be perfectly counted
        RefreshFill();

        if (feedbackLabel != null)
        {
            bool over = _fill > _bandHi;
            feedbackLabel.text  = over ? "Overfilling — stop!" : "Keep pumping…";
            feedbackLabel.color = over ? FeedbackBad : FeedbackGood;
        }
    }

    void OnDone()
    {
        if (!_running) return;
        Finish(timedOut: false);
    }

    void RefreshFill()
    {
        if (tankFill != null) tankFill.fillAmount = _fill;
    }

    void Finish(bool timedOut)
    {
        _running = false;
        if (root != null) root.SetActive(false);

        var result = new MinigameResult
        {
            TimedOut = timedOut,
            Mistakes = 0,
            Score    = RefuelMath.ScoreFor(_fill, _bandLo, _bandHi, timedOut),
        };

        Action<MinigameResult> done = _onDone;
        _onDone = null;
        done?.Invoke(result);
    }
}
