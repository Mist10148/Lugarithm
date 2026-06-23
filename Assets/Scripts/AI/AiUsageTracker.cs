using System.Collections.Generic;
using System.Text;

/// <summary>
/// Lightweight, in-memory tally of every AI call this session — per feature and per model:
/// request counts, successes/failures (bucketed by <see cref="AiErrorKind"/>), and summed
/// prompt/output tokens. <see cref="AiResult"/> already carries this data; nothing aggregated
/// it before. Fed from a single seam in <see cref="GeminiClient.Stream"/>, so every feature is
/// covered automatically. Surface it with <b>Lugarithm &gt; AI Usage Report</b>.
///
/// Main-thread only (Unity coroutines are single-threaded), so no locking. Clears on domain
/// reload — it's a development insight tool, not persisted telemetry.
/// </summary>
public static class AiUsageTracker
{
    sealed class FeatureStat
    {
        public int Requests, Successes, Failures, PromptTokens, OutputTokens;
        public readonly Dictionary<AiErrorKind, int> Errors = new Dictionary<AiErrorKind, int>();
    }

    sealed class ModelStat { public int Requests, Successes; }

    static readonly Dictionary<AiFeature, FeatureStat> _byFeature = new Dictionary<AiFeature, FeatureStat>();
    static readonly Dictionary<string, ModelStat> _byModel = new Dictionary<string, ModelStat>();

    public static void Record(AiFeature feature, AiResult result)
    {
        if (result == null) return;

        if (!_byFeature.TryGetValue(feature, out FeatureStat stat))
            _byFeature[feature] = stat = new FeatureStat();

        stat.Requests++;
        if (result.Success)
        {
            stat.Successes++;
            stat.PromptTokens += result.PromptTokens;
            stat.OutputTokens += result.OutputTokens;
        }
        else
        {
            stat.Failures++;
            stat.Errors[result.ErrorKind] = stat.Errors.TryGetValue(result.ErrorKind, out int n) ? n + 1 : 1;
        }

        if (!string.IsNullOrEmpty(result.Model))
        {
            if (!_byModel.TryGetValue(result.Model, out ModelStat ms))
                _byModel[result.Model] = ms = new ModelStat();
            ms.Requests++;
            if (result.Success) ms.Successes++;
        }
    }

    /// <summary>A formatted multi-line report of everything recorded so far.</summary>
    public static string Summary()
    {
        if (_byFeature.Count == 0) return "[AI Usage] No AI calls recorded this session.";

        var sb = new StringBuilder();
        sb.AppendLine("=== AI Usage (this session) ===");

        int totReq = 0, totOk = 0, totPrompt = 0, totOutput = 0;
        sb.AppendLine("By feature:");
        foreach (KeyValuePair<AiFeature, FeatureStat> kv in _byFeature)
        {
            FeatureStat s = kv.Value;
            totReq += s.Requests; totOk += s.Successes;
            totPrompt += s.PromptTokens; totOutput += s.OutputTokens;
            sb.Append("  ").Append(kv.Key).Append(": ")
              .Append(s.Requests).Append(" req, ")
              .Append(s.Successes).Append(" ok / ").Append(s.Failures).Append(" fail; tokens ")
              .Append(s.PromptTokens).Append(" in + ").Append(s.OutputTokens).Append(" out");
            if (s.Errors.Count > 0)
            {
                sb.Append("  [");
                bool first = true;
                foreach (KeyValuePair<AiErrorKind, int> e in s.Errors)
                {
                    if (!first) sb.Append(", ");
                    sb.Append(e.Key).Append('×').Append(e.Value);
                    first = false;
                }
                sb.Append(']');
            }
            sb.AppendLine();
        }

        sb.AppendLine("By model:");
        foreach (KeyValuePair<string, ModelStat> kv in _byModel)
            sb.Append("  ").Append(kv.Key).Append(": ")
              .Append(kv.Value.Requests).Append(" req, ")
              .Append(kv.Value.Successes).AppendLine(" ok");

        sb.Append("Totals: ").Append(totReq).Append(" requests, ").Append(totOk).Append(" ok; tokens ")
          .Append(totPrompt).Append(" in + ").Append(totOutput).Append(" out = ")
          .Append(totPrompt + totOutput).Append(" total.");
        return sb.ToString();
    }

    public static void Reset()
    {
        _byFeature.Clear();
        _byModel.Clear();
    }
}
