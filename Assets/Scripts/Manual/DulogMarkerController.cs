using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Surfaces each onboard passenger's "dulog" (alight) target in Manual Mode:
/// a floating, color-coded world marker above the target stop that pulses harder
/// as the jeepney approaches and flips to a "Para!" request-stop prompt in range,
/// plus an edge-of-screen arrow toward the target when it is off-camera. Reads
/// the live aboard list from <see cref="PassengerManager"/> and the shared
/// <see cref="DulogModel"/> so the same data drives Manual and Automation.
/// </summary>
public class DulogMarkerController : MonoBehaviour
{
    [SerializeField] private Camera           cam;
    [SerializeField] private Transform        jeepney;
    [SerializeField] private PassengerManager passengers;
    [SerializeField] private RectTransform    edgeArrowParent;

    [Header("Tuning")]
    [SerializeField] private float markerHeight   = 2.2f;
    [SerializeField] private float baseMarkerScale = 1.1f;
    [SerializeField] private float edgeMargin      = 0.06f;

    // One live visual per aboard passenger.
    readonly Dictionary<ManualPassenger, Marker> _markers = new Dictionary<ManualPassenger, Marker>();
    readonly List<ManualPassenger> _scratch = new List<ManualPassenger>();

    Sprite _pinSprite;
    Sprite _arrowSprite;

    class Marker
    {
        public GameObject       worldGo;
        public SpriteRenderer   pin;
        public TextMeshPro      prompt;
        public RectTransform    edgeArrow;
        public Image            edgeImage;
    }

    void Awake()
    {
        _pinSprite   = Resources.Load<Sprite>("Placeholders/circle");
        _arrowSprite = Resources.Load<Sprite>("Placeholders/white_box");
        if (cam == null) cam = Camera.main;
    }

    void LateUpdate()
    {
        if (passengers == null || cam == null) return;

        IReadOnlyList<ManualPassenger> aboard = passengers.AboardPassengers;

        // Retire markers for passengers no longer aboard.
        _scratch.Clear();
        foreach (var kv in _markers)
            if (!Contains(aboard, kv.Key)) _scratch.Add(kv.Key);
        foreach (ManualPassenger gone in _scratch) Retire(gone);

        // Update / create markers for current passengers.
        for (int i = 0; i < aboard.Count; i++)
            UpdateMarker(aboard[i]);
    }

    // -------------------------------------------------------------------------

    void UpdateMarker(ManualPassenger p)
    {
        if (!_markers.TryGetValue(p, out Marker m))
        {
            m = CreateMarker(p);
            _markers[p] = m;
        }

        Vector3 target = p.TargetWorldPos + Vector3.up * markerHeight;
        float   dist   = jeepney != null
            ? Vector2.Distance(jeepney.position, p.TargetWorldPos)
            : float.MaxValue;
        float   approach = DulogModel.Approach01(dist);
        DulogState state = DulogModel.State(dist);

        // World pin: pulse amplitude + speed ramp with approach; brighten alpha.
        m.worldGo.transform.position = target;
        float pulse = 1f + 0.28f * approach * Mathf.Sin(Time.time * (3f + 7f * approach));
        m.worldGo.transform.localScale = Vector3.one * baseMarkerScale * pulse;

        Color c = p.Tint;
        c.a = Mathf.Lerp(0.55f, 1f, approach);
        m.pin.color = c;

        bool inRange = state == DulogState.InRange;
        m.prompt.gameObject.SetActive(inRange);
        if (inRange) m.prompt.color = p.Tint;

        // Edge-of-screen arrow when the target is off-camera.
        Vector3 vp = cam.WorldToViewportPoint(target);
        bool onScreen = vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;
        UpdateEdgeArrow(m, vp, onScreen, p.Tint);
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

    Marker CreateMarker(ManualPassenger p)
    {
        var go = new GameObject($"Dulog_{p.Name}");
        go.transform.SetParent(transform, false);

        var pin = go.AddComponent<SpriteRenderer>();
        pin.sprite = _pinSprite;
        pin.color = p.Tint;
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
            var arrowGo = new GameObject($"Edge_{p.Name}", typeof(RectTransform));
            arrowGo.transform.SetParent(edgeArrowParent, false);
            var rt = (RectTransform)arrowGo.transform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(46f, 14f);
            var img = arrowGo.AddComponent<Image>();
            img.sprite = _arrowSprite;
            img.color = p.Tint;
            img.raycastTarget = false;
            arrowGo.SetActive(false);
            m.edgeArrow = rt;
            m.edgeImage = img;
        }

        return m;
    }

    void Retire(ManualPassenger p)
    {
        if (_markers.TryGetValue(p, out Marker m))
        {
            if (m.worldGo  != null) Destroy(m.worldGo);
            if (m.edgeArrow != null) Destroy(m.edgeArrow.gameObject);
            _markers.Remove(p);
        }
    }

    static bool Contains(IReadOnlyList<ManualPassenger> list, ManualPassenger p)
    {
        for (int i = 0; i < list.Count; i++) if (list[i] == p) return true;
        return false;
    }
}
