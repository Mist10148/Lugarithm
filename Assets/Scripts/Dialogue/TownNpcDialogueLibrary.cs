using System;

/// <summary>
/// Ambient town-NPC conversations — the lighthearted heritage/fun-fact chats the
/// player has while walking each overworld town before boarding the jeepney.
///
/// These reuse the same branching dialogue engine as <see cref="DialogueLibrary"/>
/// (DialogueController plays them), but deliberately carry NO reveal lines, NO
/// journal page, and NO artifact/level-completion beats — those payoffs are
/// reserved for the in-jeepney main passenger. Town NPCs gesture toward the wonder
/// of a place without spending its secret. Content is sourced from
/// docs/HERITAGE_FUNFACTS.md.
///
/// Conversations use an empty passengerId so lines are delivered as authored
/// (no AI rephrase) — ambient chatter stays cheap and deterministic.
/// </summary>
public static class TownNpcDialogueLibrary
{
    /// <summary>Returns the conversation for one town NPC, or null if unknown.</summary>
    public static DialogueConversation Get(int levelIndex, string npcId)
    {
        if (string.IsNullOrEmpty(npcId)) return null;

        switch (levelIndex)
        {
            case 0: return Iloilo(npcId);
            case 1: return Molo(npcId);
            case 2: return Oton(npcId);
            case 3: return Tigbauan(npcId);
            case 4: return Miagao(npcId);
            case 5: return SanJoaquin(npcId);
            default: return null;
        }
    }

    // -------------------------------------------------------------------------
    // Level 0 — Iloilo City (Tutorial garage district / Calle Real)

    static DialogueConversation Iloilo(string npcId)
    {
        switch (npcId)
        {
            case "il_vendor":
            {
                string s = "Manang Rosa";
                var c = Convo(0, "il_vendor");
                c.nodes["START"] = Node("START", DialogueNodeKind.Line,
                    Lines(Line(s, "Sorbetes, iho? Dirty ice cream, we call it — don't worry, it's clean, it's just the nickname! Cool down before you drive.")),
                    next: "HUB");
                c.nodes["HUB"] = Node("HUB", DialogueNodeKind.Hub,
                    Lines(Line(s, "Manang Rosa scoops a cone. \"What do you want to know about the city?\"")),
                    choices: new[]
                    {
                        Choice("Why's the food such a big deal here?", "T1", once: true),
                        Choice("What's this old street?", "T2", once: true),
                        Choice("Maybe later, manang.", "BYE")
                    });
                c.nodes["T1"] = Node("T1", DialogueNodeKind.Line,
                    Lines(Line(s, "Iho, Iloilo is a UNESCO Creative City of Gastronomy! Batchoy, pancit, fresh seafood — the whole world says our cooking is world-class. I've been saying it for years, but now it's official, ano.")),
                    returnToHub: true);
                c.nodes["T2"] = Node("T2", DialogueNodeKind.Line,
                    Lines(Line(s, "Calle Real! The old downtown. Those grand buildings went up when the city got rich on cloth and sugar — 'Queen City of the South,' they called her. People still shop right under those hundred-year-old arches.")),
                    returnToHub: true);
                c.nodes["BYE"] = Node("BYE", DialogueNodeKind.Line,
                    Lines(Line(s, "Go on then. Mind the traffic — and come back for a second scoop!")));
                return c;
            }
            case "il_student":
            {
                string s = "Toto";
                var c = Convo(0, "il_student");
                c.nodes["START"] = Node("START", DialogueNodeKind.Line,
                    Lines(Line(s, "Kuya! You're driving the jeepney? Ang lupit! These are basically the kings of the road around here.")),
                    next: "HUB");
                c.nodes["HUB"] = Node("HUB", DialogueNodeKind.Hub,
                    Lines(Line(s, "Toto bounces on his heels. \"Ask me anything!\"")),
                    choices: new[]
                    {
                        Choice("What's so special about jeepneys?", "T1", once: true),
                        Choice("What's the big festival here?", "T2", once: true),
                        Choice("Catch you later.", "BYE")
                    });
                c.nodes["T1"] = Node("T1", DialogueNodeKind.Line,
                    Lines(Line(s, "After the war they took leftover army jeeps and turned them into rides for everybody — painted them up, stretched them out. Now it's the most Filipino thing on wheels. Yours is a little old, though, no offense!")),
                    returnToHub: true);
                c.nodes["T2"] = Node("T2", DialogueNodeKind.Line,
                    Lines(Line(s, "Dinagyang! Every January — drums, dancers all painted up, honoring the Santo Niño. The whole city shakes. You can hear it from blocks away. Biggest party of the year, promise.")),
                    returnToHub: true);
                c.nodes["BYE"] = Node("BYE", DialogueNodeKind.Line,
                    Lines(Line(s, "Drive safe, kuya! Beep the horn for me!")));
                return c;
            }
            case "il_tindera":
            {
                string s = "Aling Bising";
                var c = Convo(0, "il_tindera");
                c.nodes["START"] = Node("START", DialogueNodeKind.Line,
                    Lines(Line(s, "Ay, a new face behind a wheel. You sound like you're from here — that sweet Ilonggo lilt. Malambing, the visitors call it.")),
                    next: "HUB");
                c.nodes["HUB"] = Node("HUB", DialogueNodeKind.Hub,
                    Lines(Line(s, "Aling Bising tidies her little shop.")),
                    choices: new[]
                    {
                        Choice("People say we sound like we're flirting.", "T1", once: true),
                        Choice("Business good on Calle Real?", "T2", once: true),
                        Choice("I'll let you work.", "BYE")
                    });
                c.nodes["T1"] = Node("T1", DialogueNodeKind.Line,
                    Lines(Line(s, "Hoy! We just talk sweet, that's all. Say 'palangga' to someone here and you'll see them melt. It's a soft town, this one.")),
                    returnToHub: true);
                c.nodes["T2"] = Node("T2", DialogueNodeKind.Line,
                    Lines(Line(s, "Steady, salamat. This street's been trading for over a hundred years. My shop's in a building older than my lola. We keep it alive, one merienda at a time.")),
                    returnToHub: true);
                c.nodes["BYE"] = Node("BYE", DialogueNodeKind.Line,
                    Lines(Line(s, "Padayon, iho. Drive well.")));
                return c;
            }
        }
        return null;
    }

