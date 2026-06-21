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

    void Awake()
    {
        if (target == null) target = Camera.main;
    }

    void LateUpdate()
    {
        if (target == null)
        {
            target = Camera.main;
            if (target == null) return;
        }

        Vector3 cam = target.transform.position;
        Vector3 pos = transform.position;
        transform.position = new Vector3(cam.x, cam.y, pos.z);   // keep our own z (draw order)
    }
}
