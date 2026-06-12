using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One passenger riding the jeepney in Manual Mode.</summary>
public class ManualPassenger
{
    public string Name;
    public int    OriginStop;
    public int    DestStop;
    public string DestName;
    public int    Fare;
    public int    Tender;
    public Color  Tint;

    public bool FareSettled;
    public bool ParaCalled;     // "Para!" toast already shown for the upcoming stop
    public PassengerChip Chip;  // HUD ribbon chip assigned to this passenger
}

/// <summary>
/// Manual Mode passenger flow: boarding at stops, fare hand-offs into the
/// Coin Drawer, "Para!" drop-off calls, drop-offs, and missed-stop penalties.
/// Driven by <see cref="ManualDriveController"/>, which calls
/// <see cref="Init"/> and forwards Update ticks.
/// </summary>
public class PassengerManager : MonoBehaviour
{
    [Header("Boarding")]
    [SerializeField] private float stopSpeedThreshold = 0.6f;
    [SerializeField] private float stopHoldSeconds    = 0.5f;
    [SerializeField] private float boardSeconds       = 0.55f;

    [Header("Para! call")]
    [SerializeField] private float paraDistance = 14f;

    static readonly string[] PeepNames =
    {
        "Lito", "Nene", "Caloy", "Ising", "Tonyo", "Day", "Jun-Jun", "Maring",
        "Berto", "Tina", "Pidoy", "Luz",
    };

    // Wired by Init
    JeepneyController   _jeepney;
    CoinDrawerController _drawer;
    ManualHudController _hud;
    ToastNotification   _toast;
    DriveScoreTracker   _tracker;
    ManualRouteDefinition _route;
    RouteContext        _ctx;
    System.Random       _rng;

    readonly List<ManualPassenger> _aboard = new List<ManualPassenger>();
    readonly Dictionary<StopZone, float> _holdTimers = new Dictionary<StopZone, float>();
    readonly HashSet<StopZone> _servicedThisVisit = new HashSet<StopZone>();

    bool _busyServicing;
    int  _nameCursor;

    public int  AboardCount => _aboard.Count;
    public bool IsServicing => _busyServicing;

    /// <summary>True once the jeepney has finished servicing the destination stop.</summary>
    public bool ArrivedAtDestination { get; private set; }

    // -------------------------------------------------------------------------

    public void Init(JeepneyController jeepney, CoinDrawerController drawer,
                     ManualHudController hud, ToastNotification toast,
                     DriveScoreTracker tracker, ManualRouteDefinition route,
                     RouteContext ctx, int seed)
    {
        _jeepney = jeepney;
        _drawer  = drawer;
        _hud     = hud;
        _toast   = toast;
        _tracker = tracker;
        _route   = route;
        _ctx     = ctx;
        _rng     = new System.Random(seed);

        foreach (StopZone zone in ctx.Zones)
        {
            _holdTimers[zone] = 0f;
            zone.OnJeepneyCrossed += OnZoneCrossed;
        }
    }

    // -------------------------------------------------------------------------

    void Update()
    {
        if (_ctx == null || _busyServicing || ArrivedAtDestination) return;

        TickStopDetection();
        TickParaCalls();
    }

    void TickStopDetection()
    {
        foreach (StopZone zone in _ctx.Zones)
        {
            if (!zone.JeepneyInside || _servicedThisVisit.Contains(zone))
            {
                _holdTimers[zone] = 0f;
                continue;
            }

            if (_jeepney.CurrentSpeed <= stopSpeedThreshold)
            {
                _holdTimers[zone] += Time.deltaTime;
                if (_holdTimers[zone] >= stopHoldSeconds)
                {
                    _holdTimers[zone] = 0f;
                    StartCoroutine(ServiceStop(zone));
                }
            }
            else
            {
                _holdTimers[zone] = 0f;
            }
        }
    }

    void TickParaCalls()
    {
        // When a passenger's stop comes within range, they call "Para!".
        float along = RouteMath.NearestDistanceAlong(_ctx.Waypoints, _jeepney.transform.position, out _);

        foreach (ManualPassenger p in _aboard)
        {
            if (p.ParaCalled) continue;

            StopZone dest = _ctx.Zones[p.DestStop];
            float destAlong = RouteMath.NearestDistanceAlong(_ctx.Waypoints, dest.transform.position, out _);

            if (destAlong - along <= paraDistance && destAlong - along > -2f)
            {
                p.ParaCalled = true;
                if (_toast != null) _toast.Show($"“Para!”  {p.Name} wants off at {p.DestName}");
                if (p.Chip  != null) p.Chip.Flash();
            }
        }
    }

