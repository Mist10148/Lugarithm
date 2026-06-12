using System;

/// <summary>
/// Serializable data for a single town-completion badge.
/// </summary>
[Serializable]
public class BadgeDefinition
{
    public int    levelIndex;   // 0=Tutorial … 5=San Joaquin
    public string badgeName;    // e.g. "Anak ng Molo"
    public string townName;     // e.g. "Iloilo City (Molo)"
    public string description;  // one-sentence flavor shown on the unlock panel
}
