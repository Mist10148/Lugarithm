using System;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public enum AiFeature
{
    Dialogue,
    Hint,
    Oracle,
    Mentor,
    VibeCode
}

public enum AiErrorKind
{
    None,
    MissingKey,
    Cancelled,
    FirstPacketTimeout,
    Timeout,
    Network,
    Http,
    Safety,
    EmptyResponse,
    Parse
}

public sealed class AiCancellation
{
    public bool IsCancellationRequested { get; private set; }
    public void Cancel() => IsCancellationRequested = true;
}

public sealed class AiRequest
{
    public AiFeature Feature;
    public string Prompt;
    public string SystemInstruction;
    public string ResponseJsonSchema;
    public int MaxOutputTokens;
    public AiCancellation Cancellation;

    public bool WantsJson => !string.IsNullOrWhiteSpace(ResponseJsonSchema);
}

public sealed class AiResult
{
    public bool Success;
    public string Text;
    public AiErrorKind ErrorKind;
    public string Error;
    public long HttpStatus;
    public string Model;
    public int PromptTokens;
    public int OutputTokens;

    public static AiResult Failed(AiErrorKind kind, string error, string model = null, long status = 0)
        => new AiResult { ErrorKind = kind, Error = error, Model = model, HttpStatus = status };
}

public interface IAiTransport
{
    IEnumerator Send(AiRequest request, Action<string> onDelta, Action<AiResult> onDone);
}

/// <summary>
/// Runtime entry point for all Lugarithm AI features. The transport boundary keeps
/// the current direct Gemini prototype replaceable by a production relay later.
/// </summary>
public static class GeminiClient
{
    static IAiTransport _transport;

    public static IAiTransport Transport
    {
        get => _transport ??= new GeminiRestTransport();
        set => _transport = value;
    }

    public static void ResetConfigurationCache()
    {
        GeminiRestTransport.ResetApiKeyCache();
    }

    public static IEnumerator Stream(AiRequest request, Action<string> onDelta, Action<AiResult> onDone)
    {
        if (request == null)
        {
            onDone?.Invoke(AiResult.Failed(AiErrorKind.Parse, "AI request was null."));
            yield break;
        }

        yield return Transport.Send(request, onDelta, onDone);
    }

    /// <summary>Compatibility wrapper for older callers while integrations migrate.</summary>
    public static IEnumerator Ask(string prompt, Action<string> onDone)
    {
        AiResult completed = null;
        yield return Stream(new AiRequest { Feature = AiFeature.Dialogue, Prompt = prompt }, null,
            result => completed = result);
        onDone?.Invoke(completed != null && completed.Success ? completed.Text : null);
    }
}

/// <summary>Incremental parser for Gemini's server-sent-event response.</summary>
public sealed class GeminiSseParser
{
    readonly StringBuilder _pending = new StringBuilder();
    readonly Action<string> _onJson;

    public GeminiSseParser(Action<string> onJson) => _onJson = onJson;

    public void Feed(byte[] bytes, int length)
    {
        if (bytes == null || length <= 0) return;
        _pending.Append(Encoding.UTF8.GetString(bytes, 0, length));
        Drain(complete: false);
    }

    public void Complete() => Drain(complete: true);

    void Drain(bool complete)
    {
        string text = _pending.ToString();
        int consumed = 0;
        while (true)
        {
            int newline = text.IndexOf('\n', consumed);
            if (newline < 0) break;
            ProcessLine(text.Substring(consumed, newline - consumed).TrimEnd('\r'));
            consumed = newline + 1;
        }

        if (complete && consumed < text.Length)
        {
            ProcessLine(text.Substring(consumed).TrimEnd('\r'));
            consumed = text.Length;
        }

        if (consumed > 0)
        {
            _pending.Clear();
            if (consumed < text.Length) _pending.Append(text.Substring(consumed));
        }
    }

