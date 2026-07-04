using System;

/// <summary>
/// Town-hub coding maze profiles. These reuse the repair maze runner while
/// keeping the first town deliberately tiny and ramping toward the authored
/// maze-lab difficulty on later levels.
/// </summary>
public static class TownCodingMazeLibrary
{
    public static AutomationPuzzleDefinition ForLevel(int levelIndex)
    {
        if (levelIndex <= 0)
            return TutorialStraightLine();

        int mazeIndex = levelIndex - 1;
        AutomationPuzzleDefinition def = MazeLibrary.Get(mazeIndex);
        def.goalText = "Town coding maze: reach the destination (D). Keep one hand on the wall.";
        def.codeScaffold =
            "# Reach the town destination (D).\n" +
            "# Use the maze checks to follow the road safely.\n" +
            "# Queries: frontIsClear(), leftIsClear(), rightIsClear(), atDestination()\n" +
            "# Actions: moveForward(), turnLeft(), turnRight()\n";
        return def;
    }

    static AutomationPuzzleDefinition TutorialStraightLine()
    {
        const string solution =
            "moveForward()\n" +
            "moveForward()\n" +
            "moveForward()\n";

        return new AutomationPuzzleDefinition
        {
            gridMap = new[] { "S..D" },
            startFacing = 1,
            goalText = "Drive straight to the destination (D).",
            allowedBlocks = new[] { "moveForward" },
            allowedQueries = Array.Empty<string>(),
            allowedReporters = Array.Empty<string>(),
            parSteps = 3,
            softTimerSeconds = 90f,
            requireAllPassengersDelivered = false,
            codeScaffold =
                "# Drive straight to D.\n" +
                "# Use moveForward() three times.\n",
            optimalSolutionText = solution,
        };
    }
}
