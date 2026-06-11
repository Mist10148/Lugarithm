using System.Collections.Generic;

/// <summary>
/// Static library of all journal/almanac pages. One entry per level (0–5).
/// Placeholder text is explicitly tagged so the art/writing pass can find it.
/// </summary>
public static class JournalPageLibrary
{
    public static readonly IReadOnlyList<JournalPageDefinition> Pages = BuildPages();

    static List<JournalPageDefinition> BuildPages()
    {
        return new List<JournalPageDefinition>
        {
            new JournalPageDefinition
            {
                pageId = 0,
                heritageTitle = "Garage Notes",
                heritageBody =
                    "[ … placeholder heritage text for the Tutorial leg … ]\n\n" +
                    "Before we left Iloilo, your lolo made me promise I'd teach you " +
                    "the route the way he taught me: one stop at a time, no shortcuts.",
                artifactCardDescription = "[ … placeholder artifact card … ]",
                codingConceptName = "Linear Sequencing",
                codingReferenceBody =
                    "A program is just a list of instructions the jeepney follows in order. " +
                    "Put one command after another, and the jeepney does them one at a time.",
                codeExample =
                    "<mspace=0.6em>moveForward()\n" +
                    "moveForward()\n" +
                    "pickUp()\n" +
                    "dropOff()</mspace>"
            },

            new JournalPageDefinition
            {
                pageId = 1,
                heritageTitle = "Molo Back-Alleys",
                heritageBody =
                    "[ … placeholder heritage text for Iloilo City (Molo) … ]\n\n" +
                    "The plaza looked closer on the map. In real life the alleys turn back " +
                    "on themselves, and you can't just drive straight. You have to ask the " +
                    "road questions: is it clear in front? to the left? to the right?",
                artifactCardDescription = "[ … placeholder artifact card … ]",
                codingConceptName = "Conditionals (if / while)",
                codingReferenceBody =
                    "Conditionals let the jeepney make decisions. Use <b>if</b> to do something " +
                    "once when a condition is true, and <b>while</b> to keep doing it as long as " +
                    "the condition stays true.",
                codeExample =
                    "<mspace=0.6em>while not atDestination():\n" +
                    "    if frontIsClear():\n" +
                    "        moveForward()\n" +
                    "    else:\n" +
                    "        turnLeft()</mspace>"
            },

            new JournalPageDefinition
            {
                pageId = 2,
                heritageTitle = "Oton Market Run",
                heritageBody =
                    "[ … placeholder heritage text for Oton … ]\n\n" +
                    "The market stalls are numbered, and every passenger knows exactly which " +
                    "stop is theirs. You don't ask their name; you ask their index on the list.",
                artifactCardDescription = "[ … placeholder artifact card … ]",
                codingConceptName = "List Indexing",
                codingReferenceBody =
                    "A list keeps items in order. Each item has an index, starting from 0. " +
                    "Use the index to read or change a specific item in the list.",
                codeExample =
                    "<mspace=0.6em>stops = [\"Garage\", \"Market\", \"Terminal\"]\n" +
                    "next_stop = stops[1]   # Market</mspace>"
            },

            new JournalPageDefinition
            {
                pageId = 3,
                heritageTitle = "Tigbauan Crossing",
                heritageBody =
                    "[ … placeholder heritage text for Tigbauan … ]\n\n" +
                    "Every bridge here has a different toll. The fare you carry depends on where " +
                    "you started, where you're going, and how many passengers are on board.",
                artifactCardDescription = "[ … placeholder artifact card … ]",
                codingConceptName = "Function Parameters",
                codingReferenceBody =
                    "Functions can accept inputs called parameters. The same function can do " +
                    "slightly different work depending on the values you pass in.",
                codeExample =
                    "<mspace=0.6em>def collectFare(passengerCount):\n" +
                    "    return passengerCount * 12\n\n" +
                    "total = collectFare(3)</mspace>"
            },

            new JournalPageDefinition
            {
                pageId = 4,
                heritageTitle = "Miag-ao Hills",
                heritageBody =
                    "[ … placeholder heritage text for Miag-ao … ]\n\n" +
                    "The mountain road forks twice before the church tower even comes into view. " +
                    "Some mornings the left fork is flooded; other afternoons the right fork is " +
                    "blocked by a fallen branch. You have to check more than one thing at once.",
                artifactCardDescription = "[ … placeholder artifact card … ]",
                codingConceptName = "Nested Conditionals",
                codingReferenceBody =
                    "You can put an <b>if</b> inside another <b>if</b> or <b>else</b> to handle " +
                    "combinations of conditions. Check the outer condition first, then decide " +
                    "the inner one.",
                codeExample =
                    "<mspace=0.6em>if morning():\n" +
                    "    if leftForkFlooded():\n" +
                    "        takeRightFork()\n" +
                    "    else:\n" +
                    "        takeLeftFork()</mspace>"
            },

            new JournalPageDefinition
            {
                pageId = 5,
                heritageTitle = "San Joaquin Last Stop",
                heritageBody =
                    "[ … placeholder heritage text for San Joaquin … ]\n\n" +
                    "This is the end of the line. By now the jeepney is lighter, the fuel gauge " +
                    "is low, and every passenger has a different fare. You have to keep all of " +
                    "those facts true at the same time to finish the run.",
                artifactCardDescription = "[ … placeholder artifact card … ]",
                codingConceptName = "Multi-Variable Constraints",
                codingReferenceBody =
                    "Some problems require several variables to stay within limits together. " +
                    "Track fuel, fares, and capacity at the same time, and make sure every " +
                    "decision keeps all of them valid.",
                codeExample =
                    "<mspace=0.6em>while fuel > 0 and faresCollected < target:\n" +
                    "    if passengers < capacity:\n" +
                    "        pickUp()\n" +
                    "    moveForward()</mspace>"
            }
        };
    }
}
