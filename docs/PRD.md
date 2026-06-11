# Lugarithm — Product Requirements Document

> **Status:** DRAFT
> **Version:** 0.2
> **Team:** Cyfer
> **Last Updated:** June 11, 2026

---

## Table of Contents

1. [Product Overview](#1-product-overview)
2. [Goals & Success Criteria](#2-goals--success-criteria)
3. [Target Audience](#3-target-audience)
4. [Scope](#4-scope)
5. [Core Systems](#5-core-systems)
6. [Level Design Requirements](#6-level-design-requirements)
7. [AI Features](#7-ai-features)
8. [Progression & Gacha](#8-progression--gacha)
9. [Settings & Accessibility](#9-settings--accessibility)
10. [Technical Requirements](#10-technical-requirements)
11. [Narrative & Cutscenes](#11-narrative--cutscenes)
12. [Out of Scope (v1)](#12-out-of-scope-v1)
13. [Open Questions](#13-open-questions)

---

## 1. Product Overview

**Lugarithm** is a 2D narrative adventure/puzzle game built in Unity. The player drives a vintage jeepney down the historic coastal highway from Iloilo City to San Joaquin, stopping at five real towns to recover torn journal pages that hold their father's story — and the heritage of Panay.

The game functions as an interactive living museum: heritage is delivered through play, not menus. Two distinct gameplay modes teach computational thinking at different depths. Town puzzles are rooted in actual local history. Five AI systems keep dialogue fresh, support player learning, and provide post-level coaching.

### Core Pillars

| Pillar | Description |
|--------|-------------|
| **Heritage First** | Every mechanic connects to a real historical artifact, story, or place along the Iloilo coast |
| **Play to Learn** | History is the reward for playing well — it is never front-loaded or forced |
| **Replayability** | Score-chasing, gacha rewards, and AI-varied dialogue incentivize multiple runs |
| **Accessible Depth** | Manual Mode lowers the floor; Automation Mode raises the ceiling — same content, different interfaces |

---

## 2. Goals & Success Criteria

### Primary Goals
- Deliver a complete 3–5 hour narrative experience covering five historic towns plus a tutorial
- Teach players measurable knowledge of Panay coastal heritage through gameplay
- Establish a replayable loop through scoring, gacha, and AI-varied content

### Draft Success Metrics *(TBD — subject to change)*
- [ ] Player completes all five towns in a single session (first run)
- [ ] Players can correctly identify at least 3 heritage facts per town after completing it
- [ ] Average session length on replay runs > 45 minutes
- [ ] Gacha pull rate drives at least 2 additional replays per player
- [ ] AI dialogue is rated "feels natural" in playtesting

---

## 3. Target Audience

| Segment | Notes |
|---------|-------|
| **Primary** | Filipino students & young adults (ages 13–25) interested in local history or games |
| **Secondary** | Educators looking for heritage-adjacent tools; casual puzzle/narrative game players |
| **Stretch** | International players curious about Philippine culture and history |

---

## 4. Scope

### In Scope (v1)

**Route:** Tutorial Intro → Iloilo City (Molo) → Oton → Tigbauan → Miag-ao → San Joaquin

> Guimbal (*Taytay Tigre* bridge, coral stone architecture) appears as a scenic drive-through segment with heritage dialogue but is not a dedicated puzzle town in v1.

- Tutorial segment (pre-Iloilo City intro)
- Five full town levels: Iloilo City (Molo), Oton, Tigbauan, Miag-ao, San Joaquin
- Drive segments between each town in both gameplay modes
- **Manual Mode:** real-time driving with WASD, drift physics, and fare collection
- **Automation Mode:** code-based puzzle engine with Block Interface + Text Editor
- Unique repair/maintenance minigames per mode
- One unique town puzzle per stop, escalating in coding concept complexity
- Sequential passenger system (one per town leg)
- Five AI systems: procedural spawning, dynamic dialogue, coding mentor, heritage oracle, co-pilot & vibe coding
- Jeepney gacha customization system (heritage-themed cosmetics + badges per town)
- The Almanac — in-game journal with Heritage Pages, Coding Reference, and AI assistant
- Full cutscene suite (CS-00 through CS-08)

### Out of Scope (v1)
- See [Section 12](#12-out-of-scope-v1)

---

## 5. Core Systems

### 5.1 Drive System — Shared Requirements

**Description:** The drive between towns plays as a scrolling 2D/isometric scene. Both gameplay modes cover the same route content. The player chooses their mode via Settings; it applies at the start of each leg.

**Shared Requirements:**
- Background represents the Iloilo coastal highway with parallax layers (sky, mountains, coast, road)
- Passenger dialogue plays during the drive (see [Section 5.6](#56-passenger-system))
- Breakdowns trigger at scripted and semi-random intervals
- Drive length scales with the distance between towns
- Visual and audio cues signal incoming breakdowns before they fire
- Drive can be replayed from the town select screen

---

### 5.2 Manual Gameplay Mode

**Description:** The player manages real-time control of the jeepney on a seamless, continuous 2.5D isometric world. Driving requires active steering under low-friction conditions (drift physics). The player simultaneously handles passenger boarding and fare collection from the jeepney's dashboard.

**Requirements:**
- **Driving Controls:** WASD / Arrow keys with velocity momentum retention and drift physics simulating slippery road dynamics
- **Passenger Flow:** Player tracks entry flags, manages seat capacity, and monitors boarding thresholds at each stop
- **Fare Collection:** Cash fare tokens appear in an interactive Coin Drawer on the HUD; player selects denomination combinations to dispense correct change before the passenger's patience timer empties
- **HUD components:**
  - Analog-style dial speedometer
  - Oscillating fuel tank meter
  - Digital currency counter
  - Passenger Status Ribbon (top left): current count, drop-off zone flags, real-time satisfaction countdown
  - Interactive Coin Drawer: grid panel for currency token combinations
- Maintenance events (engine overheat, belt snap, empty fuel) interrupt the drive and trigger a minigame (see [Section 5.4](#54-repair--maintenance-minigames))

---

### 5.3 Automation Mode

**Description:** Direct physics control is deactivated. The route maps onto a discrete, node-based isometric tilemap grid. The player writes logic to automate the jeepney's movement, fare calculation, and passenger routing — using either drag-and-drop blocks or a text editor.

**Screen Layout:**
- Left ~40% of screen: game sandbox (discrete tilemap grid, step-by-step movement)
- Right ~60% of screen: code/block workspace panel (dual-tabbed: Block Interface or Text Editor)
- Top center ribbon: Execution Control Bar (Run, Pause, Reset, Playback Speed: 1×, 2×, 5×)
- Bottom right: Real-time variable monitor + console (execution state, tile coordinates, index limits, debug tips)

#### Block Interface *(Default)*
- Drag-and-drop blocks snap into a vertical sequence (Scratch-style)
- Command blocks: `moveForward()`, `turnLeft()`, `turnRight()`, conditionals (`if`/`while`), loops
- No syntax required — purely logical ordering
- Incorrect sequences highlight errors with plain-English feedback
- Cannot produce syntax errors by design

#### Text Editor *(Advanced Toggle)*
- Lightweight Python/pseudocode syntax window
- Same underlying problems as Block Interface
- Readable error output — no cryptic stack traces
- Higher currency reward multiplier than Block Interface

**Puzzle Concept Progression:**

| Level | Town | Primary Coding Concept |
|-------|------|------------------------|
| Tutorial | Intro | Linear sequencing (`moveForward`, `turnLeft`, `turnRight`) |
| 1 | Iloilo City (Molo) | Conditionals (`while !atDestination`, `if frontIsClear`) |
| 2 | Oton | List indexing & array sorting (cargo logs) |
| 3 | Tigbauan | Function parameters, iterative counters, pattern repetition (weaving) |
| 4 | Miag-ao | Multiple tracking variables, nested conditionals |
| 5 | San Joaquin | Multi-variable constraints (fuel caps, indexed arrays, safety paths) |

**Shared Automation Mode Requirements:**
- Soft timer — expiry reduces earnings, does not end the run
- Difficulty of concepts escalates across five towns
- Post-puzzle analytics screen (see [Section 7.3](#73-virtual-coding-mentor--post-level-analytics))

---

### 5.4 Repair / Maintenance Minigames

**Description:** When the jeepney breaks down mid-drive (engine overheat, belt snap, fuel empty), a minigame interrupts the segment. The minigame type depends on the active gameplay mode.

| Mode | Minigame Type |
|------|--------------|
| Manual Mode | Non-code: rapid pattern-matching or topological routing puzzle |
| Automation Mode | Code-based logic workspace (same pipeline as Automation Mode puzzles) |

**Requirements:**
- Soft timer on all minigames; expiry reduces earnings only — run continues
- Difficulty escalates per town
- Score output fed to the progression system
- Post-minigame analytics screen

---

### 5.5 Town Puzzle System

**Description:** On arriving at each town, the player disembarks and solves a puzzle directly tied to that town's heritage. Every puzzle follows a structured design template.

**Requirements:**
- Each town has exactly one primary puzzle (additional challenges TBD)
- Puzzle content is grounded in verifiable local history
- Solving the puzzle unlocks the journal page and Artifact Card for that town
- The passenger plays a role in the puzzle (guide, hint-giver, or active assistant)
- Heritage summary displayed on completion

**Puzzle Element Structure:**

| Element | Description |
|---------|-------------|
| Title | Heritage-themed name hinting at the coding concept (shown on Normal mode) |
| Context Brief | 1–2 sentences setting the scene in the story |
| Problem Statement | Variables given, goal stated clearly |
| Concept Label | Shown via hint button on Normal mode; hidden on Hard mode |
| Input | What the player is given to work with |
| Expected Output | What a correct solution produces |
| Codeblock Set | The specific blocks available for this puzzle |
| Code Equivalent | The solution expressed in text form (same logic) |
| Win Condition | What counts as correct |
| Fail State | Currency reduction penalty — not a block; run continues |
| Solution Reveal | Shown only when player manually requests it |
| Post-Level Review | AI compares player's solution to the optimal one |

**Town Puzzle Concepts:**

| Level | Town | Puzzle Concept | Heritage Anchor |
|-------|------|---------------|-----------------|
| Tutorial | Intro | Linear jeepney navigation + fare coin-matching | Basic sequencing & currency handling |
| 1 | Iloilo City (Molo) | Maze escape (conditional routing) + non-intersecting transit hub connections | American-era Molo district, textile heritage |
| 2 | Oton | Oton Gold Mask assembly + cargo manifest sorting (array indexing) | Pre-colonial gold-working, Katagman Festival, maritime trade |
| 3 | Tigbauan | Hablon weave pattern reconstruction (functions + iterative counters) | Traditional loom weaving, WWII guerrilla resistance |
| 4 | Miag-ao | Restore Miag-ao Church facade section (nested conditionals + variable tracking) | UNESCO Church, folk-baroque carvings, indigenous iconography |
| 5 | San Joaquin | Navigate to Campo Santo under multi-variable constraints | Battle of Tetouan bas-relief, Campo Santo cemetery |

---

### 5.6 Passenger System

**Description:** One passenger is assigned per town leg. They board at the start of a drive segment and ride to the destination. Passengers knew the father personally and carry knowledge of the next town's heritage.

**Requirements:**
- Each passenger has a defined personality, backstory, and knowledge scope
- Passengers deliver historical exposition naturally through conversation, not monologue dumps
- Player selects dialogue intents (not exact lines) during conversations
- Passengers actively assist in solving the upcoming town's puzzle
- Dialogue is AI-generated at runtime within defined character constraints (see [Section 7.2](#72-living-story-engine--dynamic-passenger-dialogue))
- Fallback to scripted lines if AI call fails or times out
- Passenger roster: five total (one per leg), defined in pre-production
- Passengers may not reference towns the player has not yet visited

---

## 6. Level Design Requirements

### 6.1 Difficulty Curve

The game teaches coding the same way good games teach anything: by making you need it. Each leg introduces one new concept in isolation; later legs combine previously learned concepts.

| Principle | Notes |
|-----------|-------|
| Early legs: one concept in isolation | Sequencing before conditionals |
| Later legs: concepts combine | Loops + conditionals used together |
| Post-game: randomized concept pool | Same systems, new configurations |
| Hard mode: hints and labels removed | Familiar mechanics become genuine problem-solving |

### 6.2 The Journal — *Ang Aking Mga Ugat*

The journal ("My Roots") is the heart of the game. It has two sections:

| Section | Content | Unlocks |
|---------|---------|---------|
| Heritage Pages | Father's recovered writing about each town's history | After completing that town's puzzles |
| Coding Reference / Guide | Plain explanation of each concept + annotated code example | After the concept is first introduced in gameplay |

**Coding Reference Entry Format:**

> **Concept Name:** *One sentence in plain English.*
>
> What it does: 2–3 sentences without assuming prior knowledge.
>
> Example: A short, annotated code snippet with inline comments.

No jargon unless it's immediately explained in the same entry.

### 6.3 Artifact Cards

- Every town completion awards an **Artifact Card**: a stylized collectible visual of the heritage object or site, with a short written explanation
- Cards are stored in the Almanac
- Card design should feel like a collectible, not a textbook page

### 6.4 Scoring

- Score per leg is based on: repair minigame efficiency + town puzzle accuracy + optional dialogue choices
- Score feeds directly into currency earned
- Score breakdowns shown on the post-level analytics screen

### 6.5 Level Variety

| Format | Description | Planned Town(s) |
|--------|-------------|-----------------|
| **Passenger as Guide** | Passenger delivers history during drive; assists with puzzle on arrival | Default format |
| **Player as Tour Guide** | Player leads tourists through town, explaining heritage at each stop | TBD from Phase 0 |
| **Special / Challenge** | Unique format unlocked via high score or story trigger | TBD from Phase 0 |

> No two consecutive towns should feel structurally identical. Exact format assignment per town is confirmed in Phase 0.

---

## 7. AI Features

### 7.1 Context-Aware Procedural Generator

**Description:** Heritage artifacts and hidden collectibles are placed procedurally, adapting to the player's demonstrated skill and playstyle — not flat random number generation.

**Requirements:**
- Tracks player pacing, accuracy records, and exploration patterns across all sessions
- For struggling players: spawns artifacts near primary pathways behind lighter puzzles
- For completionist players: places artifacts in mathematically complex, maze-locked zones with harder minigames
- Adapts dynamically without breaking narrative coherence or spoiling unseen towns

---

### 7.2 Living Story Engine — Dynamic Passenger Dialogue

**Description:** Passenger conversations are AI-generated at runtime using a hybrid system: the AI generates phrasing dynamically while operating within pre-defined narrative milestones and emotional beats. Purely open-ended generation is avoided to prevent hallucinated history.

**Requirements:**
- Each passenger has a character profile: personality, speech patterns, knowledge scope, relationship to the father
- A curated **Lore Book** of verified heritage facts per town is provided to the model as the authoritative source — all factual content must trace back to this
- AI tracks dialogue choices made, player actions, and a relationship score to produce contextually appropriate responses
- No two playthroughs should produce identical conversations
- Fallback to scripted lines if AI call fails or times out
- Passengers may not reference towns the player has not yet visited

---

### 7.3 Virtual Coding Mentor — Post-Level Analytics

**Description:** After every repair minigame and town puzzle, a summary screen shows the player how they performed versus the optimal solution, with an AI-generated explanation of the gap.

**Requirements:**
- **Efficiency Score:** Numerical rating of the player solution's performance
- **AI Peer Review:** Player's solution displayed side-by-side with an optimized AI version
- **Interactive Breakdown:** Hoverable tooltips explaining *why* the optimal version uses fewer steps or less memory
- In Text Editor mode: shows the cleanest valid code solution
- In Block Interface mode: shows the minimal block sequence
- Optional "Replay this puzzle" button from the screen
- Test: analytics are accurate and readable across all puzzle types

---

### 7.4 Heritage Oracle — Almanac AI Assistant

**Description:** The Almanac includes an AI chatbot powered by Retrieval-Augmented Generation (RAG). It draws from a dual-indexed database: Panay heritage facts and in-game coding guide entries.

**Requirements:**
- Chatbot only draws from journal pages the player has already unlocked (Heritage Pages + Coding Reference)
- **Heritage & Spoiler Gating:** If asked about a town not yet visited, the assistant withholds the answer in-world: *"My records on that region are currently corrupted or locked. Explore further to help me recover those files."*
- **Interactive Programming Companion:** Also answers concept questions (arrays, functions, loops) from unlocked Coding Reference entries — without giving away current puzzle solutions
- Accessible from the pause menu and the Almanac screen
- Tonally consistent with the game — not clinical or encyclopedic

---

### 7.5 Scaffolding Hints & Vibe Coding — Co-Pilot & Autopilot

**Description:** Two complementary AI assistance systems. The Co-Pilot serves players who want to learn; the Autopilot serves players who prioritize story immersion over coding challenge.

#### Co-Pilot (Progressive Hint System)

Three tiers, requested on-demand via a hint button in the puzzle UI:

| Tier | What the AI Does |
|------|-----------------|
| 1st request | Subtle nudge: points out a logical flaw in the player's current approach |
| 2nd request | Explains the specific concept or function they should be using |
| 3rd request | Provides pseudocode that guides them to the solution without giving it away |

#### Vibe Coding / Autopilot

Shifts the game from imperative coding to declarative prompt engineering:

- Player types a plain-language intent (e.g., *"Stop at every flag and collect the fare"*)
- AI compiles the declarative string into valid, executable block structures
- Compiled blocks play out on the tilemap grid
- Ambiguous or impossible inputs return plain-language feedback
- Designed for players who want story immersion without writing code line-by-line

---

## 8. Progression & Gacha

### 8.1 Currency

- Single in-game currency *(name TBD)*
- Earned by: completing repair minigames, solving town puzzles, scoring above threshold on dialogue choices
- Text Editor multiplier applied vs. Block Interface score
- Cannot be purchased with real money *(draft stance — TBD)*

### 8.2 Badge System

Each town completion awards a collectible badge displayed on the player profile and visible on the jeepney:

| Level | Town | Badge |
|-------|------|-------|
| 1 | Iloilo City (Molo) | Anak ng Molo |
| 2 | Oton | Panday ng Dagat |
| 3 | Tigbauan | Mangangukit |
| 4 | Miag-ao | UNESCO Keeper |
| 5 | San Joaquin | Tagapagtanggol |

### 8.3 Gacha System

Players spend currency to pull from a gacha pool.

**Pull categories:**

| Category | Contents |
|----------|----------|
| Heritage cosmetics (per town) | Floral side panel (Molo), Anchor decal (Oton), Coral-stone trim (Tigbauan), Golden sandstone paint (Miag-ao), Full battle-scene mural (San Joaquin) |
| General aesthetics | Neon lights, mudflaps, stainless steel horses, paint jobs |
| Performance upgrades | Engine tolerance, breakdown frequency reduction |
| Unlockable vehicles | Alternative rides usable on replay runs |

- Pity system: guaranteed rare pull after N consecutive non-rare pulls *(N = TBD)*
- No duplicate pulls for unique items — excess converts to currency *(TBD)*

### 8.4 Replay Incentive

- Town select screen available after completing the game
- Each town shows best score, badge earned, and completion status
- Score targets give players a clear goal on replays

---

## 9. Settings & Accessibility

| Setting | Options | Default |
|---------|---------|---------|
| **Gameplay Mode** | Manual Mode / Automation Mode | Manual Mode |
| **Automation Interface** | Block Interface / Text Editor | Block Interface |
| **Dialogue Speed** | Slow / Normal / Fast / Instant | Normal |
| **Subtitles** | On / Off | On |
| **Music Volume** | 0–100 | 80 |
| **SFX Volume** | 0–100 | 80 |
| **Language** | English / Filipino *(TBD)* | English |

> Additional accessibility options (colorblind mode, font size) to be scoped in a later draft.

---

## 10. Technical Requirements

| Requirement | Detail |
|------------|--------|
| **Engine** | Unity 2D (LTS version TBD) |
| **UI System** | Unity UI (Screen Space – Overlay) for Automation Mode workspace canvas |
| **Target Platforms** | PC — Windows & macOS |
| **Minimum Spec** | TBD |
| **AI Integration** | Runtime LLM calls for dialogue/hints; RAG pipeline for Almanac chatbot; procedural generator for artifact spawning (provider TBD) |
| **Save System** | Local save file; auto-save after each town completion |
| **Build Pipeline** | TBD |

---

## 11. Narrative & Cutscenes

### 11.1 Story Summary

The player is a young man who recently lost a father he barely knew. The father left behind two things: a vintage jeepney kept in perfect running order, and a thick journal titled *Ang Aking Mga Ugat* ("My Roots"), meant to explain the family's history. When opened, every page has been torn out. A note inside explains the pages were given to old friends in towns along the coast — one per town — so the son would have to go meet them himself.

So he drives.

### 11.2 Narrative Themes

Family heritage · Cultural identity · Historical appreciation · Generational legacy · Personal growth · Grief and healing · Filipino pride

### 11.3 Narrative Progression Structure

1. Father passes away
2. Journal is discovered
3. Journey begins (Tutorial)
4. Journal pages are recovered town by town
5. Family history is gradually revealed through each page
6. Father's final message uncovered (San Joaquin)
7. Protagonist understands their identity
8. Journey concludes with acceptance

### 11.4 Cutscene List

| ID | Name | Purpose |
|----|------|---------|
| CS-00 | Prologue — The Inheritance | Establish protagonist, father, and the jeepney as the symbol of inheritance |
| CS-01 | The Journal — Discovery in the Garage | Introduce journal mechanic and main objective |
| CS-02 | Departure — The Road Calls | Transition from intro to gameplay |
| CS-03 | Level 1 Arrival — Iloilo City (Molo) | Protagonist arrives at Molo Plaza; first journal page scattered |
| CS-03b | Level 1 Completion | Page recovered: grandmother's story; the role of women in the family's history |
| CS-04 | Level 2 Arrival — Oton | Elderly fisherman hints at the town's connection to the missing page |
| CS-04b | Level 2 Completion | Page recovered: great-grandfather built jeepneys by hand |
| CS-05 | Level 3 Arrival — Tigbauan | Young girl speaks about stories hidden within the stone carvings |
| CS-05b | Level 3 Completion | Page recovered: ancestor helped carve the church; meaningful work outlives the maker |
| CS-06 | Level 4 Arrival — Miag-ao | Elderly woman recognizes the journal and hints at the tradition behind it |
| CS-06b | Level 4 Completion | Page recovered: the family's deep connection to this region and its culture |
| CS-07 | Level 5 Arrival — San Joaquin | Protagonist enters the Campo Santo cemetery searching for the final page |
| CS-07b | Level 5 Completion | Father's final message: why the pages were scattered in the first place |
| CS-08 | Epilogue — Who You Are | Journey ends; jeepney carries the family's visual story; full Almanac unlocked |

### 11.5 Rewards Per Level

| Level | Town | Badge | Jeepney Cosmetic |
|-------|------|-------|-----------------|
| 1 | Iloilo City (Molo) | Anak ng Molo | Floral side panel decorations |
| 2 | Oton | Panday ng Dagat | Anchor decal |
| 3 | Tigbauan | Mangangukit | Decorative coral-stone trim |
| 4 | Miag-ao | UNESCO Keeper | Golden sandstone paint |
| 5 | San Joaquin | Tagapagtanggol | Full battle-scene mural |

---

## 12. Out of Scope (v1)

- Mobile platform (iOS / Android)
- Multiplayer or co-op
- Voiced dialogue
- Guimbal as a dedicated puzzle town *(drive-through with heritage dialogue only)*
- Additional routes beyond Iloilo City → San Joaquin
- Real-money purchases
- Mod support
- Console ports

---

## 13. Open Questions

| # | Question | Owner | Priority | Status |
|---|----------|-------|----------|--------|
| 1 | Which LLM provider for AI dialogue, chatbot, and analytics? | TBD | High | Open |
| 2 | Online-required or offline-capable for AI features? | TBD | High | Open |
| 3 | Scripting language for Text Editor mode (Python subset / custom pseudocode)? | TBD | High | Open |
| 4 | Exact level format assignment per town (Tour Guide / Special Challenge) | Design | Medium | Open |
| 5 | Currency name | Design | Medium | Open |
| 6 | Gacha pity threshold (N consecutive non-rare pulls) | Design | Medium | Open |
| 7 | Filipino language localization — in v1 scope? | TBD | Medium | Open |
| 8 | Guimbal: drive-through heritage dialogue only, or a lite stop with a light puzzle? | Design | Medium | Open |
| 9 | Voiced dialogue — planned for a post-v1 version? | TBD | Low | Open |
| 10 | Are real-money purchases ever on the table? | TBD | Low | Open |