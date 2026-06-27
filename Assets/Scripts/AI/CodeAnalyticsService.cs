using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public sealed class CodeAnalysis
{
    public int EfficiencyScore;
    public int StepScore;
    public int RetryScore;
    public int TimeScore;
    public int StructureScore;
    public int StatementCount;
    public int WeightedComplexity;
    public int MaxNesting;
    public int LoopDepth;
    public int HotLine;
    public string ComplexityClass;
    public string Summary;
}

public static class CodeAnalyticsService
{
    public static CodeAnalysis Analyze(string playerSource, string optimalSource,
                                       int steps, int parSteps, int retries,
                                       float elapsed, float softTimer,
                                       IReadOnlyDictionary<int, int> lineHits = null)
    {
        AstMetrics player = Measure(playerSource);
        AstMetrics optimal = Measure(optimalSource);

        int stepScore = (int)Math.Round(50d * Math.Min(1d, (double)Math.Max(1, parSteps) / Math.Max(1, steps)));
        int retryScore = (int)Math.Round(20d / (1d + Math.Max(0, retries)));
        int timeScore = softTimer <= 0f ? 15 :
            (int)Math.Round(15d * Math.Min(1d, softTimer / Math.Max(1f, elapsed)));
        int structureScore = (int)Math.Round(15d * Math.Min(1d,
            (double)Math.Max(1, optimal.Weighted) / Math.Max(1, player.Weighted)));
        int total = Math.Max(0, Math.Min(100, stepScore + retryScore + timeScore + structureScore));

        int hotLine = 0;
        if (lineHits != null && lineHits.Count > 0)
            hotLine = lineHits.OrderByDescending(kvp => kvp.Value).First().Key;

        return new CodeAnalysis
        {
            EfficiencyScore = total,
            StepScore = stepScore,
            RetryScore = retryScore,
            TimeScore = timeScore,
            StructureScore = structureScore,
            StatementCount = player.Statements,
            WeightedComplexity = player.Weighted,
            MaxNesting = player.MaxNesting,
            LoopDepth = player.MaxLoopDepth,
            HotLine = hotLine,
            ComplexityClass = player.MaxLoopDepth == 0 ? "O(1)" :
                              player.MaxLoopDepth == 1 ? "O(n)" : $"O(n^{player.MaxLoopDepth})",
            Summary = $"Steps {stepScore}/50 · reliability {retryScore}/20 · time {timeScore}/15 · structure {structureScore}/15"
        };
    }

    public static AstMetrics Measure(string source)
    {
        ProgramNode program = Parser.Compile(source ?? "", out List<LangError> errors);
        if (errors.Count > 0 || program == null) return new AstMetrics();
        AstMetrics metrics = new AstMetrics();
        Visit(program.Statements, 0, 0, metrics);
        return metrics;
    }

    static void Visit(IEnumerable<StmtNode> statements, int nesting, int loopDepth, AstMetrics metrics)
    {
        foreach (StmtNode stmt in statements)
        {
            metrics.Statements++;
            metrics.MaxNesting = Math.Max(metrics.MaxNesting, nesting);
            int weight = 1 + nesting;
            switch (stmt)
            {
                case WhileStmt loop:
                    weight += 3;
                    metrics.MaxLoopDepth = Math.Max(metrics.MaxLoopDepth, loopDepth + 1);
                    Visit(loop.Body, nesting + 1, loopDepth + 1, metrics);
                    break;
                case ForStmt loop:
                    weight += 3;
                    metrics.MaxLoopDepth = Math.Max(metrics.MaxLoopDepth, loopDepth + 1);
                    Visit(loop.Body, nesting + 1, loopDepth + 1, metrics);
                    break;
                case IfStmt branch:
                    weight += 2;
                    Visit(branch.Body, nesting + 1, loopDepth, metrics);
                    foreach (ElifClause clause in branch.Elifs) Visit(clause.Body, nesting + 1, loopDepth, metrics);
                    if (branch.ElseBody != null) Visit(branch.ElseBody, nesting + 1, loopDepth, metrics);
                    break;
                case FuncDefStmt function:
                    weight += 2;
                    Visit(function.Body, nesting + 1, loopDepth, metrics);
                    break;
            }
            metrics.Weighted += weight;
        }
    }
}

public sealed class AstMetrics
{
    public int Statements;
    public int Weighted;
    public int MaxNesting;
    public int MaxLoopDepth;
}

public static class GeneratedProgramPolicy
{
    public static bool Validate(string source, string[] allowedBlocks, string[] allowedQueries,
                                out List<LangError> errors)
    {
        ProgramNode program = Parser.Compile(source ?? "", out errors);
        if (errors.Count > 0 || program == null) return false;
        HashSet<string> blocks = new HashSet<string>(allowedBlocks ?? Array.Empty<string>(), StringComparer.Ordinal);
        HashSet<string> queries = new HashSet<string>(allowedQueries ?? Array.Empty<string>(), StringComparer.Ordinal);
        HashSet<string> functions = CollectFunctionNames(program.Statements);
        ValidateStatements(program.Statements, blocks, queries, functions, errors);
        return errors.Count == 0;
    }

