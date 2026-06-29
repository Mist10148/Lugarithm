using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Automation-mode counterpart to <see cref="DulogMarkerController"/>: surfaces each onboard
/// passenger's drop-off (dulog) target as a color-coded world pin that pulses harder as the jeepney
/// nears it, flips to a "Para!" prompt in range, and a screen-edge compass arrow when the target is
/// off-camera. Reads the live ride list from <see cref="AgentSim.Rides"/> (via the
/// <see cref="ExecutionController"/>) and the shared <see cref="DulogModel"/> math, so the same
/// distance/pulse curve drives Manual and Automation.
///
/// Initialized at runtime (the active grid space is chosen per level — iso vs top-down), so it takes
/// its references from the controller rather than serialized scene refs.
/// </summary>
public class AutomationDulogMarkerController : MonoBehaviour
{
    [SerializeField] private RectTransform edgeArrowParent;

    [Header("Tuning")]
    [SerializeField] private float markerHeight    = 2.2f;
    [SerializeField] private float baseMarkerScale = 1.1f;
    [SerializeField] private float edgeMargin      = 0.06f;

    ExecutionController _exec;
    IGridSpace          _space;
    Transform           _agent;
    Camera              _cam;

    // One live visual per aboard ride, keyed by GridRide.id (survives grid rebinds on streaming).
    readonly Dictionary<int, Marker> _markers = new Dictionary<int, Marker>();
    readonly List<int> _scratch = new List<int>();

    Sprite _pinSprite;
    Sprite _arrowSprite;

    class Marker
    {
        public GameObject     worldGo;
        public SpriteRenderer pin;
        public TextMeshPro    prompt;
        public RectTransform  edgeArrow;
        public Image          edgeImage;
    }

    void Awake()
    {
        _pinSprite   = Resources.Load<Sprite>("Placeholders/circle");
        _arrowSprite = Resources.Load<Sprite>("Placeholders/white_box");
    }

    /// <summary>Binds the marker layer to the live sim, grid space, jeepney and world camera.</summary>
    public void Init(ExecutionController exec, IGridSpace space, Transform agent, Camera cam)
    {
        _exec  = exec;
        _space = space;
        _agent = agent;
        _cam   = cam != null ? cam : Camera.main;
        ClearAll();
    }

    /// <summary>Retires every marker (called on world reset).</summary>
    public void ClearAll()
    {
        _scratch.Clear();
        foreach (var kv in _markers) _scratch.Add(kv.Key);
        foreach (int id in _scratch) Retire(id);
    }

    void LateUpdate()
    {
        if (_exec == null || _space == null || _cam == null) return;

        AgentSim sim = _exec.Sim;
        IReadOnlyList<GridRide> rides = sim != null ? sim.Rides : null;
        if (rides == null) { ClearAll(); return; }

        // Retire markers for rides that are no longer aboard (delivered or reset).
        _scratch.Clear();
        foreach (var kv in _markers)
            if (!IsAboard(rides, kv.Key)) _scratch.Add(kv.Key);
        foreach (int id in _scratch) Retire(id);

        // Update / create markers for the rides currently aboard.
        for (int i = 0; i < rides.Count; i++)
        {
            GridRide ride = rides[i];
            if (ride.aboard && !ride.delivered) UpdateMarker(ride);
        }
    }

    // -------------------------------------------------------------------------

    void UpdateMarker(GridRide ride)
    {
        if (!_markers.TryGetValue(ride.id, out Marker m))
        {
            m = CreateMarker(ride);
            _markers[ride.id] = m;
        }

        Vector3 dest   = _space.CellToWorld(ride.dest);
        Vector3 target = dest + Vector3.up * markerHeight;
        float   dist   = _agent != null ? Vector2.Distance(_agent.position, dest) : float.MaxValue;
        float   approach = DulogModel.Approach01(dist);
        DulogState state = DulogModel.State(dist);

        // World pin: pulse amplitude + speed ramp with approach; brighten alpha.
        m.worldGo.transform.position = target;
        float pulse = 1f + 0.28f * approach * Mathf.Sin(Time.time * (3f + 7f * approach));
        m.worldGo.transform.localScale = Vector3.one * baseMarkerScale * pulse;

        Color c = ride.color;
        c.a = Mathf.Lerp(0.55f, 1f, approach);
        m.pin.color = c;

        bool inRange = state == DulogState.InRange;
        m.prompt.gameObject.SetActive(inRange);
        if (inRange) m.prompt.color = ride.color;

        // Edge-of-screen arrow when the target is off-camera.
        Vector3 vp = _cam.WorldToViewportPoint(target);
        bool onScreen = vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;
        UpdateEdgeArrow(m, vp, onScreen, ride.color);
    }

    void UpdateEdgeArrow(Marker m, Vector3 vp, bool onScreen, Color tint)
    {
        if (m.edgeArrow == null) return;

        if (onScreen)
        {
            m.edgeArrow.gameObject.SetActive(false);
            return;
        }

        m.edgeArrow.gameObject.SetActive(true);
        m.edgeImage.color = tint;

        // Behind the camera: mirror so the arrow points the sensible way.
        if (vp.z < 0f) { vp.x = 1f - vp.x; vp.y = 1f - vp.y; }

        float x = Mathf.Clamp(vp.x, edgeMargin, 1f - edgeMargin);
        float y = Mathf.Clamp(vp.y, edgeMargin, 1f - edgeMargin);
        Vector2 size = edgeArrowParent.rect.size;
        m.edgeArrow.anchoredPosition = new Vector2(x * size.x, y * size.y);

        // Point from screen center toward the target.
        Vector2 dir = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        m.edgeArrow.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    Marker CreateMarker(GridRide ride)
    {
        var go = new GameObject($"Dulog_{ride.id}");
        go.transform.SetParent(transform, false);

        var pin = go.AddComponent<SpriteRenderer>();
        pin.sprite = _pinSprite;
        pin.color = ride.color;
        pin.sortingOrder = 40;

        var promptGo = new GameObject("Prompt");
        promptGo.transform.SetParent(go.transform, false);
        promptGo.transform.localPosition = new Vector3(0f, 1.1f, 0f);
        var prompt = promptGo.AddComponent<TextMeshPro>();
        prompt.text = "Para!";
        prompt.fontSize = 3f;
        prompt.alignment = TextAlignmentOptions.Center;
        prompt.sortingOrder = 41;
        promptGo.SetActive(false);

        var m = new Marker { worldGo = go, pin = pin, prompt = prompt };

        if (edgeArrowParent != null)
        {
            var arrowGo = new GameObject($"Edge_{ride.id}", typeof(RectTransform));
            arrowGo.transform.SetParent(edgeArrowParent, false);
            var rt = (RectTransform)arrowGo.transform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(46f, 14f);
            var img = arrowGo.AddComponent<Image>();
            img.sprite = _arrowSprite;
            img.color = ride.color;
            img.raycastTarget = false;
            arrowGo.SetActive(false);
            m.edgeArrow = rt;
            m.edgeImage = img;
        }

        return m;
    }

    void Retire(int id)
    {
        if (_markers.TryGetValue(id, out Marker m))
        {
            if (m.worldGo   != null) Destroy(m.worldGo);
            if (m.edgeArrow != null) Destroy(m.edgeArrow.gameObject);
            _markers.Remove(id);
        }
    }

    static bool IsAboard(IReadOnlyList<GridRide> rides, int id)
    {
        for (int i = 0; i < rides.Count; i++)
            if (rides[i].id == id) return rides[i].aboard && !rides[i].delivered;
        return false;
    }
}
