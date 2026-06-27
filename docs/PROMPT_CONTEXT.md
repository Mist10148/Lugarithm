# Lugarithm Prompt Context

Canonical repository context for agents that write implementation prompts for *Lugarithm*. Read this
first, then inspect the actual code/scenes/data/docs relevant to the task — this snapshot does not
replace source inspection.

- Authority: [`AGENTS.md`](../AGENTS.md) → [`README.md`](../README.md) (GDD) →
  [`docs/PRD.md`](PRD.md) (requirements) → [`docs/PHASE_TASKS.md`](PHASE_TASKS.md) (build order).
  Treat running code as truth for what exists; if code and docs disagree, report it.
- Snapshot context: 2026-06-27. Recent change — **Automation↔Manual convergence (partial):** the
  procedural town is pre-grown at start so the dressed street is present from frame 1; autopilot and
  the reference solution are now user-defined functions; the completion card matches Manual. Known
  remaining gaps are listed under "Verified state" and "Follow-ups".

## Game Identity

- **Concept:** A cozy, single-player heritage road-trip that teaches programming. A young man drives
  his late father's vintage jeepney down the real Iloilo → San Joaquin coast, recovering torn
  journal pages from people in five historic towns and learning the family's and region's history.
- **Theme:** "Giving Our History a New Heartbeat through the Intelligence of Tomorrow"
  (AI Game On! IV · AI Fest 2026, Region VI – Western Visayas; Team Cyfer).
- **Two modes, same content:** **Manual** (real-time driving) and **Automation** (drive by writing
  code in **Para**, a Python-subset, via drag-drop blocks or a text editor). The mode is the only
  difference; route, heritage, mechanics, and the ending are 1:1.
- **Genre/audience:** Cozy narrative adventure + coding puzzle, for ages ~10–16 and up.
- **Engine:** Unity 2D (isometric authored puzzles + top-down procedural towns), C#, no namespaces,
  manager singletons, editor-generated scenes.
- **Levels:** Tutorial → Iloilo City/Molo (conditionals) → Oton (lists/indexing) → Tigbauan
  (functions + loops) → Miag-ao (nested conditionals) → San Joaquin (multi-variable constraints).
  Guimbal is a scenic drive-through, not a puzzle town in v1.

## Core Loop

1. Walk the top-down **town hub** on foot; talk to townsfolk (press E) for ambient heritage flavor.
2. Board the jeepney; a **story passenger** who knew the father rides along and shares history.
3. Drive the leg — **Manual** (WASD + Coin Drawer fares) or **Automation** (write Para; run/step/
   pause/speed; one-click Autopilot). A mandatory mid-drive **progression gate** (town puzzle) pops.
4. Deliver the passenger to the destination **and** finish their chat → heritage reveal → completion
   card → results → currency → LevelSelect.
5. Recover a journal page; spend currency on gacha heritage cosmetics; replay for better scores.

## Verified State (2026-06-27)

- **Both modes implemented.** Manual: `ManualDriveController` + `JeepneyController` (continuous
  physics, lane drift), `PassengerManager`, `CoinDrawerController`, `DulogMarkerController`,
  streaming dressed town (`StreamingTownGenerator` → `ManualLayoutProjector` →
  `RouteVisualBuilder.BuildProcedural` + `RoadsideDecorator`). Automation: `AutomationDriveController`
  + grid sim (`AgentSim`/`GridModel`/`GridPathfinder`), `ExecutionController`, `TopDownGridSpace`
  (reuses the Manual dressed builder), block + code editors, `SelfDrivePlanner` autopilot.
- **Para language:** `Lexer`/`Parser`/`Ast`/`Interpreter` support sequencing, `if/elif/else`,
  `while`, `for`/`range`, `repeat`, `break`/`continue`, **user-defined functions** (`def`),
  variables, expressions/operators, lists/dicts/tuples, and built-ins (`len`, `range`, `print`,
  `randint`, …). Domain API in `AgentApi`.
- **Automation now streams the procedural town during the coded drive** using the same generator and
  visual append path as Manual; the dressed street is present from frame 1 and grows before the
  jeepney reaches the frontier. Autopilot/reference solution is function-structured
  (`drive()`/`handlePassengers()`/`handleFares()`/`handleDropoffs()`).
