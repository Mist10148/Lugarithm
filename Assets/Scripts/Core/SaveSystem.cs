using UnityEngine;

/// <summary>
/// Placeholder save system using PlayerPrefs.
/// Will be expanded in a later phase with a proper save file.
/// </summary>
public static class SaveSystem
{
    private const string SAVE_EXISTS_KEY = "lugarithm_has_save";

    // -------------------------------------------------------------------------
    // Basic checks

    public static bool HasSave()
    {
        return PlayerPrefs.HasKey(SAVE_EXISTS_KEY);
    }

    public static void ClearSave()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }

    // TODO Phase 1: Replace with a proper serialized save file
    // that tracks: current town, collected journal pages,
    // inventory, best scores, and settings.
}
