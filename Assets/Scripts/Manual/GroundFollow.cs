using UnityEngine;

/// <summary>
/// Keeps a large tiled ground sprite centered on the camera so endless
/// free-roam never reaches the edge of the grass (a green void). The tiling is
/// uniform, so re-centering is invisible — the ground just appears infinite.
/// Attached to the "Ground" object by <c>ManualDriveSceneBuilder</c>.
/// </summary>
public class GroundFollow : MonoBehaviour
{
    [SerializeField] private Camera target;

    SpriteRenderer _sr;

    void Awake()
    {
        if (target == null) target = Camera.main;
        _sr = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        if (target == null)
        {
            target = Camera.main;
            if (target == null) return;
        }

        // Re-center in whole-tile steps, so the tiled texture stays anchored
        // to the WORLD. Following the camera exactly makes the grass pattern
        // ride along with it — a visibly "moving background".
        Vector3 cam = target.transform.position;
        Vector3 pos = transform.position;
        Vector2 tile = _sr != null && _sr.sprite != null
            ? (Vector2)_sr.sprite.bounds.size : Vector2.one;
        float x = Mathf.Round(cam.x / tile.x) * tile.x;
        float y = Mathf.Round(cam.y / tile.y) * tile.y;
        transform.position = new Vector3(x, y, pos.z);   // keep our own z (draw order)
    }
}
