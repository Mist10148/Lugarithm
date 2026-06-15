using System.Collections.Generic;

/// <summary>
/// Recursive-descent parser for the automation language. Grammar (Phase 5):
/// <code>
///   program   := statement* EOF
///   statement := assign | indexAssign | call NEWLINE | ifStmt | whileStmt |
///                forStmt | repeatStmt | break | continue | funcDef
///   assign    := IDENT '=' expr NEWLINE
///   indexAssign := primary '[' expr ']' '=' expr NEWLINE
///   call      := IDENT '(' args ')' | primary '.' IDENT '(' args ')'
///   args      := (expr (',' expr)*)?
///   ifStmt    := 'if' condition ':' block ('elif' condition ':' block)* ('else' ':' block)?
///   whileStmt := 'while' condition ':' block
///   forStmt   := 'for' IDENT 'in' expr ':' block
///   repeatStmt:= 'repeat' expr ':' block
///   break     := 'break' NEWLINE
///   continue  := 'continue' NEWLINE
///   funcDef   := 'def' IDENT '(' params ')' ':' block
///   params    := (IDENT (',' IDENT)*)?
///   return    := 'return' expr? NEWLINE
///   block     := NEWLINE INDENT statement+ DEDENT
///   condition := expr
///   expr      := or_expr
///   ... (precedence climbing)
///   primary   := NUMBER | STRING | TRUE | FALSE | NONE | IDENT suffix*
///                | '(' expr (',' expr)* ')' | '[' items ']' | '{' entries '}'
///   suffix    := '[' expr (':' expr (':' expr)?)? ']' | '.' IDENT ('(' args ')')?
/// </code>
/// Names are validated against <see cref="AgentApi"/> at parse time so errors
/// read like coaching, not compiler output.
/// </summary>
public static class Parser
{
    // -------------------------------------------------------------------------
    // Precedence levels (higher = tighter binding)

    const int PrecOr         = 1;
    const int PrecAnd        = 2;
    const int PrecNot        = 3; // unary, right-assoc
    const int PrecMembership = 4;
    const int PrecComparison = 5;
    const int PrecAddSub     = 6;
    const int PrecMulDiv     = 7;
    const int PrecUnary      = 8;
    const int PrecPower      = 9; // right-assoc
    const int PrecPrimary    = 10;

    static readonly HashSet<string> Builtins = new HashSet<string>
    {
        "print", "len", "append", "pop", "range",
        "int", "str", "float", "min", "max", "sum", "sorted",
        "randint",
    };

    // -------------------------------------------------------------------------
    // Public API

    /// <summary>Lex + parse in one call.</summary>
    public static ProgramNode Compile(string source, out List<LangError> errors)
    {
        List<Token> tokens = Lexer.Tokenize(source, out errors);
        HashSet<string> functionNames = CollectFunctionNames(tokens);
        ProgramNode program = Parse(tokens, functionNames, out List<LangError> parseErrors);
        errors.AddRange(parseErrors);
        return program;
    }

    static HashSet<string> CollectFunctionNames(List<Token> tokens)
    {
        var names = new HashSet<string>();
        for (int i = 0; i < tokens.Count - 1; i++)
        {
            if (tokens[i].Type == TokenType.KeywordDef &&
                tokens[i + 1].Type == TokenType.Identifier)
            {
                names.Add(tokens[i + 1].Text);
            }
        }
        return names;
    }

    /// <summary>Parses a token list. Never throws — problems land in errors.</summary>
    public static ProgramNode Parse(List<Token> tokens, out List<LangError> errors)
    {
        return Parse(tokens, new HashSet<string>(), out errors);
    }

