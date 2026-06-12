using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manual Mode breakdown repair (code-based, both faults): a shuffled column of
/// instruction "blocks" the player reorders with ↑/↓ to match the correct
/// repair procedure (<see cref="RepairProcedure"/>), then presses RUN to
/// validate. Failed runs dent the score; the run always continues. Self
/// contained — it reuses the block-card look but never touches the grid sim, so
/// it can live in either drive scene. The dispatcher reaches for this whenever
/// the random interface roll lands on "code".
/// </summary>
public class CodeFixMinigame : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text   titleLabel;
    [SerializeField] private TMP_Text   feedbackLabel;
    [SerializeField] private TMP_Text[] cardLabels;        // up to MaxSteps
    [SerializeField] private Image[]    cardBackgrounds;   // parallel to cardLabels
    [SerializeField] private Button[]   upButtons;         // parallel to cardLabels
    [SerializeField] private Button[]   downButtons;       // parallel to cardLabels
    [SerializeField] private Button     runButton;
    [SerializeField] private Image      timerFill;

    [Header("Timing")]
    [SerializeField] private float softTimerSeconds = 45f;

    public const int MaxSteps = 6;

    static readonly Color CardNormal     = new Color(0.22f, 0.30f, 0.42f);
    static readonly Color CardWrong      = new Color(0.55f, 0.25f, 0.25f);
    static readonly Color FeedbackGood   = new Color(0.45f, 0.85f, 0.45f);
    static readonly Color FeedbackBad    = new Color(0.92f, 0.45f, 0.40f);
    static readonly Color FeedbackNeutral = new Color(0.85f, 0.86f, 0.82f);

    Action<MinigameResult> _onDone;
    BreakdownFault _fault;
    readonly List<string> _order = new List<string>();
    int   _count;
    int   _mistakes;
    float _timer;
    bool  _running;

    // -------------------------------------------------------------------------

    void Awake()
    {
        int slots = cardLabels != null ? cardLabels.Length : 0;
        for (int i = 0; i < slots; i++)
        {
            int idx = i;
            if (upButtons   != null && i < upButtons.Length   && upButtons[i]   != null)
                upButtons[i].onClick.AddListener(() => Move(idx, -1));
            if (downButtons != null && i < downButtons.Length && downButtons[i] != null)
                downButtons[i].onClick.AddListener(() => Move(idx, +1));
        }
        if (runButton != null) runButton.onClick.AddListener(OnRun);
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
    public void Show(BreakdownFault fault, int seed, Action<MinigameResult> onDone)
    {
        _onDone   = onDone;
        _fault    = fault;
        _mistakes = 0;
        _timer    = softTimerSeconds;
        _running  = true;

        var rng = new System.Random(seed);

        string[] correct = RepairProcedure.Steps(fault);
        _count = Mathf.Min(correct.Length, cardLabels != null ? cardLabels.Length : correct.Length);

        _order.Clear();
        for (int i = 0; i < _count; i++) _order.Add(correct[i]);
        do { Shuffle(_order, rng); }
        while (_count > 1 && RepairProcedure.IsCorrect(_order, _fault));

        if (titleLabel != null) titleLabel.text = RepairProcedure.Title(fault);
        if (feedbackLabel != null)
        {
            feedbackLabel.text  = "Use ↑ ↓ to order the steps, then press RUN.";
            feedbackLabel.color = FeedbackNeutral;
        }

        RefreshCards(wrongIndex: -1);
        if (root != null) root.SetActive(true);
    }

    // -------------------------------------------------------------------------

    void Move(int index, int dir)
    {
        if (!_running) return;

        int target = index + dir;
        if (index < 0 || index >= _count || target < 0 || target >= _count) return;

        (_order[index], _order[target]) = (_order[target], _order[index]);
        RefreshCards(wrongIndex: -1);

        if (feedbackLabel != null)
        {
            feedbackLabel.text  = "Use ↑ ↓ to order the steps, then press RUN.";
            feedbackLabel.color = FeedbackNeutral;
        }
    }

    void OnRun()
    {
        if (!_running) return;

        int wrong = RepairProcedure.FirstWrongIndex(_order, _fault);
        if (wrong < 0)
        {
            if (feedbackLabel != null) { feedbackLabel.text = "Fixed!  Back on the road."; feedbackLabel.color = FeedbackGood; }
            Finish(timedOut: false);
            return;
        }

        _mistakes++;
        if (feedbackLabel != null)
        {
            feedbackLabel.text  = $"Step {wrong + 1} is out of order — keep arranging.";
            feedbackLabel.color = FeedbackBad;
        }
        RefreshCards(wrongIndex: wrong);
    }

    // -------------------------------------------------------------------------

    void RefreshCards(int wrongIndex)
    {
        int slots = cardLabels != null ? cardLabels.Length : 0;
        for (int i = 0; i < slots; i++)
        {
            bool used = i < _count;

            // cardBackgrounds[i] is the row's own background image, so toggling
            // its GameObject hides the whole card (label + arrows included).
            if (cardBackgrounds != null && i < cardBackgrounds.Length && cardBackgrounds[i] != null)
                cardBackgrounds[i].gameObject.SetActive(used);

            if (!used) continue;

            if (cardLabels[i] != null) cardLabels[i].text = _order[i];
            if (cardBackgrounds != null && i < cardBackgrounds.Length && cardBackgrounds[i] != null)
                cardBackgrounds[i].color = i == wrongIndex ? CardWrong : CardNormal;

            if (upButtons   != null && i < upButtons.Length   && upButtons[i]   != null)
                upButtons[i].interactable = i > 0;
            if (downButtons != null && i < downButtons.Length && downButtons[i] != null)
                downButtons[i].interactable = i < _count - 1;
        }
    }

    void Finish(bool timedOut)
    {
        _running = false;
        if (root != null) root.SetActive(false);

        var result = new MinigameResult
        {
            TimedOut = timedOut,
            Mistakes = _mistakes,
            Score    = Mathf.Max(10, 100 - 15 * _mistakes - (timedOut ? 40 : 0)),
        };

        Action<MinigameResult> done = _onDone;
        _onDone = null;
        done?.Invoke(result);
    }

    static void Shuffle(List<string> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
