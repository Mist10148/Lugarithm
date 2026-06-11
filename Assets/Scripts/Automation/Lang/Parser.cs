using System.Collections.Generic;

/// <summary>
/// Recursive-descent parser for the automation language. Grammar:
/// <code>
///   program   := statement* EOF
///   statement := call NEWLINE | ifStmt | whileStmt
///   ifStmt    := 'if' expr ':' NEWLINE INDENT statement+ DEDENT
///                ('else' ':' NEWLINE INDENT statement+ DEDENT)?
///   whileStmt := 'while' expr ':' NEWLINE INDENT statement+ DEDENT
///   expr      := 'not' expr | IDENT '(' ')'
///   call      := IDENT '(' ')'
/// </code>
/// Names are validated against <see cref="AgentApi"/> at parse time so errors
/// read like coaching, not compiler output.
/// </summary>
public static class Parser
{
    // -------------------------------------------------------------------------
    // Public API

    /// <summary>Lex + parse in one call.</summary>
    public static ProgramNode Compile(string source, out List<LangError> errors)
    {
        List<Token> tokens = Lexer.Tokenize(source, out errors);
        ProgramNode program = Parse(tokens, out List<LangError> parseErrors);
        errors.AddRange(parseErrors);
        return program;
    }

    /// <summary>Parses a token list. Never throws — problems land in errors.</summary>
    public static ProgramNode Parse(List<Token> tokens, out List<LangError> errors)
    {
        var state = new ParseState { Tokens = tokens, Errors = new List<LangError>() };
        var program = new ProgramNode();

        while (!state.Check(TokenType.EndOfFile))
        {
            StmtNode stmt = ParseStatement(state);
            if (stmt != null)
                program.Statements.Add(stmt);
        }

        errors = state.Errors;
        return program;
    }

    // -------------------------------------------------------------------------
    // Statements

    static StmtNode ParseStatement(ParseState s)
    {
        Token t = s.Peek();

        switch (t.Type)
        {
            case TokenType.KeywordWhile:
                return ParseWhile(s);

            case TokenType.KeywordIf:
                return ParseIf(s);

            case TokenType.Identifier:
                return ParseCall(s);

            case TokenType.KeywordElse:
                s.Error(t.Line, "this 'else' has no matching 'if' above it.");
                s.SkipToNextLine();
                return null;

            case TokenType.KeywordNot:
                s.Error(t.Line, "'not' only makes sense inside an if or while condition.");
                s.SkipToNextLine();
                return null;

            // Stray structure tokens — recover quietly and keep going.
            case TokenType.Newline:
            case TokenType.Indent:
            case TokenType.Dedent:
                s.Advance();
                return null;

            default:
                s.Error(t.Line, "expected a command here.");
                s.SkipToNextLine();
                return null;
        }
    }

    static StmtNode ParseCall(ParseState s)
    {
        Token name = s.Advance();

        if (!s.Match(TokenType.LParen))
            s.Error(name.Line, $"'{name.Text}' needs parentheses — write {name.Text}().");
        else if (!s.Match(TokenType.RParen))
            s.Error(name.Line, $"missing the closing ')' after {name.Text}(.");

        s.ExpectEndOfLine(name.Line);

        // Semantic checks against the agent API.
        if (AgentApi.IsQuery(name.Text))
        {
            s.Error(name.Line,
                $"{name.Text}() is a question, not an action — use it inside an if or while condition.");
            return null;
        }

        if (!AgentApi.IsAction(name.Text))
        {
            string suggestion = AgentApi.Suggest(name.Text);
            s.Error(name.Line, suggestion != null
                ? $"'{name.Text}' is not a known command. Did you mean '{suggestion}'?"
                : $"'{name.Text}' is not a known command.");
            return null;
        }

        return new CallStmt { Name = name.Text, Line = name.Line };
    }

    static StmtNode ParseWhile(ParseState s)
    {
        Token kw = s.Advance();
        var node = new WhileStmt { Line = kw.Line };

        node.Condition = ParseExpr(s);
        node.Body      = ParseBlockAfterHeader(s, kw.Line, "while");

        return node;
    }

