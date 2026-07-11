using NUnit.Framework;

public class ArtifactProximityAudioTests
{
    [Test]
    public void Volume_IsFullNearArtifact_AndSilentAtFarLimit()
    {
        Assert.AreEqual(0.8f, ArtifactProximityAudio.EvaluateVolume(1f, 1.5f, 24f, 0.8f), 0.0001f);
        Assert.AreEqual(0f, ArtifactProximityAudio.EvaluateVolume(24f, 1.5f, 24f, 0.8f), 0.0001f);
    }

    [Test]
    public void Volume_RisesMonotonicallyAsPlayerApproaches()
    {
        float far = ArtifactProximityAudio.EvaluateVolume(20f, 1.5f, 24f);
        float middle = ArtifactProximityAudio.EvaluateVolume(12f, 1.5f, 24f);
        float near = ArtifactProximityAudio.EvaluateVolume(4f, 1.5f, 24f);

        Assert.Less(far, middle);
        Assert.Less(middle, near);
    }

    [Test]
    public void Volume_ClampsOutsideDistanceRange()
    {
        Assert.AreEqual(1f, ArtifactProximityAudio.EvaluateVolume(-5f, 1.5f, 24f), 0.0001f);
        Assert.AreEqual(0f, ArtifactProximityAudio.EvaluateVolume(50f, 1.5f, 24f), 0.0001f);
    }
}