    void ProcessLine(string line)
    {
        if (!line.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return;
        string json = line.Substring(5).TrimStart();
        if (json.Length > 0 && json != "[DONE]") _onJson?.Invoke(json);
    }
}

sealed class GeminiRestTransport : IAiTransport
{
    const string ApiRoot = "https://generativelanguage.googleapis.com/v1beta/models/";
    // Back-compat defaults used only when ai_config.json carries no model_ladder.
    const string FastModel = "gemini-3.1-flash-lite";
    const string CodeModel = "gemini-3.5-flash";

    // How long a key+model combo is deprioritised after a quota/availability failure.
    const float QuotaCooldownSeconds = 90f;

    static EndpointConfig _config;
    // Last key index that produced a success — subsequent calls start here so we don't
    // re-hammer an exhausted key 1 on every request. Survives until a domain reload.
    static int _keyCursor;
    // key "#" model  ->  realtime at which the combo becomes eligible again.
    static readonly System.Collections.Generic.Dictionary<string, float> _cooldownUntil =
        new System.Collections.Generic.Dictionary<string, float>();

    static EndpointConfig Config
    {
        get
        {
            if (_config != null) return _config;
            TextAsset cfg = Resources.Load<TextAsset>("ai_config");
            return _config = EndpointConfig.Parse(cfg != null ? cfg.text : null);
        }
    }

    internal static void ResetApiKeyCache()
    {
        _config = null;
        _keyCursor = 0;
        _cooldownUntil.Clear();
    }

    public IEnumerator Send(AiRequest aiRequest, Action<string> onDelta, Action<AiResult> onDone)
    {
        EndpointConfig config = Config;
        if (config.Keys.Length == 0)
        {
            onDone?.Invoke(AiResult.Failed(AiErrorKind.MissingKey, "No usable GEMINI_API_KEY in ai_config.json."));
            yield break;
        }

        string body = BuildRequestJson(aiRequest);
        FeatureLimits limits = FeatureLimits.For(aiRequest);
        int[] ladderOrder = LadderOrder(aiRequest.Feature, config.Ladder.Length);

        // Build the full attempt order: walk keys from the cursor, and within each key
        // walk the feature's preferred model order. Combos still cooling down are tried
        // last so we always attempt everything but prefer fresh quota.
        var fresh = new System.Collections.Generic.List<(int key, int model)>();
        var cooling = new System.Collections.Generic.List<(int key, int model)>();
        float now = Time.realtimeSinceStartup;
        for (int k = 0; k < config.Keys.Length; k++)
        {
            int keyIndex = (_keyCursor + k) % config.Keys.Length;
            foreach (int modelIndex in ladderOrder)
            {
                var combo = (keyIndex, modelIndex);
                if (IsCooling(keyIndex, config.Ladder[modelIndex], now)) cooling.Add(combo);
                else fresh.Add(combo);
            }
        }
        fresh.AddRange(cooling);

        AiResult lastFailure = null;
        foreach ((int keyIndex, int modelIndex) in fresh)
        {
            if (aiRequest.Cancellation?.IsCancellationRequested == true)
            {
                onDone?.Invoke(AiResult.Failed(AiErrorKind.Cancelled, "AI request cancelled."));
                yield break;
            }

            string key = config.Keys[keyIndex];
            string model = config.Ladder[modelIndex];
            AiResult attempt = null;
            yield return Attempt(key, model, body, limits, aiRequest.Cancellation, onDelta, r => attempt = r);

            if (attempt != null && attempt.Success)
            {
                _keyCursor = keyIndex;                 // stick to the key that worked
                onDone?.Invoke(attempt);
                yield break;
            }

            lastFailure = attempt;
            if (attempt != null && IsRetryable(attempt))
            {
                if (attempt.ErrorKind == AiErrorKind.Http &&
                    (attempt.HttpStatus == 429 || attempt.HttpStatus == 403))
                    _cooldownUntil[ComboKey(keyIndex, model)] = now + QuotaCooldownSeconds;
                continue;                              // try the next key/model combo
            }

            // Terminal (safety, parse, empty, cancel) — falling back won't help.
            onDone?.Invoke(attempt ?? AiResult.Failed(AiErrorKind.Network, "AI request failed."));
            yield break;
        }

        onDone?.Invoke(lastFailure ?? AiResult.Failed(AiErrorKind.Network,
            "All AI keys and models are exhausted or cooling down."));
    }

