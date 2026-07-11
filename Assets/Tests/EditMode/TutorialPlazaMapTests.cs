using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class TutorialPlazaMapTests
{
    [Test]
    public void TutorialCharacterControllers_AreAvailable()
    {
        foreach (int characterIndex in new[] { 3, 5, 13, 15 })
        {
            string name = $"Townspeople_{characterIndex}_NPC_Animator";
            RuntimeAnimatorController controller =
                Resources.Load<RuntimeAnimatorController>(
                    $"TutorialCharacters/Townspeople_{characterIndex}/{name}");

            Assert.IsNotNull(controller, $"Missing controller for Townspeople_{characterIndex}.");
        }
    }

    [Test]
    public void TutorialMap_IsTwentyFourByThirtySix_WithNpcIdentityOrderPreserved()
    {
        OverworldMapData map = OverworldMapLibrary.TutorialMap();

        Assert.AreEqual(24, map.width);
        Assert.AreEqual(36, map.height);

        var npcIds = new List<string>();
        foreach (MapEntity entity in map.entities)
        {
            if (entity.type == EntityType.Npc)
                npcIds.Add(entity.npcId);
        }

        CollectionAssert.AreEqual(
            new[] { "il_vendor", "il_student", "il_tindera" },
            npcIds);

        List<MapEntity> npcs = map.entities.FindAll(entity => entity.type == EntityType.Npc);
        Assert.AreEqual(new Vector2Int(8, 8), new Vector2Int(npcs[0].gridX, npcs[0].gridY));
        Assert.AreEqual(new Vector2Int(16, 8), new Vector2Int(npcs[1].gridX, npcs[1].gridY));
        Assert.AreEqual(new Vector2Int(12, 24), new Vector2Int(npcs[2].gridX, npcs[2].gridY));

        // Stations sit on the benches painted into the plaza art
        // (TutorialHeritagePlaza.png, 1 cell = 16 px) — pinned per bench cell.
        List<MapEntity> puzzles = map.entities.FindAll(
            entity => entity.type == EntityType.PuzzleStation);
        Assert.AreEqual(4, puzzles.Count);
        Assert.AreEqual(new Vector2Int(7, 6), new Vector2Int(puzzles[0].gridX, puzzles[0].gridY));
        Assert.AreEqual(new Vector2Int(16, 6), new Vector2Int(puzzles[1].gridX, puzzles[1].gridY));
        Assert.AreEqual(new Vector2Int(8, 13), new Vector2Int(puzzles[2].gridX, puzzles[2].gridY));
        Assert.AreEqual(new Vector2Int(15, 13), new Vector2Int(puzzles[3].gridX, puzzles[3].gridY));

        // Coding stations: row-major first = side objective, second = main quest.
        List<MapEntity> coding = map.entities.FindAll(
            entity => entity.type == EntityType.CodeChallenge);
        Assert.AreEqual(2, coding.Count);
        Assert.AreEqual(new Vector2Int(16, 28), new Vector2Int(coding[0].gridX, coding[0].gridY));
        Assert.AreEqual(new Vector2Int(9, 31), new Vector2Int(coding[1].gridX, coding[1].gridY));

        Assert.AreEqual("Garage Maze", TownMinigameLibrary.ForLevel(0)[0].title);
        Assert.AreEqual("Capiz Window", TownMinigameLibrary.ForLevel(0)[1].title);
    }

    [Test]
    public void TutorialMap_AllEntitiesAreWalkableAndReachableFromSpawn()
    {
        OverworldMapData map = OverworldMapLibrary.TutorialMap();
        MapEntity start = map.entities.Find(entity => entity.type == EntityType.PlayerStart);
        Assert.IsNotNull(start);

        var visited = new HashSet<Vector2Int>();
        var queue = new Queue<Vector2Int>();
        var startCell = new Vector2Int(start.gridX, start.gridY);
        visited.Add(startCell);
        queue.Enqueue(startCell);

        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right,
        };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            foreach (Vector2Int direction in directions)
            {
                Vector2Int next = current + direction;
                if (next.x < 0 || next.x >= map.width ||
                    next.y < 0 || next.y >= map.height ||
                    visited.Contains(next) ||
                    map.GetTile(next.x, next.y) == TileType.Wall ||
                    map.GetTile(next.x, next.y) == TileType.Water)
                    continue;

                visited.Add(next);
                queue.Enqueue(next);
            }
        }

        foreach (MapEntity entity in map.entities)
        {
            Assert.AreEqual(
                TileType.Path,
                map.GetTile(entity.gridX, entity.gridY),
                $"{entity.type} at ({entity.gridX},{entity.gridY}) is not on a path.");
            Assert.IsTrue(
                visited.Contains(new Vector2Int(entity.gridX, entity.gridY)),
                $"{entity.type} at ({entity.gridX},{entity.gridY}) is unreachable.");
        }
    }
}
