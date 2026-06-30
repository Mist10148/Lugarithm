# Lugarithm — Product Requirements Document

Testable build contract for *Lugarithm* (AI Game On! IV · AI Fest 2026 · Team Cyfer). This PRD turns
the [GDD](../README.md) into requirement IDs with acceptance criteria. It may clarify the GDD but may
not drop a GDD promise. Authority: [`AGENTS.md`](../AGENTS.md) → README (GDD) → this PRD →
[`PHASE_TASKS.md`](PHASE_TASKS.md).

| Field | Value |
|---|---|
| Version | 0.4 |
| Last updated | 2026-06-27 |
| Engine | Unity 2D (C#) |
| Platforms | PC — Windows & macOS |
| Save | Local JSON, auto-save on town completion |
| Runtime AI | Google Gemini (keys via git-ignored `.env`), authored fallbacks for every system |

## Status markers

`[x]` implemented & verified · `[-]` implemented, needs verification/balancing · `[ ]` not started ·
`[!]` blocked by a decision/dependency.

---

## 1. Product Overview

A single-player heritage road-trip that teaches programming. The player drives a late father's
jeepney down the Iloilo → San Joaquin coast, recovering torn journal pages from people in five
historic towns. The game ships **two modes that are the same game**: Manual (real-time driving) and
Automation (drive by writing **Para**, a Python-subset, as blocks or text). The mode is the only
difference; route, heritage, mechanics, and the ending are 1:1.

## 2. Goals & Non-Goals

**Goals.** Teach computational thinking and regional heritage together; make coding feel like a
second way to play, not a tutorial; keep history sourced and respectful; ship a polished vertical
slice (Tutorial + 5 towns) for AI Fest with all five AI systems live and offline-safe fallbacks.

**Non-goals (v1).** Multiplayer; mobile/console; full story/heritage localization (UI is
English/Filipino; story content is English this pass); Guimbal as a full puzzle town; an OOP language.

## 3. Target Audience

Learners ~10–16 and up; players of cozy, story-rich games. The coding interface scales from
no-syntax blocks to a text editor.

---

## 4. Core Loop Requirements (LOOP)

- **LOOP-R1** `[x]` A leg begins on foot in a top-down **town hub** where the player can walk and
  talk to townsfolk (press E) for ambient heritage flavor, then board the jeepney.
- **LOOP-R2** `[x]` A **story passenger** who knew the father boards at the start of a drive and
  shares heritage during the leg.
- **LOOP-R3** `[x]` A mandatory mid-drive **progression gate** (a town puzzle) appears at a random
  point and must be cleared before the leg can complete.
- **LOOP-R4** `[x]` A leg **completes** when the story passenger is delivered to the destination
  **and** their conversation has finished — in *either* order. This triggers the heritage reveal,
  completion card, and results.
- **LOOP-R5** `[x]` The player cannot "lose"; mistakes only reduce a leg's earnings.

## 5. Mode Parity Requirements (MODE) — the headline invariant

- **MODE-R1** `[x]` Manual and Automation cover the same route, passengers, and heritage; the mode is
  switchable in Settings.
- **MODE-R2** `[x]` Both modes build the procedural world from the same `TownLayout` and dress it with
  `RouteVisualBuilder.BuildProcedural` + `RoadsideDecorator` (Automation via `TopDownGridSpace`).
- **MODE-R3** `[-]` The Automation procedural town is **present and dressed from the first frame**,
  laid out ahead of the start (pre-grown at `Start()`), not a stub that only extends after a win.
  *Verify in-editor that the street + townsfolk render ahead from frame 1.*
- **MODE-R4** `[-]` Automation driving is **smooth/continuous** (the jeepney eases along the road,
  not visibly teleporting cell-to-cell). *Verify on-screen.*
- **MODE-R5** `[x]` Both modes end through the **same** flow: deliver + finish chat → heritage reveal
  → `LegCompletionController` completion card (same wording) → results → `GameManager.CompleteLevel`
  → LevelSelect.
- **MODE-R6** `[x]` Automation fares/change resolve via code (`collectFare()`/`giveChange(amount)`)
  with the **same `FareMath`** Manual uses; no Coin Drawer in Automation.
- **MODE-R7** `[ ]` *Follow-up:* true mid-program world streaming in Automation (today: pre-grow +
  on-completion append, because rebuilding the interpreter grid mid-run interrupts execution).
- **MODE-R8** `[-]` Any change to a shared mechanic updates both modes or the shared system; a
  parity audit is maintained. *Audit pending.*

## 6. Manual Mode Requirements (MAN)

- **MAN-R1** `[x]` Real-time top-down driving with lane drift and momentum (`JeepneyController`); a
  configurable Hold/Toggle brake.
- **MAN-R2** `[x]` Passenger boarding with seat capacity and per-passenger drop-off (`PassengerManager`);
  missed stops eject the passenger with a penalty.
- **MAN-R3** `[x]` Interactive **Coin Drawer** fare/change minigame with a satisfaction timer
  (`CoinDrawerController`, `FareMath`).
- **MAN-R4** `[x]` **Dulog** markers show each passenger's drop-off stop and call *"Para!"* on
  approach (`DulogMarkerController`/`DulogModel`).
- **MAN-R5** `[x]` Breakdown minigames (engine/fuel/maze) interrupt the drive and resume it.

## 7. Automation Mode Requirements (AUT)

- **AUT-R1** `[x]` A coding workspace with a **block** editor and a **text** editor at 1:1 parity,
  switchable via the Coding Interface setting.
- **AUT-R2** `[x]` Execution bar: **Run / Pause / Step / Reset** and a **speed** slider; per-line
  highlight + heatmap; plain-language runtime coaching (`ExecutionController`).
- **AUT-R3** `[x]` **Autopilot** loads the canonical reference solution into the active editor and
  runs it to completion.
- **AUT-R4** `[x]` The reference/autopilot solution is written as **user-defined functions**
  (`drive()`, `handlePassengers()`, `handleFares()`) and includes the full ride logic (board, fare,
  change, drop-off); the scaffold teaches defining functions.
- **AUT-R5** `[x]` Win = reach the destination with every committed rider served and settled
  (`AgentSim.IsWin`/`routeComplete`), feeding the same completion flow as Manual (MODE-R5).

## 8. Para Language Requirements (LANG)

- **LANG-R1** `[x]` One shared program model (`Ast`) behind block + text front-ends; a stepping
  interpreter (one action per tick) keeps Run/Pause/Step/Speed natural and deterministic.
- **LANG-R2** `[x]` Control flow: `if/elif/else`, `while`, `for … in range`, `repeat`, `break`,
  `continue`, `not/and/or`.
- **LANG-R3** `[x]` Values & data: variables, numbers/strings/booleans, lists/dicts/tuples,
  indexing/slicing, built-ins (`len`, `range`, `print`, `min`, `max`, `sum`, `sorted`, `randint`, …).
- **LANG-R4** `[x]` **User-defined functions** (`def`), including calls to user functions from within
  other functions; the parser resolves function names regardless of definition order.
- **LANG-R5** `[-]` Block-mode representation of "define/call function" — or a clean fallback to Code
  mode when a program uses code-only features (the autopilot fallback exists). *Verify coverage.*
- **LANG-R6** `[x]` Errors are coached in plain language, never raw stack traces.
- **LANG-R7** `[x]` Domain vocabulary lives in `AgentApi`; per-level allowed sets drive the
  palette/UI; parse validation accepts API actions, builtins, and user-defined functions.

## 9. AI System Requirements (AI)

All runtime AI uses Google Gemini via `GeminiClient`, keys from the git-ignored `.env`; **each system
has a deterministic authored fallback** and per-feature timeouts/token budgets. See
[`PROMPT_CONTEXT.md`](PROMPT_CONTEXT.md) for budgets.

- **AI-R1** `[x]` **Living Story** dialogue rephrases authored lines only; it must not change facts,
  names, or plot, and falls back to the authored line on timeout (`LivingStoryService`).
- **AI-R2** `[x]` **Heritage Oracle** (Almanac) answers only from **unlocked** journal pages (RAG),
  cites records, and refuses spoilers for unvisited towns (`HeritageOracleService`,
  `KnowledgeRagService`).
- **AI-R3** `[x]` **Coding Mentor** shows the player's solution beside an authored optimal one with
  explanations; any refactor is validated against the level's unlocked vocabulary
  (`CodingMentorService`).
- **AI-R4** `[x]` **Co-Pilot** gives tiered, spoiler-free hints; **Vibe-Coding** offers
  Ask/Plan/Agent/Refactor — Agent/Refactor output is validated and must solve the puzzle before it
  touches the editor (`CopilotHintService`, `VibeCodingService`, `VibeIntentRouter`).
- **AI-R5** `[-]` **Context-aware placement** of heritage collectibles by skill/playstyle.
  *Verify adaptation logic.*
- **AI-R6** `[x]` No keys, provider URLs, or prompt-secrets in scripts; config is synced from `.env`
  to a generated `ai_config.json` by `EnvConfigSync`. Usage is tracked (`AiUsageTracker`).
- **AI-R7** `[ ]` *Follow-up:* the Vibe-Coding action-graph generator may emit user-defined `def`
  blocks (the language supports functions; the generator currently emits a flattened action graph).

## 10. Heritage & Journal Requirements (HER)

- **HER-R1** `[x]` Five sourced town foci + Guimbal drive-through; all claims trace to
  `docs/HERITAGE_RESEARCH.md`; folklore is labelled as folklore.
- **HER-R2** `[x]` The **journal** (*Ang Aking Mga Ugat*) has a Heritage section (per-town reveals)
  and a Coding Reference (per-concept explanation), unlocking as concepts/towns are reached.
- **HER-R3** `[x]` The Almanac hosts the Heritage Oracle chatbot over unlocked pages (AI-R2).
- **HER-R4** `[x]` Town puzzles are tied to each town's real heritage and award a journal page.

## 11. Progression Requirements (PROG)

- **PROG-R1** `[x]` Currency earned by leg efficiency, puzzle accuracy, and learning; the Code editor
  carries a small scoring multiplier over Blocks.
- **PROG-R2** `[x]` **Gacha** pulls grant per-town badges, jeepney cosmetics, performance upgrades,
  and vehicles; towns are replayable for more pulls.
- **PROG-R3** `[x]` Progress, currency, unlocks, and journal state persist via local JSON save.

## 12. Settings & Accessibility (SET)

- **SET-R1** `[x]` Sectioned settings (Gameplay, Controls, Audio, Language & Text, Appearance) using
  segmented pill selectors.
- **SET-R2** `[x]` Drive Mode (Manual/Automation) and Coding Interface (Blocks/Code) are settings.
- **SET-R3** `[x]` English/Filipino **UI** switches live via `LocalizationManager`/`LocalizedLabel`
  (no restart). Story/heritage content is English this pass.
- **SET-R4** `[x]` Subtitles toggle and Dialogue Speed (Slow/Normal/Fast/Instant); music/SFX sliders;
  unlockable code themes.

## 13. Platform & Technical (PLAT)

- **PLAT-R1** `[x]` Unity 2D, C#, no namespaces, manager singletons (null-guarded for direct editor
  play).
