# AGENTS.md — Operating Contract for Lugarithm

Guidance for AI coding agents (and humans) working in this repository. Read this and
[`docs/PROMPT_CONTEXT.md`](docs/PROMPT_CONTEXT.md) before editing. Authority order:
this file → [`README.md`](README.md) (the GDD / full-game promise) →
[`docs/PRD.md`](docs/PRD.md) (testable requirements) →
[`docs/PHASE_TASKS.md`](docs/PHASE_TASKS.md) (build order + evidence). If code and docs disagree,
report it — do not silently rewrite behavior.

*Lugarithm* is a heritage jeepney coding road-trip for **AI Game On! IV · AI Fest 2026**, by
**Team Cyfer** (Region VI – Western Visayas). It teaches programming through driving a jeepney down
the real Iloilo → San Joaquin coast.

## Stack

- **Engine:** Unity (2D; isometric authored puzzles + top-down procedural towns). Scripts are C#.
- **Scenes are editor-generated.** Production scenes are built by editor scripts under
  `Assets/Editor/SceneBuilders/` (e.g. `AutomationDriveSceneBuilder.cs`, `CodeDriveSceneBuilder.cs`,
  and the Manual/menu builders). **Do not hand-edit generated `.unity` scenes** — change the builder
  and regenerate. When you add a `[SerializeField]` to a controller, wire it in the builder too.
- **Runtime AI:** Google Gemini, configured from the local git-ignored root `.env`, synced by
  `Assets/Editor/EnvConfigSync.cs` into a generated `Assets/Resources/ai_config.json`. Up to five
  free-tier keys + a model ladder are supported with automatic fallback (`Assets/Scripts/AI/GeminiClient.cs`).
- **Save:** local JSON via `SaveSystem`; auto-saves on town completion.
- **Localization:** `LocalizationManager` + `LocalizedLabel` (English/Filipino UI, live switch).

## Conventions

- **No namespaces.** Types are global; keep names unique and descriptive.
- **Manager singletons** (`GameManager.Instance`, `SettingsManager.Instance`, etc.) — always
  null-guard them so scenes stay playable when launched directly in the editor.
- One primary class per file; `PascalCase` types/methods, `camelCase` locals/fields,
  `UPPER_SNAKE`/`PascalCase` consts as the surrounding code does. Match the file you're editing.
- Pure gameplay logic (the sim, fare math, language interpreter, layout generation) is kept
  presentation-agnostic and is the source of truth; views animate what the logic reports.
- Authored content (heritage facts, dialogue, levels, fares) lives in data/libraries, not in
  control flow. Keep historical claims sourced (`docs/HERITAGE_RESEARCH.md`).
- Conventional Commits. Preserve unrelated work in a dirty tree. Never commit secrets or `.env`.

## The Para language (play-by-coding)

- Python-minus-OOP, one shared AST behind two front-ends (text editor + drag-drop blocks).
  Pipeline: `Lexer` → `Parser` → `Ast` → `Interpreter` (a stepping VM, one agent action per tick).
  Vocabulary is registered in `AgentApi`; the parser validates names against it.
- **User-defined functions are supported** (`def name(params):`, `Ast.FuncDefStmt`,
  `Parser.ParseDef`, interpreter frame stack). The parser pre-collects function names so calls
  resolve regardless of definition order. Functions may call other functions; actions bubble up
  through the frame stack.
- Actions (`driveToNextStop`, `pickUp`, `dropOff`, `collectFare`, `giveChange`), queries
  (`passengerWaiting`, `atRequestedStop`, `routeComplete`, …), and reporters (`changeOwed`,
  `fareOwed`, `seatsLeft`, …) are the domain API. See `docs/AutomationCommands.md` and
  `docs/LANGUAGE_PLAN.md`.
- The simulation (`AgentSim`) is deterministic and is the single source of truth for win/loss,
  fares, and passenger state. EditMode tests drive it directly; `HeadlessProgramRunner` dry-runs
  programs without a scene.

## The two modes must stay 1:1

Manual and Automation are the **same game** with one difference — direct driving vs. driving by
code. They must share mechanics and the ending:

- Both build the procedural world from the same `TownLayout` and dress it with
  `RouteVisualBuilder.BuildProcedural` + `RoadsideDecorator` (Automation does this via
  `TopDownGridSpace`, which reuses the Manual builder).
- Both end the same way: deliver the story passenger to the destination **and** finish their chat →
  heritage reveal → `LegCompletionController` card → results → `GameManager.CompleteLevel` →
  LevelSelect. Keep the completion wording and flow identical across `ManualDriveController` and
  `AutomationDriveController`.
- Fares/change in Automation resolve via code calls (`collectFare()`/`giveChange(amount)`), not the
  Coin Drawer minigame; the underlying fare math (`FareMath`) is shared with Manual.
- Autopilot and the canonical reference solution are written as user-defined functions so generated
  code reads like the real "ride a jeepney" program.
- When you change a shared mechanic, update both modes (or the shared system) — never let them drift.

## The five runtime AI systems

All go through `GeminiClient`; all have deterministic authored fallbacks; all are budgeted/timed.

1. **Living Story** (`LivingStoryService`) — rephrases authored passenger lines; never invents facts.
2. **Heritage Oracle** (`HeritageOracleService` + `KnowledgeRagService`) — Almanac chatbot, RAG over
   *unlocked* journal pages, refuses spoilers for unvisited towns.
3. **Coding Mentor** (`CodingMentorService` + `CodeAnalyticsService`) — post-level analysis vs. an
   authored optimal solution; refactored code is validated against unlocked vocabulary.
4. **Co-Pilot / Vibe-Coding** (`CopilotHintService`, `VibeCodingService`, `VibeIntentRouter`) —
   tiered hints + Ask/Plan/Agent/Refactor; generated programs are validated and must solve the
   puzzle before they touch the editor.
5. **Ghost-text completion** (`GhostTextController`) — Copilot-style inline next-line suggestion in
   the Code editor (Tab to accept); debounced/cached; disabled in block mode.

> The secret Artifact's placement (`OverworldArtifactPlacement`) and its Cultural Echo proximity
> audio (`ArtifactProximityAudio`) are **deterministic C#, not a Gemini system**; skill-adaptive
> placement is a future idea, not implemented.

Never hardcode keys, provider URLs, or prompts-with-secrets in scripts. New AI work →
append `docs/AI_DISCLOSURE.md`.

## Commands

- Run the game from Unity (the Bootstrap/menu scene). Levels resolve via
  `GameManager.Instance.SelectedLevelIndex`; both drive scenes fall back to Tutorial when launched
  directly in the editor.
- Tests are Unity **EditMode** tests (run from the Test Runner, or batchmode
  `-runTests -testPlatform EditMode`). Add focused tests for new logic (sim, language, fares,
  generation). Keep the suite green.
- After changing controller serialized fields or world wiring, **regenerate the affected scene**
  via its builder under `Assets/Editor/SceneBuilders/`.

## Definition of done

- Satisfies the mapped README promise + PRD requirement(s); keeps the two modes 1:1.
- Logic is typed, deterministic where it should be, and covered by EditMode tests.
- Authored/heritage content stays in data and is sourced; folklore labelled as folklore.
- In-editor manual check for user-facing behavior (driving, world, completion, AI flows).
- No committed secrets/caches; `docs/AI_DISCLOSURE.md` updated for new AI tools/assets/runtime work;
  `docs/PHASE_TASKS.md` evidence updated only after the stated gates pass.
