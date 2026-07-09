using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Moderate moving traffic for the shared procedural road. It stays clear of
/// stops, queues behind the jeepney and other cars, and never becomes a hard
/// fail state in Manual. Automation reads the active cells through AgentSim.
/// </summary>
public class RoadTrafficController : MonoBehaviour
{
    [Header("Traffic")]
    [SerializeField] private int maxActiveVehicles = 3;
    [SerializeField] private int minActiveVehicles = 1;
    [SerializeField] private float minSpawnCooldown = 3f;
    [SerializeField] private float maxSpawnCooldown = 6f;
    [SerializeField] private float minSpawnAhead = 16f;
    [SerializeField] private float maxSpawnAhead = 36f;
    [SerializeField] private float minSpawnBehind = 10f;
    [SerializeField] private float maxSpawnBehind = 24f;
    [SerializeField] private float despawnBehind = 30f;
    [SerializeField, Range(0f, 1f)] private float oncomingTrafficRatio = 0.4f;
    [SerializeField] private float minCarSpeed = 2.2f;
    [SerializeField] private float maxCarSpeed = 3.3f;
    [SerializeField] private float laneOffset = 1.35f;
    [SerializeField] private float stopClearance = 8f;
    [SerializeField] private float minVehicleSpacing = 8f;
    [SerializeField] private float followDistance = 3f;
    [SerializeField] private float followSlowZone = 3.5f;
    [SerializeField] private float speedSmoothTime = 0.35f;
    [SerializeField] private float laneSmoothTime = 0.28f;
    [SerializeField] private float cornerEaseDistance = 2.5f;
    [SerializeField] private float rotationFollowSpeed = 4f;
    [SerializeField] private float softCollisionRadius = 1.4f;
    [SerializeField] private bool enableManualSoftContacts = false;

    readonly List<TrafficVehicle> _vehicles = new List<TrafficVehicle>();
    readonly Stack<TrafficVehicle> _pool = new Stack<TrafficVehicle>();
    static Sprite _carSprite;
    readonly List<float> _stopAlong = new List<float>();
    readonly HashSet<Vector2Int> _trafficCells = new HashSet<Vector2Int>();
    readonly HashSet<Vector2Int> _lastSyncedTrafficCells = new HashSet<Vector2Int>();
    readonly Dictionary<Vector2Int, int> _trafficLaneMasks = new Dictionary<Vector2Int, int>();
    readonly Dictionary<Vector2Int, int> _lastSyncedLaneMasks = new Dictionary<Vector2Int, int>();

    RouteContext _route;
    Vector2[] _line;
    float _routeLength;
    Transform _root;
    Transform _target;
    JeepneyController _manualJeepney;
    TopDownGridSpace _automationSpace;
    AgentSim _automationSim;
    float _nextSpawnTime;
    float _lastTargetAlong;
    float _targetAlongSpeed;
    int _spawnSerial;
    bool _manualMode;
    bool _automationMode;
    bool _hasLastTargetAlong;
    bool _trafficCellsDirty = true;

    class TrafficVehicle
    {
        public GameObject go;
        public SpriteRenderer renderer;
        public float along;
        public float side;
        public int direction = 1;
        public float cruiseSpeed;
        public float currentSpeed;
        public float speedVel;
        public float visualSide;
        public float sideVel;
        public float visualAngle;
        public Vector2 lastPosition;
        public bool placed;
    }

    public IReadOnlyCollection<Vector2Int> TrafficCells => _trafficCells;
    public int ActiveVehicleCount => _vehicles.Count;
    public float FollowDistanceForTests => followDistance;
    public int MinActiveVehiclesForTests => Mathf.Clamp(minActiveVehicles, 0, VehicleCap());

    public bool ForceSpawnForTests(float targetAlong)
    {
        int before = _vehicles.Count;
        TrySpawn(targetAlong);
        return _vehicles.Count > before;
    }

    public bool ForceSpawnAtForTests(float along, float side = float.NaN, float cruiseSpeed = -1f, int direction = 1)
    {
        if (_line == null || _line.Length < 2) return false;
        if (_vehicles.Count >= VehicleCap()) return false;
        if (along < 2f || along > _routeLength - 2f) return false;

        // Default side follows right-hand driving: forward = route-right (-1), oncoming = route-left (+1).
        float resolvedSide = float.IsNaN(side)
            ? (direction >= 0 ? -1f : 1f)
            : (side >= 0f ? 1f : -1f);
        if (!FarFromStops(along) || !FarFromVehicles(along, resolvedSide)) return false;

        SpawnVehicle(along, resolvedSide,
                     cruiseSpeed > 0f ? cruiseSpeed : RandomCarSpeed(),
                     direction);
        return true;
    }

