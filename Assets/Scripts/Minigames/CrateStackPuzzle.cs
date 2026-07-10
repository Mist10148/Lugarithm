using System.Collections.Generic;

/// <summary>
/// Pure model for the Oton "Crate Stack" town puzzle: order a column of market
/// crates so the heaviest sits at the bottom — weights strictly increase from
/// the top slot downward. Index 0 is the top crate, Count-1 the bottom. No Unity
/// dependencies, so EditMode tests pin it.
/// </summary>
public class CrateStackPuzzle
{
    readonly List<int> _order = new List<int>();   // top → bottom weights (kg)

    public int Count => _order.Count;
    public IReadOnlyList<int> Order => _order;

    public CrateStackPuzzle(int count, int seed)
    {
        if (count < 1) count = 1;
        var rng = new System.Random(seed);

        var weights = new List<int>(count);
        for (int i = 1; i <= count; i++) weights.Add(i * 5);   // distinct: 5, 10, 15, …

        do { Shuffle(weights, rng); }
        while (count > 1 && IsSorted(weights));

        _order.AddRange(weights);
    }

    /// <summary>Swaps the crate at <paramref name="index"/> with a neighbour. True if it moved.</summary>
    public bool Move(int index, int dir)
    {
        int target = index + dir;
        if (index < 0 || index >= _order.Count || target < 0 || target >= _order.Count) return false;
        (_order[index], _order[target]) = (_order[target], _order[index]);
        return true;
    }

    public bool MoveTo(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= _order.Count) return false;
        toIndex = System.Math.Max(0, System.Math.Min(_order.Count - 1, toIndex));
        if (fromIndex == toIndex) return false;
        int value = _order[fromIndex];
        _order.RemoveAt(fromIndex);
        _order.Insert(toIndex, value);
        return true;
    }

    /// <summary>True when the heaviest crate is at the bottom (weights increase downward).</summary>
    public bool IsSolved() => IsSorted(_order);

    static bool IsSorted(List<int> list)
    {
        for (int i = 1; i < list.Count; i++)
            if (list[i] <= list[i - 1]) return false;
        return true;
    }

    static void Shuffle(List<int> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