    static ProgramNode Parse(List<Token> tokens, HashSet<string> functionNames, out List<LangError> errors)
    {
        var state = new ParseState
        {
            Tokens = tokens,
            Errors = new List<LangError>(),
            FunctionNames = functionNames,
        };
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
            case TokenType.KeywordWhile:  return ParseWhile(s);
            case TokenType.KeywordIf:     return ParseIf(s);
            case TokenType.KeywordFor:    return ParseFor(s);
            case TokenType.KeywordRepeat: return ParseRepeat(s);
            case TokenType.KeywordDef:    return ParseDef(s);

            case TokenType.KeywordReturn:
                if (!s.InsideFunction)
                {
                    s.Error(t.Line, "'return' only makes sense inside a function.");
                    s.SkipToNextLine();
                    return null;
                }
                return ParseReturn(s);

            case TokenType.KeywordBreak:
                s.Advance();
                s.ExpectEndOfLine(t.Line);
                return new BreakStmt { Line = t.Line };

            case TokenType.KeywordContinue:
                s.Advance();
                s.ExpectEndOfLine(t.Line);
                return new ContinueStmt { Line = t.Line };

            case TokenType.KeywordElif:
                s.Error(t.Line, "this 'elif' has no matching 'if' above it.");
                s.SkipToNextLine();
                return null;

            case TokenType.KeywordElse:
                s.Error(t.Line, "this 'else' has no matching 'if' above it.");
                s.SkipToNextLine();
                return null;

            case TokenType.KeywordNot:
            case TokenType.KeywordAnd:
            case TokenType.KeywordOr:
                s.Error(t.Line, $"'{t.Text}' only makes sense inside an if or while condition.");
                s.SkipToNextLine();
                return null;

            case TokenType.Identifier:
                return ParseIdentifierStatement(s);

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

    static StmtNode ParseIdentifierStatement(ParseState s)
    {
        Token name = s.Advance();

        if (s.Match(TokenType.Assign))
            return ParseAssign(s, name);

        // a.append(...) or a[0].x? (only method calls are valid statements)
        if (s.Check(TokenType.Dot))
        {
            ExprNode target = ParseSuffixes(s, new VarExpr { Name = name.Text, Line = name.Line });
            if (target is CallExpr callExpr)
            {
                if (!ValidateStatementCall(s, callExpr))
                    return null;
                s.ExpectEndOfLine(name.Line);
                return new CallStmt { Name = callExpr.Name, Args = callExpr.Args, Line = callExpr.Line };
            }
            s.Error(name.Line, "you can only assign to a name or an index, like 'a[0] = 5'.");
            s.SkipToNextLine();
            return null;
        }

        // a[...] = ...
        if (s.Check(TokenType.LBracket))
        {
            ExprNode target = ParseSuffixes(s, new VarExpr { Name = name.Text, Line = name.Line });
            if (s.Match(TokenType.Assign))
            {
                ExprNode value = ParseExpr(s);
                s.ExpectEndOfLine(name.Line);
                if (target is IndexExpr idx)
                {
                    return new IndexAssignStmt
                    {
                        Container = idx.Container,
                        Index     = idx.Index,
                        Value     = value,
                        Line      = name.Line,
                    };
                }
                s.Error(name.Line, "you can only assign to a name or an index, like 'a[0] = 5'.");
                return null;
            }
            s.Error(name.Line, "expected '=' after the index.");
            s.SkipToNextLine();
            return null;
        }

        return ParseCall(s, name);
    }

    static StmtNode ParseAssign(ParseState s, Token target)
    {
        ExprNode value = ParseExpr(s);
        s.ExpectEndOfLine(target.Line);
        return new AssignStmt { Name = target.Text, Value = value, Line = target.Line };
    }

    static StmtNode ParseCall(ParseState s, Token name)
    {
        int line = name.Line;

        if (!s.Match(TokenType.LParen))
            s.Error(line, $"'{name.Text}' needs parentheses — write {name.Text}().");

        List<ExprNode> args = ParseArgs(s, line);

        if (!s.Match(TokenType.RParen))
            s.Error(line, $"missing the closing ')' after {name.Text}(.");

        s.ExpectEndOfLine(line);

        var call = new CallExpr { Name = name.Text, Args = args, Line = line };
        if (!ValidateStatementCall(s, call))
            return null;
        return new CallStmt { Name = call.Name, Args = call.Args, Line = call.Line };
    }

    static bool ValidateStatementCall(ParseState s, CallExpr call)
    {
        LangError arityError = AgentApi.CheckArity(call.Name, call.Args.Count, call.Line);
        if (arityError != null)
        {
            s.Errors.Add(arityError);
            return false;
        }

        if (AgentApi.IsQuery(call.Name))
        {
            s.Error(call.Line,
                $"{call.Name}() is a question, not an action — use it inside an if or while condition.");
            return false;
        }

        if (AgentApi.IsReporter(call.Name))
        {
            s.Error(call.Line,
                $"{call.Name}() gives back a value, so it doesn't do anything on its own — assign it to a variable or use it inside another block.");
            return false;
        }

        if (!AgentApi.IsAction(call.Name) && !IsBuiltin(call.Name) &&
            !s.FunctionNames.Contains(call.Name))
        {
            string suggestion = AgentApi.Suggest(call.Name);
            s.Error(call.Line, suggestion != null
                ? $"'{call.Name}' is not a known command. Did you mean '{suggestion}'?"
                : $"'{call.Name}' is not a known command.");
            return false;
        }

        return true;
    }

    static List<ExprNode> ParseArgs(ParseState s, int line)
    {
        var args = new List<ExprNode>();
        if (s.Check(TokenType.RParen))
            return args;

        args.Add(ParseExpr(s));
        while (s.Match(TokenType.Comma))
        {
            if (s.Check(TokenType.RParen))
            {
                s.Error(line, "there's an extra ',' with nothing after it.");
                break;
            }
            args.Add(ParseExpr(s));
        }
        return args;
    }

    static StmtNode ParseWhile(ParseState s)
    {
        Token kw = s.Advance();
        var node = new WhileStmt { Line = kw.Line };
        node.Condition = ParseCondition(s);
        node.Body      = ParseBlockAfterHeader(s, kw.Line, "while");
        return node;
    }

    static StmtNode ParseIf(ParseState s)
    {
        Token kw = s.Advance();
        var node = new IfStmt { Line = kw.Line };
        node.Condition = ParseCondition(s);
        node.Body      = ParseBlockAfterHeader(s, kw.Line, "if");

        while (s.Check(TokenType.KeywordElif))
        {
            Token elifKw = s.Advance();
            var elif = new ElifClause { Line = elifKw.Line };
            elif.Condition = ParseCondition(s);
            elif.Body      = ParseBlockAfterHeader(s, elifKw.Line, "elif");
            node.Elifs.Add(elif);
        }

        if (s.Check(TokenType.KeywordElse))
        {
            Token elseKw = s.Advance();
            node.ElseBody = ParseBlockAfterHeader(s, elseKw.Line, "else");
        }

        return node;
    }

    static StmtNode ParseFor(ParseState s)
    {
        Token kw = s.Advance();

        if (!s.Check(TokenType.Identifier))
        {
            s.Error(kw.Line, "for needs a variable name, like 'for i in range(3):'.");
            s.SkipToNextLine();
            return null;
        }

        Token var = s.Advance();
        if (!s.Match(TokenType.KeywordIn))
        {
            s.Error(var.Line, "for needs 'in' before the list or range, like 'for i in range(3):'.");
            s.SkipToNextLine();
            return null;
        }

        ExprNode iterable = ParseExpr(s);
        var node = new ForStmt { Var = var.Text, Iterable = iterable, Line = kw.Line };
        node.Body = ParseBlockAfterHeader(s, kw.Line, "for");
        return node;
    }

    static StmtNode ParseRepeat(ParseState s)
    {
        Token kw = s.Advance();
        ExprNode count = ParseExpr(s);
        var node = new ForStmt
        {
            Var = "_",
            Iterable = new CallExpr { Name = "range", Args = new List<ExprNode> { count }, Line = kw.Line },
            Line = kw.Line,
        };
        node.Body = ParseBlockAfterHeader(s, kw.Line, "repeat");
        return node;
    }

    static StmtNode ParseDef(ParseState s)
    {
        Token kw = s.Advance();

        if (!s.Check(TokenType.Identifier))
        {
            s.Error(kw.Line, "def needs a function name, like 'def helper():'.");
            s.SkipToNextLine();
            return null;
        }

        Token name = s.Advance();
        var node = new FuncDefStmt { Name = name.Text, Line = name.Line };

        if (!s.Match(TokenType.LParen))
            s.Error(name.Line, $"'{name.Text}' needs parentheses — write {name.Text}().");

        if (!s.Check(TokenType.RParen))
        {
            do
            {
                if (!s.Check(TokenType.Identifier))
                {
                    s.Error(name.Line, "function inputs must be simple names, like 'def helper(x, y):'.");
                    break;
                }
                node.Params.Add(s.Advance().Text);
            }
            while (s.Match(TokenType.Comma));
        }

        if (!s.Match(TokenType.RParen))
            s.Error(name.Line, $"missing the closing ')' after {name.Text}(.");

        bool wasInside = s.InsideFunction;
        s.InsideFunction = true;
        node.Body = ParseBlockAfterHeader(s, name.Line, "def");
        s.InsideFunction = wasInside;
        return node;
    }

    static StmtNode ParseReturn(ParseState s)
    {
        Token kw = s.Advance();
        ExprNode value = null;
        if (!s.Check(TokenType.Newline) && !s.Check(TokenType.EndOfFile))
            value = ParseExpr(s);
        s.ExpectEndOfLine(kw.Line);
        return new ReturnStmt { Value = value, Line = kw.Line };
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

    static ExprNode ParseCondition(ParseState s)
    {
        ExprNode expr = ParseExpr(s);
        ValidateNoActionsInCondition(expr, s);
        return expr;
    }

    static void ValidateNoActionsInCondition(ExprNode expr, ParseState s)
    {
        if (expr == null) return;

        switch (expr)
        {
            case CallExpr call when AgentApi.IsAction(call.Name):
                s.Errors.Add(new LangError(call.Line,
                    $"{call.Name}() is an action, not a question — it can't be used as a condition."));
                break;
            case BinaryExpr bin:
                ValidateNoActionsInCondition(bin.Left, s);
                ValidateNoActionsInCondition(bin.Right, s);
                break;
            case UnaryExpr un:
                ValidateNoActionsInCondition(un.Operand, s);
                break;
        }
    }

    static ExprNode ParseExpr(ParseState s) => ParsePrecedence(s, PrecOr);

    static ExprNode ParsePrecedence(ParseState s, int minPrec)
    {
        ExprNode left = ParsePrefix(s);

        while (true)
        {
            Token op = s.Peek();

            // 'not in' is a single membership operator.
            if (op.Type == TokenType.KeywordNot && s.Peek(1).Type == TokenType.KeywordIn)
            {
                if (PrecMembership < minPrec) break;
                int line = op.Line;
                s.Advance(); // not
                s.Advance(); // in
                ExprNode memberRight = ParsePrecedence(s, PrecMembership + 1);
                left = new UnaryExpr
                {
                    Op = TokenType.KeywordNot,
                    Operand = new BinaryExpr { Left = left, Op = TokenType.KeywordIn, Right = memberRight, Line = line },
                    Line = line,
                };
                continue;
            }

            int prec = InfixPrecedence(op.Type);
            if (prec < minPrec) break;

            bool rightAssoc = op.Type == TokenType.StarStar;
            s.Advance();
            ExprNode right = ParsePrecedence(s, rightAssoc ? prec : prec + 1);
            left = new BinaryExpr { Left = left, Op = op.Type, Right = right, Line = op.Line };
        }

        return left;
    }

    static ExprNode ParsePrefix(ParseState s)
    {
        Token t = s.Peek();

        if (t.Type == TokenType.Minus || t.Type == TokenType.Plus || t.Type == TokenType.KeywordNot)
        {
            s.Advance();
            ExprNode operand = ParsePrecedence(s, PrecUnary);
            return new UnaryExpr { Op = t.Type, Operand = operand, Line = t.Line };
        }

        return ParsePrimary(s);
    }

    static ExprNode ParsePrimary(ParseState s)
    {
        Token t = s.Peek();

        switch (t.Type)
        {
            case TokenType.Number:
                s.Advance();
                return LiteralFromNumber(t);

            case TokenType.String:
                s.Advance();
                return new LiteralExpr { Value = Value.Str(t.Text), Line = t.Line };

            case TokenType.KeywordTrue:
                s.Advance();
                return new LiteralExpr { Value = Value.Bool(true), Line = t.Line };

            case TokenType.KeywordFalse:
                s.Advance();
                return new LiteralExpr { Value = Value.Bool(false), Line = t.Line };

            case TokenType.KeywordNone:
                s.Advance();
                return new LiteralExpr { Value = Value.None, Line = t.Line };

            case TokenType.LParen:
                return ParseParenOrTuple(s);

            case TokenType.LBracket:
                return ParseListLiteral(s);

            case TokenType.LBrace:
                return ParseDictLiteral(s);

            case TokenType.Identifier:
                s.Advance();
                return ParseSuffixes(s, new VarExpr { Name = t.Text, Line = t.Line });

            default:
                s.Error(t.Line, "expected a value here, like a number, word, or variable.");
                return new LiteralExpr { Value = Value.None, Line = t.Line };
        }
    }

    static ExprNode ParseSuffixes(ParseState s, ExprNode primary)
    {
        while (true)
        {
            if (s.Check(TokenType.LParen) && primary is VarExpr v)
            {
                primary = ParseCallSuffix(s, v);
                continue;
            }
            if (s.Check(TokenType.LBracket))
            {
                primary = ParseIndexSuffix(s, primary);
                continue;
            }
            if (s.Check(TokenType.Dot))
            {
                primary = ParseDotSuffix(s, primary);
                continue;
            }
            break;
        }
        return primary;
    }

    static ExprNode ParseCallSuffix(ParseState s, VarExpr target)
    {
        int line = s.Peek().Line;
        s.Advance(); // (
        List<ExprNode> args = ParseArgs(s, line);
        if (!s.Match(TokenType.RParen))
            s.Error(line, $"missing the closing ')' after {target.Name}(.");
        return new CallExpr { Name = target.Name, Args = args, Line = line };
    }

    static ExprNode ParseIndexSuffix(ParseState s, ExprNode primary)
    {
        int line = s.Peek().Line;
        s.Advance(); // [
        ExprNode start = ParseExpr(s);

        if (s.Match(TokenType.Colon))
        {
            ExprNode stop = null, step = null;
            if (!s.Check(TokenType.RBracket)) stop = ParseExpr(s);
            if (s.Match(TokenType.Colon))
            {
                if (!s.Check(TokenType.RBracket)) step = ParseExpr(s);
            }
            if (!s.Match(TokenType.RBracket))
                s.Error(line, "missing the closing ']' for this index.");
            return new IndexExpr { Container = primary, Index = start, Stop = stop, Step = step, Line = line };
        }

        if (!s.Match(TokenType.RBracket))
            s.Error(line, "missing the closing ']' for this index.");
        return new IndexExpr { Container = primary, Index = start, Line = line };
    }

    static ExprNode ParseDotSuffix(ParseState s, ExprNode primary)
    {
        int line = s.Peek().Line;
        s.Advance(); // .

        if (!s.Check(TokenType.Identifier))
        {
            s.Error(line, "expected a name after '.'.");
            return primary;
        }

        Token member = s.Advance();

        if (s.Match(TokenType.LParen))
        {
            List<ExprNode> args = ParseArgs(s, member.Line);
            if (!s.Match(TokenType.RParen))
                s.Error(member.Line, $"missing the closing ')' after {member.Text}(.");
            args.Insert(0, primary);
            return new CallExpr { Name = member.Text, Args = args, Line = member.Line };
        }

        s.Error(member.Line, "you can't read a property like that — use a function instead.");
        return primary;
    }

    static ExprNode ParseParenOrTuple(ParseState s)
    {
        int line = s.Peek().Line;
        s.Advance(); // (
        ExprNode first = ParseExpr(s);

        if (s.Match(TokenType.RParen))
            return first; // grouping

        var items = new List<ExprNode> { first };
        while (s.Match(TokenType.Comma))
        {
            if (s.Check(TokenType.RParen)) break;
            items.Add(ParseExpr(s));
        }

        if (!s.Match(TokenType.RParen))
            s.Error(line, "missing the closing ')' for this group.");

        return new TupleExpr { Items = items, Line = line };
    }

    static ExprNode ParseListLiteral(ParseState s)
    {
        int line = s.Peek().Line;
        s.Advance(); // [
        var items = new List<ExprNode>();

        if (!s.Check(TokenType.RBracket))
        {
            items.Add(ParseExpr(s));
            while (s.Match(TokenType.Comma))
            {
                if (s.Check(TokenType.RBracket)) break;
                items.Add(ParseExpr(s));
            }
        }

        if (!s.Match(TokenType.RBracket))
            s.Error(line, "missing the closing ']' for this list.");

        return new ListExpr { Items = items, Line = line };
    }

    static ExprNode ParseDictLiteral(ParseState s)
    {
        int line = s.Peek().Line;
        s.Advance(); // {
        var entries = new List<KeyValuePair<ExprNode, ExprNode>>();

        if (!s.Check(TokenType.RBrace))
        {
            do
            {
                ExprNode key = ParseExpr(s);
                if (!s.Match(TokenType.Colon))
                {
                    s.Error(line, "dictionary entries need a ':' between the key and value.");
                    break;
                }
                ExprNode value = ParseExpr(s);
                entries.Add(new KeyValuePair<ExprNode, ExprNode>(key, value));
            }
            while (s.Match(TokenType.Comma));
        }

        if (!s.Match(TokenType.RBrace))
            s.Error(line, "missing the closing '}' for this dictionary.");

        return new DictExpr { Entries = entries, Line = line };
    }

    static ExprNode LiteralFromNumber(Token t)
    {
        if (t.Text.Contains("."))
        {
            if (double.TryParse(t.Text, System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture, out double f))
                return new LiteralExpr { Value = Value.Float(f), Line = t.Line };
        }
        else
        {
            if (long.TryParse(t.Text, out long i))
                return new LiteralExpr { Value = Value.Int(i), Line = t.Line };
        }
        return new LiteralExpr { Value = Value.Int(0), Line = t.Line };
    }

    static bool IsBuiltin(string name) => Builtins.Contains(name);

    // -------------------------------------------------------------------------
    // Operator tables

    static int InfixPrecedence(TokenType type)
    {
        switch (type)
        {
            case TokenType.KeywordOr:  return PrecOr;
            case TokenType.KeywordAnd: return PrecAnd;
            case TokenType.KeywordIn:  return PrecMembership;
            case TokenType.EqEq:
            case TokenType.NotEq:
            case TokenType.Lt:
            case TokenType.Gt:
            case TokenType.Le:
            case TokenType.Ge:
                return PrecComparison;
            case TokenType.Plus:
            case TokenType.Minus:
                return PrecAddSub;
            case TokenType.Star:
            case TokenType.Slash:
            case TokenType.SlashSlash:
            case TokenType.Percent:
                return PrecMulDiv;
            case TokenType.StarStar:
                return PrecPower;
            default:
                return -1;
        }
    }

    // -------------------------------------------------------------------------
    // Parse state / cursor helpers

    class ParseState
    {
        public List<Token>     Tokens;
        public List<LangError> Errors;
        public int             Pos;
        public bool            InsideFunction;
        public HashSet<string> FunctionNames;

        public Token Peek() => Tokens[Pos];
        public Token Peek(int offset) => Tokens[Pos + offset];

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
