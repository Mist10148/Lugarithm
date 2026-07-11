# Lugarithm

### *A Heritage Jeepney Road-Trip that Teaches You to Code*

> *"Drive the coast. Recover the pages. Learn the history."*

**Game Design Document** — AI Game On! IV • AI Fest 2026
Team **Cyfer** • Region VI – Western Visayas
*Theme: "Giving Our History a New Heartbeat through the Intelligence of Tomorrow."*

> **About this document.** This GDD describes Lugarithm's design and the systems the team is building
> in this repository. Both play modes (Manual and Automation), the five-town coastal route, the Para
> play-by-coding language, the journal/Almanac, and the five runtime AI systems are implemented; some
> content, balancing, and production assets are still in active development (Section 13). The
> narrative beats and heritage reveals are authored from sourced research and continue to be reviewed.
> This document fixes the *structure, mechanics, and roles*.

---

## Contents

1. Executive Summary
2. Theme Alignment
3. Game Overview
4. Narrative Premise
5. Design Pillars
6. Core Gameplay Loop
7. The Two Modes (Manual & Automation)
8. The Para Language (Play-by-Coding)
9. Heritage, Towns & the Journal
10. AI Integration & Disclosure
11. Progression & Rewards
12. Technical Overview
13. Current Build Status
14. Ethics, Copyright & Cultural Responsibility
15. Team
16. Closing
- Appendix A — Glossary

---

## 1. Executive Summary

Lugarithm is a cozy, single-player **heritage road-trip that teaches computational thinking**. You
play a young man who has just lost a father he barely knew. The father left two things: a vintage
jeepney kept in perfect running order, and a journal — *Ang Aking Mga Ugat* ("My Roots") — meant to
explain the family's history. But every page has been torn out (*gisi*) and given away to old friends
in towns along the coast, one per town, so the son has to go meet them himself.

So he drives. The game is one road trip down the real coastal highway from **Iloilo City to San
Joaquin**, stopping at five historic towns to find the person holding a page and earn it back. Each
stop is a real place with real, sourced history. By the final town the player has absorbed enough to
give the tour themselves — which is the whole point.

Lugarithm's hook is that **you can drive the jeepney two ways, and they are the same game**: drive it
yourself in real time (**Manual Mode**), or write code to drive it (**Automation Mode**) using
**Para**, a friendly Python-subset offered as drag-and-drop blocks or a text editor. The route, the
passengers, the heritage, the mechanics, and the ending are identical; only your hands on the wheel
change. Coding is taught the way driving is — by doing, with stakes that are about earnings, never
failure.

AI is woven through the game as live, diegetic systems: passengers whose dialogue is freshly phrased
each playthrough but never invents history; an in-world heritage chatbot that answers only from what
you've unlocked; a coding mentor that reviews your solution against the ideal; and a co-pilot that
hints, explains, and can even compile plain-language intent into runnable code.

---

## 2. Theme Alignment

*"Giving Our History a New Heartbeat through the Intelligence of Tomorrow."*

- **History, driven not displayed.** Heritage is never delivered on a menu. You drive into a real
  town, meet a person who knew your father, and earn back a page of a single connected story — from
  pre-colonial gold-working to Spanish fort-churches to the craftsmen who signed their work in stone.
- **The intelligence of tomorrow, in service of yesterday.** AI in Lugarithm is diegetic, not a
  shortcut: it keeps passengers' voices alive on replay, runs the Almanac's heritage chatbot, mentors
  the player's code, and adapts placement to how each player plays — always grounded in sourced facts.
- **Roots passed hand to hand.** *Ang Aking Mga Ugat* is the father's journal; the pages were given
  to people, not left in a drawer. The player gathers them by meeting those people and listening.
- **Coding as a new literacy.** Teaching programming alongside heritage answers the theme literally:
  the tools of tomorrow carry the stories of yesterday forward to the next generation.

---

## 3. Game Overview

