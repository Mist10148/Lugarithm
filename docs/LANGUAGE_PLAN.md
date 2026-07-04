# Lugarithm Language Plan

> Status: **design / planning only — no implementation yet.**
> Scope decision: full Python basics **minus OOP**, expressed in **both** play modes.

The language that powers both **Automation Mode** (write an algorithm that drives the
jeepney for you) and the **puzzle mini-games** (e.g. escape a maze). It is a
Python *subset*: same syntax, same compiler, same interpreter — the only thing
that changes between the two play modes is the **domain vocabulary** exposed.

---

## 1. North star

One language. One AST. One interpreter. Two equally-powerful front ends:

- **Option A — type it:** an authentic Python-subset text editor (*The Farmer Was
  Replaced* style).
- **Option B — build it:** a Scratch-style drag-drop canvas that can express
  **everything** Option A can (full 1:1 parity — locked decision).

```
Text editor ─┐
             ├─► tokens ─► AST ─► tree-walking interpreter ─► AgentSim
Block canvas ┘            (shared)        (shared)
```

The current pipeline already proves the shared-AST approach
(`Assets/Scripts/Automation/Lang/` + `Assets/Scripts/Automation/Blocks/`).
`AgentApi` is the single registry both editors validate against, so they can
never drift apart.

### Locked decisions
- **Block ↔ text parity:** full 1:1.
- **Programming concepts:** all unlocked at once (no per-concept gating).
- **Data structures:** lists **+ dict + tuple**.
- **Core actions take arguments:** **yes** (e.g. `moveForward(3)`, `wait(2)`).
- **Domain vocabulary** (actions/queries/reporters) **is scoped per mode & per
  level** — a maze has no passengers, so it simply doesn't expose them. This is
  *not* a contradiction of "unlock all at once": *programming concepts* are
  always on; *domain API* is contextual.

---

## 2. Where we are today (the MVP surface)

| Aspect | Current state |
|---|---|
| Calls | **zero-argument only** — `moveForward()`, never `moveForward(3)` |
| Values | **none** — no variables, numbers, strings; conditions are bool-only |
| Actions | `moveForward turnLeft turnRight pickUp dropOff collectFare driveToNextStop driveToDestination` |
| Queries | `frontIsClear leftIsClear rightIsClear atStop atDestination hasPassengerAboard atRequestedStop` |
| Control flow | `if / else`, `while`, `not`, Python-style indentation |
| Interpreter | stepping VM, one action per `Step()`, frame stack, guard limits |

Honest framing: today this is a **command-and-control-flow language**, not "Python
with some basics missing." Everything in §3 is a net-new **value layer** the
interpreter does not have yet.

---

## 3. Shared core language (always on, both modes)

| Tier | Concepts |
|---|---|
| **T1 Values** | `int` · `float` · `string` · `bool` · `None` · variables/assignment · `print()` · `#` comments |
| **T2 Expressions** | `+ - * / // % **` · `== != < > <= >=` · `and or not` · `in / not in` · grouping/precedence · *(opt.)* tuple unpacking `x, y = ...` |
| **T3 Control flow** | `if/elif/else` · `while` · `for i in range()` · `for x in coll` · `break` · `continue` · friendly `repeat(N)` sugar |
| **T4 Functions** | `def` · params · `return` · local/global scope · reporters (functions usable as values) |
| **T5 Data** | **list** (literal/index/slice/`append`/`pop`/`len`/iterate) · **dict** (literal/lookup/assign/keys-values/iterate) · **tuple** (literal/coords/unpack); collection **mutation** + indexed assignment `a[i]=v`, `d[k]=v` |
| **T6 Built-ins** | `print len range int str float min max sum sorted` · `random.randint` (seeded for determinism) · **no `input()`** (agent runs autonomously) |

Out of scope (for now): classes/OOP, lambdas/`key=`, exceptions/`try`, generators,
comprehensions, imports beyond a curated `random`.

---

## 4. Domain API — scoped per mode/level (LOCKED tables)

Three categories. `Actions` cause side effects and may **return a value**;
`Queries` return bool; `Reporters` return a value (int/string/tuple). Reporters
depend on the T1 value system — they are what make fare/passenger logic
expressible at all. Gated via `LevelDefinition.allowedBlocks` / `allowedQueries`
(+ a new `allowedReporters`).

