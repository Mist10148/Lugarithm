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
                    "A <b>command</b> is an instruction that tells the jeepney to <i>do</i> one thing. " +
                    "You write it as a name followed by a pair of parentheses <b>()</b> — the " +
                    "parentheses are how the computer knows you mean \"do this action now.\"\n\n" +
                    "Driving commands: <b>moveForward()</b> drives one tile ahead, <b>turnLeft()</b> " +
                    "and <b>turnRight()</b> rotate the jeepney without moving. Route helpers " +
                    "<b>driveToNextStop()</b> and <b>driveToTerminal()</b> plan smooth road paths for you. " +
                    "Passenger commands: <b>pickUp()</b>, <b>dropOff()</b>, <b>collectFare()</b>, and " +
                    "<b>giveChange(amount)</b>. To pause for a beat, " +
                    "use <b>wait()</b>.\n\n" +
                    "<b>Watch out:</b> the jeepney only turns — it never moves sideways. After a turn " +
                    "you still need a <b>moveForward()</b> to actually go that way. Forgetting it is " +
                    "the most common beginner slip.",
                codeExample =
                    "<mspace=0.6em># Pull up to a waiting rider, then carry them one tile on:\n" +
                    "moveForward()\n" +
                    "pickUp()        # rider boards\n" +
                    "collectFare()   # they hand over cash\n" +
                    "giveChange(changeOwed())\n" +
                    "moveForward()\n" +
                    "dropOff()       # rider gets off\n\n" +
                    "# Turning needs a move afterwards to go that way:\n" +
                    "turnLeft()\n" +
                    "moveForward()</mspace>"
            },

            new CodingConceptEntry
            {
                title = "Sensing (Queries)",
                body =
                    "A <b>query</b> lets the jeepney <i>look around</i> and report back either " +
                    "<b>True</b> or <b>False</b>. Think of them as yes/no questions about the world: " +
                    "<b>frontIsClear()</b> asks \"is the tile ahead open?\", <b>atStop()</b> asks " +
                    "\"am I on a passenger stop?\", and <b>routeComplete()</b> asks whether every required " +
                    "rider is delivered and the jeepney is at the current terminal. <b>atDestination()</b> " +
                    "still exists for maze-style puzzles.\n\n" +
                    "Other handy queries: <b>leftIsClear()</b>, <b>rightIsClear()</b>, " +
                    "<b>passengerWaiting()</b>, <b>isFull()</b>, and <b>hasPassengerAboard()</b>.\n\n" +
                    "<b>Watch out:</b> a query only <i>answers</i> a question — it doesn't <i>do</i> " +
                    "anything on its own. It belongs inside an <b>if</b> or <b>while</b> condition. " +
                    "Writing <b>frontIsClear()</b> on its own line does nothing useful.",
                codeExample =
                    "<mspace=0.6em># Ask before you act:\n" +
                    "if frontIsClear():\n" +
                    "    moveForward()\n" +
                    "else:\n" +
                    "    turnRight()\n\n" +
                    "# Only pick up when someone is actually waiting:\n" +
                    "if atStop() and passengerWaiting():\n" +
                    "    pickUp()</mspace>"
            },

            new CodingConceptEntry
            {
                title = "Sequencing",
                body =
                    "<b>Sequencing</b> means the order of your lines matters. The jeepney reads your " +
                    "program from top to bottom and does exactly one instruction at a time, in the " +
                    "order you wrote them — like steps in a recipe.\n\n" +
                    "Swap two lines and you get a different result: picking a rider up <i>before</i> " +
                    "you reach their stop is not the same as doing it <i>after</i>.\n\n" +
                    "<b>Watch out:</b> the computer does precisely what you wrote, not what you meant. " +
                    "If the jeepney does things in a strange order, re-read your lines top to bottom " +
                    "and check the sequence first.",
                codeExample =
                    "<mspace=0.6em># Correct order: arrive, board, collect, give change, then drive on.\n" +
                    "moveForward()\n" +
                    "pickUp()\n" +
                    "collectFare()\n" +
                    "giveChange(changeOwed())\n" +
                    "moveForward()\n" +
                    "dropOff()\n\n" +
                    "# Wrong order: dropping off before anyone boarded does nothing.\n" +
                    "# dropOff()\n" +
                    "# pickUp()</mspace>"
            },

            new CodingConceptEntry
            {
                title = "Variables",
                body =
                    "A <b>variable</b> is a labelled box that remembers a value for you. You create one " +
                    "with an <b>=</b> sign: the name goes on the left, the value on the right. " +
                    "Afterwards, writing the name anywhere means \"use whatever is in that box.\"\n\n" +
                    "Variables can hold numbers, text in \"quotes\", or True/False. You can change a " +
                    "variable later by assigning to it again — the new value replaces the old one.\n\n" +
                    "<b>Watch out:</b> <b>=</b> means \"store this value\" (assignment), while <b>==</b> " +
                    "means \"are these equal?\" (a comparison). Mixing them up is a classic bug.",
                codeExample =
                    "<mspace=0.6em>fare = 12          # one box called fare, holding 12\n" +
                    "passengers = 3\n" +
                    "total = fare * passengers   # total now holds 36\n\n" +
                    "# Counting as you go — update the same box:\n" +
                    "onboard = 0\n" +
                    "pickUp()\n" +
                    "onboard = onboard + 1       # onboard is now 1</mspace>"
            },

            new CodingConceptEntry
            {
                title = "Operators & Logic",
                body =
                    "<b>Operators</b> are symbols that combine values. Math operators: <b>+</b> add, " +
                    "<b>-</b> subtract, <b>*</b> multiply, <b>/</b> divide, <b>%</b> remainder.\n\n" +
                    "<b>Comparison</b> operators ask a true/false question about two values: " +
                    "<b>==</b> equal, <b>!=</b> not equal, <b>&lt;</b> less than, <b>&gt;</b> greater " +
                    "than, <b>&lt;=</b> and <b>&gt;=</b>.\n\n" +
                    "<b>Logic</b> operators glue conditions together: <b>and</b> is true only when both " +
                    "sides are true, <b>or</b> is true when at least one side is, and <b>not</b> flips " +
                    "true to false.\n\n" +
                    "<b>Watch out:</b> <b>and</b> is stricter than <b>or</b>. \"Seats left AND someone " +
                    "waiting\" only acts when both are true.",
                codeExample =
                    "<mspace=0.6em># Combine two questions with 'and':\n" +
                    "if seatsLeft() > 0 and passengerWaiting():\n" +
                    "    pickUp()\n\n" +
                    "# 'not' flips a query — act when the front is blocked:\n" +
                    "if not frontIsClear():\n" +
                    "    turnLeft()\n\n" +
                    "# 'or' acts when either side opens up:\n" +
                    "if leftIsClear() or rightIsClear():\n" +
                    "    moveForward()</mspace>"
            },

            new CodingConceptEntry
            {
                title = "Conditionals (if / else)",
                body =
                    "A <b>conditional</b> lets the jeepney make a decision. <b>if</b> runs the indented " +
                    "lines underneath it <i>only</i> when its condition is true. Add <b>else</b> to give " +
                    "a different set of lines to run when the condition is false. <b>elif</b> (\"else " +
                    "if\") checks another condition in between.\n\n" +
                    "The indentation (the spaces at the start of a line) is what tells the computer " +
                    "which lines belong inside the <b>if</b>.\n\n" +
                    "<b>Watch out:</b> don't forget the colon <b>:</b> at the end of the if-line, and " +
                    "keep the lines inside it indented the same amount.",
                codeExample =
                    "<mspace=0.6em># One decision with a fallback:\n" +
                    "if atStop():\n" +
                    "    pickUp()\n" +
                    "else:\n" +
                    "    moveForward()\n\n" +
                    "# Three-way choice with elif:\n" +
                    "if frontIsClear():\n" +
                    "    moveForward()\n" +
                    "elif leftIsClear():\n" +
                    "    turnLeft()\n" +
                    "else:\n" +
                    "    turnRight()</mspace>"
            },

            new CodingConceptEntry
            {
                title = "Loops (while / for / repeat)",
                body =
                    "A <b>loop</b> repeats lines so you don't copy-paste the same command over and over.\n\n" +
                    "<b>while</b> keeps repeating as long as its condition stays true — perfect when you " +
                    "don't know the exact number of steps (\"keep driving until I arrive\"). " +
                    "<b>for i in range(n)</b> and <b>repeat(n)</b> run a fixed number of times when you " +
                    "<i>do</i> know the count.\n\n" +
                    "<b>Watch out:</b> a <b>while</b> loop needs something inside it that eventually makes " +
                    "the condition false, or it runs forever (an \"infinite loop\"). Make sure the " +
                    "jeepney keeps moving toward the goal each time around.",
                codeExample =
                    "<mspace=0.6em># Repeat until you arrive — the classic driving loop:\n" +
                    "while not routeComplete():\n" +
                    "    driveToNextStop()\n" +
                    "    if passengerWaiting():\n" +
                    "        pickUp()\n" +
                    "        collectFare()\n" +
                    "        giveChange(changeOwed())\n" +
                    "    if atRequestedStop():\n" +
                    "        dropOff()\n\n" +
                    "# Do something an exact number of times:\n" +
                    "for i in range(3):\n" +
                    "    moveForward()</mspace>"
            },

            new CodingConceptEntry
            {
                title = "Lists & Indexing",
                body =
                    "A <b>list</b> stores several values in order inside one variable, written between " +
                    "square brackets and separated by commas. Each slot has a number called its " +
                    "<b>index</b>, and counting starts at <b>0</b> — so the first item is " +
                    "<b>list[0]</b>, the second is <b>list[1]</b>, and so on.\n\n" +
                    "You can read an item, change one, or check how many there are with <b>len()</b>.\n\n" +
                    "<b>Watch out:</b> because indexing starts at 0, the last item of a 3-item list is " +
                    "at index 2, not 3. Asking for an index that doesn't exist is an error.",
                codeExample =
                    "<mspace=0.6em>stops = [\"Garage\", \"Market\", \"Terminal\"]\n" +
                    "first = stops[0]    # \"Garage\"\n" +
                    "next_stop = stops[1]   # \"Market\"\n" +
                    "count = len(stops)  # 3\n\n" +
                    "# Visit every stop in order:\n" +
                    "for i in range(len(stops)):\n" +
                    "    announceStop()\n" +
                    "    moveForward()</mspace>"
            },

            new CodingConceptEntry
            {
                title = "Functions & Parameters",
                body =
                    "A <b>function</b> is a named block of steps you write once and reuse whenever you " +
                    "like. You create it with <b>def</b>, then \"call\" it later by writing its name " +
                    "with parentheses. This keeps long programs short and readable.\n\n" +
                    "A <b>parameter</b> is an input slot in the parentheses. Passing a different value " +
                    "(an \"argument\") each time lets the same function do slightly different work. " +
                    "<b>return</b> hands a result back to whoever called the function.\n\n" +
                    "<b>Watch out:</b> you have to <i>call</i> a function for it to run — defining it " +
                    "alone does nothing. And only code below the <b>return</b>'s caller sees that value.",
                codeExample =
                    "<mspace=0.6em># Define once, with a parameter:\n" +
                    "def fareFor(passengerCount):\n" +
                    "    return passengerCount * 12\n\n" +
                    "total = fareFor(3)   # 36\n\n" +
                    "# A reusable move: turn around in place.\n" +
                    "def turnAround():\n" +
                    "    turnLeft()\n" +
                    "    turnLeft()\n\n" +
                    "turnAround()         # call it whenever you need it</mspace>"
            },

            new CodingConceptEntry
            {
                title = "Nested Conditionals",
                body =
                    "\"<b>Nested</b>\" means one thing placed inside another. A nested conditional is an " +
                    "<b>if</b> written <i>inside</i> another <b>if</b> or <b>else</b>. The outer check " +
                    "runs first; only if it passes does the computer reach the inner check.\n\n" +
                    "This lets you handle combinations — first decide \"am I at a stop?\", and only then " +
                    "decide \"is the jeepney full?\" Each level of nesting is indented one step further " +
                    "so you can see what belongs to what.\n\n" +
                    "<b>Watch out:</b> deep nesting gets hard to read. Often you can replace it with " +
                    "<b>and</b> (e.g. <b>if atStop() and not isFull():</b>) for the same result.",
                codeExample =
                    "<mspace=0.6em># Outer decision, then an inner one:\n" +
                    "if atStop():\n" +
                    "    if isFull():\n" +
                    "        dropOff()\n" +
                    "    else:\n" +
                    "        pickUp()\n\n" +
                    "# The same idea, flattened with 'and':\n" +
                    "if atStop() and not isFull():\n" +
                    "    pickUp()</mspace>"
            },
        };
    }
}
