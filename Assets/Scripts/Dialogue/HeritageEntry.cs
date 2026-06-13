using System;

/// <summary>
/// A town along the route and the heritage the passenger shares (or withholds) there.
/// </summary>
[Serializable]
public class HeritageEntry
{
    public string townKey;
    public int    levelIndex;           // matches LevelLibrary; Guimbal = -1
    public string townName;
    public string theme;
    public string gameRole;
    public string eraTouched;
    public string signatureSite;
    public string beyondTheChurch;
    public string mood;
    public string codingConceptAnchor;

    public HeritageFact[] keyFacts;

    /// <summary>What the passenger spends on the drive (texture).</summary>
    public string driveSpend;

    /// <summary>What is held back for the completion cutscene (payoff).</summary>
    public string reveal;

    public string[] sources;
}
