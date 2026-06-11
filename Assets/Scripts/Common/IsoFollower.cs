using UnityEngine;

/// <summary>
/// Draws a logical object isometrically. Each LateUpdate it copies the
/// projected position and depth-sort of its logical <see cref="source"/>, so
/// the physics/trigger object can stay on the flat logical plane while this
/// sprite is its isometric view. The sprite is kept upright (billboard).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class IsoFollower : MonoBehaviour
{
    [SerializeField] private Transform source;
    [SerializeField] private int sortingBias;

    SpriteRenderer _sr;

    void Awake() { _sr = GetComponent<SpriteRenderer>(); }

    public void SetSource(Transform s) { source = s; }

    void LateUpdate()
    {
        if (source == null) return;

        Vector2 logical = source.position;
        Vector3 p = IsoProjection.Project(logical);
        transform.position = new Vector3(p.x, p.y, transform.position.z);

        if (_sr != null)
            _sr.sortingOrder = IsoProjection.SortOrder(logical) + sortingBias;
    }
}
