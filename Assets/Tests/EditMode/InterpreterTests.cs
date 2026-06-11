using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// EditMode tests for the stepping VM, driven by a scripted fake agent.
/// </summary>
public class InterpreterTests
{
    /// <summary>Scripted IAgentApi: queued answers per query, then a default.</summary>
    class FakeAgent : IAgentApi
    {
        public readonly Dictionary<string, Queue<bool>> Scripted = new Dictionary<string, Queue<bool>>();
        public readonly Dictionary<string, bool> Defaults = new Dictionary<string, bool>();

        public FakeAgent Script(string query, params bool[] answers)
        {
            Scripted[query] = new Queue<bool>(answers);
            return this;
        }

        public bool EvaluateQuery(string name)
        {
            if (Scripted.TryGetValue(name, out Queue<bool> queue) && queue.Count > 0)
                return queue.Dequeue();
            return Defaults.TryGetValue(name, out bool d) && d;
        }
    }

    static List<string> Run(string source, IAgentApi agent, out StepResult last)
    {
        var program = Parser.Compile(source, out var errors);
        CollectionAssert.IsEmpty(errors, "program should compile cleanly");

        var vm = new Interpreter();
        vm.Load(program);

        var actions = new List<string>();
        last = null;

        for (int i = 0; i < 5000; i++)
        {
            last = vm.Step(agent);
            if (last.Finished) return actions;
            actions.Add(last.ActionName);
        }

        Assert.Fail("interpreter never finished within the safety limit");
        return actions;
    }

    // -------------------------------------------------------------------------

    [Test]
    public void LinearProgram_EmitsActionsInOrder_OnePerStep()
    {
        var actions = Run("moveForward()\nturnLeft()\nmoveForward()\n", new FakeAgent(), out var last);

        CollectionAssert.AreEqual(new[] { "moveForward", "turnLeft", "moveForward" }, actions);
        Assert.IsTrue(last.Finished);
        Assert.IsNull(last.RuntimeError);
    }

    [Test]
    public void If_TakesTheTrueBranch()
    {
        string source =
            "if frontIsClear():\n" +
            "    moveForward()\n" +
            "else:\n" +
            "    turnLeft()\n";

        var actions = Run(source, new FakeAgent().Script("frontIsClear", true), out _);
        CollectionAssert.AreEqual(new[] { "moveForward" }, actions);
    }

    [Test]
    public void If_TakesTheElseBranch()
    {
        string source =
            "if frontIsClear():\n" +
            "    moveForward()\n" +
            "else:\n" +
            "    turnLeft()\n";

        var actions = Run(source, new FakeAgent().Script("frontIsClear", false), out _);
        CollectionAssert.AreEqual(new[] { "turnLeft" }, actions);
    }

    [Test]
    public void While_RunsUntilTheQueryFlips()
    {
        string source =
            "while frontIsClear():\n" +
            "    moveForward()\n" +
            "turnRight()\n";

        var actions = Run(source, new FakeAgent().Script("frontIsClear", true, true, false), out _);
        CollectionAssert.AreEqual(new[] { "moveForward", "moveForward", "turnRight" }, actions);
    }

    [Test]
    public void WhileNot_InvertsTheQuery()
    {
        string source =
            "while not atDestination():\n" +
            "    moveForward()\n";

        var actions = Run(source, new FakeAgent().Script("atDestination", false, false, true), out _);
        CollectionAssert.AreEqual(new[] { "moveForward", "moveForward" }, actions);
    }

    [Test]
    public void NestedIfInsideWhile_ExecutesPerIteration()
    {
        string source =
            "while not atDestination():\n" +
            "    if rightIsClear():\n" +
            "        turnRight()\n" +
            "        moveForward()\n" +
            "    else:\n" +
            "        moveForward()\n";

        var agent = new FakeAgent()
            .Script("atDestination", false, false, true)
            .Script("rightIsClear", true, false);

        var actions = Run(source, agent, out _);
        CollectionAssert.AreEqual(new[] { "turnRight", "moveForward", "moveForward" }, actions);
    }

    [Test]
    public void InfiniteLoop_TripsTheActionGuard_WithAPlainEnglishError()
    {
        string source =
            "while not atDestination():\n" +
            "    moveForward()\n";

        // atDestination is always false → the loop can never end.
        var program = Parser.Compile(source, out _);
        var vm = new Interpreter();
        vm.Load(program);

        var agent = new FakeAgent();
        int actions = 0;
        StepResult last;

        while (true)
        {
            last = vm.Step(agent);
            if (last.Finished) break;
            actions++;
            Assert.LessOrEqual(actions, Interpreter.MaxActions, "guard should have fired");
        }

        Assert.AreEqual(Interpreter.MaxActions, actions);
        Assert.IsNotNull(last.RuntimeError);
        StringAssert.Contains("infinite loop", last.RuntimeError.Message);
        Assert.AreEqual(2, last.RuntimeError.Line);
    }

    [Test]
    public void ConditionOnlySpin_TripsTheEvaluationGuard()
    {
        // A while whose body is an if that never fires: no actions, only
        // evaluations — the second guard has to catch it.
        string source =
            "while not atDestination():\n" +
            "    if frontIsClear():\n" +
            "        moveForward()\n";

        var agent = new FakeAgent();
        agent.Defaults["atDestination"] = false;
        agent.Defaults["frontIsClear"]  = false;

        var program = Parser.Compile(source, out _);
        var vm = new Interpreter();
        vm.Load(program);

        StepResult result = vm.Step(agent);

        Assert.IsTrue(result.Finished);
        Assert.IsNotNull(result.RuntimeError);
        StringAssert.Contains("stuck", result.RuntimeError.Message);
    }

    [Test]
    public void EmptyProgram_FinishesImmediately()
    {
        var vm = new Interpreter();
        vm.Load(new ProgramNode());

        StepResult result = vm.Step(new FakeAgent());
        Assert.IsTrue(result.Finished);
        Assert.IsNull(result.RuntimeError);
    }

    [Test]
    public void Reload_ResetsExecutionState()
    {
        var program = Parser.Compile("moveForward()\n", out _);
        var vm = new Interpreter();

        vm.Load(program);
        vm.Step(new FakeAgent());
        Assert.IsTrue(vm.Step(new FakeAgent()).Finished);

        vm.Load(program);
        Assert.IsFalse(vm.IsFinished);
        Assert.AreEqual("moveForward", vm.Step(new FakeAgent()).ActionName);
    }
}