    public float VehicleAlongForTests(int index) => _vehicles[index].along;
    public float VehicleCruiseSpeedForTests(int index) => _vehicles[index].cruiseSpeed;
    public int VehicleDirectionForTests(int index) => _vehicles[index].direction;
    public float VehicleRotationForTests(int index) => _vehicles[index].visualAngle;
    public float VehicleSideOffsetForTests(int index) => _vehicles[index].visualSide;

    public void InitManual(RouteContext route, Transform root, Transform target,
                           JeepneyController jeepney)
    {
        Clear();
        BindRoute(route);
        _root = root != null ? root : transform;
        _target = target;
        _manualJeepney = jeepney;
        _automationSpace = null;
        _automationSim = null;
        _manualMode = true;
        _automationMode = false;
        _hasLastTargetAlong = false;
        ScheduleNextSpawn(immediate: true);
    }

    public void InitAutomation(RouteContext route, Transform root, Transform target,
                               TopDownGridSpace space, AgentSim sim)
    {
        Clear();
        BindRoute(route);
        _root = root != null ? root : transform;
        _target = target;
        _manualJeepney = null;
        _automationSpace = space;
        _automationSim = sim;
        _manualMode = false;
        _automationMode = true;
        if (_automationSim != null)
        {
            _automationSim.TrafficEnabled = true;
            _automationSim.TrafficBlocksMovement = false;
        }
        _hasLastTargetAlong = false;
        ScheduleNextSpawn(immediate: true);
    }

    public void RebindRoute(RouteContext route)
    {
        BindRoute(route);
    }

    public void RebindAutomation(TopDownGridSpace space, AgentSim sim)
    {
        _automationSpace = space;
        _automationSim = sim;
        if (_automationSim != null)
        {
            _automationSim.TrafficEnabled = true;
            _automationSim.TrafficBlocksMovement = false;
        }
        _trafficCellsDirty = true;
        SyncAutomationCells();
    }

    public void Clear()
    {
        for (int i = _vehicles.Count - 1; i >= 0; i--)
            ReleaseVehicle(_vehicles[i]);
        _vehicles.Clear();
        _trafficCells.Clear();
        _lastSyncedTrafficCells.Clear();
        _trafficLaneMasks.Clear();
        _lastSyncedLaneMasks.Clear();
        _trafficCellsDirty = true;
        if (_automationSim != null)
            _automationSim.ClearTraffic();
    }

    void Update()
    {
        Tick(Time.deltaTime);
    }

    public void Tick(float dt)
    {
        if (_line == null || _line.Length < 2 || _target == null)
            return;

        float targetAlong = RouteMath.NearestDistanceAlong(_line, _target.position, out _);
        float targetLateral = TargetLateralOffset(targetAlong);
        UpdateTargetSpeed(targetAlong, dt);
        MoveVehicles(dt, targetAlong, targetLateral);

        TopUpVisibleTraffic(targetAlong);

        if (_vehicles.Count < VehicleCap() && Time.time >= _nextSpawnTime)
        {
            TrySpawn(targetAlong);
            ScheduleNextSpawn(immediate: false);
        }

        if (_manualMode)
            TickManualSoftContacts();
        if (_automationMode)
            SyncAutomationCells();
    }

