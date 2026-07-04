using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable]
public sealed class CodeReviewAnnotation
{
    public string side;
    public int startLine;
    public int endLine;
    public string title;
    public string explanation;
    public string category;
}

[Serializable]
public sealed class MentorReview
{
    public string summary;
    public string optimizedCode;
    public CodeReviewAnnotation[] annotations;
}

public static class CodingMentorService
{
    public const string ResponseSchema =
        "{\"type\":\"object\",\"properties\":{" +
        "\"summary\":{\"type\":\"string\"},\"optimizedCode\":{\"type\":\"string\"}," +
        "\"annotations\":{\"type\":\"array\",\"maxItems\":8,\"items\":{\"type\":\"object\",\"properties\":{" +
        "\"side\":{\"type\":\"string\",\"enum\":[\"player\",\"optimized\"]}," +
        "\"startLine\":{\"type\":\"integer\",\"minimum\":1},\"endLine\":{\"type\":\"integer\",\"minimum\":1}," +
        "\"title\":{\"type\":\"string\"},\"explanation\":{\"type\":\"string\"}," +
        "\"category\":{\"type\":\"string\",\"enum\":[\"logic\",\"memory\",\"steps\",\"clarity\",\"complexity\"]}}," +
        "\"required\":[\"side\",\"startLine\",\"endLine\",\"title\",\"explanation\",\"category\"],\"additionalProperties\":false}}," +
        "\"required\":[\"summary\",\"optimizedCode\",\"annotations\"],\"additionalProperties\":false}";

    public static AiRequest BuildRequest(string levelDisplayName, string conceptName,
                                         CodeAnalysis analysis, string playerSolution,
                                         string authoredOptimal, string[] allowedBlocks,
                                         string[] allowedQueries,
                                         IReadOnlyList<CodeRunAttempt> attempts = null,
                                         bool preserveAuthoredOptimal = false)
    {
        StringBuilder prompt = new StringBuilder();
        prompt.AppendLine($"Level: {levelDisplayName}");
        prompt.AppendLine($"Concept: {conceptName}");
        prompt.AppendLine($"Deterministic efficiency: {analysis.EfficiencyScore}/100 ({analysis.Summary})");
        prompt.AppendLine($"Runs attempted before completion: {Math.Max(1, analysis.AttemptCount)}");
        prompt.AppendLine($"Measured structure: {analysis.StatementCount} statements, weight {analysis.WeightedComplexity}, {analysis.ComplexityClass}, max nesting {analysis.MaxNesting}.");
        prompt.AppendLine($"Unlocked actions/control blocks: {string.Join(", ", allowedBlocks ?? Array.Empty<string>())}");
        prompt.AppendLine($"Unlocked queries: {string.Join(", ", allowedQueries ?? Array.Empty<string>())}");
        prompt.AppendLine("Current fare flow: collectFare() records payment, changeOwed() reports sukli, giveChange(changeOwed()) settles it before dropOff(). Procedural routes use routeComplete().");
        if (attempts != null)
        {
            prompt.AppendLine("RUN HISTORY:");
            foreach (CodeRunAttempt attempt in attempts)
            {
                if (attempt == null) continue;
                prompt.AppendLine($"Run {attempt.runNumber}: {attempt.mode} / {attempt.status} / steps {attempt.steps}");
                if (!string.IsNullOrWhiteSpace(attempt.summary))
                    prompt.AppendLine($"Summary: {attempt.summary}");
            }
        }
        prompt.AppendLine("PLAYER SOLUTION:"); prompt.AppendLine(playerSolution);
        prompt.AppendLine("AUTHORED VALID REFERENCE:"); prompt.AppendLine(authoredOptimal);
        if (preserveAuthoredOptimal)
            prompt.AppendLine("This is a line-order minigame: return optimizedCode exactly as the authored valid reference.");
        prompt.AppendLine("Return a concise encouraging review, a valid refactor using only unlocked vocabulary, and accurate line annotations. Do not change the puzzle goal.");
        return new AiRequest
        {
            Feature = AiFeature.Mentor,
            SystemInstruction =
                "You are Lugarithm's coding mentor for learners aged 10–16. The supplied score and measurements are authoritative. " +
                "Lead with what worked, then explain one or two improvements plainly. Never invent performance measurements.",
            Prompt = prompt.ToString(),
            ResponseJsonSchema = ResponseSchema,
            MaxOutputTokens = 800
        };
    }

    public static bool TryParseAndValidate(string json, string authoredOptimal,
                                           string[] allowedBlocks, string[] allowedQueries,
                                           int playerLineCount, out MentorReview review,
                                           bool preserveAuthoredOptimal = false)
    {
        review = null;
        if (string.IsNullOrWhiteSpace(json)) return false;
        try { review = JsonUtility.FromJson<MentorReview>(json); }
        catch { return false; }
        if (review == null || string.IsNullOrWhiteSpace(review.summary)) return false;
        if (preserveAuthoredOptimal)
            review.optimizedCode = authoredOptimal;
        else if (!GeneratedProgramPolicy.Validate(review.optimizedCode, allowedBlocks, allowedQueries, out _))
            review.optimizedCode = authoredOptimal;
        int optimizedLines = LineCount(review.optimizedCode);
        List<CodeReviewAnnotation> valid = new List<CodeReviewAnnotation>();
        foreach (CodeReviewAnnotation annotation in review.annotations ?? Array.Empty<CodeReviewAnnotation>())
        {
            int max = annotation.side == "player" ? playerLineCount : optimizedLines;
            if (annotation.startLine < 1 || annotation.endLine < annotation.startLine || annotation.endLine > max) continue;
            if (string.IsNullOrWhiteSpace(annotation.title) || string.IsNullOrWhiteSpace(annotation.explanation)) continue;
            valid.Add(annotation);
        }
        review.annotations = valid.ToArray();
        return true;
    }

    public static MentorReview Fallback(string optimal, CodeAnalysis analysis)
        => new MentorReview
        {
            summary = $"You solved the route. Your efficiency score is {analysis.EfficiencyScore}/100. " +
                      "Compare your structure with the reference and look for repeated actions that a condition or loop could express more clearly.",
            optimizedCode = optimal,
            annotations = Array.Empty<CodeReviewAnnotation>()
        };

    static int LineCount(string source) => string.IsNullOrEmpty(source) ? 0 : source.Replace("\r", "").Split('\n').Length;
}
