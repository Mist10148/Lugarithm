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

    // Kept terse on purpose — the system instruction is re-sent on every request.
    const string SystemInstruction =
        "You are the Heritage Oracle in Lugarithm: warm, respectful, clear for ages 10–16. " +
        "Answer only from the supplied unlocked records; cite each used record by its exact bracketed ID. " +
        "For coding, explain the concept without giving a puzzle's solution. " +
        "If records don't support it, set status=unknown and say so. " +
        "Never invent history, family-plot details, or puzzle answers.";

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

        // Greetings and small talk get a warm local reply — no API call needed.
        if (TryLocalChat(question, out localResponse))
        {
            request = null;
            hits = Array.Empty<KnowledgeHit>();
            return false;
        }

        hits = KnowledgeRagService.Retrieve(question, save, 3);
        if (hits.Count == 0)
        {
            request = null;
            localResponse =
                "I keep two kinds of records: the heritage and stories of the towns you've reached, " +
                "and the coding lessons you've unlocked. Ask me about either and I'll dig through the archive.";
            return false;
        }

        StringBuilder prompt = new StringBuilder();
        prompt.AppendLine("UNLOCKED RECORDS:");
        prompt.AppendLine(KnowledgeRagService.FormatContext(hits));
        if (history != null && history.Count > 0)
        {
            prompt.AppendLine("RECENT CHAT (context only; records remain authoritative):");
            foreach (string turn in history.TakeLast(3)) prompt.AppendLine(turn);
        }
        prompt.AppendLine("PLAYER QUESTION:");
        prompt.Append(question);

        request = new AiRequest
        {
            Feature = AiFeature.Oracle,
            SystemInstruction = SystemInstruction,
            Prompt = prompt.ToString(),
            ResponseJsonSchema = ResponseSchema,
            MaxOutputTokens = 320
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

    // Greetings, thanks, and "what can you do" — handled locally so casual chat never
    // costs a token. Everything else falls through to the gated RAG + model path.
    static readonly string[] Greetings =
        { "hi", "hello", "hey", "yo", "kumusta", "kamusta", "musta", "good morning",
          "good afternoon", "good evening", "magandang" };
    static readonly string[] Thanks =
        { "thanks", "thank you", "salamat", "maraming salamat" };
    static readonly string[] MetaAsks =
        { "who are you", "what are you", "what can you do", "what do you do",
          "how can you help", "what can i ask", "help me", "what is this" };

    static bool TryLocalChat(string question, out string response)
    {
        string q = (question ?? "").Trim().ToLowerInvariant().Trim('?', '!', '.', ',');

        foreach (string g in Greetings)
            if (q == g || q.StartsWith(g + " "))
            {
                response = "Hello, traveler. I'm the Heritage Oracle — I keep the stories of the towns " +
                           "you've reached and the coding lessons you've unlocked. What would you like to explore?";
                return true;
            }

        foreach (string t in Thanks)
            if (q == t || q.StartsWith(t))
            {
                response = "Anytime. Keep driving the coast and more records will surface for us to talk about.";
                return true;
            }

        foreach (string m in MetaAsks)
            if (q.Contains(m))
            {
                response = "Ask me about two things: the heritage, characters and storyline of the towns you've " +
                           "visited, or any coding concept you've unlocked — commands, loops, conditionals, and more.";
                return true;
            }

        response = null;
        return false;
    }

    public static string FallbackResponse(int currentLevelIndex)
    {
        HeritageEntry entry = HeritageLibrary.ForLevel(currentLevelIndex);
        return entry != null
            ? $"(The Oracle speaks from memory:) {entry.driveSpend}"
            : "The Oracle's voice fades for a moment. Try again later.";
    }
}
