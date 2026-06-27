using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable]
public sealed class VibeCodeResponse
{
    public string kind;
    public string message;
    public string code;
}

/// <summary>The Copilot-style interaction modes for the in-editor agent.</summary>
public enum VibeMode
{
    Auto,     // no explicit mode: classify the message and route to one of the below
    Ask,      // read-only: sees the world + editor, answers questions, never edits
    Plan,     // returns a numbered plan in plain language, never edits
    Agent,    // returns a structured action graph that is compiled into the editor
    Refactor  // rewrites the player's working code shorter (loops), verified equivalent-or-better
}

public static class VibeCodingService
{
    // -------------------------------------------------------------------------
    // Copilot-style agent: world-aware Ask / Plan / Agent requests.

    /// <summary>A compact, token-conscious snapshot of the puzzle the agent can read:
    /// the maze, the jeepney's state, the editor mode + contents, and the unlocked
    /// vocabulary. Shared by every mode.</summary>
    public static string BuildWorldContext(GridModel grid, AgentSim sim, AutomationPuzzleDefinition def,
                                           bool blockMode, string editorText,
                                           string[] allowedBlocks, string[] allowedQueries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("GOAL: " + (def != null && !string.IsNullOrWhiteSpace(def.goalText)
            ? def.goalText : "Reach the destination (D)."));

        if (grid != null)
        {
            // Bound the ASCII map. A large procedural town would otherwise balloon
            // the prompt into thousands of tokens — slow to first-packet, which the
            // transport reports as a timeout and the chat shows as "couldn't reach
            // the AI". Window around the jeepney when the grid is bigger than the
            // cap; small authored mazes (the minigame) are under it and unchanged.
            const int MaxSpan = 31;
            int cx = sim != null ? sim.Position.x : grid.Width / 2;
            int cy = sim != null ? sim.Position.y : grid.Height / 2;
            int x0 = 0, x1 = grid.Width, y0 = 0, y1 = grid.Height;
            bool windowed = false;
            if (grid.Width > MaxSpan)
            { x0 = Mathf.Clamp(cx - MaxSpan / 2, 0, grid.Width  - MaxSpan); x1 = x0 + MaxSpan; windowed = true; }
            if (grid.Height > MaxSpan)
            { y0 = Mathf.Clamp(cy - MaxSpan / 2, 0, grid.Height - MaxSpan); y1 = y0 + MaxSpan; windowed = true; }

            sb.AppendLine($"GRID {grid.Width}x{grid.Height} (#=wall .=road S=start D=dest P=stop, @=jeepney)" +
                          (windowed ? $" — window rows {y0}-{y1 - 1}, cols {x0}-{x1 - 1} around the jeepney:" : ":"));
            for (int y = y0; y < y1; y++)
            {
                var row = new StringBuilder();
                for (int x = x0; x < x1; x++)
                {
                    if (sim != null && sim.Position.x == x && sim.Position.y == y) { row.Append('@'); continue; }
                    switch (grid.Get(x, y))
                    {
                        case GridModel.Cell.Wall:        row.Append('#'); break;
                        case GridModel.Cell.Road:        row.Append('.'); break;
                        case GridModel.Cell.Start:       row.Append('S'); break;
                        case GridModel.Cell.Destination: row.Append('D'); break;
                        case GridModel.Cell.Stop:        row.Append('P'); break;
                    }
                }
                sb.AppendLine(row.ToString());
            }
        }

        if (sim != null)
            sb.AppendLine($"JEEPNEY: at ({sim.Position.x},{sim.Position.y}) facing " +
                          $"{AgentSim.FacingNames[sim.Facing]}; {sim.PassengersAboard} aboard.");

        sb.AppendLine("EDITOR: " + (blockMode ? "block mode" : "code mode"));
        sb.AppendLine("UNLOCKED ACTIONS/CONTROL: " + string.Join(", ", allowedBlocks ?? Array.Empty<string>()));
        sb.AppendLine("UNLOCKED QUERIES: " + string.Join(", ", allowedQueries ?? Array.Empty<string>()));

        if (!string.IsNullOrWhiteSpace(editorText))
        {
            string trimmed = editorText.Length > 700 ? editorText.Substring(0, 700) + "…" : editorText;
            sb.AppendLine("CURRENT EDITOR CONTENTS:");
            sb.AppendLine(trimmed);
        }
        return sb.ToString();
    }

