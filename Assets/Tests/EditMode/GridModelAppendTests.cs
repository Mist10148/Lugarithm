using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode tests for <see cref="GridModel.AppendChunk"/>.
/// </summary>
public class GridModelAppendTests
{
    static GridModel SampleMap()
    {
        string[] map = new[]
        {
            "####",
            "#SD#",
            "####",
        };
        return GridModel.Parse(map, out List<string> errors);
    }

    [Test]
    public void AppendChunk_IncreasesBounds()
    {
        GridModel grid = SampleMap();
        Assert.AreEqual(4, grid.Width);
        Assert.AreEqual(3, grid.Height);

        grid.AppendChunk(2, 2, new[] { "....", "...." }, rowOffset: 3, colOffset: 0);

        Assert.AreEqual(6, grid.Width);
        Assert.AreEqual(5, grid.Height);
    }

    [Test]
    public void AppendChunk_KeepsOriginalCells()
    {
        GridModel grid = SampleMap();
        grid.AppendChunk(2, 0, new[] { "....", "...." }, rowOffset: 3, colOffset: 0);

        Assert.AreEqual(GridModel.Cell.Start, grid.Get(1, 1));
        Assert.AreEqual(GridModel.Cell.Destination, grid.Get(2, 1));
    }

    [Test]
    public void AppendChunk_RegistersNewStop()
    {
        GridModel grid = SampleMap();
        grid.AppendChunk(2, 0, new[] { "P...", "...." }, rowOffset: 3, colOffset: 0);

        Assert.Contains(new Vector2Int(0, 3), (List<Vector2Int>)grid.StopCells);
    }

    [Test]
    public void AppendChunk_OutOfBounds_ReadsAsWall()
    {
        GridModel grid = SampleMap();
        grid.AppendChunk(2, 2, new[] { "..", ".." }, rowOffset: 3, colOffset: 0);

        Assert.AreEqual(GridModel.Cell.Wall, grid.Get(10, 10));
    }
}
