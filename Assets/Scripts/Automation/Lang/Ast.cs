using System.Collections.Generic;

/// <summary>
/// AST for the automation language. Both the text parser and the block editor
/// produce these nodes, so the interpreter is shared. <c>SourceRef</c> carries
/// the originating block (block mode) for execution highlighting; <c>Line</c>
/// carries the source line (text mode).
/// </summary>
public class ProgramNode
{
    public List<StmtNode> Statements = new List<StmtNode>();
}

public abstract class StmtNode
{
    /// <summary>1-based source line in text mode; 0 for block-built programs.</summary>
    public int Line;

    /// <summary>The block-editor node this came from, if any (for highlighting).</summary>
    public object SourceRef;
}

/// <summary>A zero-argument action call: <c>moveForward()</c>.</summary>
public class CallStmt : StmtNode
{
    public string Name;
}

/// <summary><c>while CONDITION:</c> with an indented body.</summary>
public class WhileStmt : StmtNode
{
    public ExprNode       Condition;
    public List<StmtNode> Body = new List<StmtNode>();
}

/// <summary><c>if CONDITION:</c> with an optional <c>else:</c> body.</summary>
public class IfStmt : StmtNode
{
    public ExprNode       Condition;
    public List<StmtNode> Body = new List<StmtNode>();

    /// <summary>Null when the if has no else branch.</summary>
    public List<StmtNode> ElseBody;
}

// -----------------------------------------------------------------------------
// Expressions (conditions)

public abstract class ExprNode
{
    public int Line;
}

/// <summary>A zero-argument query call used as a condition: <c>frontIsClear()</c>.</summary>
public class QueryExpr : ExprNode
{
    public string Name;
}

/// <summary><c>not OPERAND</c>.</summary>
public class NotExpr : ExprNode
{
    public ExprNode Operand;
}