### 4.1 Core locomotion — both modes
| Kind | Name | Returns | Purpose |
|---|---|---|---|
| Action | `moveForward(n=1)` | — | advance `n` cells |
| Action | `turnLeft()` / `turnRight()` | — | rotate 90° |
| Action | `wait(n=1)` | — | idle `n` ticks |
| Query | `frontIsClear()` / `leftIsClear()` / `rightIsClear()` | bool | wall/obstacle sensor |
| Query | `atDestination()` | bool | reached the goal cell |

### 4.2 Maze / puzzle add-on
| Kind | Name | Returns | Purpose |
|---|---|---|---|
| Query | `atGoal()` | bool | on the exit tile |
| Action | `markCell()` / `unmark()` | — | breadcrumb (optional levels) |
| Query | `isMarked()` | bool | breadcrumb here? |
| Reporter | `position()` | tuple `(x,y)` | current coordinate (advanced levels) |
| Reporter | `facing()` | int `0..3` | current heading: 0=N, 1=E, 2=S, 3=W |

### 4.3 Automation-exclusive (jeepney driving)
| Kind | Name | Returns | Purpose |
|---|---|---|---|
| Action | `driveToNextStop()` | — | plan + drive to the next stop |
| Action | `driveToDestination()` | — | plan + drive to the route end |
| Action | `openDoor()` / `closeDoor()` | — | door control |
| Action | `board()` *(aka `pickUp`)* | — | board one waiting passenger |
| Action | `alight()` *(aka `dropOff`)* | — | let off passengers at their stop |
| Action | `collectFare()` | **int** | collect & return the peso amount |
| Action | `announceStop()` | — | the "Para!" call-out |
| Action | `honk()` | — | flavor / signal |
| Query | `atStop()` / `atRequestedStop()` | bool | at a stop / a requested (dulog) stop |
| Query | `hasPassengerAboard()` | bool | anyone on board |
| Query | `passengerWaiting()` | bool | someone waiting to board here |
| Query | `isFull()` | bool | no seats left |
| Reporter | `seatsLeft()` | int | remaining capacity |
| Reporter | `passengerCount()` | int | passengers aboard |
| Reporter | `passengerType()` | string | `"regular"`/`"student"`/`"senior"` |
| Reporter | `fareOwed()` | int | what the current passenger owes |
| Reporter | `distanceTraveled()` | int | cells/km since start |
| Reporter | `distanceToDestination()` | int | remaining distance |
| Reporter | `currentStop()` / `nextStop()` | string | stop names |
| Query | `storyDropoffArmed()` | bool | story passenger's drop-off is armed and undelivered |
| Reporter | `storyDropoffPosition()` | tuple `(x,y)` or `None` | story drop-off cell if armed |
| Reporter | `nearestStopPosition()` | tuple `(x,y)` or `None` | nearest useful pickup/drop-off cell |
| Reporter | `destinationPosition()` | tuple `(x,y)` | route terminal/destination cell |
| Reporter | `directionTo(x, y)` | int `0..3` or `None` | first-step heading along shortest path |
| Reporter | `distanceTo(x, y)` | int | shortest walkable path length to `(x, y)` |

---

## 5. Sufficiency proof (the language is "more than enough")

### Maze — wall-follower (left-hand rule) — T0+T3 only
```python
while not atDestination():
    if leftIsClear():
        turnLeft()
        moveForward()
    elif frontIsClear():
        moveForward()
    else:
        turnRight()
```

### Maze — BFS — exercises lists/dict/tuple/functions
```python
queue   = [start]
visited = {}                       # dict-as-set
while len(queue) > 0:
    cell = queue.pop(0)
    if cell == goal:
        return reconstruct(cell)
    for n in neighbors(cell):
        if n not in visited:       # membership
            visited[n] = cell      # indexed assignment
            queue.append(n)        # list mutation
```

### Automation — route with fares — value-returning queries/actions
```python
total = 0
while not atDestination():
    driveToNextStop()
    if atRequestedStop():
        openDoor()
        while seatsLeft() > 0 and passengerWaiting():
            board()
        total = total + collectFare()
announceStop()
print(total)
```

### Automation — fare calculation function — def/return/dict/built-ins
```python
fare_table = {"regular": 13, "student": 11, "senior": 11}

def compute_fare(distance, rider):
    base  = fare_table[rider]
    extra = max(0, distance - 4) * 2   # +₱2 per km past the first 4
    return base + extra
```

---

## 6. Block-mode parity requirements

To express everything as blocks (full parity), the canvas needs Scratch's shape
vocabulary:

