# Lugarithm

**A Heritage Jeepney Road Trip**

> *"Drive the coast. Recover the pages. Learn the history."*

| | |
|---|---|
| **Genre** | 2D Narrative Adventure / Puzzle |
| **Engine** | Unity (2D, Isometric) |
| **Platform** | PC (Windows / macOS) |
| **Team** | Cyfer |

---

## Story

You play as a young man who has just lost his father — someone he barely knew. His father left behind two things: a vintage jeepney kept in perfect running order, and a thick journal titled *Ang Aking Mga Ugat* ("My Roots"), meant to explain the family's history. When he finally opens it, every page has been torn out (*gisi*). A note inside says the pages were given away to old friends in towns along the coast, one per town, so the son would have to go meet them himself.

So he drives.

The game follows a single road trip down the old coastal highway from **Iloilo City to San Joaquin**, stopping at five historic towns to find the person holding a page and earn it back. Each stop is a real place with real history. By the time the player reaches the final town, they've absorbed enough to give the tour themselves — which is the whole point.

---

## Towns & Heritage

| Level | Town | Heritage Focus |
|-------|------|----------------|
| Tutorial | Intro Segment | Basic sequencing and navigation — before the journey begins |
| 1 | **Iloilo City (Molo)** | American-era architecture, textile trade history, the "feminist church" of Molo (nave lined with 16 statues of female saints), Fort San Pedro |
| 2 | **Oton** | Pre-colonial gold-working & burial customs — the Oton Gold Mask (13g hammered gold, eyepiece + nose piece), Katagman Festival, maritime trade with China via the Batiano River |
| 3 | **Tigbauan** | Hablon handloom weaving (*habol* = to weave; piña, cotton, jusi fibers), WWII guerrilla resistance — Panay Landing Memorial, First Ambush Marker, Bantayan Watch Tower |
| 4 | **Miag-ao** | Miag-ao Church (UNESCO World Heritage, completed 1797) — coconut tree as the tree of life on the facade, St. Christopher depicted in local clothing, folk-baroque fusion, defensive fort-church origin |
| 5 | **San Joaquin** | *Rendicion de Tetuan* bas-relief (Battle of Tetouan, 1861), Campo Santo Spanish-era baroque cemetery (National Cultural Treasure 2015), Augustinian convent ruins |

> **Guimbal** (*Taytay Tigre* bridge, coral stone architecture, the "little Luneta of southern Iloilo") appears as a scenic drive-through with heritage dialogue — not a dedicated puzzle town in v1.

The entire coastal route acts as an **interactive living museum** — heritage is never delivered on a menu screen. You arrive in a town, step out of the jeepney, and handle what's in front of you.

---

## Core Gameplay

### Two Ways to Drive

The game has two distinct gameplay modes, toggled any time in Settings. Both modes cover the same route and heritage — the setting changes how you interact with it.

#### Manual Mode
Drive the jeepney in real-time. WASD controls on a continuous 2.5D isometric road with drift physics and velocity momentum. While steering, you also manage passenger boarding (tracking seats and drop-off flags) and collect fares using an interactive Coin Drawer on the dashboard — selecting denominations before a satisfaction timer empties. When the engine overheats or a belt snaps, a rapid non-code minigame interrupts the drive.

#### Automation Mode
Step out of the driver's seat and into a programming workspace. The route maps to a discrete, node-based isometric tilemap grid on the left (~40% of the screen). The right side (~60%) is your code canvas — choose between drag-and-drop blocks (no syntax, Scratch-style) or a lightweight text editor (Python/pseudocode). Write logic to automate movement, fare calculation, and passenger routing; execution plays out step-by-step on the grid. An execution control bar lets you run, pause, reset, or speed up playback (1×, 2×, 5×).

### Coding Concepts Per Town

| Level | Town | Programming Focus |
|-------|------|-------------------|
| Tutorial | Intro | Linear sequencing (`moveForward`, `turnLeft`, `turnRight`) |
| 1 | Iloilo City (Molo) | Conditionals (`while !atDestination`, `if frontIsClear`) |
| 2 | Oton | List indexing & array sorting |
| 3 | Tigbauan | Function parameters & iterative counters |
| 4 | Miag-ao | Multiple tracking variables & nested conditionals |
| 5 | San Joaquin | Multi-variable constraints (fuel, arrays, safety paths) |

The difficulty curve is intentional: early legs use one concept in isolation, later legs layer them together, and post-game levels randomly recombine the full concept pool.

