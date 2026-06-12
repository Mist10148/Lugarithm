/// <summary>
/// Static library of all town-completion badges. Indices match level indexes
/// (0 = Tutorial, 1 = Iloilo City/Molo, … 5 = San Joaquin).
/// </summary>
public static class BadgeLibrary
{
    static BadgeDefinition[] _all;

    public static BadgeDefinition Get(int levelIndex)
    {
        if (_all == null) _all = Build();
        if (levelIndex < 0 || levelIndex >= _all.Length) return null;
        return _all[levelIndex];
    }

    static BadgeDefinition[] Build() => new[]
    {
        new BadgeDefinition { levelIndex = 0, badgeName = "Bagong Simula",     townName = "Tutorial",            description = "Every journey starts with a single turn of the key." },
        new BadgeDefinition { levelIndex = 1, badgeName = "Anak ng Molo",      townName = "Iloilo City (Molo)",  description = "You walked the streets where the matriarchs still stand." },
        new BadgeDefinition { levelIndex = 2, badgeName = "Panday ng Dagat",   townName = "Oton",                description = "The Batiano River remembers every hull that crossed it." },
        new BadgeDefinition { levelIndex = 3, badgeName = "Mangangukit",       townName = "Tigbauan",            description = "Coral stone holds the names of those who carved them." },
        new BadgeDefinition { levelIndex = 4, badgeName = "UNESCO Keeper",     townName = "Miag-ao",             description = "A coconut tree carved in stone outlasts a hundred seasons." },
        new BadgeDefinition { levelIndex = 5, badgeName = "Tagapagtanggol",    townName = "San Joaquin",         description = "You found the last page. The road was always the answer." },
    };
}