    void MoveVehicles(float dt, float targetAlong, float targetLateral)
    {
        float safeDt = Mathf.Max(0f, dt);
        for (int i = _vehicles.Count - 1; i >= 0; i--)
        {
            TrafficVehicle v = _vehicles[i];
            float desiredSpeed = v.cruiseSpeed;
            bool forward = v.direction >= 0;
            float followLimit = forward
                ? FindFollowLimit(v, targetAlong, targetLateral)
                : FindFollowLimitReverse(v, targetAlong, targetLateral);
            float available = forward ? followLimit - v.along : v.along - followLimit;
            if (available <= 0.05f)
            {
                desiredSpeed = 0f;
            }
            else if (available < followSlowZone)
            {
                desiredSpeed *= Mathf.Clamp01(available / Mathf.Max(0.01f, followSlowZone));
                if (forward)
                    desiredSpeed = Mathf.Min(desiredSpeed, Mathf.Max(0f, _targetAlongSpeed));
            }

            v.currentSpeed = Mathf.SmoothDamp(v.currentSpeed, desiredSpeed, ref v.speedVel,
                                              Mathf.Max(0.01f, speedSmoothTime),
                                              Mathf.Infinity, safeDt);
            float nextAlong = v.along + v.direction * v.currentSpeed * safeDt;
            bool pastLimit = forward ? nextAlong > followLimit : nextAlong < followLimit;
            bool alreadyPastLimit = forward ? followLimit < v.along : followLimit > v.along;
            if (alreadyPastLimit)
            {
                nextAlong = v.along;
                v.currentSpeed = 0f;
                v.speedVel = 0f;
            }
            else if (pastLimit)
            {
                nextAlong = followLimit;
                v.currentSpeed = 0f;
                v.speedVel = 0f;
            }

            v.along = nextAlong;
            if (ShouldDespawn(v, targetAlong))
            {
                ReleaseVehicle(v);
                _vehicles.RemoveAt(i);
                _trafficCellsDirty = true;
                continue;
            }
            PlaceVehicle(v, safeDt);
        }
    }

    // The jeepney only blocks a car when it actually occupies that car's lane.
    bool TargetSharesLane(TrafficVehicle v, float targetLateral)
    {
        return Mathf.Abs(targetLateral - laneOffset * v.side) < 0.9f;
    }

    float FindFollowLimit(TrafficVehicle v, float targetAlong, float targetLateral)
    {
        // Past the end is allowed: cars drive off the route and despawn ("leave
        // town") instead of parking forever at the frontier of a streamed road.
        float limit = _routeLength + followDistance;

        if (targetAlong > v.along && TargetSharesLane(v, targetLateral))
            limit = Mathf.Min(limit, targetAlong - followDistance);

        foreach (TrafficVehicle other in _vehicles)
        {
            if (other == v) continue;
            if (Mathf.Abs(other.side - v.side) > 0.1f) continue;
            if (other.along <= v.along) continue;
            limit = Mathf.Min(limit, other.along - followDistance);
        }

        return Mathf.Max(0f, limit);
    }

    // Mirror of FindFollowLimit for oncoming cars: a lower bound on how far
    // down-route they may travel before reaching whatever is in their lane.
    float FindFollowLimitReverse(TrafficVehicle v, float targetAlong, float targetLateral)
    {
        // Below the start is allowed: oncoming cars run off the route start and
        // despawn instead of queueing forever at along ~1.
        float limit = -followDistance;

        if (targetAlong < v.along && TargetSharesLane(v, targetLateral))
            limit = Mathf.Max(limit, targetAlong + followDistance);

        foreach (TrafficVehicle other in _vehicles)
        {
            if (other == v) continue;
            if (Mathf.Abs(other.side - v.side) > 0.1f) continue;
            if (other.along >= v.along) continue;
            limit = Mathf.Max(limit, other.along + followDistance);
        }

        return limit;
    }

    // Projects the followed target (jeepney) onto the route-left axis so we
    // know which lane it currently occupies.
    float TargetLateralOffset(float targetAlong)
    {
        if (_target == null) return 0f;
        Vector2 center = RouteMath.PointAt(_line, targetAlong);
        Vector2 roadDir = RouteMath.DirectionAt(_line, targetAlong + 0.1f);
        if (roadDir.sqrMagnitude < 1e-6f) roadDir = Vector2.up;
        Vector2 left = new Vector2(-roadDir.y, roadDir.x);
        return Vector2.Dot((Vector2)_target.position - center, left);
    }

