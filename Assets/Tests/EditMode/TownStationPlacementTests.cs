using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Placement invariants for every town's Q/C minigame stations: reachable from
/// spawn, beside a real road tile in the tile towns (levels 1–5; the tutorial
/// pins bench cells in <see cref="TutorialPlazaMapTests"/>), never crowding
/// another interactable, and the main quest bound to its documented cell.
/// </summary>
public class TownStationPlacementTests
{
    const int LevelCount = 6;

    // The second row-major C entity binds to the CodingMaze main quest
    // (canonical def order: side Coding first, main CodingMaze last).
    static readonly Dictionary<int, Vector2Int> MainQuestCells = new Dictionary<int, Vector2Int>
    {
        { 0, new Vector2Int(9, 31) },   // Tutorial
        { 1, new Vector2Int(8, 7) },    // Molo
        { 2, new Vector2Int(11, 6) },   // Oton
        { 3, new Vector2Int(10, 6) },   // Tigbauan
        { 4, new Vector2Int(11, 5) },   // Miagao
        { 5, new Vector2Int(11, 4) },   // San Joaquin
    };

    [Test]
    public void AllLevels_EveryEntityIsWalkableAndReachableFromSpawn()
    {
        for (int level = 0; level < LevelCount; level++)
        {
            OverworldMapData map = OverworldMapLibrary.ForLevel(level);
            MapEntity start = map.entities.Find(e => e.type == EntityType.PlayerStart);
            Assert.IsNotNull(start, $"level {level} has no player start");

            HashSet<Vector2Int> reachable = FloodFill(map,
                new Vector2Int(start.gridX, start.gridY));

            foreach (MapEntity entity in map.entities)
            {
                Assert.AreEqual(TileType.Path, map.GetTile(entity.gridX, entity.gridY),
                    $"level {level}: {entity.type} at ({entity.gridX},{entity.gridY}) is not on a path");
                Assert.IsTrue(reachable.Contains(new Vector2Int(entity.gridX, entity.gridY)),
                    $"level {level}: {entity.type} at ({entity.gridX},{entity.gridY}) is unreachable from spawn");
            }
        }
    }

    [Test]
    public void TileTowns_StationsSitOnOrBesideARoadTile()
    {
        for (int level = 1; level < LevelCount; level++)
        {
            OverworldMapData map = OverworldMapLibrary.ForLevel(level);

            // Entity cells are forced to Path by the parser, so "real road" means
            // a Path tile no entity occupies.
            var entityCells = new HashSet<Vector2Int>();
            foreach (MapEntity e in map.entities)
                entityCells.Add(new Vector2Int(e.gridX, e.gridY));

            foreach (MapEntity entity in map.entities)
            {
                if (entity.type != EntityType.PuzzleStation &&
                    entity.type != EntityType.CodeChallenge)
                    continue;

                bool besideRoad = false;
                foreach (Vector2Int d in new[]
                         { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                {
                    var n = new Vector2Int(entity.gridX + d.x, entity.gridY + d.y);
                    if (map.GetTile(n.x, n.y) == TileType.Path && !entityCells.Contains(n))
                    {
                        besideRoad = true;
                        break;
                    }
                }

                Assert.IsTrue(besideRoad,
                    $"level {level}: station at ({entity.gridX},{entity.gridY}) must sit on or beside a road tile");
            }
        }
    }

    [Test]
    public void AllLevels_InteractablesKeepTwoCellsApart()
    {
        for (int level = 0; level < LevelCount; level++)
        {
            OverworldMapData map = OverworldMapLibrary.ForLevel(level);

            // Player start is spawn-only (no trigger), so it may sit closer.
            List<MapEntity> interactables = map.entities.FindAll(
                e => e.type != EntityType.PlayerStart);

            for (int i = 0; i < interactables.Count; i++)
                for (int j = i + 1; j < interactables.Count; j++)
                {
                    MapEntity a = interactables[i];
                    MapEntity b = interactables[j];
                    int manhattan = Mathf.Abs(a.gridX - b.gridX) + Mathf.Abs(a.gridY - b.gridY);
                    Assert.GreaterOrEqual(manhattan, 2,
                        $"level {level}: {a.type} at ({a.gridX},{a.gridY}) crowds {b.type} at ({b.gridX},{b.gridY})");
                }
        }
    }

    [Test]
    public void AllLevels_MainQuestBindsToDocumentedCell()
    {
        for (int level = 0; level < LevelCount; level++)
        {
            // Mirror TopDownLevelController.SpawnEntities: coding defs bind to
            // C entities in row-major order.
            var codingDefs = new List<MinigameStationDef>();
            foreach (MinigameStationDef def in TownMinigameLibrary.ForLevel(level))
                if (def.IsCoding) codingDefs.Add(def);

            OverworldMapData map = OverworldMapLibrary.ForLevel(level);
            List<MapEntity> codingCells = map.entities.FindAll(
                e => e.type == EntityType.CodeChallenge);

            Assert.AreEqual(codingDefs.Count, codingCells.Count,
                $"level {level}: C cell count must match the coding def count");

            Vector2Int mainCell = default;
            bool found = false;
            for (int i = 0; i < codingDefs.Count; i++)
            {
                if (!codingDefs[i].isMainQuest) continue;
                mainCell = new Vector2Int(codingCells[i].gridX, codingCells[i].gridY);
                found = true;
            }

            Assert.IsTrue(found, $"level {level}: no main-quest coding def");
            Assert.AreEqual(MainQuestCells[level], mainCell,
                $"level {level}: main quest must bind to its documented cell");
        }
    }

    static HashSet<Vector2Int> FloodFill(OverworldMapData map, Vector2Int start)
    {
        var visited = new HashSet<Vector2Int> { start };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(start);

        Vector2Int[] directions =
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
        };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            foreach (Vector2Int d in directions)
            {
                Vector2Int next = current + d;
                if (next.x < 0 || next.x >= map.width ||
                    next.y < 0 || next.y >= map.height ||
                    visited.Contains(next) ||
                    map.IsSolid(next.x, next.y))
                    continue;

                visited.Add(next);
                queue.Enqueue(next);
            }
        }

        return visited;
    }
}
