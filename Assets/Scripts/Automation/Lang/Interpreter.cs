using System;
using System.Collections.Generic;

/// <summary>
/// What one interpreter step produced: either a single agent action to
/// perform, a runtime error, or the end of the program.
/// </summary>
public class StepResult
{
    /// <summary>Action to execute (an <see cref="AgentApi"/> action name), or null.</summary>
    public string ActionName;

    /// <summary>Already-evaluated action inputs, passed to the world simulation.</summary>
    public List<Value> ActionArgs = new List<Value>();

    /// <summary>The statement that produced the action (for highlighting).</summary>
    public StmtNode Node;

    /// <summary>
    /// When set, the controller must apply the action and call
    /// <see cref="Interpreter.DeliverActionResult(Value)"/> before the next step.
    /// </summary>
    public string BindResultTo;

    public LangError RuntimeError;

    public bool Finished;
}

/// <summary>A chain of variable scopes. Functions read globals; assignment makes a local.</summary>
public class Environment
{
    public readonly Dictionary<string, Value> Values = new Dictionary<string, Value>();
    public readonly Environment Parent;

    public Environment(Environment parent = null) { Parent = parent; }

    public bool TryGet(string name, out Value value)
    {
        if (Values.TryGetValue(name, out value)) return true;
        return Parent != null && Parent.TryGet(name, out value);
    }

    /// <summary>Creates or updates a local binding in this scope.</summary>
    public void Assign(string name, Value value) => Values[name] = value;

    /// <summary>Defines a name in this scope without searching parents.</summary>
    public void Define(string name, Value value) => Values[name] = value;
}

/// <summary>
/// Stepping VM for the automation language. <see cref="Step"/> evaluates
/// conditions internally and returns exactly one agent action per call, which
/// makes Run / Pause / Reset and playback speed trivial for the caller (it
/// owns the clock). Guards stop runaway programs with a plain-English error.
/// </summary>
public class Interpreter
{
    public const int MaxActions     = 1000;
    public const int MaxEvaluations = 20000;

    class Frame
    {
        public List<StmtNode> Body;
        public int            Index;
        public Environment    Env;

        /// <summary>When set, this frame is a while body — recheck on completion.</summary>
        public WhileStmt WhileLoop;

        /// <summary>When set, this frame is a for body — advance iterator on completion.</summary>
        public ForStmt ForLoop;
        public List<Value> ForValues;
        public int         ForIndex;

        /// <summary>When set, this frame is a function body.</summary>
        public FunctionValue Function;
    }

    readonly Stack<Frame> _frames = new Stack<Frame>();
    ProgramNode _program;

    public int  ActionsExecuted { get; private set; }
    public int  Evaluations     { get; private set; }
    public bool IsFinished      { get; private set; }

    readonly Dictionary<int, int> _lineHits = new Dictionary<int, int>();
    public IReadOnlyDictionary<int, int> LineHits => _lineHits;

    Environment _globals;
    public List<string> Output { get; } = new List<string>();

    string _pendingBindTarget;
    bool   _hasPendingResult;
    Value  _pendingResult;

    string _repeatAction;
    int    _repeatRemaining;
    StmtNode _repeatNode;

    Value _returnValue;
    bool  _hasReturnValue;

    // -------------------------------------------------------------------------

    /// <summary>Loads (or reloads) a program and resets all execution state.</summary>
    public void Load(ProgramNode program)
    {
        _program = program;
        _frames.Clear();
        _globals = new Environment();
        Output.Clear();
        ActionsExecuted = 0;
        Evaluations     = 0;
        IsFinished      = program == null || program.Statements.Count == 0;
        _lineHits.Clear();

        _pendingBindTarget = null;
        _hasPendingResult  = false;
        _repeatAction      = null;
        _repeatRemaining   = 0;
        _repeatNode        = null;
        _returnValue       = Value.None;
        _hasReturnValue    = false;

        if (!IsFinished)
            _frames.Push(new Frame { Body = program.Statements, Env = _globals });
    }

