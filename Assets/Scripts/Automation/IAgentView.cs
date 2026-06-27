using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Presentation-neutral view of the automation jeepney/agent. The execution
/// controller drives this so the same program can animate either the iso grid
/// jeepney or the top-down road jeepney.
/// </summary>
public interface IAgentView
{
    /// <summary>Places the agent at the starting cell and faces it.</summary>
    void Init(IGridSpace space, Vector2Int cell, int facing);

    /// <summary>Instantly teleports the agent to a cell/facing.</summary>
    void SnapTo(Vector2Int cell, int facing);

    /// <summary>Plays one action (move, turn, pickUp, dropOff, collectFare).</summary>
    IEnumerator PlayAction(AgentActionResult result, float duration);
}

/// <summary>Optional richer view for continuous navigation-macro playback.</summary>
public interface IPathAgentView : IAgentView
{
    IEnumerator PlayPath(IReadOnlyList<AgentActionResult> moves, float secondsPerStep);
}
