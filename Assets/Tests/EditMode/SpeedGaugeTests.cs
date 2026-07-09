using NUnit.Framework;

public class SpeedGaugeTests
{
    [Test]
    public void Normalize_MapsZeroToTopSpeedOntoUnitRange()
    {
        Assert.AreEqual(0f, SpeedGauge.Normalize(0f), 1e-5f);
        Assert.AreEqual(0.5f, SpeedGauge.Normalize(SpeedGauge.TopSpeed * 0.5f), 1e-5f);
        Assert.AreEqual(1f, SpeedGauge.Normalize(SpeedGauge.TopSpeed), 1e-5f);
        Assert.AreEqual(1f, SpeedGauge.Normalize(SpeedGauge.TopSpeed * 3f), 1e-5f,
            "faster-than-top playback must clamp, not overshoot the dial");
    }

    [Test]
    public void FormatKph_UsesDisplayScale_AndNeverGoesNegative()
    {
        Assert.AreEqual("40 km/h", SpeedGauge.FormatKph(SpeedGauge.TopSpeed));
        Assert.AreEqual("0 km/h", SpeedGauge.FormatKph(0f));
        Assert.AreEqual("0 km/h", SpeedGauge.FormatKph(-1f));
        Assert.AreEqual(20, SpeedGauge.ToKph(2f));
    }
}
