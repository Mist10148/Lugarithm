using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Static library of sourced heritage for every stop on the Iloilo coastal route.
/// </summary>
public static class HeritageLibrary
{
    public static readonly IReadOnlyList<HeritageEntry> All = BuildAll();

    public static readonly IReadOnlyList<string> RouteOrder = new[]
    {
        "iloilo-molo",
        "oton",
        "tigbauan",
        "guimbal",
        "miag-ao",
        "san-joaquin"
    };

    static List<HeritageEntry> BuildAll()
    {
        return new List<HeritageEntry>
        {
            new HeritageEntry
            {
                townKey = "iloilo-molo",
                levelIndex = 0,
                townName = "Iloilo City — Molo",
                theme = "The textile capital and colonial crossroads",
                gameRole = "Tutorial level + Level 1 (\"The Alley Escape\")",
                eraTouched = "Pre-colonial trade port → Spanish colonial → American era → present",
                signatureSite = "Molo Church (Santa Ana Parish)",
                beyondTheChurch =
                    "Calle Real heritage district; the textile-then-sugar port economy; Fort San Pedro; " +
                    "Dinagyang Festival; UNESCO Creative City of Gastronomy (2023)",
                mood = "Bustling, proud, urban; batchoy and pancit Molo in the air",
                codingConceptAnchor = "Sequencing → conditionals",
                keyFacts = new[]
                {
                    new HeritageFact
                    {
                        headline = "Molo Church — the \"women's church\"",
                        detail =
                            "Santa Ana Parish was begun in 1831 under Fray Pablo Montaño and completed in 1888 " +
                            "under Fray Agapito Buenaflor, supervised by Don José Manuel Locsin. Built of white coral stone " +
                            "and limestone bound with egg-white-and-sand mortar; crowned with twin pointed red spires. " +
                            "Dr. José Rizal visited in 1886; declared a national landmark in 1992.",
                        holdForReveal = false
                    },
                    new HeritageFact
                    {
                        headline = "Sixteen female saints",
                        detail =
                            "Inside Molo Church, besides patroness St. Anne, stand life-sized statues of sixteen female saints — " +
                            "earning it the nickname the women's or feminist church.",
                        holdForReveal = false
                    },
                    new HeritageFact
                    {
                        headline = "From textile capital to sugar port",
                        detail =
                            "Iloilo was the country's textile center, with over 50,000 looms in the province before the British arrived. " +
                            "Nicholas Loney, first British vice-consul (1856–1869), pivoted the economy toward Negros sugar by importing " +
                            "steam-powered mills on credit to Filipino planters.",
                        holdForReveal = true
                    },
                    new HeritageFact
                    {
                        headline = "Calle Real",
                        detail =
                            "J.M. Basa Street — Calle Real — is an American-era heritage commercial strip lined with neoclassical, " +
                            "beaux-arts, and art deco buildings.",
                        holdForReveal = false
                    },
                    new HeritageFact
                    {
                        headline = "Fort San Pedro",
                        detail =
                            "Built in 1602 by Pedro Bravo de Acuña to defend against Moro and Dutch raids; destroyed in World War II.",
                        holdForReveal = false
                    },
                    new HeritageFact
                    {
                        headline = "Dinagyang Festival",
                        detail =
                            "Began in 1967 when a replica of the Santo Niño de Cebu was brought to San José Parish and received with " +
                            "Ati-Atihan-style street dancing.",
                        holdForReveal = false
                    },
                    new HeritageFact
                    {
                        headline = "UNESCO Creative City of Gastronomy",
                        detail =
                            "In October 2023 Iloilo City became the Philippines' first UNESCO Creative City of Gastronomy, recognizing " +
                            "its living food culture.",
                        holdForReveal = false
                    }
                },
                driveSpend = "The women's church, Molo food pride, the old streets of Calle Real.",
                reveal = "The textile economy — over 50,000 looms and the cloth-to-sugar pivot — ties to the grandmother who ran the family's trade logistics.",
                sources = new[]
                {
                    "https://www.iloiloph.com/molo-church/",
                    "https://getaway.ph/blog/travel/why-the-molo-church-in-iloilo-is-called-the-womens-church/",
                    "https://www.exploreiloilo.com/do/info/molo-church/",
                    "https://tulay.ph/2017/04/25/the-rise-and-fall-of-chinese-textile-business-in-iloilo/",
                    "https://www.encyclopedia.com/history/news-wires-white-papers-and-books/loney-nicholas-1828-1869",
                    "https://www.yodisphere.com/2023/10/Iloilo-City-History-Heritage-Sites.html",
                    "https://www.rappler.com/life-and-style/travel/ilonggo-notes-city-cultural-heritage-tourism-zones-iloilo/",
                    "https://experiencewesternvisayas.com/destinations/iloilocity/",
                    "https://www.philstar.com/headlines/2023/11/03/2308524/iloilo-city-named-unesco-creative-city-of-gastronomy",
                    "https://www.unesco.org/en/creative-cities/iloilo-city"
                }
            },

            new HeritageEntry
            {
                townKey = "oton",
                levelIndex = 2,
                townName = "Oton",
                theme = "The first pueblo and the Gold Death Mask",
                gameRole = "Level 2 (\"The Market Cargo\")",
                eraTouched = "Protohistoric / pre-colonial Age of Trade → early Spanish",
                signatureSite = "The Oton Gold Death Mask (National Cultural Treasure)",
                beyondTheChurch =
                    "Katagman burial customs; pre-colonial maritime trade with China; the Iloilo–Batiano river port",
                mood = "Salt air, old shipyards, artisans, deep antiquity",
                codingConceptAnchor = "List indexing / array sorting",
                keyFacts = new[]
                {
                    new HeritageFact
                    {
                        headline = "Oton Gold Death Mask",
                        detail =
                            "Hammered gold foil cut into two leaf-shaped eye pieces joined by a strip over the nose, with repoussé dot decoration. " +
                            "Found in situ on June 5, 1967, Grave #6 at the Mediavilla property, Barangay San Antonio (Katagman), by National Museum " +
                            "anthropologist Dr. Alfredo E. Evangelista with UP Diliman's Dr. F. Landa Jocano. Dated late 14th–early 15th century; " +
                            "now a National Cultural Treasure at the National Museum – Western Visayas.",
                        holdForReveal = false
                    },
                    new HeritageFact
                    {
                        headline = "Why gold?",
                        detail =
                            "Early Bisayans believed gold's brightness drove evil spirits away. Gold coverings over the eyes, nose, and mouth " +
                            "protected the dead, and the more gold buried with a person, the higher their rank in life.",
                        holdForReveal = true
                    },
                    new HeritageFact
                    {
                        headline = "Katagman — a pre-colonial port",
                        detail =
                            "A protohistoric port settlement between the Iloilo and Batiano rivers, one of the oldest and most important seaports " +
                            "of the late 1300s–early 1400s. Chinese and Southeast Asian ceramics found locally attest to trade with China and neighbors.",
                        holdForReveal = false
                    },
                    new HeritageFact
                    {
                        headline = "Katagman Festival",
                        detail =
                            "Every May, Oton holds the Katagman Festival; students wear costumes with eyes and noses covered in gold-colored paper " +
                            "to remember the burial site where the mask was found.",
                        holdForReveal = false
                    }
                },
                driveSpend = "The market, the panday / shipyard tradition, the old port between two rivers.",
                reveal = "The meaning of the gold mask — gold repels evil spirits and signals rank — rhymes with the great-grandfather who forged the first jeepney frames by hand.",
                sources = new[]
                {
                    "https://www.nationalmuseum.gov.ph/exhibitions/nm-western-visayas-regional-museum/oton-gold-death-mask-gallery/",
                    "https://verafiles.org/articles/oton-death-mask-celebrating-the-afterlife",
                    "https://www.yodisphere.com/2023/01/Oton-Gold-Death-Mask-Iloilo.html",
                    "https://dailyguardian.com.ph/oton-gold-death-mask-is-coming-home/",
                    "https://www.festivalscape.com/philippines/iloilo/katagman-festival/"
                }
            },

            new HeritageEntry
            {
                townKey = "tigbauan",
                levelIndex = 3,
                townName = "Tigbauan",
                theme = "Hablon weaving, the first school for boys, and the guerrilla town",
                gameRole = "Level 3 (\"The Weaver's Pattern\")",
                eraTouched = "Early Spanish (Jesuit mission) → 19th-c. textile boom → WWII → present",
                signatureSite = "San Juan de Sahagun Church (National Cultural Treasure facade)",
                beyondTheChurch =
                    "First school for boys in the Philippines (1592); WWII guerrilla resistance; Bantayan Watch Tower",
                mood = "Rhythmic looms, fiesta textiles, a quiet town with a sharper history",
                codingConceptAnchor = "Functions + loops (pattern repetition)",
                keyFacts = new[]
                {
                    new HeritageFact
                    {
                        headline = "First school for boys (1592)",
                        detail =
                            "Jesuit Pedro Chirino established a school for boys in Tigbauan in 1592, teaching catechism, reading, writing, Spanish, " +
                            "and liturgical music, followed in 1593–94 by what is described as the first Jesuit boarding school in the Philippines. " +
                            "(Some historians argue the location may have been Suaraga, present-day San Joaquin.)",
                        holdForReveal = false
                    },
                    new HeritageFact
                    {
                        headline = "Hablon weaving",
                        detail =
                            "From the Hiligaynon habol, \"to weave.\" Handloom cloth of cotton, piña, jusi, and abaca in plaid, striped, and checkered patterns. " +
                            "The multi-step process — warp, beam, heddle/pedal, shuttle, reed — is a literal instruction set that repeats.",
                        holdForReveal = false
                    },
                    new HeritageFact
                    {
                        headline = "WWII guerrilla resistance",
                        detail =
                            "The First Ambush Marker in Barangay Namocon commemorates the September 2, 1942 ambush of a Japanese convoy by local guerrillas. " +
                            "Also remembered: the Panay Landing Memorial (Parara) and the Bantayan Watch Tower.",
                        holdForReveal = true
                    },
                    new HeritageFact
                    {
                        headline = "San Juan de Sahagun Church",
                        detail =
                            "Churrigueresque style, completed in 1867 using reddish coral limestone; its richly carved facade was declared a National Cultural Treasure " +
                            "by the National Museum on June 27, 2019.",
                        holdForReveal = false
                    }
                },
                driveSpend = "Hablon weaving, the word habol, fiesta textiles, and the loom as instruction.",
                reveal = "The WWII guerrilla resistance — the First Ambush of 1942 — reveals that this quiet weaving town was also a town of fighters, like an ancestor whose work outlived them.",
                sources = new[]
                {
                    "https://www.theoldchurches.com/philippines/iloilo/tigbauan/san-juan-de-sahagun-church/",
                    "https://discoverthebeautyofonetigbauan.wordpress.com/2018/10/15/first-jesuit-boarding-school-for-boys/",
                    "https://myphilippinelife.com/the-jesuits-in-tigbauan-iloilo/",
                    "http://www.iloilo.gov.ph/iloilos-firsts",
                    "https://discoverthebeautyofonetigbauan.wordpress.com/2018/10/16/first-ambush/",
                    "https://pinasculture.com/hablon-textiles-reviving-iloilos-ancient-weaving-tradition/",
                    "https://outoftownblog.com/the-art-of-hablon-weaving-in-iloilo/",
                    "https://www.bworldonline.com/editors-picks/2019/05/13/230436/weaving-new-life-into-iloilos-hablon/"
                }
            },

            new HeritageEntry
            {
                townKey = "guimbal",
                levelIndex = -1,
                townName = "Guimbal",
                theme = "The \"agong\" town — Spanish civil infrastructure and golden coral stone",
                gameRole = "Scenic drive-through between Tigbauan and Miag-ao; no puzzle in v1",
                eraTouched = "Spanish colonial",
                signatureSite = "Taytay Tigre bridge; San Nicolas de Tolentino Church",
                beyondTheChurch =
                    "\"Little Luneta\" plaza; \"Parthenon of Western Visayas\" municipal hall; yellow coral-stone (igang) material culture",
                mood = "A pretty, golden-stoned town passing by the window",
                codingConceptAnchor = "—",
                keyFacts = new[]
                {
                    new HeritageFact
                    {
                        headline = "A name from the agong",
                        detail =
                            "\"Guimbal\" is said to come from the agong, the gong-like instrument early settlers beat to warn of Moro raids. " +
                            "The Spaniards heard \"cymbal\"; locals rendered it \"guimbal.\"",
                        holdForReveal = false
                    },
                    new HeritageFact
                    {
                        headline = "The golden church",
                        detail =
                            "San Nicolás de Tolentino Church: parish 1580; present stone church and convent built under Fr. Juan Campos c. 1769–1774 of yellow sandstone / coral rock " +
                            "called igang, quarried from Guimaras. Its four-storey belfry doubled as a watchtower.",
                        holdForReveal = false
                    },
                    new HeritageFact
                    {
                        headline = "Taytay Tigre",
                        detail =
                            "A short Spanish-colonial bridge popularly called \"Taytay Tigre\" for the tiger stone figures guarding both approaches.",
                        holdForReveal = false
                    },
                    new HeritageFact
                    {
                        headline = "Civic pride",
                        detail =
                            "The town plaza is called the \"little Luneta of southern Iloilo,\" and the municipal building has been nicknamed the \"Parthenon of Western Visayas.\"",
                        holdForReveal = false
                    }
                },
                driveSpend = "The agong name, the golden igang stone, Taytay Tigre, and the little Luneta.",
                reveal = "Guimbal has no dedicated reveal; it plants the coral-stone motif that pays off at Miag-ao.",
                sources = new[]
                {
                    "https://www.theoldchurches.com/philippines/iloilo/guimbal/san-nicolas-de-tolentino-church/",
                    "https://www.siranglente.com/2016/01/guimbal-church-ilo-ilo-visit-travel-itinerary.html",
                    "https://ubasnamaycyanide.wixsite.com/philippine-towns-ety/post/guimbal-iloilo-etymology",
                    "https://www.angelfire.com/linux/guimbal_csed/LANDMARKS.htm"
                }
            },

            new HeritageEntry
            {
                townKey = "miag-ao",
                levelIndex = 4,
                townName = "Miag-ao",
                theme = "Where Filipino artisans rewrote Catholic imagery",
                gameRole = "Level 4 (\"The Stone Fortress\")",
                eraTouched = "Spanish colonial (defensive era)",
                signatureSite = "Church of Santo Tomás de Villanueva — UNESCO World Heritage",
                beyondTheChurch =
                    "The bas-relief facade's indigenous iconography; hablon weaving cooperatives (Indag-an)",
                mood = "Monumental, golden-stoned, layered with meaning",
                codingConceptAnchor = "Nested conditionals + tracking variables",
                keyFacts = new[]
                {
                    new HeritageFact
                    {
                        headline = "A fortress that is a church",
                        detail =
                            "The Church of Santo Tomás de Villanueva was built by Spanish Augustinians beginning 1787 (often given as completed 1797) " +
                            "to double as a defensive structure against Muslim raiders. Foundation 6 m deep, walls ~1.5 m thick, flying buttresses ~4 m thick — " +
                            "per Royal Decree 111 of 1573 (Law of the Indies).",
                        holdForReveal = false
                    },
                    new HeritageFact
                    {
                        headline = "UNESCO World Heritage",
                        detail =
                            "Inscribed on the UNESCO World Heritage List in 1993 as one of the four Baroque Churches of the Philippines.",
                        holdForReveal = false
                    },
                    new HeritageFact
                    {
                        headline = "Two unequal towers",
                        detail =
                            "The facade is flanked by twin belfries of different heights — one two storeys, the other three — built in different periods.",
                        holdForReveal = false
                    },
                    new HeritageFact
                    {
                        headline = "The facade in Filipino terms",
                        detail =
                            "The bas-relief centers on St. Thomas of Villanova; around him climbs a coconut \"tree of life\" with St. Christopher — dressed in local everyday clothing — " +
                            "carrying the Child Jesus, surrounded by papaya and guava trees, native flora and fauna, and scenes of daily life. " +
                            "Filipino craftsmen put their own world into European religious art.",
                        holdForReveal = true
                    },
                    new HeritageFact
                    {
                        headline = "Living hablon",
                        detail =
                            "Miag-ao is a recognized hablon hub; weaving cooperatives, notably in Indag-an, sustain livelihood and cultural continuity.",
                        holdForReveal = false
                    }
                },
                driveSpend = "The coconut tree of life, the fortress-church purpose, the Indag-an weaving cooperatives.",
                reveal = "St. Christopher carved in local dress — Filipinos wrote themselves into the art — becomes the keystone about the family's deep roots in the region.",
                sources = new[]
                {
                    "https://www.theoldchurches.com/philippines/iloilo/miagao/santo-tomas-de-villanueva-church/",
                    "https://www.miagao.gov.ph/about-miagao/religious-heritage/santo-tomas-de-villanueva-miagao-iloilo/",
                    "https://intrepidwanderer.com/2013/11/miag-aos-church-of-santo-tomas-de-villanueva-one-of-the-baroque-churches-of-the-philippines/",
                    "https://whc.unesco.org/en/list/677/",
                    "https://fameplus.com/touchpoint/the-resurgence-of-miagaos-hablon"
                }
            },

            new HeritageEntry
            {
                townKey = "san-joaquin",
                levelIndex = 5,
                townName = "San Joaquin",
                theme = "The church that celebrated a war — and the cemetery that holds the last page",
                gameRole = "Level 5 (\"The Final Road\") + finale",
                eraTouched = "Spanish colonial (mid–late 19th c.)",
                signatureSite = "San Joaquin Church + the Campo Santo cemetery",
                beyondTheChurch =
                    "Augustinian convent ruins (kiln, round water well); the sea-facing complex",
                mood = "Dramatic, sea-swept, elegiac — the journey's end",
                codingConceptAnchor = "Multi-variable constraints",
                keyFacts = new[]
                {
                    new HeritageFact
                    {
                        headline = "The militaristic facade",
                        detail =
                            "San Joaquin Church was erected by Fr. Tomás Santarén, OSA, and inaugurated in 1869, built of white coral rock from nearby shores and Igbaras. " +
                            "Its oversized pediment carries the famous \"Rendición de Tetuán\" battle relief, depicting the Spanish victory over Moroccan forces at the Battle of Tetuán.",
                        holdForReveal = false
                    },
                    new HeritageFact
                    {
                        headline = "Why a foreign war on a Filipino church?",
                        detail =
                            "Tradition holds the Tetuán relief was a personal tribute connected to Santarén's father, said to have served in the campaign. " +
                            "A son carved his father's war into stone — a message from a son to a father.",
                        holdForReveal = true
                    },
                    new HeritageFact
                    {
                        headline = "National Cultural Treasure / national shrine",
                        detail =
                            "San Joaquin Church is considered the most militaristic church facade in the Philippines and is a National Cultural Treasure / national shrine.",
                        holdForReveal = false
                    },
                    new HeritageFact
                    {
                        headline = "Convent and the sea",
                        detail =
                            "Remnants of the old convent include a kiln and a round water well; the whole complex faces the sea.",
                        holdForReveal = false
                    },
                    new HeritageFact
                    {
                        headline = "Campo Santo",
                        detail =
                            "About a kilometer east stands the San Joaquin Campo Santo, a Spanish-era Baroque cemetery built by Fr. Mariano Vamba, OSA, in 1892. " +
                            "Dominated by an octagonal coral-stone and red-brick chapel approached up a grand stone staircase; declared a National Cultural Treasure in December 2015.",
                        holdForReveal = false
                    }
                },
                driveSpend = "The dramatic battle facade, the sea-facing complex, the road ending at the Campo Santo.",
                reveal = "The reason the war is carved there — a son's tribute to a father — mirrors the father's final letter and the whole game's frame.",
                sources = new[]
                {
                    "https://www.iias.asia/the-newsletter/article/facade-san-joaquin-church",
                    "https://www.theoldchurches.com/philippines/iloilo/san-joaquin/san-joaquin-church/",
                    "https://lifestyle.inquirer.net/331886/philippines-most-militaristic-church-marks-150th-year/",
                    "https://pia.gov.ph/features/revisiting-san-joaquins-national-cultural-treasures/",
                    "https://lifestyle.inquirer.net/243122/national-museum-restores-desecrated-campo-santo-of-san-joaquin-in-iloilo/"
                }
            }
        };
    }

    public static HeritageEntry Get(string townKey)
        => All.FirstOrDefault(e => e.townKey == townKey);

    public static HeritageEntry ForLevel(int levelIndex)
    {
        // Molo district heritage is shared by the Tutorial (0) and Level 1 (1).
        if (levelIndex == 1) levelIndex = 0;
        return All.FirstOrDefault(e => e.levelIndex == levelIndex);
    }

    public static int RouteIndexOf(string townKey)
    {
        for (int i = 0; i < RouteOrder.Count; i++)
            if (RouteOrder[i] == townKey)
                return i;
        return -1;
    }
}
