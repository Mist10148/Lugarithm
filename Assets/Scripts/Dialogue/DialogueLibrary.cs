using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Static library of every branching conversation in the game, encoded node-for-node
/// from the dialogue script. The runtime state machine plays these; the AI fallback
/// may rephrase but may not assert facts outside these lines or the heritage dossier.
/// </summary>
public static class DialogueLibrary
{
    public static DialogueConversation ForLevel(int levelIndex)
    {
        return Get(levelIndex);
    }

    public static DialogueConversation Get(int levelIndex)
    {
        switch (levelIndex)
        {
            case 0: return Tutorial();
            case 1: return Molo();
            case 2: return Oton();
            case 3: return Tigbauan();
            case 4: return Miagao();
            case 5: return SanJoaquin();
            default: return null;
        }
    }

    public static DialogueConversation Guimbal()
    {
        string kardo = "Lolo Kardo";
        var convo = new DialogueConversation
        {
            levelIndex = -1,
            passengerId = "kardo",
            startNode = "G-START",
            hubNode = null,
            journalPageId = -1
        };

        convo.nodes["G-START"] = Node("G-START", DialogueNodeKind.Line,
            Lines(
                Line(kardo, "Pulling through Guimbal, ha? Slow down a second, iho, it's worth the look."),
                Line(kardo, "See that little bridge — Taytay Tigre. Spanish-built. Those stone tigers on the ends? Been guarding the way into town longer than anyone alive.")
            ),
            choices: new[]
            {
                Choice("Everything's so… golden here.", "G1", once: true),
                Choice("Keep driving.", "G-END")
            });

        convo.nodes["G1"] = Node("G1", DialogueNodeKind.Line,
            Lines(Line(kardo, "Coral stone, iho. Igang — they pulled it yellow from the shore and from Guimaras. Whole town's built warm because of it. You'll see the same stone up the road in Miag-ao. This coast shared its bones to build itself.")),
            next: "G-END");

        convo.nodes["G-END"] = Node("G-END", DialogueNodeKind.Event,
            Lines(
                Line(kardo, "Plaza there's the \"little Luneta,\" and that grand hall they call the Parthenon of the West."),
                Line(kardo, "Big names for a small town. Padayon, iho — Miag-ao's next, and that one'll stop your breath.")
            ),
            eventKind: DialogueEventKind.Continue);

        return convo;
    }

    // -------------------------------------------------------------------------

