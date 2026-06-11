using System.Text;

/// <summary>
/// Renders an AST back to canonical source text (4-space indents). Used to
/// show "your solution" for block-built programs on the results screen, and
/// by round-trip tests that prove blocks and text produce the same programs.
/// </summary>
public static class AstPrinter
{
    public static string Print(ProgramNode program)
    {
        var sb = new StringBuilder();
        if (program != null)
            PrintBody(program.Statements, 0, sb);
        return sb.ToString();
    }

    // -------------------------------------------------------------------------

    static void PrintBody(System.Collections.Generic.List<StmtNode> body, int depth, StringBuilder sb)
    {
        foreach (StmtNode stmt in body)
            PrintStatement(stmt, depth, sb);
    }

    static void PrintStatement(StmtNode stmt, int depth, StringBuilder sb)
    {
        switch (stmt)
        {
            case CallStmt call:
                Indent(depth, sb).Append(call.Name).Append("()\n");
                break;

            case WhileStmt loop:
                Indent(depth, sb).Append("while ").Append(PrintExpr(loop.Condition)).Append(":\n");
                PrintBody(loop.Body, depth + 1, sb);
                break;

            case IfStmt branch:
                Indent(depth, sb).Append("if ").Append(PrintExpr(branch.Condition)).Append(":\n");
                PrintBody(branch.Body, depth + 1, sb);
                if (branch.ElseBody != null)
                {
                    Indent(depth, sb).Append("else:\n");
                    PrintBody(branch.ElseBody, depth + 1, sb);
                }
                break;
        }
    }

    static string PrintExpr(ExprNode expr)
    {
        switch (expr)
        {
            case NotExpr not:     return "not " + PrintExpr(not.Operand);
            case QueryExpr query: return query.Name + "()";
            default:              return "";
        }
    }

    static StringBuilder Indent(int depth, StringBuilder sb)
    {
        return sb.Append(new string(' ', depth * 4));
    }
}
