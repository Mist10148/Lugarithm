using System.Collections;
using UnityEngine;

/// <summary>
/// Animated view of the grid jeepney: tweens cell-to-cell moves, rotates the
/// facing arrow, bumps on blocked moves, and pops icons for pickups and
/// fares. Mirrors whatever <see cref="AgentSim"/> reports — no game rules here.
/// </summary>
public class JeepneyAgentView : MonoBehaviour, IAgentView
{
    [Header("References")]
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private SpriteRenderer arrow;

    IGridSpace _view;
    IStopView  _stopView;

    // -------------------------------------------------------------------------

    public void Init(IGridSpace space, Vector2Int cell, int facing)
    {
        _view = space;
        _stopView = space as IStopView;
        SnapTo(cell, facing);
    }

    public void SnapTo(Vector2Int cell, int facing)
    {
        transform.position = _view.CellToWorld(cell) + new Vector3(0f, 0.18f, 0f);
        SetSortOrder(cell);
        SetArrowFacing(facing);
    }

    // -------------------------------------------------------------------------

    /// <summary>Plays one action over <paramref name="duration"/> seconds.</summary>
    public IEnumerator PlayAction(AgentActionResult result, float duration)
    {
        switch (result.Action)
        {
            case "moveForward":
                if (result.Blocked)
                    yield return Bump(result.FacingBefore, duration);
                else
                    yield return MoveTo(result.From, result.To, duration);
                break;

            case "turnLeft":
            case "turnRight":
                yield return Turn(result.FacingBefore, result.FacingAfter, duration);
                break;

            case "pickUp":
                if (result.PickedUp && _stopView != null)
                    _stopView.RemoveWaitingPeeps(result.From, Mathf.Max(1, result.PickedUpCount));
                yield return Pop("Placeholders/peep", duration);
                break;

            case "dropOff":
                yield return Pop("Placeholders/peep", duration);
                break;

            case "collectFare":
                yield return Pop("Placeholders/coin", duration);
                break;

            default:
                yield return new WaitForSeconds(duration);
                break;
        }
    }

    // -------------------------------------------------------------------------

    IEnumerator MoveTo(Vector2Int from, Vector2Int to, float duration)
    {
        Vector3 a = _view.CellToWorld(from) + new Vector3(0f, 0.18f, 0f);
        Vector3 b = _view.CellToWorld(to)   + new Vector3(0f, 0.18f, 0f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            transform.position = Vector3.Lerp(a, b, t);
            if (t > 0.5f) SetSortOrder(to);
            yield return null;
        }

        transform.position = b;
        SetSortOrder(to);
    }

    IEnumerator Bump(int facing, float duration)
    {
        Vector3 origin = transform.position;
        Vector3 push   = (Vector3)(_view.FacingDirection(facing) * 0.16f);

        float half = Mathf.Max(0.05f, duration * 0.5f);
        float elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(origin, origin + push, elapsed / half);
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(origin + push, origin, elapsed / half);
            yield return null;
        }
        transform.position = origin;
    }

    IEnumerator Turn(int fromFacing, int toFacing, float duration)
    {
        if (arrow == null) { yield return new WaitForSeconds(duration); yield break; }

        Quaternion a = ArrowRotation(fromFacing);
        Quaternion b = ArrowRotation(toFacing);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            arrow.transform.localRotation = Quaternion.Slerp(a, b, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        arrow.transform.localRotation = b;
    }

    IEnumerator Pop(string spritePath, float duration)
    {
        var icon = new GameObject("Pop");
        icon.transform.SetParent(transform, false);
        icon.transform.localPosition = new Vector3(0f, 0.45f, 0f);

        var sr = icon.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>(spritePath);
        sr.sortingOrder = 999;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            icon.transform.localPosition = new Vector3(0f, 0.45f + 0.35f * t, 0f);
            sr.color = new Color(1f, 1f, 1f, 1f - t);
            yield return null;
        }

        Destroy(icon);
    }

    // -------------------------------------------------------------------------

    void SetSortOrder(Vector2Int cell)
    {
        int order = _view.SortOrder(cell) + 1;
        if (body  != null) body.sortingOrder  = order;
        if (arrow != null) arrow.sortingOrder = order + 1;
    }

    void SetArrowFacing(int facing)
    {
        if (arrow != null)
            arrow.transform.localRotation = ArrowRotation(facing);
    }

    Quaternion ArrowRotation(int facing)
    {
        Vector2 dir = _view.FacingDirection(facing);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        return Quaternion.Euler(0f, 0f, angle);
    }
}