    /// <summary>
    /// Advances execution until one agent action is produced, the program
    /// ends, or a guard trips. Conditions are evaluated against
    /// <paramref name="agent"/> along the way.
    /// </summary>
    public StepResult Step(IAgentApi agent)
    {
        if (IsFinished)
            return new StepResult { Finished = true };

        // Bind a value-returning action result delivered by the controller.
        if (_hasPendingResult)
        {
            CurrentEnv().Assign(_pendingBindTarget, _pendingResult);
            _hasPendingResult = false;
            _pendingBindTarget = null;
        }

        // Emit the next tick of a repeated action (moveForward(3), wait(2), ...).
        if (_repeatRemaining > 0)
        {
            _repeatRemaining--;
            ActionsExecuted++;
            if (ActionsExecuted > MaxActions)
            {
                return Trip(_repeatNode,
                    $"stopped after {MaxActions} actions — this looks like an infinite loop.");
            }
            return new StepResult { ActionName = _repeatAction, Node = _repeatNode };
        }
        _repeatAction = null;
        _repeatNode = null;

        while (true)
        {
            if (_frames.Count == 0)
            {
                IsFinished = true;
                return new StepResult { Finished = true };
            }

            Frame frame = _frames.Peek();

            // Frame exhausted: loop frames re-check/advance, plain frames pop.
            if (frame.Index >= frame.Body.Count)
            {
                bool stay = false;
                try
                {
                    stay = AdvanceLoop(frame, agent);
                }
                catch (InterpreterRuntimeException ex)
                {
                    return Trip(frame.WhileLoop ?? (StmtNode)frame.ForLoop, ex.Message, ex.Line);
                }

                if (!stay)
                    _frames.Pop();

                if (Evaluations > MaxEvaluations)
                    return Trip(frame.WhileLoop ?? (StmtNode)frame.ForLoop,
                        "this program seems stuck checking conditions forever.");

                continue;
            }

            StmtNode stmt = frame.Body[frame.Index];
            frame.Index++;

            if (stmt.Line > 0)
            {
                _lineHits[stmt.Line] = _lineHits.TryGetValue(stmt.Line, out int hits) ? hits + 1 : 1;
            }

            StepResult result = null;
            try
            {
                result = ExecuteStatement(stmt, agent);
            }
            catch (BreakException)
            {
                if (!HandleLoopControl(loop => true))
                    return Trip(stmt, "'break' only makes sense inside a loop.");
                continue;
            }
            catch (ContinueException)
            {
                if (!HandleLoopControl(loop => false))
                    return Trip(stmt, "'continue' only makes sense inside a loop.");
                continue;
            }
            catch (ReturnException ret)
            {
                HandleReturn(ret.Value);
                continue;
            }
            catch (InterpreterRuntimeException ex)
            {
                return Trip(stmt, ex.Message, ex.Line);
            }

            if (result != null)
                return result;

            if (Evaluations > MaxEvaluations)
                return Trip(stmt, "this program seems stuck checking conditions forever.");
        }
    }

    void HandleReturn(Value value)
    {
        _returnValue = value;
        _hasReturnValue = true;

        // Pop frames until we leave the current function (or run out of frames).
        while (_frames.Count > 0)
        {
            Frame f = _frames.Pop();
            if (f.Function != null) break;
        }
    }

    /// <summary>
    /// Delivers the result of a value-returning action (like <c>collectFare()</c>)
    /// so the next <see cref="Step"/> can bind it to the assignment target.
    /// </summary>
    public void DeliverActionResult(Value value)
    {
        _pendingResult = value;
        _hasPendingResult = true;
    }

    // -------------------------------------------------------------------------

    StepResult ExecuteStatement(StmtNode stmt, IAgentApi agent)
    {
        switch (stmt)
        {
            case CallStmt call:
                return ExecuteCallStmt(call, agent);

            case AssignStmt assign:
                return ExecuteAssignStmt(assign, agent);

            case IndexAssignStmt idxAssign:
                ExecuteIndexAssign(idxAssign, agent);
                return null;

            case WhileStmt loop:
                if (Evaluate(loop.Condition, agent, CurrentEnv()).IsTruthy())
                    _frames.Push(new Frame { Body = loop.Body, Env = CurrentEnv(), WhileLoop = loop });
                return null;

            case ForStmt loop:
                EnterForLoop(loop, agent);
                return null;

            case IfStmt branch:
                return ExecuteIfStmt(branch, agent);

            case BreakStmt:
                throw new BreakException();

            case ContinueStmt:
                throw new ContinueException();

            case FuncDefStmt def:
                CurrentEnv().Define(def.Name, Value.Func(new FunctionValue
                {
                    Name   = def.Name,
                    Params = def.Params,
                    Body   = def.Body,
                }));
                return null;

            case ReturnStmt ret:
                Value v = ret.Value != null ? Evaluate(ret.Value, agent, CurrentEnv()) : Value.None;
                throw new ReturnException(v);

            default:
                return null;
        }
    }

    StepResult ExecuteIfStmt(IfStmt branch, IAgentApi agent)
    {
        if (Evaluate(branch.Condition, agent, CurrentEnv()).IsTruthy())
        {
            _frames.Push(new Frame { Body = branch.Body, Env = CurrentEnv() });
            return null;
        }

        foreach (ElifClause elif in branch.Elifs)
        {
            if (Evaluate(elif.Condition, agent, CurrentEnv()).IsTruthy())
            {
                _frames.Push(new Frame { Body = elif.Body, Env = CurrentEnv() });
                return null;
            }
        }

        if (branch.ElseBody != null)
            _frames.Push(new Frame { Body = branch.ElseBody, Env = CurrentEnv() });

        return null;
    }

    void EnterForLoop(ForStmt loop, IAgentApi agent)
    {
        List<Value> values;
        Environment env = CurrentEnv();

        if (loop.Iterable is CallExpr rangeCall && rangeCall.Name == "range")
        {
            List<Value> args = EvaluateArgList(rangeCall.Args, agent, env);
            long start = 0, stop = 0, step = 1;
            if (args.Count == 1)
            {
                stop = RequireInt(args[0], rangeCall.Line);
            }
            else if (args.Count == 2)
            {
                start = RequireInt(args[0], rangeCall.Line);
                stop  = RequireInt(args[1], rangeCall.Line);
            }
            else if (args.Count == 3)
            {
                start = RequireInt(args[0], rangeCall.Line);
                stop  = RequireInt(args[1], rangeCall.Line);
                step  = RequireInt(args[2], rangeCall.Line);
            }
            else
            {
                throw RuntimeLangError(rangeCall.Line,
                    "range() needs 1 to 3 numbers — range(stop), range(start, stop), or range(start, stop, step).");
            }

            values = new List<Value>();
            if (step > 0)
                for (long i = start; i < stop; i += step) values.Add(Value.Int(i));
            else if (step < 0)
                for (long i = start; i > stop; i += step) values.Add(Value.Int(i));
            else
                throw RuntimeLangError(rangeCall.Line, "range() step can't be zero.");
        }
        else
        {
            Value coll = Evaluate(loop.Iterable, agent, env);
            values = ValuesAsIterable(coll, loop.Line);
        }

        _frames.Push(new Frame
        {
            Body      = loop.Body,
            Env       = env,
            ForLoop   = loop,
            ForValues = values,
            ForIndex  = 0,
        });

        AdvanceForFrame(_frames.Peek());
    }