- **PLAT-R2** `[x]` Production scenes are **generated by editor scripts**
  (`Assets/Editor/SceneBuilders/`); serialized wiring is added in the builder, not by hand-editing
  scenes.
- **PLAT-R3** `[-]` Targets Windows & macOS; build/export verification pending.
- **PLAT-R4** `[-]` Performance acceptable on the procedural streaming town at the reference
  resolution; profiling pending.

## 14. Per-Level Design

| Level | Town | Heritage | Coding concept | Town puzzle |
|---|---|---|---|---|
| Tutorial | Intro | Sequencing intro | Linear sequencing | guided drills |
| 1 | Iloilo City (Molo) | Molo Church, textile trade | Conditionals | (heritage-tied) |
| 2 | Oton | Oton Gold Mask, river trade | Lists & indexing | assemble the mask |
| 3 | Tigbauan | Hablon weaving, WWII markers | Functions + loops | reconstruct a weave |
| 4 | Miag-ao | Miag-ao Church (UNESCO) | Nested conditionals | restore the facade |
| 5 | San Joaquin | Rendicion de Tetuan, Campo Santo | Multi-variable constraints | reach the Campo Santo |

Difficulty is intentional: early legs isolate one concept; later legs layer them; post-game
recombines the pool.

## 15. Out of Scope (v1)

Multiplayer; mobile/console; full story/heritage localization; Guimbal as a puzzle town; OOP language
features (lambdas, classes, exceptions, imports beyond seeded `random`).

## 16. Open Questions

- **Q1** Final currency name. `[ ]`
- **Q2** Gacha pity threshold / drop rates. `[ ]`
- **Q3** Mid-program streaming approach for Automation (MODE-R7) vs. the pre-grow compromise. `[!]`
- **Q4** Whether the Vibe-Coding generator should emit user-defined functions (AI-R7). `[ ]`
- **Q5** Team member names/roles for the submission package. `[x]` Resolved — Carlos John
  Aristoki (Lead Developer, Game Design, Narrative, Documentation); Sol Vincent Sartaguda
  (Developer, Narrative, Documentation); Zneb John Delariman (2D Artist, Asset Maker, Design).
