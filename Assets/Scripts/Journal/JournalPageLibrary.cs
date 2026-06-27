using System.Collections.Generic;

/// <summary>
/// Static library of all journal/almanac pages. One entry per level (0–5).
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
                    "Before we left Iloilo, your lolo made me promise I'd teach you the route the way he taught me: " +
                    "one stop at a time, no shortcuts. This city is a goldmine of history if you know where to look — " +
                    "Calle Real's American-era facades, the dinagyang drums in January, and the food that earned us a UNESCO " +
                    "name for gastronomy. But the real inheritance starts with the women who held the line while the men " +
                    "were away. Listen to Ate Gemma. She knew your father better than most.",
                artifactCardDescription =
                    "A grease-stamped dispatcher's note tucked behind the sun visor — the first clue that the route was never just about driving.",
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
                    "Page 1 — *Ang Aking Mga Ugat*\n\n" +
                    "To whoever reads this first — start with your lola. Before me, before the jeepney, there was her, " +
                    "and the thread she never let break. Molo remembers her in its women's church, sixteen female saints " +
                    "standing like a council of quiet strength. Our family runs because a woman refused to let it stop. " +
                    "The plaza looked closer on the map, but the alleys turn back on themselves. You have to ask the road questions.",
                artifactCardDescription =
                    "A torn embroidery sampler from Molo — the motif matches the floral side panel Lola Caring once stitched for the jeepney.",
                codingConceptName = "Conditionals (if / while)",
                codingReferenceBody =
                    "Conditionals let the jeepney make decisions. Use <b>if</b> to do something " +
                    "once when a condition is true, and <b>while</b> to keep doing it as long as " +
                    "the condition stays true.",
                codeExample =
                    "<mspace=0.6em>while not routeComplete():\n" +
                    "    driveToNextStop()\n" +
                    "    if passengerWaiting():\n" +
                    "        pickUp()\n" +
                    "        collectFare()\n" +
                    "        giveChange(changeOwed())\n" +
                    "    if atRequestedStop():\n" +
                    "        dropOff()</mspace>"
            },

            new JournalPageDefinition
            {
                pageId = 2,
                heritageTitle = "Oton Market Run",
                heritageBody =
                    "Page 2\n\n" +
                    "The frame outlasts the smith. Your great-grandfather knew it when he beat the first one straight. " +
                    "I knew it every time I turned the key. Now you know it too. Oton was a port before it was anything else — " +
                    "Chinese ceramics, iron, and gold moving through the Batiano River. The gold death mask they dug at San Antonio " +
                    "was not just treasure; it was a belief made bright. Metal, like family, only stays true if someone keeps hammering it.",
                artifactCardDescription =
                    "A small brass anchor charm from the Oton shipyards — the same shape Lolo Nicro still carries in his pocket.",
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
                    "Page 3\n\n" +
                    "The pattern outlives the hands. He set something in motion he never got to see finished — and it held. " +
                    "Meaningful work doesn't ask to outlive you. It just does. Tigbauan taught me that. The looms there speak " +
                    "in loops and rules: do this, then this, then repeat. But the town has a harder thread too — in '42, on the road " +
                    "to Antique, ordinary folk stopped a convoy. Some of them did not live to see the country free. What they did outlived them.",
                artifactCardDescription =
                    "A shuttle from a Tigbauan handloom, wrapped in a strip of patadyong cloth the color of old coral stone.",
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
                    "Page 4\n\n" +
                    "They could have carved someone else's saint and been forgotten. Instead they carved themselves. " +
                    "Remember that when the world tells you who you're supposed to be. We were always here. So are you. " +
                    "Miag-ao's church was built as a fort against raiders, but its real defense is the facade — St. Christopher " +
                    "in our clothes, barefoot, carrying the Child across a river of coconut and guava. The carvers wrote themselves " +
                    "into stone so no one could say they weren't here. Your family is in that stone the same way.",
                artifactCardDescription =
                    "A rubbing of the Miag-ao facade's coconut-tree border, pressed into the journal with charcoal from a weaver's stove.",
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
                    "Page 5 — the father's letter\n\n" +
                    "If you're reading this, you drove the whole coast to reach me, and you finally know where you come from — " +
                    "the women who held us together, the smith who hammered our first frame, the hands that outlived their work, " +
                    "the family that carved itself into the stone. I scattered the pages because I was a coward with words while I was alive; " +
                    "I could only give you my history by making you go and earn it. That man in San Joaquin carved a war to say " +
                    "'I love you, father.' I had to scatter a journal across five towns to say it to my son. I'm sorry it took this long. " +
                    "Welcome home, anak. — Tatay",
                artifactCardDescription =
                    "The last page, pressed flat at the top of the Campo Santo — the ink still smells of salt and stone.",
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
