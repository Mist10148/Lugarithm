using System.Collections.Generic;

/// <summary>
/// Assembles the Gemini prompt for Vibe Coding / Autopilot: the player types
/// plain-English intent and the model returns valid automation-language code.
/// Also provides a lightweight parse validation so bad outputs are rejected
/// before they reach the code editor.
/// </summary>
public static class VibeCodingService
{
    public static string BuildPrompt(string intent, string[] allowedBlocks, string[] allowedQueries)
    {
        string blocks  = allowedBlocks  != null ? string.Join(" ", allowedBlocks)  : "moveForward turnLeft turnRight pickUp dropOff collectFare";
        string queries = allowedQueries != null ? string.Join(" ", allowedQueries) : "frontIsClear leftIsClear rightIsClear atStop atDestination";

        return
            "You are a code generator for Lugarithm, a game with a custom Python-style programming language.\n\n" +
            "Language rules:\n" +
            "- Python-style indentation (4 spaces per level). Use only these exact names.\n" +
            "- Actions (used as statements): " + blocks + "\n" +
            "- Queries (used ONLY inside if/while conditions): " + queries + "\n" +
            "- Syntax: if COND(): ... / while COND(): ... / if COND(): ... else: ... / not COND()\n" +
            "- All names need parentheses: moveForward() not moveForward\n" +
            "- Queries only in conditions, never standalone. Actions never as conditions.\n" +
            "- No variables, functions, imports, or comments.\n\n" +
            "Output ONLY the code — no explanation, no markdown fences.\n\n" +
            "Player's intent: \"" + intent + "\"";
    }

    /// <summary>
    /// Prompt for the in-editor AI helper: a friendly tutor that answers questions
    /// in plain language and ONLY writes code (in a single fenced block) when the
    /// player actually asks for it. Used by <see cref="VibeCodingController"/>.
    /// </summary>
    public static string BuildTutorPrompt(string message, string[] allowedBlocks, string[] allowedQueries)
    {
        string blocks  = allowedBlocks  != null && allowedBlocks.Length  > 0 ? string.Join(" ", allowedBlocks)  : "moveForward turnLeft turnRight pickUp dropOff collectFare";
        string queries = allowedQueries != null && allowedQueries.Length > 0 ? string.Join(" ", allowedQueries) : "frontIsClear leftIsClear rightIsClear atStop atDestination";

        return
            "You are a warm, encouraging coding tutor inside Lugarithm, a game that teaches a Python-style language to beginners.\n" +
            "The player drives a jeepney by writing code.\n" +
            "Actions (statements): " + blocks + "\n" +
            "Queries (only inside if/while conditions): " + queries + "\n\n" +
            "Reply in 2–4 short, friendly sentences of plain language. Do NOT lecture.\n" +
            "Only if the player explicitly asks you to write or fix the code, ALSO append ONE fenced code block (```), using only the names above, 4-space indentation, parentheses on every name, and queries only inside conditions. Otherwise include no code block at all.\n\n" +
            "Player: \"" + message + "\"";
    }

    /// <summary>
    /// Parses the generated code and returns the first error message, or null
    /// when the code is syntactically valid.
    /// </summary>
    public static string Validate(string code, out List<LangError> errors)
    {
        Parser.Compile(code, out errors);
        return errors.Count > 0 ? errors[0].Message : null;
    }
}
