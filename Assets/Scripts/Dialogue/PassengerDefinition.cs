using System;

/// <summary>
/// A speaking passenger or guide for one leg of the journey.
/// </summary>
[Serializable]
public class PassengerDefinition
{
    public string id;
    public int    levelIndex;        // 0..5; Guimbal interlude uses -1
    public string displayName;       // e.g. "Ate Gemma"
    public string speakerName;       // e.g. "Gemma" (used in dialogue boxes)
    public string town;
    public string role;
    public string background;
    public string voice;
    public string relationshipToFather;
}
