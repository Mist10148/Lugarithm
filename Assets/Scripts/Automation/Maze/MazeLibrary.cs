/// <summary>
/// The curated, escalating intro series for the Maze Lab: a fixed handful of
/// perfect mazes of increasing size, produced deterministically from
/// <see cref="MazeGenerator"/> (so they're guaranteed solvable and the EditMode
/// tests pin them). After this series the lab continues with endless generated
/// mazes. Kept small so the right-hand wall-follower always finishes well within
/// the interpreter's 1000-action guard.
/// </summary>
public static class MazeLibrary
{
    // (cols, rows, seed) — sizes ramp up; seeds are arbitrary but fixed.
    static readonly (int cols, int rows, int seed)[] Specs =
    {
        (3, 3, 1011),
        (4, 4, 2027),
        (5, 5, 3041),
        (6, 6, 4099),
    };

    public static int Count => Specs.Length;

    /// <summary>Authored maze <paramref name="index"/> (0..Count-1).</summary>
    public static AutomationPuzzleDefinition Get(int index)
    {
        if (index < 0) index = 0;
        if (index >= Specs.Length) index = Specs.Length - 1;

        (int cols, int rows, int seed) spec = Specs[index];
        AutomationPuzzleDefinition def = MazeGenerator.Generate(spec.cols, spec.rows, spec.seed);
        def.goalText = $"Maze {index + 1} of {Count}: escape to D. Keep one hand on the wall.";
        return def;
    }
}
