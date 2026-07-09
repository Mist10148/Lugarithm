using NUnit.Framework;

/// <summary>Pins the pure streaming guard: the endless road must keep generating in any
/// exec state, but never while the world could shift under an in-flight animation.</summary>
public class AutomationStreamingGateTests
{
    static bool Stream(bool busy = false, bool pending = false, bool storyFrozen = false,
                       bool wonFrozen = false, int chunks = 0, int maxChunks = int.MaxValue,
                       float distToEnd = 10f, float lookAhead = 70f)
    {
        return AutomationDriveController.ShouldStreamNow(
            busy, pending, storyFrozen, wonFrozen, chunks, maxChunks, distToEnd, lookAhead);
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
    public void NeverStreams_WhileStoryOrWinFreezeIsActive()
    {
        Assert.IsFalse(Stream(storyFrozen: true));
        Assert.IsFalse(Stream(wonFrozen: true));
    }

    [Test]
    public void StreamsOnlyWithinLookahead_AndUnderChunkCap()
    {
        Assert.IsFalse(Stream(distToEnd: 71f, lookAhead: 70f));
        Assert.IsTrue(Stream(distToEnd: 69f, lookAhead: 70f));
        Assert.IsFalse(Stream(chunks: 5, maxChunks: 5));
    }
}
