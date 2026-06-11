using System.Collections.Generic;

/// <summary>
/// What one interpreter step produced: either a single agent action to
/// perform, a runtime error, or the end of the program.
/// </summary>
public class StepResult
{
    /// <summary>Action to execute (an <see cref="AgentApi.Actions"/> name), or null.</summary>
    public string ActionName;

    /// <summary>The statement that produced the action (for highlighting).</summary>
    public StmtNode Node;

    public LangError RuntimeError;

    public bool Finished;
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

        /// <summary>When set, this frame is a while body — recheck on completion.</summary>
        public WhileStmt Loop;
    }

    readonly Stack<Frame> _frames = new Stack<Frame>();
    ProgramNode _program;

    public int  ActionsExecuted { get; private set; }
    public int  Evaluations     { get; private set; }
    public bool IsFinished      { get; private set; }

    // -------------------------------------------------------------------------

    /// <summary>Loads (or reloads) a program and resets all execution state.</summary>
    public void Load(ProgramNode program)
    {
        _program = program;
        _frames.Clear();
        ActionsExecuted = 0;
        Evaluations     = 0;
        IsFinished      = program == null || program.Statements.Count == 0;

        if (!IsFinished)
            _frames.Push(new Frame { Body = program.Statements });
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

        while (true)
        {
            if (_frames.Count == 0)
            {
                IsFinished = true;
                return new StepResult { Finished = true };
            }

            Frame frame = _frames.Peek();

            // Frame exhausted: loop frames re-check their condition, plain
            // frames just pop.
            if (frame.Index >= frame.Body.Count)
            {
                if (frame.Loop != null && Evaluate(frame.Loop.Condition, agent))
                {
                    frame.Index = 0;
                }
                else
                {
                    _frames.Pop();
                }

                if (Evaluations > MaxEvaluations)
                    return Trip(frame.Loop, "this program seems stuck checking conditions forever.");

                continue;
            }

            StmtNode stmt = frame.Body[frame.Index];
            frame.Index++;

            switch (stmt)
            {
                case CallStmt call:
                    ActionsExecuted++;
                    if (ActionsExecuted > MaxActions)
                        return Trip(call, $"stopped after {MaxActions} actions — this looks like an infinite loop.");
                    return new StepResult { ActionName = call.Name, Node = call };

                case WhileStmt loop:
                    if (Evaluate(loop.Condition, agent))
                        _frames.Push(new Frame { Body = loop.Body, Loop = loop });
                    break;

                case IfStmt branch:
                    if (Evaluate(branch.Condition, agent))
                        _frames.Push(new Frame { Body = branch.Body });
                    else if (branch.ElseBody != null)
                        _frames.Push(new Frame { Body = branch.ElseBody });
                    break;
            }

            if (Evaluations > MaxEvaluations)
                return Trip(stmt, "this program seems stuck checking conditions forever.");
        }
    }

    // -------------------------------------------------------------------------

    bool Evaluate(ExprNode expr, IAgentApi agent)
    {
        Evaluations++;

        switch (expr)
        {
            case NotExpr not:     return !Evaluate(not.Operand, agent);
            case QueryExpr query: return agent != null && agent.EvaluateQuery(query.Name);
            default:              return false;
        }
    }

    StepResult Trip(StmtNode at, string message)
    {
        IsFinished = true;
        return new StepResult
        {
            Finished     = true,
            RuntimeError = new LangError(at != null ? at.Line : 0, message),
        };
    }
}
