using System;
using System.IO;
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
/// </summary>
public static class EnvConfigSync
{
    const string EnvFileName  = ".env";
    const string ConfigPath   = "Assets/Resources/ai_config.json";
    const string EnvKey       = "GEMINI_API_KEY";
    const string Placeholder  = "YOUR_GEMINI_API_KEY_HERE";

    [MenuItem("Lugarithm/Sync AI config from .env")]
    public static void SyncMenu()
    {
        if (Sync(out string message))
            Debug.Log($"[EnvConfigSync] {message}");
        else
            Debug.LogWarning($"[EnvConfigSync] {message}");
    }

    /// <summary>
    /// Reads GEMINI_API_KEY from the root .env and writes ai_config.json.
    /// Returns false (and leaves any existing config untouched) when there is no
    /// usable key, so a missing/blank .env never clobbers a working config.
    /// </summary>
    public static bool Sync(out string message)
    {
        string root    = Directory.GetParent(Application.dataPath)!.FullName;
        string envPath = Path.Combine(root, EnvFileName);

        if (!File.Exists(envPath))
        {
            message = $".env not found at {envPath} — skipped (ai_config.json left as-is).";
            return false;
        }

        string key = ReadValue(envPath, EnvKey);
        if (string.IsNullOrWhiteSpace(key) || key == Placeholder)
        {
            message = $"{EnvKey} is empty/placeholder in .env — skipped (ai_config.json left as-is).";
            return false;
        }

        string json = "{\n  \"gemini_api_key\": \"" + Escape(key) + "\"\n}\n";
        string full = Path.Combine(root, ConfigPath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, json);
        AssetDatabase.ImportAsset(ConfigPath, ImportAssetOptions.ForceUpdate);

        message = $"Wrote {ConfigPath} from .env ({EnvKey} length {key.Length}).";
        return true;
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
