using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manual Mode HUD: speedometer needle, fuel bar, currency counter, and the
/// Passenger Status Ribbon (a pool of <see cref="PassengerChip"/>s).
/// </summary>
public class ManualHudController : MonoBehaviour
{
    [Header("Dashboard")]
    [SerializeField] private RectTransform speedNeedle;
    [SerializeField] private TMP_Text      speedValueLabel;
    [SerializeField] private Image         fuelFill;
    [SerializeField] private TMP_Text      currencyLabel;

    [Header("Passenger Ribbon")]
    [SerializeField] private PassengerChip[] chips;

    [Header("Needle sweep (degrees)")]
    [SerializeField] private float needleMinAngle = 115f;
    [SerializeField] private float needleMaxAngle = -115f;

    JeepneyController _jeepney;
    int _shownCurrency = int.MinValue;
    int _shownDebt = int.MinValue;
    int _shownKph = int.MinValue;
    float _needleAngle;

    // -------------------------------------------------------------------------

    public void Init(JeepneyController jeepney)
    {
        _jeepney = jeepney;
        _needleAngle = needleMinAngle;

        if (chips != null)
            foreach (PassengerChip chip in chips)
                if (chip != null) chip.Hide();
    }

    void Update()
    {
        if (_jeepney != null)
        {
            if (speedNeedle != null)
            {
                float target = Mathf.Lerp(needleMinAngle, needleMaxAngle, _jeepney.CurrentSpeed01);
                // Frame-rate-independent ease so the needle settles instead of jittering.
                _needleAngle = Mathf.Lerp(_needleAngle, target, 1f - Mathf.Exp(-8f * Time.deltaTime));
                speedNeedle.localRotation = Quaternion.Euler(0f, 0f, _needleAngle);
            }

            if (speedValueLabel != null)
            {
                int kph = SpeedGauge.ToKph(_jeepney.CurrentSpeed);
                if (kph != _shownKph)
                {
                    _shownKph = kph;
                    speedValueLabel.text = $"{kph} km/h";
                }
            }

            if (fuelFill != null)
            {
                fuelFill.fillAmount = _jeepney.Fuel01;
                fuelFill.color = _jeepney.Fuel01 > 0.25f
                    ? new Color(0.95f, 0.65f, 0.15f)
                    : new Color(0.9f, 0.2f, 0.15f);
            }
        }

        int pending = GameManager.Instance != null ? GameManager.Instance.PendingCurrency : 0;
        int saved   = SaveSystem.Current != null ? SaveSystem.Current.currency : 0;
        int debt    = SaveSystem.Current != null ? SaveSystem.Current.debt : 0;
        int wallet  = saved + pending;
        if ((wallet != _shownCurrency || debt != _shownDebt) && currencyLabel != null)
        {
            _shownCurrency = wallet;
            _shownDebt     = debt;
            currencyLabel.text = debt > 0 ? $"₱ {wallet}  debt -{debt}" : $"₱ {wallet}";
        }
    }

    // -------------------------------------------------------------------------
    // Ribbon

    /// <summary>Claims a free chip for a boarding passenger (null if the ribbon is full).</summary>
    public PassengerChip ClaimChip(string text, Color tint)
    {
        if (chips == null) return null;

        foreach (PassengerChip chip in chips)
        {
            if (chip == null || chip.InUse) continue;
            chip.Show(text, tint);
            return chip;
        }

        return null;
    }
}
