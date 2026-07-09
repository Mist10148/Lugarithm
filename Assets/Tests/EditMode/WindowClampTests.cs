using NUnit.Framework;
using UnityEngine;

/// <summary>Pins the pure window clamp: the title bar and its right-end button
/// cluster must always stay reachable on-screen.</summary>
public class WindowClampTests
{
    static readonly Rect Canvas = new Rect(0f, 0f, 1280f, 720f);
    const float TitleBar = 34f;
    const float MinVisible = 160f;

    static Vector2 Push(Rect window) =>
        WindowClampUtil.ComputeClampPush(window, Canvas, TitleBar, MinVisible);

    [Test]
    public void FullyInside_NeedsNoPush()
    {
        Assert.AreEqual(Vector2.zero, Push(new Rect(100f, 100f, 500f, 400f)));
    }

    [Test]
    public void PushedOffRight_ButtonsComeBackInside()
    {
        // Window right edge at 1400 — 120 past the canvas; must push back left.
        Vector2 push = Push(new Rect(900f, 100f, 500f, 400f));
        Assert.AreEqual(-120f, push.x, 0.01f);
        Assert.AreEqual(0f, push.y, 0.01f);
    }

    [Test]
    public void PushedOffLeft_MinimumStripStaysVisible()
    {
        // Window right edge at 100 — only 100px visible; needs +60 to reach 160.
        Vector2 push = Push(new Rect(-400f, 100f, 500f, 400f));
        Assert.AreEqual(60f, push.x, 0.01f);
    }

    [Test]
    public void PushedOffTopAndBottom_TitleBarStaysOnScreen()
    {
        // Top edge above the canvas → push down.
        Vector2 pushTop = Push(new Rect(100f, 500f, 500f, 400f)); // yMax = 900
        Assert.AreEqual(-180f, pushTop.y, 0.01f);

        // Sunk so low the title bar is below the canvas bottom → push up so the
        // full bar is visible (yMax must reach at least TitleBar).
        Vector2 pushBottom = Push(new Rect(100f, -600f, 500f, 400f)); // yMax = -200
        Assert.AreEqual(TitleBar - (-200f), pushBottom.y, 0.01f);
    }
}