    bool AdvanceLoop(Frame frame, IAgentApi agent)
    {
        if (frame.WhileLoop != null)
        {
            if (frame.WhileLoop.Line > 0)
                _lineHits[frame.WhileLoop.Line] = _lineHits.TryGetValue(frame.WhileLoop.Line, out int n) ? n + 1 : 1;

            bool stay = Evaluate(frame.WhileLoop.Condition, agent, frame.Env).IsTruthy();
            if (stay) frame.Index = 0;
            return stay;
        }

        if (frame.ForLoop != null)
        {
            frame.ForIndex++;
            return AdvanceForFrame(frame);
        }

        return false;
    }

    bool AdvanceForFrame(Frame frame)
    {
        if (frame.ForIndex >= frame.ForValues.Count)
            return false;

        frame.Env.Assign(frame.ForLoop.Var, frame.ForValues[frame.ForIndex]);
        frame.Index = 0;
        return true;
    }

    bool HandleLoopControl(System.Func<bool, bool> breakOrContinue)
    {
        var popped = new List<Frame>();
        bool found = false;

        while (_frames.Count > 0)
        {
            Frame f = _frames.Pop();
            popped.Add(f);
            if (f.WhileLoop != null || f.ForLoop != null)
            {
                found = true;
                bool isBreak = breakOrContinue(true);
                if (!isBreak)
                {
                    _frames.Push(f);
                    f.Index = f.Body.Count;
                }
                break;
            }
        }

        if (!found)
        {
            for (int i = popped.Count - 1; i >= 0; i--)
                _frames.Push(popped[i]);
        }

        return found;
    }

    // -------------------------------------------------------------------------

    StepResult ExecuteCallStmt(CallStmt call, IAgentApi agent)
    {
        // User-defined function called as a statement.
        if (TryResolveFunction(call.Name, out FunctionValue func))
        {
            EnterFunction(func, EvaluateArgList(call.Args, agent, CurrentEnv()), call.Line);
            return null;
        }

        if (call.Name == "print")
        {
            var parts = new List<string>(call.Args.Count);
            foreach (ExprNode arg in call.Args)
                parts.Add(Evaluate(arg, agent, CurrentEnv()).Display());
            Output.Add(string.Join(" ", parts));
            return null;
        }

        List<Value> argValues = EvaluateArgList(call.Args, agent, CurrentEnv());

        if (IsBuiltin(call.Name))
        {
            ExecuteBuiltin(call.Name, argValues, call.Line);
            return null;
        }

        if (call.Name == "moveForward" || call.Name == "wait")
        {
            long repeat = 1;
            if (argValues.Count > 0)
            {
                Value v = argValues[0];
                if (v.Kind != ValueKind.Int && v.Kind != ValueKind.Float)
                    throw RuntimeLangError(call.Line,
                        $"{call.Name}() needs a number, not {v.TypeName}.");
                repeat = v.AsInt();
                if (repeat < 0)
                    throw RuntimeLangError(call.Line,
                        $"{call.Name}() can't repeat a negative number of times.");
            }

            string baseAction = call.Name == "moveForward" ? "moveForward" : "wait";
            ActionsExecuted++;
            if (ActionsExecuted > MaxActions)
                return Trip(call, $"stopped after {MaxActions} actions — this looks like an infinite loop.");

            if (repeat <= 1)
                return new StepResult { ActionName = baseAction, Node = call, ActionArgs = argValues };

            _repeatAction = baseAction;
            _repeatRemaining = (int)(repeat - 1);
            _repeatNode = call;
            return new StepResult { ActionName = baseAction, Node = call, ActionArgs = argValues };
        }

        ActionsExecuted++;
        if (ActionsExecuted > MaxActions)
            return Trip(call, $"stopped after {MaxActions} actions — this looks like an infinite loop.");

        return new StepResult { ActionName = call.Name, Node = call, ActionArgs = argValues };
    }

    StepResult ExecuteAssignStmt(AssignStmt assign, IAgentApi agent)
    {
        // Value-returning actions need a tick, so we yield them and bind later.
        if (assign.Value is CallExpr rhsCall && AgentApi.IsValueReturningAction(rhsCall.Name))
        {
            LangError arityError = AgentApi.CheckArity(rhsCall.Name, rhsCall.Args.Count, assign.Line);
            if (arityError != null)
                throw RuntimeLangError(assign.Line, arityError.Message);

            ActionsExecuted++;
            if (ActionsExecuted > MaxActions)
                return Trip(assign, $"stopped after {MaxActions} actions — this looks like an infinite loop.");

            _pendingBindTarget = assign.Name;
            return new StepResult
            {
                ActionName   = rhsCall.Name,
                ActionArgs   = EvaluateArgList(rhsCall.Args, agent, CurrentEnv()),
                Node         = assign,
                BindResultTo = assign.Name,
            };
        }

        CurrentEnv().Assign(assign.Name, Evaluate(assign.Value, agent, CurrentEnv()));
        return null;
    }

