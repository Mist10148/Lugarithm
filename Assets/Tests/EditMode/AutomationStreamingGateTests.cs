using NUnit.Framework;

/// <summary>Pins the pure streaming guard: the endless road must keep generating in any
/// exec state, but never while the world could shift under an in-flight animation.
/// The guard has two consumers, both routed through ShouldStreamNow: the per-frame
/// Update path (idle/paused/finished programs, passing live busy/pending flags) and the
/// exec loop's OnStaticWorldWindow (running programs at batch boundaries, where
/// busy=false and pending=false hold by construction).</summary>
public class AutomationStreamingGateTests
{
    static bool Stream(bool busy = false, bool pending = false,
                       int chunks = 0, int maxChunks = int.MaxValue,
                       float distToEnd = 10f, float lookAhead = 70f)
    {
        return AutomationDriveController.ShouldStreamNow(
            busy, pending, chunks, maxChunks, distToEnd, lookAhead);
    }

    [Test]
    public void Streams_WhenIdleNearFrontier()
    {
        // The guard has no run-state input at all — an idle or finished program
        // still gets road as long as the world is static.
        Assert.IsTrue(Stream());
    }

    [Test]
    public void NeverStreams_MidAnimationOrWithQueuedMoves()
    {
        Assert.IsFalse(Stream(busy: true));
        Assert.IsFalse(Stream(pending: true));
        Assert.IsFalse(Stream(busy: true, pending: true));
    }

    [Test]
    public void Streams_RegardlessOfStoryProgress()
    {
        // The guard has no story/win input: delivering the story passenger or having
        // the LEVEL COMPLETE panel open must never stop the road from extending.
        Assert.IsTrue(Stream());
        Assert.IsFalse(Stream(busy: true), "the static-world safety inputs still hold");
    }

    [Test]
    public void StreamsOnlyWithinLookahead_AndUnderChunkCap()
    {
        Assert.IsFalse(Stream(distToEnd: 71f, lookAhead: 70f));
        Assert.IsTrue(Stream(distToEnd: 69f, lookAhead: 70f));
        Assert.IsFalse(Stream(chunks: 5, maxChunks: 5));
    }

    [Test]
    public void StreamsInBatchBoundaryWindow_BusyFalsePendingFalse()
    {
        // The exec loop's static-world window fires only when nothing is animating and
        // the move queue is empty — the exact inputs a continuously driving endless
        // program presents between keepDriving batches. The guard must pass there, or
        // the road starves under a `while True: keepDriving()` run.
        Assert.IsTrue(Stream(busy: false, pending: false, distToEnd: 60f, lookAhead: 90f));
        Assert.IsFalse(Stream(busy: false, pending: false, distToEnd: 120f, lookAhead: 90f),
                       "far from the frontier the window appends nothing");
    }
}