    /// <summary>One streamed request against a single key+model. Reports the outcome
    /// (success or a classified failure) via <paramref name="onAttempt"/>.</summary>
    IEnumerator Attempt(string key, string model, string body, FeatureLimits limits,
                        AiCancellation cancellation, Action<string> onDelta, Action<AiResult> onAttempt)
    {
        string url = $"{ApiRoot}{model}:streamGenerateContent?alt=sse";
        StringBuilder combined = new StringBuilder();
        AiErrorKind parseError = AiErrorKind.None;
        int promptTokens = 0, outputTokens = 0;
        bool receivedPacket = false;

        GeminiSseParser parser = new GeminiSseParser(json =>
        {
            receivedPacket = true;
            if (!TryParseChunk(json, out string delta, out int inputCount, out int outputCount, out bool safetyBlocked))
            {
                parseError = AiErrorKind.Parse;
                return;
            }
            if (safetyBlocked)
            {
                parseError = AiErrorKind.Safety;
                return;
            }
            promptTokens = Math.Max(promptTokens, inputCount);
            outputTokens = Math.Max(outputTokens, outputCount);
            if (string.IsNullOrEmpty(delta)) return;
            combined.Append(delta);
            onDelta?.Invoke(delta);
        });

        using UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
        request.downloadHandler = new SseDownloadHandler(parser);
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("x-goog-api-key", key);

        float started = Time.realtimeSinceStartup;
        UnityWebRequestAsyncOperation operation = request.SendWebRequest();
        while (!operation.isDone)
        {
            if (cancellation?.IsCancellationRequested == true)
            {
                request.Abort();
                onAttempt?.Invoke(AiResult.Failed(AiErrorKind.Cancelled, "AI request cancelled.", model));
                yield break;
            }

            float elapsed = Time.realtimeSinceStartup - started;
            if (!receivedPacket && elapsed >= limits.FirstPacketSeconds)
            {
                request.Abort();
                onAttempt?.Invoke(AiResult.Failed(AiErrorKind.FirstPacketTimeout,
                    "AI did not respond before the first-packet deadline.", model));
                yield break;
            }
            if (elapsed >= limits.TotalSeconds)
            {
                request.Abort();
                onAttempt?.Invoke(AiResult.Failed(AiErrorKind.Timeout, "AI request timed out.", model));
                yield break;
            }
            yield return null;
        }

        parser.Complete();
        if (request.result != UnityWebRequest.Result.Success)
        {
            AiErrorKind kind = request.result == UnityWebRequest.Result.ProtocolError
                ? AiErrorKind.Http : AiErrorKind.Network;
            Debug.LogWarning($"[GeminiClient] {model}: {request.error} ({request.responseCode})");
            onAttempt?.Invoke(AiResult.Failed(kind, request.error, model, request.responseCode));
            yield break;
        }

        if (parseError != AiErrorKind.None)
        {
            onAttempt?.Invoke(AiResult.Failed(parseError,
                parseError == AiErrorKind.Safety ? "AI response was safety-blocked." : "AI stream could not be parsed.", model));
            yield break;
        }

        string text = combined.ToString().Trim();
        if (text.Length == 0)
        {
            onAttempt?.Invoke(AiResult.Failed(AiErrorKind.EmptyResponse, "AI returned no text.", model));
            yield break;
        }

        onAttempt?.Invoke(new AiResult
        {
            Success = true,
            Text = text,
            ErrorKind = AiErrorKind.None,
            Model = model,
            HttpStatus = request.responseCode,
            PromptTokens = promptTokens,
            OutputTokens = outputTokens
        });
    }

    /// <summary>A failure worth retrying on the next key/model: quota, throttling,
    /// outages, and slow/no responses. Safety blocks, parse errors, empty responses
    /// and cancellation are terminal — another model won't fix them.</summary>
    static bool IsRetryable(AiResult r)
    {
        switch (r.ErrorKind)
        {
            case AiErrorKind.Network:
            case AiErrorKind.FirstPacketTimeout:
            case AiErrorKind.Timeout:
                return true;
            case AiErrorKind.Http:
                return r.HttpStatus == 429 || r.HttpStatus == 403 || r.HttpStatus >= 500;
            default:
                return false;
        }
    }