    static HashSet<string> CollectFunctionNames(IEnumerable<StmtNode> statements)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (StmtNode stmt in statements)
            if (stmt is FuncDefStmt def)
                names.Add(def.Name);
        return names;
    }

    static void ValidateStatements(IEnumerable<StmtNode> statements, HashSet<string> blocks,
                                   HashSet<string> queries, HashSet<string> functions,
                                   List<LangError> errors)
    {
        foreach (StmtNode stmt in statements)
        {
            switch (stmt)
            {
                case CallStmt call:
                    if (functions.Contains(call.Name))
                    {
                        if (!blocks.Contains("callFunction"))
                            errors.Add(new LangError(call.Line, "function calls are not unlocked in this level."));
                    }
                    else if (!blocks.Contains(call.Name))
                    {
                        errors.Add(new LangError(call.Line, $"{call.Name}() is not unlocked in this level."));
                    }
                    foreach (ExprNode arg in call.Args) ValidateExpr(arg, queries, errors);
                    break;
                case WhileStmt loop:
                    if (!blocks.Contains("while")) errors.Add(new LangError(loop.Line, "while is not unlocked in this level."));
                    ValidateExpr(loop.Condition, queries, errors);
                    ValidateStatements(loop.Body, blocks, queries, functions, errors);
                    break;
                case IfStmt branch:
                    if (!blocks.Contains("if") && !blocks.Contains("ifElse"))
                        errors.Add(new LangError(branch.Line, "if is not unlocked in this level."));
                    ValidateExpr(branch.Condition, queries, errors);
                    ValidateStatements(branch.Body, blocks, queries, functions, errors);
                    foreach (ElifClause clause in branch.Elifs)
                    {
                        ValidateExpr(clause.Condition, queries, errors);
                        ValidateStatements(clause.Body, blocks, queries, functions, errors);
                    }
                    if (branch.ElseBody != null) ValidateStatements(branch.ElseBody, blocks, queries, functions, errors);
                    break;
                case ForStmt loop:
                    if (!blocks.Contains("for")) errors.Add(new LangError(loop.Line, "for is not unlocked in this level."));
                    ValidateExpr(loop.Iterable, queries, errors);
                    ValidateStatements(loop.Body, blocks, queries, functions, errors);
                    break;
                case AssignStmt assignment:
                    if (!blocks.Contains("variables") && !blocks.Contains("list"))
                        errors.Add(new LangError(assignment.Line, "variables are not unlocked in this level."));
                    ValidateExpr(assignment.Value, queries, errors);
                    break;
                case FuncDefStmt def:
                    if (!blocks.Contains("functionDef"))
                        errors.Add(new LangError(def.Line, "function definitions are not unlocked in this level."));
                    if (def.Params.Count > 0)
                        errors.Add(new LangError(def.Line, "generated function blocks use no inputs in this level."));
                    ValidateStatements(def.Body, blocks, queries, functions, errors);
                    break;
                default:
                    errors.Add(new LangError(stmt.Line, $"{stmt.GetType().Name} is not permitted in generated code for this level."));
                    break;
            }
        }
    }

    static void ValidateExpr(ExprNode expr, HashSet<string> queries, List<LangError> errors)
    {
        if (expr == null) return;
        switch (expr)
        {
            case CallExpr call:
                if (AgentApi.IsQuery(call.Name) && !queries.Contains(call.Name))
                    errors.Add(new LangError(call.Line, $"{call.Name}() is not unlocked in this level."));
                else if (AgentApi.IsAction(call.Name))
                    errors.Add(new LangError(call.Line, $"{call.Name}() cannot be used as a condition."));
                foreach (ExprNode arg in call.Args) ValidateExpr(arg, queries, errors);
                break;
            case BinaryExpr binary:
                ValidateExpr(binary.Left, queries, errors); ValidateExpr(binary.Right, queries, errors); break;
            case UnaryExpr unary: ValidateExpr(unary.Operand, queries, errors); break;
            case IndexExpr index:
                ValidateExpr(index.Container, queries, errors); ValidateExpr(index.Index, queries, errors);
                ValidateExpr(index.Stop, queries, errors); ValidateExpr(index.Step, queries, errors); break;
            case ListExpr list:
                foreach (ExprNode item in list.Items) ValidateExpr(item, queries, errors); break;
            case TupleExpr tuple:
                foreach (ExprNode item in tuple.Items) ValidateExpr(item, queries, errors); break;
        }
    }
}
