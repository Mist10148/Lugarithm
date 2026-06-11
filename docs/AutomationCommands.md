# Automation Mode — Command Reference

> The jeepney does exactly what you tell it — nothing more. Write a program,
> press **▶ RUN**, and watch it play out one step at a time on the tilemap.

In **Automation Mode** you drive the jeepney by *programming* it instead of
steering. You build a program in one of two interfaces (switch any time with the
**BLOCKS / CODE** tabs, or set your default in **Settings → Automation
Interface**):

- **Block Interface** — drag command blocks from the palette and snap them into
  place. No syntax to get wrong. *(Easy / default.)*
- **Code Editor** — write the same logic as short, Python-style text. Earns a
  **×1.5** score multiplier. *(Hard.)*

Both interfaces compile to the same program and run on the same jeepney, so a
solution that works in one works in the other.

---

## How a program runs

- The jeepney executes **one action per tick**. Use the speed buttons
  (**1× / 2× / 5×**) to slow it down or speed it up, **❚❚** to pause, and
  **↺ Reset** to put the world back.
- You can't "crash" or lose. A wrong move just bumps and warns in the console;
  fix the program and run again.
- A program that never reaches the goal simply stops — the console tells you
  what's still missing.

---

## Actions

Actions *do* something and take one tick each. Every action is written with
empty parentheses.

| Command | What it does |
|---|---|
| `moveForward()` | Move one tile in the direction you're facing. If a wall is ahead, the jeepney bumps and stays put. |
| `turnLeft()` | Rotate 90° counter-clockwise (facing only — you don't move). |
| `turnRight()` | Rotate 90° clockwise. |
| `pickUp()` | Board the waiting passenger on the current stop tile. |
| `dropOff()` | Let passengers off at the destination tile. |
| `collectFare()` | Collect the fare from everyone currently aboard. |

## Queries (conditions)

Queries *ask* a yes/no question about the world. They don't take a tick — use
them inside `if` and `while` to decide what to do.

| Query | True when… |
|---|---|
| `frontIsClear()` | The tile directly ahead is open (not a wall). |
| `leftIsClear()` | The tile to your left is open. |
| `rightIsClear()` | The tile to your right is open. |
| `atStop()` | You're standing on a passenger stop. |
| `atDestination()` | You're standing on the goal tile. |

> Each level unlocks only the commands it needs — the palette and the editor's
> autocomplete show what's available for the puzzle you're on.

## Control flow

| Form | Meaning |
|---|---|
| `if <condition>:` | Run the indented block once, only if the condition is true. |
| `if <condition>:` … `else:` | Run the first block if true, otherwise the `else` block. |
| `while <condition>:` | Repeat the indented block as long as the condition stays true. |
| `not <condition>` | Flip a condition — true becomes false. e.g. `while not atDestination():` |

**Syntax rules (Code Editor):**

- End every `if` / `else` / `while` line with a colon `:`.
- Indent the body **4 spaces** (or one Tab). Lines at the same indent belong to
  the same block.
- Comments start with `#` and run to the end of the line.

In the **Block Interface** there's no syntax at all: `if` / `while` blocks are
C-shaped — drop other blocks *inside* them. Click the condition chip on a
control block to cycle through the allowed conditions, and toggle **not** to
invert it.

---

## Examples

**Sequencing (Tutorial)** — drive forward, turn, and continue:

```
moveForward()
moveForward()
turnRight()
moveForward()
```

**Wall-following maze (Level 1 — Molo)** — keep a wall on your right until you
arrive:

```
while not atDestination():
    if rightIsClear():
        turnRight()
        moveForward()
    else:
        if frontIsClear():
            moveForward()
        else:
            turnLeft()
```

**Pick up, charge the fare, deliver:**

```
while not atDestination():
    if atStop():
        pickUp()
        collectFare()
    moveForward()
dropOff()
```

---

*The full list of actions and queries lives in code at
`Assets/Scripts/Automation/Lang/AgentApi.cs` — the parser, the block palette,
and the interpreter all validate against it, so the reference above and the game
can never drift apart.*