    static DialogueConversation Tutorial()
    {
        string gemma = "Gemma";
        var convo = new DialogueConversation
        {
            levelIndex = 0,
            passengerId = "gemma",
            startNode = "T-BOARD",
            hubNode = "HUB-T",
            journalPageId = 0
        };

        convo.nodes["T-BOARD"] = Node("T-BOARD", DialogueNodeKind.Line,
            Lines(
                Line(gemma, "Aba! So you finally fit behind your old man's wheel. Sit up straight, you look like you're about to faint."),
                Line(gemma, "I'm Gemma. I dispatched this route with your father for twenty years. He could thread this jeepney through Calle Real traffic with his eyes closed. You? We'll start slow.")
            ),
            next: "HUB-T");

        convo.nodes["HUB-T"] = Node("HUB-T", DialogueNodeKind.Hub,
            Lines(Line(gemma, "Ate Gemma is sizing you up.")),
            choices: new[]
            {
                Choice("Who were you to my father?", "T1", once: true),
                Choice("How do I even drive this thing?", "T2"),
                Choice("What's the deal with the coins?", "T3"),
                Choice("What is there to see around here?", "T4", once: true),
                Choice("I'm ready. Let's go.", "T-ADV", requires: new[] { "T2", "T3" })
            });

        convo.nodes["T1"] = Node("T1", DialogueNodeKind.Line,
            Lines(Line(gemma, "His dispatcher. His friend. The one who covered for him when he drove off to leave things in towns down the coast — wouldn't say what. You'll find out. That's the whole point of this trip, anó?", affinity: 1)),
            returnToHub: true);

        convo.nodes["T2"] = Node("T2", DialogueNodeKind.Branch,
            Lines(Line(gemma, "Hands on the wheel. The old roads here are slick — she'll keep sliding after you let go, so steer early, not late. Treat momentum like a passenger who doesn't want to stop.")),
            choices: new[]
            {
                Choice("Slide? That sounds dangerous.", "T2a"),
                Choice("Got it — let me try.", "T2b")
            });

        convo.nodes["T2a"] = Node("T2a", DialogueNodeKind.Line,
            Lines(Line(gemma, "It's a jeepney, not a banca. You won't sink. Worst case you bump and I laugh at you. Go on.")),
            next: "T2b");

        convo.nodes["T2b"] = Node("T2b", DialogueNodeKind.Event,
            Lines(Line(gemma, "Ha! Not bad. Your father bumped the curb his first day too. Don't tell anyone I told you.", affinity: 1)),
            eventKind: DialogueEventKind.DrivingTutorial,
            returnToHub: true);

        convo.nodes["T3"] = Node("T3", DialogueNodeKind.Event,
            Lines(
                Line(gemma, "A passenger pays, the coins drop in your drawer. You give back exact change before they lose patience — watch the little timer over their head. Short-change them and you'll hear about it."),
                Line(gemma, "Salamat! — see? A happy passenger is money in your pocket. Mess it up and it just dents your take. You never lose, you just earn less.")
            ),
            eventKind: DialogueEventKind.FareTutorial,
            returnToHub: true);

        convo.nodes["T4"] = Node("T4", DialogueNodeKind.Line,
            Lines(Line(gemma, "You're parked in a goldmine and you don't even know it. Those buildings down Calle Real — American-era, all of it, neoclassical, art deco, the works. And the food? In 2023 the whole city got named a UNESCO Creative City of Gastronomy. Batchoy, pancit Molo — namit gid.", affinity: 1)),
            returnToHub: true);

        convo.nodes["T-ADV"] = Node("T-ADV", DialogueNodeKind.Event,
            Lines(Line(gemma, "Look at you. Your first real stop is Molo, just across the district. Old friend of your father's is waiting — and so's the first page of that journal. Drive safe, iho. Padayon.")),
            eventKind: DialogueEventKind.TutorialComplete);

        convo.revealLines = new[]
        {
            Line(gemma, "One more thing before you go. Your father left this note on my desk the night before he stopped driving — said it was the key to the whole route. Read it when you're ready, iho. And don't let Molo rush you; that district has been waiting for you a long time.", isReveal: true)
        };

        return convo;
    }

    // -------------------------------------------------------------------------

