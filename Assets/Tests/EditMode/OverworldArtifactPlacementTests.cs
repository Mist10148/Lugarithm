using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class OverworldArtifactPlacementTests
{
    [Test]
    public void EveryPlayableTown_PrefersReachableJeepStopForPlayerSpawn()
    {
        for (int level = 0; level <= 5; level++)
        {
            OverworldMapData map = OverworldMapLibrary.ForLevel(level);
            Assert.IsTrue(OverworldArtifactPlacement.TryGetPreferredSpawnCell(map, out Vector2Int spawn));

            MapEntity jeep = map.entities.Find(entity => entity.type == EntityType.JeepStop);
            Assert.IsNotNull(jeep, $"level {level} jeep stop");
            Assert.AreEqual(new Vector2Int(jeep.gridX, jeep.gridY), spawn, $"level {level} spawn");
            Assert.IsFalse(map.IsSolid(spawn.x, spawn.y), $"level {level} spawn must be walkable");
        }
    }

    [Test]
    public void ArtifactUnlock_RequiresAllFiveSideObjectivesAndMainObjective()
    {
        var solved = new HashSet<string> { "side1", "side2", "side3", "side4", "side5" };
        Assert.IsFalse(OverworldArtifactPlacement.AreObjectivesComplete(solved, "main", 5));

        solved.Add("main");
        Assert.IsTrue(OverworldArtifactPlacement.AreObjectivesComplete(solved, "main", 5));

        solved.Remove("side5");
        Assert.IsFalse(OverworldArtifactPlacement.AreObjectivesComplete(solved, "main", 5));
    }

    [Test]
    public void EveryPlayableTown_ArtifactCellsAreReachableWalkableAndUnoccupied()
    {
        for (int level = 0; level <= 5; level++)
        {
            OverworldMapData map = OverworldMapLibrary.ForLevel(level);
            Assert.IsTrue(OverworldArtifactPlacement.TryGetPreferredSpawnCell(map, out Vector2Int origin));
            HashSet<Vector2Int> excluded = ExcludedCells(map);

            for (int seed = 0; seed < 20; seed++)
            {
                Assert.IsTrue(OverworldArtifactPlacement.TryChooseCell(
                    map, origin, excluded, new System.Random(seed), out Vector2Int cell),
                    $"level {level}, seed {seed}");
                Assert.That(cell.x, Is.InRange(0, map.width - 1));
                Assert.That(cell.y, Is.InRange(0, map.height - 1));
                Assert.IsFalse(map.IsSolid(cell.x, cell.y));
                Assert.IsFalse(excluded.Contains(cell));
                Assert.IsTrue(OverworldArtifactPlacement.IsReachable(map, origin, cell));
            }
        }
    }

    [Test]
    public void ArtifactSelection_IsDeterministicForSeedAndVariesAcrossInstances()
    {
        OverworldMapData map = OverworldMapLibrary.TutorialMap();
        Assert.IsTrue(OverworldArtifactPlacement.TryGetPreferredSpawnCell(map, out Vector2Int origin));
        HashSet<Vector2Int> excluded = ExcludedCells(map);

        Assert.IsTrue(OverworldArtifactPlacement.TryChooseCell(
            map, origin, excluded, new System.Random(42), out Vector2Int first));
        Assert.IsTrue(OverworldArtifactPlacement.TryChooseCell(
            map, origin, excluded, new System.Random(42), out Vector2Int repeated));
        Assert.AreEqual(first, repeated);

        var selections = new HashSet<Vector2Int>();
        for (int seed = 0; seed < 12; seed++)
        {
            Assert.IsTrue(OverworldArtifactPlacement.TryChooseCell(
                map, origin, excluded, new System.Random(seed), out Vector2Int cell));
            selections.Add(cell);
        }
        Assert.Greater(selections.Count, 1);
    }

    [Test]
    public void ArtifactSelection_FailsSafelyWhenNoCandidateExists()
    {
        OverworldMapData map = OverworldMapData.Parse(new[]
        {
            "WWW",
            "WJW",
            "WWW",
        });
        var origin = new Vector2Int(1, 1);

        Assert.IsFalse(OverworldArtifactPlacement.TryChooseCell(
            map, origin, new HashSet<Vector2Int> { origin },
            new System.Random(0), out _));
    }

    [Test]
    public void ArtifactHudLocalization_IsPresent()
    {
        Assert.IsTrue(LocalizationTable.Has("hud.artifactfound"));
        Assert.AreEqual("Artifact Found:",
            LocalizationTable.Get("hud.artifactfound", GameLanguage.English));
    }

    static HashSet<Vector2Int> ExcludedCells(OverworldMapData map)
    {
        var excluded = new HashSet<Vector2Int>();
        foreach (MapEntity entity in map.entities)
            for (int y = -1; y <= 1; y++)
                for (int x = -1; x <= 1; x++)
                    excluded.Add(new Vector2Int(entity.gridX + x, entity.gridY + y));
        return excluded;
    }
}
