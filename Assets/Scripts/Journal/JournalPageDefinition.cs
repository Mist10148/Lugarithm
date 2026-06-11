using System;

/// <summary>
/// One recoverable journal page: a heritage narrative written in the father's
/// voice, a short artifact card, and the coding concept reference for that town.
/// </summary>
[Serializable]
public class JournalPageDefinition
{
    /// <summary>Matches levelIndex: 0 = Tutorial … 5 = San Joaquin.</summary>
    public int pageId;

    /// <summary>Title of the heritage entry.</summary>
    public string heritageTitle;

    /// <summary>Father's recovered writing for this leg of the trip.</summary>
    public string heritageBody;

    /// <summary>Short artifact blurb shown alongside the heritage page.</summary>
    public string artifactCardDescription;

    /// <summary>Name of the coding concept taught in this town.</summary>
    public string codingConceptName;

    /// <summary>Plain-text explanation of the coding concept.</summary>
    public string codingReferenceBody;

    /// <summary>Rich-text code snippet using TMP tags (e.g. &lt;mspace&gt;).</summary>
    public string codeExample;
}