    // -------------------------------------------------------------------------
    // Level 1 — Molo

    static DialogueConversation Molo(string npcId)
    {
        switch (npcId)
        {
            case "molo_cook":
            {
                string s = "Manang Pacing";
                var c = Convo(1, "molo_cook");
                c.nodes["START"] = Node("START", DialogueNodeKind.Line,
                    Lines(Line(s, "Smell that, anak? That's the pots going. You can't pass through Molo without a bowl in you.")),
                    next: "HUB");
                c.nodes["HUB"] = Node("HUB", DialogueNodeKind.Hub,
                    Lines(Line(s, "Manang Pacing stirs a big steaming pot.")),
                    choices: new[]
                    {
                        Choice("Is pancit Molo really from here?", "T1", once: true),
                        Choice("These old houses are beautiful.", "T2", once: true),
                        Choice("Smells amazing — but I'd better go.", "BYE")
                    });
                c.nodes["T1"] = Node("T1", DialogueNodeKind.Line,
                    Lines(Line(s, "From here? It's NAMED for here! 'Molo' the district, not the noodle — and here's the joke, anak: there are no noodles in pancit Molo at all! It's dumplings in broth. Everyone's surprised.")),
                    returnToHub: true);
                c.nodes["T2"] = Node("T2", DialogueNodeKind.Line,
                    Lines(Line(s, "The Molo mansions! Trading families built them — Chinese-mestizo merchants, rich on cloth. Whole district used to be its own town. Couples come from everywhere just to take photos on the church steps.")),
                    returnToHub: true);
                c.nodes["BYE"] = Node("BYE", DialogueNodeKind.Line,
                    Lines(Line(s, "Off you go. I'll save you a bowl, anak.")));
                return c;
            }
            case "molo_sacristan":
            {
                string s = "Mang Ambo";
                var c = Convo(1, "molo_sacristan");
                c.nodes["START"] = Node("START", DialogueNodeKind.Line,
                    Lines(Line(s, "Going by the church? Stop a moment. Look up at those spires — coral stone, every block.")),
                    next: "HUB");
                c.nodes["HUB"] = Node("HUB", DialogueNodeKind.Hub,
                    Lines(Line(s, "Mang Ambo nods toward Molo Church.")),
                    choices: new[]
                    {
                        Choice("What's special about the church?", "T1", once: true),
                        Choice("Why coral stone?", "T2", once: true),
                        Choice("Beautiful. I'll be on my way.", "BYE")
                    });
                c.nodes["T1"] = Node("T1", DialogueNodeKind.Line,
                    Lines(Line(s, "They call it the women's church, iho. Every saint standing inside is a woman. Built Gothic, all in pale stone. The young ones call it the feminist church now — and they're not wrong.")),
                    returnToHub: true);
                c.nodes["T2"] = Node("T2", DialogueNodeKind.Line,
                    Lines(Line(s, "Whole coast built itself from the stone it had — coral, cut from the shore. Warms up gold in the afternoon light. You'll see the same trick all the way down to Miag-ao.")),
                    returnToHub: true);
                c.nodes["BYE"] = Node("BYE", DialogueNodeKind.Line,
                    Lines(Line(s, "Go with care. The plaza's just ahead.")));
                return c;
            }
            case "molo_kid":
            {
                string s = "Inday";
                var c = Convo(1, "molo_kid");
                c.nodes["START"] = Node("START", DialogueNodeKind.Line,
                    Lines(Line(s, "Hi po! Are you the jeepney driver? My lola rides the Causing jeepney every Sunday!")),
                    next: "HUB");
                c.nodes["HUB"] = Node("HUB", DialogueNodeKind.Hub,
                    Lines(Line(s, "Inday grins, hands behind her back.")),
                    choices: new[]
                    {
                        Choice("What do you do around the plaza?", "T1", once: true),
                        Choice("Bye for now.", "BYE")
                    });
                c.nodes["T1"] = Node("T1", DialogueNodeKind.Line,
                    Lines(Line(s, "We play by the bandstand! And after Mass everybody buys pan de Molo. The plaza smells like bread and the church bells are SO loud. I love it here.")),
                    returnToHub: true);
                c.nodes["BYE"] = Node("BYE", DialogueNodeKind.Line,
                    Lines(Line(s, "Bye po! Beep beep!")));
                return c;
            }
        }
        return null;
    }

