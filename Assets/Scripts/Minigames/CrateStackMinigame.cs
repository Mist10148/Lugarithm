using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Oton town gate (non-code Mini-Game 2): reorder a column of market crates with
/// ↑/↓ so the heaviest sits at the bottom (<see cref="CrateStackPuzzle"/>). Must
/// be solved to advance — retries are free and the soft timer only dents the
/// score. Reuses the shared <see cref="MinigameResult"/> contract.
/// </summary>
public class CrateStackMinigame : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject root;
    [SerializeField] private MinigameResultsPanel resultsPanel;
    [SerializeField] private TMP_Text   titleLabel;
    [SerializeField] private TMP_Text   feedbackLabel;
    [SerializeField] private TMP_Text[] cardLabels;        // top → bottom
    [SerializeField] private Image[]    cardBackgrounds;
    [SerializeField] private Button[]   upButtons;         // move a crate up (toward the top)
    [SerializeField] private Button[]   downButtons;       // move a crate down (toward the bottom)
    [SerializeField] private Image      timerFill;

    [Header("Timing")]
    [SerializeField] private float softTimerSeconds = 60f;

    public const int Slots = 5;

    static readonly Color CardCrate   = new Color(0.45f, 0.33f, 0.20f);   // crate brown
    static readonly Color GoodColor   = new Color(0.45f, 0.85f, 0.45f);
    static readonly Color WarnColor   = new Color(0.92f, 0.45f, 0.40f);
    static readonly Color NeutralCol  = new Color(0.85f, 0.86f, 0.82f);

    Action<MinigameResult> _onDone;
    CrateStackPuzzle _puzzle;
    float _timer;
    bool  _running;
    bool  _timedOut;

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
        if (root != null) root.SetActive(false);
    }

    void Update()
    {
        if (!_running || _timedOut) return;

        _timer -= Time.deltaTime;
        if (timerFill != null) timerFill.fillAmount = Mathf.Clamp01(_timer / softTimerSeconds);

        if (_timer <= 0f)
        {
            _timedOut = true;
            if (feedbackLabel != null)
            {
                feedbackLabel.text  = "Out of time — finish stacking to continue (score dented).";
                feedbackLabel.color = WarnColor;
            }
        }
    }

    // -------------------------------------------------------------------------

    /// <summary>Opens the puzzle; <paramref name="onDone"/> fires once it's solved.</summary>
    public void Show(int seed, Action<MinigameResult> onDone)
    {
        _onDone   = onDone;
        _timer    = softTimerSeconds;
        _running  = true;
        _timedOut = false;

        int max = cardLabels != null ? cardLabels.Length : Slots;
        _puzzle = new CrateStackPuzzle(max, seed);

        if (titleLabel != null) titleLabel.text = "OTON MARKET — stack the crates heaviest at the bottom:";
        if (feedbackLabel != null)
        {
            feedbackLabel.text  = "Use ↑ ↓ to reorder. Heaviest crate on the bottom.";
            feedbackLabel.color = NeutralCol;
        }

        Refresh();
        if (root != null) root.SetActive(true);
    }

    // -------------------------------------------------------------------------

    void Move(int index, int dir)
    {
        if (!_running || _puzzle == null) return;

        if (_puzzle.Move(index, dir))
        {
            Refresh();
            if (_puzzle.IsSolved()) Finish();
        }
    }

    void Refresh()
    {
        int slots = cardLabels != null ? cardLabels.Length : 0;
        for (int i = 0; i < slots; i++)
        {
            bool used = _puzzle != null && i < _puzzle.Count;

            if (cardBackgrounds != null && i < cardBackgrounds.Length && cardBackgrounds[i] != null)
                cardBackgrounds[i].gameObject.SetActive(used);

            if (!used) continue;

            if (cardLabels[i] != null) cardLabels[i].text = $"Crate  ·  {_puzzle.Order[i]} kg";
            if (cardBackgrounds[i] != null) cardBackgrounds[i].color = CardCrate;

            if (upButtons   != null && i < upButtons.Length   && upButtons[i]   != null)
                upButtons[i].interactable = i > 0;
            if (downButtons != null && i < downButtons.Length && downButtons[i] != null)
                downButtons[i].interactable = i < _puzzle.Count - 1;
        }
    }

    void Finish()
    {
        _running = false;
        if (feedbackLabel != null) { feedbackLabel.text = "Stacked — the cart is balanced!"; feedbackLabel.color = GoodColor; }
        if (root != null) root.SetActive(false);

        var result = new MinigameResult
        {
            TimedOut = _timedOut,
            Mistakes = 0,
            Score    = _timedOut ? 60 : 100,
        };

        Action<MinigameResult> done = _onDone;
        _onDone = null;
        if (resultsPanel != null)
            resultsPanel.Show("MINIGAME · Non-code", "CRATE STACK", result, null, () => done?.Invoke(result));
        else
            done?.Invoke(result);
    }
}
