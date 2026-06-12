/// <summary>
/// Outcome of any breakdown / repair minigame. Shared by every minigame panel
/// (engine pattern-match, refuel gauge, code-fix procedure) so the breakdown
/// dispatcher can treat them uniformly. Expiry only dents the score — the run
/// always continues (PRD §5.4).
/// </summary>
public class MinigameResult
{
    public int  Score;
    public bool TimedOut;
    public int  Mistakes;
}