    public static AiRequest BuildAgentRequest(VibeMode mode, string message, string worldContext)
    {
        switch (mode)
        {
            case VibeMode.Ask:
                return new AiRequest
                {
                    Feature = AiFeature.VibeCode,
                    SystemInstruction =
                        "You are Lugarithm's in-editor coding tutor for ages 10–16. You can see the maze, the " +
                        "jeepney's state, and the player's current code, but you are READ-ONLY: never write or " +
                        "change code. Answer the question in two to four clear, encouraging sentences.",
                    Prompt = worldContext + "\nPLAYER QUESTION:\n" + message,
                    MaxOutputTokens = 280
                };

            case VibeMode.Plan:
                return new AiRequest
                {
                    Feature = AiFeature.VibeCode,
                    SystemInstruction =
                        "You are Lugarithm's planning assistant for ages 10–16. Using what you can see of the maze " +
                        "and the jeepney, lay out a short numbered plan (3–6 steps) in plain language for how to " +
                        "solve it. Do NOT write code — describe the approach so the player can write it.",
                    Prompt = worldContext + "\nWHAT TO PLAN FOR:\n" + message,
                    MaxOutputTokens = 320
                };

            default: // Agent
                return new AiRequest
                {
                    Feature = AiFeature.VibeCode,
                    SystemInstruction =
                        "You are Lugarithm's in-editor coding agent for ages 10–16. Read the maze and the jeepney's " +
                        "state, then return a program as a flat action graph (the 'nodes' list). Use ONLY the unlocked " +
                        "actions, control structures, and queries. Express control flow with explicit markers: open with " +
                        "op 'if'/'while' and close with 'endif'/'endwhile'; use 'elif'/'else' between them. Conditions go " +
                        "in the 'condition' field and may combine unlocked queries with and/or/not. Put each command in an " +
                        "'action' node ('name' is the command, 'arg' an optional count). Add short, friendly 'comment' " +
                        "fields so the player learns from the code. Keep 'message' to one or two sentences.",
                    Prompt = worldContext + "\nTASK:\n" + message,
                    ResponseJsonSchema = ActionGraphCompiler.ResponseSchema,
                    MaxOutputTokens = 900
                };
        }
    }

    /// <summary>Agent-mode retry after a compiled graph failed local validation. Sends
    /// only the error (not the rejected program) to keep the prompt small.</summary>
    public static AiRequest BuildAgentRepairRequest(string message, string worldContext, string validationError)
    {
        AiRequest request = BuildAgentRequest(VibeMode.Agent, message, worldContext);
        request.Prompt += "\nThe previous action graph was rejected: " + validationError +
                          "\nReturn one corrected action graph using only the unlocked vocabulary.";
        return request;
    }

    /// <summary>Refactor mode: rewrite the player's already-working program to do the SAME thing
    /// in fewer instructions using the unlocked loops. Returns an action graph so it flows through
    /// the same compile + vocabulary + dry-run gate as agent mode.</summary>
    public static AiRequest BuildRefactorRequest(string playerCode, string worldContext)
    {
        return new AiRequest
        {
            Feature = AiFeature.VibeCode,
            SystemInstruction =
                "You are Lugarithm's refactoring coach for ages 10–16. The player's program already " +
                "works. Rewrite it to do the SAME thing in FEWER instructions by using the unlocked loops " +
                "(while/for) and control structures instead of repeating actions. Keep the behavior and the " +
                "route identical — only make it shorter and clearer. Return a program as a flat action graph " +
                "(the 'nodes' list) using ONLY unlocked actions, control structures, and queries. Express " +
                "control flow with explicit markers: open 'if'/'while' and close 'endif'/'endwhile'; use " +
                "'elif'/'else' between them. Put each command in an 'action' node ('name' is the command, " +
                "'arg' an optional count). Add short, friendly 'comment' fields that teach why the loop helps. " +
                "Keep 'message' to one or two sentences naming what you compressed.",
            Prompt = worldContext + "\nREWRITE THIS SHORTER — same behavior, use loops instead of repeats:\n" + playerCode,
            ResponseJsonSchema = ActionGraphCompiler.ResponseSchema,
            MaxOutputTokens = 900
        };
    }

    /// <summary>Refactor retry after the rewrite failed validation, didn't solve, or wasn't shorter.</summary>
    public static AiRequest BuildRefactorRepairRequest(string playerCode, string worldContext, string reason)
    {
        AiRequest request = BuildRefactorRequest(playerCode, worldContext);
        request.Prompt += "\nThe previous rewrite was rejected: " + reason +
                          "\nReturn one corrected, shorter action graph that still solves it, using only the unlocked vocabulary.";
        return request;
    }

    // -------------------------------------------------------------------------
    // Inline ghost-text completion (Copilot-style next-line suggestion).

