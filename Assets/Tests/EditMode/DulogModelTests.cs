using NUnit.Framework;

/// <summary>EditMode tests for the shared dulog (alight) highlighting math.</summary>
public class DulogModelTests
{
    [Test]
    public void Approach01_RampsFromZeroFarToOneInRange()
    {
        Assert.AreEqual(0f, DulogModel.Approach01(DulogModel.ApproachRange + 5f), 1e-4f, "far off");
        Assert.AreEqual(0f, DulogModel.Approach01(DulogModel.ApproachRange), 1e-4f, "edge of range");
        Assert.AreEqual(1f, DulogModel.Approach01(DulogModel.RequestRange), 1e-4f, "at request range");
        Assert.AreEqual(1f, DulogModel.Approach01(0f), 1e-4f, "right on top");
    }

    [Test]
    public void Approach01_IsMonotonicAsTheJeepneyCloses()
    {
        float prev = -1f;
        for (float d = DulogModel.ApproachRange; d >= 0f; d -= 1f)
        {
            float a = DulogModel.Approach01(d);
            Assert.GreaterOrEqual(a, prev, $"approach must not drop as distance {d} shrinks");
            prev = a;
        }
    }

    [Test]
    public void State_TransitionsOnboardThenApproachingThenInRange()
    {
        Assert.AreEqual(DulogState.Onboard,     DulogModel.State(DulogModel.ApproachRange + 1f));
        Assert.AreEqual(DulogState.Approaching, DulogModel.State((DulogModel.ApproachRange + DulogModel.RequestRange) / 2f));
        Assert.AreEqual(DulogState.InRange,     DulogModel.State(DulogModel.RequestRange - 1f));
    }
}
