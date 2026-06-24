using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Bridges the repo-root <c>.env</c> (the source of truth for secrets, gitignored)
/// into <c>Assets/Resources/ai_config.json</c>, which the runtime
/// <see cref="GeminiClient"/> loads via <c>Resources.Load</c>. A built player can't
/// read a root .env at runtime, so we generate the Resources file here — from a
/// menu item and automatically before every player build.
///
/// Supports up to five free-plan keys (<c>GEMINI_API_KEY_1</c>…<c>GEMINI_API_KEY_5</c>;
/// plain <c>GEMINI_API_KEY</c> is an alias for slot 1) so the runtime can fall back
/// across accounts. An optional <c>GEMINI_MODEL_LADDER</c> (comma-separated) defines
/// the per-key model fallback order; when absent we preserve any ladder already in
/// ai_config.json so hand-edited model IDs are never clobbered, falling back to
/// placeholders only on a first write.
/// </summary>
public static class EnvConfigSync
{
    const string EnvFileName  = ".env";
    const string ConfigPath   = "Assets/Resources/ai_config.json";
    const string EnvKey       = "GEMINI_API_KEY";
    const string LadderKey    = "GEMINI_MODEL_LADDER";
    const string Placeholder  = "YOUR_GEMINI_API_KEY_HERE";
    const int    MaxKeys      = 5;

    // Default per-key model fallback order, walked in this exact order for every key
    // before advancing to the next key. Override the whole list via GEMINI_MODEL_LADDER
    // in .env, or by editing ai_config.json directly.
    static readonly string[] DefaultLadder =
    {
        "gemini-3.5-flash",
        "gemini-3.1-flash-lite",
        "gemini-2.5-flash",
        "gemini-2.5-flash-lite",
        "gemini-3.0-flash"
    };

    [MenuItem("Lugarithm/Sync AI config from .env")]
    public static void SyncMenu()
    {
        if (Sync(out string message))
            Debug.Log($"[EnvConfigSync] {message}");
        else
            Debug.LogWarning($"[EnvConfigSync] {message}");
    }

    /// <summary>
    /// Reads the Gemini key(s) and optional model ladder from the root .env and
    /// writes ai_config.json. Returns false (and leaves any existing config
    /// untouched) when there is no usable key, so a missing/blank .env never
    /// clobbers a working config.
    /// </summary>
    public static bool Sync(out string message)
    {
        string root    = Directory.GetParent(Application.dataPath)!.FullName;
        string envPath = Path.Combine(root, EnvFileName);
        string full    = Path.Combine(root, ConfigPath);

        if (!File.Exists(envPath))
        {
            message = $".env not found at {envPath} — skipped (ai_config.json left as-is).";
            return false;
        }

        List<string> keys = ReadKeys(envPath);
        if (keys.Count == 0)
        {
            message = $"{EnvKey}/{EnvKey}_1..{MaxKeys} are empty/placeholder in .env — skipped (ai_config.json left as-is).";
            return false;
        }

        string[] ladder = ResolveLadder(envPath, full);

        string json = BuildConfigJson(keys, ladder);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, json);
        AssetDatabase.ImportAsset(ConfigPath, ImportAssetOptions.ForceUpdate);
        GeminiClient.ResetConfigurationCache();