    /// <summary>A deliberately tiny, fast request: complete ONLY the next single line after the
    /// cursor. Short output cap + a compact prompt keep latency and free-tier token spend minimal;
    /// pair it with <see cref="AiResponseCache.Ghost"/> so repeated prefixes never hit the API.</summary>
    public static AiRequest BuildGhostRequest(string codeBeforeCursor, string goalText,
                                              string[] allowedBlocks, string[] allowedQueries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("GOAL: " + (string.IsNullOrWhiteSpace(goalText) ? "Reach the destination (D)." : goalText));
        sb.AppendLine("UNLOCKED ACTIONS/CONTROL: " + string.Join(", ", allowedBlocks ?? Array.Empty<string>()));
        sb.AppendLine("UNLOCKED QUERIES: " + string.Join(", ", allowedQueries ?? Array.Empty<string>()));
        sb.AppendLine("CODE SO FAR (the cursor is at the very end):");
        sb.Append(codeBeforeCursor);

        return new AiRequest
        {
            Feature = AiFeature.VibeCode,
            SystemInstruction =
                "You are an inline code-completion engine for a simple Python-like language that drives a " +
                "jeepney through a maze, for kids aged 10–16. Output ONLY the single next line of code that " +
                "should follow the cursor — no explanation, no markdown fences, no blank lines, nothing else. " +
                "Use only the unlocked actions, control structures, and queries. Match the indentation the next " +
                "line should have. If no useful next line is obvious, output nothing at all.",
            Prompt = sb.ToString(),
            MaxOutputTokens = 32
        };
    }

    // -------------------------------------------------------------------------
    // Legacy single-shot tutor (kept for compatibility / tests).


    public const string ResponseSchema =
        "{\"type\":\"object\",\"properties\":{" +
        "\"kind\":{\"type\":\"string\",\"enum\":[\"explanation\",\"code\"]}," +
        "\"message\":{\"type\":\"string\"},\"code\":{\"type\":\"string\"}}," +
        "\"required\":[\"kind\",\"message\",\"code\"],\"additionalProperties\":false}";

    public static AiRequest BuildTutorRequest(string message, string[] allowedBlocks, string[] allowedQueries)
    {
        StringBuilder prompt = new StringBuilder();
        prompt.AppendLine("Unlocked actions/control structures: " + string.Join(", ", allowedBlocks ?? Array.Empty<string>()));
        prompt.AppendLine("Unlocked queries: " + string.Join(", ", allowedQueries ?? Array.Empty<string>()));
        prompt.AppendLine("If the player explicitly asks to create, automate, write, fix, or change a program, return kind=code and a complete program.");
        prompt.AppendLine("Otherwise return kind=explanation and an empty code field. Keep the message to two or three friendly sentences.");
        prompt.AppendLine("Player request:");
        prompt.Append(message);
        return new AiRequest
        {
            Feature = AiFeature.VibeCode,
            SystemInstruction =
                "You are Lugarithm's in-editor tutor and code generator. Generated programs use Python-style indentation and parentheses. " +
                "Use only explicitly unlocked names and structures. Queries belong only in conditions. Never use imports, hidden APIs, or markdown fences.",
            Prompt = prompt.ToString(),
            ResponseJsonSchema = ResponseSchema,
            MaxOutputTokens = 900
        };
    }

    public static AiRequest BuildRepairRequest(string intent, VibeCodeResponse previous, string validationError,
                                               string[] allowedBlocks, string[] allowedQueries)
    {
        AiRequest request = BuildTutorRequest(intent, allowedBlocks, allowedQueries);
        request.Prompt += "\nThe previous generated program was rejected locally:\n" + previous.code +
                          "\nValidation error: " + validationError +
                          "\nReturn one corrected complete program using only the unlocked vocabulary.";
        return request;
    }

    public static bool TryParse(string json, out VibeCodeResponse response)
    {
        response = null;
        if (string.IsNullOrWhiteSpace(json)) return false;
        try { response = JsonUtility.FromJson<VibeCodeResponse>(json); }
        catch { return false; }
        return response != null && (response.kind == "code" || response.kind == "explanation") &&
               !string.IsNullOrWhiteSpace(response.message);
    }

    public static string Validate(string code, string[] allowedBlocks, string[] allowedQueries,
                                  out List<LangError> errors)
    {
        GeneratedProgramPolicy.Validate(code, allowedBlocks, allowedQueries, out errors);
        return errors.Count > 0 ? errors[0].Message : null;
    }

    public static string Validate(string code, out List<LangError> errors)
    {
        Parser.Compile(code, out errors);
        return errors.Count > 0 ? errors[0].Message : null;
    }
}
