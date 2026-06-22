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

public static class VibeCodingService
{
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