- **Stack blocks** — statements (have these): actions, assignment, `print`, loops, `if`.
- **Reporter ovals** — produce a value: variables, literals, arithmetic, `len()`,
  list index, **domain reporters** (`seatsLeft()`), function calls that return.
- **Boolean hexagons** — produce true/false: comparisons, `and/or/not`, `in`, sensor queries.
- **Input slots** — number fields, text fields, **dropdowns** (variable/list pickers).
- **C-blocks** — wrap a body: `if`, `while`, `for`, `repeat` (basic version exists).
- **Custom blocks** — the block form of `def`; input slots = parameters; reporter
  custom blocks = functions with `return`.

All compile to the same AST via `BlockProgram.ToAst`.

---

## 7. Engine build order (dependency order; player still sees everything)

1. **Value system** + variables + `print` + literals + comments **+ value-returning
   reporters/actions** + **action arguments**. *(Keystone — unlocks fare/passenger
   logic and `moveForward(n)`.)*
2. **Expression parser** (precedence-climbing / Pratt) incl. `in`.
3. `elif` · `for`/`range` · `for-each` · `break`/`continue` · `repeat(N)`.
4. **Functions** (`def`/`return`/params/scope) — biggest interpreter change; extends
   the existing frame stack.
5. **Lists → dict → tuple**, with mutation, indexed assignment, `None`.
6. **Block-mode parity**, riding behind each engine tier so blocks never promise
   what the interpreter can't yet run.

### Interpreter impact summary
| Layer | Today | Becomes |
|---|---|---|
| Lexer | keywords + `():` + indent | + literals, operators, `, [] {}`, `= == elif for in def return break continue and or True False None` |
| AST | Call/While/If + Query/Not | + Literal, Var, Assign, BinOp, UnaryOp, Call(args), For, FuncDef, Return, Break/Continue, List/Dict/Tuple, Index |
| Parser | statements + `not query()` | full expression-precedence parser |
| Interpreter | action emitter + bool eval | environment (scopes) + `Value` type + expression eval + call stack + return/break/continue — **keeps one-action-per-`Step()`** |
| Blocks | flat blocks + 1 container | nested slots, reporter/boolean shapes, custom blocks |

---

## 8. Scoring & constraints (LOCKED)

One shared **Run Metrics** struct produced by every run; each level declares an
**objective** + **star/score rubric** referencing those metrics (data-driven, like
the rest of `LevelDefinition`).

Run Metrics (collected for every run): `ticks`, `actions`, `codeSize`
(lines/blocks), `faresCollected`, `passengersServed`, `passengersMissed`,
`fuelUsed`, `distance`, `penalties`.

### Puzzle / maze — "code golf"
Pass = reach the goal. Then up to 3 stars:
- ⭐ completed
- ⭐ under the **action/tick par** (efficiency)
- ⭐ under the **code-size par** (rewards loops & functions over copy-paste)

No fuel — mazes stay pure logic.

### Automation — "run a profitable shift" (optimization, not pass/fail)
`score = faresCollected − fuelCost − penalties`. Pressure comes from constraints:
- **Fuel** — each move/drive burns it; empty ⇒ shift ends.
- **Shift length** — finite ticks; passengers spawn over time.
- **Passenger patience** — a waiting rider leaves after K ticks ⇒ missed-fare penalty.
- **Fare accuracy** — wrong fare for the rider type ⇒ penalty.

This is what makes good fare logic + responsive routing actually pay off.

---

## 9. Runtime error coaching (LOCKED)

Every runtime failure is **caught** and rendered in the parser's existing calm,
second-person tone — offending line (text) or block (`SourceRef`) highlighted,
**never a stack trace**. Extend `StepResult.RuntimeError` (`LangError`) with an
optional `hint`. Errors return control to the editor; they don't crash the run.

| Failure | Message |
|---|---|
| Undefined var | `you're using 'total' before giving it a value — add 'total = 0' first.` |
| Index out of range | `the list has 3 items (0–2), but you asked for item 5.` |
| Missing dict key | `there's no "tourist" in fare_table. Known riders: regular, student, senior.` |
| Divide by zero | `can't divide by zero — check the value first.` |
| Type mismatch | `can't add text and a number ("Para" + 1). Convert with str() first.` |
| Wrong arg count | `compute_fare() needs 2 inputs (distance, rider) but got 1.` |
| Value-less call in expr | `turnLeft() doesn't give back a value, so it can't be used here.` |