    bool TrySpawn(float targetAlong)
    {
        if (_vehicles.Count >= VehicleCap()) return false;

        if (_routeLength < 4f) return false;

        for (int attempt = 0; attempt < 16; attempt++)
        {
            bool oncoming = ShouldSpawnOncoming(_spawnSerial + attempt);
            bool behind = !oncoming && (_spawnSerial + attempt) % 3 == 1;
            int slot = (_spawnSerial + attempt) % 5;
            float slotT = slot / 4f;
            float offset = behind
                ? Mathf.Lerp(minSpawnBehind, maxSpawnBehind, slotT)
                : Mathf.Lerp(minSpawnAhead, maxSpawnAhead, slotT);
            float along = targetAlong + (behind ? -offset : offset);
            if (along < 2f || along > _routeLength - 2f) continue;

            // Right-hand driving: forward traffic keeps to route-right (-1),
            // oncoming traffic keeps to route-left (+1).
            int direction = oncoming ? -1 : 1;
            float side = oncoming ? 1f : -1f;
            if (!FarFromStops(along) || !FarFromVehicles(along, side)) continue;

            SpawnVehicle(along, side, RandomCarSpeed(), direction);
            _spawnSerial++;
            return true;
        }
        return false;
    }

    void TopUpVisibleTraffic(float targetAlong)
    {
        int floor = Mathf.Clamp(minActiveVehicles, 0, VehicleCap());
        int guard = floor - _vehicles.Count;
        while (guard-- > 0 && _vehicles.Count < floor)
        {
            if (!TrySpawn(targetAlong))
                break;
        }
    }

    static Sprite CarSprite()
    {
        if (_carSprite == null)
        {
            _carSprite = Resources.Load<Sprite>("Placeholders/traffic_car");
            if (_carSprite == null)
                _carSprite = Resources.Load<Sprite>("Placeholders/jeepney_top");
        }
        return _carSprite;
    }

    void SpawnVehicle(float along, float side, float cruiseSpeed, int direction = 1)
    {
        TrafficVehicle v = null;
        while (_pool.Count > 0 && (v == null || v.go == null))
            v = _pool.Pop();
        if (v == null || v.go == null)
        {
            var go = new GameObject("TrafficCar");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CarSprite();
            sr.sortingOrder = 9;
            sr.color = new Color(0.25f, 0.55f, 0.9f, 1f);
            v = new TrafficVehicle { go = go, renderer = sr };
        }

        v.go.transform.SetParent(_root != null ? _root : transform, false);
        v.go.SetActive(true);
        v.along = along;
        v.side = side;
        v.direction = direction < 0 ? -1 : 1;
        v.cruiseSpeed = Mathf.Max(0.1f, cruiseSpeed);
        v.currentSpeed = Mathf.Max(0.1f, cruiseSpeed);
        v.speedVel = 0f;
        v.sideVel = 0f;
        v.placed = false;

        _vehicles.Add(v);
        _trafficCellsDirty = true;
        PlaceVehicle(v, 0f);
    }

    void PlaceVehicle(TrafficVehicle v, float dt)
    {
        Vector2 center = RouteMath.PointAt(_line, v.along);
        Vector2 roadDir = RouteMath.DirectionAt(_line, v.along + 0.1f);
        if (roadDir.sqrMagnitude < 1e-6f) roadDir = Vector2.up;
        Vector2 dir = v.direction >= 0 ? roadDir : -roadDir;
        Vector2 left = new Vector2(-roadDir.y, roadDir.x);
        float distToCorner = RouteMath.DistanceToNearestCorner(_line, v.along);
        float cornerFactor = cornerEaseDistance > 0f
            ? Mathf.Clamp01(distToCorner / cornerEaseDistance)
            : 1f;
        float sideTarget = laneOffset * v.side * cornerFactor;
        if (!v.placed || dt <= 0f)
        {
            v.visualSide = sideTarget;
        }
        else
        {
            v.visualSide = Mathf.SmoothDamp(v.visualSide, sideTarget, ref v.sideVel,
                                            Mathf.Max(0.01f, laneSmoothTime),
                                            Mathf.Infinity, dt);
        }
        Vector2 pos = center + left * v.visualSide;

        v.go.transform.position = new Vector3(pos.x, pos.y, 0f);
        float rawAngle = Vector2.SignedAngle(Vector2.up, dir);
        float targetAngle = rawAngle;
        if (v.placed)
        {
            Vector2 motion = pos - v.lastPosition;
            if (v.currentSpeed > 0.02f && motion.sqrMagnitude > 1e-8f)
            {
                Vector2 motionDir = motion.normalized;
                targetAngle = Vector2.Dot(motionDir, dir) > 0f
                    ? Vector2.SignedAngle(Vector2.up, motionDir)
                    : rawAngle;
            }
            else
                targetAngle = v.visualAngle;
        }

        v.visualAngle = !v.placed || dt <= 0f
            ? rawAngle
            : Mathf.LerpAngle(v.visualAngle, targetAngle, rotationFollowSpeed * dt);
        v.go.transform.rotation = Quaternion.Euler(0f, 0f, v.visualAngle);
        v.lastPosition = pos;
        v.placed = true;
        if (v.renderer != null)
            v.renderer.sortingOrder = Mathf.RoundToInt(pos.y * 100f) + 8;
    }

