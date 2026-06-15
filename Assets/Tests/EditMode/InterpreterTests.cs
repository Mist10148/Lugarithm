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
        public readonly Dictionary<string, Queue<Value>> ReporterValues = new Dictionary<string, Queue<Value>>();
        public readonly Dictionary<string, Value> ReporterDefaults = new Dictionary<string, Value>();

        public FakeAgent Script(string query, params bool[] answers)
        {
            Scripted[query] = new Queue<bool>(answers);
            return this;
        }

        public FakeAgent ScriptReporter(string name, params Value[] values)
        {
            ReporterValues[name] = new Queue<Value>(values);
            return this;
        }

        public bool EvaluateQuery(string name)
        {
            if (Scripted.TryGetValue(name, out Queue<bool> queue) && queue.Count > 0)
                return queue.Dequeue();
            return Defaults.TryGetValue(name, out bool d) && d;
        }

        public Value ReadReporter(string name, IReadOnlyList<Value> args)
        {
            if (ReporterValues.TryGetValue(name, out Queue<Value> queue) && queue.Count > 0)
                return queue.Dequeue();
            return ReporterDefaults.TryGetValue(name, out Value v) ? v : Value.None;
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

    // -------------------------------------------------------------------------
    // Phase 1 — value system

    [Test]
    public void Assignment_ReadsBackVariable()
    {
        string source =
            "x = 5\n" +
            "moveForward(x)\n";

        var actions = Run(source, new FakeAgent(), out _);
        CollectionAssert.AreEqual(new[] { "moveForward", "moveForward", "moveForward", "moveForward", "moveForward" }, actions);
    }

    [Test]
    public void MoveForwardWithLiteral_RepeatsNTimes()
    {
        var actions = Run("moveForward(3)\n", new FakeAgent(), out _);
        CollectionAssert.AreEqual(new[] { "moveForward", "moveForward", "moveForward" }, actions);
    }

    [Test]
    public void Wait_RepeatsNTimes()
    {
        var actions = Run("wait(2)\n", new FakeAgent(), out _);
        CollectionAssert.AreEqual(new[] { "wait", "wait" }, actions);
    }

    [Test]
    public void Print_CollectsOutput()
    {
        var program = Parser.Compile("print(13)\nprint(\"Para\")\n", out var errors);
        CollectionAssert.IsEmpty(errors);

        var vm = new Interpreter();
        vm.Load(program);

        while (!vm.Step(new FakeAgent()).Finished) { }

        CollectionAssert.AreEqual(new[] { "13", "Para" }, vm.Output);
    }

    [Test]
    public void Reporter_ReadsValueIntoVariable()
    {
        var program = Parser.Compile("seats = seatsLeft()\nprint(seats)\n", out var errors);
        CollectionAssert.IsEmpty(errors);

        var vm = new Interpreter();
        vm.Load(program);

        var agent = new FakeAgent().ScriptReporter("seatsLeft", Value.Int(4));

        while (!vm.Step(agent).Finished) { }

        CollectionAssert.AreEqual(new[] { "4" }, vm.Output);
    }

    [Test]
    public void ValueReturningAction_BindsViaDeliverProtocol()
    {
        var program = Parser.Compile("fare = collectFare()\nprint(fare)\n", out var errors);
        CollectionAssert.IsEmpty(errors);

        var vm = new Interpreter();
        vm.Load(program);

        StepResult step1 = vm.Step(new FakeAgent());
        Assert.AreEqual("collectFare", step1.ActionName);
        Assert.AreEqual("fare", step1.BindResultTo);

        vm.DeliverActionResult(Value.Int(13));

        StepResult step2 = vm.Step(new FakeAgent());
        Assert.IsTrue(step2.Finished);
        Assert.IsNull(step2.RuntimeError);

        CollectionAssert.AreEqual(new[] { "13" }, vm.Output);
    }

    [Test]
    public void UndefinedVariable_RuntimeErrorWithCoachingMessage()
    {
        var program = Parser.Compile("print(unknown)\n", out var errors);
        CollectionAssert.IsEmpty(errors);

        var vm = new Interpreter();
        vm.Load(program);

        StepResult result = vm.Step(new FakeAgent());
        Assert.IsTrue(result.Finished);
        Assert.IsNotNull(result.RuntimeError);
        StringAssert.Contains("unknown", result.RuntimeError.Message);
        StringAssert.Contains("before giving it a value", result.RuntimeError.Message);
    }

    [Test]
    public void ActionAsValue_RuntimeErrorWithCoachingMessage()
    {
        var program = Parser.Compile("x = turnLeft()\n", out var errors);
        CollectionAssert.IsEmpty(errors);

        var vm = new Interpreter();
        vm.Load(program);

        StepResult result = vm.Step(new FakeAgent());
        Assert.IsTrue(result.Finished);
        Assert.IsNotNull(result.RuntimeError);
        StringAssert.Contains("turnLeft", result.RuntimeError.Message);
    }

    // -------------------------------------------------------------------------
    // Phase 2 — expressions

    [Test]
    public void Arithmetic_ComputesCorrectly()
    {
        var program = Parser.Compile("x = 2 + 3 * 4\nprint(x)\n", out var errors);
        CollectionAssert.IsEmpty(errors);

        var vm = new Interpreter();
        vm.Load(program);
        while (!vm.Step(new FakeAgent()).Finished) { }

        CollectionAssert.AreEqual(new[] { "14" }, vm.Output);
    }

    [Test]
    public void BooleanAnd_ShortCircuits()
    {
        var program = Parser.Compile("print(0 and 5)\nprint(1 and 5)\n", out var errors);
        CollectionAssert.IsEmpty(errors);

        var vm = new Interpreter();
        vm.Load(program);
        while (!vm.Step(new FakeAgent()).Finished) { }

        CollectionAssert.AreEqual(new[] { "0", "5" }, vm.Output);
    }

    [Test]
    public void Comparison_UsesExpressions()
    {
        string source =
            "x = 3\n" +
            "if x > 2 and x < 5:\n" +
            "    moveForward()\n";

        var actions = Run(source, new FakeAgent(), out _);
        CollectionAssert.AreEqual(new[] { "moveForward" }, actions);
    }

    [Test]
    public void TypeMismatch_AddingStringAndNumber_Errors()
    {
        var program = Parser.Compile("x = \"Para\" + 1\n", out var errors);
        CollectionAssert.IsEmpty(errors);

        var vm = new Interpreter();
        vm.Load(program);

        StepResult result = vm.Step(new FakeAgent());
        Assert.IsTrue(result.Finished);
        Assert.IsNotNull(result.RuntimeError);
        StringAssert.Contains("add", result.RuntimeError.Message);
        StringAssert.Contains("text", result.RuntimeError.Message);
    }

    [Test]
    public void StringConcatenation_Works()
    {
        var program = Parser.Compile("x = \"Para\" + \"!\"\nprint(x)\n", out var errors);
        CollectionAssert.IsEmpty(errors);

        var vm = new Interpreter();
        vm.Load(program);
        while (!vm.Step(new FakeAgent()).Finished) { }

        CollectionAssert.AreEqual(new[] { "Para!" }, vm.Output);
    }

    [Test]
    public void QueryAsValue_AssignedAsBool()
    {
        string source =
            "clear = frontIsClear()\n" +
            "if clear:\n" +
            "    moveForward()\n";

        var actions = Run(source, new FakeAgent().Script("frontIsClear", true), out _);
        CollectionAssert.AreEqual(new[] { "moveForward" }, actions);
    }

    [Test]
    public void NotIn_Membership()
    {
        var program = Parser.Compile("print(\"z\" not in \"Para\")\n", out var errors);
        CollectionAssert.IsEmpty(errors);

        var vm = new Interpreter();
        vm.Load(program);
        while (!vm.Step(new FakeAgent()).Finished) { }

        CollectionAssert.AreEqual(new[] { "True" }, vm.Output);
    }

    // -------------------------------------------------------------------------
    // Phase 3 — control flow

    [Test]
    public void ForRange_LoopsCorrectNumberOfTimes()
    {
        var program = Parser.Compile("for i in range(3):\n    moveForward()\n", out var errors);
        CollectionAssert.IsEmpty(errors);

        var vm = new Interpreter();
        vm.Load(program);

        var actions = new List<string>();
        StepResult last;
        for (int n = 0; n < 100; n++)
        {
            last = vm.Step(new FakeAgent());
            if (last.Finished) break;
            actions.Add(last.ActionName);
        }

        CollectionAssert.AreEqual(new[] { "moveForward", "moveForward", "moveForward" }, actions);
    }

    [Test]
    public void ForEach_OverString_IteratesCharacters()
    {
        var program = Parser.Compile("for c in \"ab\":\n    print(c)\n", out var errors);
        CollectionAssert.IsEmpty(errors);

        var vm = new Interpreter();
        vm.Load(program);
        while (!vm.Step(new FakeAgent()).Finished) { }

        CollectionAssert.AreEqual(new[] { "a", "b" }, vm.Output);
    }

    [Test]
    public void Elif_TakesTheMatchingBranch()
    {
        string source =
            "x = 2\n" +
            "if x == 1:\n" +
            "    moveForward()\n" +
            "elif x == 2:\n" +
            "    turnLeft()\n" +
            "else:\n" +
            "    turnRight()\n";

        var actions = Run(source, new FakeAgent(), out _);
        CollectionAssert.AreEqual(new[] { "turnLeft" }, actions);
    }

    [Test]
    public void Break_ExitsLoop()
    {
        string source =
            "for i in range(10):\n" +
            "    moveForward()\n" +
            "    break\n";

        var actions = Run(source, new FakeAgent(), out _);
        CollectionAssert.AreEqual(new[] { "moveForward" }, actions);
    }

    [Test]
    public void Continue_SkipsRestOfIteration()
    {
        string source =
            "for i in range(3):\n" +
            "    continue\n" +
            "    moveForward()\n";

        var actions = Run(source, new FakeAgent(), out _);
        CollectionAssert.IsEmpty(actions);
    }

    [Test]
    public void RepeatSugar_LowersToRangeLoop()
    {
        var program = Parser.Compile("repeat(2):\n    moveForward()\n", out var errors);
        CollectionAssert.IsEmpty(errors);

        var vm = new Interpreter();
        vm.Load(program);

        var actions = new List<string>();
        StepResult last;
        for (int n = 0; n < 100; n++)
        {
            last = vm.Step(new FakeAgent());
            if (last.Finished) break;
            actions.Add(last.ActionName);
        }

        CollectionAssert.AreEqual(new[] { "moveForward", "moveForward" }, actions);
    }

    // -------------------------------------------------------------------------
    // Phase 4 — functions

    [Test]
    public void Function_ReturnsValue_UsedInExpression()
    {
        string source =
            "def double(x):\n" +
            "    return x * 2\n" +
            "print(double(3))\n";

        var vm = new Interpreter();
        vm.Load(Parser.Compile(source, out var errors));
        CollectionAssert.IsEmpty(errors);

        while (!vm.Step(new FakeAgent()).Finished) { }
        CollectionAssert.AreEqual(new[] { "6" }, vm.Output);
    }

    [Test]
    public void Function_ReadsGlobal_AssignmentMakesLocal()
    {
        string source =
            "total = 10\n" +
            "def add(x):\n" +
            "    total = total + x\n" +
            "    return total\n" +
            "print(add(5))\n" +
            "print(total)\n";

        var vm = new Interpreter();
        vm.Load(Parser.Compile(source, out var errors));
        CollectionAssert.IsEmpty(errors);

        while (!vm.Step(new FakeAgent()).Finished) { }
        CollectionAssert.AreEqual(new[] { "15", "10" }, vm.Output);
    }

    [Test]
    public void Function_CallAsStatement()
    {
        string source =
            "def step():\n" +
            "    moveForward()\n" +
            "step()\n";

        var actions = Run(source, new FakeAgent(), out _);
        CollectionAssert.AreEqual(new[] { "moveForward" }, actions);
    }

    [Test]
    public void Function_WrongArgCount_RuntimeError()
    {
        string source =
            "def one(x):\n" +
            "    return x\n" +
            "print(one())\n";

        var vm = new Interpreter();
        vm.Load(Parser.Compile(source, out var errors));
        CollectionAssert.IsEmpty(errors);

        StepResult result = vm.Step(new FakeAgent());
        Assert.IsTrue(result.Finished);
        Assert.IsNotNull(result.RuntimeError);
        StringAssert.Contains("needs 1 input", result.RuntimeError.Message);
    }

    // -------------------------------------------------------------------------
    // Phase 5 — data structures

    [Test]
    public void List_LiteralAndAppend()
    {
        string source =
            "xs = [1, 2]\n" +
            "append(xs, 3)\n" +
            "print(len(xs))\n";

        var vm = new Interpreter();
        vm.Load(Parser.Compile(source, out var errors));
        CollectionAssert.IsEmpty(errors);
        while (!vm.Step(new FakeAgent()).Finished) { }

        CollectionAssert.AreEqual(new[] { "3" }, vm.Output);
    }

    [Test]
    public void List_IndexAndAssign()
    {
        string source =
            "xs = [10, 20, 30]\n" +
            "xs[1] = 99\n" +
            "print(xs[1])\n";

        var vm = new Interpreter();
        vm.Load(Parser.Compile(source, out var errors));
        CollectionAssert.IsEmpty(errors);
        while (!vm.Step(new FakeAgent()).Finished) { }

        CollectionAssert.AreEqual(new[] { "99" }, vm.Output);
    }

    [Test]
    public void Dict_LookupAndAssign()
    {
        string source =
            "d = {\"a\": 1, \"b\": 2}\n" +
            "print(d[\"a\"])\n" +
            "d[\"c\"] = 3\n" +
            "print(len(d))\n";

        var vm = new Interpreter();
        vm.Load(Parser.Compile(source, out var errors));
        CollectionAssert.IsEmpty(errors);
        while (!vm.Step(new FakeAgent()).Finished) { }

        CollectionAssert.AreEqual(new[] { "1", "3" }, vm.Output);
    }

    [Test]
    public void Tuple_CreateAndIndex()
    {
        string source =
            "t = (1, 2)\n" +
            "print(t[1])\n";

        var vm = new Interpreter();
        vm.Load(Parser.Compile(source, out var errors));
        CollectionAssert.IsEmpty(errors);
        while (!vm.Step(new FakeAgent()).Finished) { }

        CollectionAssert.AreEqual(new[] { "2" }, vm.Output);
    }

    [Test]
    public void Builtins_SumSortedLen()
    {
        string source =
            "xs = [3, 1, 2]\n" +
            "print(sum(xs))\n" +
            "print(len(sorted(xs)))\n";

        var vm = new Interpreter();
        vm.Load(Parser.Compile(source, out var errors));
        CollectionAssert.IsEmpty(errors);
        while (!vm.Step(new FakeAgent()).Finished) { }

        CollectionAssert.AreEqual(new[] { "6", "3" }, vm.Output);
    }

    [Test]
    public void MethodCall_Append()
    {
        string source =
            "xs = [1]\n" +
            "xs.append(2)\n" +
            "print(len(xs))\n";

        var vm = new Interpreter();
        vm.Load(Parser.Compile(source, out var errors));
        CollectionAssert.IsEmpty(errors);
        while (!vm.Step(new FakeAgent()).Finished) { }

        CollectionAssert.AreEqual(new[] { "2" }, vm.Output);
    }
}
