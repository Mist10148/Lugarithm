using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

public enum KnowledgeDomain { Heritage, Coding }
public enum KnowledgeGate { UnlockedLevel, CompletedLevel, DiscoveredFact }

[Serializable]
public sealed class KnowledgeChunk
{
    public string id;
    public KnowledgeDomain domain;
    public int levelIndex;
    public KnowledgeGate gate;
    public string factKey;
    public string town;
    public string title;
    public string body;
    public string sourceLabel;
    public string[] aliases;
    public string[] concepts;
}

public sealed class KnowledgeHit
{
    public KnowledgeChunk Chunk;
    public float Score;
}

/// <summary>
/// Small local hybrid RAG index. It combines field-weighted lexical matching,
/// phrase/alias boosts and concept tags, then applies progression gates before
/// ranking so locked text never enters an AI prompt.
/// </summary>
public static class KnowledgeRagService
{
    static readonly Regex Words = new Regex(@"[\p{L}\p{N}][\p{L}\p{N}'-]*", RegexOptions.Compiled);

    // Declared before Chunks: BuildChunks() reads GameGuide, and static fields
    // initialize in textual order, so this must exist first.
    static readonly (string id, string title, string body, string[] aliases)[] GameGuide =
    {
        ("guide:modes", "Automation vs Manual mode",
            "Lugarithm has two ways to drive. In Manual mode you steer the jeepney yourself. " +
            "In Automation mode you write a little program — in Blocks (drag-and-snap) or Code " +
            "(typed) — and press Run to let it drive. Switch the editor with the Editor: Code / " +
            "Editor: Blocks button.",
            new[] { "automation", "manual", "blocks", "code", "editor mode" }),
        ("guide:run", "Run, Pause, Step, Reset and running again",
            "Run starts your program; Pause and Run resume where it stopped; Step runs one action " +
            "at a time; the speed button (x1.0) changes how fast she drives. Pressing Run again does " +
            "NOT send the jeepney back to the start — she keeps her place, her riders and her fares, " +
            "so you can run a short routine again and again to serve the route. Only Reset returns " +
            "her to the garage.",
            new[] { "run", "pause", "step", "reset", "run again", "autopilot" }),
        ("guide:passengers-fares", "Passengers, fares and change (sukli)",
            "Stop where a passenger waits and pickUp() them, but only if there's room (seatsLeft, " +
            "isFull). collectFare() takes their payment; giveChange(changeOwed()) returns the exact " +
            "sukli; dropOff() lets them down at their stop. Deliver every rider to finish a leg.",
            new[] { "passenger", "fare", "sukli", "change", "collectFare" }),
        ("guide:fuel-repair", "Fuel and breakdowns",
            "The jeepney burns fuel and can break down. Refueling is a by-hand mini-game — tap to " +
            "pump and stop in the green band. A breakdown pauses the drive for a quick repair drill; " +
            "code can't fix her, your hands do.",
            new[] { "fuel", "gas", "refuel", "breakdown", "repair" }),
    };

