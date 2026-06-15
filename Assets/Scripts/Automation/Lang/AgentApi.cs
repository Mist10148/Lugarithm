using System;
using System.Collections.Generic;

/// <summary>
/// The kind of entry in the agent vocabulary.
/// </summary>
public enum ApiKind
{
    Action,
    Query,
    Reporter,
}

/// <summary>
/// One domain vocabulary entry: name, kind, allowed argument count, and whether
/// it returns a value. Used by the parser and block editor for validation.
/// </summary>
public class ApiEntry
{
    public string Name;
    public ApiKind Kind;
    public int MinArity;
    public int MaxArity;

    public bool ReturnsValue => Kind == ApiKind.Reporter;
    public bool IsValueReturningAction => Kind == ApiKind.Action && Name == "collectFare";

    public ApiEntry(string name, ApiKind kind, int minArity, int maxArity)
    {
        Name     = name;
        Kind     = kind;
        MinArity = minArity;
        MaxArity = maxArity;
    }
}

/// <summary>
/// The single registry of everything the jeepney agent can do (actions),
/// ask (queries), and read (reporters). The parser, the block editor, and the
/// interpreter all validate against this list, so the two editors can never drift.
/// </summary>
public static class AgentApi
{
    public static readonly List<ApiEntry> Entries = new List<ApiEntry>
    {
        // Core locomotion (both modes)
        new ApiEntry("moveForward",        ApiKind.Action,  minArity: 0, maxArity: 1),
        new ApiEntry("turnLeft",           ApiKind.Action,  minArity: 0, maxArity: 0),
        new ApiEntry("turnRight",          ApiKind.Action,  minArity: 0, maxArity: 0),
        new ApiEntry("wait",               ApiKind.Action,  minArity: 0, maxArity: 1),

        new ApiEntry("frontIsClear",       ApiKind.Query,   minArity: 0, maxArity: 0),
        new ApiEntry("leftIsClear",        ApiKind.Query,   minArity: 0, maxArity: 0),
        new ApiEntry("rightIsClear",       ApiKind.Query,   minArity: 0, maxArity: 0),
        new ApiEntry("atDestination",      ApiKind.Query,   minArity: 0, maxArity: 0),

        // Maze / puzzle add-on
        new ApiEntry("atGoal",             ApiKind.Query,   minArity: 0, maxArity: 0),
        new ApiEntry("markCell",           ApiKind.Action,  minArity: 0, maxArity: 0),
        new ApiEntry("unmark",             ApiKind.Action,  minArity: 0, maxArity: 0),
        new ApiEntry("isMarked",           ApiKind.Query,   minArity: 0, maxArity: 0),
        new ApiEntry("position",           ApiKind.Reporter,minArity: 0, maxArity: 0),

        // Automation-exclusive actions
        new ApiEntry("driveToNextStop",    ApiKind.Action,  minArity: 0, maxArity: 0),
        new ApiEntry("driveToDestination", ApiKind.Action,  minArity: 0, maxArity: 0),
        new ApiEntry("openDoor",           ApiKind.Action,  minArity: 0, maxArity: 0),
        new ApiEntry("closeDoor",          ApiKind.Action,  minArity: 0, maxArity: 0),
        new ApiEntry("board",              ApiKind.Action,  minArity: 0, maxArity: 0),
        new ApiEntry("alight",             ApiKind.Action,  minArity: 0, maxArity: 0),
        new ApiEntry("pickUp",             ApiKind.Action,  minArity: 0, maxArity: 0),
        new ApiEntry("dropOff",            ApiKind.Action,  minArity: 0, maxArity: 0),
        new ApiEntry("collectFare",        ApiKind.Action,  minArity: 0, maxArity: 0),
        new ApiEntry("announceStop",       ApiKind.Action,  minArity: 0, maxArity: 0),
        new ApiEntry("honk",               ApiKind.Action,  minArity: 0, maxArity: 0),

        // Automation-exclusive queries
        new ApiEntry("atStop",             ApiKind.Query,   minArity: 0, maxArity: 0),
        new ApiEntry("atRequestedStop",    ApiKind.Query,   minArity: 0, maxArity: 0),
        new ApiEntry("hasPassengerAboard", ApiKind.Query,   minArity: 0, maxArity: 0),
        new ApiEntry("passengerWaiting",   ApiKind.Query,   minArity: 0, maxArity: 0),
        new ApiEntry("isFull",             ApiKind.Query,   minArity: 0, maxArity: 0),

        // Automation-exclusive reporters
        new ApiEntry("seatsLeft",          ApiKind.Reporter,minArity: 0, maxArity: 0),
        new ApiEntry("passengerCount",     ApiKind.Reporter,minArity: 0, maxArity: 0),
        new ApiEntry("passengerType",      ApiKind.Reporter,minArity: 0, maxArity: 0),
        new ApiEntry("fareOwed",           ApiKind.Reporter,minArity: 0, maxArity: 0),
        new ApiEntry("distanceTraveled",   ApiKind.Reporter,minArity: 0, maxArity: 0),
        new ApiEntry("distanceToDestination", ApiKind.Reporter,minArity: 0, maxArity: 0),
        new ApiEntry("currentStop",        ApiKind.Reporter,minArity: 0, maxArity: 0),
        new ApiEntry("nextStop",           ApiKind.Reporter,minArity: 0, maxArity: 0),
    };

