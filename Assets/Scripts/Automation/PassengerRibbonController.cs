using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Automation Mode's Passenger Status Ribbon: a pool of <see cref="PassengerChip"/>
/// (the same component Manual Mode uses) claimed on pickUp and hidden on dropOff,
/// keyed by ride id so multi-passenger pickups/dropoffs claim/release the right chips.
/// </summary>
public class PassengerRibbonController : MonoBehaviour
{
    [SerializeField] private PassengerChip[] chips;

    readonly Dictionary<int, PassengerChip> _byRideId = new Dictionary<int, PassengerChip>();

    public void Init()
    {
        if (chips == null) return;
        foreach (PassengerChip chip in chips)
            if (chip != null) chip.Hide();
        _byRideId.Clear();
    }

    /// <summary>Claims a free chip for a ride that just boarded.</summary>
    public void Claim(int rideId, string destLabel, Color tint)
    {
        if (chips == null || _byRideId.ContainsKey(rideId)) return;

        foreach (PassengerChip chip in chips)
        {
            if (chip == null || chip.InUse) continue;
            chip.Show($"Rider → {destLabel}", tint);
            _byRideId[rideId] = chip;
            return;
        }
    }

    /// <summary>Hides the chip for a ride that was just dropped off.</summary>
    public void Release(int rideId)
    {
        if (_byRideId.TryGetValue(rideId, out PassengerChip chip))
        {
            chip.Hide();
            _byRideId.Remove(rideId);
        }
    }

    /// <summary>Clears every chip — used on world reset / re-run.</summary>
    public void ReleaseAll()
    {
        if (chips != null)
            foreach (PassengerChip chip in chips)
                if (chip != null) chip.Hide();
        _byRideId.Clear();
    }
}
