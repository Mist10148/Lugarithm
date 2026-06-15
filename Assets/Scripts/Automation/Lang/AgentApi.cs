using System;

/// <summary>
/// The single registry of everything the jeepney agent can do (actions) and
/// ask (queries). The parser, the block editor, and the interpreter all
/// validate against this list, so the two editors can never drift apart.
/// </summary>
public static class AgentApi
{
    public static readonly string[] Actions =
    {
        "moveForward", "turnLeft", "turnRight", "pickUp", "dropOff", "collectFare",
        // High-level navigation building blocks (self-driving): each plans a path
        // and the jeepney drives it cell-by-cell.
        "driveToNextStop", "driveToDestination",
    };

    public static readonly string[] Queries =
    {
        "frontIsClear", "leftIsClear", "rightIsClear", "atStop", "atDestination",
        "hasPassengerAboard", "atRequestedStop",
    };

    // -------------------------------------------------------------------------

    public static bool IsAction(string name) => Array.IndexOf(Actions, name) >= 0;
    public static bool IsQuery(string name)  => Array.IndexOf(Queries, name) >= 0;

    /// <summary>
    /// Closest known action/query to a misspelled name, or null when nothing
    /// is close enough. Used for "did you mean ...?" parser errors.
    /// </summary>
    public static string Suggest(string unknown)
    {
        if (string.IsNullOrEmpty(unknown)) return null;

        string best = null;
        int bestDistance = 4; // anything 4+ edits away isn't a useful suggestion

        foreach (string[] group in new[] { Actions, Queries })
        {
            foreach (string candidate in group)
            {
                int d = Levenshtein(unknown, candidate);
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = candidate;
                }
            }
        }

        return best;
    }

    // -------------------------------------------------------------------------

    static int Levenshtein(string a, string b)
    {
        int[,] d = new int[a.Length + 1, b.Length + 1];

        for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) d[0, j] = j;

        for (int i = 1; i <= a.Length; i++)
        {
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = char.ToLowerInvariant(a[i - 1]) == char.ToLowerInvariant(b[j - 1]) ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }

        return d[a.Length, b.Length];
    }
}

/// <summary>
/// What the interpreter needs from the world to evaluate a condition.
/// <see cref="AgentSim"/> implements this; tests use scripted fakes.
/// </summary>
public interface IAgentApi
{
    bool EvaluateQuery(string name);
}