    // -------------------------------------------------------------------------

    Value Evaluate(ExprNode expr, IAgentApi agent, Environment env)
    {
        Evaluations++;

        switch (expr)
        {
            case LiteralExpr lit:
                return lit.Value;

            case VarExpr var:
                if (env.TryGet(var.Name, out Value value))
                    return value;
                throw RuntimeLangError(var.Line,
                    $"you're using '{var.Name}' before giving it a value — add '{var.Name} = 0' first.");

            case CallExpr call:
                return EvaluateCallExpr(call, agent, env);

            case BinaryExpr bin:
                return EvaluateBinary(bin, agent, env);

            case UnaryExpr un:
                return EvaluateUnary(un, agent, env);

            case ListExpr list:
                var listValues = new List<Value>(list.Items.Count);
                foreach (ExprNode item in list.Items) listValues.Add(Evaluate(item, agent, env));
                return Value.List(listValues);

            case DictExpr dict:
                var dictValues = new Dictionary<Value, Value>();
                foreach (var kv in dict.Entries)
                    dictValues[Evaluate(kv.Key, agent, env)] = Evaluate(kv.Value, agent, env);
                return Value.Dict(dictValues);

            case TupleExpr tuple:
                var tupleValues = new Value[tuple.Items.Count];
                for (int i = 0; i < tuple.Items.Count; i++)
                    tupleValues[i] = Evaluate(tuple.Items[i], agent, env);
                return Value.Tuple(tupleValues);

            case IndexExpr idx:
                return EvaluateIndex(idx, agent, env);

            default:
                return Value.None;
        }
    }

    Value EvaluateCallExpr(CallExpr call, IAgentApi agent, Environment env)
    {
        // User-defined function called in expression position.
        if (TryResolveFunction(call.Name, out FunctionValue func))
        {
            return CallFunction(func, EvaluateArgList(call.Args, agent, env), call.Line, agent);
        }

        if (call.Name == "print")
            return Value.None;

        if (IsBuiltin(call.Name))
        {
            return ExecuteBuiltin(call.Name, EvaluateArgList(call.Args, agent, env), call.Line);
        }

        if (AgentApi.IsValueReturningAction(call.Name))
            throw RuntimeLangError(call.Line,
                $"{call.Name}() doesn't give back a value instantly — assign it to a variable first, like 'fare = {call.Name}()'.");

        if (AgentApi.IsAction(call.Name))
            throw RuntimeLangError(call.Line,
                $"{call.Name}() is an action, not a value — it can't be used here.");

        if (AgentApi.IsQuery(call.Name))
        {
            if (agent == null)
                return Value.Bool(false);
            return Value.Bool(agent.EvaluateQuery(call.Name));
        }

        if (AgentApi.IsReporter(call.Name))
        {
            if (agent == null)
                return Value.None;
            return agent.ReadReporter(call.Name, EvaluateArgList(call.Args, agent, env));
        }

        throw RuntimeLangError(call.Line, $"'{call.Name}' is not a known function.");
    }

    bool TryResolveFunction(string name, out FunctionValue func)
    {
        func = null;
        if (_globals == null) return false;
        if (!_globals.TryGet(name, out Value value)) return false;
        if (value.Kind != ValueKind.Func) return false;
        func = value.AsFunc();
        return func != null;
    }

    Value CallFunction(FunctionValue func, List<Value> args, int line, IAgentApi agent)
    {
        if (args.Count != func.Params.Count)
            throw RuntimeLangError(line,
                $"{func.Name}() needs {func.Params.Count} inputs but got {args.Count}.");

        var nested = new Interpreter();
        nested._globals = _globals;
        nested._hasReturnValue = false;

        var local = new Environment(_globals);
        for (int i = 0; i < func.Params.Count; i++)
            local.Define(func.Params[i], args[i]);

        nested._frames.Push(new Frame
        {
            Body     = func.Body,
            Env      = local,
            Function = func,
        });

        while (true)
        {
            StepResult r = nested.Step(agent);
            if (r.RuntimeError != null)
                throw RuntimeLangError(line, r.RuntimeError.Message);
            if (r.ActionName != null)
                throw RuntimeLangError(line,
                    $"{func.Name}() performs an action, so it can't be used here — call it on its own line instead.");
            if (r.Finished)
                return nested._hasReturnValue ? nested._returnValue : Value.None;
        }
    }