    static bool IsCooling(int keyIndex, string model, float now)
        => _cooldownUntil.TryGetValue(ComboKey(keyIndex, model), out float until) && now < until;

    static string ComboKey(int keyIndex, string model) => keyIndex + "#" + model;

    /// <summary>Preferred order to walk the model ladder for a feature. The canonical
    /// ladder is [latest-flash, latest-lite, prev-flash, prev-lite]. Code features lead
    /// with flash; lighter features lead with lite to conserve flash quota, then fall
    /// through to flash if needed.</summary>
    static int[] LadderOrder(AiFeature feature, int ladderLen)
    {
        if (ladderLen >= 4)
            return IsCodeFeature(feature) ? new[] { 0, 1, 2, 3 } : new[] { 1, 3, 0, 2 };
        int[] order = new int[ladderLen];
        for (int i = 0; i < ladderLen; i++) order[i] = i;
        return order;
    }

    static bool IsCodeFeature(AiFeature feature)
        => feature == AiFeature.Mentor || feature == AiFeature.VibeCode;

    static string BuildRequestJson(AiRequest request)
    {
        int maxTokens = request.MaxOutputTokens > 0
            ? request.MaxOutputTokens
            : FeatureLimits.For(request).MaxOutputTokens;
        string thinking = IsCodeFeature(request.Feature) ? "medium" : "minimal";

        StringBuilder json = new StringBuilder(512 + (request.Prompt?.Length ?? 0));
        json.Append('{');
        if (!string.IsNullOrWhiteSpace(request.SystemInstruction))
        {
            json.Append("\"systemInstruction\":{\"parts\":[{\"text\":\"")
                .Append(EscapeJson(request.SystemInstruction)).Append("\"}]},");
        }
        json.Append("\"contents\":[{\"role\":\"user\",\"parts\":[{\"text\":\"")
            .Append(EscapeJson(request.Prompt ?? "")).Append("\"}]}],");
        json.Append("\"generationConfig\":{\"maxOutputTokens\":").Append(maxTokens)
            .Append(",\"thinkingConfig\":{\"thinkingLevel\":\"").Append(thinking).Append("\"}");
        if (request.WantsJson)
        {
            json.Append(",\"responseMimeType\":\"application/json\",\"responseJsonSchema\":")
                .Append(request.ResponseJsonSchema);
        }
        json.Append("}}");
        return json.ToString();
    }

    static bool TryParseChunk(string json, out string delta, out int inputTokens,
                              out int outputTokens, out bool safetyBlocked)
    {
        delta = "";
        inputTokens = outputTokens = 0;
        safetyBlocked = false;
        try
        {
            GeminiResponse response = JsonUtility.FromJson<GeminiResponse>(json);
            if (response == null) return false;
            if (response.promptFeedback != null && !string.IsNullOrEmpty(response.promptFeedback.blockReason))
                safetyBlocked = true;
            if (response.usageMetadata != null)
            {
                inputTokens = response.usageMetadata.promptTokenCount;
                outputTokens = response.usageMetadata.candidatesTokenCount;
            }
            if (response.candidates == null) return true;
            StringBuilder text = new StringBuilder();
            foreach (GeminiCandidate candidate in response.candidates)
            {
                if (candidate?.content?.parts == null) continue;
                foreach (GeminiPart part in candidate.content.parts)
                    if (part != null && !part.thought && !string.IsNullOrEmpty(part.text)) text.Append(part.text);
            }
            delta = text.ToString();
            return true;
        }
        catch
        {
            return false;
        }
    }

    static string EscapeJson(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        StringBuilder escaped = new StringBuilder(value.Length + 16);
        foreach (char c in value)
        {
            switch (c)
            {
                case '"': escaped.Append("\\\""); break;
                case '\\': escaped.Append("\\\\"); break;
                case '\n': escaped.Append("\\n"); break;
                case '\r': escaped.Append("\\r"); break;
                case '\t': escaped.Append("\\t"); break;
                default:
                    if (c < 32) escaped.Append("\\u").Append(((int)c).ToString("x4"));
                    else escaped.Append(c);
                    break;
            }
        }
        return escaped.ToString();
    }

