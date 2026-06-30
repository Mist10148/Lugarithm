using System.Collections.Generic;

/// <summary>Every block the palette can offer.</summary>
public enum BlockType
{
    MoveForward,
    TurnLeft,
    TurnRight,
    MoveLeft,
    MoveRight,
    DriveToNextStop,
    DriveToTerminal,
    KeepDriving,
    DriveToDropoff,
    PickUp,
    DropOff,
    CollectFare,
    GiveChange,
    If,
    IfElse,
    While,
    FunctionDef,    // def name(): … — a no-argument helper definition (container)
    FunctionCall,   // name()         — calls a defined helper
}

/// <summary>
/// One block on the canvas. Containers (if / ifElse / while) hold child
/// lists; condition blocks carry a query + optional negation. Pure data —
/// <see cref="BlockProgram"/> turns trees of these into the shared AST.
/// </summary>
public class BlockNode
{
    public BlockType Type;
    public string    Query  = "frontIsClear";
    public bool      Negate;

    /// <summary>Helper name for FunctionDef / FunctionCall blocks.</summary>
    public string    FuncName = "drive";

    public readonly List<BlockNode> Body     = new List<BlockNode>();
    public readonly List<BlockNode> ElseBody = new List<BlockNode>();

    public BlockNode(BlockType type) { Type = type; }

    public bool IsContainer =>
        Type == BlockType.If || Type == BlockType.IfElse || Type == BlockType.While ||
        Type == BlockType.FunctionDef;

    public bool HasElse => Type == BlockType.IfElse;
}

/// <summary>
/// Pure block-tree → AST compiler. Blocks cannot produce syntax errors by
/// design (PRD §5.3) — the only possible problem is an empty container body,
/// reported in plain English with the offending block attached.
/// </summary>
public static class BlockProgram
{
    // -------------------------------------------------------------------------
    // Names / labels

    /// <summary>Agent action name for an action block.</summary>
    public static string ActionName(BlockType type)
    {
        switch (type)
        {
            case BlockType.MoveForward: return "moveForward";
            case BlockType.TurnLeft:    return "turnLeft";
            case BlockType.TurnRight:   return "turnRight";
            case BlockType.MoveLeft:    return "moveLeft";
            case BlockType.MoveRight:   return "moveRight";
            case BlockType.DriveToNextStop: return "driveToNextStop";
            case BlockType.DriveToTerminal: return "driveToTerminal";
            case BlockType.KeepDriving:     return "keepDriving";
            case BlockType.DriveToDropoff:  return "driveToDropoff";
            case BlockType.PickUp:      return "pickUp";
            case BlockType.DropOff:     return "dropOff";
            case BlockType.CollectFare: return "collectFare";
            case BlockType.GiveChange:  return "giveChange";
            default:                    return null;
        }
    }

    /// <summary>Palette name (LevelDefinition.allowedBlocks) → block type.</summary>
    public static BlockType? FromPaletteName(string name)
    {
        switch (name)
        {
            case "moveForward": return BlockType.MoveForward;
            case "turnLeft":    return BlockType.TurnLeft;
            case "turnRight":   return BlockType.TurnRight;
            case "moveLeft":    return BlockType.MoveLeft;
            case "moveRight":   return BlockType.MoveRight;
            case "driveToNextStop": return BlockType.DriveToNextStop;
            case "driveToTerminal": return BlockType.DriveToTerminal;
            case "keepDriving":     return BlockType.KeepDriving;
            case "driveToDropoff":  return BlockType.DriveToDropoff;
            case "pickUp":      return BlockType.PickUp;
            case "dropOff":     return BlockType.DropOff;
            case "collectFare": return BlockType.CollectFare;
            case "giveChange":  return BlockType.GiveChange;
            case "if":          return BlockType.If;
            case "ifElse":      return BlockType.IfElse;
            case "while":       return BlockType.While;
            case "functionDef": return BlockType.FunctionDef;
            case "callFunction":return BlockType.FunctionCall;
            default:            return null;
        }
    }