| Attribute | Detail |
|---|---|
| Working title | Lugarithm |
| Genre | Cozy narrative road-trip × coding puzzle |
| Platform | PC (Windows / macOS) |
| Engine | Unity (2D; isometric authored puzzles + top-down procedural towns), C# |
| Modes | Manual (real-time driving) and Automation (drive by code) — same content, 1:1 |
| Play-by-coding | **Para** — a Python-subset, as drag-drop blocks or a text editor (1:1 parity) |
| Session length | ~3–5 hours for a first full route |
| Target audience | Learners ~10–16 and up; players who enjoy cozy, story-rich games |
| Input | Keyboard/mouse; Manual uses WASD + brake |
| Language | English + Filipino UI (live switch); story/heritage content English in this pass |
| Team | Team Cyfer (Section 15) |

---

## 4. Narrative Premise

A young man inherits his estranged father's vintage jeepney and a journal whose pages have all been
torn out and scattered to old friends along the Iloilo coast. To rebuild the journal — and understand
the man who left it — he drives the old coastal highway from **Iloilo City to San Joaquin**, stopping
in five towns. In each, a person who knew his father rides with him or hosts him, shares the town's
history, and returns one page. The pages assemble a single arc: the region's deep history *and* the
personal reason the father scattered them in the first place, which the final town closes.

The road trip is structured as an **interactive living museum**. Heritage arrives through people and
place — a passenger's stories on the drive, townsfolk chatter on foot, and a heritage reveal at each
town's end — and is recorded, permanently, in the journal.

---

## 5. Design Pillars

| Pillar | What it means in play |
|---|---|
| **Two hands, one road** | Manual and Automation are the same journey. Whether you steer or you code, you cover the same route, meet the same people, and end the same way. Switching modes never changes the story. |
| **History is inherited, not assigned** | You don't read facts off a card; you earn them from people who knew your father. The passenger system makes heritage feel handed down. |
| **You can't lose, only learn** | Mistakes reduce a leg's earnings, never end the run. Coding and driving are both taught by doing, safely. |
| **AI assists; the player drives** | AI keeps voices alive, mentors your code, and hints without spoiling — but it never invents history and never finishes the puzzle for you unless you ask it to. |

---

## 6. Core Gameplay Loop

Each leg of the journey runs the same shape, in either mode:

1. **Arrive on foot.** A leg begins in a top-down **town hub**. Walk around, talk to townsfolk
   (press **E**) for ambient, lighter heritage flavor, then step onto the jeepney stop to drive.
2. **Board the story passenger.** A specific person who knew the father rides with you, sharing
   stories that set up the town's history (and often help with its puzzle).
3. **Drive the leg.** Either steer it yourself (**Manual**) or program it (**Automation**). Mid-drive,
   a mandatory **progression gate** — a short town puzzle tied to that town's real heritage — pops at
   a random point and must be cleared to continue.
4. **Deliver & reveal.** Drop the passenger at the destination *and* finish their conversation →
   a **heritage reveal** plays inline → a **completion card** → a **results** screen with your score
   and earnings.
5. **Recover & grow.** A **journal page** is recovered and a per-town **badge** is earned; earnings
   are banked to your wallet. Replay towns for higher scores (Section 11).

The leg-completion gate is the same in both modes: **passenger delivered + conversation finished**.