    static readonly Dictionary<string, ApiEntry> EntryByName = new Dictionary<string, ApiEntry>();

    static AgentApi()
    {
        foreach (ApiEntry e in Entries)
            EntryByName[e.Name] = e;
    }

    // -------------------------------------------------------------------------

    public static bool TryGetEntry(string name, out ApiEntry entry) => EntryByName.TryGetValue(name, out entry);

    public static bool IsAction(string name)  => TryGetEntry(name, out ApiEntry e) && e.Kind == ApiKind.Action;
    public static bool IsQuery(string name)   => TryGetEntry(name, out ApiEntry e) && e.Kind == ApiKind.Query;
    public static bool IsReporter(string name)=> TryGetEntry(name, out ApiEntry e) && e.Kind == ApiKind.Reporter;

    public static bool IsKnown(string name) => EntryByName.ContainsKey(name);

    public static bool IsValueReturningAction(string name) =>
        TryGetEntry(name, out ApiEntry e) && e.IsValueReturningAction;

    /// <summary>
    /// Closest known action/query/reporter to a misspelled name, or null when
    /// nothing is close enough. Used for "did you mean ...?" parser errors.
    /// </summary>
    public static string Suggest(string unknown)
    {
        if (string.IsNullOrEmpty(unknown)) return null;

        string best = null;
        int bestDistance = 4; // anything 4+ edits away isn't a useful suggestion

        foreach (ApiEntry candidate in Entries)
        {
            int d = Levenshtein(unknown, candidate.Name);
            if (d < bestDistance)
            {
                bestDistance = d;
                best = candidate.Name;
            }
        }

        return best;
    }

    // -------------------------------------------------------------------------

    /// <summary>
    /// Validates argument count for a known call. Returns null when the count
    /// is acceptable, otherwise a friendly <see cref="LangError"/>.
    /// </summary>
    public static LangError CheckArity(string name, int argCount, int line)
    {
        if (!TryGetEntry(name, out ApiEntry e)) return null;

        if (argCount < e.MinArity || argCount > e.MaxArity)
        {
            string expected = e.MinArity == e.MaxArity
                ? $"{e.MinArity} input{(e.MinArity == 1 ? "" : "s")}"
                : $"between {e.MinArity} and {e.MaxArity} inputs";
            return new LangError(line,
                $"{name}() needs {expected} but got {argCount}.");
        }
        return null;
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
/// What the interpreter needs from the world to evaluate conditions, read
/// reporters, and apply actions.
/// </summary>
public interface IAgentApi
{
    bool EvaluateQuery(string name);
    Value ReadReporter(string name, IReadOnlyList<Value> args);
}
