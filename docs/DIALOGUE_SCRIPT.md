# Lugarithm — Dialogue & Script

> **Status:** DRAFT · **Version:** 1.2 · **Team:** Cyfer · **Compiled:** June 13, 2026
>
> **Purpose:** The spoken, branching dialogue for every level — played *live
> during the drive*, Genshin-style: the player chooses an intent, the passenger
> answers, and the conversation branches. Companion to the
> [Heritage Research Dossier](HERITAGE_RESEARCH.md) (the fact source) and the
> [PRD](PRD.md) — the story-passenger loop (LOOP-R2) and the Living Story system (AI-R1).
>
> **v1.2 changes:** Kept the dialogue **lean** — removed the artifact/portal
> theme layer and the heavier folded-in detail. Every spoken fact has been
> verified against the latest **v1.1 research dossier** for accuracy.

---

## How to read this script

The conversation is **node-based**. Each node is a passenger line plus the player
choices that branch from it. These scripted lines are also the **AI fallback**:
the Living Story Engine rephrases them at runtime, but may never assert a fact
that isn't seeded here or in the dossier's Lore Book set.

| Symbol | Meaning |
|--------|---------|
| `NAME:` | A spoken line by that character |
| `YOU ▸` | A player **intent choice** (the player picks a topic, not the exact words) |
| `→ N2` | Jump to node `N2` |
| `↩ HUB` | Return to the conversation hub (the re-selectable topic menu) |
| `✦ once` | One-time topic — greys out in the hub after it's heard |
| `[EVENT: …]` | A gameplay beat fires here (breakdown, fare, arrival) — dialogue pauses for it |
| `(+♥)` | Small relationship/affinity gain (feeds the passenger relationship/affinity score) |
| `🔒 REVEAL` | Held-back heritage beat — only plays on the completion cutscene, never on the drive |

**Drive conversation model.** Each leg opens with a scripted **boarding beat**,
then unlocks a **HUB** of 2–4 topics the player can pick in any order while
driving (or while the Automation program runs). Topics are short heritage
exchanges. A gameplay `[EVENT]` interrupts mid-leg. When the player picks the
**▸ advance** topic (or all topics are spent), the leg moves to **arrival**. The
town puzzle has its own short assist lines, and the **completion cutscene**
delivers the held-back fact + the recovered journal page.

