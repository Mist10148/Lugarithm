using System;

[Serializable]
public sealed class CodeRunAttempt
{
    public int runNumber;
    public string source;
    public string mode;
    public string status;
    public string summary;
    public int steps;
    public bool completed;
    public bool succeeded;

    public string DisplayName
    {
        get
        {
            string label = string.IsNullOrWhiteSpace(mode) ? "Run" : mode;
            string result = string.IsNullOrWhiteSpace(status) ? "" : $" - {status}";
            return $"Run {runNumber}: {label}{result}";
        }
    }
}
