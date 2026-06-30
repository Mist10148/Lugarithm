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

    [Test]
    public void EarnCurrency_NoDebt_AddsFullAmountAsRemainder()
    {
        var save = new SaveData { debt = 0 };

        int remainder = GameManager.EarnCurrency(save, 50);

        Assert.AreEqual(50, remainder);
        Assert.AreEqual(0, save.debt);
    }

    [Test]
    public void EarnCurrency_PaysDownDebtFirst_RemainderIsLeftover()
    {
        var save = new SaveData { debt = 30 };

        int remainder = GameManager.EarnCurrency(save, 50);

        Assert.AreEqual(20, remainder);
        Assert.AreEqual(0, save.debt);
    }

    [Test]
    public void EarnCurrency_DebtExceedsEarnings_AllGoesToDebt_NoRemainder()
    {
        var save = new SaveData { debt = 100 };

        int remainder = GameManager.EarnCurrency(save, 40);

        Assert.AreEqual(0, remainder);
        Assert.AreEqual(60, save.debt);
    }

    [Test]
    public void EarnCurrency_NonPositiveAmount_IsNoOp()
    {
        var save = new SaveData { debt = 10 };

        int remainder = GameManager.EarnCurrency(save, 0);

        Assert.AreEqual(0, remainder);
        Assert.AreEqual(10, save.debt);
    }

    [Test]
    public void ShortfallToDebt_SufficientFunds_NoDebtAccrued()
    {
        var save = new SaveData { currency = 100, debt = 0 };
        int pending = 0;

        int shortfall = GameManager.ShortfallToDebt(save, ref pending, 50);

        Assert.AreEqual(0, shortfall);
        Assert.AreEqual(50, save.currency);
        Assert.AreEqual(0, save.debt);
    }

    [Test]
    public void ShortfallToDebt_InsufficientFunds_AddsShortfallToDebt_NotForgiven()
    {
        var save = new SaveData { currency = 10, debt = 0 };
        int pending = 5;

        int shortfall = GameManager.ShortfallToDebt(save, ref pending, 40);

        Assert.AreEqual(25, shortfall);
        Assert.AreEqual(0, pending);
        Assert.AreEqual(0, save.currency);
        Assert.AreEqual(25, save.debt);
    }

    [Test]
    public void ShortfallToDebt_AccumulatesAcrossMultipleUnderfundedSpends()
    {
        var save = new SaveData { currency = 0, debt = 10 };
        int pending = 0;

        GameManager.ShortfallToDebt(save, ref pending, 15);

        Assert.AreEqual(25, save.debt);
    }
}
