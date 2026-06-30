using UnityEngine;

/// <summary>
/// Keeps the player readable when a tutorial tree canopy overlaps them.
/// The trigger is visual-only and never blocks movement.
/// </summary>
[DisallowMultipleComponent]
public sealed class TutorialTreeOccluder : MonoBehaviour
{
    [SerializeField] private SpriteRenderer treeRenderer;
    [SerializeField, Range(0f, 1f)] private float occludedAlpha = 0.42f;
    [SerializeField] private float fadeOutDuration = 0.12f;
    [SerializeField] private float fadeInDuration = 0.15f;
    [SerializeField] private float behindThreshold = 0.1f;
    [SerializeField] private int behindSortingOrder = 6;
    [SerializeField] private int frontSortingOrder = 4;

    private TopDownPlayerController _overlappingPlayer;
    private float _targetAlpha = 1f;

    public void Configure(SpriteRenderer renderer)
    {
        treeRenderer = renderer;
        RestoreVisuals();
    }

    void Awake()
    {
        if (treeRenderer == null)
            treeRenderer = GetComponent<SpriteRenderer>();

        RestoreVisuals();
    }

    void Update()
    {
        if (treeRenderer == null) return;

        float duration = _targetAlpha < treeRenderer.color.a
            ? fadeOutDuration
            : fadeInDuration;
        float distance = Mathf.Abs(1f - occludedAlpha);
        float speed = duration > 0f ? distance / duration : float.PositiveInfinity;

        Color color = treeRenderer.color;
        color.a = Mathf.MoveTowards(
            color.a,
            _targetAlpha,
            speed * Time.unscaledDeltaTime);
        treeRenderer.color = color;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TopDownPlayerController player = other.GetComponentInParent<TopDownPlayerController>();
        if (player == null) return;

        _overlappingPlayer = player;
        RefreshOcclusion();
    }

    void OnTriggerStay2D(Collider2D other)
    {
        TopDownPlayerController player = other.GetComponentInParent<TopDownPlayerController>();
        if (player == null) return;

        _overlappingPlayer = player;
        RefreshOcclusion();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        TopDownPlayerController player = other.GetComponentInParent<TopDownPlayerController>();
        if (player == null || player != _overlappingPlayer) return;

        _overlappingPlayer = null;
        RestoreVisuals();
    }

    void OnDisable()
    {
        _overlappingPlayer = null;
        RestoreVisuals();
    }

    void RefreshOcclusion()
    {
        if (treeRenderer == null || _overlappingPlayer == null) return;

        bool playerIsBehind =
            _overlappingPlayer.transform.position.y > transform.position.y + behindThreshold;
        _targetAlpha = playerIsBehind ? occludedAlpha : 1f;
        treeRenderer.sortingOrder = playerIsBehind
            ? behindSortingOrder
            : frontSortingOrder;
    }

    void RestoreVisuals()
    {
        _targetAlpha = 1f;
        if (treeRenderer == null) return;

        Color color = treeRenderer.color;
        color.a = 1f;
        treeRenderer.color = color;
        treeRenderer.sortingOrder = behindSortingOrder;
    }
}