    // -------------------------------------------------------------------------
    // Level 2 — Oton

    static DialogueConversation Oton(string npcId)
    {
        switch (npcId)
        {
            case "oton_fisher":
            {
                string s = "Mang Dado";
                var c = Convo(2, "oton_fisher");
                c.nodes["START"] = Node("START", DialogueNodeKind.Line,
                    Lines(Line(s, "Mooring up, ha. This shore's been launching boats longer than anybody can count.")),
                    next: "HUB");
                c.nodes["HUB"] = Node("HUB", DialogueNodeKind.Hub,
                    Lines(Line(s, "Mang Dado coils a wet rope.")),
                    choices: new[]
                    {
                        Choice("This town feels old.", "T1", once: true),
                        Choice("Were there really shipyards here?", "T2", once: true),
                        Choice("Fair winds. I'll go.", "BYE")
                    });
                c.nodes["T1"] = Node("T1", DialogueNodeKind.Line,
                    Lines(Line(s, "Older than it looks. This was a port before the Spanish ever came — ships in from China, trade, gold. History was sailing here long before any church went up. We don't forget that.")),
                    returnToHub: true);
                c.nodes["T2"] = Node("T2", DialogueNodeKind.Line,
                    Lines(Line(s, "Aye. Oton built sea-going boats when boats were how the world moved. Pandays — smiths and shapers, wood and iron both. The old craft's in the bones of this place.")),
                    returnToHub: true);
                c.nodes["BYE"] = Node("BYE", DialogueNodeKind.Line,
                    Lines(Line(s, "Go on. The river road's that way.")));
                return c;
            }
            case "oton_guide":
            {
                string s = "Ate Let";
                var c = Convo(2, "oton_guide");
                c.nodes["START"] = Node("START", DialogueNodeKind.Line,
                    Lines(Line(s, "First time in Oton? You're just in time — there's gold in the air here, almost literally.")),
                    next: "HUB");
                c.nodes["HUB"] = Node("HUB", DialogueNodeKind.Hub,
                    Lines(Line(s, "Ate Let gestures down the road toward San Antonio.")),
                    choices: new[]
                    {
                        Choice("Gold? What do you mean?", "T1", once: true),
                        Choice("What's the river like?", "T2", once: true),
                        Choice("Thanks — heading off.", "BYE")
                    });
                c.nodes["T1"] = Node("T1", DialogueNodeKind.Line,
                    Lines(Line(s, "They dug a famous gold treasure out of the ground here, by the old shipyards — centuries old. Every year the town celebrates it, faces painted in gold paper. It's beautiful. There's a deeper story to the gold, but… that one you have to earn.")),
                    returnToHub: true);
                c.nodes["T2"] = Node("T2", DialogueNodeKind.Line,
                    Lines(Line(s, "The Batiano? Still winds right through town, same as it has for ages. The whole place grew up around the water.")),
                    returnToHub: true);
                c.nodes["BYE"] = Node("BYE", DialogueNodeKind.Line,
                    Lines(Line(s, "Enjoy Oton! Mind the market crowds.")));
                return c;
            }
        }
        return null;
    }