    Value EvaluateBinary(BinaryExpr bin, IAgentApi agent, Environment env)
    {
        switch (bin.Op)
        {
            case TokenType.KeywordOr:
            {
                Value left = Evaluate(bin.Left, agent, env);
                if (left.IsTruthy()) return left;
                return Evaluate(bin.Right, agent, env);
            }
            case TokenType.KeywordAnd:
            {
                Value left = Evaluate(bin.Left, agent, env);
                if (!left.IsTruthy()) return left;
                return Evaluate(bin.Right, agent, env);
            }
            case TokenType.KeywordIn:
                return Value.Bool(Member(Evaluate(bin.Left, agent, env), Evaluate(bin.Right, agent, env), bin.Line));
        }

        Value l = Evaluate(bin.Left, agent, env);
        Value r = Evaluate(bin.Right, agent, env);

        switch (bin.Op)
        {
            case TokenType.Plus:     return Add(l, r, bin.Line);
            case TokenType.Minus:    return Sub(l, r, bin.Line);
            case TokenType.Star:     return Mul(l, r, bin.Line);
            case TokenType.Slash:    return Div(l, r, bin.Line);
            case TokenType.SlashSlash: return FloorDiv(l, r, bin.Line);
            case TokenType.Percent:  return Mod(l, r, bin.Line);
            case TokenType.StarStar: return Pow(l, r, bin.Line);

            case TokenType.EqEq:     return Value.Bool(ValuesEqual(l, r));
            case TokenType.NotEq:    return Value.Bool(!ValuesEqual(l, r));
            case TokenType.Lt:       return Compare(l, r, bin.Line, "<",  (a,b)=>a<b,  (x,y)=>string.CompareOrdinal(x,y)<0);
            case TokenType.Gt:       return Compare(l, r, bin.Line, ">",  (a,b)=>a>b,  (x,y)=>string.CompareOrdinal(x,y)>0);
            case TokenType.Le:       return Compare(l, r, bin.Line, "<=", (a,b)=>a<=b, (x,y)=>string.CompareOrdinal(x,y)<=0);
            case TokenType.Ge:       return Compare(l, r, bin.Line, ">=", (a,b)=>a>=b, (x,y)=>string.CompareOrdinal(x,y)>=0);

            default:
                throw RuntimeLangError(bin.Line, $"unknown operator '{bin.Op}'.");
        }
    }

    Value EvaluateUnary(UnaryExpr un, IAgentApi agent, Environment env)
    {
        if (un.Op == TokenType.KeywordNot)
            return Value.Bool(!Evaluate(un.Operand, agent, env).IsTruthy());

        Value v = Evaluate(un.Operand, agent, env);
        if (v.Kind != ValueKind.Int && v.Kind != ValueKind.Float)
            throw RuntimeLangError(un.Line, $"can't use '{un.Op}' on {v.TypeName} — it needs a number.");

        if (un.Op == TokenType.Minus)
        {
            if (v.Kind == ValueKind.Int) return Value.Int(-v.I);
            return Value.Float(-v.F);
        }
        return v; // unary plus
    }

    // -------------------------------------------------------------------------
    // Indexing and mutation

    Value EvaluateIndex(IndexExpr idx, IAgentApi agent, Environment env)
    {
        Value container = Evaluate(idx.Container, agent, env);
        Value startVal  = idx.Index != null ? Evaluate(idx.Index, agent, env) : Value.None;

        if (idx.Stop == null)
        {
            // Simple index.
            switch (container.Kind)
            {
                case ValueKind.List:
                {
                    long i = startVal.AsInt();
                    var list = (List<Value>)container.Obj;
                    long real = NormalizeIndex(i, list.Count, idx.Line);
                    return list[(int)real];
                }
                case ValueKind.Str:
                {
                    long i = startVal.AsInt();
                    long real = NormalizeIndex(i, container.S.Length, idx.Line);
                    return Value.Str(container.S[(int)real].ToString());
                }
                case ValueKind.Tuple:
                {
                    long i = startVal.AsInt();
                    var arr = (Value[])container.Obj;
                    long real = NormalizeIndex(i, arr.Length, idx.Line);
                    return arr[(int)real];
                }
                case ValueKind.Dict:
                {
                    var dict = (Dictionary<Value, Value>)container.Obj;
                    if (!dict.TryGetValue(startVal, out Value v))
                        throw RuntimeLangError(idx.Line,
                            $"there's no {startVal.Display()} in this dictionary.");
                    return v;
                }
                default:
                    throw RuntimeLangError(idx.Line,
                        $"can't index {container.TypeName} with [ ].");
            }
        }

        // Slice.
        Value stopVal  = idx.Stop != null ? Evaluate(idx.Stop, agent, env) : Value.None;
        Value stepVal  = idx.Step != null ? Evaluate(idx.Step, agent, env) : Value.Int(1);
        int start = startVal.Kind == ValueKind.None ? 0 : ClampIndex((int)startVal.AsInt(), LengthOf(container));
        int stop  = stopVal.Kind  == ValueKind.None ? LengthOf(container) : ClampIndex((int)stopVal.AsInt(), LengthOf(container));
        int step  = stepVal.Kind  == ValueKind.None ? 1 : (int)stepVal.AsInt();

        if (container.Kind == ValueKind.Str)
            return Value.Str(SliceString(container.S, start, stop, step));
        if (container.Kind == ValueKind.List)
            return Value.List(SliceList((List<Value>)container.Obj, start, stop, step));
        if (container.Kind == ValueKind.Tuple)
            return Value.Tuple(SliceTuple((Value[])container.Obj, start, stop, step));

        throw RuntimeLangError(idx.Line, $"can't slice {container.TypeName}.");
    }