    static DialogueConversation Molo()
    {
        string caring = "Lola Caring";
        var convo = new DialogueConversation
        {
            levelIndex = 1,
            passengerId = "caring",
            startNode = "M-BOARD",
            hubNode = "HUB-M",
            journalPageId = 1,
            assistHints = new[]
            {
                Line(caring, "These alleys twist like my embroidery thread. Keep one hand on the wall and you'll never be lost — follow it all the way to the end."),
                Line(caring, "There! Out the other side. Your father would be clapping."),
                Line(caring, "Bumped a wall? Pff. Try again, the wall isn't going anywhere.")
            }
        };

        convo.nodes["M-BOARD"] = Node("M-BOARD", DialogueNodeKind.Line,
            Lines(
                Line(caring, "Ay, the Causing jeepney! I'd know this rattle anywhere. And you must be the boy. You have your lola's eyes, did anyone ever tell you that? No? Of course not. Drive on, anak — Molo Plaza, please.")
            ),
            next: "HUB-M");

        convo.nodes["HUB-M"] = Node("HUB-M", DialogueNodeKind.Hub,
            Lines(Line(caring, "Lola Caring folds her hands, ready to talk.")),
            choices: new[]
            {
                Choice("You knew my grandmother?", "M1", once: true),
                Choice("What's special about Molo?", "M2"),
                Choice("Something smells amazing.", "M3", once: true),
                Choice("We're almost there.", "M-ADV")
            });

        convo.nodes["M1"] = Node("M1", DialogueNodeKind.Branch,
            Lines(Line(caring, "Knew her? I sat beside her in the pews of Molo Church every Sunday.")),
            choices: new[]
            {
                Choice("What's the church like?", "M1a"),
                Choice("What was she like?", "M1b")
            });

        convo.nodes["M1a"] = Node("M1a", DialogueNodeKind.Line,
            Lines(Line(caring, "They call it the women's church — the feminist church, the young ones say now. Every saint standing inside is a woman. Sixteen of them. Built for the strong women of this town. Your lola fit right in.")),
            returnToHub: true);

        convo.nodes["M1b"] = Node("M1b", DialogueNodeKind.Line,
            Lines(Line(caring, "Iron, that one. Soft voice, iron underneath. She kept things running when nobody thought a woman should. But that's for the page to tell you, not me.", affinity: 1)),
            returnToHub: true);

        convo.nodes["M2"] = Node("M2", DialogueNodeKind.Line,
            Lines(Line(caring, "Everything, if you have eyes. The old houses. The plaza. The church with all its lady saints. And the food — but you'll smell that before I finish the sentence.")),
            returnToHub: true);

        convo.nodes["M3"] = Node("M3", DialogueNodeKind.Line,
            Lines(Line(caring, "Pancit Molo. My recipe — well, your lola's recipe, really, I only borrowed it. This whole city got a UNESCO name for its cooking, you know. We've been feeding people properly for a hundred years.", affinity: 1)),
            returnToHub: true);

        convo.nodes["M-BRK"] = Node("M-BRK", DialogueNodeKind.Event,
            Lines(Line(caring, "Take your time, anak. Your father never rushed a repair either. A jeepney you love, you don't hurry.")),
            eventKind: DialogueEventKind.Breakdown,
            returnToHub: true);

        convo.nodes["M-ADV"] = Node("M-ADV", DialogueNodeKind.Event,
            Lines(
                Line(caring, "There — Molo Plaza, the church spires over the rooftops."),
                Line(caring, "The wind scattered your father's first page all over this square. Go on. Put it back together. I'll wait right here.")
            ),
            eventKind: DialogueEventKind.Arrive);

        convo.revealLines = new[]
        {
            Line(caring, "Ah. So he wrote it down after all. This city ran on cloth once, anak. Sixty thousand looms in the province, the records say — a whole economy of thread. And the ledgers, the trade, the keeping-it-all-alive when the men were away or gone… that was your grandmother. She ran it. Quietly. Like the saints in that church — no statue, but holding the whole place up.", isReveal: true)
        };

        return convo;
    }

    // -------------------------------------------------------------------------

