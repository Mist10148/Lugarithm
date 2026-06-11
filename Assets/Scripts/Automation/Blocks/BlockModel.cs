using System.Collections.Generic;

/// <summary>Every block the palette can offer.</summary>
public enum BlockType
{
    MoveForward,
    TurnLeft,
    TurnRight,
    PickUp,
    DropOff,
    CollectFare,
    If,
    IfElse,
    While,
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

    public readonly List<BlockNode> Body     = new List<BlockNode>();
    public readonly List<BlockNode> ElseBody = new List<BlockNode>();

    public BlockNode(BlockType type) { Type = type; }

    public bool IsContainer =>
        Type == BlockType.If || Type == BlockType.IfElse || Type == BlockType.While;

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
            case BlockType.PickUp:      return "pickUp";
            case BlockType.DropOff:     return "dropOff";
            case BlockType.CollectFare: return "collectFare";
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
            case "pickUp":      return BlockType.PickUp;
            case "dropOff":     return BlockType.DropOff;
            case "collectFare": return BlockType.CollectFare;
            case "if":          return BlockType.If;
            case "ifElse":      return BlockType.IfElse;
            case "while":       return BlockType.While;
            default:            return null;
        }
    }

    /// <summary>Display label for a block row (header line for containers).</summary>
    public static string Label(BlockNode node)
    {
        if (!node.IsContainer)
            return ActionName(node.Type) + "()";

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

    static void CompileList(List<BlockNode> source, List<StmtNode> target,
                            List<LangError> errors, List<BlockNode> offenders)
    {
        foreach (BlockNode node in source)
        {
            if (!node.IsContainer)
            {
                target.Add(new CallStmt { Name = ActionName(node.Type), SourceRef = node });
                continue;
            }

            ExprNode condition = new QueryExpr { Name = node.Query };
            if (node.Negate)
                condition = new NotExpr { Operand = condition };

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
}
