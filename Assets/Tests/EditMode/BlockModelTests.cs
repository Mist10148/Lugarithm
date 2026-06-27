using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// EditMode tests proving the block editor and the text editor produce the
/// same programs: block tree → AST → canonical text round-trips, and the
/// interpreter treats both identically.
/// </summary>
public class BlockModelTests
{
    static BlockNode Action(BlockType type) => new BlockNode(type);

    static BlockNode Container(BlockType type, string query, bool negate, params BlockNode[] body)
    {
        var node = new BlockNode(type) { Query = query, Negate = negate };
        node.Body.AddRange(body);
        return node;
    }

    // -------------------------------------------------------------------------

    [Test]
    public void LinearBlocks_CompileToCallsInOrder()
    {
        var roots = new List<BlockNode>
        {
            Action(BlockType.MoveForward),
            Action(BlockType.TurnLeft),
            Action(BlockType.PickUp),
        };

        var program = BlockProgram.ToAst(roots, out var errors, out _);

        CollectionAssert.IsEmpty(errors);
        Assert.AreEqual(3, program.Statements.Count);
        Assert.AreEqual("moveForward", ((CallStmt)program.Statements[0]).Name);
        Assert.AreEqual("pickUp",      ((CallStmt)program.Statements[2]).Name);
    }

    [Test]
    public void BlockTree_PrintsAsCanonicalText()
    {
        var ifElse = Container(BlockType.IfElse, "rightIsClear", false,
                               Action(BlockType.TurnRight), Action(BlockType.MoveForward));
        ifElse.ElseBody.Add(Action(BlockType.MoveForward));

        var loop = Container(BlockType.While, "atDestination", true, ifElse);
        var roots = new List<BlockNode> { loop, Action(BlockType.DropOff) };

        var program = BlockProgram.ToAst(roots, out var errors, out _);
        CollectionAssert.IsEmpty(errors);

        string expected =
            "while not atDestination():\n" +
            "    if rightIsClear():\n" +
            "        turnRight()\n" +
            "        moveForward()\n" +
            "    else:\n" +
            "        moveForward()\n" +
            "dropOff()\n";

        Assert.AreEqual(expected, AstPrinter.Print(program));
    }

    [Test]
    public void BlockProgram_RunsIdenticallyToParsedText()
    {
        // Same logic, built both ways.
        string source =
            "while not atDestination():\n" +
            "    if frontIsClear():\n" +
            "        moveForward()\n" +
            "    else:\n" +
            "        turnLeft()\n";

        ProgramNode fromText = Parser.Compile(source, out var textErrors);
        CollectionAssert.IsEmpty(textErrors);

        var branch = Container(BlockType.IfElse, "frontIsClear", false, Action(BlockType.MoveForward));
        branch.ElseBody.Add(Action(BlockType.TurnLeft));
        var roots = new List<BlockNode> { Container(BlockType.While, "atDestination", true, branch) };

        ProgramNode fromBlocks = BlockProgram.ToAst(roots, out var blockErrors, out _);
        CollectionAssert.IsEmpty(blockErrors);

        // Identical canonical text ⇒ identical programs.
        Assert.AreEqual(AstPrinter.Print(fromText), AstPrinter.Print(fromBlocks));
    }

    [Test]
    public void EmptyContainer_IsReportedWithTheOffendingBlock()
    {
        var emptyLoop = Container(BlockType.While, "frontIsClear", false);
        var roots = new List<BlockNode> { emptyLoop };

        BlockProgram.ToAst(roots, out var errors, out var offenders);

        Assert.AreEqual(1, errors.Count);
        StringAssert.Contains("at least one block", errors[0].Message);
        Assert.AreSame(emptyLoop, offenders[0]);
    }

    [Test]
    public void EmptyElseBranch_IsReportedSeparately()
    {
        var branch = Container(BlockType.IfElse, "atStop", false, Action(BlockType.PickUp));
        var roots = new List<BlockNode> { branch };

        BlockProgram.ToAst(roots, out var errors, out var offenders);

        Assert.AreEqual(1, errors.Count);
        StringAssert.Contains("else", errors[0].Message);
        Assert.AreSame(branch, offenders[0]);
    }

    [Test]
    public void SourceRefs_PointBackAtTheirBlocks()
    {
        var move = Action(BlockType.MoveForward);
        var loop = Container(BlockType.While, "frontIsClear", false, move);
        var roots = new List<BlockNode> { loop };

        var program = BlockProgram.ToAst(roots, out _, out _);

        var loopStmt = (WhileStmt)program.Statements[0];
        Assert.AreSame(loop, loopStmt.SourceRef);
        Assert.AreSame(move, loopStmt.Body[0].SourceRef);
    }

    [Test]
    public void PaletteNames_MapToAllBlockTypes()
    {
        Assert.AreEqual(BlockType.MoveForward, BlockProgram.FromPaletteName("moveForward"));
        Assert.AreEqual(BlockType.While,       BlockProgram.FromPaletteName("while"));
        Assert.AreEqual(BlockType.IfElse,      BlockProgram.FromPaletteName("ifElse"));
        Assert.AreEqual(BlockType.GiveChange,  BlockProgram.FromPaletteName("giveChange"));
        Assert.IsNull(BlockProgram.FromPaletteName("garbage"));
    }

    [Test]
    public void Labels_ReadLikeTheLanguage()
    {
        Assert.AreEqual("moveForward()", BlockProgram.Label(Action(BlockType.MoveForward)));
        Assert.AreEqual("while not atDestination():",
            BlockProgram.Label(Container(BlockType.While, "atDestination", true)));
        Assert.AreEqual("if rightIsClear():",
            BlockProgram.Label(Container(BlockType.If, "rightIsClear", false)));
        Assert.AreEqual("giveChange(changeOwed())", BlockProgram.Label(Action(BlockType.GiveChange)));
    }

    [Test]
    public void ReferenceSolution_RoundTripsThroughBlocks_WithGiveChange()
    {
        ProgramNode program = Parser.Compile(SelfDrivePlanner.ReferenceSolution, out var errors);
        CollectionAssert.IsEmpty(errors);

        List<BlockNode> roots = BlockProgram.FromAst(program, out bool fullyRepresentable);
        Assert.IsTrue(fullyRepresentable);

        ProgramNode fromBlocks = BlockProgram.ToAst(roots, out var blockErrors, out _);
        CollectionAssert.IsEmpty(blockErrors);
        StringAssert.Contains("giveChange(changeOwed())", AstPrinter.Print(fromBlocks));
        StringAssert.Contains("def drive():", AstPrinter.Print(fromBlocks));
        StringAssert.Contains("handlePassengers()", AstPrinter.Print(fromBlocks));
    }

    [Test]
    public void FunctionBlocks_CompileToDefAndCall()
    {
        var def = new BlockNode(BlockType.FunctionDef) { FunctionName = "drive" };
        def.Body.Add(Action(BlockType.DriveToNextStop));
        var roots = new List<BlockNode>
        {
            def,
            new BlockNode(BlockType.FunctionCall) { FunctionName = "drive" },
        };

        ProgramNode program = BlockProgram.ToAst(roots, out var errors, out _);

        CollectionAssert.IsEmpty(errors);
        string text = AstPrinter.Print(program);
        StringAssert.Contains("def drive():", text);
        StringAssert.Contains("    driveToNextStop()", text);
        StringAssert.EndsWith("drive()\n", text);
    }
}
