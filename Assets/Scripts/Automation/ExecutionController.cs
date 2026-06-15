using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Owns the execution clock for Automation Mode: steps the
/// <see cref="Interpreter"/> one action at a time, applies each action to the
/// <see cref="AgentSim"/>, and lets the <see cref="JeepneyAgentView"/> animate
/// it. Run / Pause / Reset and the 1×/2×/5× speeds all live here.
/// </summary>
public class ExecutionController : MonoBehaviour
{
    public enum ExecState { Idle, Running, Paused, Finished }

    [Header("Timing")]
    [SerializeField] private float baseStepSeconds = 0.45f;

    public ExecState State { get; private set; } = ExecState.Idle;
    public float Speed { get; private set; } = 1f;

    /// <summary>Fired after each executed action (sim already updated).</summary>
    public event Action<AgentActionResult, StepResult> OnStepDone;

    public event Action<LangError> OnRuntimeError;

    /// <summary>Fired when execution stops; argument = goal met.</summary>
    public event Action<bool> OnFinished;

    public event Action OnWorldReset;

    readonly Interpreter _vm = new Interpreter();

    AgentSim          _sim;
    IAgentView        _view;
    IGridSpace        _space;
    IStopView         _stopView;
    GridModel         _grid;
    AutomationPuzzleDefinition _def;
    int               _startFacing;
    Coroutine         _loop;

    public AgentSim Sim => _sim;

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

    // -------------------------------------------------------------------------
    // Controls

    /// <summary>Starts (or restarts) execution of a compiled program.</summary>
    public void Run(ProgramNode program)
    {
        Stop();
        ResetWorld();

        _vm.Load(program);
        State = ExecState.Running;
        _loop = StartCoroutine(ExecutionLoop());
    }

    public void TogglePause()
    {
        if (State == ExecState.Running)      State = ExecState.Paused;
        else if (State == ExecState.Paused)  State = ExecState.Running;
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

        State = ExecState.Idle;
        OnWorldReset?.Invoke();
    }

    public void SetSpeed(float speed)
    {
        Speed = Mathf.Clamp(speed, 1f, 8f);
    }

    void Stop()
    {
        if (_loop != null)
        {
            StopCoroutine(_loop);
            _loop = null;
        }
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
                AgentActionResult moveResult = _sim.Apply(_sim.DequeueMove());
                OnStepDone?.Invoke(moveResult, new StepResult { ActionName = moveResult.Action });

                float moveDuration = baseStepSeconds / Speed;
                if (_view != null)
                    yield return _view.PlayAction(moveResult, moveDuration);
                else
                    yield return new WaitForSeconds(moveDuration);

                if (_sim.IsWin(_def))
                {
                    State = ExecState.Finished;
                    OnFinished?.Invoke(true);
                    yield break;
                }
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

            AgentActionResult result = _sim.Apply(step.ActionName);
            if (!string.IsNullOrEmpty(step.BindResultTo))
                _vm.DeliverActionResult(result.ReturnValue);

            OnStepDone?.Invoke(result, step);

            float duration = baseStepSeconds / Speed;
            if (_view != null)
                yield return _view.PlayAction(result, duration);
            else
                yield return new WaitForSeconds(duration);

            // Goal reached mid-program still counts — stop right away.
            if (_sim.IsWin(_def))
            {
                State = ExecState.Finished;
                OnFinished?.Invoke(true);
                yield break;
            }
        }
    }
}
