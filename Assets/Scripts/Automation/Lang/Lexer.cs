using System.Collections.Generic;

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

            if (c == '(') { tokens.Add(new Token(TokenType.LParen, "(", lineNo)); pos++; continue; }
            if (c == ')') { tokens.Add(new Token(TokenType.RParen, ")", lineNo)); pos++; continue; }
            if (c == ':') { tokens.Add(new Token(TokenType.Colon,  ":", lineNo)); pos++; continue; }

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

    static TokenType KeywordType(string word)
    {
        switch (word)
        {
            case "while": return TokenType.KeywordWhile;
            case "if":    return TokenType.KeywordIf;
            case "else":  return TokenType.KeywordElse;
            case "not":   return TokenType.KeywordNot;
            default:      return TokenType.Identifier;
        }
    }
}
