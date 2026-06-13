using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// EditMode tests for the authored dialogue content: validation, coverage,
/// spoiler gating, and reveal-line integrity.
/// </summary>
public class DialogueLibraryTests
{
    [Test]
    public void EveryConversation_Validates()
    {
        for (int level = 0; level <= 5; level++)
        {
            DialogueConversation convo = DialogueLibrary.ForLevel(level);
            Assert.IsNotNull(convo, $"Level {level} conversation missing");
            string error = convo.Validate();
            Assert.IsNull(error, $"Level {level} validation failed: {error}");
        }

        DialogueConversation guimbal = DialogueLibrary.Guimbal();
        Assert.IsNotNull(guimbal);
        string gError = guimbal.Validate();
        Assert.IsNull(gError, $"Guimbal validation failed: {gError}");
    }

    [Test]
    public void EachLevel_HasPassengerHeritageAndJournalPage()
    {
        for (int level = 0; level <= 5; level++)
        {
            DialogueConversation convo = DialogueLibrary.ForLevel(level);
            Assert.IsNotNull(convo, $"Level {level} conversation missing");

            Assert.IsNotNull(PassengerLibrary.Get(convo.passengerId),
                $"Level {level} passenger '{convo.passengerId}' missing");

            HeritageEntry heritage = HeritageLibrary.ForLevel(level);
            Assert.IsNotNull(heritage, $"Level {level} heritage entry missing");

            Assert.IsTrue(convo.journalPageId >= 0 && convo.journalPageId < JournalPageLibrary.Pages.Count,
                $"Level {level} journalPageId {convo.journalPageId} out of range");
        }
    }

    [Test]
    public void SpoilerGate_NoEarlyTownMentioned()
    {
        for (int level = 0; level <= 5; level++)
        {
            DialogueConversation convo = DialogueLibrary.ForLevel(level);
            int routeIndex = RouteIndexForLevel(level);
            Assert.AreNotEqual(-1, routeIndex, $"Level {level} has no route index");

            IEnumerable<DialogueLine> spoken = convo.nodes.Values
                .SelectMany(n => n.lines ?? Array.Empty<DialogueLine>())
                .Where(l => l != null && !l.isReveal);

            foreach (DialogueLine line in spoken)
            {
                foreach (HeritageEntry town in HeritageLibrary.All)
                {
                    int townRoute = HeritageLibrary.RouteIndexOf(town.townKey);
                    if (townRoute <= routeIndex + 1)
                        continue; // current, earlier, and the next town are allowed

                    string search = town.townName.ToLowerInvariant();
                    if (line.text.ToLowerInvariant().Contains(search))
                    {
                        Assert.Fail($"Level {level} line by {line.speaker} mentions later town {town.townName}: {line.text}");
                    }

                    // Also guard against the shorter town name (e.g. "Miag-ao" inside "Miagao").
                    string shortName = town.townKey.Replace("iloilo-", "").Replace("-", "").ToLowerInvariant();
                    if (shortName.Length > 3 && line.text.ToLowerInvariant().Contains(shortName))
                    {
                        Assert.Fail($"Level {level} line by {line.speaker} mentions later town {town.townName}: {line.text}");
                    }
                }
            }
        }
    }

    [Test]
    public void EveryLevelRevealLines_AreRevealAndNonEmpty()
    {
        for (int level = 0; level <= 5; level++)
        {
            DialogueConversation convo = DialogueLibrary.ForLevel(level);
            Assert.IsNotNull(convo.revealLines, $"Level {level} revealLines null");
            Assert.IsTrue(convo.revealLines.Length > 0, $"Level {level} has no revealLines");

            foreach (DialogueLine line in convo.revealLines)
            {
                Assert.IsTrue(line.isReveal, $"Level {level} reveal line not flagged isReveal");
                Assert.IsFalse(string.IsNullOrWhiteSpace(line.text), $"Level {level} reveal line empty");
            }
        }
    }

    static int RouteIndexForLevel(int levelIndex)
    {
        HeritageEntry entry = HeritageLibrary.ForLevel(levelIndex);
        if (entry == null) return -1;
        return HeritageLibrary.RouteIndexOf(entry.townKey);
    }
}
