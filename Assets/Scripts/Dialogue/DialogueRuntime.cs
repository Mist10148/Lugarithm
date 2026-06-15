using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Pure, Unity-free state machine for a branching dialogue conversation.
/// Drives the dialogue line-by-line, resolves hubs/branches/events, and tracks
/// which topics have been heard and how much affinity has accumulated.
/// </summary>
public class DialogueRuntime
{
    readonly DialogueConversation _conversation;
    readonly HashSet<string>      _heard = new HashSet<string>();
    readonly HashSet<string>      _visited = new HashSet<string>();
    readonly HashSet<string>      _affinityConsumed = new HashSet<string>();

    DialogueNode _currentNode;
    bool         _awaitingChoice;
    bool         _awaitingEventClear;

    public DialogueRuntime(DialogueConversation conversation)
    {
        _conversation = conversation ?? throw new ArgumentNullException(nameof(conversation));
    }

    public DialogueConversation Conversation => _conversation;
    public string               CurrentNodeId { get; private set; }
    public int                  CurrentLineIndex { get; private set; }
    public DialogueLine         Current { get; private set; }
    public int                  Affinity { get; private set; }
    public bool                 IsFinished { get; private set; }
    public DialogueEventKind    PendingEvent { get; private set; }

    /// <summary>
    /// Starts the conversation at the configured start node.
    /// </summary>
    public void Begin()
    {
        IsFinished = false;
        PendingEvent = DialogueEventKind.None;
        _awaitingChoice = false;
        _awaitingEventClear = false;
        JumpTo(_conversation.startNode);
    }

    /// <summary>
    /// Advances to the next line, or resolves the current node when exhausted.
    /// Returns true while there is still dialogue content to present.
    /// </summary>
    public bool AdvanceLine()
    {
        if (IsFinished) return false;
        if (_awaitingChoice) return true;
        if (_awaitingEventClear) return true;

        int nextIndex = CurrentLineIndex + 1;
        if (nextIndex < _currentNode.lines.Length)
        {
            EnterLine(nextIndex);
            return true;
        }

        return ResolveNodeEnd();
    }

    /// <summary>
    /// Returns the currently available choices, filtered by once-heard and
    /// requires-satisfied. Empty when not at a hub/branch.
    /// </summary>
    public IReadOnlyList<DialogueChoice> AvailableChoices()
    {
        if (!(_awaitingChoice || IsHubLike(_currentNode)))
            return Array.Empty<DialogueChoice>();

        var result = new List<DialogueChoice>();
        foreach (DialogueChoice choice in _currentNode.choices)
        {
            if (choice.once && _heard.Contains(choice.target))
                continue;

            bool satisfied = true;
            if (choice.requires != null)
            {
                foreach (string req in choice.requires)
                {
                    if (!_heard.Contains(req))
                    {
                        satisfied = false;
                        break;
                    }
                }
            }

            if (satisfied)
                result.Add(choice);
        }
        return result;
    }

    /// <summary>
    /// Selects a choice target, marks it heard, and jumps to the target node.
    /// </summary>
    public void Choose(string target)
    {
        if (_currentNode == null || _currentNode.choices == null)
            return;

        DialogueChoice choice = _currentNode.choices.FirstOrDefault(c => c.target == target);
        if (choice == null)
            return;

        _heard.Add(target);
        _awaitingChoice = false;
        JumpTo(target);
    }

    /// <summary>
    /// Clears a pending event and resolves the event node's completion routing.
    /// Call this after the drive controller has handled the gameplay beat.
    /// </summary>
    public void ClearEvent()
    {
        if (!_awaitingEventClear)
        {
            PendingEvent = DialogueEventKind.None;
            return;
        }

        PendingEvent = DialogueEventKind.None;
        _awaitingEventClear = false;
        ResolveEventNodeCompletion();
    }

    /// <summary>
    /// True if the runtime is paused waiting for an event to be cleared.
    /// </summary>
    public bool IsAwaitingEventClear => _awaitingEventClear;

    /// <summary>True if the player has already visited the given node id.</summary>
    public bool HasHeard(string nodeId) => _heard.Contains(nodeId);

    /// <summary>True if the player has already left the given node id.</summary>
    public bool HasVisited(string nodeId) => _visited.Contains(nodeId);

    /// <summary>Node ids that have been heard so far (read-only snapshot).</summary>
    public IEnumerable<string> HeardNodes => _heard.ToArray();

    /// <summary>Node ids that have been fully visited (left) at least once.</summary>
    public IEnumerable<string> VisitedNodes => _visited.ToArray();

    /// <summary>The current node being visited.</summary>
    public DialogueNode CurrentNode => _currentNode;

    // -------------------------------------------------------------------------

    void JumpTo(string nodeId)
    {
        if (!_conversation.nodes.TryGetValue(nodeId, out DialogueNode node))
        {
            IsFinished = true;
            Current = null;
            return;
        }

        // Mark the node we are leaving as visited before switching.
        if (_currentNode != null)
            _visited.Add(_currentNode.id);

        CurrentNodeId = nodeId;
        _currentNode = node;
        CurrentLineIndex = 0;
        _awaitingChoice = false;
        _awaitingEventClear = false;
        PendingEvent = DialogueEventKind.None;

        if (_currentNode.lines.Length > 0)
            EnterLine(0);
        else
            Current = null;
    }

    void EnterLine(int lineIndex)
    {
        CurrentLineIndex = lineIndex;
        Current = _currentNode.lines[lineIndex];

        string key = CurrentNodeId + ":" + lineIndex;
        if (!_affinityConsumed.Contains(key) && Current.affinity > 0)
        {
            Affinity += Current.affinity;
            _affinityConsumed.Add(key);
        }
    }

    bool ResolveNodeEnd()
    {
        switch (_currentNode.kind)
        {
            case DialogueNodeKind.Line:
                return ResolveRouting();

            case DialogueNodeKind.Event:
                PendingEvent = _currentNode.eventKind;
                _awaitingEventClear = true;
                Current = null;
                return true;

            case DialogueNodeKind.Hub:
            case DialogueNodeKind.Branch:
                _awaitingChoice = true;
                Current = null;
                return true;

            case DialogueNodeKind.Advance:
            case DialogueNodeKind.End:
                IsFinished = true;
                Current = null;
                return false;
        }

        IsFinished = true;
        Current = null;
        return false;
    }

    void ResolveEventNodeCompletion()
    {
        ResolveRouting();
    }

    bool ResolveRouting()
    {
        if (!string.IsNullOrEmpty(_currentNode.next))
        {
            JumpTo(_currentNode.next);
            return !IsFinished;
        }

        if (_currentNode.returnToHub && !string.IsNullOrEmpty(_conversation.hubNode))
        {
            JumpTo(_conversation.hubNode);
            return true;
        }

        IsFinished = true;
        Current = null;
        return false;
    }

    static bool IsHubLike(DialogueNode node)
    {
        return node != null && (node.kind == DialogueNodeKind.Hub || node.kind == DialogueNodeKind.Branch);
    }
}
