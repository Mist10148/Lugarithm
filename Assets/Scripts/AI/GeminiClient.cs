using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Thin wrapper around the Gemini REST API using UnityWebRequest.
/// Reads the API key from Assets/Resources/ai_config.json at first use.
/// All calls are coroutine-based; results are delivered via callback.
/// </summary>
public static class GeminiClient
{
    const string BaseUrl =
        "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";

    static string _cachedKey;

    static string ApiKey
    {
        get
        {
            if (_cachedKey != null) return _cachedKey;
            var cfg = Resources.Load<TextAsset>("ai_config");
            if (cfg == null)
            {
                Debug.LogWarning("[GeminiClient] Resources/ai_config.json not found — Oracle disabled.");
                return _cachedKey = "";
            }
            var parsed = JsonUtility.FromJson<AiConfig>(cfg.text);
            _cachedKey = parsed?.gemini_api_key ?? "";
            return _cachedKey;
        }
    }

    /// <summary>
    /// Sends <paramref name="prompt"/> to Gemini and invokes <paramref name="onDone"/>
    /// with the model's text reply, or <c>null</c> on failure / missing key.
    /// Yield-return this from a MonoBehaviour coroutine.
    /// </summary>
    public static IEnumerator Ask(string prompt, Action<string> onDone)
    {
        if (string.IsNullOrEmpty(ApiKey))
        {
            onDone(null);
            yield break;
        }

        string url  = $"{BaseUrl}?key={ApiKey}";
        string body = BuildRequestJson(prompt);

        using var request = new UnityWebRequest(url, "POST");
        request.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = 10;

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[GeminiClient] {request.error}");
            onDone(null);
            yield break;
        }

        onDone(ParseResponseText(request.downloadHandler.text));
    }

    // -------------------------------------------------------------------------

    static string BuildRequestJson(string prompt)
    {
        string escaped = EscapeJson(prompt);
        return $"{{\"contents\":[{{\"role\":\"user\",\"parts\":[{{\"text\":\"{escaped}\"}}]}}]," +
               "\"generationConfig\":{\"maxOutputTokens\":200,\"temperature\":0.7}}";
    }

    static string ParseResponseText(string json)
    {
        // Use JsonUtility with matching model classes.
        var resp = JsonUtility.FromJson<GeminiResponse>(json);
        if (resp?.candidates == null || resp.candidates.Length == 0) return null;
        var parts = resp.candidates[0].content?.parts;
        if (parts == null || parts.Length == 0) return null;
        string text = parts[0].text?.Trim();
        return string.IsNullOrEmpty(text) ? null : text;
    }

    static string EscapeJson(string s)
    {
        var sb = new StringBuilder(s.Length + 16);
        foreach (char c in s)
        {
            switch (c)
            {
                case '"':  sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n");  break;
                case '\r': sb.Append("\\r");  break;
                case '\t': sb.Append("\\t");  break;
                default:   sb.Append(c);      break;
            }
        }
        return sb.ToString();
    }

    // -------------------------------------------------------------------------
    // JSON model classes for JsonUtility deserialization

    [Serializable] class AiConfig       { public string gemini_api_key; }
    [Serializable] class GeminiResponse { public GeminiCandidate[] candidates; }
    [Serializable] class GeminiCandidate{ public GeminiContent content; }
    [Serializable] class GeminiContent  { public GeminiPart[] parts; }
    [Serializable] class GeminiPart     { public string text; }
}
