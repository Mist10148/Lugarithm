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
    /// Parses the generated code and returns the first error message, or null
    /// when the code is syntactically valid.
    /// </summary>
    public static string Validate(string code, out List<LangError> errors)
    {
        Parser.Compile(code, out errors);
        return errors.Count > 0 ? errors[0].Message : null;
    }
}
