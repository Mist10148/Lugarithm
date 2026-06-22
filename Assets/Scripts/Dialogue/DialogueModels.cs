using System;

/// <summary>
/// Gameplay beats a dialogue node can raise. The drive controller listens and
/// pauses/resumes the conversation around the matching tutorial or breakdown.
/// </summary>
public enum DialogueEventKind
{
    None,
    DrivingTutorial,
    FareTutorial,
    Breakdown,
    Maintenance,
    Arrive,
    Advance,
    TutorialComplete,
    Continue,
    TutorialRepair,   // scripted tutorial drill: code-based CodeFixMinigame (engine fault)
    TutorialRefuel    // scripted tutorial drill: non-code RefuelMinigame
}

/// <summary>
/// What kind of state a dialogue node represents.
/// </summary>
public enum DialogueNodeKind
{
    Line,    // one or more spoken lines, then next / returnToHub
    Hub,     // re-selectable topic menu; waits for player choice
    Branch,  // a short branch that ends back at the hub
    Event,   // spoken line(s) followed by a gameplay event
    Advance, // end-of-leg advance gate
    End      // terminal node
}

public enum DialogueTone
{
    Neutral,
    Warm,
    Curious,
    Dismissive
}

/// <summary>
/// A single spoken line with optional affinity and reveal gating.
/// </summary>
[Serializable]
public class DialogueLine
{
    public string speaker;
    public string text;
    public int    affinity;
    public bool   isReveal;
}

/// <summary>
/// A player intent choice leading to another node.
/// </summary>
[Serializable]
public class DialogueChoice
{
    public string label;
    public string target;
    public bool   once;
    public string[] requires;
    public DialogueEventKind unlocksEvent;
    public DialogueTone tone;
    public int affinityDelta;
}

/// <summary>
/// One node in a branching conversation: lines, choices, routing, and events.
/// </summary>
[Serializable]
public class DialogueNode
{
    public string id;
    public DialogueNodeKind kind;
    public DialogueLine[] lines;
    public DialogueChoice[] choices;
    public string next;
    public bool returnToHub;
    public DialogueEventKind eventKind;
    public string eventPayload;
}