Plus the existing guard trips (infinite loop / too many actions).

---

## 10. Starter library & scaffolding (LOCKED)

Two distinct, per-level-configurable things:
- **Starter library** — a tiny "garage" of always-available helpers, built from
  primitives, whose source players can **read and override**: `turnAround()`,
  `goToWall()`. Removes busywork without trivializing primitives.
- **Starter code** — per-level pre-filled editor content (tutorial scaffolding).

**Withheld on purpose:** fare/passenger helpers (e.g. `compute_fare()`). Computing
fares and handling passengers *is* the automation gameplay — handing it over would
hollow out the mode. Controlled by `LevelDefinition.starterFunctions` /
`starterCode`.

---

## 11. Locked semantics

- **Execution accounting** — expressions/queries/reporters are **free** (instant);
  each **action** costs one tick / one `Step()`, including value-returning actions
  like `collectFare()`; `moveForward(n)` costs `n`. Preserves the current
  one-action-per-`Step()` contract and makes `ticks` a fair efficiency metric.
- **Scope** — functions may **read** globals (`fare_table`); assigning a name inside
  a function makes a **local**; **no `global` keyword**; **top-level `def` only**
  (no nested defs for now).
- **Determinism** — `random` is **seeded from the run seed** (reuses seeded procgen)
  ⇒ reproducible keystone tests; no wall-clock randomness.
- **Type strictness** — **friendly error, no silent coercion** (`"a" + 1` errors).
- **Truthiness** — Python-style (`if my_list:` is false when empty; `0`/`""`/`None`
  falsy).

---

## 12. Final decisions (LOCKED)

- **Language name:** **Para** — the call passengers shout to stop a jeepney; also
  reads like "parameter"/"for". Working name for docs/UI only; **cosmetic** — swap
  freely without touching code.
- **String formatting:** **`+` concatenation and `str()` in v1; f-strings deferred**
  (lexer cost not worth it yet).
- **Block ↔ text round-tripping:** **v1 = one-way blocks→text** ("View as code" via
  an extended `AstPrinter`). **text→blocks (auto-layout) deferred.**

The plan is now fully locked. Implementation guidance: see **Appendix A** (build-step
1 spec) and `docs/IMPLEMENTATION_PROMPT.md` (the full handoff prompt for an LLM).

---

## Appendix A — Build-Step 1 spec (the value system)

> The keystone step: turn today's command-only VM into one that has **values,
> variables, output, call arguments, and value-returning world reads**. No binary
> operators yet (those are step 2) — step-1 expressions are just *atoms* and calls.

### A.1 Scope
In: int/float/string/bool/`None` literals · variables + assignment · `print()` ·
call **arguments** (`moveForward(3)`) · **reporters** (pure value-returning world
reads) · value-returning **actions** (`collectFare()`). Out (later steps): operators,
`elif`/`for`, functions, collections.

### A.2 The `Value` type — new file `Lang/Value.cs`
A small tagged value, designed to grow into lists/dicts/tuples/functions later.
```
enum ValueKind { None, Int, Float, Bool, Str, List, Dict, Tuple, Func }  // step-1 uses first 5
readonly struct Value {
    ValueKind Kind; long I; double F; bool B; string S; object Obj;     // Obj reserved for collections
    static Value Int(long)/Float(double)/Bool(bool)/Str(string)/None;
    bool   IsTruthy();          // Python truthiness: 0/0.0/""/None/empty → false
    string TypeName;            // "number"/"text"/"true-or-false"/"nothing" (beginner words)
    string Display();           // print() rendering: 13, "Para", True, None
}
```

### A.3 Lexer additions (`Lexer.cs` + `Token.cs`)
New `TokenType`s: `Number`, `String`, `Comma`, `Assign`, `KeywordTrue`,
`KeywordFalse`, `KeywordNone`.
- **Number:** digits with at most one `.` → `Number` token (text = literal).
- **String:** `"..."` or `'...'`, escapes `\n \t \\ \" \'`; unterminated → friendly
  `LangError`.
- **Punctuation:** `,` → `Comma`, `=` → `Assign`.
- **Keywords:** `True`/`False`/`None` recognized in `KeywordType()`. `print` stays a
  plain identifier (resolved as a builtin in the interpreter).

### A.4 AST additions (`Ast.cs`)
- `LiteralExpr : ExprNode { Value Value; }`
- `VarExpr : ExprNode { string Name; }`
- `CallExpr : ExprNode { string Name; List<ExprNode> Args; }` — a call in *value*
  position (reporter / builtin / value-returning action).
