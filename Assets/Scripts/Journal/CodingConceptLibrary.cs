using System.Collections.Generic;

/// <summary>One Coding Reference entry: a programming concept, its explanation, and a
/// short code example. Unlike <see cref="JournalPageDefinition"/> (one place per level),
/// concepts are not tied to towns — they're browsable reference material.</summary>
public class CodingConceptEntry
{
    public string title;
    public string body;
    public string codeExample;   // rich-text (TMP) snippet, monospaced
}

/// <summary>
/// The "Coding Reference" tab's categories: programming concepts (commands, sensing,
/// sequencing, variables, conditionals, loops, lists, functions, operators) rather than
/// the per-level place pages. Command/query names mirror <see cref="AgentApi"/> so the
/// reference can't drift from what the parser actually accepts.
/// </summary>
public static class CodingConceptLibrary
{
    public static readonly IReadOnlyList<CodingConceptEntry> Concepts = Build();

    static List<CodingConceptEntry> Build()
    {
        return new List<CodingConceptEntry>
        {
            new CodingConceptEntry
            {
                title = "In-game Commands",
                body =
                    "Commands (actions) tell the jeepney to <i>do</i> something. Each one is a name " +
                    "followed by parentheses. Drive and turn with <b>moveForward()</b>, " +
                    "<b>turnLeft()</b>, <b>turnRight()</b>; tend passengers with <b>pickUp()</b>, " +
                    "<b>dropOff()</b>, <b>collectFare()</b>; and pause with <b>wait()</b>.",
                codeExample =
                    "<mspace=0.6em>moveForward()\n" +
                    "turnLeft()\n" +
                    "moveForward()\n" +
                    "pickUp()\n" +
                    "collectFare()\n" +
                    "dropOff()</mspace>"
            },

            new CodingConceptEntry
            {
                title = "Sensing (Queries)",
                body =
                    "Queries let the jeepney <i>look around</i> and answer true or false. Use them " +
                    "inside <b>if</b> and <b>while</b> conditions — never on their own. The common " +
                    "ones are <b>frontIsClear()</b>, <b>leftIsClear()</b>, <b>rightIsClear()</b>, " +
                    "<b>atStop()</b>, and <b>atDestination()</b>.",
                codeExample =
                    "<mspace=0.6em>if frontIsClear():\n" +
                    "    moveForward()\n" +
                    "else:\n" +
                    "    turnRight()</mspace>"
            },

            new CodingConceptEntry
            {
                title = "Sequencing",
                body =
                    "A program is just a list of instructions the jeepney follows in order. " +
                    "Put one command after another, and it does them one at a time, top to bottom.",
                codeExample =
                    "<mspace=0.6em>moveForward()\n" +
                    "moveForward()\n" +
                    "pickUp()\n" +
                    "dropOff()</mspace>"
            },

            new CodingConceptEntry
            {
                title = "Variables",
                body =
                    "A variable is a name that remembers a value so you can use it later. Give it a " +
                    "value with <b>=</b>, then read or change it anywhere below.",
                codeExample =
                    "<mspace=0.6em>fare = 12\n" +
                    "passengers = 3\n" +
                    "total = fare * passengers   # 36</mspace>"
            },

            new CodingConceptEntry
            {
                title = "Operators & Logic",
                body =
                    "Operators combine values. Do math with <b>+ - * / %</b>, compare with " +
                    "<b>== != &lt; &gt; &lt;= &gt;=</b>, and join conditions with <b>and</b>, " +
                    "<b>or</b>, and <b>not</b>.",
                codeExample =
                    "<mspace=0.6em>if seatsLeft() > 0 and passengerWaiting():\n" +
                    "    pickUp()\n" +
                    "if not frontIsClear():\n" +
                    "    turnLeft()</mspace>"
            },

            new CodingConceptEntry
            {
                title = "Conditionals (if / else)",
                body =
                    "Conditionals let the jeepney make decisions. Use <b>if</b> to do something once " +
                    "when a condition is true, and <b>else</b> to do something different when it isn't.",
                codeExample =
                    "<mspace=0.6em>if atStop():\n" +
                    "    pickUp()\n" +
                    "else:\n" +
                    "    moveForward()</mspace>"
            },

            new CodingConceptEntry
            {
                title = "Loops (while / for / repeat)",
                body =
                    "Loops repeat work so you don't write the same line over and over. <b>while</b> " +
                    "keeps going as long as a condition stays true; <b>repeat(n)</b> and " +
                    "<b>for i in range(n)</b> run a fixed number of times.",
                codeExample =
                    "<mspace=0.6em>while not atDestination():\n" +
                    "    if frontIsClear():\n" +
                    "        moveForward()\n" +
                    "    else:\n" +
                    "        turnLeft()</mspace>"
            },

            new CodingConceptEntry
            {
                title = "Lists & Indexing",
                body =
                    "A list keeps items in order. Each item has an index starting from 0. Use the " +
                    "index in square brackets to read or change one specific item.",
                codeExample =
                    "<mspace=0.6em>stops = [\"Garage\", \"Market\", \"Terminal\"]\n" +
                    "next_stop = stops[1]   # Market</mspace>"
            },

            new CodingConceptEntry
            {
                title = "Functions & Parameters",
                body =
                    "A function is a named block of steps you can reuse. Parameters are inputs you " +
                    "pass in so the same function can do slightly different work each time.",
                codeExample =
                    "<mspace=0.6em>def fareFor(passengerCount):\n" +
                    "    return passengerCount * 12\n\n" +
                    "total = fareFor(3)   # 36</mspace>"
            },

            new CodingConceptEntry
            {
                title = "Nested Conditionals",
                body =
                    "You can put an <b>if</b> inside another <b>if</b> or <b>else</b> to handle " +
                    "combinations of conditions. Check the outer condition first, then decide the inner one.",
                codeExample =
                    "<mspace=0.6em>if atStop():\n" +
                    "    if isFull():\n" +
                    "        dropOff()\n" +
                    "    else:\n" +
                    "        pickUp()</mspace>"
            },
        };
    }
}
