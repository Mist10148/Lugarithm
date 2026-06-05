using System.IO;
using UnityEngine;

/// <summary>
/// Local JSON save system. Serializes a single <see cref="SaveData"/> instance
/// to Application.persistentDataPath. Settings live in the same file but persist
/// independently of run progress (see <see cref="StartNewRun"/>).
/// </summary>
public static class SaveSystem
{
    private const string FILE_NAME = "save.json";

    private static string FilePath => Path.Combine(Application.persistentDataPath, FILE_NAME);

    /// <summary>In-memory save state. Always non-null, even before <see cref="Load"/>.</summary>
    public static SaveData Current { get; private set; } = new SaveData();

    // -------------------------------------------------------------------------
    // Load / Save

    /// <summary>
    /// Loads the save file into <see cref="Current"/>. If no file exists (or it
    /// is corrupt), falls back to a fresh <see cref="SaveData"/> so settings
    /// always have a home.
    /// </summary>
    public static void Load()
    {
        if (!File.Exists(FilePath))
        {
            Current = new SaveData();
            return;
        }

        try
        {
            string json = File.ReadAllText(FilePath);
            Current = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSystem] Failed to read save, starting fresh: {e.Message}");
            Current = new SaveData();
        }
    }

    /// <summary>Writes <see cref="Current"/> to disk as JSON.</summary>
    public static void Save()
    {
        try
        {
            string json = JsonUtility.ToJson(Current, prettyPrint: true);
            File.WriteAllText(FilePath, json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSystem] Failed to write save: {e.Message}");
        }
    }

    // -------------------------------------------------------------------------
    // Run lifecycle

    /// <summary>True if a game is in progress (drives the Continue button).</summary>
    public static bool HasSave()
    {
        return Current != null && Current.hasActiveRun;
    }

    /// <summary>
    /// Begins a fresh run: resets progress but KEEPS the player's settings,
    /// then saves. Use this for "New Game" instead of wiping the whole file.
    /// </summary>
    public static void StartNewRun()
    {
        GameSettings keep = Current != null ? Current.settings : new GameSettings();

        Current = new SaveData
        {
            hasActiveRun     = true,
            currentTownIndex = 0,
            settings         = keep
        };

        Save();
    }

    /// <summary>Convenience hook for the "auto-save on town completion" task.</summary>
    public static void AutoSave()
    {
        Save();
    }

    /// <summary>Deletes the save file and resets in-memory state (full wipe).</summary>
    public static void DeleteSave()
    {
        if (File.Exists(FilePath))
            File.Delete(FilePath);

        Current = new SaveData();
    }
}
