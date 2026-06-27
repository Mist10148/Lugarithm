using NUnit.Framework;

/// <summary>EditMode tests for run-wallet spending helpers.</summary>
public class GameManagerCurrencyTests
{
    [Test]
    public void SpendCurrency_UsesPendingBeforeSaved()
    {
        var save = new SaveData { currency = 100 };
        int pending = 30;

        int spent = GameManager.SpendCurrency(save, ref pending, 50);

        Assert.AreEqual(50, spent);
        Assert.AreEqual(0, pending);
        Assert.AreEqual(80, save.currency);
    }

    [Test]
    public void SpendCurrency_ClampsAtAvailableWallet()
    {
        var save = new SaveData { currency = 10 };
        int pending = 5;

        int spent = GameManager.SpendCurrency(save, ref pending, 100);

        Assert.AreEqual(15, spent);
        Assert.AreEqual(0, pending);
        Assert.AreEqual(0, save.currency);
    }

    [Test]
    public void SpendCurrency_NonPositiveAmount_IsNoOp()
    {
        var save = new SaveData { currency = 10 };
        int pending = 5;

        int spent = GameManager.SpendCurrency(save, ref pending, 0);

        Assert.AreEqual(0, spent);
        Assert.AreEqual(5, pending);
        Assert.AreEqual(10, save.currency);
    }
}
