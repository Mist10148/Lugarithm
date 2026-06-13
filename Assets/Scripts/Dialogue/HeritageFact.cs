using System;

/// <summary>
/// One citable fact about a town, drawn from the research dossier's Lore Book set.
/// </summary>
[Serializable]
public class HeritageFact
{
    public string headline;
    public string detail;
    public bool   holdForReveal;
}
