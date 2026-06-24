using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manual Mode breakdown repair (non-code, engine fault): a target sequence of
/// five colored parts is shown; click the matching parts on the 3×3 grid in
/// order before the soft timer empties. Expiry only costs score — the run
/// always continues (PRD §5.4). The dispatcher reaches for this on an engine
/// fault when the random interface roll lands on "non-code".
/// </summary>
public class PatternMatchMinigame : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject root;
    [SerializeField] private MinigameResultsPanel resultsPanel;
    [SerializeField] private TMP_Text   titleLabel;
    [SerializeField] private TMP_Text   feedbackLabel;
    [SerializeField] private Image[]    targetSlots;   // 5
    [SerializeField] private Button[]   gridButtons;   // 9
    [SerializeField] private Image      timerFill;

    [Header("Timing")]
    [SerializeField] private float softTimerSeconds = 30f;

    static readonly Color[] PartColors =
    {
        new Color(0.90f, 0.30f, 0.25f),   // gasket red
        new Color(0.25f, 0.55f, 0.90f),   // coolant blue
        new Color(0.95f, 0.78f, 0.25f),   // brass fitting
        new Color(0.40f, 0.80f, 0.40f),   // fan-belt green
        new Color(0.75f, 0.45f, 0.85f),   // wire violet
        new Color(0.90f, 0.55f, 0.20f),   // filter orange
    };

    static readonly Color FeedbackGood = new Color(0.45f, 0.85f, 0.45f);
    static readonly Color FeedbackBad  = new Color(0.92f, 0.45f, 0.40f);

    Action<MinigameResult> _onDone;
    string _title;
    readonly List<int> _sequence = new List<int>();   // indices into PartColors
    int[] _gridParts;
    int   _progress;
    int   _mistakes;
    float _timer;
    bool  _running;

    // -------------------------------------------------------------------------

    void Awake()
    {
        for (int i = 0; i < gridButtons.Length; i++)
        {
            int index = i;
            if (gridButtons[i] != null)
                gridButtons[i].onClick.AddListener(() => OnGridClicked(index));
        }

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
    public void Show(int seed, Action<MinigameResult> onDone) => Show(seed, null, onDone);

    /// <summary>As <see cref="Show(int, Action{MinigameResult})"/> with a custom
    /// fault headline (e.g. "ENGINE OVERHEAT" vs "BELT SNAPPED").</summary>
    public void Show(int seed, string title, Action<MinigameResult> onDone)
    {
        _onDone    = onDone;
        _title     = title;
        _progress  = 0;
        _mistakes  = 0;
        _timer     = softTimerSeconds;
        _running   = true;

        var rng = new System.Random(seed);

        // Target sequence: five parts drawn from the palette (no repeats).
        _sequence.Clear();
        var pool = new List<int> { 0, 1, 2, 3, 4, 5 };
        for (int i = 0; i < 5; i++)
        {
            int pick = rng.Next(pool.Count);
            _sequence.Add(pool[pick]);
            pool.RemoveAt(pick);
        }

        // Grid: the five needed parts + four random decoys, shuffled.
        _gridParts = new int[gridButtons.Length];
        var bag = new List<int>(_sequence);
        while (bag.Count < gridButtons.Length)
            bag.Add(rng.Next(PartColors.Length));
        for (int i = bag.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (bag[i], bag[j]) = (bag[j], bag[i]);
        }
        for (int i = 0; i < _gridParts.Length; i++)
            _gridParts[i] = bag[i];

        RefreshVisuals();
        if (titleLabel != null)
            titleLabel.text = string.IsNullOrEmpty(_title)
                ? "ENGINE TROUBLE!  Fit the parts in order:"
                : _title;
        if (feedbackLabel != null) feedbackLabel.text = "";
        if (root          != null) root.SetActive(true);
    }

    // -------------------------------------------------------------------------

    void RefreshVisuals()
    {
        for (int i = 0; i < targetSlots.Length; i++)
        {
            if (targetSlots[i] == null) continue;
            targetSlots[i].color = PartColors[_sequence[i]];

            // Completed slots dim out.
            if (i < _progress)
                targetSlots[i].color *= 0.35f;
        }

        for (int i = 0; i < gridButtons.Length; i++)
        {
            if (gridButtons[i] == null) continue;
            Image face = gridButtons[i].targetGraphic as Image;
            if (face != null) face.color = PartColors[_gridParts[i]];
        }
    }

    void OnGridClicked(int index)
    {
        if (!_running) return;

        if (_gridParts[index] == _sequence[_progress])
        {
            _progress++;
            if (feedbackLabel != null)
            {
                feedbackLabel.text  = _progress >= _sequence.Count ? "Fixed!" : "Fitted!";
                feedbackLabel.color = FeedbackGood;
            }
            RefreshVisuals();

            if (_progress >= _sequence.Count)
                Finish(timedOut: false);
        }
        else
        {
            _mistakes++;
            if (feedbackLabel != null)
            {
                feedbackLabel.text  = "Wrong part!";
                feedbackLabel.color = FeedbackBad;
            }
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
        if (resultsPanel != null)
            resultsPanel.Show("MINIGAME · Non-code", "ENGINE REPAIR", result, null, () => done?.Invoke(result));
        else
            done?.Invoke(result);
    }
}