**Voice cast** (placeholders — see [Dossier Appendix A](HERITAGE_RESEARCH.md#appendix-a--proposed-level-cast-placeholder-names)):
Ate Gemma (tutorial), Lola Caring (Molo), Lolo Nicro (Oton), Manang Delia
(Tigbauan), Lolo Kardo (Guimbal narration / framing), Lola Sabel (Miag-ao),
Mang Tomas (San Joaquin). **YOU** = the protagonist, a young man who grew up in
the city and barely knew his late father.

---

# Tutorial — Iloilo City · *Ate Gemma*

> **Role:** Teaches controls + fare basics; warm, sharp-tongued dispatcher who
> ran the routes with the father. She does *not* ride along — she's the terminal
> guide who talks you through your first loop.
> **Heritage spend:** light — the city as "a goldmine of history" framing only.

### Phase 1 — At the terminal *(boarding beat, auto)*

GEMMA: *(rapping the hood)* Aba! So you finally fit behind your old man's wheel.
Sit up straight, you look like you're about to faint.

GEMMA: I'm Gemma. I dispatched this route with your father for twenty years.
He could thread this jeepney through Calle Real traffic with his eyes closed.
You? *(she squints)* We'll start slow.

**[HUB-T]** *Ate Gemma is sizing you up. (Pick a topic — or tell her you're ready.)*

- **YOU ▸ "Who were you to my father?"** → T1 ✦ once
- **YOU ▸ "How do I even drive this thing?"** → T2 *(unlocks driving tutorial)*
- **YOU ▸ "What's the deal with the coins?"** → T3 *(unlocks fare tutorial)*
- **YOU ▸ "What is there to see around here?"** → T4 ✦ once
- **YOU ▸ "I'm ready. Let's go."** → T-ADV *(requires T2 + T3 heard)*

---

**[T1] "Who were you to my father?"** ✦
GEMMA: His dispatcher. His friend. The one who covered for him when he drove off
to leave things in towns down the coast — wouldn't say what. *(beat)* You'll
find out. That's the whole point of this trip, anó? (+♥)
↩ HUB-T

**[T2] "How do I even drive this thing?"** *(launches driving tutorial)*
GEMMA: Hands on the wheel. The old roads here are slick — she'll keep sliding
after you let go, so steer *early*, not late. Treat momentum like a passenger
who doesn't want to stop.

- **YOU ▸ "Slide? That sounds dangerous."** → T2a
- **YOU ▸ "Got it — let me try."** → T2b

  **[T2a]**
  GEMMA: It's a jeepney, not a banca. You won't sink. Worst case you bump and I
  laugh at you. Go on.
  → T2b

  **[T2b]** `[EVENT: DRIVING TUTORIAL — short loop around the terminal; WASD / arrow keys; practice a turn]`
  GEMMA: *(after)* Ha! Not bad. Your father bumped the curb his first day too.
  Don't tell anyone I told you. (+♥)
  ↩ HUB-T *(T2 now spent)*

**[T3] "What's the deal with the coins?"** *(launches fare tutorial)*
GEMMA: A passenger pays, the coins drop in your drawer. You give back exact
change before they lose patience — watch the little timer over their head.
Short-change them and you'll hear about it.

  `[EVENT: FARE TUTORIAL — Coin Drawer; build correct change for one passenger before the patience timer empties]`
GEMMA: *(after)* "Salamat!" — see? A happy passenger is money in your pocket.
Mess it up and it just dents your take. You never *lose*, you just earn less.
↩ HUB-T *(T3 now spent)*

**[T4] "What is there to see around here?"** ✦
GEMMA: You're parked in a goldmine and you don't even know it. Those buildings
down Calle Real — American-era, all of it, neoclassical, art deco, the works.
And the food? In 2023 the whole city got named a UNESCO Creative City of
Gastronomy. Batchoy, pancit Molo — *namit gid.* (+♥)
↩ HUB-T

---

**[T-ADV] "I'm ready. Let's go."**
GEMMA: Look at you. *(she pats the door)* Your first real stop is Molo, just
across the district. Old friend of your father's is waiting — and so's the first
page of that journal. Drive safe, iho. Padayon.

`[EVENT: TUTORIAL COMPLETE → transition to Level 1 drive]`

---

# Level 1 — Molo · *Lola Caring*

> **Role:** Retired embroiderer and pancit Molo cook; family friend who boards
> for the short hop into the Molo district. Proud, maternal, carries the
> women-of-the-family thread.
> **Heritage spend:** the "women's church," Molo food pride, the old streets.
> **🔒 Hold for reveal:** the textile economy → the grandmother ran the family's
> trade logistics.

### Phase 1 — Boarding *(auto)*

LOLA CARING: *(settling in with a basket)* Ay, the Causing jeepney! I'd know
this rattle anywhere. *(she looks at you)* And you must be the boy. You have
your lola's eyes, did anyone ever tell you that? No? *(soft laugh)* Of course
not. Drive on, anak — Molo Plaza, please.

**[HUB-M]** *Lola Caring folds her hands, ready to talk. (Pick a topic.)*

- **YOU ▸ "You knew my grandmother?"** → M1 ✦ once
- **YOU ▸ "What's special about Molo?"** → M2
- **YOU ▸ "Something smells amazing."** → M3 ✦ once
- **YOU ▸ "We're almost there." (advance)** → M-ADV

---

**[M1] "You knew my grandmother?"** ✦
LOLA CARING: Knew her? I sat beside her in the pews of Molo Church every Sunday.

- **YOU ▸ "What's the church like?"** → M1a
- **YOU ▸ "What was she like?"** → M1b

  **[M1a]** *(spend; the deep family link is held)*
  LOLA CARING: They call it the women's church — the feminist church, the young
  ones say now. Every saint standing inside is a woman. Sixteen of them. Built
  for the strong women of this town. *(she smiles)* Your lola fit right in.
  ↩ HUB-M

  **[M1b]**
  LOLA CARING: Iron, that one. Soft voice, iron underneath. She kept things
  running when nobody thought a woman should. *(she stops herself)* But that's
  for the page to tell you, not me. (+♥)
  ↩ HUB-M

**[M2] "What's special about Molo?"**
LOLA CARING: Everything, if you have eyes. The old houses. The plaza. The church
with all its lady saints. And the *food* — but you'll smell that before I finish
the sentence.
↩ HUB-M

**[M3] "Something smells amazing."** ✦
LOLA CARING: *(pleased)* Pancit Molo. My recipe — well, your lola's recipe,
really, I only borrowed it. This whole city got a UNESCO name for its cooking,
you know. We've been feeding people properly for a hundred years. (+♥)
↩ HUB-M

`[EVENT: MID-ROUTE BREAKDOWN — tire-pressure / belt check minigame. Soft timer.]`
LOLA CARING: *(as you fix it)* Take your time, anak. Your father never rushed a
repair either. A jeepney you love, you don't hurry.

---

**[M-ADV] "We're almost there."**
LOLA CARING: There — Molo Plaza, the church spires over the rooftops. *(her
voice drops)* The wind scattered your father's first page all over this square.
Go on. Put it back together. I'll wait right here.

`[EVENT: ARRIVE MOLO → town puzzles]`

### Phase 2 — Town puzzle assist *(Molo: maze escape + non-intersecting connections)*

LOLA CARING *(hint, on request)*: These alleys twist like my embroidery thread.
Keep one hand on the wall and you'll never be lost — follow it all the way to the
end.
LOLA CARING *(on success)*: There! Out the other side. Your father would be
clapping.
LOLA CARING *(on fail / retry, no scolding)*: Bumped a wall? Pff. Try again,
the wall isn't going anywhere.

### Phase 3 — 🔒 Completion cutscene & reveal

*(The scattered fragments snap together into the first journal page.)*

🔒 REVEAL — LOLA CARING: *(quietly, reading over your shoulder)* Ah. So he wrote
it down after all. *(beat)* This city ran on cloth once, anak. Sixty thousand
looms in the province, the records say — a whole economy of thread. And the
ledgers, the trade, the keeping-it-all-alive when the men were away or gone…
that was your grandmother. She ran it. Quietly. Like the saints in that church —
no statue, but holding the whole place up.

**JOURNAL PAGE 1 — *Ang Aking Mga Ugat*:** *"To whoever reads this first — start
with your lola. Before me, before the jeepney, there was her, and the thread she
never let break. Our family runs because a woman refused to let it stop."*

**Rewards:** Badge **Anak ng Molo** · Cosmetic **Floral Side Panel** (Molo
embroidery motif).

---

# Level 2 — Oton · *Lolo Nicro*

> **Role:** Retired shipwright / *panday*; enigmatic, weathered, speaks of gold,
> sea, and old trade. Recognizes the jeepney's welds.
> **Heritage spend:** the market, the *panday* / shipyard tradition, the old port.
> **🔒 Hold for reveal:** the *meaning* of the gold mask → the great-grandfather
> who forged the first jeepney frames by hand.

### Phase 1 — Boarding *(auto)*

At the Oton market stop, an old man climbs aboard with a basket of hand-forged
tools.

LOLO NICRO: *(tossing a coin in the tray)* Estacion San Antonio, by the old
shipyards. *(he runs a thumb along the doorframe)* …This welding. I'd know this
chassis anywhere. This was old Causing's, wasn't it. Your father's.
YOU ▸ *(nod)*
LOLO NICRO: Hm. Then sit. We'll talk. The road's long enough for it.

**[HUB-O]** *Lolo Nicro watches the sea go by. (Pick a topic.)*

- **YOU ▸ "You knew this jeepney?"** → O1 ✦ once
- **YOU ▸ "What kind of town is Oton?"** → O2
- **YOU ▸ "You said shipyards?"** → O3
- **YOU ▸ "There's your stop." (advance)** → O-ADV

---

**[O1] "You knew this jeepney?"** ✦
LOLO NICRO: I knew the *hands* that made the frame it grew from. *(he taps his
chest)* Pandays. Smiths. We don't sign our work, but we know it. *(he won't say
more — yet)* (+♥)
↩ HUB-O

**[O2] "What kind of town is Oton?"**
LOLO NICRO: Older than it looks. Long before the Spanish, this was a port —
between the rivers, ships in from China, ceramics, gold, trade. People think
history started when the churches went up. *(snorts)* History was *sailing* here
already.

- **YOU ▸ "Gold?"** → O2a
- **YOU ▸ "What did they trade?"** → O2b

  **[O2a]** *(spend — the belief/meaning is held for the reveal)*
  LOLO NICRO: Gold. They dug a death mask out of the ground at San Antonio —
  hammered gold, thin as a leaf, a piece for the eyes and a piece for the nose.
  Every May the town still covers their faces in gold paper to remember it.
  Beautiful thing. *(he goes quiet)* …There's a reason for the gold. But you
  should earn that one.
  ↩ HUB-O

  **[O2b]**
  LOLO NICRO: Pots. Ceramics from China and the lands south. Iron. And the gold,
  always the gold. This coast was rich in conversation long before it was rich
  in churches. (+♥)
  ↩ HUB-O

**[O3] "You said shipyards?"**
LOLO NICRO: Aye. Oton built sea-going boats when boats were how the world moved.
Pandays of the sea — we beat iron and we shaped wood and we sent it out on the
tide. *(he looks at the jeepney's frame again, says nothing)*
↩ HUB-O

`[EVENT: MID-ROUTE MAINTENANCE — fuel-cap / refuel minigame. Soft timer.]`
LOLO NICRO: Top her off properly. A good machine fed badly still dies. Your
great-grandfather used to say that. *(catches himself)* …Drive.

---

**[O-ADV] "There's your stop."**
LOLO NICRO: The old shipyard. *(he doesn't move yet)* Your father left a page
with me, boy. Said one day his child would come down this road looking for
answers. *(he steps down onto the sand)* Solve the market's puzzle first. Then
come find me. I'll have it ready.

`[EVENT: ARRIVE OTON → town puzzles]`

### Phase 2 — Town puzzle assist *(Oton: cargo sort / array indexing + mask assembly)*

LOLO NICRO *(hint, on request)*: Heaviest on the bottom, smallest on top. Order
is everything — a badly stacked load tips the whole jeepney. Same with your
numbers: put them in their right place, one by one.
LOLO NICRO *(mask assembly success)*: There. Eye-piece, nose-piece, set right.
Look how it catches the light.

### Phase 3 — 🔒 Completion cutscene & reveal

🔒 REVEAL — LOLO NICRO: *(turning the assembled mask in the sun)* You want to
know the reason for the gold? They believed its brightness drove the evil spirits
off — covered the eyes and nose so nothing dark could get in. And the more gold
you were buried with, the higher you'd stood in life. *(he hands you the page)*
Your great-grandfather understood metal like that. After the war he took the old
boat-smith craft and bent it to something new — hammered out the *first jeepney
frames* on this coast, by hand. The machine you're sitting in? It started with
him. Panday ng dagat. Smith of the sea.

**JOURNAL PAGE 2:** *"The frame outlasts the smith. Your great-grandfather knew
it when he beat the first one straight. I knew it every time I turned the key.
Now you know it too."*

**Rewards:** Badge **Panday ng Dagat** · Cosmetic **Anchor Decal**.

---

# Level 3 — Tigbauan · *Manang Delia*

> **Role:** Hablon handloom weaver; patient, rhythmic, speaks of pattern and
> repetition. The quiet one whose town hides a sharper history.
> **Heritage spend:** hablon weaving, the word *habol*, fiesta textiles.
> **🔒 Hold for reveal:** the WWII guerrilla resistance → an ancestor whose work
> outlives the maker.

### Phase 1 — Boarding *(auto)*

MANANG DELIA: *(boarding with a bundle of folded cloth)* Careful of the
patadyong, anak, the dye's still setting. *(she settles)* To the weaving village,
if you please. Fiesta's coming and the looms haven't slept.

**[HUB-TG]** *Manang Delia's hands move even at rest, as if weaving. (Pick a topic.)*

- **YOU ▸ "What is hablon?"** → TG1
- **YOU ▸ "What's the cloth for?"** → TG2 ✦ once
- **YOU ▸ "This town seems so quiet."** → TG3 *(unlocks a deeper beat)*
- **YOU ▸ "The looms are in sight." (advance)** → TG-ADV

---

**[TG1] "What is hablon?"**
MANANG DELIA: *Habol.* It means "to weave," in Hiligaynon. The cloth took the
name of the doing. *(she shows you the weave)* Cotton, piña, jusi — pineapple
and banana made into thread. Plaid, stripe, check. The pattern is just
instructions, anak. Do this, then this, then this, and repeat — same as your
father's old code, no?

- **YOU ▸ "Instructions?"** → TG1a
- **YOU ▸ "It's beautiful."** → TG1b

  **[TG1a]**
  MANANG DELIA: A pattern is a loop. Three threads red, one white, again, again,
  a hundred times — and a blanket appears. Tell the loom the rule once, and let
  it repeat. *(she smiles)* You'll see, at the village. (+♥)
  ↩ HUB-TG

  **[TG1b]**
  MANANG DELIA: Beauty with a memory in it. Every region has its own pattern;
  you can read where a person's from by what they wear. Cloth remembers.
  ↩ HUB-TG

**[TG2] "What's the cloth for?"** ✦
MANANG DELIA: The fiesta! Banners, the patadyong, the good clothes. Tigbauan was
weaving for the whole province back in the Spanish times — buyers still come from
far for our hablon. (+♥)
↩ HUB-TG

**[TG3] "This town seems so quiet."** *(plants the reveal — kept shallow here)*
MANANG DELIA: *(her hands pause for the first time)* …Quiet now. It wasn't always.
This soft little weaving town has a harder thread in it than people expect.
*(she resumes weaving)* But that's a story for when you've earned the page, anak.
Not for the road.
↩ HUB-TG

`[EVENT: MID-ROUTE MAINTENANCE — thread-snap / bobbin re-thread minigame. Soft timer.]`
MANANG DELIA: A snapped thread isn't the end of the cloth. You tie it, you go on.
Remember that. It's true of more than weaving.

---

**[TG-ADV] "The looms are in sight."**
MANANG DELIA: Listen — hear them? *(the click of handlooms)* That sound hasn't
stopped in two hundred years. Help me set the broken pattern right, and the page
your father left is yours.

`[EVENT: ARRIVE TIGBAUAN → town puzzles]`

### Phase 2 — Town puzzle assist *(Tigbauan: pattern function + mirror grid)*

MANANG DELIA *(hint, on request)*: Don't place every thread by hand — you'll be
here till next fiesta. Find the rule. Write it once. Let it repeat the right
number of times.
MANANG DELIA *(on success)*: There's the pattern! Clean as anything. You have
the hands for this, anak.

### Phase 3 — 🔒 Completion cutscene & reveal

🔒 REVEAL — MANANG DELIA: *(folding the finished cloth, then meeting your eyes)*
Now the harder thread. This quiet town fought, anak. In '42, on the road to
Antique, our people ambushed a Japanese convoy — the first ambush, they call it.
Ordinary folk. Weavers, farmers. *(she touches the cloth)* The man in your
father's page was one of them. He didn't live to see the country free. But what
he did outlived him — same as a pattern outlives the weaver who set it. *(she
presses the page into your hand)* That's the whole secret of cloth. And of
people.

**JOURNAL PAGE 3:** *"The pattern outlives the hands. He set something in motion
he never got to see finished — and it held. Meaningful work doesn't ask to
outlive you. It just does."*

**Rewards:** Badge **Mangangukit** · Cosmetic **Coral-Stone Trim**.

---

# Guimbal — *Drive-through interlude · Lolo Kardo (radio)*

> **Role:** The father's old co-driver, riding shotgun on the CB radio as a
> recurring voice. No puzzle here — a few warm lines as the golden town slides
> past, planting the coral-stone motif. No branching hub; one optional aside.

LOLO KARDO *(over the radio crackle)*: Pulling through Guimbal, ha? Slow down a
second, iho, it's worth the look.

LOLO KARDO: See that little bridge — Taytay Tigre. Spanish-built. Those stone
tigers on the ends? Been guarding the way into town longer than anyone alive.

- **YOU ▸ "Everything's so… golden here."** → G1
- **YOU ▸ "Keep driving."** → G-END

  **[G1]**
  LOLO KARDO: Coral stone, iho. *Igang* — they pulled it yellow from the shore
  and from Guimaras. Whole town's built warm because of it. You'll see the same
  stone up the road in Miag-ao. This coast shared its bones to build itself.
  → G-END

**[G-END]**
LOLO KARDO: Plaza there's the "little Luneta," and that grand hall they call the
Parthenon of the West. *(chuckles)* Big names for a small town. Padayon, iho —
Miag-ao's next, and that one'll stop your breath.

`[EVENT: CONTINUE → Level 4 drive]`

---

# Level 4 — Miag-ao · *Lola Sabel*

> **Role:** Elder of an Indag-an weaving cooperative and church devotee; she
> recognizes the journal and hints at a larger tradition. Warm, knowing.
> **Heritage spend:** the coconut "tree of life," the fortress-church purpose,
> the weaving cooperatives.
> **🔒 Hold for reveal:** St. Christopher in local dress — "Filipinos wrote
> themselves into the art" → the family's deep roots in the region.

### Phase 1 — Boarding *(auto)*

LOLA SABEL: *(boarding, then freezing at the sight of the journal on the seat)*
…That book. *Ang Aking Mga Ugat.* *(she touches the cover)* I never thought I'd
see it again. *(she looks at you, eyes bright)* You're his. Of course you are.
Drive to the church, anak. There's something there you need to stand in front of.

**[HUB-MG]** *Lola Sabel keeps glancing at the journal. (Pick a topic.)*

- **YOU ▸ "You recognize the journal?"** → MG1 ✦ once
- **YOU ▸ "Tell me about the church."** → MG2
- **YOU ▸ "You're a weaver too?"** → MG3 ✦ once
- **YOU ▸ "The facade — I can see it now." (advance)** → MG-ADV

---

**[MG1] "You recognize the journal?"** ✦
LOLA SABEL: Your father showed it to me once, half-empty already, pages given
away. He said his child would carry it the whole coast one day. *(softly)* And
here you are, in front of me, with his eyes. *(she dabs at her own)* Forgive an
old woman. (+♥)
↩ HUB-MG

**[MG2] "Tell me about the church."**
LOLA SABEL: Miag-ao. UNESCO calls it a treasure of the world, but we built it as
a *fort.* Walls thick as a man is tall, towers to watch the sea — the raiders
came in the old days, and the church was the town's shield.

- **YOU ▸ "A church that's a fortress?"** → MG2a
- **YOU ▸ "What's carved on the front?"** → MG2b

  **[MG2a]**
  LOLA SABEL: Faith and fear in the same stone. They prayed in it and they hid in
  it. *(she nods)* A building can do more than one thing. So can a person.
  ↩ HUB-MG

  **[MG2b]** *(spend — the St. Christopher insight is held)*
  LOLA SABEL: A coconut tree, right up the middle of the facade — the tree of
  life, reaching for heaven. Around it, our flowers, our birds, our everyday
  world, all in stone. *(she smiles)* There's a figure up there too, but… stand
  in front of him yourself. I won't spoil it. (+♥)
  ↩ HUB-MG

**[MG3] "You're a weaver too?"** ✦
LOLA SABEL: Indag-an village — our cooperative keeps the hablon alive here, same
as Tigbauan downcoast. *(she pats her bag)* The loom feeds the family and keeps
the old knowing from dying. Same idea as that journal of yours, no?
↩ HUB-MG

`[EVENT: MID-ROUTE MAINTENANCE — scaffold-balance / counterweight minigame. Soft timer.]`
LOLA SABEL: Balance it gently. They restore that facade the same way — one
careful weight at a time. Rush, and you lose what you were trying to save.

---

**[MG-ADV] "The facade — I can see it now."**
LOLA SABEL: *(quietly, as the great golden church fills the windshield)* There.
Let it be big for a moment. *(beat)* Help the curator set the carvings right.
Then come stand with me at the front. There's a man in the stone I want you to
meet.

`[EVENT: ARRIVE MIAG-AO → town puzzles]`

### Phase 2 — Town puzzle assist *(Miag-ao: nested-condition facade restore + relief reassembly)*

LOLA SABEL *(hint, on request)*: Not every stone goes in the same way. Check
each one — its place, its layer, what sits behind it — *then* set it. One wrong
question and the whole panel reads wrong.
LOLA SABEL *(on success)*: The facade remembers itself. Look at it — whole again.

### Phase 3 — 🔒 Completion cutscene & reveal

🔒 REVEAL — LOLA SABEL: *(leading you to the center of the facade, pointing up)*
There he is. St. Christopher, carrying the Child Jesus across the river. But look
how they carved him — not in robes, not Spanish. In *our* clothes. Barefoot. Like
a man from this town. *(her voice trembles a little)* The carvers were told to
make European saints, anak. Instead they put *themselves* into the stone —
their tree, their clothes, their world — right on the front of God's house. They
wrote themselves into the art so no one could ever say they weren't here. *(she
hands you the page)* Your family is in that stone the same way. Roots this deep
don't wash out.

**JOURNAL PAGE 4:** *"They could have carved someone else's saint and been
forgotten. Instead they carved themselves. Remember that when the world tells you
who you're supposed to be. We were always here. So are you."*

**Rewards:** Badge **UNESCO Keeper** · Cosmetic **Golden Sandstone Paint**.

---

# Level 5 — San Joaquin · *Mang Tomas*

> **Role:** Caretaker of the Campo Santo cemetery and local historian; elegiac,
> grounded, gentle. The keeper of the last page and the journey's end.
> **Heritage spend:** the dramatic battle facade, the sea-facing complex.
> **🔒 Hold for reveal:** *why* the war is carved there — a son's tribute to a
> father → mirrored by the father's final letter.

### Phase 1 — Boarding *(auto)*

MANG TOMAS: *(boarding slowly, hat in hand)* So you made it all the way down.
*(he looks at you a long moment)* I'm Tomas. I keep the Campo Santo — the old
cemetery up by the church. *(quietly)* I've been keeping the last page for you a
long time, son. Let's not rush the last stretch.

**[HUB-SJ]** *Mang Tomas watches the sea out the window. (Pick a topic.)*

- **YOU ▸ "What's the church known for?"** → SJ1
- **YOU ▸ "Tell me about the Campo Santo."** → SJ2 ✦ once
- **YOU ▸ "Did you know my father well?"** → SJ3 ✦ once
- **YOU ▸ "I think we're here." (advance)** → SJ-ADV

---

**[SJ1] "What's the church known for?"**
MANG TOMAS: For a *war*, of all things. Carved right across the front — soldiers,
cavalry, a battle. The surrender of Tetuán, a Spanish fight against Moroccan
forces, an ocean away from here. The most warlike church face in the whole
country, they say.

- **YOU ▸ "Why carve a foreign war on a church?"** → SJ1a
- **YOU ▸ "Who built it?"** → SJ1b

  **[SJ1a]** *(the reveal's seed — answer is deflected, held for finale)*
  MANG TOMAS: *(a small, sad smile)* That… is the right question. And it has a
  reason — a very personal one. But it belongs with the last page, not before it.
  Be patient with me, son. We're almost there.
  ↩ HUB-SJ

  **[SJ1b]**
  MANG TOMAS: The friars, the locals, stone by stone over years. Faced the whole
  thing toward the sea. Everything here looks at the sea. *(beat)* The dead too.
  ↩ HUB-SJ

**[SJ2] "Tell me about the Campo Santo."** ✦
MANG TOMAS: A baroque cemetery, Spanish-era, declared a national treasure back
in 2015. There's the old convent ruins beside it — a kiln, a round stone well.
*(he turns his hat in his hands)* People think a cemetery is a sad place. I think
it's just a place that *remembers.* That's my whole job. Remembering. (+♥)
↩ HUB-SJ

**[SJ3] "Did you know my father well?"** ✦
MANG TOMAS: Well enough that he trusted me with the ending. *(he meets your eyes)*
He talked about you more than you'd believe, for a man you thought didn't know
you. *(gently)* The page will say it better than I can. (+♥)
↩ HUB-SJ

`[EVENT: MID-ROUTE MAINTENANCE — engine-overheat / coolant-threshold minigame. Soft timer.]`
MANG TOMAS: Easy on her now. She's carried you the whole coast. Let her arrive
with some dignity.

---

**[SJ-ADV] "I think we're here."**
MANG TOMAS: *(as the sea-facing church rises on the hill)* The end of the road,
son. *(he opens the door)* Plan your way up to the Campo Santo carefully — it's a
hard climb with a tired engine. The last page is waiting at the top. So is your
father, in the only way he still can be.

`[EVENT: ARRIVE SAN JOAQUIN → town puzzle: multi-constraint route to Campo Santo]`

### Phase 2 — Town puzzle assist *(San Joaquin: multi-variable constraint routing + priority safety routing)*

MANG TOMAS *(hint, on request)*: You can't watch only one thing now. Fuel, the
stops, the safe path — all at once. The whole journey's been teaching you to hold
more than one thing in your head. This is where it counts.
MANG TOMAS *(on success)*: You made it up. *(softly)* Of course you did.

### Phase 3 — 🔒 Completion cutscene, reveal & finale

*(At the top of the Campo Santo, the final page waits.)*

🔒 REVEAL — MANG TOMAS: *(beside you, looking at the battle carved on the church
below)* You asked why a town would carve a foreign war on its church. *(beat)*
The friar who built it — they say he did it for his *father.* His father fought
in that battle, and the son carved the victory in stone so the old man would
never be forgotten. A whole church facade… as a message from a son to a father.
*(he steps back)* Your father knew that story. I think it's why he chose this
place for the last page.

**JOURNAL PAGE 5 — the father's letter:** *"If you're reading this, you drove the
whole coast to reach me, and you finally know where you come from — the women who
held us together, the smith who hammered our first frame, the hands that outlived
their work, the family that carved itself into the stone. I scattered the pages
because I was a coward with words while I was alive; I could only give you my
history by making you go and earn it. That man in San Joaquin carved a war to
say 'I love you, father.' I had to scatter a journal across five towns to say it
to my son. I'm sorry it took this long. Welcome home, anak. — Tatay."*

MANG TOMAS: *(quietly)* Go home, son. Take all of it with you.

**Rewards:** Badge **Tagapagtanggol** · Cosmetic **Full Battle-Scene Mural** ·
Story completion → **CS-08 Epilogue**.

---

# Appendix — Writing & AI-generation notes

**For the Living Story Engine (PRD AI-R1):**
- Every node above is a **scripted fallback** *and* a **content boundary.** The AI
  may rephrase, shorten, or react to the player's relationship score and prior
  choices — but it may only assert facts seeded in this script or in the
  [Dossier](HERITAGE_RESEARCH.md) Lore Book sets.
- **Never** let a passenger speak a `🔒 REVEAL` fact during the drive. Those are
  completion-cutscene only — they are the payoff the dossier reserves.
- **Spoiler gate:** a passenger may not mention a town the player hasn't reached.
  Order is Tutorial → Molo → Oton → Tigbauan → *Guimbal* → Miag-ao → San Joaquin.
  (Foreshadowing the *next* stop by name is allowed and encouraged — e.g. Manang
  Delia or Lolo Kardo nodding toward Miag-ao.)
- **Affinity (♥):** higher relationship score should unlock warmer phrasings and
  one extra personal aside per passenger; it never unlocks extra *facts*.
- **Tone guide per voice:** Gemma — teasing, brisk. Caring — maternal, proud.
  Nicro — terse, weighty, withholds. Delia — calm, metaphor-in-craft, one turn
  to steel. Kardo — folksy, fond. Sabel — tender, devotional. Tomas — slow,
  elegiac, kind.

**For the editor / scene builders:**
- HUB topics are re-selectable; `✦ once` topics grey out after one viewing.
- `▸ advance` should require the tutorial's mechanical topics (T2 + T3) to be
  heard first; elsewhere it's always available so players who want to skip can.
- Each `[EVENT]` is an existing minigame hook — wire dialogue pause/resume around
  it; soft-timer failure dents score only, never blocks the conversation.

---

*Companion to the Heritage Research Dossier. Spoken heritage uses only a portion
of the researched facts by design — the reserved beats live on the completion
cutscenes so each level keeps a reveal worth driving for.*