    static DialogueConversation Oton()
    {
        string nicro = "Lolo Nicro";
        var convo = new DialogueConversation
        {
            levelIndex = 2,
            passengerId = "nicro",
            startNode = "O-BOARD",
            hubNode = "HUB-O",
            journalPageId = 2,
            assistHints = new[]
            {
                Line(nicro, "Heaviest on the bottom, smallest on top. Order is everything — a badly stacked load tips the whole jeepney. Same with your numbers: put them in their right place, one by one."),
                Line(nicro, "There. Eye-piece, nose-piece, set right. Look how it catches the light.")
            }
        };

        convo.nodes["O-BOARD"] = Node("O-BOARD", DialogueNodeKind.Line,
            Lines(
                Line(nicro, "Estacion San Antonio, by the old shipyards. …This welding. I'd know this chassis anywhere. This was old Causing's, wasn't it. Your father's."),
                Line(nicro, "Hm. Then sit. We'll talk. The road's long enough for it.")
            ),
            next: "HUB-O");

        convo.nodes["HUB-O"] = Node("HUB-O", DialogueNodeKind.Hub,
            Lines(Line(nicro, "Lolo Nicro watches the sea go by.")),
            choices: new[]
            {
                Choice("You knew this jeepney?", "O1", once: true),
                Choice("What kind of town is Oton?", "O2"),
                Choice("You said shipyards?", "O3"),
                Choice("There's your stop.", "O-ADV")
            });

        convo.nodes["O1"] = Node("O1", DialogueNodeKind.Line,
            Lines(Line(nicro, "I knew the hands that made the frame it grew from. Pandays. Smiths. We don't sign our work, but we know it. He won't say more — yet.", affinity: 1)),
            returnToHub: true);

        convo.nodes["O2"] = Node("O2", DialogueNodeKind.Branch,
            Lines(Line(nicro, "Older than it looks. Long before the Spanish, this was a port — between the rivers, ships in from China, ceramics, gold, trade. People think history started when the churches went up. History was sailing here already.")),
            choices: new[]
            {
                Choice("Gold?", "O2a"),
                Choice("What did they trade?", "O2b")
            });

        convo.nodes["O2a"] = Node("O2a", DialogueNodeKind.Line,
            Lines(Line(nicro, "Gold. They dug a death mask out of the ground at San Antonio — hammered gold, thin as a leaf, a piece for the eyes and a piece for the nose. Every May the town still covers their faces in gold paper to remember it. Beautiful thing. …There's a reason for the gold. But you should earn that one.")),
            returnToHub: true);

        convo.nodes["O2b"] = Node("O2b", DialogueNodeKind.Line,
            Lines(Line(nicro, "Pots. Ceramics from China and the lands south. Iron. And the gold, always the gold. This coast was rich in conversation long before it was rich in churches.", affinity: 1)),
            returnToHub: true);

        convo.nodes["O3"] = Node("O3", DialogueNodeKind.Line,
            Lines(Line(nicro, "Aye. Oton built sea-going boats when boats were how the world moved. Pandays of the sea — we beat iron and we shaped wood and we sent it out on the tide.")),
            returnToHub: true);

        convo.nodes["O-MNT"] = Node("O-MNT", DialogueNodeKind.Event,
            Lines(Line(nicro, "Top her off properly. A good machine fed badly still dies. Your great-grandfather used to say that. …Drive.")),
            eventKind: DialogueEventKind.Maintenance,
            returnToHub: true);

        convo.nodes["O-ADV"] = Node("O-ADV", DialogueNodeKind.Event,
            Lines(
                Line(nicro, "The old shipyard. Your father left a page with me, boy. Said one day his child would come down this road looking for answers."),
                Line(nicro, "Solve the market's puzzle first. Then come find me. I'll have it ready.")
            ),
            eventKind: DialogueEventKind.Arrive);

        convo.revealLines = new[]
        {
            Line(nicro, "You want to know the reason for the gold? They believed its brightness drove the evil spirits off — covered the eyes and nose so nothing dark could get in. And the more gold you were buried with, the higher you'd stood in life. Your great-grandfather understood metal like that. After the war he took the old boat-smith craft and bent it to something new — hammered out the first jeepney frames on this coast, by hand. The machine you're sitting in? It started with him. Panday ng dagat. Smith of the sea.", isReveal: true)
        };

        return convo;
    }

    // -------------------------------------------------------------------------

