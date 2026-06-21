using System.Text;

/// <summary>
/// Tallies everything that affects a manual leg's score, then hands the
/// numbers to <see cref="ScoreCalculator.ManualScore"/>. Plain class —
/// owned by <see cref="ManualDriveController"/>.
/// </summary>
public class DriveScoreTracker
{
    public int  FaresExactCount    { get; private set; }
    public int  FaresWrongCount    { get; private set; }
    public int  FaresTimedOutCount { get; private set; }
    public int  MissedStopCount    { get; private set; }
    public int  SatisfactionBonus  { get; private set; }
    public int  PassengersBoarded  { get; private set; }
    public int  PassengersDeliveredCount { get; private set; }
    public bool HadBreakdown       { get; private set; }
    public bool BreakdownTimedOut  { get; private set; }

    // -------------------------------------------------------------------------
    // Recording

    public void FareExact()    => FaresExactCount++;
    public void FareWrong()    => FaresWrongCount++;
    public void FareTimedOut() => FaresTimedOutCount++;
    public void MissedStop()   => MissedStopCount++;

    public void AddSatisfaction(int amount) => SatisfactionBonus += amount;
    public void PassengerBoarded()   => PassengersBoarded++;
    public void PassengerDelivered() => PassengersDeliveredCount++;

    public void SetBreakdownResult(bool timedOut)
    {
        HadBreakdown      = true;
        BreakdownTimedOut = timedOut;
    }

    // -------------------------------------------------------------------------
    // Output

    public int ComputeScore(float elapsedSeconds, float parTimeSeconds)
    {
        return ScoreCalculator.ManualScore(
            FaresExactCount, FaresWrongCount, FaresTimedOutCount,
            MissedStopCount, SatisfactionBonus, BreakdownTimedOut,
            elapsedSeconds, parTimeSeconds);
    }

    /// <summary>Line-item breakdown for the results panel.</summary>
    public string BuildBreakdownText(float elapsedSeconds)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Passengers carried:  {PassengersBoarded}");
        sb.AppendLine($"Exact change given:  {FaresExactCount}   (+{FaresExactCount * 10})");

        if (SatisfactionBonus  > 0) sb.AppendLine($"Passenger satisfaction:  +{SatisfactionBonus}");
        if (FaresWrongCount    > 0) sb.AppendLine($"Wrong change attempts:  {FaresWrongCount}   (-{FaresWrongCount * 50})");
        if (FaresTimedOutCount > 0) sb.AppendLine($"Fares timed out:  {FaresTimedOutCount}   (-{FaresTimedOutCount * 25})");
        if (MissedStopCount    > 0) sb.AppendLine($"Missed stops:  {MissedStopCount}   (-{MissedStopCount * 100})");

        if (HadBreakdown)
            sb.AppendLine(BreakdownTimedOut
                ? "Roadside repair fumbled:  (-100)"
                : "Roadside repair handled:  OK");

        int minutes = (int)(elapsedSeconds / 60f);
        int seconds = (int)(elapsedSeconds % 60f);
        sb.AppendLine($"Drive time:  {minutes:0}:{seconds:00}");

        return sb.ToString();
    }
}