**Optional — the secret Artifact hunt (100% completion).** A town hub has one main coding objective
(which gates leaving) plus optional side objectives. Clear **every** objective — the main coding
quest *and* all side objectives — and a hidden **heritage Artifact** spawns somewhere in the town,
at a different randomized reachable spot each time. You find it by ear: a **Cultural Echo** ambient
cue plays from the artifact and **strengthens as you get closer** (silent from far away, swelling to
full as you're nearly on top of it), guiding you in. Walking up and pressing **E** collects it. It's
a pure completionist reward layered on top of the required progression.

---

## 7. The Two Modes (Manual & Automation)

Both modes cover the same route and heritage; the Settings menu switches between them at any time.

### 7.1 Manual Mode
Drive in real time on a continuous top-down road with lane drift and momentum. While steering you
manage **passenger boarding** (seats and drop-off flags) and **collect fares** through an interactive
**Coin Drawer** — choosing denominations to make exact change before a satisfaction timer empties. A
breakdown (overheating engine, snapped belt, empty tank) interrupts with a quick repair/refuel
minigame. Floating **dulog** markers show each passenger's drop-off stop and call *"Para!"* as you
approach.

### 7.2 Automation Mode
Step out of the seat and into a **coding workspace**. The route is shown as the same dressed,
streaming town as Manual; you drive it by writing **Para** — drag-and-drop blocks (no syntax) or a
text editor — and pressing **Run**. An execution bar gives **Run / Pause / Step / Reset** and a
**speed** slider; the jeepney animates smoothly along the road as your program runs. Fares and change
resolve through code (`collectFare()`, `giveChange(changeOwed())`), not the Coin Drawer.

A one-click **Autopilot** loads the canonical solution into your editor and runs it, so you can watch
the intended program drive the whole route — boarding riders, settling fares, and finishing at the
terminal. The solution is written as **named functions** so it reads like the real "ride a jeepney"
program (see Section 8).

**Parity:** the procedural town is dressed (heritage frontage + townsfolk) and laid out ahead from
the first frame, the driving is smooth, and the leg ends through the *same* reveal → completion card →
results flow as Manual. The only difference between the modes is that you write code instead of steer.

---

## 8. The Para Language (Play-by-Coding)

**Para** (after the call passengers shout to alight) is a **Python-minus-OOP** language with one
shared program model behind two front-ends — a **block editor** and a **text editor** — kept at 1:1
parity. It runs on a stepping interpreter that performs one jeepney action per tick, which makes
Run/Pause/Step/Speed natural and keeps procedural runs reproducible.

- **Control flow:** `if` / `elif` / `else`, `while`, `for … in range(…)`, `repeat`, `break`,
  `continue`, and the `not` / `and` / `or` operators.
- **Values & data:** variables, numbers/strings/booleans, lists, dicts, tuples, indexing/slicing, and
  built-ins (`len`, `range`, `print`, `min`, `max`, `sum`, `sorted`, `randint`, …).
- **User-defined functions:** players can `def` their own functions and call them — and are
  encouraged to, splitting a route into jobs like `drive()`, `handlePassengers()`, and
  `handleFares()`. Autopilot and the reference solutions are written this way as a model.
- **Domain API:** actions (`moveForward`, `turnLeft`, `turnRight`, `moveLeft`, `moveRight`, `driveToNextStop`, `driveToTerminal`, `pickUp`, `dropOff`, `collectFare`, `giveChange`, `wait`), questions (`frontIsClear`, `leftIsClear`, `rightIsClear`, `carInFront`, `atStop`, `passengerWaiting`, `hasPassengerAboard`, `atRequestedStop`, `isFull`, `routeComplete`, `atDestination`), and reporters (`fareOwed`, `cashTendered`, `changeOwed`, `seatsLeft`, `passengerCount`, `distanceToDestination`, `distanceTraveled`).

Concepts escalate per town: conditionals → loops + conditionals → functions → helper functions + loops → nested conditionals → multi-variable constraints. Errors are coached in plain language, never raw stack
traces. Full reference: [`docs/AutomationCommands.md`](docs/AutomationCommands.md) and the design in
[`docs/LANGUAGE_PLAN.md`](docs/LANGUAGE_PLAN.md).

---

## 9. Heritage, Towns & the Journal

The five-town route is a real stretch of the Iloilo coast, each town a sourced heritage focus:

| Level | Town | Heritage focus | Coding focus |
|---|---|---|---|
| Tutorial | Intro segment | Basic sequencing before the journey begins | Conditionals (`if` / `else`) |
| 1 | **Iloilo City (Molo)** | Molo Church ("feminist church"), American-era architecture, textile trade | Loops (`while` / `for`) + Conditionals |
| 2 | **Oton** | Pre-colonial gold-working & burial customs (the Oton Gold Mask), Batiano River trade | Functions (`def`) |
| 3 | **Tigbauan** | Hablon handloom weaving; WWII guerrilla resistance markers | Helper functions + loops |
| 4 | **Miag-ao** | Miag-ao Church (UNESCO, 1797): coconut tree of life on the facade, fort-church origin | Nested conditionals |
| 5 | **San Joaquin** | *Rendicion de Tetuan* bas-relief; Campo Santo baroque cemetery | Multi-variable constraints |

> **Guimbal** (*Taytay Tigre* bridge, coral-stone architecture) appears as a scenic drive-through with
> heritage dialogue, not a dedicated puzzle town in v1.

**Town puzzles** are tied to each town's real heritage (assembling the Oton Gold Mask, reconstructing
a Hablon weave, restoring the Miag-ao facade, finding the Campo Santo). Solving a town earns a
journal page.