    static readonly IReadOnlyList<KnowledgeChunk> Chunks = BuildChunks();
    static readonly Dictionary<string, string[]> SemanticAliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["array"] = new[] { "list", "index", "collection", "items" },
        ["loop"] = new[] { "repeat", "while", "iteration", "pattern" },
        ["function"] = new[] { "procedure", "helper", "reuse", "method" },
        ["condition"] = new[] { "if", "decision", "branch", "query", "sensor" },
        ["heritage"] = new[] { "history", "culture", "tradition", "landmark" },
        ["church"] = new[] { "facade", "parish", "saint", "stone" },
        ["weaving"] = new[] { "hablon", "loom", "cloth", "textile" },
        ["code"] = new[] { "program", "syntax", "automation", "script" },
        ["coding"] = new[] { "code", "program", "syntax", "automation" },
        ["sequence"] = new[] { "sequencing", "order", "steps" }
    };

    public static IReadOnlyList<KnowledgeChunk> All => Chunks;

    public static bool TryGetLockedTownMessage(string question, SaveData save, out string message)
    {
        string normalized = Normalize(question);
        foreach (HeritageEntry entry in HeritageLibrary.All)
        {
            if (entry.levelIndex < 0) continue;
            bool named = normalized.Contains(Normalize(entry.townName)) ||
                         normalized.Contains(Normalize(entry.townKey.Replace('-', ' ')));
            if (named && !ProgressionRules.IsUnlocked(save, entry.levelIndex))
            {
                message = "My records on that region are still locked. Explore further to help me recover those files.";
                return true;
            }
        }
        message = null;
        return false;
    }

    public static IReadOnlyList<KnowledgeHit> Retrieve(string query, SaveData save, int maxResults = 4,
                                                        KnowledgeDomain? domain = null)
    {
        HashSet<string> queryTerms = ExpandTerms(Tokenize(query));
        string normalizedQuery = Normalize(query);
        List<KnowledgeHit> candidates = new List<KnowledgeHit>();

        foreach (KnowledgeChunk chunk in Chunks)
        {
            if (domain.HasValue && chunk.domain != domain.Value) continue;
            if (!IsEligible(chunk, save)) continue;

            float score = 0f;
            HashSet<string> bodyTerms = Tokenize(chunk.body);
            HashSet<string> titleTerms = Tokenize(chunk.title);
            HashSet<string> sourceTerms = Tokenize(chunk.sourceLabel);
            foreach (string term in queryTerms)
            {
                if (titleTerms.Contains(term)) score += 4f;
                if (bodyTerms.Contains(term)) score += 1f;
                if (sourceTerms.Contains(term)) score += 2f;
                if (chunk.concepts != null && chunk.concepts.Any(c => Normalize(c).Contains(term))) score += 3f;
            }

            if (!string.IsNullOrEmpty(chunk.town) && normalizedQuery.Contains(Normalize(chunk.town))) score += 8f;
            if (chunk.aliases != null)
                foreach (string alias in chunk.aliases)
                    if (normalizedQuery.Contains(Normalize(alias))) score += 6f;

            if (score > 0f) candidates.Add(new KnowledgeHit { Chunk = chunk, Score = score });
        }

        // Diversify by source label so one long town entry cannot crowd out coding context.
        List<KnowledgeHit> result = new List<KnowledgeHit>();
        HashSet<string> usedSources = new HashSet<string>();
        foreach (KnowledgeHit hit in candidates.OrderByDescending(h => h.Score))
        {
            if (result.Count >= maxResults) break;
            string source = hit.Chunk.sourceLabel ?? hit.Chunk.id;
            if (usedSources.Contains(source) && candidates.Count > maxResults) continue;
            usedSources.Add(source);
            result.Add(hit);
        }
        if (result.Count < maxResults)
        {
            foreach (KnowledgeHit hit in candidates.OrderByDescending(h => h.Score))
            {
                if (result.Contains(hit)) continue;
                result.Add(hit);
                if (result.Count >= maxResults) break;
            }
        }
        return result;
    }

    public static string FormatContext(IReadOnlyList<KnowledgeHit> hits)
    {
        StringBuilder sb = new StringBuilder();
        foreach (KnowledgeHit hit in hits)
        {
            KnowledgeChunk c = hit.Chunk;
            sb.Append('[').Append(c.id).Append("] ").Append(c.sourceLabel).Append(" — ")
              .Append(c.title).AppendLine();
            sb.AppendLine(c.body);
        }
        return sb.ToString().Trim();
    }

    public static bool AreCitationsValid(IEnumerable<string> ids, IReadOnlyList<KnowledgeHit> supplied)
    {
        HashSet<string> allowed = new HashSet<string>(supplied.Select(h => h.Chunk.id), StringComparer.Ordinal);
        return ids != null && ids.All(id => !string.IsNullOrWhiteSpace(id) && allowed.Contains(id));
    }

    static bool IsEligible(KnowledgeChunk chunk, SaveData save)
    {
        if (chunk.levelIndex < 0) return true;
        switch (chunk.gate)
        {
            case KnowledgeGate.CompletedLevel: return ProgressionRules.IsCompleted(save, chunk.levelIndex);
            case KnowledgeGate.DiscoveredFact: return save != null && save.HasFact(chunk.factKey);
            default: return ProgressionRules.IsUnlocked(save, chunk.levelIndex);
        }
    }

    static IReadOnlyList<KnowledgeChunk> BuildChunks()
    {
        List<KnowledgeChunk> chunks = new List<KnowledgeChunk>();
        foreach (HeritageEntry entry in HeritageLibrary.All)
        {
            chunks.Add(new KnowledgeChunk
            {
                id = $"heritage:{entry.townKey}:overview",
                domain = KnowledgeDomain.Heritage,
                levelIndex = entry.levelIndex,
                gate = KnowledgeGate.UnlockedLevel,
                town = entry.townName,
                title = entry.theme,
                body = $"{entry.signatureSite}. {entry.beyondTheChurch}. {entry.driveSpend}",
                sourceLabel = $"Heritage Almanac — {entry.townName}",
                aliases = new[] { entry.townKey.Replace('-', ' '), entry.signatureSite },
                concepts = new[] { entry.theme, entry.codingConceptAnchor }
            });

            for (int i = 0; i < entry.keyFacts.Length; i++)
            {
                HeritageFact fact = entry.keyFacts[i];
                string factKey = entry.townKey + ":" + i;
                chunks.Add(new KnowledgeChunk
                {
                    id = $"heritage:{entry.townKey}:fact:{i}",
                    domain = KnowledgeDomain.Heritage,
                    levelIndex = entry.levelIndex,
                    gate = fact.holdForReveal ? KnowledgeGate.CompletedLevel : KnowledgeGate.DiscoveredFact,
                    factKey = factKey,
                    town = entry.townName,
                    title = fact.headline,
                    body = fact.detail,
                    sourceLabel = $"Heritage Almanac — {entry.townName}",
                    aliases = new[] { fact.headline },
                    concepts = new[] { entry.theme, entry.signatureSite }
                });
            }
        }

        foreach (JournalPageDefinition page in JournalPageLibrary.Pages)
        {
            string town = page.pageId >= 0 && page.pageId < LevelLibrary.Count ? LevelLibrary.Names[page.pageId] : "Journey";
            chunks.Add(new KnowledgeChunk
            {
                id = $"coding:{page.pageId}",
                domain = KnowledgeDomain.Coding,
                levelIndex = page.pageId,
                gate = KnowledgeGate.UnlockedLevel,
                town = town,
                title = page.codingConceptName,
                body = StripRichText(page.codingReferenceBody + "\n" + page.codeExample),
                sourceLabel = $"Coding Reference — {page.codingConceptName}",
                aliases = new[] { page.codingConceptName },
                concepts = Tokenize(page.codingConceptName).ToArray()
            });
            chunks.Add(new KnowledgeChunk
            {
                id = $"journal:{page.pageId}",
                domain = KnowledgeDomain.Heritage,
                levelIndex = page.pageId,
                gate = KnowledgeGate.CompletedLevel,
                town = town,
                title = page.heritageTitle,
                body = page.heritageBody + " " + page.artifactCardDescription,
                sourceLabel = $"Recovered Journal — {town}",
                aliases = new[] { page.heritageTitle },
                concepts = Array.Empty<string>()
            });
        }

        // Characters along the route — so the Oracle can discuss the storyline and the
        // people in it, gated to towns the player has actually reached.
        foreach (PassengerDefinition p in PassengerLibrary.All)
        {
            chunks.Add(new KnowledgeChunk
            {
                id = $"character:{p.id}",
                domain = KnowledgeDomain.Heritage,
                levelIndex = p.levelIndex,
                gate = KnowledgeGate.UnlockedLevel,
                town = p.town,
                title = $"{p.displayName} — {p.role}",
                body = $"{p.background} Voice: {p.voice} Connection to your father: {p.relationshipToFather}",
                sourceLabel = $"Story Records — {p.displayName}",
                aliases = new[] { p.displayName, p.speakerName, p.town, p.role },
                concepts = new[] { "story", "character", p.town }
            });
        }

        // The full Coding Reference — every concept the game teaches (commands, sensing,
        // conditionals, loops, functions, indentation, errors…). Always eligible
        // (levelIndex < 0): coding lessons aren't spoilers, so the Oracle can genuinely
        // teach them on request instead of deferring to the tutorial.
        for (int i = 0; i < CodingConceptLibrary.Concepts.Count; i++)
        {
            CodingConceptEntry entry = CodingConceptLibrary.Concepts[i];
            chunks.Add(new KnowledgeChunk
            {
                id = $"concept:{i}",
                domain = KnowledgeDomain.Coding,
                levelIndex = -1,
                gate = KnowledgeGate.UnlockedLevel,
                title = entry.title,
                body = StripRichText(entry.body + "\n" + entry.codeExample),
                sourceLabel = $"Coding Reference — {entry.title}",
                aliases = new[] { entry.title },
                concepts = Tokenize(entry.title).ToArray()
            });
        }

        // A small "how the game works" guide so the Oracle can answer plain gameplay
        // questions (also always eligible).
        foreach ((string id, string title, string body, string[] aliases) in GameGuide)
        {
            chunks.Add(new KnowledgeChunk
            {
                id = id,
                domain = KnowledgeDomain.Coding,
                levelIndex = -1,
                gate = KnowledgeGate.UnlockedLevel,
                title = title,
                body = body,
                sourceLabel = "Driver's Guide",
                aliases = aliases,
                concepts = new[] { "game", "how to play", "controls" }
            });
        }
        return chunks;
    }

    static HashSet<string> ExpandTerms(HashSet<string> input)
    {
        HashSet<string> expanded = new HashSet<string>(input);
        foreach (string term in input.ToArray())
            if (SemanticAliases.TryGetValue(term, out string[] aliases))
                foreach (string alias in aliases) expanded.Add(alias);
        return expanded;
    }

    static HashSet<string> Tokenize(string value)
    {
        HashSet<string> result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(value)) return result;
        foreach (Match match in Words.Matches(value.ToLowerInvariant()))
            if (match.Value.Length > 1) result.Add(match.Value);
        return result;
    }

    static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        return string.Join(" ", Words.Matches(value.ToLowerInvariant()).Cast<Match>().Select(m => m.Value));
    }

    static string StripRichText(string value)
        => Regex.Replace(value ?? "", "<[^>]+>", "");
}

