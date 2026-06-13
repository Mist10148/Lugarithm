using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Static library of every speaking character along the Iloilo coastal route.
/// </summary>
public static class PassengerLibrary
{
    public static readonly IReadOnlyList<PassengerDefinition> All = BuildAll();

    static List<PassengerDefinition> BuildAll()
    {
        return new List<PassengerDefinition>
        {
            new PassengerDefinition
            {
                id = "gemma",
                levelIndex = 0,
                displayName = "Ate Gemma",
                speakerName = "Gemma",
                town = "Iloilo City",
                role = "Veteran jeepney dispatcher",
                background =
                    "Ate Gemma dispatched the coastal route with your father for twenty years. " +
                    "She knows every bump, every shortcut, and every passenger who still owes a fare. " +
                    "Now she is the one who gets you behind the wheel for the first time.",
                voice = "Sharp, warm, no-nonsense; teases like an aunt but never misses a teaching moment.",
                relationshipToFather = "His dispatcher and close friend — she covered for him when he drove off to leave things in towns down the coast."
            },

            new PassengerDefinition
            {
                id = "caring",
                levelIndex = 1,
                displayName = "Lola Caring",
                speakerName = "Lola Caring",
                town = "Molo",
                role = "Retired embroiderer and pancit Molo cook",
                background =
                    "A family friend who boards for the short hop into Molo district. " +
                    "Proud, maternal, and quietly fierce — she carries the women-of-the-family thread " +
                    "and remembers your grandmother as the iron backbone of the household.",
                voice = "Maternal and proud; speaks of food, church, and the old streets like she is polishing memory.",
                relationshipToFather = "Family friend who knew his mother and grandmother well."
            },

            new PassengerDefinition
            {
                id = "nicro",
                levelIndex = 2,
                displayName = "Lolo Nicro",
                speakerName = "Lolo Nicro",
                town = "Oton",
                role = "Retired shipwright / panday",
                background =
                    "A weathered old smith who once built sea-going boats in Oton and recognizes the jeepney's welds. " +
                    "He speaks sparingly, lets silence do work, and only hands over a fact once he thinks you have earned it.",
                voice = "Terse, weighty, withholds; every sentence feels carved rather than spoken.",
                relationshipToFather = "Knew the hands — the pandays — who shaped the first frames of your father's jeepney."
            },

            new PassengerDefinition
            {
                id = "delia",
                levelIndex = 3,
                displayName = "Manang Delia",
                speakerName = "Manang Delia",
                town = "Tigbauan",
                role = "Hablon handloom weaver",
                background =
                    "A patient weaver who explains pattern and repetition the way others explain prayer. " +
                    "Her town looks gentle, but she knows it has a harder thread running through it.",
                voice = "Calm, rhythmic, metaphor-in-craft; one quiet turn to steel when the war memory surfaces.",
                relationshipToFather = "Met him through the weaving cooperatives; he left a page with her."
            },

            new PassengerDefinition
            {
                id = "kardo",
                levelIndex = -1,
                displayName = "Lolo Kardo",
                speakerName = "Lolo Kardo",
                town = "Guimbal",
                role = "Father's old co-driver / mentor",
                background =
                    "The father's old co-driver, riding shotgun on the CB radio as a recurring voice. " +
                    "No puzzle here — just warm passing commentary as the golden town slides by.",
                voice = "Folksy, fond, road-worn; talks to you like you are already family.",
                relationshipToFather = "His co-driver and mentor for years along the southern route."
            },

            new PassengerDefinition
            {
                id = "sabel",
                levelIndex = 4,
                displayName = "Lola Sabel",
                speakerName = "Lola Sabel",
                town = "Miag-ao",
                role = "Elder of an Indag-an weaving cooperative; church devotee",
                background =
                    "An elder who recognizes the journal the moment she sees it. " +
                    "She speaks of roots, hablon, and the Miag-ao facade with the certainty of someone " +
                    "who has stood in front of it for decades.",
                voice = "Tender, devotional, knowing; treats heritage like a living relative.",
                relationshipToFather = "He showed her the journal once and told her his child would carry it the whole coast."
            },

            new PassengerDefinition
            {
                id = "tomas",
                levelIndex = 5,
                displayName = "Mang Tomas",
                speakerName = "Mang Tomas",
                town = "San Joaquin",
                role = "Campo Santo caretaker / local historian",
                background =
                    "The keeper of the San Joaquin Campo Santo and the last page of the journal. " +
                    "Slow, elegiac, and gentle — he has been waiting for you to finish the road.",
                voice = "Slow, elegiac, kind; speaks like surf at the end of a long beach.",
                relationshipToFather = "Trusted with the ending; your father talked about you more than you believed."
            }
        };
    }

    public static PassengerDefinition Get(string id)
        => All.FirstOrDefault(p => p.id == id);

    public static IReadOnlyList<PassengerDefinition> ForLevel(int levelIndex)
        => All.Where(p => p.levelIndex == levelIndex).ToList();
}
