using System.Collections.Generic;

/// <summary>
/// Static registry of purchasable code editor themes. Mirrors the pattern used
/// by BadgeLibrary for badges.
/// </summary>
public static class CodeThemeLibrary
{
    public static readonly List<CodeTheme> Themes = new List<CodeTheme>
    {
        CodeTheme.DarkPlus,
        CodeTheme.Neon,
        CodeTheme.Amber,
        CodeTheme.Solarized,
        CodeTheme.Mono,
    };

    static readonly Dictionary<int, CodeTheme> ById = new Dictionary<int, CodeTheme>();

    static CodeThemeLibrary()
    {
        foreach (CodeTheme theme in Themes)
            ById[theme.id] = theme;
    }

    public static CodeTheme Get(int id)
    {
        return ById.TryGetValue(id, out CodeTheme theme) ? theme : Themes[0];
    }

    public static bool Exists(int id) => ById.ContainsKey(id);

    public static int Count => Themes.Count;
}
