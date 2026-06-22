using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

[Serializable]
public sealed class OracleResponse
{
    public string status;
    public string answer;
    public string[] citations;
}

public static class HeritageOracleService
{
    public const string ResponseSchema =
        "{\"type\":\"object\",\"properties\":{" +
        "\"status\":{\"type\":\"string\",\"enum\":[\"answered\",\"locked\",\"unknown\"]}," +
        "\"answer\":{\"type\":\"string\",\"description\":\"Two or three concise sentences grounded only in supplied records.\"}," +
        "\"citations\":{\"type\":\"array\",\"items\":{\"type\":\"string\"},\"maxItems\":4}}," +
        "\"required\":[\"status\",\"answer\",\"citations\"],\"additionalProperties\":false}";

    const string SystemInstruction =
        "You are the Heritage Oracle in Lugarithm: warm, culturally respectful, and clear for learners aged 10–16. " +
        "Answer only from the supplied unlocked records. Cite every record you use by its exact bracketed ID. " +
        "For programming, explain the concept or syntax without reconstructing a puzzle's answer. " +
        "If the records do not support the answer, set status to unknown and say the record is unavailable. " +
        "Never infer undiscovered history, family plot details, or an executable puzzle solution.";

    public static bool TryBuildRequest(string question, SaveData save, IReadOnlyList<string> history,
                                       out AiRequest request, out IReadOnlyList<KnowledgeHit> hits,
                                       out string localResponse)
    {
        if (KnowledgeRagService.TryGetLockedTownMessage(question, save, out localResponse))
        {
            request = null;
            hits = Array.Empty<KnowledgeHit>();
            return false;
        }

        hits = KnowledgeRagService.Retrieve(question, save, 4);
        if (hits.Count == 0)
        {
            request = null;
            localResponse = "Those records are not in my recovered archive yet. Try asking about an unlocked town or coding lesson.";
            return false;
        }

        StringBuilder prompt = new StringBuilder();
        prompt.AppendLine("UNLOCKED RECORDS:");
        prompt.AppendLine(KnowledgeRagService.FormatContext(hits));
        if (history != null && history.Count > 0)
        {
            prompt.AppendLine("RECENT CHAT (context only; records remain authoritative):");
            foreach (string turn in history.TakeLast(4)) prompt.AppendLine(turn);
        }
        prompt.AppendLine("PLAYER QUESTION:");
        prompt.Append(question);

        request = new AiRequest
        {
            Feature = AiFeature.Oracle,
            SystemInstruction = SystemInstruction,
            Prompt = prompt.ToString(),
            ResponseJsonSchema = ResponseSchema,
            MaxOutputTokens = 450
        };
        localResponse = null;
        return true;
    }

    public static bool TryParseAndValidate(string json, IReadOnlyList<KnowledgeHit> supplied,
                                           out OracleResponse response)
    {
        response = null;
        if (string.IsNullOrWhiteSpace(json)) return false;
        try { response = JsonUtility.FromJson<OracleResponse>(json); }
        catch { return false; }
        if (response == null || string.IsNullOrWhiteSpace(response.answer) || response.answer.Length > 700)
            return false;
        if (response.status == "answered" && (response.citations == null || response.citations.Length == 0))
            return false;
        return KnowledgeRagService.AreCitationsValid(response.citations ?? Array.Empty<string>(), supplied);
    }

    public static string FallbackResponse(int currentLevelIndex)
    {
        HeritageEntry entry = HeritageLibrary.ForLevel(currentLevelIndex);
        return entry != null
            ? $"(The Oracle speaks from memory:) {entry.driveSpend}"
            : "The Oracle's voice fades for a moment. Try again later.";
    }
}
