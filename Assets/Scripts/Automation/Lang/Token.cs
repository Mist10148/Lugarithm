/// <summary>
/// Token kinds and the token struct produced by <see cref="Lexer"/>.
/// The language is Python-style: indentation becomes Indent/Dedent tokens.
/// </summary>
public enum TokenType
{
    Identifier,
    KeywordWhile,
    KeywordIf,
    KeywordElif,
    KeywordElse,
    KeywordFor,
    KeywordRepeat,
    KeywordDef,
    KeywordReturn,
    KeywordBreak,
    KeywordContinue,
    KeywordNot,
    KeywordAnd,
    KeywordOr,
    KeywordIn,
    KeywordTrue,
    KeywordFalse,
    KeywordNone,
    LParen,
    RParen,
    LBracket,
    RBracket,
    LBrace,
    RBrace,
    Colon,
    Comma,
    Dot,
    Assign,
    Plus,
    Minus,
    Star,
    Slash,
    SlashSlash,
    Percent,
    StarStar,
    EqEq,
    NotEq,
    Lt,
    Gt,
    Le,
    Ge,
    Number,
    String,
    Newline,
    Indent,
    Dedent,
    EndOfFile,
}

/// <summary>One lexed token with its 1-based source line.</summary>
public struct Token
{
    public TokenType Type;
    public string    Text;
    public int       Line;

    public Token(TokenType type, string text, int line)
    {
        Type = type;
        Text = text;
        Line = line;
    }

    public override string ToString() => $"{Type}('{Text}')@{Line}";
}