/// <summary>Grounding checks shared by streamed dialogue and final responses.</summary>
public static class AiGroundingValidator
{
    static readonly Regex Numbers = new Regex(@"\b\d[\d,.]*(?:[-–]\d+)?\b", RegexOptions.Compiled);

    public static bool ValidateParaphrase(string original, string generated, string context, out string reason)
    {
        if (string.IsNullOrWhiteSpace(generated)) { reason = "empty"; return false; }
        if (generated.Length > Math.Max(420, original.Length * 2 + 80)) { reason = "too long"; return false; }
        if (generated.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries).Length > 3)
        { reason = "too many sentences"; return false; }

        HashSet<string> allowedNumbers = Extract(Numbers, (original ?? "") + " " + (context ?? ""));
        HashSet<string> outputNumbers = Extract(Numbers, generated);
        if (!outputNumbers.IsSubsetOf(allowedNumbers)) { reason = "introduced a number or date"; return false; }

        HashSet<string> requiredNumbers = Extract(Numbers, original);
        if (!requiredNumbers.IsSubsetOf(outputNumbers)) { reason = "dropped a number or date"; return false; }

        reason = null;
        return true;
    }

    public static bool ValidatePartial(string original, string generated, string context)
    {
        if (string.IsNullOrWhiteSpace(generated) || generated.Length > Math.Max(420, original.Length * 2 + 80))
            return false;
        HashSet<string> allowedNumbers = Extract(Numbers, (original ?? "") + " " + (context ?? ""));
        return Extract(Numbers, generated).IsSubsetOf(allowedNumbers);
    }

    static HashSet<string> Extract(Regex regex, string value)
        => new HashSet<string>(regex.Matches(value ?? "").Cast<Match>().Select(m => m.Value));
}
