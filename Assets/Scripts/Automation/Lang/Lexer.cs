using System.Collections.Generic;
using System.Text;

/// <summary>
/// Line-based lexer for the Python-style automation language.
/// Indentation is tracked with a stack (tabs count as 4 spaces) and emitted
/// as Indent/Dedent tokens; '#' starts a comment; blank lines are skipped.
/// All problems come back as plain-English <see cref="LangError"/>s.
/// </summary>
public static class Lexer
{
    const int TabWidth = 4;

    // -------------------------------------------------------------------------

    /// <summary>Tokenizes source text. Never throws — problems land in errors.</summary>
    public static List<Token> Tokenize(string source, out List<LangError> errors)
    {
        errors = new List<LangError>();
        var tokens = new List<Token>();
        var indents = new Stack<int>();
        indents.Push(0);

        string[] lines = (source ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            int lineNo = i + 1;
            string line = lines[i];

            // Strip comments.
            int hash = line.IndexOf('#');
            if (hash >= 0) line = line.Substring(0, hash);

            // Skip blank / comment-only lines entirely (no Newline emitted).
            if (line.Trim().Length == 0) continue;

            // Measure indentation (spaces; a tab counts as TabWidth).
            int indent = 0;
            int pos = 0;
            while (pos < line.Length && (line[pos] == ' ' || line[pos] == '\t'))
            {
                indent += line[pos] == '\t' ? TabWidth : 1;
                pos++;
            }

            // Compare against the indentation stack.
            if (indent > indents.Peek())
            {
                indents.Push(indent);
                tokens.Add(new Token(TokenType.Indent, "", lineNo));
            }
            else
            {
                while (indent < indents.Peek())
                {
                    indents.Pop();
                    tokens.Add(new Token(TokenType.Dedent, "", lineNo));
                }
                if (indent != indents.Peek())
                {
                    errors.Add(new LangError(lineNo,
                        "this line's indentation doesn't line up with any surrounding block."));
                    // Recover by treating it as the nearest enclosing level.
                }
            }

            // Scan the body of the line.
            ScanLineBody(line, pos, lineNo, tokens, errors);

            tokens.Add(new Token(TokenType.Newline, "", lineNo));
        }

        // Close any open blocks at end of file.
        int lastLine = lines.Length;
        while (indents.Peek() > 0)
        {
            indents.Pop();
            tokens.Add(new Token(TokenType.Dedent, "", lastLine));
        }

        tokens.Add(new Token(TokenType.EndOfFile, "", lastLine));
        return tokens;
    }

    // -------------------------------------------------------------------------

    static void ScanLineBody(string line, int pos, int lineNo,
                             List<Token> tokens, List<LangError> errors)
    {
        while (pos < line.Length)
        {
            char c = line[pos];

            if (c == ' ' || c == '\t') { pos++; continue; }

            if (c == '(') { tokens.Add(new Token(TokenType.LParen,   "(", lineNo)); pos++; continue; }
            if (c == ')') { tokens.Add(new Token(TokenType.RParen,   ")", lineNo)); pos++; continue; }
            if (c == '[') { tokens.Add(new Token(TokenType.LBracket, "[", lineNo)); pos++; continue; }
            if (c == ']') { tokens.Add(new Token(TokenType.RBracket, "]", lineNo)); pos++; continue; }
            if (c == '{') { tokens.Add(new Token(TokenType.LBrace,   "{", lineNo)); pos++; continue; }
            if (c == '}') { tokens.Add(new Token(TokenType.RBrace,   "}", lineNo)); pos++; continue; }
            if (c == ':') { tokens.Add(new Token(TokenType.Colon,    ":", lineNo)); pos++; continue; }
            if (c == ',') { tokens.Add(new Token(TokenType.Comma,    ",", lineNo)); pos++; continue; }
            if (c == '.') { tokens.Add(new Token(TokenType.Dot,      ".", lineNo)); pos++; continue; }
            if (c == '+') { tokens.Add(new Token(TokenType.Plus,   "+", lineNo)); pos++; continue; }
            if (c == '%') { tokens.Add(new Token(TokenType.Percent,"%", lineNo)); pos++; continue; }

            if (c == '-')
            {
                tokens.Add(new Token(TokenType.Minus, "-", lineNo));
                pos++;
                continue;
            }

            if (c == '*')
            {
                if (pos + 1 < line.Length && line[pos + 1] == '*')
                {
                    tokens.Add(new Token(TokenType.StarStar, "**", lineNo));
                    pos += 2;
                }
                else
                {
                    tokens.Add(new Token(TokenType.Star, "*", lineNo));
                    pos++;
                }
                continue;
            }

            if (c == '/')
            {
                if (pos + 1 < line.Length && line[pos + 1] == '/')
                {
                    tokens.Add(new Token(TokenType.SlashSlash, "//", lineNo));
                    pos += 2;
                }
                else
                {
                    tokens.Add(new Token(TokenType.Slash, "/", lineNo));
                    pos++;
                }
                continue;
            }

            if (c == '=')
            {
                if (pos + 1 < line.Length && line[pos + 1] == '=')
                {
                    tokens.Add(new Token(TokenType.EqEq, "==", lineNo));
                    pos += 2;
                }
                else
                {
                    tokens.Add(new Token(TokenType.Assign, "=", lineNo));
                    pos++;
                }
                continue;
            }

            if (c == '!')
            {
                if (pos + 1 < line.Length && line[pos + 1] == '=')
                {
                    tokens.Add(new Token(TokenType.NotEq, "!=", lineNo));
                    pos += 2;
                }
                else
                {
                    errors.Add(new LangError(lineNo, $"I don't understand the character '{c}' here."));
                    pos++;
                }
                continue;
            }

            if (c == '<')
            {
                if (pos + 1 < line.Length && line[pos + 1] == '=')
                {
                    tokens.Add(new Token(TokenType.Le, "<=", lineNo));
                    pos += 2;
                }
                else
                {
                    tokens.Add(new Token(TokenType.Lt, "<", lineNo));
                    pos++;
                }
                continue;
            }

            if (c == '>')
            {
                if (pos + 1 < line.Length && line[pos + 1] == '=')
                {
                    tokens.Add(new Token(TokenType.Ge, ">=", lineNo));
                    pos += 2;
                }
                else
                {
                    tokens.Add(new Token(TokenType.Gt, ">", lineNo));
                    pos++;
                }
                continue;
            }

            if (c == '"' || c == '\'')
            {
                pos = ScanString(line, pos, lineNo, tokens, errors);
                continue;
            }

            if (char.IsDigit(c))
            {
                pos = ScanNumber(line, pos, lineNo, tokens);
                continue;
            }

            if (char.IsLetter(c) || c == '_')
            {
                int start = pos;
                while (pos < line.Length && (char.IsLetterOrDigit(line[pos]) || line[pos] == '_'))
                    pos++;

                string word = line.Substring(start, pos - start);
                tokens.Add(new Token(KeywordType(word), word, lineNo));
                continue;
            }

            errors.Add(new LangError(lineNo, $"I don't understand the character '{c}'."));
            pos++;
        }
    }

