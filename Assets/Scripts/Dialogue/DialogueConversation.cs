using System;
using System.Collections.Generic;

/// <summary>
/// A complete branching conversation for one level: boarding beat, topic hub,
/// assist hints, and the held-back completion reveal.
/// </summary>
[Serializable]
public class DialogueConversation
{
    public int levelIndex;
    public string passengerId;
    public string startNode;
    public string hubNode;
    public Dictionary<string, DialogueNode> nodes = new Dictionary<string, DialogueNode>();
    public DialogueLine[] assistHints = Array.Empty<DialogueLine>();
    public DialogueLine[] revealLines = Array.Empty<DialogueLine>();
    public int journalPageId;

    /// <summary>
    /// Checks that all jump targets exist, reveal lines are flagged, and
    /// non-reveal lines do not contain reveal text. Returns a human-readable
    /// error or null when valid.
    /// </summary>
    public string Validate()
    {
        if (string.IsNullOrEmpty(startNode))
            return "startNode is empty";
        if (!nodes.ContainsKey(startNode))
            return $"startNode '{startNode}' not found";
        if (!string.IsNullOrEmpty(hubNode) && !nodes.ContainsKey(hubNode))
            return $"hubNode '{hubNode}' not found";

        foreach (var kvp in nodes)
        {
            DialogueNode node = kvp.Value;
            if (node == null)
                return $"node '{kvp.Key}' is null";
            if (node.id != kvp.Key)
                return $"node dictionary key '{kvp.Key}' does not match node.id '{node.id}'";

            if (node.choices != null)
            {
                foreach (DialogueChoice choice in node.choices)
                {
                    if (choice == null)
                        return $"node '{node.id}' has a null choice";
                    if (string.IsNullOrEmpty(choice.target))
                        return $"node '{node.id}' has a choice with empty target";
                    if (!nodes.ContainsKey(choice.target))
                        return $"node '{node.id}' choice targets missing node '{choice.target}'";
                    if (choice.requires != null)
                    {
                        foreach (string req in choice.requires)
                        {
                            if (string.IsNullOrEmpty(req))
                                return $"node '{node.id}' choice has empty requires entry";
                            if (!nodes.ContainsKey(req))
                                return $"node '{node.id}' choice requires missing node '{req}'";
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(node.next) && !nodes.ContainsKey(node.next))
                return $"node '{node.id}' next targets missing node '{node.next}'";

            if (node.lines != null)
            {
                foreach (DialogueLine line in node.lines)
                {
                    if (line == null)
                        return $"node '{node.id}' has a null line";
                    if (node.kind != DialogueNodeKind.End && line.isReveal)
                        return $"node '{node.id}' contains a reveal line but is not an End/reveal node";
                }
            }
        }

        foreach (DialogueLine line in revealLines)
        {
            if (line == null)
                return "revealLines contains a null entry";
            if (!line.isReveal)
                return $"reveal line by '{line.speaker}' is not flagged isReveal";
        }

        return null;
    }
}
