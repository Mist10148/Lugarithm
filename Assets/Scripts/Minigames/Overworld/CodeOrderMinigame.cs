using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lightweight coding challenge for the overworld stations: a shuffled column of
/// program lines the player reorders with ↑/↓ to match the correct program, then
/// presses RUN to check. Concept-tied content comes from
/// <see cref="OverworldPuzzleLibrary"/> (sequencing, conditionals, lists, …).
///
/// Shares the look/flow of the drive-scene <see cref="CodeFixMinigame"/> but is
/// self-contained and reports through simple onSolved/onQuit callbacks so it slots
/// into the overworld station dispatch.
/// </summary>
public class CodeOrderMinigame : MonoBehaviour, IVerticalReorderTarget
{
    public const int MaxLines = 6;

    [Header("References")]
    [SerializeField] private GameObject root;
    [SerializeField] private MinigameResultsPanel resultsPanel;
    [SerializeField] private TMP_Text   titleLabel;
    [SerializeField] private TMP_Text   goalLabel;
    [SerializeField] private TMP_Text   feedbackLabel;
    [SerializeField] private TMP_Text[] cardLabels;        // up to MaxLines
    [SerializeField] private Image[]    cardBackgrounds;   // parallel to cardLabels
    [SerializeField] private Button[]   upButtons;
    [SerializeField] private Button[]   downButtons;
    [SerializeField] private VerticalReorderHandle[] dragHandles;
    [SerializeField] private Button     runButton;
    [SerializeField] private Button     quitButton;
    [SerializeField] private Button     hintButton;
    [SerializeField] private TMP_Text   hintLabel;
    [SerializeField] private RectTransform previewMarker;
    [SerializeField] private TMP_Text previewLabel;

    static readonly Color CardNormal = new Color(0.22f, 0.30f, 0.42f);
    static readonly Color CardWrong  = new Color(0.55f, 0.25f, 0.25f);
    static readonly Color Good       = new Color(0.45f, 0.85f, 0.45f);
    static readonly Color Bad        = new Color(0.92f, 0.45f, 0.40f);
    static readonly Color Neutral    = new Color(0.85f, 0.86f, 0.82f);