    void ExecuteIndexAssign(IndexAssignStmt stmt, IAgentApi agent)
    {
        Value container = Evaluate(stmt.Container, agent, CurrentEnv());
        Value key       = Evaluate(stmt.Index, agent, CurrentEnv());
        Value value     = Evaluate(stmt.Value, agent, CurrentEnv());

        switch (container.Kind)
        {
            case ValueKind.List:
            {
                var list = (List<Value>)container.Obj;
                long i = NormalizeIndex(key.AsInt(), list.Count, stmt.Line);
                list[(int)i] = value;
                break;
            }
            case ValueKind.Dict:
            {
                var dict = (Dictionary<Value, Value>)container.Obj;
                dict[key] = value;
                break;
            }
            default:
                throw RuntimeLangError(stmt.Line, $"can't assign into {container.TypeName} with [ ].");
        }
    }

    long NormalizeIndex(long i, int count, int line)
    {
        if (i < 0) i += count;
        if (i < 0 || i >= count)
            throw RuntimeLangError(line, $"the list has {count} items (0–{count - 1}), but you asked for item {i}.");
        return i;
    }

    int ClampIndex(int i, int count)
    {
        if (i < 0) i += count;
        if (i < 0) return 0;
        if (i > count) return count;
        return i;
    }

    int LengthOf(Value v)
    {
        switch (v.Kind)
        {
            case ValueKind.Str:   return v.S.Length;
            case ValueKind.List:  return ((List<Value>)v.Obj).Count;
            case ValueKind.Tuple: return ((Value[])v.Obj).Length;
            case ValueKind.Dict:  return ((Dictionary<Value, Value>)v.Obj).Count;
            default:              return 0;
        }
    }

    string SliceString(string s, int start, int stop, int step)
    {
        var sb = new System.Text.StringBuilder();
        if (step > 0)
            for (int i = start; i < stop; i += step) sb.Append(s[i]);
        else if (step < 0)
            for (int i = start; i > stop; i += step) sb.Append(s[i]);
        return sb.ToString();
    }

    List<Value> SliceList(List<Value> list, int start, int stop, int step)
    {
        var result = new List<Value>();
        if (step > 0)
            for (int i = start; i < stop; i += step) result.Add(list[i]);
        else if (step < 0)
            for (int i = start; i > stop; i += step) result.Add(list[i]);
        return result;
    }

    Value[] SliceTuple(Value[] arr, int start, int stop, int step)
    {
        var result = new List<Value>();
        if (step > 0)
            for (int i = start; i < stop; i += step) result.Add(arr[i]);
        else if (step < 0)
            for (int i = start; i > stop; i += step) result.Add(arr[i]);
        return result.ToArray();
    }

    // -------------------------------------------------------------------------
    // Operators

    static bool ValuesEqual(Value a, Value b)
    {
        if (a.Kind == ValueKind.Int && b.Kind == ValueKind.Float) return a.I == b.F;
        if (a.Kind == ValueKind.Float && b.Kind == ValueKind.Int) return a.F == b.I;
        return a.Equals(b);
    }

    Value Add(Value a, Value b, int line)
    {
        if (a.Kind == ValueKind.Int && b.Kind == ValueKind.Int) return Value.Int(a.I + b.I);
        if (IsNumber(a) && IsNumber(b)) return Value.Float(a.AsFloat() + b.AsFloat());
        if (a.Kind == ValueKind.Str && b.Kind == ValueKind.Str) return Value.Str(a.S + b.S);
        throw TypeError(a, b, "add", line);
    }

    Value Sub(Value a, Value b, int line)
    {
        if (a.Kind == ValueKind.Int && b.Kind == ValueKind.Int) return Value.Int(a.I - b.I);
        if (IsNumber(a) && IsNumber(b)) return Value.Float(a.AsFloat() - b.AsFloat());
        throw TypeError(a, b, "subtract", line);
    }

    Value Mul(Value a, Value b, int line)
    {
        if (a.Kind == ValueKind.Int && b.Kind == ValueKind.Int) return Value.Int(a.I * b.I);
        if (IsNumber(a) && IsNumber(b)) return Value.Float(a.AsFloat() * b.AsFloat());
        throw TypeError(a, b, "multiply", line);
    }

    Value Div(Value a, Value b, int line)
    {
        RequireNumbers(a, b, "divide", line);
        double denom = b.AsFloat();
        if (denom == 0) throw RuntimeLangError(line, "can't divide by zero — check the value first.");
        return Value.Float(a.AsFloat() / denom);
    }

    Value FloorDiv(Value a, Value b, int line)
    {
        RequireNumbers(a, b, "divide", line);
        long denom = b.AsInt();
        if (denom == 0) throw RuntimeLangError(line, "can't divide by zero — check the value first.");
        return Value.Int(a.AsInt() / denom);
    }

    Value Mod(Value a, Value b, int line)
    {
        RequireNumbers(a, b, "mod", line);
        long denom = b.AsInt();
        if (denom == 0) throw RuntimeLangError(line, "can't divide by zero — check the value first.");
        return Value.Int(a.AsInt() % denom);
    }

    Value Pow(Value a, Value b, int line)
    {
        RequireNumbers(a, b, "power", line);
        return Value.Float(Math.Pow(a.AsFloat(), b.AsFloat()));
    }