    // -------------------------------------------------------------------------
    // Level 3 — Tigbauan

    static DialogueConversation Tigbauan(string npcId)
    {
        switch (npcId)
        {
            case "tig_weaver":
            {
                string s = "Nanay Pilar";
                var c = Convo(3, "tig_weaver");
                c.nodes["START"] = Node("START", DialogueNodeKind.Line,
                    Lines(Line(s, "Hear that clack-clack, anak? That's the looms. They haven't stopped in two hundred years.")),
                    next: "HUB");
                c.nodes["HUB"] = Node("HUB", DialogueNodeKind.Hub,
                    Lines(Line(s, "Nanay Pilar's hands move even at rest.")),
                    choices: new[]
                    {
                        Choice("What is hablon?", "T1", once: true),
                        Choice("How do you keep the patterns straight?", "T2", once: true),
                        Choice("Lovely. I'll go.", "BYE")
                    });
                c.nodes["T1"] = Node("T1", DialogueNodeKind.Line,
                    Lines(Line(s, "Handloom weaving — cotton, piña, jusi, pineapple and banana made into thread. Plaid, stripe, check. You can read where a person's from by the pattern they wear. Cloth remembers, anak.")),
                    returnToHub: true);
                c.nodes["T2"] = Node("T2", DialogueNodeKind.Line,
                    Lines(Line(s, "A pattern is just a loop. Three threads red, one white — again, again, a hundred times, and a blanket appears. Tell the loom the rule once and let it repeat. Same as your father's old code, no?")),
                    returnToHub: true);
                c.nodes["BYE"] = Node("BYE", DialogueNodeKind.Line,
                    Lines(Line(s, "Padayon. Mind the dye, it's still setting.")));
                return c;
            }
            case "tig_teacher":
            {
                string s = "Mr. Tan";
                var c = Convo(3, "tig_teacher");
                c.nodes["START"] = Node("START", DialogueNodeKind.Line,
                    Lines(Line(s, "A traveler! Welcome to Tigbauan. Small town, surprisingly deep roots.")),
                    next: "HUB");
                c.nodes["HUB"] = Node("HUB", DialogueNodeKind.Hub,
                    Lines(Line(s, "Mr. Tan adjusts his glasses, pleased to have a listener.")),
                    choices: new[]
                    {
                        Choice("Tell me about the church.", "T1", once: true),
                        Choice("Anything famous happen here?", "T2", once: true),
                        Choice("Thank you, sir.", "BYE")
                    });
                c.nodes["T1"] = Node("T1", DialogueNodeKind.Line,
                    Lines(Line(s, "Our church has a facade carved like lace — almost too ornate for a town this size. People stop just to stare at the stonework. It surprises everyone.")),
                    returnToHub: true);
                c.nodes["T2"] = Node("T2", DialogueNodeKind.Line,
                    Lines(Line(s, "We hold a quiet 'first': one of the earliest schools in the islands stood here, centuries ago. This little weaving town has always valued learning. There's a harder story in its past too — but that's not mine to tell on the street.")),
                    returnToHub: true);
                c.nodes["BYE"] = Node("BYE", DialogueNodeKind.Line,
                    Lines(Line(s, "Safe travels. Do visit the looms.")));
                return c;
            }
        }
        return null;
    }

