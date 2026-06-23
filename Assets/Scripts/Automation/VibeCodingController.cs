using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// In-window AI agent for the CODE editor — a Copilot-style helper with three modes:
/// <b>Ask</b> (read-only tutor), <b>Plan</b> (numbered approach, no edits), and
/// <b>Agent</b> (generates a structured action graph that is compiled and dropped into
/// the editor for the player to RUN). The agent sees the maze, the jeepney's state, and
/// the current editor contents. The title-bar "AI" button swaps the editor body for the
/// chat; "Code" swaps back. Self-wires in Awake so it works even where <see cref="Init"/>
/// is never called.
/// </summary>
public class VibeCodingController : MonoBehaviour
{
    [Header("Chat")]
    [SerializeField] TMP_InputField      chatInput;
    [SerializeField] Button              sendButton;
    [SerializeField] CodeEditorController codeEditor;

    [Header("Transcript")]
    [SerializeField] RectTransform chatContent;     // bubble container (preferred)
    [SerializeField] TMP_Text      bubbleTemplate;  // inactive TMP template for bubbles
    [SerializeField] TMP_Text      historyLabel;    // legacy/intro line (optional)

    [Header("Modes (Ask / Plan / Agent)")]
    [SerializeField] Button askButton;
    [SerializeField] Button planButton;
    [SerializeField] Button agentButton;

    [Header("View swap (AI ⇄ Code)")]
    [SerializeField] GameObject editorBody;
    [SerializeField] GameObject chatBody;
    [SerializeField] Button     aiButton;
    [SerializeField] Button     codeButton;

    static readonly Color PlayerBubble = new Color(0.95f, 0.65f, 0.15f, 0.95f);
    static readonly Color PlayerText   = new Color(0.10f, 0.09f, 0.06f, 1f);
    static readonly Color AiBubble     = new Color(0.16f, 0.18f, 0.22f, 0.98f);
    static readonly Color AiText       = new Color(0.93f, 0.93f, 0.88f, 1f);
    static readonly Color ModeActive   = new Color(0.30f, 0.45f, 0.75f, 1f);
    static readonly Color ModeIdle     = new Color(0.18f, 0.22f, 0.30f, 1f);

    string[] _allowedBlocks;
    string[] _allowedQueries;
    readonly List<GameObject> _rows = new List<GameObject>();
    VibeMode _mode = VibeMode.Agent;

    // Live world context (references, so we always read the current state).
    GridModel                  _grid;
    AgentSim                   _sim;
    AutomationPuzzleDefinition _def;

    bool _wired;

    void Awake()
    {
        Wire();
        ShowEditor();
        SetMode(_mode);
    }

    void Wire()
    {
        if (_wired) return;
        _wired = true;

        if (sendButton  != null) sendButton.onClick.AddListener(OnSend);
        if (chatInput   != null) chatInput.onSubmit.AddListener(_ => OnSend());
        if (aiButton    != null) aiButton.onClick.AddListener(ShowChat);
        if (codeButton  != null) codeButton.onClick.AddListener(ShowEditor);
        if (askButton   != null) askButton.onClick.AddListener(() => SetMode(VibeMode.Ask));
        if (planButton  != null) planButton.onClick.AddListener(() => SetMode(VibeMode.Plan));
        if (agentButton != null) agentButton.onClick.AddListener(() => SetMode(VibeMode.Agent));

        ChatBubbleFactory.PrepareContent(chatContent);
    }

    /// <summary>Constrains generated code to the level's vocabulary and (re)binds the
    /// editor. Optional — the chat already works from the serialized wiring.</summary>
    public void Init(string[] allowedBlocks, string[] allowedQueries, CodeEditorController editor)
    {
        _allowedBlocks  = allowedBlocks;
        _allowedQueries = allowedQueries;
        if (editor != null) codeEditor = editor;
        Wire();
    }

    /// <summary>Gives the agent live access to the maze + jeepney so it can read the
    /// world. Call from the host (automation drive or maze minigame) at setup.</summary>
    public void SetWorldContext(GridModel grid, AgentSim sim, AutomationPuzzleDefinition def)
    {
        _grid = grid;
        _sim  = sim;
        _def  = def;
    }

    // -------------------------------------------------------------------------
    // Modes

    void SetMode(VibeMode mode)
    {
        _mode = mode;
        Tint(askButton,   mode == VibeMode.Ask);
        Tint(planButton,  mode == VibeMode.Plan);
        Tint(agentButton, mode == VibeMode.Agent);
    }

    void Tint(Button button, bool active)
    {
        if (button != null && button.image != null)
            button.image.color = active ? ModeActive : ModeIdle;
    }

    // -------------------------------------------------------------------------
    // View swap

    public void ShowChat()
    {
        if (chatBody   != null) chatBody.SetActive(true);
        if (editorBody != null) editorBody.SetActive(false);
        if (chatInput  != null) { chatInput.Select(); chatInput.ActivateInputField(); }
    }

    public void ShowEditor()
    {
        if (chatBody   != null) chatBody.SetActive(false);
        if (editorBody != null) editorBody.SetActive(true);
    }

    // -------------------------------------------------------------------------
    // Chat

