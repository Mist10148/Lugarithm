using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>EditMode tests for the indentation-aware lexer.</summary>
public class LexerTests
{
    static List<TokenType> Types(string source, out List<LangError> errors)
    {
        return Lexer.Tokenize(source, out errors).Select(t => t.Type).ToList();
    }

    // -------------------------------------------------------------------------

    [Test]
    public void SimpleCall_TokenizesCleanly()
    {
        var types = Types("moveForward()\n", out var errors);

        CollectionAssert.IsEmpty(errors);
        CollectionAssert.AreEqual(new[]
        {
            TokenType.Identifier, TokenType.LParen, TokenType.RParen,
            TokenType.Newline, TokenType.EndOfFile,
        }, types);
    }

    [Test]
    public void IndentedBlock_EmitsIndentAndDedent()
    {
        string source =
            "while frontIsClear():\n" +
            "    moveForward()\n" +
            "turnLeft()\n";

        var types = Types(source, out var errors);

        CollectionAssert.IsEmpty(errors);
        CollectionAssert.AreEqual(new[]
        {
            TokenType.KeywordWhile, TokenType.Identifier, TokenType.LParen, TokenType.RParen,
            TokenType.Colon, TokenType.Newline,
            TokenType.Indent,
            TokenType.Identifier, TokenType.LParen, TokenType.RParen, TokenType.Newline,
            TokenType.Dedent,
            TokenType.Identifier, TokenType.LParen, TokenType.RParen, TokenType.Newline,
            TokenType.EndOfFile,
        }, types);
    }

    [Test]
    public void NestedBlocks_CloseInOrderAtEndOfFile()
    {
        string source =
            "if atStop():\n" +
            "    if frontIsClear():\n" +
            "        moveForward()\n";

        var types = Types(source, out var errors);

        CollectionAssert.IsEmpty(errors);
        Assert.AreEqual(2, types.Count(t => t == TokenType.Indent));
        Assert.AreEqual(2, types.Count(t => t == TokenType.Dedent));
        Assert.AreEqual(TokenType.EndOfFile, types.Last());
    }

    [Test]
    public void CommentsAndBlankLines_AreSkippedEntirely()
    {
        string source =
            "# a header comment\n" +
            "\n" +
            "moveForward()  # trailing comment\n" +
            "   \n" +
            "turnLeft()\n";

        var tokens = Lexer.Tokenize(source, out var errors);

        CollectionAssert.IsEmpty(errors);
        Assert.AreEqual(2, tokens.Count(t => t.Type == TokenType.Newline));
        Assert.AreEqual(2, tokens.Count(t => t.Type == TokenType.Identifier));
    }

    [Test]
    public void HashInsideString_IsNotAComment()
    {
        string source = "print(\"hello # world\")\n";

        var tokens = Lexer.Tokenize(source, out var errors);

        CollectionAssert.IsEmpty(errors);
        Assert.AreEqual(TokenType.Identifier, tokens[0].Type); // print
        Assert.AreEqual(TokenType.LParen,     tokens[1].Type);
        Assert.AreEqual(TokenType.String,     tokens[2].Type);
        Assert.AreEqual("hello # world",      tokens[2].Text);
    }

    [Test]
    public void MisalignedDedent_ReportsPlainEnglishError()
    {
        string source =
            "if atStop():\n" +
            "    pickUp()\n" +
            "  turnLeft()\n";   // 2 spaces matches no enclosing level

        Lexer.Tokenize(source, out var errors);

        Assert.AreEqual(1, errors.Count);
        Assert.AreEqual(3, errors[0].Line);
        StringAssert.Contains("indentation", errors[0].Message);
    }

    [Test]
    public void TabsCountAsFourSpaces()
    {
        string source =
            "if atStop():\n" +
            "\tpickUp()\n" +
            "turnLeft()\n";

        Types(source, out var errors);
        CollectionAssert.IsEmpty(errors);
    }

    [Test]
    public void UnknownCharacter_IsReportedWithItsLine()
    {
        Lexer.Tokenize("moveForward() $\n", out var errors);

        Assert.AreEqual(1, errors.Count);
        Assert.AreEqual(1, errors[0].Line);
        StringAssert.Contains("'$'", errors[0].Message);
    }

