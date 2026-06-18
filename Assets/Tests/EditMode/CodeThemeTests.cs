using NUnit.Framework;

public class CodeThemeTests
{
    [Test]
    public void Library_ContainsDefaultDarkPlus()
    {
        CodeTheme theme = CodeThemeLibrary.Get(0);
        Assert.AreEqual("Dark+", theme.name);
        Assert.AreEqual(0, theme.cost);
    }

    [Test]
    public void Library_FallsBackToDefaultForUnknownId()
    {
        CodeTheme theme = CodeThemeLibrary.Get(999);
        Assert.AreEqual(0, theme.id);
        Assert.AreEqual("Dark+", theme.name);
    }

    [Test]
    public void SaveData_DefaultThemeIsUnlocked()
    {
        var data = new SaveData();
        Assert.IsTrue(data.HasTheme(0));
        Assert.IsFalse(data.HasTheme(1));
    }

    [Test]
    public void SaveData_UnlockTheme_IsRemembered()
    {
        var data = new SaveData();
        data.UnlockTheme(1);
        Assert.IsTrue(data.HasTheme(1));
    }
}