    void OnSend()
    {
        if (chatInput == null) return;
        string text = chatInput.text;
        if (string.IsNullOrWhiteSpace(text)) return;

        chatInput.text = "";
        SetEnabled(false);
        StartCoroutine(Respond(text.Trim()));
    }

    IEnumerator Respond(string message)
    {
        AddBubble(message, player: true);
        TMP_Text reply = AddBubble("…", player: false);

        bool blockMode = SaveSystem.Current != null && SaveSystem.Current.settings != null &&
                         SaveSystem.Current.settings.blockMode;
        string editorText = codeEditor != null && codeEditor.input != null ? codeEditor.input.text : "";
        string world = VibeCodingService.BuildWorldContext(_grid, _sim, _def, blockMode, editorText,
                                                           _allowedBlocks, _allowedQueries);

        if (_mode == VibeMode.Agent)
            yield return RunAgent(message, world, reply);
        else
            yield return RunTextMode(message, world, reply);

        SetEnabled(true);
        if (chatInput != null) chatInput.ActivateInputField();
    }

    /// <summary>Ask / Plan: a plain-text answer, no edits.</summary>
    IEnumerator RunTextMode(string message, string world, TMP_Text reply)
    {
        AiResult result = null;
        yield return GeminiClient.Stream(VibeCodingService.BuildAgentRequest(_mode, message, world),
            null, completed => result = completed);

        UpdateBubble(reply, result != null && result.Success && !string.IsNullOrWhiteSpace(result.Text)
            ? result.Text.Trim()
            : "(couldn't reach the AI — check your connection and try again)");
    }

    /// <summary>Agent: generate an action graph, compile + validate it, and place the
    /// resulting code into the editor. Write-only — the player presses RUN.</summary>
    IEnumerator RunAgent(string message, string world, TMP_Text reply)
    {
        AiResult result = null;
        yield return GeminiClient.Stream(VibeCodingService.BuildAgentRequest(VibeMode.Agent, message, world),
            null, completed => result = completed);

        if (result == null || !result.Success ||
            !ActionGraphCompiler.TryParse(result.Text, out ActionGraphResponse graph))
        {
            UpdateBubble(reply, "(couldn't reach the AI — check your connection and try again)");
            yield break;
        }

        string code = null;
        if (ActionGraphCompiler.TryCompile(graph, out string compiled, out string compileError) &&
            VibeCodingService.Validate(compiled, _allowedBlocks, _allowedQueries, out _) == null)
        {
            code = compiled;
        }
        else
        {
            // One repair round, sending only the error back.
            UpdateBubble(reply, (graph.message ?? "Let me fix that…").Trim() + "  (checking one correction…)");
            string err = compileError ?? "the program used something that isn't unlocked here";
            AiResult repair = null;
            yield return GeminiClient.Stream(
                VibeCodingService.BuildAgentRepairRequest(message, world, err),
                null, completed => repair = completed);

            if (repair != null && repair.Success &&
                ActionGraphCompiler.TryParse(repair.Text, out ActionGraphResponse fixedGraph) &&
                ActionGraphCompiler.TryCompile(fixedGraph, out string recompiled, out _) &&
                VibeCodingService.Validate(recompiled, _allowedBlocks, _allowedQueries, out _) == null)
            {
                code = recompiled;
                graph = fixedGraph;
            }
        }

        if (code != null && codeEditor != null && codeEditor.input != null)
        {
            codeEditor.input.SetTextWithoutNotify(code);
            codeEditor.RefreshLineNumbers();
            codeEditor.RefreshHighlight();
            UpdateBubble(reply, (graph.message ?? "Here's a program for you.").Trim() +
                "\n✓ Placed in your editor. Press Code to review it, then RUN when you're ready.");
        }
        else
        {
            UpdateBubble(reply, "I couldn't make a program that fits this level's rules, so I left your code " +
                "unchanged. Try asking in Plan mode for the approach.");
        }
    }

    // -------------------------------------------------------------------------
    // Transcript

    TMP_Text AddBubble(string text, bool player)
    {
        if (chatContent != null && bubbleTemplate != null)
        {
            TMP_Text label = ChatBubbleFactory.Add(chatContent, bubbleTemplate, text, player,
                player ? PlayerBubble : AiBubble, player ? PlayerText : AiText, out GameObject row);
            if (row != null) _rows.Add(row);
            ChatBubbleFactory.ScrollToBottom(chatContent);
            return label;
        }

        // Fallback: append to the legacy single label if no bubble container is wired.
        if (historyLabel != null)
            historyLabel.text += (historyLabel.text.Length > 0 ? "\n" : "") + (player ? "You: " : "AI: ") + text;
        return historyLabel;
    }

    void UpdateBubble(TMP_Text bubble, string text)
    {
        if (chatContent != null && bubbleTemplate != null)
        {
            ChatBubbleFactory.SetText(bubble, text, chatContent);
            ChatBubbleFactory.ScrollToBottom(chatContent);
        }
        else if (bubble != null)
        {
            bubble.text = text;
        }
    }

    void SetEnabled(bool on)
    {
        if (chatInput  != null) chatInput.interactable  = on;
        if (sendButton != null) sendButton.interactable = on;
    }
}