    // -------------------------------------------------------------------------
    // Level 4 — Miag-ao

    static DialogueConversation Miagao(string npcId)
    {
        switch (npcId)
        {
            case "miag_guide":
            {
                string s = "Kuya Boy";
                var c = Convo(4, "miag_guide");
                c.nodes["START"] = Node("START", DialogueNodeKind.Line,
                    Lines(Line(s, "Tara! You picked a good day for the light. Wait till you see the church glow gold.")),
                    next: "HUB");
                c.nodes["HUB"] = Node("HUB", DialogueNodeKind.Hub,
                    Lines(Line(s, "Kuya Boy points up the hill at the great facade.")),
                    choices: new[]
                    {
                        Choice("What makes the church so famous?", "T1", once: true),
                        Choice("What's carved on the front?", "T2", once: true),
                        Choice("Salamat — heading up.", "BYE")
                    });
                c.nodes["T1"] = Node("T1", DialogueNodeKind.Line,
                    Lines(Line(s, "It's a UNESCO World Heritage Site, kuya! But here's the thing — they built it as a fort. Walls thick as a man is tall, towers to watch the sea, because raiders came. A church you pray in AND hide in.")),
                    returnToHub: true);
                c.nodes["T2"] = Node("T2", DialogueNodeKind.Line,
                    Lines(Line(s, "A giant coconut tree, right up the middle — the tree of life — with our plants and birds and everyday world carved all around it. There's a figure up there too, but… better you stand in front of him yourself.")),
                    returnToHub: true);
                c.nodes["BYE"] = Node("BYE", DialogueNodeKind.Line,
                    Lines(Line(s, "Go, go — catch the golden hour!")));
                return c;
            }
            case "miag_weaver":
            {
                string s = "Lola Ines";
                var c = Convo(4, "miag_weaver");
                c.nodes["START"] = Node("START", DialogueNodeKind.Line,
                    Lines(Line(s, "Come in out of the sun, anak. The loom's warm and so's the welcome.")),
                    next: "HUB");
                c.nodes["HUB"] = Node("HUB", DialogueNodeKind.Hub,
                    Lines(Line(s, "Lola Ines threads a shuttle without looking down.")),
                    choices: new[]
                    {
                        Choice("You weave here too?", "T1", once: true),
                        Choice("Bye, lola.", "BYE")
                    });
                c.nodes["T1"] = Node("T1", DialogueNodeKind.Line,
                    Lines(Line(s, "Indag-an village — our cooperative keeps the hablon alive, same as Tigbauan downcoast. The loom feeds the family and keeps the old knowing from dying. Cloth and memory, woven together.")),
                    returnToHub: true);
                c.nodes["BYE"] = Node("BYE", DialogueNodeKind.Line,
                    Lines(Line(s, "Padayon, anak. Roots this deep don't wash out.")));
                return c;
            }
        }
        return null;
    }

    // -------------------------------------------------------------------------
    // Level 5 — San Joaquin

