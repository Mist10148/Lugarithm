using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure rules for unlocking and placing the optional overworld artifact.
/// Keeping the flood-fill outside the scene controller makes reachability
/// deterministic and directly testable.
/// </summary>
public static class OverworldArtifactPlacement
{
    static readonly Vector2Int[] Directions =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right,
    };

    /// <summary>Jeep stop is the canonical overworld spawn; PlayerStart remains a
    /// compatibility fallback for maps authored before that rule.</summary>
    public static bool TryGetPreferredSpawnCell(OverworldMapData map, out Vector2Int cell)
    {
        cell = default;
        if (map == null || map.entities == null) return false;

        MapEntity fallback = null;
        foreach (MapEntity entity in map.entities)
        {
            if (entity.type == EntityType.JeepStop)
            {
                cell = new Vector2Int(entity.gridX, entity.gridY);
                return true;
            }
            if (entity.type == EntityType.PlayerStart && fallback == null)
                fallback = entity;
        }

        if (fallback == null) return false;
        cell = new Vector2Int(fallback.gridX, fallback.gridY);
        return true;
    }

    public static bool AreObjectivesComplete(
        ICollection<string> solvedStationIds,
        string mainQuestId,
        int sideObjectiveCount)
    {
        if (solvedStationIds == null || string.IsNullOrEmpty(mainQuestId) ||
            !solvedStationIds.Contains(mainQuestId))
            return false;

        int solvedSide = 0;
        foreach (string id in solvedStationIds)
            if (id != mainQuestId) solvedSide++;

        return solvedSide >= sideObjectiveCount;
    }

    /// <summary>
    /// Chooses an in-bounds, walkable cell reachable from <paramref name="origin"/>.
    /// Interior cells are preferred so the artifact never appears on the visual edge;
    /// small maps fall back to any valid reachable cell.
    /// </summary>
    public static bool TryChooseCell(
        OverworldMapData map,
        Vector2Int origin,
        ISet<Vector2Int> excluded,
        System.Random random,
        out Vector2Int chosen)
    {
        chosen = default;
        if (map == null || random == null || !IsWalkable(map, origin)) return false;

        var visited = new HashSet<Vector2Int> { origin };
        var queue = new Queue<Vector2Int>();
        var interior = new List<Vector2Int>();
        var fallback = new List<Vector2Int>();
        queue.Enqueue(origin);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            if ((excluded == null || !excluded.Contains(current)) && current != origin)
            {
                fallback.Add(current);
                if (current.x > 0 && current.x < map.width - 1 &&
                    current.y > 0 && current.y < map.height - 1)
                    interior.Add(current);
            }

            foreach (Vector2Int direction in Directions)
            {
                Vector2Int next = current + direction;
                if (visited.Contains(next) || !IsWalkable(map, next)) continue;
                visited.Add(next);
                queue.Enqueue(next);
            }
        }

        List<Vector2Int> candidates = interior.Count > 0 ? interior : fallback;
        if (candidates.Count == 0) return false;

        chosen = candidates[random.Next(candidates.Count)];
        return true;
    }

    public static bool IsReachable(
        OverworldMapData map,
        Vector2Int origin,
        Vector2Int target)
    {
        if (!IsWalkable(map, origin) || !IsWalkable(map, target)) return false;

        var visited = new HashSet<Vector2Int> { origin };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(origin);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            if (current == target) return true;

            foreach (Vector2Int direction in Directions)
            {
                Vector2Int next = current + direction;
                if (visited.Contains(next) || !IsWalkable(map, next)) continue;
                visited.Add(next);
                queue.Enqueue(next);
            }
        }

        return false;
    }

    static bool IsWalkable(OverworldMapData map, Vector2Int cell)
    {
        return map != null &&
               cell.x >= 0 && cell.x < map.width &&
               cell.y >= 0 && cell.y < map.height &&
               !map.IsSolid(cell.x, cell.y);
    }
}
