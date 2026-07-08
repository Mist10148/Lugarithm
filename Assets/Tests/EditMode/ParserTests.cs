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
    public void AvoidTraffic_ParsesAsZeroArgAction()
    {
        var program = Compile("avoidTraffic()\n", out var errors);

        CollectionAssert.IsEmpty(errors);
        Assert.AreEqual("avoidTraffic", ((CallStmt)program.Statements[0]).Name);
    }

    [Test]
    public void AvoidTraffic_WithAnArgument_IsAnArityError()
    {
        Compile("avoidTraffic(1)\n", out var errors);

        Assert.AreEqual(1, errors.Count);
        StringAssert.Contains("avoidTraffic", errors[0].Message);
        StringAssert.Contains("0 inputs", errors[0].Message);
    }

    [Test]
    public void UserDefinedAvoidTraffic_StillParses()
    {
        string source =
            "def avoidTraffic():\n" +
            "    if carInFront():\n" +
            "        moveLeft()\n" +
            "\n" +
            "avoidTraffic()\n";

        var program = Compile(source, out var errors);

        CollectionAssert.IsEmpty(errors);
        Assert.AreEqual(2, program.Statements.Count);
    }

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
        var not  = (UnaryExpr)loop.Condition;
        Assert.AreEqual(TokenType.KeywordNot, not.Op);
        Assert.AreEqual("atDestination", ((CallExpr)not.Operand).Name);
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

    [Test]
    public void FareChangeCommands_ParseWithReporterArgument()
    {
        var program = Compile("collectFare()\ngiveChange(changeOwed())\n", out var errors);

        CollectionAssert.IsEmpty(errors);
        Assert.AreEqual("collectFare", ((CallStmt)program.Statements[0]).Name);
        var give = (CallStmt)program.Statements[1];
        Assert.AreEqual("giveChange", give.Name);
        Assert.AreEqual("changeOwed", ((CallExpr)give.Args[0]).Name);
        Assert.IsTrue(AgentApi.IsReporter("cashTendered"));
        Assert.IsTrue(AgentApi.IsReporter("changeOwed"));
        Assert.IsTrue(AgentApi.IsAction("driveToTerminal"));
        Assert.IsTrue(AgentApi.IsQuery("routeComplete"));
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

    // -------------------------------------------------------------------------
    // Phase 1 — value system

    [Test]
    public void Assignment_ParsesTargetAndValue()
    {
        var program = Compile("x = 5\n", out var errors);
        CollectionAssert.IsEmpty(errors);

        var assign = (AssignStmt)program.Statements[0];
        Assert.AreEqual("x", assign.Name);
        Assert.AreEqual(5, ((LiteralExpr)assign.Value).Value.I);
    }

    [Test]
    public void CallWithArgs_ParsesArgumentList()
    {
        var program = Compile("moveForward(3)\n", out var errors);
        CollectionAssert.IsEmpty(errors);

        var call = (CallStmt)program.Statements[0];
        Assert.AreEqual("moveForward", call.Name);
        Assert.AreEqual(1, call.Args.Count);
        Assert.AreEqual(3, ((LiteralExpr)call.Args[0]).Value.I);
    }

    [Test]
    public void PrintWithReporter_ParsesCallExprArgument()
    {
        var program = Compile("print(seatsLeft())\n", out var errors);
        CollectionAssert.IsEmpty(errors);

        var call = (CallStmt)program.Statements[0];
        Assert.AreEqual("print", call.Name);
        Assert.AreEqual("seatsLeft", ((CallExpr)call.Args[0]).Name);
    }

    [Test]
    public void ValueReturningActionAssignment_ParsesCleanly()
    {
        var program = Compile("fare = collectFare()\n", out var errors);
        CollectionAssert.IsEmpty(errors);

        var assign = (AssignStmt)program.Statements[0];
        Assert.AreEqual("fare", assign.Name);
        Assert.AreEqual("collectFare", ((CallExpr)assign.Value).Name);
    }

    [Test]
    public void MissingClosingParen_IsReported()
    {
        Compile("moveForward(\n", out var errors);

        Assert.IsTrue(errors.Count >= 1, "expected at least one error");
        Assert.IsTrue(errors.Exists(e => e.Message.Contains("closing ')'")),
            "expected a missing-closing-paren error, got: " + string.Join(" | ", errors));
    }

    [Test]
    public void WrongArgCount_IsReported()
    {
        Compile("moveForward(1, 2)\n", out var errors);

        Assert.AreEqual(1, errors.Count);
        StringAssert.Contains("moveForward", errors[0].Message);
        StringAssert.Contains("got 2", errors[0].Message);
    }

    [Test]
    public void ReporterUsedAsStatement_IsExplained()
    {
        Compile("seatsLeft()\n", out var errors);

        Assert.AreEqual(1, errors.Count);
        StringAssert.Contains("gives back a value", errors[0].Message);
    }

    // -------------------------------------------------------------------------
    // Phase 2 — expressions

    [Test]
    public void Precedence_ParsesMultiplyBeforeAdd()
    {
        var program = Compile("x = 1 + 2 * 3\n", out var errors);
        CollectionAssert.IsEmpty(errors);

        var assign = (AssignStmt)program.Statements[0];
        var bin = (BinaryExpr)assign.Value;
        Assert.AreEqual(TokenType.Plus, bin.Op);
        Assert.IsInstanceOf<LiteralExpr>(bin.Left);
        Assert.IsInstanceOf<BinaryExpr>(bin.Right);
    }

    [Test]
    public void ComparisonAndBoolean_Combines()
    {
        var program = Compile("if a > 0 and b < 5:\n    moveForward()\n", out var errors);
        CollectionAssert.IsEmpty(errors);

        var branch = (IfStmt)program.Statements[0];
        var and = (BinaryExpr)branch.Condition;
        Assert.AreEqual(TokenType.KeywordAnd, and.Op);
    }

    [Test]
    public void NotIn_ParsesAsNotMembership()
    {
        var program = Compile("x = 1 not in y\n", out var errors);
        CollectionAssert.IsEmpty(errors);

        var assign = (AssignStmt)program.Statements[0];
        var not = (UnaryExpr)assign.Value;
        Assert.AreEqual(TokenType.KeywordNot, not.Op);
        Assert.IsInstanceOf<BinaryExpr>(not.Operand);
    }

    [Test]
    public void StringTypeError_AddNumber_IsReported()
    {
        Compile("x = \"a\" + 1\n", out var errors);

        Assert.AreEqual(0, errors.Count, "type errors are runtime, not parse-time");
    }

    // -------------------------------------------------------------------------
    // Phase 3 — control flow

    [Test]
    public void ForRange_ParsesLoopVariableAndIterable()
    {
        var program = Compile("for i in range(3):\n    moveForward()\n", out var errors);
        CollectionAssert.IsEmpty(errors);

        var loop = (ForStmt)program.Statements[0];
        Assert.AreEqual("i", loop.Var);
        Assert.AreEqual("range", ((CallExpr)loop.Iterable).Name);
    }

    [Test]
    public void Elif_ParsesChain()
    {
        var program = Compile(
            "if a:\n    moveForward()\nelif b:\n    turnLeft()\nelse:\n    turnRight()\n",
            out var errors);
        CollectionAssert.IsEmpty(errors);

        var branch = (IfStmt)program.Statements[0];
        Assert.AreEqual(1, branch.Elifs.Count);
        Assert.AreEqual("b", ((VarExpr)branch.Elifs[0].Condition).Name);
        Assert.IsNotNull(branch.ElseBody);
    }

    [Test]
    public void Repeat_LowersToForRange()
    {
        var program = Compile("repeat(3):\n    moveForward()\n", out var errors);
        CollectionAssert.IsEmpty(errors);

        var loop = (ForStmt)program.Statements[0];
        Assert.AreEqual("_", loop.Var);
        Assert.AreEqual("range", ((CallExpr)loop.Iterable).Name);
    }

    [Test]
    public void BreakContinue_Parse()
    {
        var program = Compile("while True:\n    break\n    continue\n", out var errors);
        CollectionAssert.IsEmpty(errors);

        var loop = (WhileStmt)program.Statements[0];
        Assert.IsInstanceOf<BreakStmt>(loop.Body[0]);
        Assert.IsInstanceOf<ContinueStmt>(loop.Body[1]);
    }

    // -------------------------------------------------------------------------
    // Phase 4 — functions

    [Test]
    public void FunctionDef_ParsesNameParamsBody()
    {
        var program = Compile("def add(a, b):\n    return a + b\n", out var errors);
        CollectionAssert.IsEmpty(errors);

        var def = (FuncDefStmt)program.Statements[0];
        Assert.AreEqual("add", def.Name);
        CollectionAssert.AreEqual(new[] { "a", "b" }, def.Params);
        Assert.AreEqual(1, def.Body.Count);
    }

    [Test]
    public void ReturnAtTopLevel_IsReported()
    {
        Compile("return 1\n", out var errors);

        Assert.AreEqual(1, errors.Count);
        StringAssert.Contains("inside a function", errors[0].Message);
    }

    // -------------------------------------------------------------------------
    // Phase 5 — data structures

    [Test]
    public void ListLiteral_ParsesItems()
    {
        var program = Compile("x = [1, 2, 3]\n", out var errors);
        CollectionAssert.IsEmpty(errors);

        var list = (ListExpr)((AssignStmt)program.Statements[0]).Value;
        Assert.AreEqual(3, list.Items.Count);
    }

    [Test]
    public void DictLiteral_ParsesEntries()
    {
        var program = Compile("x = {\"a\": 1}\n", out var errors);
        CollectionAssert.IsEmpty(errors);

        var dict = (DictExpr)((AssignStmt)program.Statements[0]).Value;
        Assert.AreEqual(1, dict.Entries.Count);
    }

    [Test]
    public void IndexAccess_Parses()
    {
        var program = Compile("x = a[0]\n", out var errors);
        CollectionAssert.IsEmpty(errors);

        var idx = (IndexExpr)((AssignStmt)program.Statements[0]).Value;
        Assert.AreEqual("a", ((VarExpr)idx.Container).Name);
    }

    [Test]
    public void IndexAssign_Parses()
    {
        var program = Compile("a[0] = 5\n", out var errors);
        CollectionAssert.IsEmpty(errors);

        var idx = (IndexAssignStmt)program.Statements[0];
        Assert.AreEqual("a", ((VarExpr)idx.Container).Name);
    }

    [Test]
    public void MethodCall_ParsesAsFunctionCall()
    {
        var program = Compile("a.append(5)\n", out var errors);
        CollectionAssert.IsEmpty(errors);

        var call = (CallStmt)program.Statements[0];
        Assert.AreEqual("append", call.Name);
        Assert.AreEqual(2, call.Args.Count);
    }
}
