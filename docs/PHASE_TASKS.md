# Lugarithm — Phase Tasks

> **Status:** DRAFT
> **Version:** 0.1
> **Team:** Cyfer
> **Last Updated:** June 4, 2026

This document breaks development into phases. Phases are sequential at the macro level but individual tasks within a phase can run in parallel. All estimates are rough and subject to revision.

---

## Phase Overview

| Phase | Name | Focus |
|-------|------|-------|
| 0 | Pre-Production | Design lock, asset pipeline, tech decisions |
| 1 | Foundation | Core Unity systems, scene structure, save/load |
| 2 | Drive System | Scrolling drive, breakdown triggers, passenger framework |
| 3 | Repair Puzzles | Block Mode + Code Mode puzzle engine |
| 4 | Town 1 — Oton | First full town: puzzle, passenger, journal page, artifact |
| 5 | AI Integration | Dynamic dialogue, Almanac chatbot, post-level analytics |
| 6 | Towns 2–5 | Tigbauan, Guimbal, Miagao, San Joaquin |
| 7 | Gacha & Progression | Currency, pulls, jeepney customization, vehicle unlocks |
| 8 | Level Variety Layer | Tour guide levels, special challenge levels |
| 9 | Polish & QA | Audio, UI, playtesting, bug fixes, localization |
| 10 | Ship Prep | Build pipeline, platform submission, final QA |

---

## Phase 0 — Pre-Production