    sealed class SseDownloadHandler : DownloadHandlerScript
    {
        readonly GeminiSseParser _parser;
        public SseDownloadHandler(GeminiSseParser parser) : base(new byte[8192]) => _parser = parser;
        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            _parser.Feed(data, dataLength);
            return true;
        }
    }

    readonly struct FeatureLimits
    {
        public readonly float FirstPacketSeconds;
        public readonly float TotalSeconds;
        public readonly int MaxOutputTokens;

        FeatureLimits(float first, float total, int max)
        {
            FirstPacketSeconds = first;
            TotalSeconds = total;
            MaxOutputTokens = max;
        }

        // Token budgets (output caps) are deliberately conservative — callers pass
        // their own MaxOutputTokens, and these are the fallback ceilings. First-packet
        // and total timeouts bound how long each attempt waits before the transport
        // falls through to the next key/model.
        public static FeatureLimits For(AiRequest request)
        {
            switch (request.Feature)
            {
                case AiFeature.Dialogue: return new FeatureLimits(3f, 6f, 120);
                case AiFeature.Hint: return new FeatureLimits(3f, 6f, 220);
                case AiFeature.Oracle: return new FeatureLimits(3f, 12f, 320);
                case AiFeature.Mentor: return new FeatureLimits(5f, 18f, 800);
                case AiFeature.VibeCode: return new FeatureLimits(5f, 15f, 900);
                default: return new FeatureLimits(3f, 10f, 300);
            }
        }
    }

    [Serializable] sealed class AiConfig
    {
        public string gemini_api_key;
        public string[] gemini_api_keys;
        public string[] model_ladder;
    }

    /// <summary>Resolved, validated endpoint settings: the usable keys (in order) and
    /// the model fallback ladder. Built once from ai_config.json and cached.</summary>
    sealed class EndpointConfig
    {
        public string[] Keys = Array.Empty<string>();
        public string[] Ladder = { CodeModel, FastModel };

        public static EndpointConfig Parse(string jsonText)
        {
            var config = new EndpointConfig();
            if (string.IsNullOrWhiteSpace(jsonText)) return config;

            AiConfig parsed;
            try { parsed = JsonUtility.FromJson<AiConfig>(jsonText); }
            catch { return config; }
            if (parsed == null) return config;

            var keys = new System.Collections.Generic.List<string>();
            void Add(string raw)
            {
                if (string.IsNullOrWhiteSpace(raw)) return;
                string k = raw.Trim();
                if (k.Contains("YOUR_") || k.Contains("PLACEHOLDER")) return;
                if (!keys.Contains(k)) keys.Add(k);
            }
            if (parsed.gemini_api_keys != null)
                foreach (string k in parsed.gemini_api_keys) Add(k);
            Add(parsed.gemini_api_key);   // back-compat / slot-1 alias
            config.Keys = keys.ToArray();

            if (parsed.model_ladder != null)
            {
                string[] ladder = parsed.model_ladder
                    .Where(m => !string.IsNullOrWhiteSpace(m))
                    .Select(m => m.Trim())
                    .ToArray();
                if (ladder.Length > 0) config.Ladder = ladder;
            }
            return config;
        }
    }
    [Serializable] sealed class GeminiResponse
    {
        public GeminiCandidate[] candidates;
        public GeminiPromptFeedback promptFeedback;
        public GeminiUsage usageMetadata;
    }
    [Serializable] sealed class GeminiCandidate { public GeminiContent content; }
    [Serializable] sealed class GeminiContent { public GeminiPart[] parts; }
    [Serializable] sealed class GeminiPart { public string text; public bool thought; }
    [Serializable] sealed class GeminiPromptFeedback { public string blockReason; }
    [Serializable] sealed class GeminiUsage
    {
        public int promptTokenCount;
        public int candidatesTokenCount;
    }
}