    [Test]
    public void KeywordsAreRecognized()
    {
        var tokens = Lexer.Tokenize("while if elif else for repeat def return break continue not and or in True False None\n", out _);

        Assert.AreEqual(TokenType.KeywordWhile,    tokens[0].Type);
        Assert.AreEqual(TokenType.KeywordIf,       tokens[1].Type);
        Assert.AreEqual(TokenType.KeywordElif,     tokens[2].Type);
        Assert.AreEqual(TokenType.KeywordElse,     tokens[3].Type);
        Assert.AreEqual(TokenType.KeywordFor,      tokens[4].Type);
        Assert.AreEqual(TokenType.KeywordRepeat,   tokens[5].Type);
        Assert.AreEqual(TokenType.KeywordDef,      tokens[6].Type);
        Assert.AreEqual(TokenType.KeywordReturn,   tokens[7].Type);
        Assert.AreEqual(TokenType.KeywordBreak,    tokens[8].Type);
        Assert.AreEqual(TokenType.KeywordContinue, tokens[9].Type);
        Assert.AreEqual(TokenType.KeywordNot,      tokens[10].Type);
        Assert.AreEqual(TokenType.KeywordAnd,      tokens[11].Type);
        Assert.AreEqual(TokenType.KeywordOr,       tokens[12].Type);
        Assert.AreEqual(TokenType.KeywordIn,       tokens[13].Type);
        Assert.AreEqual(TokenType.KeywordTrue,     tokens[14].Type);
        Assert.AreEqual(TokenType.KeywordFalse,    tokens[15].Type);
        Assert.AreEqual(TokenType.KeywordNone,     tokens[16].Type);
    }

    // -------------------------------------------------------------------------
    // Phase 1 — literals and punctuation

    [Test]
    public void NumberLiteral_TokenizesIntAndFloat()
    {
        var tokens = Lexer.Tokenize("123 45.67\n", out var errors);
        CollectionAssert.IsEmpty(errors);

        Assert.AreEqual(TokenType.Number, tokens[0].Type);
        Assert.AreEqual("123", tokens[0].Text);
        Assert.AreEqual(TokenType.Number, tokens[1].Type);
        Assert.AreEqual("45.67", tokens[1].Text);
    }

    [Test]
    public void StringLiteral_TokenizesWithEscapes()
    {
        var tokens = Lexer.Tokenize("\"Para\"\n", out var errors);
        CollectionAssert.IsEmpty(errors);

        Assert.AreEqual(TokenType.String, tokens[0].Type);
        Assert.AreEqual("Para", tokens[0].Text);
    }

    [Test]
    public void StringLiteral_EscapesAreDecoded()
    {
        var tokens = Lexer.Tokenize("\"a\\nb\\tc\"\n", out var errors);
        CollectionAssert.IsEmpty(errors);

        Assert.AreEqual(TokenType.String, tokens[0].Type);
        Assert.AreEqual("a\nb\tc", tokens[0].Text);
    }

    [Test]
    public void UnterminatedString_IsReported()
    {
        Lexer.Tokenize("\"hello\n", out var errors);

        Assert.AreEqual(1, errors.Count);
        StringAssert.Contains("string", errors[0].Message);
        StringAssert.Contains("closed", errors[0].Message);
    }

    [Test]
    public void CommaAndAssign_Tokenize()
    {
        var types = Types("x = 5, y\n", out var errors);
        CollectionAssert.IsEmpty(errors);

        CollectionAssert.AreEqual(new[]
        {
            TokenType.Identifier, TokenType.Assign, TokenType.Number,
            TokenType.Comma, TokenType.Identifier,
            TokenType.Newline, TokenType.EndOfFile,
        }, types);
    }

    // -------------------------------------------------------------------------
    // Phase 2 — operators

    [Test]
    public void Operators_Tokenize()
    {
        var types = Types("+ - * / // % ** == != < > <= >=\n", out var errors);
        CollectionAssert.IsEmpty(errors);

        CollectionAssert.AreEqual(new[]
        {
            TokenType.Plus, TokenType.Minus, TokenType.Star, TokenType.Slash,
            TokenType.SlashSlash, TokenType.Percent, TokenType.StarStar,
            TokenType.EqEq, TokenType.NotEq, TokenType.Lt, TokenType.Gt,
            TokenType.Le, TokenType.Ge,
            TokenType.Newline, TokenType.EndOfFile,
        }, types);
    }

    [Test]
    public void BracketsBracesDot_Tokenize()
    {
        var types = Types("[ ] { } .\n", out var errors);
        CollectionAssert.IsEmpty(errors);

        CollectionAssert.AreEqual(new[]
        {
            TokenType.LBracket, TokenType.RBracket,
            TokenType.LBrace, TokenType.RBrace,
            TokenType.Dot,
            TokenType.Newline, TokenType.EndOfFile,
        }, types);
    }

    [Test]
    public void NotIn_TokenizesAsTwoKeywords()
    {
        var types = Types("x not in y\n", out var errors);
        CollectionAssert.IsEmpty(errors);

        Assert.AreEqual(TokenType.Identifier, types[0]);
        Assert.AreEqual(TokenType.KeywordNot, types[1]);
        Assert.AreEqual(TokenType.KeywordIn,  types[2]);
        Assert.AreEqual(TokenType.Identifier, types[3]);
    }
}
