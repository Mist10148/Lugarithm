using NUnit.Framework;

/// <summary>EditMode tests for the Oton Crate-Stack puzzle logic.</summary>
public class CrateStackTests
{
    [Test]
    public void NewPuzzle_StartsUnsorted()
    {
        for (int seed = 0; seed < 100; seed++)
        {
            var p = new CrateStackPuzzle(5, seed);
            Assert.AreEqual(5, p.Count);
            Assert.IsFalse(p.IsSolved(), $"seed {seed} should not begin solved");
        }
    }

    [Test]
    public void SortingHeaviestToBottom_Solves()
    {
        var p = new CrateStackPuzzle(5, 12345);

        bool changed = true;
        int guard = 0;
        while (changed && guard++ < 1000)
        {
            changed = false;
            for (int i = 0; i < p.Count - 1; i++)
                if (p.Order[i] > p.Order[i + 1]) { p.Move(i, +1); changed = true; }
        }

        Assert.IsTrue(p.IsSolved());
        for (int i = 1; i < p.Count; i++)
            Assert.Less(p.Order[i - 1], p.Order[i], "weights strictly increase downward");
    }

    [Test]
    public void Move_SwapsNeighbours_AndRejectsOffEnds()
    {
        var p = new CrateStackPuzzle(3, 7);
        int top = p.Order[0], second = p.Order[1];

        Assert.IsTrue(p.Move(0, +1));
        Assert.AreEqual(second, p.Order[0]);
        Assert.AreEqual(top,    p.Order[1]);

        Assert.IsFalse(p.Move(0, -1),            "cannot move the top crate up");
        Assert.IsFalse(p.Move(p.Count - 1, +1),  "cannot move the bottom crate down");
    }

    [Test]
    public void SingleCrate_IsTriviallySolved()
    {
        Assert.IsTrue(new CrateStackPuzzle(1, 3).IsSolved());
    }
}