- **Five Gemini AI systems** implemented with authored fallbacks (see below).
- **Settings/localization:** sectioned settings with segmented pill selectors; English/Filipino UI
  live switch (`LocalizationManager`/`LocalizedLabel`).
- **Follow-ups (not yet done):** in-editor side-by-side verification of the new stream-ahead behavior
  and completion flow; broader Manual/Automation mechanic-parity audit beyond the shared generation,
  passenger/fare, function-autopilot, and ending changes covered by Phase 7.

## Runtime AI — feature budgets & grounding

All via `GeminiClient` (keys from git-ignored `.env` → generated `ai_config.json`); each has a
deterministic authored fallback.

| System | Service(s) | Grounding / safeguard |
|---|---|---|
| Living Story dialogue | `LivingStoryService`, `DialogueController` | Rephrases authored lines only; never changes facts/names/plot; hard timeout → authored fallback; trivial lines skip AI. |
| Heritage Oracle (Almanac) | `HeritageOracleService`, `KnowledgeRagService` | RAG over **unlocked** journal pages; cites records; refuses spoilers for unvisited towns. |
| Coding Mentor | `CodingMentorService`, `CodeAnalyticsService` | Compares player code to an authored optimal; refactor validated against unlocked vocabulary. |
| Co-Pilot / Vibe-Coding | `CopilotHintService`, `VibeCodingService`, `VibeIntentRouter`, `GhostTextController` | Tiered hints (no spoilers); Agent/Refactor output validated + dry-run; only applied if it solves the puzzle. |
| Procedural placement | (context-aware spawner) | Places collectibles by skill/playstyle. |

Usage is tracked by `AiUsageTracker` (editor report). No keys/prompts-with-secrets in scripts.

## Important Files

- `AGENTS.md`, `README.md`, `docs/PRD.md`, `docs/PHASE_TASKS.md`, `docs/LANGUAGE_PLAN.md`,
  `docs/AutomationCommands.md`, `docs/HERITAGE_RESEARCH.md`, `docs/HERITAGE_FUNFACTS.md`,
  `docs/DIALOGUE_SCRIPT.md`, `docs/AI_DISCLOSURE.md`.
- Manual: `Assets/Scripts/Manual/*` (`ManualDriveController`, `JeepneyController`, `PassengerManager`,
  `FareMath`, `CoinDrawerController`, `DulogMarkerController`, `RouteVisualBuilder`, `RoadsideDecorator`).
- Automation: `Assets/Scripts/Automation/*` (`AutomationDriveController`, `AgentSim`,
  `ExecutionController`, `SelfDriveAgent`, `TopDownGridSpace`/`TopDownAgentView`, `Lang/*`, `Blocks/*`).
- Generation (shared): `Assets/Scripts/Levels/Generation/*` (`TownLayout`, `TownLayoutGenerator`,
  `StreamingTownGenerator`, `ManualLayoutProjector`, `GridLayoutProjector`, `PassengerRequest`).
- AI: `Assets/Scripts/AI/*`; config: `Assets/Editor/EnvConfigSync.cs`, root `.env` (+ `.env.example`).
- Scene builders: `Assets/Editor/SceneBuilders/*` (regenerate scenes after wiring changes).

## Conventions & Verification

- See `AGENTS.md` for the full conventions (no namespaces, singletons null-guarded, editor-generated
  scenes, modes-stay-1:1, language invariants).
- Verify: Unity EditMode tests green (`-runTests -testPlatform EditMode`); regenerate affected
  scenes; in-editor manual check of driving/world/completion and any AI flow touched. Confirm no
  secret is tracked (root `.env` stays git-ignored).

## Prompt Requirements

Every implementation prompt for this repo must: (1) require reading this file, then the exact
relevant source; (2) name the README promise / PRD requirement being implemented; (3) separate
verified behavior from assumptions; (4) preserve the two-modes-1:1 invariant, typed C#, the shared
sim/fare/generation source-of-truth, and editor-generated scenes; (5) include EditMode test +
in-editor manual gates; (6) require `docs/AI_DISCLOSURE.md` updates for new AI tools/assets/runtime
work and `docs/PHASE_TASKS.md` evidence only after gates pass.
