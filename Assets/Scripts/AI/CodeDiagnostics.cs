using System.Collections.Generic;

/// <summary>Structured, pre-analyzed evidence about a player's program — derived locally from
/// the parsed AST plus a headless <see cref="RunReport"/>. Fed to the co-pilot hint so the LLM
/// can name the actual flaw ("6 moveForward() in a row → loop candidate", "stuck at (3,5)")
/// instead of receiving only a coarse goal gap. The booleans also drive tier/proactive logic.</summary>
public sealed class DiagnosticsResult
{
    public readonly List<string> Findings = new List<string>();

    public bool Empty;                  // nothing written yet
    public bool HasRepeatedPrimitive;   // N>=3 identical calls in a row
    public bool NoLoopButRepeats;       // no loop used, yet clearly repetitive
    public bool NeverMoved;             // jeepney's cell never changed
    public bool BlockedEarly;           // first movement bumped a wall
    public bool Undelivered;            // ran, missed passengers/destination
    public bool InfiniteGuardTripped;   // runtime guard / error stopped it

    /// <summary>One-line concatenation for prompt embedding ("None." when clean).</summary>
    public string Summary => Findings.Count == 0 ? "None." : string.Join(" ", Findings);
}

/// <summary>
/// Reads a player's program and a dry-run <see cref="RunReport"/> and detects the teachable
/// anti-patterns the co-pilot should react to. Pure analysis — no AI, no side effects — so it
/// runs anywhere (hints, the refactor gate, tests). Reuses <see cref="CodeAnalyticsService.Measure"/>
/// for loop depth/statement counts and the shared <see cref="Parser"/> for the AST.
/// </summary>
public static class CodeDiagnostics
{
    // A run of this many identical adjacent calls reads as "spam" worth folding into a loop.
    const int RepeatThreshold = 3;
    // Above this many statements with no loop at all, suggest a loop even without a long run.
    const int VerboseStatementThreshold = 8;

    public static DiagnosticsResult Analyze(string playerSource, RunReport report, string[] allowedBlocks)
    {
        var result = new DiagnosticsResult();

        if (string.IsNullOrWhiteSpace(playerSource))
        {
            result.Empty = true;
            result.Findings.Add("The editor is empty — start with a first action so we have something to work from.");
            // No program to analyze structurally; still surface any run evidence below.
            AppendRunFindings(result, report);
            return result;
        }

        ProgramNode program = Parser.Compile(playerSource, out List<LangError> _);

        // Structural: longest run of identical adjacent calls, anywhere in the tree.
        string repeatedName = null;
        int longestRun = program != null ? LongestRun(program.Statements, out repeatedName) : 0;

        AstMetrics metrics = CodeAnalyticsService.Measure(playerSource);

        if (longestRun >= RepeatThreshold && repeatedName != null)
        {
            result.HasRepeatedPrimitive = true;
            result.Findings.Add($"You repeat {repeatedName}() {longestRun} times in a row — a loop " +
                                "could do that in one place instead of copying the line.");
        }

        if (metrics.MaxLoopDepth == 0 &&
            (metrics.Statements >= VerboseStatementThreshold || longestRun >= RepeatThreshold + 1))
        {
            result.NoLoopButRepeats = true;
            result.Findings.Add("The program is long and uses no loop yet — a while/for would shrink the repetition.");
        }

        AppendRunFindings(result, report);
        return result;
    }

    /// <summary>Findings that come from the dry run itself (trace + outcome).</summary>
    static void AppendRunFindings(DiagnosticsResult result, RunReport report)
    {
        if (report == null) return;

        if (report.RuntimeErrored)
        {
            result.InfiniteGuardTripped = true;
            if (!string.IsNullOrWhiteSpace(report.GoalGap))
                result.Findings.Add("It stopped with an error: " + report.GoalGap);
        }

        if (report.Trace != null && report.Trace.Count > 0)
        {
            // Never moved: every recorded cell matches the first.
            UnityEngine.Vector2Int first = report.Trace[0].Pos;
            bool moved = false;
            foreach (TraceStep s in report.Trace)
                if (s.Pos != first) { moved = true; break; }
            if (!moved && report.Trace.Count > 1)
            {
                result.NeverMoved = true;
                result.Findings.Add("The jeepney never actually left its starting cell.");
            }

            // Blocked early: the first movement attempt bumped a wall.
            foreach (TraceStep s in report.Trace)
            {
                if (s.Action == "moveForward")
                {
                    if (s.Blocked)
                    {
                        result.BlockedEarly = true;
                        result.Findings.Add("The first moveForward() bumped a wall — check which way the " +
                                            "jeepney faces at the start before moving.");
                    }
                    break;
                }
            }
        }

        if (!report.Win && !report.RuntimeErrored && !string.IsNullOrWhiteSpace(report.GoalGap))
        {
            result.Undelivered = report.DeliveredCount < report.TotalPassengers;
            result.Findings.Add(report.GoalGap);
        }
    }

    /// <summary>Longest run of consecutive same-named <see cref="CallStmt"/> across any statement
    /// list in the tree (recurses into loop/if/def bodies). Returns the run length and its name.</summary>
    static int LongestRun(List<StmtNode> stmts, out string name)
    {
        name = null;
        if (stmts == null) return 0;

        int best = 0;
        string bestName = null;
        int run = 0;
        string runName = null;

        foreach (StmtNode stmt in stmts)
        {
            if (stmt is CallStmt call)
            {
                if (call.Name == runName) run++;
                else { runName = call.Name; run = 1; }
                if (run > best) { best = run; bestName = runName; }
            }
            else
            {
                runName = null;
                run = 0;
            }

            // Recurse into nested bodies; a longer run inside a block still counts.
            foreach (List<StmtNode> body in Bodies(stmt))
            {
                int inner = LongestRun(body, out string innerName);
                if (inner > best) { best = inner; bestName = innerName; }
            }
        }

        name = bestName;
        return best;
    }

    static IEnumerable<List<StmtNode>> Bodies(StmtNode stmt)
    {
        switch (stmt)
        {
            case WhileStmt w: yield return w.Body; break;
            case ForStmt f:   yield return f.Body; break;
            case FuncDefStmt d: yield return d.Body; break;
            case IfStmt i:
                yield return i.Body;
                foreach (ElifClause e in i.Elifs) yield return e.Body;
                if (i.ElseBody != null) yield return i.ElseBody;
                break;
        }
    }
}
