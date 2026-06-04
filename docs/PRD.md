# Lugarithm — Product Requirements Document

> **Status:** DRAFT
> **Version:** 0.1
> **Team:** Cyfer
> **Last Updated:** June 4, 2026

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
11. [Out of Scope (v1)](#11-out-of-scope-v1)
12. [Open Questions](#12-open-questions)

---

## 1. Product Overview

**Lugarithm** is a 2D narrative adventure/puzzle game built in Unity. The player drives a vintage jeepney down the historic coastal highway from Iloilo City to San Joaquin, stopping at five real towns to recover torn journal pages that hold their father's story — and the heritage of Panay.

The game functions as an interactive living museum: heritage is delivered through play, not menus. Repair puzzles teach computational thinking. Town puzzles are rooted in actual local history. An AI layer keeps dialogue fresh and provides post-level coaching.

### Core Pillars

| Pillar | Description |
|--------|-------------|
| **Heritage First** | Every mechanic connects to a real historical artifact, story, or place along the Iloilo coast |
| **Play to Learn** | History is the reward for playing well — it is never front-loaded or forced |
| **Replayability** | Score-chasing, gacha rewards, and AI-varied dialogue incentivize multiple runs |
| **Accessible Depth** | Block Mode lowers the floor; Code Mode raises the ceiling — same content, different interfaces |

---

## 2. Goals & Success Criteria

### Primary Goals
- Deliver a complete 3–5 hour narrative experience covering five historic towns
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
- Five towns: Oton, Tigbauan, Guimbal, Miagao, San Joaquin
- Drive segments between each town
- Repair puzzle system (Block Mode + Code Mode)
- One unique town puzzle per stop
- Sequential passenger system (one per town leg)
- AI dynamic dialogue, journal chatbot, post-level analytics
- Jeepney gacha customization system
- The Almanac (in-game journal/history compendium)

### Out of Scope (v1)
- See [Section 11](#11-out-of-scope-v1)

---

## 5. Core Systems

### 5.1 Drive System

**Description:** The drive between towns plays as a scrolling 2D side-view. The jeepney moves automatically while the passenger delivers dialogue. The player's active role is monitoring and responding to breakdowns.

**Requirements:**
- Seamless background scroll representing the Iloilo coastal highway
- Passenger dialogue plays during the drive (see [Section 7.1](#71-dynamic-passenger-dialogue))
- Breakdowns trigger at scripted and semi-random intervals
- Drive length scales with the distance between towns
- Visual and audio cues signal incoming breakdowns before they occur
- Drive can be replayed from town select screen

---

### 5.2 Repair Puzzle System

**Description:** When the jeepney breaks down mid-drive, a repair puzzle interrupts the scene. The player fixes the problem by constructing a solution. Two modes exist and are toggled globally in Settings.

#### Block Mode (Easy)
- Visual drag-and-drop blocks representing instruction steps
- Blocks snap into a sequence; player runs the sequence to test it
- Incorrect sequences display feedback and allow retry
- No syntax — purely logical ordering

#### Code Mode (Hard)
- Text input with a lightweight syntax (TBD — likely Lua or pseudocode subset)
- Player types out the repair logic directly
- Same underlying problems as Block Mode, different interface
- Syntax errors are caught with readable feedback
- Higher currency reward multiplier than Block Mode

**Shared Requirements:**
- Soft timer — expiry reduces earnings, does not end the run
- Difficulty of puzzles escalates across the five towns
- Post-puzzle analytics screen (see [Section 7.3](#73-post-level-analytics))
- Puzzle types cover at least: sequencing, conditionals, simple loops *(scope TBD)*

---

### 5.3 Town Puzzle System

**Description:** On arriving at each town, the player disembarks and solves a puzzle directly tied to that town's heritage. Puzzles vary in format across towns.

**Requirements:**
- Each town has exactly one primary puzzle (additional challenges TBD)
- Puzzle content is grounded in verifiable local history
- Solving the puzzle unlocks the corresponding journal page
- The passenger who rode with the player plays a role in the puzzle (guide, hint-giver, or active assistant)
- Players receive a cultural summary/artifact card upon completion

**Town Puzzle Concepts** *(Draft — subject to revision)*

| Town | Puzzle Concept | Heritage Anchor |
|------|---------------|-----------------|
| Oton | Assemble the Oton Gold Mask fragments in correct order | Pre-colonial gold-working & burial customs |
| Tigbauan | TBD | Spanish colonial settlement |
| Guimbal | TBD | Coastal culture & local traditions |
| Miagao | Restore a section of the Miagao Church facade | UNESCO Church, folk-baroque carvings |
| San Joaquin | TBD | Battle of San Joaquin facade relief |

---

### 5.4 Passenger System

**Description:** One passenger is assigned per town leg. They board at the start of a drive segment and ride until the player arrives at their destination town. Passengers knew the father personally and hold information about the next town.

**Requirements:**
- Each passenger has a defined personality, backstory, and knowledge set
- Passengers deliver historical exposition naturally through conversation, not monologue dumps
- Player selects dialogue intents (not exact lines) during conversations
- Passengers actively assist in solving the upcoming town's puzzle
- Dialogue is AI-generated at runtime within defined character constraints (see [Section 7.1](#71-dynamic-passenger-dialogue))
- Passenger roster: five total (one per leg), designed in pre-production

---

## 6. Level Design Requirements

### 6.1 Level Variety

Levels must not follow an identical structure. Each town introduces a different primary mode of experiencing its heritage.

| Format | Description | Planned Town(s) |
|--------|-------------|-----------------|
| **Passenger as Guide** | Passenger delivers history during drive; assists with puzzle on arrival | Town 1 (Oton) |
| **Player as Tour Guide** | Player leads tourists through the town, explaining heritage stops; receives artifact + context on arrival | Town 2 or Special Level |
| **Special / Challenge** | Unique format unlocked via high score or story trigger — TBD | TBD |

> **Note:** Exact assignment of formats to towns is TBD. The goal is that no two consecutive towns feel structurally identical.

### 6.2 Artifact Cards

- Every town completion awards an **Artifact Card**: a stylized visual of the heritage object or site with a short written explanation
- Cards are stored in the Almanac
- Card design should feel like a collectible, not a textbook page

### 6.3 Scoring

- Score per leg is based on: repair puzzle efficiency, town puzzle accuracy, and optional dialogue choices
- Score feeds directly into currency earned
- Score breakdowns are shown on the post-level analytics screen

---

## 7. AI Features

### 7.1 Dynamic Passenger Dialogue

**Description:** Passenger conversations are AI-generated at runtime rather than pulled from a fixed script.

**Requirements:**
- Each passenger has a defined character profile: personality, speech patterns, knowledge scope, relationship to the father
- AI generates dialogue responses within those constraints
- Historical facts embedded in dialogue must be accurate — a curated fact set is provided to the model as a ground-truth source
- No two playthroughs should produce identical conversations
- Fallback to scripted lines if AI call fails or times out

**Open Questions:**
- Which LLM provider / Unity integration? *(TBD)*
- Online-only or cached for offline play? *(TBD)*

---

### 7.2 Almanac AI Assistant (Journal Chatbot)

**Description:** The Almanac is the in-game collection of recovered journal pages. It includes a built-in AI chatbot the player can query at any time.

**Requirements:**
- Chatbot only draws from journal pages the player has already unlocked
- Responds to: factual questions about Panay heritage, hints for current puzzles, lore about characters
- Cannot spoil towns not yet visited
- Tonally consistent with the game — not clinical or encyclopedic
- Accessible from the pause menu and the Almanac screen

---

### 7.3 Post-Level Analytics

**Description:** After every repair puzzle and town puzzle, a summary screen shows the player how they did versus the optimal solution.

**Requirements:**
- Displays player's solution side-by-side with the optimal solution
- Highlights inefficiencies (extra steps, wrong order, missed shortcuts)
- Shows time taken, score earned, and currency reward
- In Code Mode: shows the cleanest valid code solution
- In Block Mode: shows the minimal block sequence
- Optional "Replay this puzzle" button from the screen

---

## 8. Progression & Gacha

### 8.1 Currency

- Single in-game currency *(name TBD)*
- Earned by: completing repair puzzles, solving town puzzles, scoring above threshold on dialogue choices
- Multipliers apply in Code Mode vs. Block Mode
- Cannot be purchased with real money *(draft stance — TBD)*

### 8.2 Gacha System

- Players spend currency to pull from a gacha pool
- **Pull categories:**
  - Aesthetic customizations: neon lights, mudflaps, stainless steel horses, paint jobs
  - Performance upgrades: engine tolerance, breakdown frequency reduction
  - Unlockable vehicles: alternative rides usable on replay runs
- Pity system: guaranteed rare pull after N consecutive non-rare pulls *(N = TBD)*
- No duplicate pulls for unique items — excess converts to currency *(TBD)*

### 8.3 Replay Incentive

- Town select screen available after completing the game
- Each town shows your best score and current completion status
- Score targets give players a clear goal on replays

---

## 9. Settings & Accessibility

| Setting | Options | Default |
|---------|---------|---------|
| **Repair Puzzle Mode** | Block Mode / Code Mode | Block Mode |
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
| **Target Platforms** | PC — Windows & macOS |
| **Minimum Spec** | TBD |
| **AI Integration** | Runtime LLM calls built into Unity (provider TBD) |
| **Save System** | Local save file; auto-save after each town completion |
| **Build Pipeline** | TBD |

---

## 11. Out of Scope (v1)

- Mobile platform (iOS / Android)
- Multiplayer or co-op
- Voiced dialogue
- Additional routes beyond Iloilo City → San Joaquin
- Real-money purchases
- Mod support
- Console ports

---

## 12. Open Questions

These items need decisions before or during production:

| # | Question | Owner | Priority |
|---|----------|-------|----------|
| 1 | Which LLM provider for AI dialogue & chatbot? (OpenAI, Anthropic, local model?) | TBD | High |
| 2 | Online-required or offline-capable for AI features? | TBD | High |
| 3 | What scripting language for Code Mode? (Lua, Python subset, custom?) | TBD | High |
| 4 | Exact level format assignment per town | Design | Medium |
| 5 | Currency name and gacha pity threshold | Design | Medium |
| 6 | Filipino language localization — is it in v1 scope? | TBD | Medium |
| 7 | Are real-money purchases ever on the table? | TBD | Low |
| 8 | Voiced dialogue — out of scope now, but planned for later? | TBD | Low |
