using System.Collections.Generic;
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
                Indent(depth, sb).Append(call.Name).Append("(").Append(PrintArgs(call.Args)).Append(")\n");
                break;

            case AssignStmt assign:
                Indent(depth, sb).Append(assign.Name).Append(" = ").Append(PrintExpr(assign.Value)).Append("\n");
                break;

            case WhileStmt loop:
                Indent(depth, sb).Append("while ").Append(PrintExpr(loop.Condition)).Append(":\n");
                PrintBody(loop.Body, depth + 1, sb);
                break;

            case ForStmt loop:
                Indent(depth, sb).Append("for ").Append(loop.Var).Append(" in ")
                    .Append(PrintExpr(loop.Iterable)).Append(":\n");
                PrintBody(loop.Body, depth + 1, sb);
                break;

            case IfStmt branch:
                Indent(depth, sb).Append("if ").Append(PrintExpr(branch.Condition)).Append(":\n");
                PrintBody(branch.Body, depth + 1, sb);
                foreach (ElifClause elif in branch.Elifs)
                {
                    Indent(depth, sb).Append("elif ").Append(PrintExpr(elif.Condition)).Append(":\n");
                    PrintBody(elif.Body, depth + 1, sb);
                }
                if (branch.ElseBody != null)
                {
                    Indent(depth, sb).Append("else:\n");
                    PrintBody(branch.ElseBody, depth + 1, sb);
                }
                break;

            case BreakStmt:
                Indent(depth, sb).Append("break\n");
                break;

            case ContinueStmt:
                Indent(depth, sb).Append("continue\n");
                break;

            case FuncDefStmt def:
                Indent(depth, sb).Append("def ").Append(def.Name).Append("(")
                    .Append(string.Join(", ", def.Params)).Append("):\n");
                PrintBody(def.Body, depth + 1, sb);
                break;

            case ReturnStmt ret:
                Indent(depth, sb).Append("return");
                if (ret.Value != null) sb.Append(" ").Append(PrintExpr(ret.Value));
                sb.Append("\n");
                break;

            case IndexAssignStmt idx:
                Indent(depth, sb).Append(PrintExpr(idx.Container)).Append("[").Append(PrintExpr(idx.Index))
                    .Append("] = ").Append(PrintExpr(idx.Value)).Append("\n");
                break;
        }
    }

    static string PrintExpr(ExprNode expr)
    {
        switch (expr)
        {
            case LiteralExpr lit:
                switch (lit.Value.Kind)
                {
                    case ValueKind.Str:  return "\"" + lit.Value.S + "\"";
                    case ValueKind.Bool: return lit.Value.B ? "True" : "False";
                    case ValueKind.None: return "None";
                    default:             return lit.Value.Display();
                }
            case VarExpr var:      return var.Name;
            case CallExpr call:    return call.Name + "(" + PrintArgs(call.Args) + ")";
            case BinaryExpr bin:   return PrintExpr(bin.Left) + " " + OpSymbol(bin.Op) + " " + PrintExpr(bin.Right);
            case UnaryExpr un:
            {
                string sym = OpSymbol(un.Op);
                return sym + (un.Op == TokenType.KeywordNot ? " " : "") + PrintExpr(un.Operand);
            }
            case ListExpr list:    return "[" + string.Join(", ", list.Items.ConvertAll(PrintExpr)) + "]";
            case DictExpr dict:
            {
                var pairs = new List<string>();
                foreach (var kv in dict.Entries)
                    pairs.Add(PrintExpr(kv.Key) + ": " + PrintExpr(kv.Value));
                return "{" + string.Join(", ", pairs) + "}";
            }
            case TupleExpr tuple:  return "(" + string.Join(", ", tuple.Items.ConvertAll(PrintExpr)) + ")";
            case IndexExpr idx:
            {
                string stop = idx.Stop != null ? ":" + PrintExpr(idx.Stop) : "";
                string step = idx.Step != null ? ":" + PrintExpr(idx.Step) : "";
                return PrintExpr(idx.Container) + "[" + PrintExpr(idx.Index) + stop + step + "]";
            }
            default:               return "";
        }
    }

    static string PrintArgs(System.Collections.Generic.List<ExprNode> args)
    {
        if (args == null || args.Count == 0) return "";
        var sb = new StringBuilder();
        for (int i = 0; i < args.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(PrintExpr(args[i]));
        }
        return sb.ToString();
    }

    static string OpSymbol(TokenType op)
    {
        switch (op)
        {
            case TokenType.Plus:       return "+";
            case TokenType.Minus:      return "-";
            case TokenType.Star:       return "*";
            case TokenType.Slash:      return "/";
            case TokenType.SlashSlash: return "//";
            case TokenType.Percent:    return "%";
            case TokenType.StarStar:   return "**";
            case TokenType.EqEq:       return "==";
            case TokenType.NotEq:      return "!=";
            case TokenType.Lt:         return "<";
            case TokenType.Gt:         return ">";
            case TokenType.Le:         return "<=";
            case TokenType.Ge:         return ">=";
            case TokenType.KeywordAnd: return "and";
            case TokenType.KeywordOr:  return "or";
            case TokenType.KeywordIn:  return "in";
            case TokenType.KeywordNot: return "not";
            default:                   return op.ToString();
        }
    }

    static StringBuilder Indent(int depth, StringBuilder sb)
    {
        return sb.Append(new string(' ', depth * 4));
    }
}
