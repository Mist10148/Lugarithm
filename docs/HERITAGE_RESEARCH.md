# Lugarithm — Heritage Research Dossier

> **Status:** DRAFT · **Version:** 1.1 (expanded) · **Team:** Cyfer · **Compiled:** June 13, 2026
>
> **Purpose:** A deep, sourced reference pool on the six real places along the
> Iloilo coastal route. This is the *raw material* the writers draw from when
> scripting each level's passenger dialogue, journal Heritage Pages, Artifact
> Cards, and minigame framing.
>
> **v1.1 changes:** Expanded every town with construction histories, named
> figures, dates, and architectural/archaeological detail. **All sources are
> non-Wikipedia** — drawn from the National Museum, UNESCO, the IIAS, government
> sites (province / municipality), Philippine journalism, and specialist heritage
> writing. See [References](#references).

---

## How to use this document

This dossier exists so the narrative team has **verified, citable history** in
one place before any dialogue is written. A few rules of the road:

1. **Not everything here goes into conversation.** Each town has far more
   material than any single drive can carry. Pick the warm, human, *tellable*
   facts for spoken lines; leave the rest as texture and as fuel for the
   Almanac / Heritage Oracle.
2. **Hold the best beat for the reveal.** Every level ends with a recovered
   journal page (and a minigame heritage summary). Deliberately *withhold* one
   strong fact per town so the completion screen lands with a payoff the player
   didn't already hear on the drive. These are flagged **🔒 Hold for the reveal**.
3. **Stay inside the Lore Book.** When the Living Story Engine (AI dialogue)
   generates passenger lines, the facts it may assert must trace back to entries
   like the ones here. Treat the **Key facts** lists as the authoritative set.
4. **Spoiler gating.** A passenger may not reference a town the player has not
   yet reached. Order: Tutorial (Iloilo City) → Molo → Oton → Tigbauan →
   *Guimbal (drive-through)* → Miag-ao → San Joaquin.

**Sourcing note.** Wikipedia is deliberately excluded per the team's request.
Popular sources still disagree on some Spanish-era dates and pre-colonial
figures; where they conflict, the discrepancy is flagged **in-line** rather than
silently resolved, so no contested claim is asserted as settled. Full URLs are
in [References](#references).

---

## The route at a glance

The game follows the old coastal road south from Iloilo City to San Joaquin.
That single stretch holds a clean cross-section of Panay's history — pre-colonial
gold and maritime trade, the Spanish church-building era, and World War II
resistance — which is exactly why it works as a route.

| Stop | Level | Heritage spine (beyond the church) | Coding concept anchor |
|------|-------|-----------------------------------|-----------------------|
| **Iloilo City (Molo)** | Tutorial + Level 1 | Textile-trade capital; American-era Calle Real; Fort San Pedro; the "women's church" | Sequencing → conditionals |
| **Oton** | Level 2 | The Oton Gold Death Mask; pre-colonial maritime trade; Katagman burial customs | List indexing / array sorting |
| **Tigbauan** | Level 3 | Hablon handloom weaving; the first school for boys (1592); WWII guerrilla resistance | Functions + loops (pattern repetition) |
| **Guimbal** | *Drive-through* | Taytay Tigre bridge; yellow coral-stone "agong" town; watchtower-belfry | — (scenic dialogue only) |
| **Miag-ao** | Level 4 | UNESCO church; the coconut "tree of life" facade; fortress-church architecture | Nested conditionals + tracking variables |
| **San Joaquin** | Level 5 | The *Rendición de Tetuán* battle facade; the Campo Santo cemetery | Multi-variable constraints |

> Guimbal is a scenic drive-through in v1 — no dedicated puzzle, but it still
> earns a few lines of passing heritage dialogue. Its research is included so the
> segment can be written with the same care as the puzzle towns.

---

# 1 · Iloilo City — Molo

> *Theme:* The textile capital and colonial crossroads — where the journey
> begins and the tone is set.
> *Game role:* Tutorial level + Level 1 ("The Alley Escape").

### Snapshot

| | |
|---|---|
| **Era touched** | Pre-colonial trade port → Spanish colonial → American era → present |
| **Signature site** | Molo Church (Santa Ana Parish) |
| **Beyond the church** | Calle Real heritage district; the textile-then-sugar port economy; Fort San Pedro; Dinagyang Festival; UNESCO Creative City of Gastronomy (2023) |
| **Mood** | Bustling, proud, urban; batchoy and pancit Molo in the air |

### Heritage deep-dive

**Molo Church — the "women's church."** Officially **Santa Ana Parish**, this
Gothic-Renaissance Revival church was **begun in 1831** under Fray Pablo Montaño
and **completed in 1888** under Fray Agapito Buenaflor, with construction
supervised by **Don José Manuel Locsin**. It is built of **white coral stone and
limestone**, bound — as cement was scarce — with mortar of **egg white and
sand**, and crowned with **twin pointed red spires**. It is popularly described
as the only Gothic-Renaissance-style church outside Metro Manila. **Dr. José
Rizal visited in 1886**, and the church served as a refuge during World War II;
it was declared a **national landmark in 1992**. [1][2][3]

Its interior holds, beside its patroness **St. Anne (Sta. Ana)**, life-sized
statues of **sixteen female saints** — Sta. Marcela, Apolonia, Genoveva, Isabel,
Felicia, Ines, Monica, Magdalena, Juliana, Lucia, Rosa de Lima, Teresa, Clara,
Cecilia, Margarita, and Marta — earning it the nickname the **women's** or
**feminist church.** [2][3]

> The perfect hook for a first journal page about **the women of the
> protagonist's family.** The "women's church" frames the grandmother's story
> without forcing a lecture.

**A textile capital that became a sugar port.** Iloilo was the country's textile
center, with reportedly **over 50,000 looms** in the province before the British
arrived. **Nicholas Loney** — the **first British vice-consul** in Iloilo,
appointed **11 July 1856** and serving until his **death from malaria in 1869** —
recorded the booming cloth trade he saw at town market fairs around **1857**. But
Loney then engineered a pivot: he **imported steam-powered mills on credit** to
Filipino planters and shifted labor and capital toward **sugar** on neighboring
Negros. By the **1880s** the weaving industry had lost its primacy; British
textile *imports* into Iloilo were already worth **$360,000–$480,000 a year** by
the 1860s. The cloth-to-sugar story is the economic spine of the whole route and
ties Molo forward to the weaving towns of Tigbauan and Miag-ao. [4][5]

**Layers of colonial city.** The old downtown's **Calle Real** (officially **J.M.
Basa Street**) is a heritage commercial strip lined with American-era
**neoclassical, beaux-arts, and art deco** buildings — a visible *second*
colonial layer atop the Spanish one. Nearby stood **Fort San Pedro** (*Fuerza de
Nuestra Señora del Rosario*), built in **1602 by Pedro Bravo de Acuña** to defend
against Moro and Dutch raids and **destroyed in World War II.** [6][7][8]

**Living culture.** The **Dinagyang Festival** began in **1967**, when a replica
of the **Santo Niño de Cebu** was brought to Iloilo's San José Parish and
received with Ati-Atihan-style street dancing; it has since become one of the
country's signature festivals. In **October 2023**, Iloilo City became the
**Philippines' first UNESCO Creative City of Gastronomy**, recognizing its living
food culture (batchoy, pancit Molo, and more). [6][9][10]

### Key facts (Lore Book set)

- Molo Church (Santa Ana Parish): begun 1831 (Montaño), completed 1888
  (Buenaflor, supervised by J.M. Locsin); white coral stone + egg-white/sand
  mortar; twin red spires; Rizal visited 1886; national landmark 1992. [1][3]
- Interior: all-female saints (16 named) + patroness St. Anne → "women's /
  feminist church." [2][3]
- Textile capital (>50,000 looms); Nicholas Loney, first British vice-consul
  (1856–1869), pivoted Iloilo from weaving to Negros sugar via steam mills. [4][5]
- Calle Real (J.M. Basa St): American-era neoclassical / beaux-arts / art deco
  heritage district. [6][7]
- Fort San Pedro: built 1602 by Pedro Bravo de Acuña; destroyed in WWII. [8]
- Dinagyang Festival: began 1967 (Santo Niño de Cebu replica + Ati-Atihan
  dancing). [6]
- Iloilo City: first UNESCO Creative City of Gastronomy in the Philippines
  (Oct 2023). [9][10]

### Gameplay & dialogue hooks

- **Tutorial framing:** the dispatcher/guide teaches controls while gesturing at
  "a goldmine of history" — Calle Real, the food-city pride.
- **Level 1 puzzle ("Alley Escape"):** Molo's tight old streets justify a
  wall-following maze; the non-code transit-hub connection puzzle maps onto the
  city's old route/terminal layout.

> **🔒 Hold for the reveal:** Save the **textile-economy depth** (50,000+ looms;
> the cloth-to-sugar pivot) for the completion page about the grandmother
> managing the family's trade logistics — let the *family's* place in the cloth
> economy be the surprise that connects personal history to city history.

---

# 2 · Oton

> *Theme:* The first pueblo and the Gold Death Mask — the most purely
> pre-colonial stop on the route.
> *Game role:* Level 2 ("The Market Cargo").

### Snapshot

| | |
|---|---|
| **Era touched** | Protohistoric / pre-colonial "Age of Trade" → early Spanish |
| **Signature object** | The Oton Gold Death Mask (a National Cultural Treasure) |
| **Beyond the church** | Katagman burial customs; pre-colonial trade with China; the Iloilo–Batiano river port |
| **Mood** | Salt air, old shipyards, artisans, deep antiquity |

### Heritage deep-dive

**The Oton Gold Death Mask.** The country's most famous gold death mask was found
**in situ on June 5, 1967**, in **Grave #6** at the **Mediavilla property,
Barangay San Antonio, Oton** — one of four areas systematically excavated by
**National Museum** anthropologist **Dr. Alfredo E. Evangelista** with **UP
Diliman**'s **Dr. F. Landa Jocano** (himself an Ilonggo). It is described as the
**first gold death mask systematically recovered by archaeologists** (the dig is
dated to the 1960s; some popular accounts loosely say 1973 — prefer the
documented **1967** find). [11]

The mask is **hammered gold foil** cut into **two leaf-shaped pieces** (covering
the eyes) **joined by a strip over the bridge of the nose**, with thin strips
that hung at the nostrils and **repoussé dot** decoration along the edges. By its
associated grave goods it is dated to the **late 14th–early 15th century** — the
"Age of Trade." (Popular sources round its weight to **~13 grams**; treat that as
an approximate figure, not a precise total.) It is now a **National Cultural
Treasure**, housed at the **National Museum – Western Visayas** in Iloilo. [11][13][14]

**Why a gold mask?** Early **Bisayans** believed that **gold coverings over the
eyes, nose, and mouth protected the dead** from evil spirits seeking to occupy
the body — the **brightness of gold was thought to drive evil spirits away.**
Gold was also a marker of **rank**: high-status persons were buried with as much
gold as possible — face covers, beads, jewelry, and prized ceramics. [12][13]

**A pre-colonial port.** The find site corresponds to **Katagman**, a
protohistoric **port settlement between the Iloilo and Batiano rivers** — one of
the **oldest and most important seaports of the late 1300s–early 1400s**, plugged
into the maritime trade network. (Chinese and Southeast Asian ceramics from Oton
attest to commercial and social ties with China and its neighbors *before* the
Spanish.) [11][12]

**The Katagman Festival.** Every **May**, Oton holds the **Katagman Festival**;
students wear colorful costumes with their **eyes and noses covered in
gold-colored paper**, commemorating the burial site where the mask was found. [15]

### Key facts (Lore Book set)

- Oton Gold Death Mask: hammered gold foil, two eye pieces joined over the nose,
  repoussé dots; found in situ June 5, 1967, Grave #6, San Antonio (Katagman);
  excavated by Evangelista (NM) & Jocano (UP); dated late 14th–early 15th c.;
  now a National Cultural Treasure at NM–Western Visayas. [11][13][14]
- Belief: gold's brightness repels evil spirits; gold coverings guard the dead;
  gold = social rank in burial. [12][13]
- Katagman: pre-colonial river port (Iloilo–Batiano), major seaport c. 1300s,
  trading with China; Chinese/SEA ceramics found locally. [11][12]
- Katagman Festival, every May; gold-paper eye/nose coverings. [15]

### Gameplay & dialogue hooks

- **Level 2 puzzle (cargo sorting / array indexing):** sorting cargo logs at
  Oton's market and stacking crates by weight — the market and old shipyard are
  real heritage texture.
- **Town puzzle (mask assembly):** assembling the mask's two pieces in correct
  order is a literal, tactile heritage minigame.

> **🔒 Hold for the reveal:** Keep the **belief behind the mask** (gold's
> brightness guards the dead; gold = status) for the puzzle's heritage summary or
> the journal page — let the *meaning* of the object be the reward for assembling
> it, not a fact pre-loaded on the drive. The family page (great-grandfather who
> forged the first jeepney frames) then rhymes with the *panday* tradition.

---

# 3 · Tigbauan

> *Theme:* Hablon weaving, the country's first school for boys, and the guerrilla
> town — heritage layers that have nothing to do with a church interior.
> *Game role:* Level 3 ("The Weaver's Pattern").

### Snapshot

| | |
|---|---|
| **Era touched** | Early Spanish (Jesuit mission) → 19th-c. textile boom → WWII → present |
| **Signature craft** | Hablon handloom weaving |
| **Beyond the church** | First school for boys in the Philippines (1592); WWII guerrilla resistance; Bantayan Watch Tower |
| **Signature site** | San Juan de Sahagun Church (a National Cultural Treasure facade) |
| **Mood** | Rhythmic looms, fiesta textiles, a quiet town with a sharper history |

### Heritage deep-dive

**The first school for boys (1592).** The Jesuit priest and historian **Pedro
Chirino** established a **school for boys in Tigbauan in 1592** — taught
catechism, **reading, writing, Spanish, and liturgical music** — followed in
**1593–94** by what is described as the **first Jesuit boarding school** in the
Philippines. *(Scholarly caveat: some historians, e.g. Javellana, argue Chirino's
mission school may actually have stood at **Suaraga — present-day San Joaquin** —
even though the NHI placed a commemorative marker at Tigbauan in 1975. Note the
debate; don't assert the location as settled.)* [17][18][19]

**Hablon weaving.** Tigbauan was a noted producer of **hablon**, and a small
weaving community keeps the craft alive today. *Hablon* comes from the Hiligaynon
word **habol**, "to weave" — the cloth took the name of the *doing*. It is woven
on a **handloom** from natural fibers — **cotton, piña** (pineapple), and **jusi /
abaca** — in **plaid, striped, and checkered** patterns. The process is a literal
sequence of steps: threads set on a **warping tool/frame**, wound onto the
**weaver's beam**, the **heddle** raised or lowered by a **foot pedal**, the
**weft** thrown across by a **shuttle** and beaten into place by the **reed** —
an instruction set that repeats, the perfect metaphor for loops and functions.
Hablon predates the Spanish and rose to prominence in the **1800s** when the
Iloilo port opened to international trade. [21][22][23]

**The guerrilla town (WWII).** Tigbauan carries a distinct World War II layer:
- The **First Ambush Marker** in **Barangay Namocon** commemorates the
  **September 2, 1942** ambush, when local guerrillas struck a Japanese convoy
  (accounts cite ~**11 trucks** heading toward Antique). [20]
- The **Panay Landing Memorial** (Barangay Parara) commemorates Allied/American
  landings on Panay. [20]
- The **Bantayan Watch Tower** survives from the town's older coastal-defense
  system. [20]

**The church.** The **San Juan de Sahagun Parish Church** was built in the
**Churrigueresque** style and **completed in 1867** using **reddish coral
limestone**; its richly carved facade — a crescendo of whorls, scrolls, and
foliate designs — was declared a **National Cultural Treasure** by the National
Museum on **June 27, 2019**, and survived both WWII and the 1948 earthquake. [16]

### Key facts (Lore Book set)

- First school for boys in the PH: Jesuit Pedro Chirino, 1592 (Tigbauan); first
  Jesuit boarding school 1593–94. (Location debated: possibly Suaraga / present
  San Joaquin.) [17][18][19]
- Hablon: from *habol* ("to weave"); handloom; cotton/piña/jusi/abaca;
  plaid/stripe/check; multi-step process (warp → beam → heddle/pedal → shuttle →
  reed); pre-colonial, boomed in the 1800s. [21][22][23]
- First Ambush Marker, Barangay Namocon — Sept 2, 1942 ambush of a Japanese
  convoy; Panay Landing Memorial (Parara); Bantayan Watch Tower. [20]
- San Juan de Sahagun Church: Churrigueresque, completed 1867, reddish coral
  limestone; facade a National Cultural Treasure (2019). [16]

### Gameplay & dialogue hooks

- **Level 3 puzzle (functions + loops):** "encode the weave" — define a repeating
  pattern with parameters and a counter; the non-code mirror-grid reproduces a
  reference weave. Weaving *is* coded instruction.

> **🔒 Hold for the reveal:** Save the **WWII guerrilla resistance** (the First
> Ambush) for the journal page / completion beat — Tigbauan's surface identity is
> gentle and craft-based, so revealing that this quiet weaving town was also a
> town of resistance fighters is a strong, earned tonal turn. The family page (an
> ancestor whose work outlived them) pairs naturally with the idea that a woven
> pattern, like a carving, outlasts its maker.

---

# 4 · Guimbal *(scenic drive-through)*

> *Theme:* The "agong" town — Spanish civil infrastructure and golden coral
> stone.
> *Game role:* Drive-through between Tigbauan and Miag-ao; heritage dialogue
> only, no dedicated puzzle in v1.

### Snapshot

| | |
|---|---|
| **Era touched** | Spanish colonial |
| **Signature site** | Taytay Tigre bridge; San Nicolas de Tolentino Church |
| **Beyond the church** | "Little Luneta" plaza; "Parthenon of Western Visayas" municipal hall; yellow coral-stone (*igang*) material culture |
| **Mood** | A pretty, golden-stoned town passing by the window |

### Heritage deep-dive

**The name itself is a warning.** "**Guimbal**" is said to come from the **agong**
— the gong/drum-like instrument early settlers **beat to warn of Moro raids.**
The Spaniards heard "cymbal"; the locals rendered it "guimbal." The town's very
name remembers the raiding era. [26]

**The golden church.** The **San Nicolás de Tolentino Church** sits in a parish
that began as a **visita of Oton, then Tigbauan** (~1575), became a **separate
parish in 1580**, and got an Augustinian **convent in 1590**; **St. Nicholas was
enthroned as patron in 1704.** The present stone church and convent were built
under **Fr. Juan Campos, c. 1769–1774**, of **yellow sandstone / coral rock
called *igang*** quarried from **Guimaras**, giving the town its warm golden hue.
Its **belfry rises four storeys and doubled as a watchtower** against Moro
pirates. The church was damaged by the **July 13, 1787 earthquake** and later
reconstructed by **Fr. José Orangren (1893–96)**; the convent did not survive the
1948 earthquake. *(An earlier popular account dates the first church to ~1742
under Fr. Juan Aguado — note the discrepancy.)* [24][25]

**Taytay Tigre.** A short Spanish-colonial bridge popularly called **"Taytay
Tigre"** for the **tiger stone figures** set at both approaches, guarding the way
into the *poblacion*. [27][25]

**Civic pride.** The town **plaza** is fondly called the **"little Luneta of
southern Iloilo,"** and the municipal building has been nicknamed the
**"Parthenon of Western Visayas."** [27]

### Key facts (Lore Book set)

- "Guimbal" ← *agong*, the alarm instrument beaten against Moro raids. [26]
- San Nicolás de Tolentino Church: parish 1580; present church under Fr. Juan
  Campos c. 1769–1774; *igang* yellow coral stone from Guimaras; 4-storey
  watchtower-belfry; rebuilt 1893–96. (Earlier ~1742 date sometimes cited.) [24][25]
- Taytay Tigre: Spanish-era bridge named for its tiger stone guardians. [27][25]
- Plaza = "little Luneta of southern Iloilo"; municipal hall = "Parthenon of
  Western Visayas." [27]

### Gameplay & dialogue hooks

- **Drive-through narration:** a few warm lines as the golden town slides past —
  ideal for the recurring driver/mentor character. The *agong*-name and the
  coral-stone motif plant a thread that pays off at Miag-ao and in Tigbauan's
  coral-stone trim cosmetic.

---

# 5 · Miag-ao

> *Theme:* Where Filipino artisans rewrote Catholic imagery — the most
> architecturally and artistically rich stop.
> *Game role:* Level 4 ("The Stone Fortress").

### Snapshot

| | |
|---|---|
| **Era touched** | Spanish colonial (defensive era) |
| **Signature site** | Church of Santo Tomás de Villanueva — UNESCO World Heritage |
| **Beyond the church** | The bas-relief facade's indigenous iconography; hablon weaving cooperatives (Indag-an) |
| **Mood** | Monumental, golden-stoned, layered with meaning |

### Heritage deep-dive

**A fortress that is a church.** The **Church of Santo Tomás de Villanueva** was
built by Spanish **Augustinians beginning 1787** (commonly given as **completed
1797**) and designed to **double as a defensive structure** against the Muslim
raiders who had been ravaging the coast. Its defenses are literal: a
**foundation 6 meters deep**, **stone walls ~1.5 meters thick**, reinforced by
**~4-meter-thick flying buttresses** — built per the fortification logic of
**Royal Decree 111 of 1573 (the Law of the Indies).** It was inscribed on the
**UNESCO World Heritage List in 1993** as one of the **four** *Baroque Churches of
the Philippines.* [28][30][31]

**Two unequal towers.** The facade is flanked by **twin belfries of different
heights** — one **two storeys**, the other **three** — built in different periods,
giving the church its distinctive asymmetric, fortress-like silhouette. [30]

**The facade that translates Catholic art into Filipino terms.** The real story
is the **bas-relief facade**, a fusion of **Spanish, Chinese, Muslim, and local**
traditions. Its center is dominated by the parish patron, **St. Thomas of
Villanova**; above and around climbs a great **coconut tree as the "tree of
life,"** reaching nearly to the apex, with **St. Christopher** — dressed in
**local everyday clothing** — **carrying the Child Jesus**, clinging to the
coconut tree and **surrounded by papaya and guava trees**, native flora and
fauna, and scenes of the townspeople's daily life. Filipino craftsmen quietly
inserting their own world into European religious art: this is the heart of the
game's heritage theme. [28][30]

**A living weaving town.** Miag-ao is also a recognized **hablon** hub; weaving
**cooperatives** (notably in **Indag-an**) sustain both livelihood and cultural
continuity, tying it back to Tigbauan's craft layer. [32]

### Key facts (Lore Book set)

- Church of Santo Tomás de Villanueva: fortress-church vs. coastal raiders; built
  from 1787 (often "completed 1797"); 6 m foundation, ~1.5 m walls, ~4 m flying
  buttresses; per Royal Decree 111 of 1573. [28][30]
- UNESCO World Heritage, 1993 (one of four *Baroque Churches of the
  Philippines*). [31]
- Twin belfries of unequal height (two-storey + three-storey). [30]
- Facade: St. Thomas of Villanova (center); coconut "tree of life"; St.
  Christopher in local dress carrying the Child Jesus; papaya/guava, native
  flora/fauna, daily life; Spanish/Chinese/Muslim/local fusion. [28][30]
- Hablon cooperatives (Indag-an). [32]

### Gameplay & dialogue hooks

- **Level 4 puzzle (nested conditionals + tracking variables):** "restore the
  facade" — place carving tiles by layered rules; the non-code reassembly puzzle
  rebuilds the relief with foreground/background depth. The facade's *layers of
  meaning* map directly to nested logic.

> **🔒 Hold for the reveal:** Save the **St. Christopher-in-local-clothing /
> "Filipinos wrote themselves into the art"** insight for the completion beat —
> it's the thematic keystone of the whole game (knowing where you come from), so
> let it land as the reward, paired with the family page about the family's deep
> roots in the region.

---

# 6 · San Joaquin

> *Theme:* The church that celebrated a war — and the cemetery that holds the
> last page.
> *Game role:* Level 5 ("The Final Road") + finale.

### Snapshot

| | |
|---|---|
| **Era touched** | Spanish colonial (mid–late 19th c.) |
| **Signature site** | San Joaquin Church + the Campo Santo cemetery |
| **Beyond the church** | Augustinian convent ruins (kiln, round water well); the sea-facing complex |
| **Mood** | Dramatic, sea-swept, elegiac — the journey's end |

### Heritage deep-dive

**The militaristic facade.** San Joaquin Church was **erected on a plain
overlooking the sea by Fr. Tomás Santarén, OSA, and inaugurated in 1869**, built
of **white coral rock** quarried from nearby shores and from the town of
**Igbaras**. Its **disproportionately large pediment** carries the country's most
famous battle relief — the **"Rendición de Tetuán"** (its title carved at the
base), depicting the **Spanish victory over Moroccan forces at the Battle of
Tetuán** during the Spanish–Moroccan War. The scene is startlingly kinetic —
cavalry and infantry tearing down Moorish defenses, the **agony of wounded
soldiers** visible in the stone. It was executed by Spanish engineer **Felipe
Díez (Díaz)** with **local or possibly Chinese carvers.** Tradition holds the
relief was a **personal tribute** connected to Santarén's father, said to have
served in the campaign. *(The fall of Tetuán dates to 1859–1860; some Philippine
sources loosely cite "1861" — flag the year as approximate.)* It is the **most
militaristic church facade in the Philippines** and a **National Cultural
Treasure / national shrine.** [33][34][35]

**The convent and the sea.** Among the remnants of the old convent are a **kiln**
and a **round water well**; the whole complex faces the sea. [34]

**The Campo Santo cemetery — the real hidden gem.** About **a kilometer east**
stands the **San Joaquin Campo Santo**, a **Spanish-era Baroque cemetery built
by Fr. Mariano Vamba, OSA, in 1892.** It is dominated by an **octagonal *capilla*
(chapel) of locally cut coral stone and red brick**, approached up a **grand,
steep stone staircase.** The Campo Santo was declared a **National Cultural
Treasure** by the National Museum in **December 2015** (and was later restored
after vandalism/treasure-hunting damage). [34][36][37]

### Key facts (Lore Book set)

- San Joaquin Church: built by Fr. Tomás Santarén, OSA, inaugurated 1869; white
  coral rock (from Igbaras); sea-facing. [33][34][35]
- *Rendición de Tetuán* relief: Spanish victory over Moroccan forces; oversized
  pediment; carved by engineer Felipe Díez with local/Chinese carvers; tradition
  ties it to Santarén's father's war service. (Battle 1859–60; "1861" loosely
  cited.) [33][35]
- Most militaristic church facade in the country; National Cultural Treasure /
  national shrine. [35]
- Convent remnants: a kiln + a round water well; complex faces the sea. [34]
- Campo Santo: ~1 km east; built by Fr. Mariano Vamba, 1892; octagonal coral-
  stone & red-brick chapel up a steep staircase; National Cultural Treasure
  (Dec 2015). [34][36][37]

### Gameplay & dialogue hooks

- **Level 5 puzzle (multi-variable constraints):** "the final road" — route to
  the Campo Santo under fuel caps, indexed stops, and prioritized safety paths;
  the non-code priority-routing puzzle avoids hazard tiles. The cemetery is the
  literal destination of the final page.
- **Finale framing:** the protagonist searches the Campo Santo for the last
  journal page; the father's final message is the emotional climax.

> **🔒 Hold for the reveal:** Save the **"why a Filipino church commemorates a war
> in Morocco" / the father-tribute story** for the finale — it mirrors the game's
> whole frame (a son discovering why a father did what he did). Let the drive
> mention the dramatic facade, but reserve the *reason behind it* to rhyme with
> the father's final letter explaining why he scattered the pages.

---

# Appendix A — Proposed level cast (placeholder names)

One speaking character per level. Names are placeholders — swap freely. The
tutorial character is the teacher/guide; the rest are passengers who **knew the
father personally** and act as living museums for their town. Guimbal, as a
drive-through, is narrated by the recurring driver-mentor rather than a dedicated
passenger.

| Level | Town | Character (placeholder) | Who they are | Voice / function |
|-------|------|------------------------|--------------|------------------|
| Tutorial | Iloilo City | **Ate Gemma** | Veteran jeepney **dispatcher** who ran the routes with the father | Sharp, warm, no-nonsense; teaches controls + fare basics |
| 1 | Molo | **Lola Caring** | Retired **embroiderer / pancit Molo cook**; family friend | Proud Molo matron; carries the women-of-the-family thread |
| 2 | Oton | **Lolo Nicro** | Retired **shipwright / *panday*** | Quiet, weathered; speaks of gold, sea, and old trade |
| 3 | Tigbauan | **Manang Delia** | Hablon **handloom weaver** | Rhythmic, patient; weaving-as-instruction; later, the war memory |
| 4 | Miag-ao | **Lola Sabel** | Elder of an **Indag-an weaving cooperative**; church devotee | Recognizes the journal; speaks of roots and the facade's meaning |
| 5 | San Joaquin | **Mang Tomas** | **Campo Santo caretaker** / local historian | Elegiac, grounded; guards the place of the final page |
| *(drive-through)* | Guimbal | **Lolo Kardo** | The father's old **co-driver / mentor**, recurring narrator | Folksy passing commentary on the golden town |

---

# Appendix B — Fact reservation guide (drive vs. reveal)

A quick map of what to spend on the drive versus what to **withhold** for the
completion page / minigame heritage summary, so each level keeps a payoff.

| Town | Spend on the drive (texture) | 🔒 Hold for the reveal (payoff) |
|------|------------------------------|---------------------------------|
| Molo | Women's church; food-city pride; Calle Real | The textile economy (50,000+ looms, cloth→sugar) → grandmother ran the family's trade logistics |
| Oton | The market; the *panday* / shipyard tradition | The *meaning* of the gold mask (gold repels evil; gold = rank) → great-grandfather forged the first jeepney frames |
| Tigbauan | Hablon weaving; *habol*; fiesta textiles | The WWII guerrilla resistance (First Ambush, 1942) → ancestor's work outlives the maker |
| Miag-ao | Coconut "tree of life"; fortress purpose; cooperatives | St. Christopher in local dress — "Filipinos wrote themselves into the art" → family's deep roots |
| San Joaquin | The dramatic battle facade; the sea-facing complex | *Why* the war is carved there (a son's tribute to a father) → the father's final letter |

---

# References

*(All sources non-Wikipedia, per team request.)*

**Iloilo City / Molo**
1. *Molo Church* (history & construction) — Iloilo Ph. https://www.iloiloph.com/molo-church/
2. *Why the Molo Church in Iloilo is called 'the women's church'* — Getaway.PH. https://getaway.ph/blog/travel/why-the-molo-church-in-iloilo-is-called-the-womens-church/
3. *Molo Church: A feminist church in the Philippines* — Explore Iloilo. https://www.exploreiloilo.com/do/info/molo-church/
4. *The rise and fall of Chinese textile business in Iloilo* — Tulay (Chinese-Filipino Digest). https://tulay.ph/2017/04/25/the-rise-and-fall-of-chinese-textile-business-in-iloilo/
5. *Loney, Nicholas (1828–1869)* — Encyclopedia.com. https://www.encyclopedia.com/history/news-wires-white-papers-and-books/loney-nicholas-1828-1869
6. *Exploring Iloilo's Rich History: Heritage Sites and Old Buildings* — Yodisphere. https://www.yodisphere.com/2023/10/Iloilo-City-History-Heritage-Sites.html
7. *[Ilonggo Notes] A city of cultural heritage tourism zones* — Rappler. https://www.rappler.com/life-and-style/travel/ilonggo-notes-city-cultural-heritage-tourism-zones-iloilo/
8. *Iloilo City* (Fort San Pedro & landmarks) — Experience Western Visayas. https://experiencewesternvisayas.com/destinations/iloilocity/
9. *Iloilo City named UNESCO Creative City of Gastronomy* — Philstar (Nov 3, 2023). https://www.philstar.com/headlines/2023/11/03/2308524/iloilo-city-named-unesco-creative-city-gastronomy
10. *Iloilo City* — UNESCO Creative Cities Network. https://www.unesco.org/en/creative-cities/iloilo-city

**Oton**
11. *Oton Gold Death Mask Gallery* — National Museum of the Philippines. https://www.nationalmuseum.gov.ph/exhibitions/nm-western-visayas-regional-museum/oton-gold-death-mask-gallery/
12. *Oton death mask: Celebrating the afterlife* — VERA Files. https://verafiles.org/articles/oton-death-mask-celebrating-the-afterlife
13. *The Oton Gold Death Mask of San Antonio, Oton, Iloilo* — Yodisphere. https://www.yodisphere.com/2023/01/Oton-Gold-Death-Mask-Iloilo.html
14. *Oton Gold Death Mask is coming home* — Daily Guardian. https://dailyguardian.com.ph/oton-gold-death-mask-is-coming-home/
15. *Katagman Festival* — Festivalscape. https://www.festivalscape.com/philippines/iloilo/katagman-festival/

**Tigbauan**
16. *San Juan de Sahagun Church in Tigbauan, Iloilo* — The Old Churches. https://www.theoldchurches.com/philippines/iloilo/tigbauan/san-juan-de-sahagun-church/
17. *First Jesuit Boarding School for Boys* — Discover the Beauty of ONE! Tigbauan. https://discoverthebeautyofonetigbauan.wordpress.com/2018/10/15/first-jesuit-boarding-school-for-boys/
18. *The Jesuits in Tigbauan, Iloilo* — My Philippine Life. https://myphilippinelife.com/the-jesuits-in-tigbauan-iloilo/
19. *Iloilo's Firsts* — Province of Iloilo (official). http://www.iloilo.gov.ph/iloilos-firsts
20. *First Ambush* — Discover the Beauty of ONE! Tigbauan. https://discoverthebeautyofonetigbauan.wordpress.com/2018/10/16/first-ambush/
21. *Hablon Textiles: Reviving Iloilo's Ancient Weaving Tradition* — Pinas Culture. https://pinasculture.com/hablon-textiles-reviving-iloilos-ancient-weaving-tradition/
22. *The art of Hablon weaving in Iloilo* — Out of Town Blog. https://outoftownblog.com/the-art-of-hablon-weaving-in-iloilo/
23. *Weaving new life into Iloilo's hablon* — BusinessWorld. https://www.bworldonline.com/editors-picks/2019/05/13/230436/weaving-new-life-into-iloilos-hablon/

**Guimbal**
24. *San Nicolas de Tolentino Church in Guimbal, Iloilo* — The Old Churches. https://www.theoldchurches.com/philippines/iloilo/guimbal/san-nicolas-de-tolentino-church/
25. *Guimbal Church: A Timeless Heritage Landmark in Iloilo* — Sirang Lente. https://www.siranglente.com/2016/01/guimbal-church-ilo-ilo-visit-travel-itinerary.html
26. *Guimbal, Iloilo Etymology* — Philippine Towns Etymology. https://ubasnamaycyanide.wixsite.com/philippine-towns-ety/post/guimbal-iloilo-etymology
27. *Historical Sites and Landmarks (Guimbal)* — Guimbal CSED. https://www.angelfire.com/linux/guimbal_csed/LANDMARKS.htm

**Miag-ao**
28. *Santo Tomas de Villanueva Church in Miagao, Iloilo* — The Old Churches. https://www.theoldchurches.com/philippines/iloilo/miagao/santo-tomas-de-villanueva-church/
29. *Santo Tomas de Villanueva, Miagao, Iloilo* — Municipality of Miagao (official). https://www.miagao.gov.ph/about-miagao/religious-heritage/santo-tomas-de-villanueva-miagao-iloilo/
30. *Miag-ao's Church of Santo Tomas de Villanueva: One of the Baroque Churches of the Philippines* — Intrepid Wanderer. https://intrepidwanderer.com/2013/11/miag-aos-church-of-santo-tomas-de-villanueva-one-of-the-baroque-churches-of-the-philippines/
31. *Baroque Churches of the Philippines* — UNESCO World Heritage Centre. https://whc.unesco.org/en/list/677/
32. *The Resurgence of Miagao's Hablon* — Fame+. https://fameplus.com/touchpoint/the-resurgence-of-miagaos-hablon

**San Joaquin**
33. *Façade of San Joaquin Church* — IIAS, The Newsletter (International Institute for Asian Studies). https://www.iias.asia/the-newsletter/article/facade-san-joaquin-church
34. *San Joaquin Church in San Joaquin, Iloilo* — The Old Churches. https://www.theoldchurches.com/philippines/iloilo/san-joaquin/san-joaquin-church/
35. *Philippines' most militaristic church marks 150th year* — Inquirer Lifestyle. https://lifestyle.inquirer.net/331886/philippines-most-militaristic-church-marks-150th-year/
36. *Revisiting San Joaquin's National Cultural Treasures* — Philippine Information Agency. https://pia.gov.ph/features/revisiting-san-joaquins-national-cultural-treasures/
37. *National Museum restores desecrated Campo Santo of San Joaquin in Iloilo* — Inquirer Lifestyle. https://lifestyle.inquirer.net/243122/national-museum-restores-desecrated-campo-santo-of-san-joaquin-in-iloilo/

---

*Compiled for the Cyfer team as pre-production reference for Lugarithm. Heritage
facts are drawn from the non-Wikipedia sources above; where popular sources
disagree on dates or figures, the discrepancy is noted in-line rather than
resolved silently.*