    /// <summary>Display label for a block row (header line for containers).</summary>
    public static string Label(BlockNode node)
    {
        if (node.Type == BlockType.FunctionDef)
            return $"def {node.FuncName}():";

        if (!node.IsContainer)
        {
            if (node.Type == BlockType.FunctionCall)
                return $"{node.FuncName}()";
            if (node.Type == BlockType.GiveChange)
                return "giveChange(changeOwed())";
            return ActionName(node.Type) + "()";
        }

        string keyword   = node.Type == BlockType.While ? "while" : "if";
        string condition = (node.Negate ? "not " : "") + node.Query + "()";
        return $"{keyword} {condition}:";
    }

    // -------------------------------------------------------------------------
    // Compilation

    /// <summary>
    /// Compiles a block tree to the shared AST. Empty container bodies are
    /// reported as errors with the offending nodes in
    /// <paramref name="offenders"/> so the canvas can highlight them.
    /// </summary>
    public static ProgramNode ToAst(List<BlockNode> roots,
                                    out List<LangError> errors,
                                    out List<BlockNode> offenders)
    {
        errors    = new List<LangError>();
        offenders = new List<BlockNode>();

        var program = new ProgramNode();
        CompileList(roots, program.Statements, errors, offenders);
        return program;
    }

    // -------------------------------------------------------------------------
    // Reverse compilation: AST → block tree (so the AI agent / refactor can build
    // and rewrite the canvas, not just the text editor). Only the block-expressible
    // subset round-trips; anything else (elif chains, for/def/assign/break/continue,
    // counted calls, non-query conditions) sets <paramref name="fullyRepresentable"/>
    // false so the caller can fall back to the code editor instead of dropping detail.

    public static List<BlockNode> FromAst(ProgramNode program, out bool fullyRepresentable)
    {
        fullyRepresentable = true;
        var roots = new List<BlockNode>();
        if (program != null)
            DecompileList(program.Statements, roots, ref fullyRepresentable);
        return roots;
    }

    static void DecompileList(List<StmtNode> source, List<BlockNode> target, ref bool ok)
    {
        foreach (StmtNode stmt in source)
        {
            switch (stmt)
            {
                case CallStmt call:
                {
                    BlockType? type = FromPaletteName(call.Name);
                    bool isAction = type.HasValue && !new BlockNode(type.Value).IsContainer;
                    bool canShowArgs = call.Name == "giveChange" && IsChangeOwedArg(call.Args);
                    bool hasArgs = call.Args != null && call.Args.Count > 0;
                    if (isAction)
                    {
                        if (hasArgs && !canShowArgs) { ok = false; break; }   // a counted call blocks can't show
                        target.Add(new BlockNode(type.Value));
                        break;
                    }
                    // Not a known action — a no-arg call to a user-defined helper round-trips
                    // as a FunctionCall block (built-in queries/reporters never appear here).
                    if (!type.HasValue && !hasArgs && IsIdentifier(call.Name))
                    {
                        target.Add(new BlockNode(BlockType.FunctionCall) { FuncName = call.Name });
                        break;
                    }
                    ok = false;
                    break;
                }

                case FuncDefStmt def:
                {
                    if (def.Params != null && def.Params.Count > 0) ok = false; // blocks: no-arg helpers only
                    var node = new BlockNode(BlockType.FunctionDef) { FuncName = def.Name };
                    DecompileList(def.Body, node.Body, ref ok);
                    target.Add(node);
                    break;
                }

                case WhileStmt loop:
                {
                    var node = new BlockNode(BlockType.While);
                    if (!ApplyCondition(loop.Condition, node)) ok = false;
                    DecompileList(loop.Body, node.Body, ref ok);
                    target.Add(node);
                    break;
                }

                case IfStmt branch:
                {
                    if (branch.Elifs != null && branch.Elifs.Count > 0) ok = false; // no elif blocks
                    bool hasElse = branch.ElseBody != null;
                    var node = new BlockNode(hasElse ? BlockType.IfElse : BlockType.If);
                    if (!ApplyCondition(branch.Condition, node)) ok = false;
                    DecompileList(branch.Body, node.Body, ref ok);
                    if (hasElse) DecompileList(branch.ElseBody, node.ElseBody, ref ok);
                    target.Add(node);
                    break;
                }

                default:
                    ok = false;   // for / def / assign / break / continue / return
                    break;
            }
        }
    }

