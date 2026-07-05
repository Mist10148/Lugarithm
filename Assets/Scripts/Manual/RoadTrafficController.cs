using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sparse moving traffic for the shared procedural road. It is intentionally a
/// light touch: one active car, clear of stops, and never treated as a hard
/// fail state in Manual. Automation reads the active cell through AgentSim.
/// </summary>
public class RoadTrafficController : MonoBehaviour
{
    [Header("Traffic")]
    [SerializeField] private int maxActiveVehicles = 1;
    [SerializeField] private float minSpawnCooldown = 5f;
    [SerializeField] private float maxSpawnCooldown = 9f;
    [SerializeField] private float minSpawnAhead = 18f;
    [SerializeField] private float maxSpawnAhead = 34f;
    [SerializeField] private float despawnBehind = 12f;
    [SerializeField] private float carSpeed = 2.6f;
    [SerializeField] private float laneOffset = 1.35f;
    [SerializeField] private float stopClearance = 8f;
    [SerializeField] private float softCollisionRadius = 1.4f;

    readonly List<TrafficVehicle> _vehicles = new List<TrafficVehicle>();
    readonly List<float> _stopAlong = new List<float>();
    readonly HashSet<Vector2Int> _trafficCells = new HashSet<Vector2Int>();

    RouteContext _route;
    Vector2[] _line;
    Transform _root;
    Transform _target;
    JeepneyController _manualJeepney;
    TopDownGridSpace _automationSpace;
    AgentSim _automationSim;
    float _nextSpawnTime;
    int _spawnSerial;
    bool _manualMode;
    bool _automationMode;

    class TrafficVehicle
    {
        public GameObject go;
        public SpriteRenderer renderer;
        public float along;
        public float side;
    }

    public IReadOnlyCollection<Vector2Int> TrafficCells => _trafficCells;
    public int ActiveVehicleCount => _vehicles.Count;

    public bool ForceSpawnForTests(float targetAlong)
    {
        int before = _vehicles.Count;
        TrySpawn(targetAlong);
        return _vehicles.Count > before;
    }

    public void InitManual(RouteContext route, Transform root, Transform target,
                           JeepneyController jeepney)
    {
        Clear();
        _route = route;
        _line = route != null ? route.Waypoints : null;
        _root = root != null ? root : transform;
        _target = target;
        _manualJeepney = jeepney;
        _automationSpace = null;
        _automationSim = null;
        _manualMode = true;
        _automationMode = false;
        CacheStops();
        ScheduleNextSpawn(immediate: true);
    }

    public void InitAutomation(RouteContext route, Transform root, Transform target,
                               TopDownGridSpace space, AgentSim sim)
    {
        Clear();
        _route = route;
        _line = route != null ? route.Waypoints : null;
        _root = root != null ? root : transform;
        _target = target;
        _manualJeepney = null;
        _automationSpace = space;
        _automationSim = sim;
        _manualMode = false;
        _automationMode = true;
        if (_automationSim != null) _automationSim.TrafficEnabled = true;
        CacheStops();
        ScheduleNextSpawn(immediate: true);
    }

    public void RebindRoute(RouteContext route)
    {
        _route = route;
        _line = route != null ? route.Waypoints : null;
        CacheStops();
    }

    public void RebindAutomation(TopDownGridSpace space, AgentSim sim)
    {
        _automationSpace = space;
        _automationSim = sim;
        if (_automationSim != null) _automationSim.TrafficEnabled = true;
        SyncAutomationCells();
    }

    public void Clear()
    {
        for (int i = _vehicles.Count - 1; i >= 0; i--)
            DestroyVehicle(_vehicles[i]);
        _vehicles.Clear();
        _trafficCells.Clear();
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
        MoveVehicles(dt, targetAlong);

        if (_vehicles.Count < Mathf.Max(1, maxActiveVehicles) && Time.time >= _nextSpawnTime)
        {
            TrySpawn(targetAlong);
            ScheduleNextSpawn(immediate: false);
        }

        if (_manualMode)
            TickManualSoftContacts();
        if (_automationMode)
            SyncAutomationCells();
    }

    void MoveVehicles(float dt, float targetAlong)
    {
        for (int i = _vehicles.Count - 1; i >= 0; i--)
        {
            TrafficVehicle v = _vehicles[i];
            v.along += carSpeed * Mathf.Max(0f, dt);
            if (v.along < targetAlong - despawnBehind || v.along > RouteMath.TotalLength(_line) - 1f)
            {
                DestroyVehicle(v);
                _vehicles.RemoveAt(i);
                continue;
            }
            PlaceVehicle(v);
        }
    }

    void TrySpawn(float targetAlong)
    {
        if (_vehicles.Count >= Mathf.Max(1, maxActiveVehicles)) return;

        float routeLength = RouteMath.TotalLength(_line);
        if (routeLength < 4f) return;

        float span = Mathf.Max(1f, maxSpawnAhead - minSpawnAhead);
        for (int attempt = 0; attempt < 8; attempt++)
        {
            float ahead = minSpawnAhead + Mathf.Repeat(_spawnSerial * 11f + attempt * 7f, span);
            float along = Mathf.Clamp(targetAlong + ahead, 2f, routeLength - 2f);
            if (!FarFromStops(along)) continue;

            float side = ((_spawnSerial + attempt) % 2 == 0) ? -1f : 1f;
            SpawnVehicle(along, side);
            _spawnSerial++;
            return;
        }
    }

    void SpawnVehicle(float along, float side)
    {
        var go = new GameObject("TrafficCar");
        go.transform.SetParent(_root != null ? _root : transform, false);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("Placeholders/traffic_car");
        if (sr.sprite == null)
            sr.sprite = Resources.Load<Sprite>("Placeholders/jeepney_top");
        sr.sortingOrder = 9;
        sr.color = new Color(0.25f, 0.55f, 0.9f, 1f);

        var v = new TrafficVehicle { go = go, renderer = sr, along = along, side = side };
        _vehicles.Add(v);
        PlaceVehicle(v);
    }

    void PlaceVehicle(TrafficVehicle v)
    {
        Vector2 center = RouteMath.PointAt(_line, v.along);
        Vector2 dir = RouteMath.DirectionAt(_line, v.along + 0.1f);
        if (dir.sqrMagnitude < 1e-6f) dir = Vector2.up;
        Vector2 left = new Vector2(-dir.y, dir.x);
        Vector2 pos = center + left * (laneOffset * v.side);

        v.go.transform.position = new Vector3(pos.x, pos.y, 0f);
        v.go.transform.rotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.up, dir));
        if (v.renderer != null)
            v.renderer.sortingOrder = Mathf.RoundToInt(pos.y * 100f) + 8;
    }

    void TickManualSoftContacts()
    {
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
        if (_automationSpace == null || _automationSim == null)
            return;

        foreach (TrafficVehicle v in _vehicles)
        {
            if (v.go == null) continue;
            Vector2Int cell = _automationSpace.WorldToCell(v.go.transform.position);
            if (IsSafeAutomationTrafficCell(cell))
                _trafficCells.Add(cell);
        }
        _automationSim.SetTrafficCells(_trafficCells);
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

    static void DestroyVehicle(TrafficVehicle v)
    {
        if (v == null || v.go == null) return;
        if (Application.isPlaying) Object.Destroy(v.go);
        else Object.DestroyImmediate(v.go);
    }

    void OnDestroy()
    {
        Clear();
    }
}
