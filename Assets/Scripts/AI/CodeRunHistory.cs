using System.Collections.Generic;
using System.Text;

public sealed class CodeRunHistory
{
    readonly List<CodeRunAttempt> _attempts = new List<CodeRunAttempt>();

    public IReadOnlyList<CodeRunAttempt> Attempts => _attempts;
    public int Count => _attempts.Count;
    public CodeRunAttempt Last => _attempts.Count > 0 ? _attempts[_attempts.Count - 1] : null;

    public void Clear()
    {
        _attempts.Clear();
    }

    public CodeRunAttempt RecordStarted(string source, string mode)
    {
        var attempt = new CodeRunAttempt
        {
            runNumber = _attempts.Count + 1,
            source = source ?? "",
            mode = mode ?? "",
            status = "Running",
            summary = "Program started.",
        };
        _attempts.Add(attempt);
        return attempt;
    }

    public void Complete(CodeRunAttempt attempt, bool succeeded, string status, int steps = 0, string summary = null)
    {
        if (attempt == null) return;
        attempt.completed = true;
        attempt.succeeded = succeeded;
        attempt.status = string.IsNullOrWhiteSpace(status) ? (succeeded ? "Solved" : "Stopped") : status;
        attempt.steps = steps;
        attempt.summary = string.IsNullOrWhiteSpace(summary) ? attempt.status : summary;
    }

    public CodeRunAttempt[] Snapshot()
    {
        return _attempts.ToArray();
    }

    public static string SourceFromLines(IEnumerable<string> lines)
    {
        if (lines == null) return "";
        StringBuilder sb = new StringBuilder();
        foreach (string line in lines)
        {
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(line ?? "");
        }
        return sb.ToString();
    }
}
