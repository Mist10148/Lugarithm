using UnityEngine;

/// <summary>
/// Shared clamping for floating windows: the title bar must stay fully on-screen
/// vertically, and the right end of the title bar (where the minimize/close
/// buttons live) must never leave the canvas — so a window can never be dragged,
/// resized, or restored into an unrecoverable spot.
/// </summary>
public static class WindowClampUtil
{
    /// <summary>How much of the window must stay visible when pushed off the left.</summary>
    public const float MinVisibleWidth = 160f;

    /// <summary>Pure clamp math on world-space rects. Returns the world-space push
    /// needed to bring the window back to a legal spot (zero when already legal).
    /// <paramref name="titleBarHeight"/> and <paramref name="minVisibleWidth"/> are
    /// in the same (world) units as the rects.</summary>
    public static Vector2 ComputeClampPush(Rect window, Rect canvas,
                                           float titleBarHeight, float minVisibleWidth)
    {
        Vector2 push = Vector2.zero;

        // Vertical: the whole title bar stays inside — window top never above the
        // canvas top, and never so low that the bar sinks below the bottom edge.
        if (window.yMax > canvas.yMax)
            push.y = canvas.yMax - window.yMax;
        else if (window.yMax - titleBarHeight < canvas.yMin)
            push.y = canvas.yMin - (window.yMax - titleBarHeight);

        // Horizontal: the right edge (button cluster) never leaves the canvas, and
        // at least minVisibleWidth of the window stays reachable from the left.
        if (window.xMax > canvas.xMax)
            push.x = canvas.xMax - window.xMax;
        else if (window.xMax < canvas.xMin + minVisibleWidth)
            push.x = (canvas.xMin + minVisibleWidth) - window.xMax;

        return push;
    }

    /// <summary>Applies the clamp to a window RectTransform inside its canvas.</summary>
    public static void Clamp(RectTransform windowRoot, RectTransform canvas)
    {
        if (windowRoot == null || canvas == null) return;

        var win = new Vector3[4];
        var can = new Vector3[4];
        windowRoot.GetWorldCorners(win);   // 0=BL 1=TL 2=TR 3=BR
        canvas.GetWorldCorners(can);

        float scale = Mathf.Max(0.0001f, canvas.localScale.x);
        var w = new Rect(win[0].x, win[0].y, win[2].x - win[0].x, win[2].y - win[0].y);
        var c = new Rect(can[0].x, can[0].y, can[2].x - can[0].x, can[2].y - can[0].y);

        Vector2 push = ComputeClampPush(w, c,
            EditorWindowController.TitleBarHeight * scale, MinVisibleWidth * scale);
        if (push != Vector2.zero)
            windowRoot.anchoredPosition += push / scale;
    }
}