    Action _onSolved, _onQuit;
    readonly List<string> _order   = new List<string>();
    string[] _correct;
    int _count;
    int _mistakes;
    int _hintTier;
    int _lastWrongIndex = -1;
    string _title = "Code Order";
    readonly CodeRunHistory _runHistory = new CodeRunHistory();

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
            if (dragHandles != null && i < dragHandles.Length && dragHandles[i] != null)
            {
                dragHandles[i].index = i;
                dragHandles[i].owner = this;
            }
        }
        if (runButton  != null) runButton.onClick.AddListener(OnRun);
        if (quitButton != null) quitButton.onClick.AddListener(QuitOut);
        if (hintButton != null) hintButton.onClick.AddListener(OnHintRequested);
        else Debug.LogWarning("[CodeOrderMinigame] hintButton is not wired — hint UI will never appear.");
        if (root != null) root.SetActive(false);
    }

    void OnDestroy()
    {
        if (runButton  != null) runButton.onClick.RemoveListener(OnRun);
        if (quitButton != null) quitButton.onClick.RemoveListener(QuitOut);
        if (hintButton != null) hintButton.onClick.RemoveListener(OnHintRequested);
    }

    public void Begin(MinigameStationDef def, Action onSolved, Action onQuit)
    {
        _onSolved = onSolved;
        _onQuit = onQuit;
        _mistakes = 0;
        _hintTier = 0;
        _lastWrongIndex = -1;
        _title = def != null && !string.IsNullOrWhiteSpace(def.title) ? def.title : "Code Order";
        _runHistory.Clear();
        if (hintButton != null) hintButton.gameObject.SetActive(false);
        if (hintLabel != null) hintLabel.text = "";
        if (previewLabel != null) previewLabel.text = "Arrange valid Para, then RUN to preview it.";
        if (previewMarker != null) previewMarker.anchoredPosition = new Vector2(-190f, 0f);
        if (runButton != null) runButton.interactable = true;

        CodingPuzzle puzzle = OverworldPuzzleLibrary.GetCoding(def.id, def.concept);
        _correct = puzzle.orderedLines;
        _count = Mathf.Min(_correct.Length, cardLabels != null ? cardLabels.Length : _correct.Length);

        _order.Clear();
        for (int i = 0; i < _count; i++) _order.Add(_correct[i]);
        var rng = new System.Random();
        do { Shuffle(_order, rng); } while (_count > 1 && IsCorrect());

        if (titleLabel != null) titleLabel.text = _title;
        if (goalLabel  != null) goalLabel.text  = puzzle.goal;
        if (feedbackLabel != null) { feedbackLabel.text = "Use ↑ ↓ to order the program, then press RUN."; feedbackLabel.color = Neutral; }

        Refresh(-1);
        if (root != null) root.SetActive(true);
    }

    void Move(int index, int dir)
    {
        int target = index + dir;
        if (index < 0 || index >= _count || target < 0 || target >= _count) return;
        (_order[index], _order[target]) = (_order[target], _order[index]);
        Refresh(-1);
        if (feedbackLabel != null) { feedbackLabel.text = "Use ↑ ↓ to order the program, then press RUN."; feedbackLabel.color = Neutral; }
    }

    public void MoveCard(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= _count) return;
        toIndex = Mathf.Clamp(toIndex, 0, _count - 1);
        if (fromIndex == toIndex) return;
        string line = _order[fromIndex];
        _order.RemoveAt(fromIndex);
        _order.Insert(toIndex, line);
        Refresh(-1);
        if (feedbackLabel != null)
        {
            feedbackLabel.text = "Drag cards or use ↑ ↓, then press RUN.";
            feedbackLabel.color = Neutral;
        }
    }

    void OnRun()
    {
        CodeRunAttempt attempt = _runHistory.RecordStarted(CurrentSourceText(), "Code order");
        int wrong = FirstWrongIndex();
        if (wrong < 0)
        {
            Parser.Compile(CurrentSourceText(), out List<LangError> errors);
            if (errors.Count > 0)
            {
                if (feedbackLabel != null) { feedbackLabel.text = errors[0].ToString(); feedbackLabel.color = Bad; }
                _runHistory.Complete(attempt, false, "Parse error", 0, errors[0].ToString());
                return;
            }
            if (feedbackLabel != null) { feedbackLabel.text = "Program is valid — previewing the route…"; feedbackLabel.color = Good; }
            _runHistory.Complete(attempt, true, "Solved", _count, $"Solved in {_runHistory.Count} run(s).");
            if (runButton != null) runButton.interactable = false;
            StartCoroutine(PreviewThenFinish());
            return;
        }

        if (feedbackLabel != null) { feedbackLabel.text = $"Line {wrong + 1} is out of order — keep arranging."; feedbackLabel.color = Bad; }
        _runHistory.Complete(attempt, false, "Wrong order", wrong + 1, $"Line {wrong + 1} was out of order.");
        _mistakes++;
        _lastWrongIndex = wrong;
        Refresh(wrong);
        RevealHintAfterStruggle();
    }

    IEnumerator PreviewThenFinish()
    {
        if (previewMarker != null)
        {
            for (int i = 0; i < _count; i++)
            {
                if (previewLabel != null) previewLabel.text = _correct[i].Trim();
                Vector2 from = previewMarker.anchoredPosition;
                Vector2 to = new Vector2(Mathf.Lerp(-190f, 190f, (i + 1f) / _count), 0f);
                float elapsed = 0f;
                while (elapsed < 0.22f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    previewMarker.anchoredPosition = Vector2.Lerp(from, to, elapsed / 0.22f);
                    yield return null;
                }
            }
        }
        if (previewLabel != null) previewLabel.text = "Preview complete — the program reaches its goal.";
        yield return new WaitForSecondsRealtime(0.35f);
        Action callback = _onSolved;
        Cleanup();
        ShowCodeResults(callback);
    }

    void RevealHintAfterStruggle()
    {
        Debug.Log($"[CodeOrderMinigame] RevealHintAfterStruggle called — mistakes={_mistakes}, hintButton={(hintButton != null ? "wired" : "NULL")}");
        if (_mistakes < 2 || hintButton == null) return;
        hintButton.gameObject.SetActive(true);
        if (hintLabel != null && string.IsNullOrWhiteSpace(hintLabel.text))
            hintLabel.text = "Stuck? Ask for a hint and I will nudge the program order.";
    }

    public void OnHintRequested()
    {
        int tier = MinigameHintLibrary.ClampTier(_hintTier, MinigameHintLibrary.CodeOrderHints.Length);
        _hintTier = MinigameHintLibrary.ClampTier(_hintTier + 1, MinigameHintLibrary.CodeOrderHints.Length);
        if (hintLabel != null)
            hintLabel.text = MinigameHintLibrary.CodeOrderHint(tier, _lastWrongIndex);
    }

    void QuitOut()
    {
        Action cb = _onQuit;
        Cleanup();
        cb?.Invoke();
    }

    bool IsCorrect() => FirstWrongIndex() < 0;

    int FirstWrongIndex()
    {
        for (int i = 0; i < _count; i++)
            if (_order[i] != _correct[i]) return i;
        return -1;
    }

    void Refresh(int wrongIndex)
    {
        int slots = cardLabels != null ? cardLabels.Length : 0;
        for (int i = 0; i < slots; i++)
        {
            bool used = i < _count;
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

    void Cleanup()
    {
        _onSolved = null;
        _onQuit = null;
        if (hintButton != null) hintButton.gameObject.SetActive(false);
        if (hintLabel != null) hintLabel.text = "";
        if (previewLabel != null) previewLabel.text = "";
        if (root != null) root.SetActive(false);
    }

    string CurrentSourceText()
    {
        return CodeRunHistory.SourceFromLines(_order);
    }

    string ReferenceSourceText()
    {
        return CodeRunHistory.SourceFromLines(_correct);
    }

    void ShowCodeResults(Action onSolved)
    {
        if (resultsPanel == null)
        {
            onSolved?.Invoke();
            return;
        }

        string playerSource = CurrentSourceText();
        string referenceSource = ReferenceSourceText();
        int attemptCount = Mathf.Max(1, _runHistory.Count);
        var result = new MinigameResult
        {
            TimedOut = false,
            Mistakes = _mistakes,
            Score = Mathf.Max(10, 100 - 12 * _mistakes),
        };
        CodeAnalysis analysis = CodeAnalyticsService.Analyze(
            playerSource, referenceSource, _count, Mathf.Max(1, _count),
            Mathf.Max(0, attemptCount - 1), 0f, 0f, null, attemptCount);

        resultsPanel.Show("MINIGAME · Code", _title, result, analysis,
            onSolved, playerSource, referenceSource, _runHistory.Attempts);
        resultsPanel.StartCoroutine(FetchMentorFeedback(playerSource, referenceSource, analysis));
    }

    IEnumerator FetchMentorFeedback(string playerSource, string referenceSource, CodeAnalysis analysis)
    {
        AiRequest request = CodingMentorService.BuildRequest(
            "Town hub code order", "program ordering", analysis, playerSource, referenceSource,
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
