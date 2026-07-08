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
                    "and <b>turnRight()</b> rotate the jeepney to take the side of the road without " +
                    "moving. To ride the lanes, <b>moveLeft()</b> and <b>moveRight()</b> slide the " +
                    "jeepney one lane sideways while it keeps facing ahead — like changing lanes in " +
                    "Manual mode. Route helpers " +
                    "<b>driveToNextStop()</b>, <b>driveToDropoff()</b> and <b>keepDriving()</b> plan smooth " +
                    "road paths for you — see <b>Endless Driving</b> for the never-ending road. " +
                    "Passenger commands: <b>pickUp()</b>, <b>dropOff()</b>, <b>collectFare()</b>, and " +
                    "<b>giveChange(amount)</b>. To pause for a beat, " +
                    "use <b>wait()</b>.\n\n" +
                    "<b>Watch out:</b> <b>turnLeft()</b>/<b>turnRight()</b> rotate but don't move — after " +
                    "a turn you still need a <b>moveForward()</b> to actually go that way. To shift lanes " +
                    "without turning, use <b>moveLeft()</b>/<b>moveRight()</b> instead.",
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
                    "moveForward()\n\n" +
                    "# Change lane without turning — slide over, then carry on:\n" +
                    "moveLeft()\n" +
                    "moveForward()</mspace>"
            },

            new CodingConceptEntry
            {
                title = "Traffic & Lane Changes",
                body =
                    "The road is shared and traffic keeps to its side: cars going your way stay on the " +
                    "right, oncoming cars pass on the left. <b>carInFront()</b> is a query that asks " +
                    "whether a moving car is directly ahead of the jeepney. When it is true, dodge with " +
                    "<b>moveLeft()</b> or <b>moveRight()</b>; those commands slide one lane sideways " +
                    "without turning the jeepney.\n\n" +
                    "<b>avoidTraffic()</b> is a built-in command that does this whole habit in one call: " +
                    "if a car blocks the cell ahead it slides into a clear lane (left first, then right), " +
                    "and simply waits when boxed in. You can still write your own <b>def avoidTraffic():</b> " +
                    "— a function you define wins over the built-in, so the example below shows what the " +
                    "built-in does under the hood.\n\n" +
                    "<b>Watch out:</b> <b>carInFront()</b> only answers the question. Put it inside an " +
                    "<b>if</b> before you move.",
                codeExample =
                    "<mspace=0.6em># One call dodges the car ahead:\n" +
                    "avoidTraffic()\n" +
                    "driveToNextStop()\n\n" +
                    "# What the built-in does under the hood:\n" +
                    "def avoidTraffic():\n" +
                    "    if carInFront():\n" +
                    "        if leftIsClear():\n" +
                    "            moveLeft()\n" +
                    "        else:\n" +
                    "            moveRight()</mspace>"
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
                    "Other handy queries: <b>leftIsClear()</b>, <b>rightIsClear()</b>, <b>carInFront()</b>, " +
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
                    "    pickUp()\n\n" +
                    "# Dodge a car before driving on:\n" +
                    "if carInFront():\n" +
                    "    moveLeft()</mspace>"
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

            new CodingConceptEntry
            {
                title = "Comments & Readability",
                body =
                    "A <b>comment</b> is a note to yourself that the computer ignores. Anything after a " +
                    "<b>#</b> on a line is a comment. Use them to explain <i>why</i> a piece of code is " +
                    "there, label a section, or temporarily switch a line off without deleting it.\n\n" +
                    "Good names and short comments turn a wall of commands into something you can re-read " +
                    "next week and still understand.\n\n" +
                    "<b>Watch out:</b> a comment can't make wrong code right — it only explains. Keep " +
                    "comments honest; a comment that disagrees with the code is worse than none.",
                codeExample =
                    "<mspace=0.6em># Pick up the rider, then settle the fare:\n" +
                    "pickUp()\n" +
                    "collectFare()        # they pay\n" +
                    "giveChange(changeOwed())   # hand back the difference\n\n" +
                    "# Switch a line off by commenting it out:\n" +
                    "# honk()</mspace>"
            },

            new CodingConceptEntry
            {
                title = "Indentation & Whitespace",
                body =
                    "<b>Indentation</b> — the spaces at the start of a line — is how the language knows " +
                    "which lines belong <i>inside</i> an <b>if</b>, <b>while</b>, <b>for</b>, or <b>def</b>. " +
                    "Every line in the same block lines up at the same depth; going one step deeper means " +
                    "you're inside the block above.\n\n" +
                    "Pick one step size (4 spaces is standard) and keep it consistent. The colon <b>:</b> " +
                    "at the end of a header line always introduces an indented block beneath it.\n\n" +
                    "<b>Watch out:</b> mixing indent sizes (some lines 2 spaces, some 4) is the most common " +
                    "\"this line's indentation doesn't line up\" error. Blank lines and spacing around " +
                    "<b>=</b> are free — use them to group related steps.",
                codeExample =
                    "<mspace=0.6em>while not routeComplete():\n" +
                    "    driveToNextStop()      # inside the while\n" +
                    "    if passengerWaiting(): # inside the while\n" +
                    "        pickUp()           # inside the if\n" +
                    "moveForward()              # back outside the while</mspace>"
            },

            new CodingConceptEntry
            {
                title = "Booleans, True / False & None",
                body =
                    "A <b>boolean</b> is a value that is either <b>True</b> or <b>False</b> — the answer to a " +
                    "yes/no question. Every query (like <b>frontIsClear()</b>) hands back a boolean, and " +
                    "every condition in an <b>if</b>/<b>while</b> boils down to one.\n\n" +
                    "You can store a boolean in a variable and combine booleans with <b>and</b>, <b>or</b>, " +
                    "<b>not</b>. <b>None</b> is a special \"nothing here\" value — what a function gives back " +
                    "when it doesn't <b>return</b> anything.\n\n" +
                    "<b>Watch out:</b> <b>while True:</b> loops forever on purpose (see Endless Driving); make " +
                    "sure that's what you want. And a bare query line like <b>isFull()</b> just produces " +
                    "True/False and throws it away — put it in a condition or a variable.",
                codeExample =
                    "<mspace=0.6em>full = isFull()        # store a True/False\n" +
                    "if not full and passengerWaiting():\n" +
                    "    pickUp()\n\n" +
                    "# True is a literal boolean — this loop runs until you stop it:\n" +
                    "# while True:\n" +
                    "#     keepDriving()</mspace>"
            },

            new CodingConceptEntry
            {
                title = "Stopping a Loop (break / continue)",
                body =
                    "Inside a loop, <b>break</b> jumps out of it immediately, and <b>continue</b> skips the " +
                    "rest of this turn and goes straight to the next one. They give you finer control than " +
                    "the loop's condition alone.\n\n" +
                    "<b>break</b> is handy with <b>while True:</b> — loop forever, then leave the moment some " +
                    "situation comes up. <b>continue</b> is handy to bail out early on a tile where there's " +
                    "nothing to do.\n\n" +
                    "<b>Watch out:</b> <b>break</b> and <b>continue</b> only affect the <i>innermost</i> loop " +
                    "they sit in. Overusing them can make a loop hard to follow — often a clearer condition " +
                    "does the same job.",
                codeExample =
                    "<mspace=0.6em>while True:\n" +
                    "    keepDriving()\n" +
                    "    if routeComplete():\n" +
                    "        break              # delivered everyone — leave the loop\n" +
                    "    if not passengerWaiting():\n" +
                    "        continue           # nothing to board here; carry on\n" +
                    "    pickUp()</mspace>"
            },

            new CodingConceptEntry
            {
                title = "Endless Driving (the never-ending road)",
                body =
                    "Automation roads never end — the town keeps generating ahead of you. Three commands " +
                    "make driving them easy:\n\n" +
                    "<b>driveToNextStop()</b> rolls to the nearest stop that has someone waiting or aboard. " +
                    "<b>driveToDropoff()</b> heads straight for your <i>story</i> passenger's drop-off — " +
                    "delivering them is what <b>completes the leg</b>. <b>keepDriving()</b> cruises on along " +
                    "the endless road, serving anyone nearby, so the jeepney never has to stop.\n\n" +
                    "The leg finishes the moment your story rider is dropped off (<b>routeComplete()</b> turns " +
                    "True), but the road carries on — you can <b>keepDriving()</b> as long as you like to " +
                    "pick up more riders.\n\n" +
                    "<b>Watch out:</b> after a turn you still owe a move; the drive-helpers handle that for " +
                    "you, but hand-stepping with moveForward()/turnLeft() does not.",
                codeExample =
                    "<mspace=0.6em># Deliver the story rider, then the leg is done:\n" +
                    "while not routeComplete():\n" +
                    "    driveToNextStop()\n" +
                    "    if passengerWaiting():\n" +
                    "        pickUp()\n" +
                    "        collectFare()\n" +
                    "        giveChange(changeOwed())\n" +
                    "    if atRequestedStop():\n" +
                    "        dropOff()\n\n" +
                    "# Or cruise the endless road forever:\n" +
                    "while True:\n" +
                    "    keepDriving()</mspace>"
            },

            new CodingConceptEntry
            {
                title = "Full Command Catalog",
                body =
                    "Every name the jeepney understands, in one place. <b>Actions</b> <i>do</i> something " +
                    "(one tick each), <b>queries</b> answer True/False, and <b>reporters</b> hand back a " +
                    "number or word you can use in a calculation.\n\n" +
                    "<b>Actions:</b> moveForward(), turnLeft(), turnRight(), moveLeft(), moveRight(), " +
                    "avoidTraffic(), wait(), " +
                    "driveToNextStop(), driveToDropoff(), driveToTerminal(), driveToDestination(), " +
                    "keepDriving(), openDoor(), closeDoor(), pickUp() / board(), dropOff() / alight(), " +
                    "collectFare(), giveChange(amount), announceStop(), honk(). " +
                    "Maze drills add markCell(), unmark().\n\n" +
                    "<b>Queries (yes/no):</b> frontIsClear(), leftIsClear(), rightIsClear(), carInFront(), atStop(), " +
                    "atRequestedStop(), passengerWaiting(), hasPassengerAboard(), isFull(), " +
                    "routeComplete(), atDestination(). Maze drills add atGoal(), isMarked().\n\n" +
                    "<b>Reporters (a value):</b> seatsLeft(), passengerCount(), passengerType(), fareOwed(), " +
                    "cashTendered(), changeOwed(), distanceTraveled(), distanceToDestination(), " +
                    "currentStop(), nextStop(). Maze drills add position().\n\n" +
                    "<b>Watch out:</b> only some commands are unlocked per level — the palette and " +
                    "autocomplete show what's available right now.",
                codeExample =
                    "<mspace=0.6em># Reporters feed calculations and arguments:\n" +
                    "owed = changeOwed()\n" +
                    "giveChange(owed)\n\n" +
                    "# Queries belong in conditions:\n" +
                    "if seatsLeft() > 0 and passengerWaiting():\n" +
                    "    pickUp()</mspace>"
            },

            new CodingConceptEntry
            {
                title = "Common Errors & How to Read Them",
                body =
                    "When a program won't run, the message tells you the <b>line</b> and a plain-English " +
                    "reason — no jargon. Read it literally, then look at that line and the one above it.\n\n" +
                    "Frequent ones: <i>\"expected ':' at the end…\"</i> — an if/while/for/def header needs a " +
                    "colon. <i>\"this line's indentation doesn't line up…\"</i> — a block's lines aren't at " +
                    "the same depth (see Indentation). <i>\"…needs N inputs but got M\"</i> — a command got " +
                    "the wrong number of values in its (). <i>\"did you mean …?\"</i> — a name is misspelled.\n\n" +
                    "If it runs but doesn't win, the co-pilot describes the <b>gap</b> (e.g. a rider still " +
                    "needs dropping). Fix one thing, RUN again, repeat.\n\n" +
                    "<b>Watch out:</b> the computer does exactly what you wrote. If behaviour looks wrong, " +
                    "re-read top to bottom and check the <b>order</b> of your lines first.",
                codeExample =
                    "<mspace=0.6em># Missing colon  →  \"expected ':' at the end of the if-line\"\n" +
                    "# if atStop()\n" +
                    "#     pickUp()\n\n" +
                    "# Fixed:\n" +
                    "if atStop():\n" +
                    "    pickUp()</mspace>"
            },

            new CodingConceptEntry
            {
                title = "Autopilot (Testing)",
                body =
                    "Some screens have an <b>Autopilot</b> button. It drops a known-good solution into your " +
                    "editor (or snaps it together on the block canvas) and presses RUN for you, so you can " +
                    "watch a correct program drive the route or solve the maze end-to-end.\n\n" +
                    "It's a <i>testing and learning</i> aid — a worked example you can read, tweak, and learn " +
                    "from, not a substitute for writing your own. On the endless road the autopilot delivers " +
                    "the required rider to complete the leg.\n\n" +
                    "<b>Watch out:</b> autopilot overwrites what's currently in the editor/canvas. Copy " +
                    "anything you want to keep before pressing it.",
                codeExample =
                    "<mspace=0.6em># A maze solver autopilot loads the wall-follower:\n" +
                    "while not atDestination():\n" +
                    "    if rightIsClear():\n" +
                    "        turnRight()\n" +
                    "        moveForward()\n" +
                    "    elif frontIsClear():\n" +
                    "        moveForward()\n" +
                    "    else:\n" +
                    "        turnLeft()</mspace>"
            },
        };
    }
}