    void TickManualSoftContacts()
    {
        if (!enableManualSoftContacts) return;
        if (_manualJeepney == null || _target == null) return;
        float radiusSqr = softCollisionRadius * softCollisionRadius;
        foreach (TrafficVehicle v in _vehicles)
        {
            if (v.go == null) continue;
            if (((Vector2)v.go.transform.position - (Vector2)_target.position).sqrMagnitude <= radiusSqr)
                _manualJeepney.ApplySoftTrafficContact(v.go.transform.position);
        }
    }

    void SyncAutomationCells()
    {
        _trafficCells.Clear();
        _trafficLaneMasks.Clear();
        if (_automationSpace == null || _automationSim == null)
            return;

        bool laneMode = _automationSim.LaneMode;
        foreach (TrafficVehicle v in _vehicles)
        {
            if (v.go == null) continue;
            Vector2Int cell = _automationSpace.WorldToCell(v.go.transform.position);
            if (!IsSafeAutomationTrafficCell(cell)) continue;
            _trafficCells.Add(cell);

            if (!laneMode) continue;
            int mask = LaneMaskFor(v);
            _trafficLaneMasks.TryGetValue(cell, out int existing);
            _trafficLaneMasks[cell] = existing | mask;
        }

        if (_trafficCellsDirty || !TrafficMasksEqual(_trafficLaneMasks, _lastSyncedLaneMasks) ||
            !TrafficSetsEqual(_trafficCells, _lastSyncedTrafficCells))
        {
            if (laneMode) _automationSim.SetTrafficOccupancy(_trafficLaneMasks);
            else          _automationSim.SetTrafficCells(_trafficCells);

            _lastSyncedTrafficCells.Clear();
            foreach (Vector2Int cell in _trafficCells)
                _lastSyncedTrafficCells.Add(cell);
            _lastSyncedLaneMasks.Clear();
            foreach (var kv in _trafficLaneMasks)
                _lastSyncedLaneMasks[kv.Key] = kv.Value;
            _trafficCellsDirty = false;
        }
    }

    /// <summary>Which lane (bit per world cardinal) this car occupies, from the
    /// dominant direction of its lateral offset off the route centerline. Near a
    /// corner the offset eases toward zero — mark BOTH lanes then (safe default).</summary>
    int LaneMaskFor(TrafficVehicle v)
    {
        Vector2 roadDir = RouteMath.DirectionAt(_line, v.along + 0.1f);
        if (roadDir.sqrMagnitude < 1e-6f) roadDir = Vector2.up;
        Vector2 left = new Vector2(-roadDir.y, roadDir.x);
        Vector2 lateral = left * v.visualSide;

        if (lateral.magnitude < 0.3f)
            return 0xF;   // ambiguous mid-corner — occupy every lane

        int best = 0;
        float bestDot = float.NegativeInfinity;
        for (int c = 0; c < 4; c++)
        {
            float dot = Vector2.Dot(lateral, _automationSpace.FacingDirection(c));
            if (dot > bestDot) { bestDot = dot; best = c; }
        }
        return 1 << best;
    }

    static bool TrafficMasksEqual(Dictionary<Vector2Int, int> a, Dictionary<Vector2Int, int> b)
    {
        if (a.Count != b.Count) return false;
        foreach (var kv in a)
            if (!b.TryGetValue(kv.Key, out int other) || other != kv.Value)
                return false;
        return true;
    }

