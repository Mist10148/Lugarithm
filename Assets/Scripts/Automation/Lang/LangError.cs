/// <summary>
/// A plain-English problem report from the lexer, parser, or interpreter.
/// Messages are written for beginners — no jargon, no stack traces (PRD §5.3).
/// </summary>
public class LangError
{
    /// <summary>1-based source line (0 when the error has no specific line).</summary>
    public int Line;

    public string Message;

    public LangError(int line, string message)
    {
        Line    = line;
        Message = message;
    }

    public override string ToString()
    {
        return Line > 0 ? $"Line {Line}: {Message}" : Message;
    }
}