- `CallStmt` gains `List<ExprNode> Args` (now `moveForward(3)`); keep `Name`.
- `AssignStmt : StmtNode { string Name; ExprNode Value; }`
- `print(...)` is represented as a `CallStmt { Name="print", Args=[...] }` (builtin
  dispatch in the interpreter — no special node, generalizes to `len` etc. later).
- Conditions keep `QueryExpr`/`NotExpr` unchanged in step 1 (operators arrive step 2).

### A.5 Parser additions (`Parser.cs`)
- **Statement dispatch:** `Identifier` followed by `Assign` ⇒ `AssignStmt`; otherwise
  a call statement.
- **Arg lists:** `'(' (expr (',' expr)*)? ')'`.
- **Atom (step-1 expression):** `Number | String | True | False | None | Identifier`
  where a trailing `(` makes it a `CallExpr`, else a `VarExpr`.
- **Validation against an extended `AgentApi`** (see A.7): unknown name → "did you
  mean…"; wrong **arg count** → `compute_fare() needs 2 inputs but got 1.`; query used
  as action / action used as value — existing checks, extended.

### A.6 Interpreter additions (`Interpreter.cs`) — the subtle part
- **Environment:** single global `Dictionary<string, Value>` (scopes come step 4).
- **Output sink:** `List<string> Output` (or `Action<string> OnPrint`) — `print`
  appends here; **instant, costs no tick**, handled inside the Step loop (not yielded).
- **`Evaluate(ExprNode) → Value`** for `LiteralExpr` (its constant), `VarExpr` (env
  lookup; undefined ⇒ runtime error), `CallExpr`:
  - **builtin** (`print`, later `len`/`range`…): computed inline.
  - **pure reporter** (`seatsLeft`, `passengerType`, …): read inline via the agent —
    **no tick**. Extend `IAgentApi` with `Value ReadReporter(string name, IReadOnlyList<Value> args)`.
- **Value-returning side-effecting actions** (`collectFare()`): these consume a tick
  and animate, so they can't be evaluated purely inline under the one-action-per-`Step`
  contract. **v1 rule:** such a call may appear only as a **bare statement** or the
  **entire RHS of an assignment** (not nested in a larger expression). Protocol:
  `Step()` yields the action plus an optional `BindResultTo` (the assignment's target
  var); the controller performs it on `AgentSim`, gets a `Value`, and calls
  `interpreter.DeliverActionResult(value)` before the next `Step()`, which binds it.
  - Teaching-friendly rewrite of the §5 example: `fare = collectFare()` then
    `total = total + fare` (the `+` itself is step 2).
- **`moveForward(n)` semantics:** the interpreter emits the base `moveForward` action
  **`n` times** (n `Step`s ⇒ n animated cells ⇒ n ticks), preserving "one cell per
  visual step." Same pattern for any future repeat-style action arg.

### A.7 `AgentApi` extension
Add a **`Reporters`** category and **arity/return metadata** per entry (replace the
bare `string[]` with descriptors carrying `name`, `kind` ∈ {Action, Query, Reporter},
`arity`, `returnsValue`). Parser + interpreter validate against it. `LevelDefinition`
gains `allowedReporters` to mirror `allowedBlocks`/`allowedQueries`.

### A.8 Other touch points
- **`AstPrinter`** must render the new nodes (assignment, args, literals, calls) so
  blocks→text still round-trips.
- **Blocks** (`BlockModel`/`BlockProgram`) gain literal/variable/assignment/print
  blocks + input slots — but block work rides *behind* this step (per §7).

### A.9 Acceptance tests (NUnit, match `InterpreterTests`/`LexerTests` style)
- Lexer tokenizes int, float, string (incl. escapes), `True/False/None`, `,`, `=`;
  unterminated string ⇒ error.
- Parser: `x = 5`, `name = "Para"`, `moveForward(3)`, `print(seatsLeft())`,
  `fare = collectFare()` parse clean; `moveForward(` ⇒ friendly error.
- Interpreter: assign + read a variable; `print` writes expected `Output`;
  `moveForward(3)` emits exactly 3 `moveForward` actions; a pure reporter returns the
  scripted value into a variable; a value-returning action binds via the deliver
  protocol; **undefined variable ⇒ runtime error** with the §9 wording.
- **Regression:** all existing 145 tests stay green.