    /// <summary>Maps an AST condition onto a block's query + negate. Blocks only model a
    /// single (optionally negated) zero-arg query call.</summary>
    static bool ApplyCondition(ExprNode condition, BlockNode node)
    {
        if (condition is UnaryExpr unary && unary.Op == TokenType.KeywordNot)
        {
            node.Negate = true;
            condition = unary.Operand;
        }

        if (condition is CallExpr call && (call.Args == null || call.Args.Count == 0))
        {
            node.Query = call.Name;
            return true;
        }
        return false;   // compound / valued condition — keep the default query, signal partial
    }

    static void CompileList(List<BlockNode> source, List<StmtNode> target,
                            List<LangError> errors, List<BlockNode> offenders)
    {
        foreach (BlockNode node in source)
        {
            if (node.Type == BlockType.FunctionCall)
            {
                target.Add(new CallStmt { Name = node.FuncName, SourceRef = node });
                continue;
            }

            if (node.Type == BlockType.FunctionDef)
            {
                if (node.Body.Count == 0)
                {
                    errors.Add(new LangError(0,
                        $"the '{Label(node)}' block needs at least one block inside it."));
                    offenders.Add(node);
                }
                var def = new FuncDefStmt { Name = node.FuncName, SourceRef = node };
                CompileList(node.Body, def.Body, errors, offenders);
                target.Add(def);
                continue;
            }

            if (!node.IsContainer)
            {
                var call = new CallStmt { Name = ActionName(node.Type), SourceRef = node };
                if (node.Type == BlockType.GiveChange)
                    call.Args.Add(new CallExpr { Name = "changeOwed" });
                target.Add(call);
                continue;
            }

            ExprNode condition = new CallExpr { Name = node.Query };
            if (node.Negate)
                condition = new UnaryExpr { Op = TokenType.KeywordNot, Operand = condition };

            if (node.Body.Count == 0)
            {
                errors.Add(new LangError(0,
                    $"the '{Label(node)}' block needs at least one block inside it."));
                offenders.Add(node);
            }

            if (node.Type == BlockType.While)
            {
                var loop = new WhileStmt { Condition = condition, SourceRef = node };
                CompileList(node.Body, loop.Body, errors, offenders);
                target.Add(loop);
            }
            else
            {
                var branch = new IfStmt { Condition = condition, SourceRef = node };
                CompileList(node.Body, branch.Body, errors, offenders);

                if (node.HasElse)
                {
                    if (node.ElseBody.Count == 0)
                    {
                        errors.Add(new LangError(0,
                            "the 'else' branch needs at least one block inside it."));
                        offenders.Add(node);
                    }
                    branch.ElseBody = new List<StmtNode>();
                    CompileList(node.ElseBody, branch.ElseBody, errors, offenders);
                }

                target.Add(branch);
            }
        }
    }

    /// <summary>True for a plain helper name (letters/digits/underscore, not a digit first).</summary>
    static bool IsIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        if (!char.IsLetter(name[0]) && name[0] != '_') return false;
        foreach (char c in name)
            if (!char.IsLetterOrDigit(c) && c != '_') return false;
        return true;
    }

    static bool IsChangeOwedArg(List<ExprNode> args)
    {
        if (args == null || args.Count != 1) return false;
        return args[0] is CallExpr call &&
               call.Name == "changeOwed" &&
               (call.Args == null || call.Args.Count == 0);
    }
}