    bool IsSafeAutomationTrafficCell(Vector2Int cell)
    {
        if (_automationSim == null || _automationSim.Grid == null) return false;
        if (!_automationSim.Grid.IsWalkable(cell)) return false;

        // Do not park on service cells, and require at least one adjacent lane to exist.
        if (_automationSim.Grid.Get(cell) == GridModel.Cell.Stop ||
            _automationSim.Grid.Get(cell) == GridModel.Cell.Destination ||
            _automationSim.Grid.Get(cell) == GridModel.Cell.Start)
            return false;

        // In lane mode the escape route is the OTHER LANE of the same cell, so no
        // adjacent walkable cell is required for the car to be dodgeable.
        if (_automationSim.LaneMode)
            return true;

        bool hasEscape = false;
        foreach (Vector2Int d in AgentSim.FacingDeltas)
        {
            Vector2Int side = cell + d;
            if (_automationSim.Grid.IsWalkable(side) &&
                _automationSim.Grid.Get(side) != GridModel.Cell.Stop)
            {
                hasEscape = true;
                break;
            }
        }
        return hasEscape;
    }

    bool FarFromStops(float along)
    {
        foreach (float stop in _stopAlong)
            if (Mathf.Abs(stop - along) < stopClearance)
                return false;
        return true;
    }

    bool FarFromVehicles(float along, float side)
    {
        foreach (TrafficVehicle v in _vehicles)
        {
            if (Mathf.Abs(v.side - side) > 0.1f) continue;
            if (Mathf.Abs(v.along - along) < minVehicleSpacing)
                return false;
        }
        return true;
    }

    bool ShouldSpawnOncoming(int serial)
    {
        if (oncomingTrafficRatio <= 0f) return false;
        if (oncomingTrafficRatio >= 1f) return true;
        float pattern = (serial % 10) / 10f;
        return pattern < oncomingTrafficRatio;
    }

    bool ShouldDespawn(TrafficVehicle v, float targetAlong)
    {
        if (v == null) return true;
        if (v.along < targetAlong - despawnBehind) return true;
        if (v.along > _routeLength - 1f) return true;
        if (v.direction < 0 && v.along < 1f) return true;
        return false;
    }

    void BindRoute(RouteContext route)
    {
        _route = route;
        _line = route != null ? route.Waypoints : null;
        _routeLength = _line != null && _line.Length >= 2 ? RouteMath.TotalLength(_line) : 0f;
        CacheStops();
        _trafficCellsDirty = true;
    }

    void CacheStops()
    {
        _stopAlong.Clear();
        if (_route == null || _route.Zones == null || _line == null || _line.Length < 2) return;
        foreach (StopZone zone in _route.Zones)
        {
            if (zone == null) continue;
            float along = RouteMath.NearestDistanceAlong(_line, zone.transform.position, out _);
            _stopAlong.Add(along);
        }
    }

    void ScheduleNextSpawn(bool immediate)
    {
        float delay = immediate ? 1.5f : Random.Range(minSpawnCooldown, maxSpawnCooldown);
        _nextSpawnTime = Time.time + delay;
    }

    void UpdateTargetSpeed(float targetAlong, float dt)
    {
        if (_hasLastTargetAlong && dt > 0f)
            _targetAlongSpeed = Mathf.Max(0f, (targetAlong - _lastTargetAlong) / dt);
        else
            _targetAlongSpeed = 0f;

        _lastTargetAlong = targetAlong;
        _hasLastTargetAlong = true;
    }

    int VehicleCap()
    {
        return Mathf.Max(1, maxActiveVehicles);
    }

    float RandomCarSpeed()
    {
        float lo = Mathf.Min(minCarSpeed, maxCarSpeed);
        float hi = Mathf.Max(minCarSpeed, maxCarSpeed);
        return Random.Range(lo, hi);
    }

    static bool TrafficSetsEqual(HashSet<Vector2Int> a, HashSet<Vector2Int> b)
    {
        if (a.Count != b.Count) return false;
        foreach (Vector2Int cell in a)
            if (!b.Contains(cell))
                return false;
        return true;
    }

    void ReleaseVehicle(TrafficVehicle v)
    {
        if (v == null || v.go == null) return;
        if (Application.isPlaying)
        {
            v.go.SetActive(false);
            _pool.Push(v);
        }
        else
        {
            // EditMode tests expect deterministic object counts — no pooling.
            Object.DestroyImmediate(v.go);
        }
    }

    void OnDestroy()
    {
        Clear();
        while (_pool.Count > 0)
        {
            TrafficVehicle v = _pool.Pop();
            if (v == null || v.go == null) continue;
            if (Application.isPlaying) Object.Destroy(v.go);
            else Object.DestroyImmediate(v.go);
        }
    }
}
