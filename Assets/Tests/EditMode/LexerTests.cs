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
        var tokens = Lexer.Tokenize("while if else not\n", out _);

        Assert.AreEqual(TokenType.KeywordWhile, tokens[0].Type);
        Assert.AreEqual(TokenType.KeywordIf,    tokens[1].Type);
        Assert.AreEqual(TokenType.KeywordElse,  tokens[2].Type);
        Assert.AreEqual(TokenType.KeywordNot,   tokens[3].Type);
    }
}
