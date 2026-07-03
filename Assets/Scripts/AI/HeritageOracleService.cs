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
        "You are the Oracle in Lugarithm: a warm, clear guide and coding tutor for ages 10–16. " +
        "Teach and answer using the supplied records; cite each record you use by its exact bracketed ID. " +
        "For coding concepts and how the game works, explain fully and give a short example — just don't " +
        "hand over the finished solution to a specific puzzle. For heritage, stay strictly within the " +
        "supplied records. If the records don't cover it, set status=unknown and say so. " +
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
                "I keep the heritage and stories of the towns you've reached, and I can teach any coding " +
                "idea the game covers — commands, conditionals, loops, functions and more. Ask me about " +
                "either and I'll dig through the archive.";
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

    /// <summary>Cache key for a validated answer: the normalized question plus the ordered
    /// IDs of the records it was grounded in. Same question against the same unlocked records
    /// yields the same answer, so a hit is safe to replay without another API call.</summary>
    public static string CacheKey(string question, IReadOnlyList<KnowledgeHit> hits)
    {
        string q = (question ?? "").Trim().ToLowerInvariant();
        var ids = new List<string>();
        if (hits != null)
            foreach (KnowledgeHit hit in hits)
                if (hit?.Chunk != null) ids.Add(hit.Chunk.id);
        return q + "::" + string.Join(",", ids);
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
    // Only the *bare* "who are you / what can you do" openers — anything with an actual
    // topic ("teach me loops", "how do functions work", "help me with fares") is a real
    // question and must reach the knowledge base instead of this canned reply.
    static readonly string[] MetaAsks =
        { "who are you", "what are you", "what can you do", "what do you do",
          "how can you help", "what can i ask" };

    static bool TryLocalChat(string question, out string response)
    {
        string q = (question ?? "").Trim().ToLowerInvariant().Trim('?', '!', '.', ',');

        foreach (string g in Greetings)
            if (q == g || q.StartsWith(g + " "))
            {
                response = "Hello, traveler. I'm the Oracle — I keep the stories of the towns " +
                           "you've reached and I can teach any coding idea the road has taught you. " +
                           "What would you like to explore?";
                return true;
            }

        foreach (string t in Thanks)
            if (q == t || q.StartsWith(t))
            {
                response = "Anytime. Keep driving the coast and more records will surface for us to talk about.";
                return true;
            }

        // Match a meta-opener only when it is essentially the whole question — a couple of
        // trailing words are fine ("what can you do here?"), but a real topic after it is
        // not, so teaching requests fall through to retrieval.
        foreach (string m in MetaAsks)
            if (q == m || (q.StartsWith(m) && q.Length <= m.Length + 6))
            {
                response = "Ask me about two things: the heritage, characters and storyline of the towns you've " +
                           "visited, or any coding idea — commands, conditionals, loops, functions and more. " +
                           "Say things like \"teach me loops\" or \"how do functions work\" and I'll explain.";
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