> Goal: Lock enough design to begin building. Resolve all blockers in [PRD Open Questions](./PRD.md#12-open-questions) that affect Phase 1–3.

### Design
- [ ] Finalize five-town journey map and narrative beats per town
- [ ] Write passenger profiles (name, personality, backstory, knowledge scope, relationship to father)
- [ ] Write first drafts of all five journal pages (what the son actually reads)
- [ ] Define Artifact Cards — one per town (object, 2-sentence description, visual style reference)
- [ ] Assign level format to each town (Passenger as Guide / Player as Tour Guide / Special)
- [ ] Define repair puzzle types needed across five towns (sequencing, conditionals, loops, etc.)
- [ ] Define town puzzle concepts for Tigbauan, Guimbal, San Joaquin *(Oton and Miagao are drafted)*
- [ ] Document scoring formula (repair puzzle + town puzzle + dialogue weights)

### Tech Decisions
- [x] Choose Unity LTS version and confirm 2D URP or built-in pipeline
- [x] Decide Code Mode scripting language (Lua / Python subset / custom)
- [x] Decide AI provider for dialogue and chatbot; prototype Unity integration
- [x] Confirm online vs. offline stance for AI features
- [x] Set up version control (Git repo, branching strategy, .gitignore for Unity)

### Art & Audio Direction
- [ ] Define visual style: color palette, character art style, background treatment
- [ ] Reference board for jeepney design (base + customization categories)
- [ ] Define audio direction: music genre, SFX tone, passenger voice style
- [ ] List all asset categories needed (backgrounds, UI, characters, puzzles, artifacts)

### Production
- [ ] Set up task tracking
- [ ] Establish sprint cadence
- [ ] Create folder structure in Unity project

---

## Phase 1 — Foundation

> Goal: A bootable Unity project with scene navigation, a working save system, and a UI framework everything else can sit inside.

### Project Setup
- [ ] Initialize Unity project with correct settings (2D, target platforms, render pipeline)
- [ ] Set up scene structure: Main Menu, Game (Drive + Town), Almanac, Gacha, Settings
- [ ] Implement scene transition system (fade in/out, loading screen)
- [ ] Implement persistent GameManager (carries state across scenes)

### Save System
- [ ] Define save data schema (current town, journal pages collected, inventory, best scores, settings)
- [ ] Implement local save/load (JSON or Unity's serialization — TBD)
- [ ] Auto-save on town completion
- [ ] Manual save slot or single-file? *(TBD — mark as open)*

### Settings Screen
- [ ] Repair Puzzle Mode toggle (Block / Code) — persisted to save
- [ ] Dialogue Speed selector
- [ ] Subtitles toggle
- [ ] Volume sliders (Music, SFX)
- [ ] Settings applied globally at runtime

### Main Menu
- [ ] New Game / Continue / Settings / Quit
- [ ] Basic placeholder art (can be swapped later)

### UI Framework
- [ ] Define and implement UI style guide (fonts, button styles, panel colors)
- [ ] Reusable dialog box component (used throughout)
- [ ] Reusable notification/toast component

---

## Phase 2 — Drive System

> Goal: A drivable coastal segment with a talking passenger, breakdown triggers, and a placeholder repair puzzle hook.

### Background & Scroll
- [ ] Implement parallax scrolling background (sky, mountains, coast, road layers)
- [ ] Placeholder background art for one drive segment (Iloilo City → Oton)
- [ ] Jeepney sprite on road (base model, no customization yet)
- [ ] Jeepney driving animation (idle jiggle, speed variation)

### Passenger Framework
- [ ] Passenger data model (character profile, dialogue intent map, portrait)
- [ ] Passenger portrait + dialogue box UI component
- [ ] Dialogue intent selection UI (player picks a general topic, not exact words)
- [ ] Scripted dialogue fallback system (static lines if AI is unavailable)
- [ ] Hook for AI dialogue (stubbed — connected in Phase 5)

### Breakdown System
- [ ] Breakdown event data model (type, trigger condition, puzzle reference)
- [ ] Scripted breakdown trigger points per drive segment
- [ ] Visual/audio warning cue (dashboard flicker, audio chug) before breakdown fires
- [ ] Breakdown interrupts drive, launches puzzle scene, returns to drive on completion
- [ ] Breakdown frequency scales per town (harder in later towns)

### Drive Completion
- [ ] Drive ends → arrival cutscene → town scene loads
- [ ] Currency earned during drive is tallied and passed to progression system

---

## Phase 3 — Repair Puzzle Engine

> Goal: A fully functional repair puzzle system in both Block Mode and Code Mode, with scoring and analytics output.

### Shared Puzzle Core
- [ ] Puzzle data model: problem type, correct solution, optimal solution, available elements
- [ ] Puzzle state machine: Idle → Active → Running → Result
- [ ] Soft timer: counts down, dents score on expiry, does not end run
- [ ] Solution validator: checks player solution against correct solution
- [ ] Score calculator: factors in time, steps used vs. optimal, retries
- [ ] "Replay this puzzle" flow

### Block Mode (Easy)
- [ ] Block types: action block, condition block, loop block (start with minimum viable set)
- [ ] Drag-and-drop block canvas
- [ ] Block snapping and sequence reordering
- [ ] Run sequence button → animated playback
- [ ] Visual feedback for correct/incorrect steps
- [ ] Error messages (plain English, no syntax terms)

### Code Mode (Hard)
- [ ] Text input field with syntax highlighting (basic — TBD on library)
- [ ] Parser for chosen scripting language
- [ ] Runtime executor (sandboxed — runs player code safely)
- [ ] Readable error messages for syntax and logic failures
- [ ] Currency multiplier applied vs. Block Mode score

### Analytics Screen (stub)
- [ ] Layout: player solution vs. optimal solution, side-by-side
- [ ] Score, time, currency earned display
- [ ] Full AI-powered breakdown connected in Phase 5

### Puzzle Library — Phase 3 Scope
- [ ] Implement puzzle types: sequencing (required), conditionals (required), loops (if time permits)
- [ ] Build enough puzzles for Oton drive segment (2–3 puzzles)

---

## Phase 4 — Town 1: Oton

> Goal: A complete, playable end-to-end run through the first town. This is the vertical slice — everything else is built on this proof of concept.

### Passenger: Oton Leg
- [ ] Write and implement Oton passenger (character, scripted fallback dialogue, knowledge scope)
- [ ] Integrate with passenger framework from Phase 2
- [ ] Drive segment Iloilo City → Oton: background art, breakdowns, dialogue triggers

### Town Scene: Oton
- [ ] Oton town scene layout (street view, puzzle area, NPC placement)
- [ ] Puzzle implementation: Oton Gold Mask assembly
  - [ ] Fragment sprites + interaction
  - [ ] Assembly validation logic
  - [ ] Hint system (3 levels of hints before showing solution)
  - [ ] Heritage summary on completion
- [ ] Passenger assists during puzzle (scripted role)

### Journal & Almanac (stub)
- [ ] Journal page 1 unlocked on town completion
- [ ] Almanac screen: shows unlocked pages, placeholder for chatbot
- [ ] Artifact Card 1 displayed on completion screen

### First Full Run
- [ ] Playtest: Main Menu → Drive → Oton Puzzle → Journal page → Almanac → Results
- [ ] Score, currency, and basic progression carried through correctly
- [ ] All Phase 3 puzzles integrated into Oton drive segment

---

## Phase 5 — AI Integration

> Goal: All three AI features functional and connected to real game content.

### 5.1 Dynamic Passenger Dialogue
- [ ] Finalize LLM provider integration in Unity
- [ ] Build prompt templates for each passenger (character voice + historical constraints)
- [ ] Ground-truth fact set per town provided to model (prevents hallucination)
- [ ] Runtime generation: player picks intent → AI generates response
- [ ] Fallback to scripted lines on failure / timeout
- [ ] Test all five passengers: tone consistency, historical accuracy, no spoilers for unvisited towns

### 5.2 Almanac Chatbot
- [ ] Connect chatbot to unlocked journal pages only (context window restricted by save state)
- [ ] Chatbot UI: chat interface inside Almanac screen
- [ ] Hint mode: contextual hints for current active puzzle
- [ ] Lore mode: answer questions about heritage and characters
- [ ] Tone guidelines enforced in system prompt
- [ ] Test: chatbot cannot reference a town the player has not visited

### 5.3 Post-Level Analytics (full)
- [ ] AI generates natural-language breakdown of player's solution vs. optimal
- [ ] Displayed on analytics screen (from Phase 3 stub)
- [ ] Separate prompts for Block Mode and Code Mode explanations
- [ ] Test: analytics are accurate and readable across all puzzle types built so far

---

## Phase 6 — Towns 2–5

> Goal: Build the remaining four towns. Each town follows the same structure as Oton (Phase 4) but with its own format, puzzle, passenger, and heritage content.

> **Prerequisite:** Level format per town confirmed in Phase 0.

### Per-Town Checklist (repeat for Tigbauan, Guimbal, Miagao, San Joaquin)
- [ ] Drive segment background art
- [ ] Breakdown puzzles for this leg (2–3 per town, escalating difficulty)
- [ ] Passenger: character, scripted fallback, knowledge scope, AI prompt template
- [ ] Town scene layout
- [ ] Town puzzle implementation
- [ ] Heritage summary and Artifact Card
- [ ] Journal page content and unlock
- [ ] End-of-game sequence (San Joaquin only): father's story resolved, player has full Almanac

### Town-Specific Notes

**Tigbauan**
- [ ] Town puzzle concept to be finalized in Phase 0
- [ ] Heritage: Spanish colonial settlement

**Guimbal**
- [ ] Town puzzle concept to be finalized in Phase 0
- [ ] Heritage: Coastal culture and local traditions

**Miagao**
- [ ] Town puzzle: restore a section of the Miagao Church facade (UNESCO site)
- [ ] Heritage: Folk-baroque carvings, fort-church history

**San Joaquin**
- [ ] Town puzzle concept to be finalized in Phase 0
- [ ] Heritage: Battle of San Joaquin facade relief
- [ ] Final journal page: explains why father scattered the pages
- [ ] Credits sequence

---

## Phase 7 — Gacha & Progression

> Goal: A working gacha system with real rewards that feed back into replayability.

### Currency System
- [ ] Finalize currency name *(TBD)*
- [ ] Implement currency ledger (earn, spend, display)
- [ ] Connect all earn sources: repair puzzles, town puzzles, dialogue score
- [ ] Code Mode multiplier applied correctly

### Gacha System
- [ ] Define full gacha pool: cosmetics, performance upgrades, vehicles
- [ ] Build gacha pull UI (pull animation, reveal screen)
- [ ] Implement pity system (guaranteed rare at N pulls — N TBD)
- [ ] Duplicate handling (convert to currency TBD)
- [ ] Inventory screen: shows owned items

### Jeepney Customization
- [ ] Customization data model (which slots exist: paint, lights, mudflaps, hood ornament, etc.)
- [ ] Apply cosmetics to jeepney sprite at runtime
- [ ] Customization preview screen in garage
- [ ] Unlocked vehicles selectable before starting a run

### Town Select / Replay
- [ ] Town select screen (unlocked after first full completion)
- [ ] Shows best score per town
- [ ] Replay a single leg or the full route

---

## Phase 8 — Level Variety Layer

> Goal: Implement the non-default level formats (Player as Tour Guide, Special Challenges) and assign them to the appropriate towns.

> **Prerequisite:** Level format assignments confirmed in Phase 0. Towns those formats apply to must be built in Phase 6.

### Player as Tour Guide Format
- [ ] Design tourist NPC group (behavior, dialogue triggers)
- [ ] Tour stop system: player walks tourist group to heritage sites in the correct order
- [ ] Explanation prompt at each stop: player selects correct heritage description
- [ ] Artifact received on arrival at final stop
- [ ] Score based on accuracy and order of explanations

### Special Challenge Format
- [ ] Define what "special challenge" means for each town it applies to *(TBD in Phase 0)*
- [ ] Implement per-town special challenge
- [ ] Unlock condition (high score gate, story trigger, or always available)

### Integration
- [ ] All level formats confirmed and playable in assigned towns
- [ ] Transitions between formats feel consistent with the rest of the game
- [ ] Analytics screen adapted to work with all level formats

---

## Phase 9 — Polish & QA

> Goal: The game feels finished. Audio is in. UI is consistent. Bugs are fixed. Players understand what to do.

### Audio
- [ ] Music: compose/source tracks for menu, each drive segment, each town, puzzle tension
- [ ] SFX: jeepney engine, breakdown warning, block snapping, UI interactions, town ambience
- [ ] Audio mixing pass (music vs. SFX vs. dialogue balance)

### UI & UX
- [ ] Full UI art pass (replace all placeholder panels, buttons, icons)
- [ ] Onboarding: first-time tutorial for drive, repair puzzles (both modes), and town puzzles
- [ ] Consistent typography and layout across all screens
- [ ] Loading screens with heritage facts or flavor text

### Playtesting
- [ ] Internal playtest: full run, note all friction points
- [ ] External playtest (5–10 players): track completion rate, confusion moments, fun moments
- [ ] Revise based on feedback
- [ ] Verify AI dialogue accuracy with heritage consultant if possible

### Bug Fixing
- [ ] All critical path bugs fixed (cannot complete a town, save corruption, crash on launch)
- [ ] Medium bugs fixed (wrong score displayed, gacha UI glitch, etc.)
- [ ] Low bugs triaged (fix, defer, or accept)

### Localization *(if in scope)*
- [ ] Filipino language strings
- [ ] QA localized build

---

## Phase 10 — Ship Prep

> Goal: Builds are out the door.

- [ ] Finalize minimum spec
- [ ] Windows build: packaged, tested on clean machine
- [ ] macOS build: packaged, notarized, tested on clean machine
- [ ] Store page assets (capsule art, screenshots, description) *(if applicable)*
- [ ] Submission checklist complete for target platform(s)
- [ ] Final version tagged in version control
- [ ] Post-ship bug monitoring plan in place

---

## Appendix: Dependency Map

```
Phase 0 ──► Phase 1 ──► Phase 2 ──► Phase 3 ──► Phase 4 ──► Phase 6
                                                     │
                                                     ▼
                                                Phase 5 (AI)
                                                     │
                                              Phase 6 (AI hooks)

Phase 4 ──► Phase 7 (progression loop needs one complete town)

Phase 6 ──► Phase 8 (level variety needs towns built first)

Phase 5, 7, 8 ──► Phase 9 (polish needs full content)

Phase 9 ──► Phase 10
```

---

*This document is a living draft. Expect tasks to be added, removed, and re-scoped as production progresses.*