    static DialogueConversation SanJoaquin(string npcId)
    {
        switch (npcId)
        {
            case "sj_keeper":
            {
                string s = "Manong Edring";
                var c = Convo(5, "sj_keeper");
                c.nodes["START"] = Node("START", DialogueNodeKind.Line,
                    Lines(Line(s, "End of the road, son. Everything here faces the sea — even up at the Campo Santo.")),
                    next: "HUB");
                c.nodes["HUB"] = Node("HUB", DialogueNodeKind.Hub,
                    Lines(Line(s, "Manong Edring leans on a worn stone wall.")),
                    choices: new[]
                    {
                        Choice("What's the church known for?", "T1", once: true),
                        Choice("Tell me about the Campo Santo.", "T2", once: true),
                        Choice("Thank you, manong.", "BYE")
                    });
                c.nodes["T1"] = Node("T1", DialogueNodeKind.Line,
                    Lines(Line(s, "A battle, of all things — soldiers and cavalry carved right across the front. The only church face like it in the whole country. Why a war on a church? That's a question worth holding onto.")),
                    returnToHub: true);
                c.nodes["T2"] = Node("T2", DialogueNodeKind.Line,
                    Lines(Line(s, "A baroque cemetery up the hill, a national treasure — old convent ruins beside it, a kiln, a round stone well. People think it's a sad place. It isn't. It's just a place that remembers.")),
                    returnToHub: true);
                c.nodes["BYE"] = Node("BYE", DialogueNodeKind.Line,
                    Lines(Line(s, "Rest easy, son. The sea's loud and kind here.")));
                return c;
            }
            case "sj_fisher":
            {
                string s = "Aling Cora";
                var c = Convo(5, "sj_fisher");
                c.nodes["START"] = Node("START", DialogueNodeKind.Line,
                    Lines(Line(s, "Made it all the way south, ha! Mind your step on the pebbles — the beach here is all smooth stones.")),
                    next: "HUB");
                c.nodes["HUB"] = Node("HUB", DialogueNodeKind.Hub,
                    Lines(Line(s, "Aling Cora spreads a net to dry in the breeze.")),
                    choices: new[]
                    {
                        Choice("What's life like out here?", "T1", once: true),
                        Choice("I'll let you work.", "BYE")
                    });
                c.nodes["T1"] = Node("T1", DialogueNodeKind.Line,
                    Lines(Line(s, "Quiet. Sea breeze, pebble beach, the quietest sunsets on the coast. People drive all this way just to watch the day end here. Can't blame them.")),
                    returnToHub: true);
                c.nodes["BYE"] = Node("BYE", DialogueNodeKind.Line,
                    Lines(Line(s, "Stay for the sunset if you can.")));
                return c;
            }
        }
        return null;
    }

    // -------------------------------------------------------------------------
    // Helpers (mirrors DialogueLibrary's authoring helpers; town convos carry no
    // reveal lines, no journal page, and an empty passengerId for authored text).

    static DialogueConversation Convo(int levelIndex, string npcId)
    {
        return new DialogueConversation
        {
            levelIndex   = levelIndex,
            passengerId  = "",        // empty → DialogueController delivers authored text (no AI)
            startNode    = "START",
            hubNode      = "HUB",
            journalPageId = -1,
        };
    }

    static DialogueLine Line(string speaker, string text)
        => new DialogueLine { speaker = speaker, text = text, affinity = 0, isReveal = false };

    static DialogueLine[] Lines(params DialogueLine[] lines) => lines;

    static DialogueChoice Choice(string label, string target, bool once = false, string[] requires = null)
        => new DialogueChoice
        {
            label = label,
            target = target,
            once = once,
            requires = requires ?? Array.Empty<string>(),
            unlocksEvent = DialogueEventKind.None,
            tone = DialogueTone.Curious,
            affinityDelta = 0
        };

    static DialogueNode Node(string id, DialogueNodeKind kind, DialogueLine[] lines,
                             DialogueChoice[] choices = null, string next = null,
                             bool returnToHub = false)
    {
        return new DialogueNode
        {
            id = id,
            kind = kind,
            lines = lines ?? Array.Empty<DialogueLine>(),
            choices = choices ?? Array.Empty<DialogueChoice>(),
            next = next,
            returnToHub = returnToHub,
            eventKind = DialogueEventKind.None,
            eventPayload = null
        };
    }
}