    Value Compare(Value a, Value b, int line, string op,
                  System.Func<double, double, bool> numberCmp,
                  System.Func<string, string, bool> stringCmp)
    {
        if (IsNumber(a) && IsNumber(b))
            return Value.Bool(numberCmp(a.AsFloat(), b.AsFloat()));
        if (a.Kind == ValueKind.Str && b.Kind == ValueKind.Str)
            return Value.Bool(stringCmp(a.S, b.S));

        string relation = op.StartsWith(">") ? "greater than" : "less than";
        throw RuntimeLangError(line,
            $"can't compare {a.TypeName} and {b.TypeName} with '{op}' — {relation} only works for numbers and text.");
    }

    bool Member(Value item, Value container, int line)
    {
        if (container.Kind == ValueKind.Str && item.Kind == ValueKind.Str)
            return container.S.Contains(item.S);
        throw RuntimeLangError(line,
            $"can't check if {item.TypeName} is inside {container.TypeName} — 'in' works for text right now.");
    }

    static bool IsNumber(Value v) => v.Kind == ValueKind.Int || v.Kind == ValueKind.Float;

    void RequireNumbers(Value a, Value b, string verb, int line)
    {
        if (!IsNumber(a) || !IsNumber(b))
            throw TypeError(a, b, verb, line);
    }

    long RequireInt(Value v, int line)
    {
        if (v.Kind != ValueKind.Int && v.Kind != ValueKind.Float)
            throw RuntimeLangError(line, $"expected a whole number here, not {v.TypeName}.");
        return v.AsInt();
    }

    InterpreterRuntimeException TypeError(Value a, Value b, string verb, int line)
    {
        return RuntimeLangError(line,
            $"can't {verb} {a.TypeName} and {b.TypeName} — check the types, or convert with str() first.");
    }

    // -------------------------------------------------------------------------
    // Collections (Phase 3+: strings; Phase 5 adds list/tuple/dict)

    List<Value> ValuesAsIterable(Value v, int line)
    {
        switch (v.Kind)
        {
            case ValueKind.Str:
            {
                var chars = new List<Value>(v.S.Length);
                foreach (char c in v.S)
                    chars.Add(Value.Str(c.ToString()));
                return chars;
            }
            case ValueKind.List:
                return v.Obj as List<Value> ?? new List<Value>();
            case ValueKind.Tuple:
                return new List<Value>(v.Obj as Value[] ?? new Value[0]);
            case ValueKind.Dict:
            {
                var dict = v.Obj as Dictionary<Value, Value>;
                var keys = new List<Value>();
                if (dict != null)
                    foreach (var k in dict.Keys)
                        keys.Add(k);
                return keys;
            }
            default:
                throw RuntimeLangError(line,
                    $"can't loop over {v.TypeName} — 'for' needs a range, list, text, or dictionary.");
        }
    }

    // -------------------------------------------------------------------------

    Environment CurrentEnv() => _frames.Count > 0 ? _frames.Peek().Env : _globals;

    static readonly System.Random Rng = new System.Random(0);

    static bool IsBuiltin(string name) =>
        name == "print" || name == "len" || name == "append" || name == "pop" || name == "range" ||
        name == "int" || name == "str" || name == "float" ||
        name == "min" || name == "max" || name == "sum" || name == "sorted" ||
        name == "randint";

    Value ExecuteBuiltin(string name, List<Value> args, int line)
    {
        switch (name)
        {
            case "len":
                RequireArgCount(name, args, 1, line);
                return Value.Int(LengthOf(args[0]));

            case "append":
                RequireArgCount(name, args, 2, line);
                if (args[0].Kind != ValueKind.List)
                    throw RuntimeLangError(line, "append() needs a list to add to.");
                ((List<Value>)args[0].Obj).Add(args[1]);
                return Value.None;

            case "pop":
                if (args.Count < 1 || args.Count > 2)
                    throw RuntimeLangError(line, "pop() needs a list and optionally an index.");
                if (args[0].Kind != ValueKind.List)
                    throw RuntimeLangError(line, "pop() needs a list.");
                var popList = (List<Value>)args[0].Obj;
                int popIndex = args.Count == 2 ? (int)NormalizeIndex(args[1].AsInt(), popList.Count, line) : popList.Count - 1;
                Value popped = popList[popIndex];
                popList.RemoveAt(popIndex);
                return popped;

            case "range":
            {
                long start = 0, stop = 0, step = 1;
                if (args.Count == 1) stop = RequireInt(args[0], line);
                else if (args.Count == 2) { start = RequireInt(args[0], line); stop = RequireInt(args[1], line); }
                else if (args.Count == 3) { start = RequireInt(args[0], line); stop = RequireInt(args[1], line); step = RequireInt(args[2], line); }
                else throw RuntimeLangError(line, "range() needs 1 to 3 numbers.");
                var rangeList = new List<Value>();
                if (step > 0) for (long i = start; i < stop; i += step) rangeList.Add(Value.Int(i));
                else if (step < 0) for (long i = start; i > stop; i += step) rangeList.Add(Value.Int(i));
                return Value.List(rangeList);
            }

            case "int":
                RequireArgCount(name, args, 1, line);
                if (args[0].Kind == ValueKind.Int) return args[0];
                if (args[0].Kind == ValueKind.Float) return Value.Int((long)args[0].F);
                if (args[0].Kind == ValueKind.Str && long.TryParse(args[0].S, out long si)) return Value.Int(si);
                throw RuntimeLangError(line, $"can't convert {args[0].TypeName} to a whole number.");

            case "float":
                RequireArgCount(name, args, 1, line);
                if (args[0].Kind == ValueKind.Int) return Value.Float(args[0].I);
                if (args[0].Kind == ValueKind.Float) return args[0];
                if (args[0].Kind == ValueKind.Str && double.TryParse(args[0].S, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double sf)) return Value.Float(sf);
                throw RuntimeLangError(line, $"can't convert {args[0].TypeName} to a decimal number.");

            case "str":
                RequireArgCount(name, args, 1, line);
                return Value.Str(args[0].Display());

            case "min":
            case "max":
                return MinMax(name, args, line);

            case "sum":
            {
                var sumList = CoerceToList(args, line);
                long sumI = 0;
                double sumF = 0;
                bool hasFloat = false;
                foreach (Value v in sumList)
                {
                    if (v.Kind == ValueKind.Float) { hasFloat = true; sumF += v.F; }
                    else if (v.Kind == ValueKind.Int) { sumI += v.I; }
                    else throw RuntimeLangError(line, "sum() only works with numbers.");
                }
                return hasFloat ? Value.Float(sumF + sumI) : Value.Int(sumI);
            }

            case "sorted":
            {
                var sortList = CoerceToList(args, line);
                var sorted = new List<Value>(sortList);
                sorted.Sort((a, b) => CompareForSort(a, b, line));
                return Value.List(sorted);
            }

            case "randint":
                RequireArgCount(name, args, 2, line);
                int a = (int)RequireInt(args[0], line);
                int b = (int)RequireInt(args[1], line);
                return Value.Int(Rng.Next(a, b + 1));

            default:
                throw RuntimeLangError(line, $"'{name}' is not a known function.");
        }
    }