        bool defaultLadder = ladder.SequenceEqual(DefaultLadder);
        message = $"Wrote {ConfigPath} from .env ({keys.Count} key(s), ladder: {string.Join("/", ladder)})." +
                  (defaultLadder
                      ? "  Using default model ladder — add previous-generation IDs via GEMINI_MODEL_LADDER in .env or by editing ai_config.json."
                      : "");
        return true;
    }

    /// <summary>Collects GEMINI_API_KEY_1..5 (with plain GEMINI_API_KEY as the slot-1
    /// alias), de-duplicated and stripped of empties/placeholders.</summary>
    static List<string> ReadKeys(string envPath)
    {
        List<string> keys = new List<string>();
        void Consider(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || raw == Placeholder) return;
            string trimmed = raw.Trim();
            if (!keys.Contains(trimmed)) keys.Add(trimmed);
        }

        // Slot 1 may come from GEMINI_API_KEY or GEMINI_API_KEY_1.
        Consider(ReadValue(envPath, EnvKey));
        for (int i = 1; i <= MaxKeys; i++)
            Consider(ReadValue(envPath, $"{EnvKey}_{i}"));
        return keys;
    }

    /// <summary>Model fallback order: GEMINI_MODEL_LADDER in .env wins; otherwise reuse
    /// the ladder already in ai_config.json; otherwise placeholder defaults.</summary>
    static string[] ResolveLadder(string envPath, string configPath)
    {
        string fromEnv = ReadValue(envPath, LadderKey);
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            string[] parsed = fromEnv.Split(',')
                                     .Select(s => s.Trim())
                                     .Where(s => s.Length > 0)
                                     .ToArray();
            if (parsed.Length > 0) return parsed;
        }

        string[] existing = ReadExistingLadder(configPath);
        if (existing != null && existing.Length > 0) return existing;

        return DefaultLadder;
    }

    static string[] ReadExistingLadder(string configPath)
    {
        if (!File.Exists(configPath)) return null;
        try
        {
            string text = File.ReadAllText(configPath);
            Match m = Regex.Match(text, "\"model_ladder\"\\s*:\\s*\\[(.*?)\\]", RegexOptions.Singleline);
            if (!m.Success) return null;
            string[] ids = Regex.Matches(m.Groups[1].Value, "\"((?:[^\"\\\\]|\\\\.)*)\"")
                                 .Cast<Match>()
                                 .Select(x => x.Groups[1].Value)
                                 .Where(s => s.Length > 0)
                                 .ToArray();
            return ids.Length > 0 ? ids : null;
        }
        catch { return null; }
    }

    static string BuildConfigJson(List<string> keys, string[] ladder)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("{\n");
        // Back-compat: first key under the original single-key field.
        sb.Append("  \"gemini_api_key\": \"").Append(Escape(keys[0])).Append("\",\n");
        sb.Append("  \"gemini_api_keys\": [\n");
        for (int i = 0; i < keys.Count; i++)
            sb.Append("    \"").Append(Escape(keys[i])).Append(i < keys.Count - 1 ? "\",\n" : "\"\n");
        sb.Append("  ],\n");
        sb.Append("  \"model_ladder\": [\n");
        for (int i = 0; i < ladder.Length; i++)
            sb.Append("    \"").Append(Escape(ladder[i])).Append(i < ladder.Length - 1 ? "\",\n" : "\"\n");
        sb.Append("  ]\n");
        sb.Append("}\n");
        return sb.ToString();
    }

    /// <summary>Parses KEY=VALUE from a .env, honoring '#' comments, optional 'export', and quotes.</summary>
    static string ReadValue(string path, string wantedKey)
    {
        foreach (string raw in File.ReadAllLines(path))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#")) continue;
            if (line.StartsWith("export ")) line = line.Substring(7).TrimStart();

            int eq = line.IndexOf('=');
            if (eq <= 0) continue;

            string k = line.Substring(0, eq).Trim();
            if (!string.Equals(k, wantedKey, StringComparison.Ordinal)) continue;

            string v = line.Substring(eq + 1).Trim();
            if (v.Length >= 2 && ((v[0] == '"' && v[^1] == '"') || (v[0] == '\'' && v[^1] == '\'')))
                v = v.Substring(1, v.Length - 2);
            return v;
        }
        return null;
    }

    static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    /// <summary>Refresh ai_config.json from .env right before a player build.</summary>
    public class BuildHook : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (Sync(out string message))
                Debug.Log($"[EnvConfigSync] {message}");
            else
                Debug.LogWarning($"[EnvConfigSync] {message} (build will use whatever ai_config.json already contains)");
        }
    }
}

/// <summary>
/// Watches the repo-root .env while the Unity editor is open. Polling is used
/// instead of FileSystemWatcher because it behaves consistently across Unity
/// domain reloads and common Windows editors.
/// </summary>
[InitializeOnLoad]
public static class EnvConfigAutoSync
{
    static DateTime _lastWriteUtc = DateTime.MinValue;
    static double _nextPoll;

    static EnvConfigAutoSync()
    {
        EditorApplication.update += Poll;
        _nextPoll = EditorApplication.timeSinceStartup + 0.25d;
    }

    static void Poll()
    {
        if (EditorApplication.timeSinceStartup < _nextPoll) return;
        _nextPoll = EditorApplication.timeSinceStartup + 1d;

        string root = Directory.GetParent(Application.dataPath)!.FullName;
        string path = Path.Combine(root, ".env");
        if (!File.Exists(path)) return;

        DateTime write = File.GetLastWriteTimeUtc(path);
        if (write == _lastWriteUtc) return;
        _lastWriteUtc = write;

        if (EnvConfigSync.Sync(out string message))
            Debug.Log($"[EnvConfigSync] Auto-synced: {message}");
    }
}
