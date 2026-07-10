using NUnit.Framework;

public class TopDownObjectiveTests
{
    [Test]
    public void EveryPlayableTown_HasSixStationDefinitions()
    {
        for (int level = 0; level <= 5; level++)
        {
            MinigameStationDef[] defs = TownMinigameLibrary.ForLevel(level);

            int puzzleCount = 0;
            int codingCount = 0;
            bool hasFlow = false;
            bool hasCrate = false;
            bool hasCoding = false;
            bool hasCodingMaze = false;
            int mainObjectiveCount = 0;
            int sideObjectiveCount = 0;

            foreach (MinigameStationDef def in defs)
            {
                if (def.IsCoding) codingCount++;
                else puzzleCount++;
                hasFlow |= def.kind == MinigamePuzzleKind.FlowConnect;
                hasCrate |= def.kind == MinigamePuzzleKind.CrateStack;
                hasCoding |= def.kind == MinigamePuzzleKind.Coding;
                hasCodingMaze |= def.kind == MinigamePuzzleKind.CodingMaze;
                if (def.isMainQuest) mainObjectiveCount++;
                else sideObjectiveCount++;
            }

            Assert.AreEqual(6, defs.Length, $"level {level} station definition count");
            Assert.AreEqual(4, puzzleCount, $"level {level} puzzle station count");
            Assert.AreEqual(2, codingCount, $"level {level} coding station count");
            Assert.IsTrue(hasFlow, $"level {level} includes transferred FlowConnect station");
            Assert.IsTrue(hasCrate, $"level {level} includes transferred CrateStack station");
            Assert.IsTrue(hasCoding, $"level {level} includes CodeOrder station");
            Assert.IsTrue(hasCodingMaze, $"level {level} includes CodingMaze station");
            Assert.AreEqual(1, mainObjectiveCount, $"level {level} main objective count");
            Assert.AreEqual(5, sideObjectiveCount, $"level {level} side objective count");
            Assert.IsTrue(System.Array.Exists(defs,
                def => def.isMainQuest && def.kind == MinigamePuzzleKind.CodingMaze),
                $"level {level} coding maze is the main objective");
            Assert.IsTrue(System.Array.Exists(defs,
                def => !def.isMainQuest && def.kind == MinigamePuzzleKind.Coding),
                $"level {level} CodeOrder is a side objective");
        }
    }

    [Test]
    public void EveryPlayableTown_MapHasFourPuzzleStationsAndTwoCodingStations()
    {
        for (int level = 0; level <= 5; level++)
        {
            OverworldMapData map = OverworldMapLibrary.ForLevel(level);
            int puzzleStations = 0;
            int codingStations = 0;

            foreach (MapEntity entity in map.entities)
            {
                if (entity.type == EntityType.PuzzleStation) puzzleStations++;
                if (entity.type == EntityType.CodeChallenge) codingStations++;
            }

            Assert.AreEqual(4, puzzleStations, $"level {level} Q station count");
            Assert.AreEqual(2, codingStations, $"level {level} C station count");
        }
    }
}
