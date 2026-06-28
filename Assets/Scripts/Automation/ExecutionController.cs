using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns the execution clock for Automation Mode: steps the
/// <see cref="Interpreter"/> one action at a time, applies each action to the
/// <see cref="AgentSim"/>, and lets the <see cref="JeepneyAgentView"/> animate
/// it. Run / Pause / Reset, a continuous speed slider, single-step, and
/// per-line heatmap detection all live here.
/// </summary>
public class ExecutionController : MonoBehaviour
{
    public enum ExecState { Idle, Running, Paused, Finished }

    [Header("Timing")]
    [SerializeField] private float baseStepSeconds = 1.0f;   // heavier, slower cruise (Manual-like weight)

    [Header("Heatmap")]
    [Tooltip("A line that executes this many times in a single frame is considered 'hot'.")]
    [SerializeField] private int hotLineThreshold = 200;

    public ExecState State { get; private set; } = ExecState.Idle;
    public float Speed { get; private set; } = 1f;

    /// <summary>Fired after each executed action (sim already updated).</summary>
    public event Action<AgentActionResult, StepResult> OnStepDone;

    public event Action<LangError> OnRuntimeError;

    /// <summary>Fired when execution stops; argument = goal met.</summary>
    public event Action<bool> OnFinished;

    public event Action OnWorldReset;

    /// <summary>Fired when a single line is executing very hot this frame.</summary>
    public event Action<int> OnHotLine;

    readonly Interpreter _vm = new Interpreter();

    AgentSim          _sim;
    IAgentView        _view;
    IGridSpace        _space;
    IStopView         _stopView;
    GridModel         _grid;
    AutomationPuzzleDefinition _def;
    int               _startFacing;
    Coroutine         _loop;

    bool  _singleStep;

    // Heatmap state: hits per line within the current frame, reset each yield.
    Dictionary<int, int> _frameLineHits = new Dictionary<int, int>();

    const int EndlessPathBatchSize = 4;

    public AgentSim Sim => _sim;
    public IReadOnlyDictionary<int, int> LineHits => _vm.LineHits;

    /// <summary>True while the agent is mid-animation (a path or action is playing).
    /// Procedural streaming only re-rasterizes the grid when this is false, so an
    /// in-flight animation can never have the world shift under it.</summary>
    public bool Busy { get; private set; }

    bool EndlessRouteActive => _def != null && _def.endlessRoute;

    // -------------------------------------------------------------------------

    public void Init(GridModel grid, AgentSim sim, IAgentView view,
                     IGridSpace space, IStopView stopView,
                     AutomationPuzzleDefinition def, int startFacing)
    {
        _grid        = grid;
        _sim         = sim;
        _view        = view;
        _space       = space;
        _stopView    = stopView;
        _def         = def;
        _startFacing = startFacing;

        if (_view != null)
            _view.Init(_space, _sim.Position, _sim.Facing);
    }

    public void RebindWorld(GridModel grid, IGridSpace space, IStopView stopView,
                            AutomationPuzzleDefinition def, int startFacing)
    {
        Stop();

        _grid        = grid;
        _space       = space;
        _stopView    = stopView;
        _def         = def;
        _startFacing = startFacing;
        State        = ExecState.Idle;
        _singleStep  = false;
        _frameLineHits.Clear();

        if (_view != null && _sim != null)
            _view.Init(_space, _sim.Position, _sim.Facing);

        OnWorldReset?.Invoke();
    }

    /// <summary>
    /// Swaps the world references for a streamed chunk WITHOUT stopping the program,
    /// resetting state, or restarting the execution coroutine — so a mid-run autopilot
    /// keeps driving straight into the freshly appended stretch. Only safe to call when
    /// <see cref="Busy"/> is false and the move queue is empty (a static-world boundary);
    /// the caller (AutomationDriveController) enforces that. The agent view is re-pinned
    /// to its current cell under the new grid mapping (a no-op visually while idle).
    /// </summary>
    public void RebindStreamingWorld(GridModel grid, IGridSpace space, IStopView stopView,
                                     AutomationPuzzleDefinition def, int startFacing)
    {
        _grid        = grid;
        _space       = space;
        _stopView    = stopView;
        _def         = def;
        _startFacing = startFacing;

        if (_view != null && _sim != null)
            _view.Init(_space, _sim.Position, _sim.Facing);
    }

    // -------------------------------------------------------------------------
    // Controls

    /// <summary>Starts (or restarts) execution of a compiled program.</summary>
    public void Run(ProgramNode program, bool resetWorld = true)
    {
        Stop();
        if (resetWorld)
            ResetWorld();

        _vm.Load(program);
        // Endless procedural legs allow an intended forever-cruise (while True: keepDriving());
        // execution is paced one action per step, so lift the runaway-loop guards for them.
        bool endless = _def != null && _def.endlessRoute;
        _vm.ActionBudget = endless ? int.MaxValue : Interpreter.MaxActions;
        _vm.EvalBudget   = endless ? int.MaxValue : Interpreter.MaxEvaluations;
        _frameLineHits.Clear();
        State = ExecState.Running;
        _loop = StartCoroutine(ExecutionLoop());
    }

    public void TogglePause()
    {
        if (State == ExecState.Running)      State = ExecState.Paused;
        else if (State == ExecState.Paused)  State = ExecState.Running;
    }