**The journal — *Ang Aking Mga Ugat*** — is the heart of the game and unlocks as you progress: a
**Heritage** section (the father's recovered writing per town) and a **Coding Reference** (a plain
explanation + annotated example for each concept as it's introduced). The journal's **Almanac** hosts
the Heritage Oracle chatbot (Section 10). Heritage *payoffs* are reserved for the in-jeepney
conversation, the reveal, and the journal — townsfolk chatter is deliberately lighter. All historical
claims are drawn from [`docs/HERITAGE_RESEARCH.md`](docs/HERITAGE_RESEARCH.md); folklore is labelled
as folklore.

---

## 10. AI Integration & Disclosure

Lugarithm uses AI on two layers: inside the game as live systems the player touches, and in
development as fully disclosed production tools. The complete log is in
[`docs/AI_DISCLOSURE.md`](docs/AI_DISCLOSURE.md).

**Runtime AI (in-game, Google Gemini).** Each system has a deterministic authored fallback, hard
timeouts/token budgets, and grounding in authored, sourced content:

| System | Role |
|---|---|
| **Living Story** | Rephrases authored passenger dialogue fresh each playthrough — phrasing/warmth only, never new facts, names, or plot. |
| **Heritage Oracle (Almanac)** | RAG chatbot that answers only from **unlocked** journal pages, cites records, and refuses spoilers for unvisited towns. |
| **Coding Mentor** | After each puzzle, shows the player's solution beside an authored optimal one with plain explanations of why it's better. |
| **Co-Pilot / Vibe-Coding** | Tiered, spoiler-free hints; and Ask / Plan / Agent / Refactor modes — Agent compiles plain-language intent into validated, puzzle-solving code. |
| **Ghost-text completion** | Copilot-style inline next-line suggestion in the Code editor after a typing pause; **Tab** to accept. Tiny debounced requests, cached, disabled in block mode. |

Keys live only in the local, git-ignored root `.env` (synced to a generated `ai_config.json`); no
secret is committed.

**Development AI (disclosed tools).**

| Area | Tooling | Use & safeguard |
|---|---|---|
| Code | **Claude (Anthropic)** + **OpenAI Codex** | Scripting and coding assistance — systems, refactors, tests, docs — reviewed and owned by the team in public Git history. |
| Art | **Gemini "Nano Banana 2"** | Concept and placeholder art generation, hand-reviewed and finished; original/licensed assets only; folklore labelled as folklore. |

---

## 11. Progression & Rewards

- **Currency (a peso `₱` wallet)** is earned by completing legs efficiently, solving town puzzles,
  and learning the history accurately; it's banked to the save on town completion and spent on
  refuels/repairs. Underfunded refuels create **debt** that is paid down from later earnings. The
  Code editor carries a small scoring multiplier over blocks.
- **Badges** — a per-town badge is awarded on completion (with an unlock overlay), recorded in the
  save.
- **Unlockable code editor themes** — earned cosmetics that reskin the text editor's syntax
  highlighting.
- **Best scores** are tracked per town; **replay** any town to improve your score.

> **Future / aspirational.** The original design imagined a **gacha** layer spending currency on
> heritage-themed cosmetics (jeepney paint per town, performance upgrades, new vehicles). That
> economy is **not implemented** in the current build — the shipped rewards are the wallet, badges,
> and code themes above. Gacha remains a possible future extension, not a current feature.

---

## 12. Technical Overview

- **Engine:** Unity 2D — isometric authored puzzles + top-down procedural towns; C#, no namespaces,
  manager singletons. Production scenes are **generated by editor scripts**
  (`Assets/Editor/SceneBuilders/`), not hand-edited.
- **Shared systems:** both modes build the procedural world from one `TownLayout`
  (`StreamingTownGenerator` → projectors → `RouteVisualBuilder.BuildProcedural` + `RoadsideDecorator`)
  and share fare math (`FareMath`) and dulog logic, so the two modes stay 1:1.
- **Para runtime:** `Lexer` → `Parser` → `Ast` → `Interpreter` (stepping VM) with a deterministic
  simulation (`AgentSim`) as the source of truth; `HeadlessProgramRunner` dry-runs programs in tests.
- **AI:** runtime Gemini calls via `GeminiClient` with a multi-key + model-ladder fallback, configured
  from the git-ignored `.env`; a RAG pipeline backs the Almanac; usage is tracked in editor.
- **Save & settings:** local JSON saves (auto-save on town completion); sectioned settings with
  segmented pill selectors; English/Filipino UI via `LocalizationManager`/`LocalizedLabel` (live).
- **Targets:** PC (Windows & macOS).

---

## 13. Current Build Status

**Implemented:** both play modes end-to-end (Manual real-time driving with passengers/fares/breakdowns;
Automation with the Para block + code editors, stepping execution, and autopilot); the shared dressed,
streaming procedural town; the Para language including user-defined functions; the five Gemini AI
systems with authored fallbacks; the journal/Almanac; the top-down town hub with objectives and the
optional secret **Artifact hunt** (randomized placement + the Cultural Echo proximity-audio cue,
both unit-tested); sectioned settings and English/Filipino UI; the **currency wallet, per-town
badges, and unlockable code themes**; local save.

**Recent (this pass):** Automation pre-grows the procedural town so the dressed street is present from
the first frame; the autopilot/reference solution is restructured into user-defined functions and the
scaffold teaches functions; the Automation completion card matches Manual's wording so endings read
identically.

**In active development:** true mid-program world streaming in Automation (currently pre-grown +
on-completion append, since rebuilding the interpreter grid mid-run interrupts execution); the
Vibe-Coding action-graph generator emitting user-defined functions; a full Manual/Automation
mechanic-parity audit; content balancing and final/original production art (placeholders in use);
story/heritage localization beyond the English pass.

---

## 14. Ethics, Copyright & Cultural Responsibility

- **Full AI disclosure** is maintained in [`docs/AI_DISCLOSURE.md`](docs/AI_DISCLOSURE.md) and updated
  with every new tool, generated asset batch, or runtime change.
- **Original or properly-licensed assets only.** No third-party IP; generated art is reviewed,
  finished, and replaced with original/licensed assets before release.
- **Sourced history, labelled folklore.** Heritage facts come from `docs/HERITAGE_RESEARCH.md`;
  runtime AI may not assert new history; folklore is always framed as folklore.
- **Cultural respect.** Town heritage and Filipino-language usage are checked against sourced
  references; the journal reserves the strongest beats for an authored reveal, not an AI paraphrase.
- **No secrets shipped.** Runtime keys stay in the local git-ignored `.env`.

---

## 15. Team

**Team Cyfer** • Region VI – Western Visayas • AI Game On! IV / AI Fest 2026.

| Member | Roles |
|---|---|
| Carlos John Aristoki | Lead Developer · Game Design · Narrative · Documentation |
| Sol Vincent Sartaguda | Developer · Narrative · Documentation |
| Zneb John Delariman | 2D Artist · Asset Maker · Design |

---

## 16. Closing

Lugarithm turns a quiet road trip into two things at once: a way to inherit a region's history from
the people who hold it, and a way to learn to code by driving. Whether the player keeps their hands on
the wheel or writes the program that does, they cover the same coast, meet the same people, and earn
back the same torn pages. AI keeps the voices alive, reviews the code, and points the way — but it's
the player who drives, listens, and, by the last town, can tell the story themselves. That is how
history gets a new heartbeat.

---

## Appendix A — Glossary

- **Para** — the call passengers shout to alight; also the name of the play-by-coding language.
- **Dulog** — a passenger's drop-off stop; marked in-world and shared by both modes.
- **Gisi** — "torn"; the journal's pages were torn out and scattered.
- **Ang Aking Mga Ugat** — "My Roots," the father's journal the player rebuilds.
- **Autopilot** — a one-click run of the canonical, function-structured reference solution.
- **Town hub** — the on-foot, top-down area at the start of a leg where you meet townsfolk and board.
- **Heritage reveal** — the inline payoff that plays when a leg's story passenger is delivered.