### Town Puzzles
Each town has a unique puzzle tied to its actual heritage — assembling the Oton Gold Mask, reconstructing a Hablon weave pattern, restoring the Miag-ao Church facade, navigating to the Campo Santo cemetery. Solving these earns a **journal page** and advances the story. You can't truly "lose" — mistakes only reduce your earnings at the end of a leg. A first run takes around **3–5 hours**.

---

## The Journal — *Ang Aking Mga Ugat*

The journal is the heart of the game. It has two sections that unlock as you progress:

| Section | Content | Unlocks |
|---------|---------|---------|
| Heritage Pages | Father's recovered writing about each town's history and the family story | After completing that town's puzzles |
| Coding Reference / Guide | Plain explanation of each programming concept + annotated code example | After the concept is first introduced in gameplay |

Each recovered page adds a piece of a single connected story — from pre-colonial gold-working and burial customs through the Spanish colonial churches that doubled as forts, to the local craftsmen who worked their own marks into stone. The final town closes the personal side: why the father scattered the pages in the first place.

---

## Level Variety

Levels aren't all structured the same way:

| Level Type | Description |
|---|---|
| **Passenger as Guide** | A local passenger rides with you, shares stories during the drive, and assists with the upcoming town's puzzle. History arrives through conversation. |
| **You as Tour Guide** | You lead tourists through the town yourself, explaining its heritage at each stop. Arriving at a landmark rewards you with an artifact and its cultural context. |
| **Special Challenges** | Unique puzzle formats and heritage encounters unlocked through progression or high scores. |

---

## Passengers

Each leg introduces a specific passenger who knew the father personally. They board at the start of a drive and ride to the next town — sharing stories and actively helping solve the upcoming puzzle. Their presence is what makes the history feel **inherited rather than assigned**.

---

## AI Features

Five AI systems are built into the game:

### 1. Context-Aware Procedural Generator
Heritage artifacts and hidden collectibles are placed on the map based on your skill and playstyle — near the main path for players who are struggling, tucked in harder zones for completionists. The AI adapts to how you play, not just rolls dice.

### 2. Living Story Engine — Dynamic Passenger Dialogue
Passengers don't repeat the same lines. Their responses are freshly generated each playthrough within a strict Lore Book of verified heritage facts — so conversations feel alive on replay without hallucinating history.

### 3. Virtual Coding Mentor — Post-Level Analytics
After every puzzle, a breakdown screen shows your solution side-by-side with the optimal one. Hoverable tooltips explain *why* the optimal version uses fewer steps or less memory. Like a coach reviewing the ideal play after the match.

### 4. Heritage Oracle — Almanac AI Assistant
The **Almanac** is where recovered journal pages live. Its built-in AI chatbot (powered by RAG) lets you ask questions about unlocked history, request contextual coding hints, and look up programming concepts from your Coding Reference — without spoiling towns you haven't visited yet.

> If you ask about a town you haven't reached: *"My records on that region are currently corrupted or locked. Explore further to help me recover those files."*

### 5. Co-Pilot & Vibe Coding — Hints + Autopilot

**Co-Pilot** is a three-tier hint system that teaches without giving away the answer:
- 1st request: a nudge pointing out a logical flaw
- 2nd request: an explanation of the concept you should use
- 3rd request: pseudocode to guide you to the finish line

**Vibe Coding / Autopilot** flips the game from writing code to managing systems. Type a plain-language intent — *"Stop at every flag and collect the fare"* — and the AI compiles it into executable block code. Designed for players who want story immersion without line-by-line syntax.

---

## Progression & Gacha

- **In-game currency** is earned by completing levels efficiently, solving puzzles, and learning history accurately.
- Spend currency on **Gacha Pulls** to unlock heritage-themed rewards:

| Town | Badge | Jeepney Cosmetic |
|------|-------|-----------------|
| Iloilo City (Molo) | Anak ng Molo | Floral side panel decorations |
| Oton | Panday ng Dagat | Anchor decal |
| Tigbauan | Mangangukit | Decorative coral-stone trim |
| Miag-ao | UNESCO Keeper | Golden sandstone paint |
| San Joaquin | Tagapagtanggol | Full battle-scene mural |

- Performance upgrades and entirely new vehicles are also in the gacha pool.
- Replay towns to improve your score and earn more pulls.

---

## Technical Stack

- **Engine:** Unity (2D, isometric)
- **UI System:** Unity UI Screen Space – Overlay for the Automation Mode code workspace
- **AI Integration:** Runtime LLM calls for dialogue, hints, and analytics; RAG pipeline for the Almanac chatbot; procedural generator for artifact spawning
- **Target Platforms:** PC — Windows & macOS