    // -------------------------------------------------------------------------
    // Stop servicing

    IEnumerator ServiceStop(StopZone zone)
    {
        _busyServicing = true;
        _servicedThisVisit.Add(zone);

        // 1. Drop off everyone whose stop this is (everyone at the destination).
        for (int i = _aboard.Count - 1; i >= 0; i--)
        {
            ManualPassenger p = _aboard[i];
            if (p.DestStop != zone.StopIndex && !zone.IsDestination) continue;

            _aboard.RemoveAt(i);
            SpawnExitingPeep(zone, p.Tint);

            int bonus = p.FareSettled ? 15 : 0;   // happy, paid passengers tip the score
            _tracker.AddSatisfaction(bonus);
            _tracker.PassengerDelivered();

            if (p.Chip != null) { p.Chip.Hide(); p.Chip = null; }
            yield return new WaitForSeconds(boardSeconds * 0.5f);
        }

        // 2. Board the queue while seats remain.
        while (zone.WaitingCount > 0 && _aboard.Count < _route.seatCapacity)
        {
            GameObject peep = zone.TakeWaitingPeep();
            Color tint = peep != null && peep.TryGetComponent(out SpriteRenderer sr)
                ? sr.color : Color.white;
            if (peep != null) Object.Destroy(peep);

            ManualPassenger passenger = CreatePassenger(zone, tint);
            _aboard.Add(passenger);

            if (_hud != null)
                passenger.Chip = _hud.ClaimChip($"{passenger.Name} → {passenger.DestName}", tint);

            if (_drawer != null)
                _drawer.Enqueue(passenger);

            _tracker.PassengerBoarded();
            yield return new WaitForSeconds(boardSeconds);
        }

        if (zone.IsDestination)
            ArrivedAtDestination = true;

        _busyServicing = false;
    }

    ManualPassenger CreatePassenger(StopZone origin, Color tint)
    {
        int destIndex = PickDestination(origin.StopIndex);
        int stopsTraveled = Mathf.Max(1, destIndex - origin.StopIndex);

        int fare   = FareMath.ComputeFare(stopsTraveled, _routeFares);
        int tender = FareMath.GenerateTender(fare, _rng);

        return new ManualPassenger
        {
            Name       = PeepNames[_nameCursor++ % PeepNames.Length],
            OriginStop = origin.StopIndex,
            DestStop   = destIndex,
            DestName   = _ctx.Zones[destIndex].StopName,
            Fare       = fare,
            Tender     = tender,
            Tint       = tint,
        };
    }

    FareTable _routeFares = new FareTable();

    /// <summary>Fare table comes from the level definition.</summary>
    public void SetFareTable(FareTable fares)
    {
        if (fares != null) _routeFares = fares;
    }

    int PickDestination(int originIndex)
    {
        // Later stops only; the final destination is the most likely ask.
        var candidates = new List<int>();
        for (int i = originIndex + 1; i < _ctx.Zones.Length; i++)
            candidates.Add(i);

        if (candidates.Count == 0)
            return _ctx.Zones.Length - 1;

        int destZone = candidates[candidates.Count - 1];
        if (_rng.NextDouble() < 0.5d || candidates.Count == 1)
            return destZone;

        return candidates[_rng.Next(candidates.Count - 1)];
    }

    // -------------------------------------------------------------------------
    // Missed stops

    void OnZoneCrossed(StopZone zone, bool entered)
    {
        if (entered || ArrivedAtDestination) return;

        _servicedThisVisit.Remove(zone);   // re-arm for the next visit

        // Anyone who wanted this stop and is still aboard got carried past it.
        for (int i = _aboard.Count - 1; i >= 0; i--)
        {
            ManualPassenger p = _aboard[i];
            if (p.DestStop != zone.StopIndex) continue;

            _aboard.RemoveAt(i);
            _tracker.MissedStop();
            _tracker.PassengerDelivered();
            SpawnExitingPeep(zone, p.Tint);

            if (_toast != null) _toast.Show($"{p.Name} missed their stop at {zone.StopName}!  (−100)");
            if (_drawer != null) _drawer.Cancel(p);
            if (p.Chip != null) { p.Chip.Hide(); p.Chip = null; }
        }
    }

    void SpawnExitingPeep(StopZone zone, Color tint)
    {
        Vector2 logicalWorld = (Vector2)(zone.transform.position +
                                         zone.transform.right * (_route.roadHalfWidth + 1.6f));
        var peep = new GameObject("ExitingPeep");
        peep.transform.position = logicalWorld;
        var sr = peep.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("Placeholders/peep");
        sr.sortingOrder = 5;
        sr.color = tint;
        Destroy(peep, 6f);
    }
}
