using UnityEngine;
using TMPro;

/// <summary>
/// One-line real-time variable monitor: jeepney position, facing, steps used,
/// passengers aboard, fares collected, and the source line being executed.
/// </summary>
public class StateMonitorController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text label;

    // -------------------------------------------------------------------------

    public void Refresh(AgentSim sim, int currentLine)
    {
        if (label == null || sim == null) return;

        string lineInfo = currentLine > 0 ? $"line {currentLine}" : "—";

        label.text = $"pos ({sim.Position.x},{sim.Position.y})   ·   " +
                     $"facing {AgentSim.FacingNames[sim.Facing]}   ·   " +
                     $"steps {sim.StepsUsed}   ·   " +
                     $"aboard {sim.PassengersAboard}   ·   " +
                     $"fares ₱{sim.FaresCollected}   ·   {lineInfo}";
    }

    public void ShowIdle()
    {
        if (label != null)
            label.text = "ready — write a program and press RUN";
    }
}
