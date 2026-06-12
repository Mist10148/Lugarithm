/// <summary>
/// Shared command surface and reference solution for every maze in the Maze Lab.
/// All mazes are navigation-only (no passengers), so the palette is the move/turn
/// actions plus the wall-following conditionals — exactly the Reeborg's-World
/// vocabulary. The wall-follower below solves any perfect (loop-free) maze, which
/// is what <see cref="MazeGenerator"/> and <see cref="MazeLibrary"/> only ever
/// produce.
/// </summary>
public static class MazeContent
{
    public static readonly string[] Blocks =
        { "moveForward", "turnLeft", "turnRight", "while", "if", "ifElse" };

    public static readonly string[] Queries =
        { "frontIsClear", "leftIsClear", "rightIsClear", "atDestination" };

    public const string Scaffold =
        "# Escape the maze: reach the destination (D).\n" +
        "# Trick: keep one hand on the wall and never let go.\n" +
        "# Queries: frontIsClear(), leftIsClear(), rightIsClear(), atDestination()\n" +
        "# Actions: moveForward(), turnLeft(), turnRight()\n";

    /// <summary>Right-hand wall-follower — solves any perfect maze.</summary>
    public const string WallFollower =
        "while not atDestination():\n" +
        "    if rightIsClear():\n" +
        "        turnRight()\n" +
        "        moveForward()\n" +
        "    else:\n" +
        "        if frontIsClear():\n" +
        "            moveForward()\n" +
        "        else:\n" +
        "            turnLeft()\n";
}
