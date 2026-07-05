using NUnit.Framework;

/// <summary>EditMode tests for the level unlock frontier.</summary>
public class ProgressionRulesTests
{
    [Test]
    public void FreshSave_UnlocksAllPlayableLevels()
    {
        var save = new SaveData();
        for (int level = 0; level < ProgressionRules.LevelCount; level++)
            Assert.IsTrue(ProgressionRules.IsUnlocked(save, level), $"level {level} should be unlocked");
        Assert.IsFalse(ProgressionRules.IsCompleted(save, 5));
    }

    [Test]
    public void CompletingEarlyLevel_DoesNotRegressFullUnlockFrontier()
    {
        var save = new SaveData();
        ProgressionRules.CompleteLevel(save, 0);

        Assert.IsTrue(ProgressionRules.IsCompleted(save, 0));
        Assert.AreEqual(5, save.currentLevelIndex);
        Assert.IsTrue(ProgressionRules.IsUnlocked(save, 5));
    }

    [Test]
    public void ReplayingAnEarlyLevel_NeverRegressesTheFrontier()
    {
        var save = new SaveData { currentLevelIndex = 3 };
        ProgressionRules.CompleteLevel(save, 0);
        Assert.AreEqual(3, save.currentLevelIndex);
    }

    [Test]
    public void CompletingTheLastLevel_StillReadsAsCompleted()
    {
        var save = new SaveData { currentLevelIndex = 5 };
        ProgressionRules.CompleteLevel(save, 5);

        Assert.IsTrue(ProgressionRules.IsCompleted(save, 5));
        Assert.IsTrue(ProgressionRules.IsUnlocked(save, 5));
    }

    [Test]
    public void OutOfRangeIndices_AreSafe()
    {
        var save = new SaveData();
        ProgressionRules.CompleteLevel(save, 7);   // no-op
        ProgressionRules.CompleteLevel(save, -1);  // no-op

        Assert.AreEqual(5, save.currentLevelIndex);
        Assert.IsFalse(ProgressionRules.IsUnlocked(save, -1));
    }

    [Test]
    public void NullSave_TreatsOnlyTutorialAsUnlocked()
    {
        Assert.IsTrue(ProgressionRules.IsUnlocked(null, 0));
        Assert.IsFalse(ProgressionRules.IsUnlocked(null, 1));
        Assert.IsFalse(ProgressionRules.IsCompleted(null, 0));
    }
}
