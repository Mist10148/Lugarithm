using System;
using System.Collections;
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
    [SerializeField] private MinigameResultsPanel resultsPanel;
    [SerializeField] private TMP_Text   titleLabel;
    [SerializeField] private TMP_Text   feedbackLabel;
    [SerializeField] private TMP_Text[] cardLabels;        // up to MaxSteps
    [SerializeField] private Image[]    cardBackgrounds;   // parallel to cardLabels
    [SerializeField] private Button[]   upButtons;         // parallel to cardLabels
    [SerializeField] private Button[]   downButtons;       // parallel to cardLabels
    [SerializeField] private Button     runButton;
    [SerializeField] private Button     hintButton;
    [SerializeField] private TMP_Text   hintLabel;
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
    int   _hintTier;
    int   _lastWrongIndex = -1;
    float _timer;
    bool  _running;
    readonly CodeRunHistory _runHistory = new CodeRunHistory();

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
        if (hintButton != null) hintButton.onClick.AddListener(OnHintRequested);
        if (root != null) root.SetActive(false);
    }

    void OnDestroy()
    {
        if (runButton != null) runButton.onClick.RemoveListener(OnRun);
        if (hintButton != null) hintButton.onClick.RemoveListener(OnHintRequested);
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
        _hintTier = 0;
        _lastWrongIndex = -1;
        _timer    = softTimerSeconds;
        _running  = true;
        _runHistory.Clear();
        if (hintButton != null) hintButton.gameObject.SetActive(false);
        if (hintLabel != null) hintLabel.text = "";

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

        CodeRunAttempt attempt = _runHistory.RecordStarted(CurrentSourceText(), "Repair order");
        int wrong = RepairProcedure.FirstWrongIndex(_order, _fault);
        if (wrong < 0)
        {
            _runHistory.Complete(attempt, true, "Solved", _count, $"Solved in {_runHistory.Count} run(s).");
            if (feedbackLabel != null) { feedbackLabel.text = "Fixed!  Back on the road."; feedbackLabel.color = FeedbackGood; }
            Finish(timedOut: false);
            return;
        }

        _runHistory.Complete(attempt, false, "Wrong order", wrong + 1, $"Step {wrong + 1} was out of order.");
        _mistakes++;
        _lastWrongIndex = wrong;
        if (feedbackLabel != null)
        {
            feedbackLabel.text  = $"Step {wrong + 1} is out of order — keep arranging.";
            feedbackLabel.color = FeedbackBad;
        }
        RefreshCards(wrongIndex: wrong);
        RevealHintAfterStruggle();
    }

    void RevealHintAfterStruggle()
    {
        if (_mistakes < 2 || hintButton == null) return;
        hintButton.gameObject.SetActive(true);
        if (hintLabel != null && string.IsNullOrWhiteSpace(hintLabel.text))
            hintLabel.text = "Stuck? Ask for a hint and I will point at the repair order without doing it for you.";
    }

    public void OnHintRequested()
    {
        int tier = MinigameHintLibrary.ClampTier(_hintTier, MinigameHintLibrary.RepairOrderHints.Length);
        _hintTier = MinigameHintLibrary.ClampTier(_hintTier + 1, MinigameHintLibrary.RepairOrderHints.Length);
        if (hintLabel != null)
            hintLabel.text = MinigameHintLibrary.RepairOrderHint(tier, _fault, _lastWrongIndex);
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
        if (hintButton != null) hintButton.gameObject.SetActive(false);
        if (hintLabel != null) hintLabel.text = "";
        if (root != null) root.SetActive(false);

        var result = new MinigameResult
        {
            TimedOut = timedOut,
            Mistakes = _mistakes,
            Score    = Mathf.Max(10, 100 - 15 * _mistakes - (timedOut ? 40 : 0)),
        };

        Action<MinigameResult> done = _onDone;
        _onDone = null;
        if (resultsPanel != null && _runHistory.Count > 0)
            ShowCodeResults(result, done);
        else
            done?.Invoke(result);
    }

    string CurrentSourceText()
    {
        return CodeRunHistory.SourceFromLines(_order);
    }

    string ReferenceSourceText()
    {
        return CodeRunHistory.SourceFromLines(RepairProcedure.Steps(_fault));
    }

    void ShowCodeResults(MinigameResult result, Action<MinigameResult> done)
    {
        string playerSource = CurrentSourceText();
        string referenceSource = ReferenceSourceText();
        int attemptCount = Mathf.Max(1, _runHistory.Count);
        float elapsed = Mathf.Max(0f, softTimerSeconds - _timer);
        CodeAnalysis analysis = CodeAnalyticsService.Analyze(
            playerSource, referenceSource, _count, Mathf.Max(1, _count),
            Mathf.Max(0, attemptCount - 1), elapsed, softTimerSeconds,
            null, attemptCount);

        resultsPanel.Show("MINIGAME · Code", RepairProcedure.Title(_fault), result, analysis,
            () => done?.Invoke(result), playerSource, referenceSource, _runHistory.Attempts);
        resultsPanel.StartCoroutine(FetchMentorFeedback(playerSource, referenceSource, analysis));
    }

    IEnumerator FetchMentorFeedback(string playerSource, string referenceSource, CodeAnalysis analysis)
    {
        AiRequest request = CodingMentorService.BuildRequest(
            "Repair order minigame", "sequencing", analysis, playerSource, referenceSource,
            Array.Empty<string>(), Array.Empty<string>(), _runHistory.Attempts,
            preserveAuthoredOptimal: true);
        AiResult response = null;
        yield return GeminiClient.Stream(request, null, completed => response = completed);

        MentorReview review = null;
        int lineCount = string.IsNullOrEmpty(playerSource) ? 0 : playerSource.Replace("\r", "").Split('\n').Length;
        if (response == null || !response.Success ||
            !CodingMentorService.TryParseAndValidate(response.Text, referenceSource,
                Array.Empty<string>(), Array.Empty<string>(), lineCount, out review,
                preserveAuthoredOptimal: true))
            review = CodingMentorService.Fallback(referenceSource, analysis);
        if (resultsPanel != null) resultsPanel.SetMentorReview(review);
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
