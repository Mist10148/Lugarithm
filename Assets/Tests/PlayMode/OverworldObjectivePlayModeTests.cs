using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class OverworldObjectivePlayModeTests
{
    [UnityTest]
    public IEnumerator MazeDragMovesAndColorConnectUsesRotationBoard()
    {
        SceneManager.LoadScene("TopDownLevel");
        yield return null;
        yield return null;

        GridPuzzleMinigame grid = UnityEngine.Object.FindAnyObjectByType<GridPuzzleMinigame>(
            FindObjectsInactive.Include);
        Assert.IsNotNull(grid);

        MinigameStationDef mazeDef = Array.Find(
            TownMinigameLibrary.ForLevel(0), def => def.kind == MinigamePuzzleKind.Maze);
        grid.Begin(mazeDef, 0, 1234, () => { }, () => { });

        MazeLayout maze = ReadField<MazeLayout>(grid, "_mazeLayout");
        Vector2Int start = ReadField<Vector2Int>(grid, "_token");
        Vector2Int next = FindOpenNeighbour(maze, start);
        Assert.AreNotEqual(start, next);

        grid.CellPointerDown(start.y * GridPuzzleMinigame.Grid + start.x);
        grid.CellPointerEnter(next.y * GridPuzzleMinigame.Grid + next.x);
        grid.CellPointerUp(next.y * GridPuzzleMinigame.Grid + next.x);
        Assert.AreEqual(next, ReadField<Vector2Int>(grid, "_token"));
        Invoke(grid, "QuitOut");

        MinigameStationDef colorDef = Array.Find(
            TownMinigameLibrary.ForLevel(1), def => def.kind == MinigamePuzzleKind.ColorConnect);
        grid.Begin(colorDef, 1, 5678, () => { }, () => { });
        RouteRotationLayout layout = ReadField<RouteRotationLayout>(grid, "_routeLayout");
        RouteRotationBoard board = ReadField<RouteRotationBoard>(grid, "_routeBoard");
        Assert.IsNotNull(layout);
        Assert.IsNotNull(board);
        Assert.IsFalse(board.IsSolved());

        Vector2Int endpoint = FirstEndpoint(layout);
        int before = board.MaskAt(endpoint.x, endpoint.y);
        grid.CellSecondaryClick(endpoint.y * GridPuzzleMinigame.Grid + endpoint.x);
        Assert.AreNotEqual(before, board.MaskAt(endpoint.x, endpoint.y));
        Invoke(grid, "QuitOut");

        yield return CleanupTopDown();
    }

    [UnityTest]
    public IEnumerator TownCodingMazeTimerExpiryDoesNotCompleteObjective()
    {
        var gameObject = new GameObject("TimerRuleTest");
        LogAssert.Expect(LogType.Warning,
            "[MazeRepairMinigame] hintButton is not wired — hint UI will never appear.");
        MazeRepairMinigame maze = gameObject.AddComponent<MazeRepairMinigame>();
        bool completed = false;
        SetField(maze, "_onDone", new Action<MinigameResult>(_ => completed = true));
        SetField(maze, "_townChallenge", true);
        SetField(maze, "_active", true);
        SetField(maze, "_timedOut", false);
        SetField(maze, "_timeLeft", -0.1f);
        Invoke(maze, "Update");

        Assert.IsFalse(completed, "Timer expiry must never solve the main objective.");
        Assert.IsTrue(ReadField<bool>(maze, "_active"));
        Assert.IsTrue(ReadField<bool>(maze, "_timedOut"));

        UnityEngine.Object.Destroy(gameObject);
        yield return null;
    }

    static Vector2Int FindOpenNeighbour(MazeLayout maze, Vector2Int start)
    {
        Vector2Int[] directions = { Vector2Int.right, Vector2Int.down, Vector2Int.left, Vector2Int.up };
        foreach (Vector2Int direction in directions)
        {
            Vector2Int cell = start + direction;
            if (cell.x >= 0 && cell.y >= 0 && cell.x < maze.width && cell.y < maze.height &&
                !maze.wall[cell.y, cell.x]) return cell;
        }
        return start;
    }

    static Vector2Int FirstEndpoint(RouteRotationLayout layout)
    {
        for (int y = 0; y < layout.height; y++)
            for (int x = 0; x < layout.width; x++)
                if (layout.endpoints[y, x]) return new Vector2Int(x, y);
        return new Vector2Int(-1, -1);
    }

    static T ReadField<T>(object target, string name)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, name);
        return (T)field.GetValue(target);
    }

    static void SetField(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, name);
        field.SetValue(target, value);
    }

    static void Invoke(object target, string name)
    {
        MethodInfo method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, name);
        method.Invoke(target, null);
    }

    static IEnumerator CleanupTopDown()
    {
        Scene cleanup = SceneManager.CreateScene("ObjectiveTestCleanup_" + Guid.NewGuid());
        SceneManager.SetActiveScene(cleanup);
        yield return SceneManager.UnloadSceneAsync("TopDownLevel");
    }
}