    void RequireArgCount(string name, List<Value> args, int expected, int line)
    {
        if (args.Count != expected)
            throw RuntimeLangError(line, $"{name}() needs {expected} input{(expected == 1 ? "" : "s")} but got {args.Count}.");
    }

    List<Value> CoerceToList(List<Value> args, int line)
    {
        if (args.Count == 1)
        {
            if (args[0].Kind == ValueKind.List) return (List<Value>)args[0].Obj;
            if (args[0].Kind == ValueKind.Tuple) return new List<Value>((Value[])args[0].Obj);
        }
        throw RuntimeLangError(line, "expected a list or tuple to work with.");
    }

    Value MinMax(string name, List<Value> args, int line)
    {
        List<Value> values;
        if (args.Count == 1) values = CoerceToList(args, line);
        else values = args;

        if (values.Count == 0)
            throw RuntimeLangError(line, $"{name}() needs at least one value.");

        Value best = values[0];
        bool isMax = name == "max";
        for (int i = 1; i < values.Count; i++)
        {
            Value cur = values[i];
            if (!IsNumber(best) || !IsNumber(cur))
                throw RuntimeLangError(line, $"{name}() only works with numbers.");
            bool better = isMax ? cur.AsFloat() > best.AsFloat() : cur.AsFloat() < best.AsFloat();
            if (better) best = cur;
        }
        return best;
    }

    int CompareForSort(Value a, Value b, int line)
    {
        if (IsNumber(a) && IsNumber(b))
            return a.AsFloat().CompareTo(b.AsFloat());
        if (a.Kind == ValueKind.Str && b.Kind == ValueKind.Str)
            return string.CompareOrdinal(a.S, b.S);
        throw RuntimeLangError(line, "sorted() can't mix different types.");
    }

    List<Value> EvaluateArgList(List<ExprNode> args, IAgentApi agent, Environment env)
    {
        var values = new List<Value>(args.Count);
        foreach (ExprNode arg in args)
            values.Add(Evaluate(arg, agent, env));
        return values;
    }

    void EnterFunction(FunctionValue func, List<Value> args, int line)
    {
        if (args.Count != func.Params.Count)
            throw RuntimeLangError(line,
                $"{func.Name}() needs {func.Params.Count} inputs but got {args.Count}.");

        var local = new Environment(_globals);
        for (int i = 0; i < func.Params.Count; i++)
            local.Define(func.Params[i], args[i]);

        _frames.Push(new Frame
        {
            Body     = func.Body,
            Env      = local,
            Function = func,
        });
    }

    // -------------------------------------------------------------------------

    StepResult Trip(StmtNode at, string message, int line = 0)
    {
        IsFinished = true;
        return new StepResult
        {
            Finished     = true,
            RuntimeError = new LangError(line > 0 ? line : (at != null ? at.Line : 0), message),
        };
    }

    InterpreterRuntimeException RuntimeLangError(int line, string message)
    {
        return new InterpreterRuntimeException(line, message);
    }

    class InterpreterRuntimeException : Exception
    {
        public readonly int Line;
        public InterpreterRuntimeException(int line, string message) : base(message) { Line = line; }
    }

    class BreakException    : Exception { }
    class ContinueException : Exception { }
    class ReturnException   : Exception
    {
        public readonly Value Value;
        public ReturnException(Value value) { Value = value; }
    }
}
