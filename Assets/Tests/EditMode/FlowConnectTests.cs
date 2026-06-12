using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode tests for the FlowConnect board logic and the authored Molo layouts.
/// Each layout's reference solution must satisfy IsSolved, and the board must
/// reject crossings — the whole point of "non-intersecting connections".
/// </summary>
public class FlowConnectTests
{
    static void ApplySolution(FlowConnectBoard board, FlowConnectLayout layout)
    {
        for (int c = 0; c < layout.Pairs.Length; c++)
        {
            Vector2Int[] path = layout.Solution[c];
            Assert.IsTrue(board.Start(c, path[0]), $"start colour {c}");
            for (int i = 1; i < path.Length; i++)
                Assert.IsTrue(board.Extend(c, path[i]),
                    $"extend colour {c} to {path[i]} (step {i})");
        }
    }

    [Test]
    public void EveryLayout_IsSolvedByItsReferenceSolution()
    {
        foreach (FlowConnectLayout layout in FlowConnectLayouts.All)
        {
            var board = new FlowConnectBoard(layout.Width, layout.Height, layout.Pairs);
            Assert.IsFalse(board.IsSolved(), "an empty board is not solved");

            ApplySolution(board, layout);
            Assert.IsTrue(board.IsSolved(), "the reference solution must solve the board");
        }
    }

    [Test]
    public void LayoutSolutions_MatchTheirHubs()
    {
        foreach (FlowConnectLayout layout in FlowConnectLayouts.All)
        {
            Assert.AreEqual(layout.Pairs.Length, layout.Solution.Length);
            for (int c = 0; c < layout.Pairs.Length; c++)
            {
                Vector2Int[] sol = layout.Solution[c];
                Assert.GreaterOrEqual(sol.Length, 2, $"colour {c} path needs both hubs");
                Assert.AreEqual(layout.Pairs[c].A, sol[0],               $"colour {c} starts at hub A");
                Assert.AreEqual(layout.Pairs[c].B, sol[sol.Length - 1],  $"colour {c} ends at hub B");
            }
        }
    }

    [Test]
    public void Get_IsDeterministicAndWraps()
    {
        Assert.AreSame(FlowConnectLayouts.Get(0),
                       FlowConnectLayouts.Get(FlowConnectLayouts.All.Length));
        Assert.AreSame(FlowConnectLayouts.Get(FlowConnectLayouts.All.Length - 1),
                       FlowConnectLayouts.Get(-1));
    }

    [Test]
    public void Extend_RejectsCrossingAnotherColoursPath()
    {
        var pairs = new[]
        {
            new FlowPair(new Vector2Int(0, 0), new Vector2Int(2, 0)),  // colour 0 across the top
            new FlowPair(new Vector2Int(1, 1), new Vector2Int(1, 2)),  // colour 1 below
        };
        var board = new FlowConnectBoard(3, 3, pairs);

        Assert.IsTrue(board.Start(0, new Vector2Int(0, 0)));
        Assert.IsTrue(board.Extend(0, new Vector2Int(1, 0)));
        Assert.IsTrue(board.Extend(0, new Vector2Int(2, 0)));
        Assert.IsTrue(board.IsComplete(0));

        Assert.IsTrue(board.Start(1, new Vector2Int(1, 1)));
        Assert.IsFalse(board.Extend(1, new Vector2Int(1, 0)),
            "colour 1 must not enter a cell already used by colour 0");
    }

    [Test]
    public void Extend_RejectsAnotherColoursHub()
    {
        var pairs = new[]
        {
            new FlowPair(new Vector2Int(0, 0), new Vector2Int(2, 2)),
            new FlowPair(new Vector2Int(1, 0), new Vector2Int(2, 0)),
        };
        var board = new FlowConnectBoard(3, 3, pairs);

        Assert.IsTrue(board.Start(0, new Vector2Int(0, 0)));
        Assert.IsFalse(board.Extend(0, new Vector2Int(1, 0)),
            "cannot route through colour 1's hub at (1,0)");
    }

    [Test]
    public void Extend_BacktrackingTruncatesThePath()
    {
        var pairs = new[] { new FlowPair(new Vector2Int(0, 0), new Vector2Int(0, 2)) };
        var board = new FlowConnectBoard(1, 3, pairs);

        Assert.IsTrue(board.Start(0, new Vector2Int(0, 0)));
        Assert.IsTrue(board.Extend(0, new Vector2Int(0, 1)));
        Assert.AreEqual(0, board.Owner(new Vector2Int(0, 1)));

        Assert.IsTrue(board.Extend(0, new Vector2Int(0, 0)), "stepping back is allowed");
        Assert.AreEqual(-1, board.Owner(new Vector2Int(0, 1)), "the abandoned cell is freed");
    }
}