    /// <summary>Explicitly pause/resume — used when a fuel/breakdown mini-game
    /// interrupts the run. No-op unless currently Running/Paused.</summary>
    public void SetPaused(bool paused)
    {
        if (paused && State == ExecState.Running)      State = ExecState.Paused;
        else if (!paused && State == ExecState.Paused) State = ExecState.Running;
    }

    /// <summary>Executes exactly one statement, then pauses again.</summary>
    public void StepOnce()
    {
        if (State == ExecState.Idle || State == ExecState.Finished) return;
        _singleStep = true;
        if (State == ExecState.Paused) State = ExecState.Running;
    }

    /// <summary>Stops execution and puts the world back to its start state.</summary>
    public void ResetWorld()
    {
        Stop();

        if (_sim != null)
        {
            _sim.Reset();
            if (_view     != null) _view.SnapTo(_sim.Position, _sim.Facing);
            if (_stopView != null) _stopView.ResetStops();
        }

        _frameLineHits.Clear();
        State = ExecState.Idle;
        OnWorldReset?.Invoke();
    }

    public void SetSpeed(float speed)
    {
        Speed = Mathf.Clamp(speed, 0.2f, 8f);
    }

    void Stop()
    {
        if (_loop != null)
        {
            StopCoroutine(_loop);
            _loop = null;
        }
        Busy = false;
    }

    // -------------------------------------------------------------------------

    IEnumerator ExecutionLoop()
    {
        while (true)
        {
            while (State == ExecState.Paused)
                yield return null;

            // A navigation macro (driveToNextStop / driveToDestination) plans a
            // path into the sim's move queue; drain it one cell per visual step so
            // the self-driving jeepney animates like real driving.
            if (_sim != null && _sim.HasPendingMoves)
            {
                if (_singleStep)
                {
                    AgentActionResult moveResult = _sim.Apply(_sim.DequeueMove());
                    OnStepDone?.Invoke(moveResult, new StepResult { ActionName = moveResult.Action });

                    float moveDuration = baseStepSeconds / Speed;
                    Busy = true;
                    if (_view != null)
                        yield return _view.PlayAction(moveResult, moveDuration);
                    else
                        yield return new WaitForSeconds(moveDuration);
                    Busy = false;

                    _singleStep = false;
                    State = ExecState.Paused;

                    if (_sim.IsWin(_def))
                    {
                        State = ExecState.Finished;
                        OnFinished?.Invoke(true);
                        yield break;
                    }
                    continue;
                }

                var moves = new List<AgentActionResult>();
                bool wonDuringPath = false;
                int batchLimit = EndlessRouteActive
                    ? EndlessPathBatchSize
                    : int.MaxValue;
                while (_sim.HasPendingMoves && moves.Count < batchLimit)
                {
                    AgentActionResult moveResult = _sim.Apply(_sim.DequeueMove());
                    moves.Add(moveResult);
                    OnStepDone?.Invoke(moveResult, new StepResult { ActionName = moveResult.Action });
                    if (_sim.IsWin(_def)) { wonDuringPath = true; break; }
                }

                float pathMoveDuration = baseStepSeconds / Speed;
                Busy = true;
                if (_view is IPathAgentView pathView)
                    yield return pathView.PlayPath(moves, pathMoveDuration);
                else if (_view != null)
                    foreach (AgentActionResult move in moves)
                        yield return _view.PlayAction(move, pathMoveDuration);
                else
                    yield return new WaitForSeconds(pathMoveDuration * moves.Count);
                Busy = false;

                if (wonDuringPath && !EndlessRouteActive)
                {
                    State = ExecState.Finished;
                    OnFinished?.Invoke(true);
                    yield break;
                }
                yield return null;
                continue;
            }

            StepResult step = _vm.Step(_sim);

            if (step.RuntimeError != null)
            {
                State = ExecState.Finished;
                OnRuntimeError?.Invoke(step.RuntimeError);
                OnFinished?.Invoke(false);
                yield break;
            }

            if (step.Finished)
            {
                State = ExecState.Finished;
                OnFinished?.Invoke(_sim.IsWin(_def));
                yield break;
            }

            AgentActionResult result = _sim.Apply(step.ActionName, step.ActionArgs);
            if (!string.IsNullOrEmpty(step.BindResultTo))
                _vm.DeliverActionResult(result.ReturnValue);

            OnStepDone?.Invoke(result, step);

            // Heatmap tracking: count per line within this frame.
            if (step.Node != null && step.Node.Line > 0)
            {
                int line = step.Node.Line;
                _frameLineHits[line] = _frameLineHits.TryGetValue(line, out int n) ? n + 1 : 1;

                if (_frameLineHits[line] > hotLineThreshold)
                    OnHotLine?.Invoke(line);
            }

            float duration = baseStepSeconds / Speed;
            Busy = true;
            if (_view != null)
                yield return _view.PlayAction(result, duration);
            else
                yield return new WaitForSeconds(duration);
            Busy = false;

            _frameLineHits.Clear();

            if (_singleStep)
            {
                _singleStep = false;
                State = ExecState.Paused;
            }

            // Goal reached mid-program still counts. Endless procedural story legs keep
            // the VM alive after the one-time completion signal so free-roam remains smooth.
            if (_sim.IsWin(_def))
            {
                if (!EndlessRouteActive)
                {
                    State = ExecState.Finished;
                    OnFinished?.Invoke(true);
                    yield break;
                }
            }
        }
    }
}