    static int ScanNumber(string line, int pos, int lineNo, List<Token> tokens)
    {
        int start = pos;
        bool sawDot = false;
        while (pos < line.Length && (char.IsDigit(line[pos]) || line[pos] == '.'))
        {
            if (line[pos] == '.')
            {
                if (sawDot) break;
                sawDot = true;
            }
            pos++;
        }
        tokens.Add(new Token(TokenType.Number, line.Substring(start, pos - start), lineNo));
        return pos;
    }

    static int ScanString(string line, int start, int lineNo,
                          List<Token> tokens, List<LangError> errors)
    {
        char quote = line[start];
        int pos = start + 1;
        var sb = new StringBuilder();

        while (pos < line.Length && line[pos] != quote)
        {
            if (line[pos] == '\\' && pos + 1 < line.Length)
            {
                char esc = line[pos + 1];
                switch (esc)
                {
                    case 'n':  sb.Append('\n'); break;
                    case 't':  sb.Append('\t'); break;
                    case '\\': sb.Append('\\'); break;
                    case '"':  sb.Append('"');  break;
                    case '\'': sb.Append('\''); break;
                    default:
                        sb.Append('\\').Append(esc);
                        break;
                }
                pos += 2;
            }
            else
            {
                sb.Append(line[pos]);
                pos++;
            }
        }

        if (pos >= line.Length)
        {
            errors.Add(new LangError(lineNo,
                $"this string started with '{quote}' but never closed — add a matching '{quote}' at the end."));
            tokens.Add(new Token(TokenType.String, sb.ToString(), lineNo));
            return pos;
        }

        tokens.Add(new Token(TokenType.String, sb.ToString(), lineNo));
        return pos + 1; // skip closing quote
    }

    static TokenType KeywordType(string word)
    {
        switch (word)
        {
            case "while":  return TokenType.KeywordWhile;
            case "if":     return TokenType.KeywordIf;
            case "elif":   return TokenType.KeywordElif;
            case "else":   return TokenType.KeywordElse;
            case "for":    return TokenType.KeywordFor;
            case "repeat": return TokenType.KeywordRepeat;
            case "def":    return TokenType.KeywordDef;
            case "return": return TokenType.KeywordReturn;
            case "break":  return TokenType.KeywordBreak;
            case "continue":return TokenType.KeywordContinue;
            case "not":    return TokenType.KeywordNot;
            case "and":    return TokenType.KeywordAnd;
            case "or":     return TokenType.KeywordOr;
            case "in":     return TokenType.KeywordIn;
            case "True":   return TokenType.KeywordTrue;
            case "False":  return TokenType.KeywordFalse;
            case "None":   return TokenType.KeywordNone;
            default:      return TokenType.Identifier;
        }
    }
}
