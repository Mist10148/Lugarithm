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

/// <summary>An action call: <c>moveForward()</c> or <c>moveForward(3)</c>.</summary>
public class CallStmt : StmtNode
{
    public string Name;
    public List<ExprNode> Args = new List<ExprNode>();
}

/// <summary><c>name = expression</c></summary>
public class AssignStmt : StmtNode
{
    public string   Name;
    public ExprNode Value;
}

/// <summary><c>container[index] = expression</c></summary>
public class IndexAssignStmt : StmtNode
{
    public ExprNode Container;
    public ExprNode Index;
    public ExprNode Value;
}

/// <summary><c>while CONDITION:</c> with an indented body.</summary>
public class WhileStmt : StmtNode
{
    public ExprNode       Condition;
    public List<StmtNode> Body = new List<StmtNode>();
}

/// <summary>One <c>elif CONDITION:</c> branch.</summary>
public class ElifClause
{
    public int            Line;
    public ExprNode       Condition;
    public List<StmtNode> Body = new List<StmtNode>();
}

/// <summary><c>if/elif/else</c> chain.</summary>
public class IfStmt : StmtNode
{
    public ExprNode       Condition;
    public List<StmtNode> Body = new List<StmtNode>();
    public List<ElifClause> Elifs = new List<ElifClause>();

    /// <summary>Null when the if has no else branch.</summary>
    public List<StmtNode> ElseBody;
}

/// <summary><c>for var in iterable:</c> with an indented body.</summary>
public class ForStmt : StmtNode
{
    public string         Var;
    public ExprNode       Iterable;
    public List<StmtNode> Body = new List<StmtNode>();
}

/// <summary><c>break</c></summary>
public class BreakStmt : StmtNode { }

/// <summary><c>continue</c></summary>
public class ContinueStmt : StmtNode { }

/// <summary><c>def name(params):</c> with an indented body.</summary>
public class FuncDefStmt : StmtNode
{
    public string         Name;
    public List<string>   Params = new List<string>();
    public List<StmtNode> Body   = new List<StmtNode>();
}

/// <summary><c>return expr</c></summary>
public class ReturnStmt : StmtNode
{
    public ExprNode Value;
}

// -----------------------------------------------------------------------------
// Expressions

public abstract class ExprNode
{
    public int Line;
}

/// <summary>A literal value: <c>5</c>, <c>"Para"</c>, <c>True</c>, <c>None</c>.</summary>
public class LiteralExpr : ExprNode
{
    public Value Value;
}

/// <summary>A variable read: <c>total</c>.</summary>
public class VarExpr : ExprNode
{
    public string Name;
}

/// <summary>A call in value position: <c>seatsLeft()</c>, <c>len(items)</c>.</summary>
public class CallExpr : ExprNode
{
    public string         Name;
    public List<ExprNode> Args = new List<ExprNode>();
}

/// <summary>A binary operation: <c>a + b</c>, <c>x == y</c>, <c>p and q</c>.</summary>
public class BinaryExpr : ExprNode
{
    public ExprNode  Left;
    public TokenType Op;
    public ExprNode  Right;
}

/// <summary>A unary operation: <c>not x</c>, <c>-n</c>.</summary>
public class UnaryExpr : ExprNode
{
    public TokenType Op;
    public ExprNode  Operand;
}

/// <summary>A list literal: <c>[1, 2, 3]</c>.</summary>
public class ListExpr : ExprNode
{
    public List<ExprNode> Items = new List<ExprNode>();
}

/// <summary>A dictionary literal: <c>{"a": 1}</c>.</summary>
public class DictExpr : ExprNode
{
    public List<System.Collections.Generic.KeyValuePair<ExprNode, ExprNode>> Entries
        = new List<System.Collections.Generic.KeyValuePair<ExprNode, ExprNode>>();
}

/// <summary>A tuple literal: <c>(1, 2)</c>.</summary>
public class TupleExpr : ExprNode
{
    public List<ExprNode> Items = new List<ExprNode>();
}

/// <summary>An index access: <c>a[i]</c> or <c>a[i:j]</c>.</summary>
public class IndexExpr : ExprNode
{
    public ExprNode Container;
    public ExprNode Index;
    public ExprNode Stop;   // null for simple index
    public ExprNode Step;   // null for no step
}