    static DialogueConversation Tigbauan()
    {
        string delia = "Manang Delia";
        var convo = new DialogueConversation
        {
            levelIndex = 3,
            passengerId = "delia",
            startNode = "TG-BOARD",
            hubNode = "HUB-TG",
            journalPageId = 3,
            assistHints = new[]
            {
                Line(delia, "Don't place every thread by hand — you'll be here till next fiesta. Find the rule. Write it once. Let it repeat the right number of times."),
                Line(delia, "There's the pattern! Clean as anything. You have the hands for this, anak.")
            }
        };

        convo.nodes["TG-BOARD"] = Node("TG-BOARD", DialogueNodeKind.Line,
            Lines(
                Line(delia, "Careful of the patadyong, anak, the dye's still setting. To the weaving village, if you please. Fiesta's coming and the looms haven't slept.")
            ),
            next: "HUB-TG");

        convo.nodes["HUB-TG"] = Node("HUB-TG", DialogueNodeKind.Hub,
            Lines(Line(delia, "Manang Delia's hands move even at rest, as if weaving.")),
            choices: new[]
            {
                Choice("What is hablon?", "TG1"),
                Choice("What's the cloth for?", "TG2", once: true),
                Choice("This town seems so quiet.", "TG3"),
                Choice("The looms are in sight.", "TG-ADV")
            });

        convo.nodes["TG1"] = Node("TG1", DialogueNodeKind.Branch,
            Lines(Line(delia, "Habol. It means 'to weave,' in Hiligaynon. The cloth took the name of the doing. Cotton, piña, jusi — pineapple and banana made into thread. Plaid, stripe, check. The pattern is just instructions, anak. Do this, then this, then this, and repeat — same as your father's old code, no?")),
            choices: new[]
            {
                Choice("Instructions?", "TG1a"),
                Choice("It's beautiful.", "TG1b")
            });

        convo.nodes["TG1a"] = Node("TG1a", DialogueNodeKind.Line,
            Lines(Line(delia, "A pattern is a loop. Three threads red, one white, again, again, a hundred times — and a blanket appears. Tell the loom the rule once, and let it repeat. You'll see, at the village.", affinity: 1)),
            returnToHub: true);

        convo.nodes["TG1b"] = Node("TG1b", DialogueNodeKind.Line,
            Lines(Line(delia, "Beauty with a memory in it. Every region has its own pattern; you can read where a person's from by what they wear. Cloth remembers.")),
            returnToHub: true);

        convo.nodes["TG2"] = Node("TG2", DialogueNodeKind.Line,
            Lines(Line(delia, "The fiesta! Banners, the patadyong, the good clothes. Tigbauan was weaving for the whole province back in the Spanish times — buyers still come from far for our hablon.", affinity: 1)),
            returnToHub: true);

        convo.nodes["TG3"] = Node("TG3", DialogueNodeKind.Line,
            Lines(Line(delia, "Quiet now. It wasn't always. This soft little weaving town has a harder thread in it than people expect. But that's a story for when you've earned the page, anak. Not for the road.")),
            returnToHub: true);

        convo.nodes["TG-MNT"] = Node("TG-MNT", DialogueNodeKind.Event,
            Lines(Line(delia, "A snapped thread isn't the end of the cloth. You tie it, you go on. Remember that. It's true of more than weaving.")),
            eventKind: DialogueEventKind.Maintenance,
            returnToHub: true);

        convo.nodes["TG-ADV"] = Node("TG-ADV", DialogueNodeKind.Event,
            Lines(
                Line(delia, "Listen — hear them? That sound hasn't stopped in two hundred years."),
                Line(delia, "Help me set the broken pattern right, and the page your father left is yours.")
            ),
            eventKind: DialogueEventKind.Arrive);

        convo.revealLines = new[]
        {
            Line(delia, "Now the harder thread. This quiet town fought, anak. In '42, on the road to Antique, our people ambushed a Japanese convoy — the first ambush, they call it. Ordinary folk. Weavers, farmers. The man in your father's page was one of them. He didn't live to see the country free. But what he did outlived him — same as a pattern outlives the weaver who set it. That's the whole secret of cloth. And of people.", isReveal: true)
        };

        return convo;
    }

    // -------------------------------------------------------------------------

