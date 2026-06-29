using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// The interactive Coin Drawer (PRD §5.2): when a passenger pays, their fare
/// appears here. The player taps denominations to assemble the exact change
/// and confirms before the patience timer empties. Wrong totals can be
/// retried; a timeout settles for half credit. Driving stays live throughout —
/// that tension is the mechanic.
/// </summary>
public class CoinDrawerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text   headerLabel;
    [SerializeField] private TMP_Text   selectedLabel;
    [SerializeField] private Image      patienceFill;
    [SerializeField] private Button[]   denominationButtons;   // labels define values
    [SerializeField] private Button     clearButton;
    [SerializeField] private Button     giveButton;
    [SerializeField] private RectTransform window;             // shaken on mistakes

    [Header("Timing")]
    [SerializeField] private float patienceSeconds = 20f;

    /// <summary>Values matching <see cref="denominationButtons"/> one-to-one.</summary>
    static readonly int[] ButtonValues = { 1, 5, 10, 20, 20, 50, 100 };

    readonly Queue<ManualPassenger> _queue = new Queue<ManualPassenger>();
    readonly List<int> _selected = new List<int>();

    ManualPassenger _current;
    float _patience;
    DriveScoreTracker _tracker;
    ToastNotification _toast;
    Coroutine _shake;

    /// <summary>True while any fare is pending or being handled.</summary>
    public bool Busy => _current != null || _queue.Count > 0;

    // -------------------------------------------------------------------------

    public void Init(DriveScoreTracker tracker, ToastNotification toast)
    {
        _tracker = tracker;
        _toast   = toast;

        for (int i = 0; i < denominationButtons.Length; i++)
        {
            int value = ButtonValues[Mathf.Min(i, ButtonValues.Length - 1)];
            Button button = denominationButtons[i];
            if (button != null)
                button.onClick.AddListener(() => OnDenomination(value));
        }

        if (clearButton != null) clearButton.onClick.AddListener(OnClear);
        if (giveButton  != null) giveButton.onClick.AddListener(OnGive);
        if (root        != null) root.SetActive(false);
    }

    // -------------------------------------------------------------------------
    // Queue

    /// <summary>A boarding passenger hands over their tender.</summary>
    public void Enqueue(ManualPassenger passenger)
    {
        _queue.Enqueue(passenger);
        if (_current == null)
            NextFare();
    }

    /// <summary>Drops a passenger's pending fare (they left the jeepney).</summary>
    public void Cancel(ManualPassenger passenger)
    {
        if (_current == passenger)
        {
            _current = null;
            NextFare();
            return;
        }

        if (_queue.Contains(passenger))
        {
            var keep = new List<ManualPassenger>(_queue);
            keep.Remove(passenger);
            _queue.Clear();
            foreach (ManualPassenger p in keep) _queue.Enqueue(p);
        }
    }

    void NextFare()
    {
        _selected.Clear();

        while (_queue.Count > 0 && _current == null)
        {
            ManualPassenger candidate = _queue.Dequeue();
            if (!candidate.FareSettled) _current = candidate;
        }

        if (_current == null)
        {
            if (root != null) root.SetActive(false);
            return;
        }

        _patience = patienceSeconds;
        if (root != null) root.SetActive(true);
        RefreshLabels();
    }

    // -------------------------------------------------------------------------

    void Update()
    {
        if (_current == null) return;

        _patience -= Time.deltaTime;
        if (patienceFill != null)
            patienceFill.fillAmount = Mathf.Clamp01(_patience / patienceSeconds);
        if (_current.Chip != null)
            _current.Chip.SetPatience(_patience / patienceSeconds);

        if (_patience <= 0f)
            ResolveTimeout();
    }

    void RefreshLabels()
    {
        int change = FareMath.ChangeFor(_current.Tender, _current.Fare);

        if (headerLabel != null)
            headerLabel.text = change > 0
                ? $"{_current.Name} paid ₱{_current.Tender} for a ₱{_current.Fare} fare\nGive ₱{change} change"
                : $"{_current.Name} paid exact: ₱{_current.Fare}\nPress GIVE to accept";

        if (selectedLabel != null)
        {
            int total = 0;
            foreach (int v in _selected) total += v;
            selectedLabel.text = $"Selected: ₱{total}";
        }
    }

    // -------------------------------------------------------------------------
    // Buttons

    void OnDenomination(int value)
    {
        if (_current == null) return;
        _selected.Add(value);
        RefreshLabels();
    }

    void OnClear()
    {
        _selected.Clear();
        RefreshLabels();
    }

    void OnGive()
    {
        if (_current == null) return;

        int change = FareMath.ChangeFor(_current.Tender, _current.Fare);

        if (FareMath.ValidateChange(_selected, change))
        {
            _tracker.FareExact();
            CreditFare(_current.Fare);
            if (_toast != null) _toast.Show($"Fare collected: ₱{_current.Fare}  ✓");
            SettleCurrent();
        }
        else
        {
            _tracker.FareWrong();
            _selected.Clear();
            RefreshLabels();
            if (_shake != null) StopCoroutine(_shake);
            if (window != null && isActiveAndEnabled) _shake = StartCoroutine(Shake());
        }
    }

    void ResolveTimeout()
    {
        _tracker.FareTimedOut();
        CreditFare(_current.Fare / 2);
        if (_toast != null)
            _toast.Show($"{_current.Name} gave up waiting — half fare (₱{_current.Fare / 2})");
        SettleCurrent();
    }

    void SettleCurrent()
    {
        _current.FareSettled = true;
        if (_current.Chip != null) _current.Chip.SetPatience(-1f);
        _current = null;
        NextFare();
    }

    static void CreditFare(int amount)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.EarnCurrency(amount);
    }

    IEnumerator Shake()
    {
        Vector2 basePos = window.anchoredPosition;
        for (int i = 0; i < 6; i++)
        {
            window.anchoredPosition = basePos + new Vector2(Random.Range(-8f, 8f), Random.Range(-5f, 5f));
            yield return new WaitForSeconds(0.04f);
        }
        window.anchoredPosition = basePos;
    }
}