    static StmtNode ParseIf(ParseState s)
    {
        Token kw = s.Advance();
        var node = new IfStmt { Line = kw.Line };

        node.Condition = ParseExpr(s);
        node.Body      = ParseBlockAfterHeader(s, kw.Line, "if");

        if (s.Check(TokenType.KeywordElse))
        {
            Token elseKw = s.Advance();
            node.ElseBody = ParseBlockAfterHeader(s, elseKw.Line, "else");
        }

        return node;
    }

    /// <summary>Parses <c>: NEWLINE INDENT statement+ DEDENT</c> after a header.</summary>
    static List<StmtNode> ParseBlockAfterHeader(ParseState s, int headerLine, string headerName)
    {
        var body = new List<StmtNode>();

        if (!s.Match(TokenType.Colon))
        {
            s.Error(headerLine, $"expected ':' at the end of the {headerName} line.");
            s.SkipToNextLine();
        }
        else if (!s.Match(TokenType.Newline))
        {
            s.Error(headerLine, $"nothing else should follow the ':' on the {headerName} line.");
            s.SkipToNextLine();
        }

        if (!s.Match(TokenType.Indent))
        {
            s.Error(headerLine, $"the {headerName} needs at least one indented line under it.");
            return body;
        }

        while (!s.Check(TokenType.Dedent) && !s.Check(TokenType.EndOfFile))
        {
            StmtNode stmt = ParseStatement(s);
            if (stmt != null)
                body.Add(stmt);
        }

        s.Match(TokenType.Dedent);
        return body;
    }

    // -------------------------------------------------------------------------
    // Expressions

    static ExprNode ParseExpr(ParseState s)
    {
        Token t = s.Peek();

        if (t.Type == TokenType.KeywordNot)
        {
            s.Advance();
            return new NotExpr { Operand = ParseExpr(s), Line = t.Line };
        }

        if (t.Type == TokenType.Identifier)
        {
            s.Advance();

            if (!s.Match(TokenType.LParen))
                s.Error(t.Line, $"'{t.Text}' needs parentheses — write {t.Text}().");
            else if (!s.Match(TokenType.RParen))
                s.Error(t.Line, $"missing the closing ')' after {t.Text}(.");

            if (AgentApi.IsAction(t.Text))
            {
                s.Error(t.Line,
                    $"{t.Text}() is an action, not a question — it can't be used as a condition.");
            }
            else if (!AgentApi.IsQuery(t.Text))
            {
                string suggestion = AgentApi.Suggest(t.Text);
                s.Error(t.Line, suggestion != null
                    ? $"'{t.Text}' is not a known question. Did you mean '{suggestion}'?"
                    : $"'{t.Text}' is not a known question.");
            }

            return new QueryExpr { Name = t.Text, Line = t.Line };
        }

        s.Error(t.Line, "expected a condition here, like frontIsClear().");
        return new QueryExpr { Name = "frontIsClear", Line = t.Line }; // safe placeholder
    }

    // -------------------------------------------------------------------------
    // Parse state / cursor helpers

    class ParseState
    {
        public List<Token>     Tokens;
        public List<LangError> Errors;
        public int             Pos;

        public Token Peek() => Tokens[Pos];

        public Token Advance()
        {
            Token t = Tokens[Pos];
            if (Pos < Tokens.Count - 1) Pos++; // never advance past EOF
            return t;
        }

        public bool Check(TokenType type) => Tokens[Pos].Type == type;

        public bool Match(TokenType type)
        {
            if (!Check(type)) return false;
            Advance();
            return true;
        }

        public void Error(int line, string message)
        {
            Errors.Add(new LangError(line, message));
        }

        /// <summary>Error recovery: skip to just past the next Newline.</summary>
        public void SkipToNextLine()
        {
            while (!Check(TokenType.Newline) && !Check(TokenType.EndOfFile))
                Advance();
            Match(TokenType.Newline);
        }

        /// <summary>Consumes the statement's trailing Newline, complaining about extras.</summary>
        public void ExpectEndOfLine(int line)
        {
            if (Match(TokenType.Newline)) return;
            if (Check(TokenType.EndOfFile)) return;

            Error(line, "unexpected extra text on this line.");
            SkipToNextLine();
        }
    }
}