    static DialogueConversation Miagao()
    {
        string sabel = "Lola Sabel";
        var convo = new DialogueConversation
        {
            levelIndex = 4,
            passengerId = "sabel",
            startNode = "MG-BOARD",
            hubNode = "HUB-MG",
            journalPageId = 4,
            assistHints = new[]
            {
                Line(sabel, "Not every stone goes in the same way. Check each one — its place, its layer, what sits behind it — then set it. One wrong question and the whole panel reads wrong."),
                Line(sabel, "The facade remembers itself. Look at it — whole again.")
            }
        };

        convo.nodes["MG-BOARD"] = Node("MG-BOARD", DialogueNodeKind.Line,
            Lines(
                Line(sabel, "…That book. Ang Aking Mga Ugat. I never thought I'd see it again. You're his. Of course you are. Drive to the church, anak. There's something there you need to stand in front of.")
            ),
            next: "HUB-MG");

        convo.nodes["HUB-MG"] = Node("HUB-MG", DialogueNodeKind.Hub,
            Lines(Line(sabel, "Lola Sabel keeps glancing at the journal.")),
            choices: new[]
            {
                Choice("You recognize the journal?", "MG1", once: true),
                Choice("Tell me about the church.", "MG2"),
                Choice("You're a weaver too?", "MG3", once: true),
                Choice("The facade — I can see it now.", "MG-ADV")
            });

        convo.nodes["MG1"] = Node("MG1", DialogueNodeKind.Line,
            Lines(Line(sabel, "Your father showed it to me once, half-empty already, pages given away. He said his child would carry it the whole coast one day. And here you are, in front of me, with his eyes. Forgive an old woman.", affinity: 1)),
            returnToHub: true);

        convo.nodes["MG2"] = Node("MG2", DialogueNodeKind.Branch,
            Lines(Line(sabel, "Miag-ao. UNESCO calls it a treasure of the world, but we built it as a fort. Walls thick as a man is tall, towers to watch the sea — the raiders came in the old days, and the church was the town's shield.")),
            choices: new[]
            {
                Choice("A church that's a fortress?", "MG2a"),
                Choice("What's carved on the front?", "MG2b")
            });

        convo.nodes["MG2a"] = Node("MG2a", DialogueNodeKind.Line,
            Lines(Line(sabel, "Faith and fear in the same stone. They prayed in it and they hid in it. A building can do more than one thing. So can a person.")),
            returnToHub: true);

        convo.nodes["MG2b"] = Node("MG2b", DialogueNodeKind.Line,
            Lines(Line(sabel, "A coconut tree, right up the middle of the facade — the tree of life, reaching for heaven. Around it, our flowers, our birds, our everyday world, all in stone. There's a figure up there too, but… stand in front of him yourself. I won't spoil it.", affinity: 1)),
            returnToHub: true);

        convo.nodes["MG3"] = Node("MG3", DialogueNodeKind.Line,
            Lines(Line(sabel, "Indag-an village — our cooperative keeps the hablon alive here, same as Tigbauan downcoast. The loom feeds the family and keeps the old knowing from dying. Same idea as that journal of yours, no?")),
            returnToHub: true);

        convo.nodes["MG-MNT"] = Node("MG-MNT", DialogueNodeKind.Event,
            Lines(Line(sabel, "Balance it gently. They restore that facade the same way — one careful weight at a time. Rush, and you lose what you were trying to save.")),
            eventKind: DialogueEventKind.Maintenance,
            returnToHub: true);

        convo.nodes["MG-ADV"] = Node("MG-ADV", DialogueNodeKind.Event,
            Lines(
                Line(sabel, "There. Let it be big for a moment."),
                Line(sabel, "Help the curator set the carvings right. Then come stand with me at the front. There's a man in the stone I want you to meet.")
            ),
            eventKind: DialogueEventKind.Arrive);

        convo.revealLines = new[]
        {
            Line(sabel, "There he is. St. Christopher, carrying the Child Jesus across the river. But look how they carved him — not in robes, not Spanish. In our clothes. Barefoot. Like a man from this town. The carvers were told to make European saints, anak. Instead they put themselves into the stone — their tree, their clothes, their world — right on the front of God's house. They wrote themselves into the art so no one could ever say they weren't here. Your family is in that stone the same way. Roots this deep don't wash out.", isReveal: true)
        };

        return convo;
    }

    // -------------------------------------------------------------------------

