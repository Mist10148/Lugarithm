using System.Collections.Generic;
using NUnit.Framework;

/// <summary>EditMode tests for the parser's tree shapes and coaching errors.</summary>
public class ParserTests
{
    static ProgramNode Compile(string source, out List<LangError> errors)
    {
        return Parser.Compile(source, out errors);
    }

    // -------------------------------------------------------------------------
    // Tree shapes

    [Test]
    public void LinearProgram_ParsesToCallsInOrder()
    {
        var program = Compile("moveForward()\nturnLeft()\nmoveForward()\n", out var errors);

        CollectionAssert.IsEmpty(errors);
        Assert.AreEqual(3, program.Statements.Count);
        Assert.AreEqual("moveForward", ((CallStmt)program.Statements[0]).Name);
        Assert.AreEqual("turnLeft",    ((CallStmt)program.Statements[1]).Name);
    }

    [Test]
    public void WhileNot_ParsesConditionAndBody()
    {
        string source =
            "while not atDestination():\n" +
            "    moveForward()\n";

        var program = Compile(source, out var errors);

        CollectionAssert.IsEmpty(errors);
        var loop = (WhileStmt)program.Statements[0];
        var not  = (NotExpr)loop.Condition;
        Assert.AreEqual("atDestination", ((QueryExpr)not.Operand).Name);
        Assert.AreEqual(1, loop.Body.Count);
    }

    [Test]
    public void IfElse_ParsesBothBranches()
    {
        string source =
            "if frontIsClear():\n" +
            "    moveForward()\n" +
            "else:\n" +
            "    turnLeft()\n" +
            "    turnLeft()\n";

        var program = Compile(source, out var errors);

        CollectionAssert.IsEmpty(errors);
        var branch = (IfStmt)program.Statements[0];
        Assert.AreEqual(1, branch.Body.Count);
        Assert.AreEqual(2, branch.ElseBody.Count);
    }

    [Test]
    public void NestedIfInsideWhile_Parses()
    {
        string source =
            "while not atDestination():\n" +
            "    if rightIsClear():\n" +
            "        turnRight()\n" +
            "        moveForward()\n" +
            "    else:\n" +
            "        turnLeft()\n";

        var program = Compile(source, out var errors);

        CollectionAssert.IsEmpty(errors);
        var loop   = (WhileStmt)program.Statements[0];
        var branch = (IfStmt)loop.Body[0];
        Assert.AreEqual(2, branch.Body.Count);
        Assert.AreEqual(1, branch.ElseBody.Count);
    }

    [Test]
    public void LineNumbers_AreCarriedOntoNodes()
    {
        var program = Compile("moveForward()\nturnLeft()\n", out _);

        Assert.AreEqual(1, program.Statements[0].Line);
        Assert.AreEqual(2, program.Statements[1].Line);
    }

    // -------------------------------------------------------------------------
    // Coaching errors

    [Test]
    public void MissingColon_IsExplained()
    {
        Compile("while frontIsClear()\n    moveForward()\n", out var errors);

        Assert.IsTrue(errors.Exists(e => e.Message.Contains("':'")),
            "expected a missing-colon error, got: " + string.Join(" | ", errors));
    }

    [Test]
    public void EmptyBody_IsExplained()
    {
        Compile("if frontIsClear():\nmoveForward()\n", out var errors);

        Assert.IsTrue(errors.Exists(e => e.Message.Contains("indented")),
            "expected an empty-body error, got: " + string.Join(" | ", errors));
    }

    [Test]
    public void MisspelledCommand_GetsDidYouMean()
    {
        Compile("moveForwad()\n", out var errors);

        Assert.AreEqual(1, errors.Count);
        StringAssert.Contains("moveForward", errors[0].Message);
        StringAssert.Contains("Did you mean", errors[0].Message);
    }

    [Test]
    public void QueryUsedAsStatement_IsExplained()
    {
        Compile("frontIsClear()\n", out var errors);

        Assert.AreEqual(1, errors.Count);
        StringAssert.Contains("question", errors[0].Message);
    }

    [Test]
    public void ActionUsedAsCondition_IsExplained()
    {
        Compile("while moveForward():\n    turnLeft()\n", out var errors);

        Assert.IsTrue(errors.Exists(e => e.Message.Contains("action")),
            "expected an action-as-condition error, got: " + string.Join(" | ", errors));
    }

    [Test]
    public void ElseWithoutIf_IsExplained()
    {
        Compile("else:\n    moveForward()\n", out var errors);

        Assert.IsTrue(errors.Exists(e => e.Message.Contains("else")),
            "expected an orphan-else error, got: " + string.Join(" | ", errors));
    }

    [Test]
    public void ScaffoldComments_ParseToNothing()
    {
        var def = LevelLibrary.Get(1).auto;
        var program = Compile(def.codeScaffold, out var errors);

        CollectionAssert.IsEmpty(errors);
        CollectionAssert.IsEmpty(program.Statements);
    }
}