    static DialogueConversation SanJoaquin()
    {
        string tomas = "Mang Tomas";
        var convo = new DialogueConversation
        {
            levelIndex = 5,
            passengerId = "tomas",
            startNode = "SJ-BOARD",
            hubNode = "HUB-SJ",
            journalPageId = 5,
            assistHints = new[]
            {
                Line(tomas, "You can't watch only one thing now. Fuel, the stops, the safe path — all at once. The whole journey's been teaching you to hold more than one thing in your head. This is where it counts."),
                Line(tomas, "You made it up. Of course you did.")
            }
        };

        convo.nodes["SJ-BOARD"] = Node("SJ-BOARD", DialogueNodeKind.Line,
            Lines(
                Line(tomas, "So you made it all the way down. I'm Tomas. I keep the Campo Santo — the old cemetery up by the church. I've been keeping the last page for you a long time, son. Let's not rush the last stretch.")
            ),
            next: "HUB-SJ");

        convo.nodes["HUB-SJ"] = Node("HUB-SJ", DialogueNodeKind.Hub,
            Lines(Line(tomas, "Mang Tomas watches the sea out the window.")),
            choices: new[]
            {
                Choice("What's the church known for?", "SJ1"),
                Choice("Tell me about the Campo Santo.", "SJ2", once: true),
                Choice("Did you know my father well?", "SJ3", once: true),
                Choice("I think we're here.", "SJ-ADV")
            });

        convo.nodes["SJ1"] = Node("SJ1", DialogueNodeKind.Branch,
            Lines(Line(tomas, "For a war, of all things. Carved right across the front — soldiers, cavalry, a battle. The surrender of Tetuán, a Spanish fight against Moroccan forces, an ocean away from here. The most warlike church face in the whole country, they say.")),
            choices: new[]
            {
                Choice("Why carve a foreign war on a church?", "SJ1a"),
                Choice("Who built it?", "SJ1b")
            });

        convo.nodes["SJ1a"] = Node("SJ1a", DialogueNodeKind.Line,
            Lines(Line(tomas, "That… is the right question. And it has a reason — a very personal one. But it belongs with the last page, not before it. Be patient with me, son. We're almost there.")),
            returnToHub: true);

        convo.nodes["SJ1b"] = Node("SJ1b", DialogueNodeKind.Line,
            Lines(Line(tomas, "The friars, the locals, stone by stone over years. Faced the whole thing toward the sea. Everything here looks at the sea. The dead too.")),
            returnToHub: true);

        convo.nodes["SJ2"] = Node("SJ2", DialogueNodeKind.Line,
            Lines(Line(tomas, "A baroque cemetery, Spanish-era, declared a national treasure back in 2015. There's the old convent ruins beside it — a kiln, a round stone well. People think a cemetery is a sad place. I think it's just a place that remembers. That's my whole job. Remembering.", affinity: 1)),
            returnToHub: true);

        convo.nodes["SJ3"] = Node("SJ3", DialogueNodeKind.Line,
            Lines(Line(tomas, "Well enough that he trusted me with the ending. He talked about you more than you'd believe, for a man you thought didn't know you. The page will say it better than I can.", affinity: 1)),
            returnToHub: true);

        convo.nodes["SJ-MNT"] = Node("SJ-MNT", DialogueNodeKind.Event,
            Lines(Line(tomas, "Easy on her now. She's carried you the whole coast. Let her arrive with some dignity.")),
            eventKind: DialogueEventKind.Maintenance,
            returnToHub: true);

        convo.nodes["SJ-ADV"] = Node("SJ-ADV", DialogueNodeKind.Event,
            Lines(
                Line(tomas, "The end of the road, son."),
                Line(tomas, "Plan your way up to the Campo Santo carefully — it's a hard climb with a tired engine. The last page is waiting at the top. So is your father, in the only way he still can be.")
            ),
            eventKind: DialogueEventKind.Arrive);

        convo.revealLines = new[]
        {
            Line(tomas, "You asked why a town would carve a foreign war on its church. The friar who built it — they say he did it for his father. His father fought in that battle, and the son carved the victory in stone so the old man would never be forgotten. A whole church facade… as a message from a son to a father. Your father knew that story. I think it's why he chose this place for the last page.", isReveal: true),
            Line(tomas, "Go home, son. Take all of it with you.", isReveal: true)
        };

        return convo;
    }

    // -------------------------------------------------------------------------
    // Helpers

    static DialogueLine Line(string speaker, string text, int affinity = 0, bool isReveal = false)
        => new DialogueLine { speaker = speaker, text = text, affinity = affinity, isReveal = isReveal };

    static DialogueLine[] Lines(params DialogueLine[] lines) => lines;

    static DialogueChoice Choice(string label, string target, bool once = false,
                                 string[] requires = null, DialogueEventKind unlocksEvent = DialogueEventKind.None)
        => new DialogueChoice
        {
            label = label,
            target = target,
            once = once,
            requires = requires ?? Array.Empty<string>(),
            unlocksEvent = unlocksEvent
        };

    static DialogueNode Node(string id, DialogueNodeKind kind, DialogueLine[] lines,
                             DialogueChoice[] choices = null, string next = null,
                             bool returnToHub = false,
                             DialogueEventKind eventKind = DialogueEventKind.None,
                             string eventPayload = null)
    {
        return new DialogueNode
        {
            id = id,
            kind = kind,
            lines = lines ?? Array.Empty<DialogueLine>(),
            choices = choices ?? Array.Empty<DialogueChoice>(),
            next = next,
            returnToHub = returnToHub,
            eventKind = eventKind,
            eventPayload = eventPayload
        };
    }
}
