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
    [SerializeField] BlockCanvasController blockCanvas;   // block-mode target (optional)

    [Header("Transcript")]
    [SerializeField] RectTransform chatContent;     // bubble container (preferred)
    [SerializeField] TMP_Text      bubbleTemplate;  // inactive TMP template for bubbles
    [SerializeField] TMP_Text      historyLabel;    // legacy/intro line (optional)

    [Header("Modes (Auto / Ask / Plan / Agent / Refactor)")]
    [SerializeField] Button autoButton;
    [SerializeField] Button askButton;
    [SerializeField] Button planButton;
    [SerializeField] Button agentButton;
    [SerializeField] Button refactorButton;

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
    VibeMode _mode = VibeMode.Auto;
    string   _stashedCode;   // the program text before the last refactor, for a one-tap "undo"
    bool     _blockMode;     // set per request: are we editing blocks (vs text) this turn?

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
        if (autoButton     != null) autoButton.onClick.AddListener(() => SetMode(VibeMode.Auto));
        if (askButton      != null) askButton.onClick.AddListener(() => SetMode(VibeMode.Ask));
        if (planButton     != null) planButton.onClick.AddListener(() => SetMode(VibeMode.Plan));
        if (agentButton    != null) agentButton.onClick.AddListener(() => SetMode(VibeMode.Agent));
        if (refactorButton != null) refactorButton.onClick.AddListener(() => SetMode(VibeMode.Refactor));

        ChatBubbleFactory.PrepareContent(chatContent);
    }

    /// <summary>Constrains generated code to the level's vocabulary and (re)binds the
    /// editor. Optional — the chat already works from the serialized wiring.</summary>
    public void Init(string[] allowedBlocks, string[] allowedQueries, CodeEditorController editor,
                     BlockCanvasController blocks = null)
    {
        _allowedBlocks  = allowedBlocks;
        _allowedQueries = allowedQueries;
        if (editor != null) codeEditor = editor;
        if (blocks != null) blockCanvas = blocks;
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
        Tint(autoButton,     mode == VibeMode.Auto);
        Tint(askButton,      mode == VibeMode.Ask);
        Tint(planButton,     mode == VibeMode.Plan);
        Tint(agentButton,    mode == VibeMode.Agent);
        Tint(refactorButton, mode == VibeMode.Refactor);
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

        _blockMode = SaveSystem.Current != null && SaveSystem.Current.settings != null &&
                     SaveSystem.Current.settings.blockMode;

        // In block mode the player's program lives on the canvas, not the code editor — read
        // it as source so the AI sees (and can rewrite) the actual blocks.
        string currentSource = _blockMode && blockCanvas != null
            ? blockCanvas.ToSourceText()
            : (codeEditor != null && codeEditor.input != null ? codeEditor.input.text : "");

        string world = VibeCodingService.BuildWorldContext(_grid, _sim, _def, _blockMode, currentSource,
                                                           _allowedBlocks, _allowedQueries);

        // Hand the agent the same problem read-out the hint system gets — what the current
        // program actually does and where it falls short — so it reasons like an IDE copilot.
        string diagnosis = BuildDiagnosis(currentSource);
        if (!string.IsNullOrEmpty(diagnosis))
            world += "\nCURRENT PROGRAM DIAGNOSIS:\n" + diagnosis;

        // A one-tap "undo" after a refactor restores the player's original program (no AI call).
        if (_stashedCode != null && VibeIntentRouter.IsUndo(message))
        {
            RevertStash(reply);
        }
        else
        {
            // In Auto, classify the message; an explicit mode button overrides the router.
            VibeMode effective = _mode == VibeMode.Auto
                ? VibeIntentRouter.Classify(message, !string.IsNullOrWhiteSpace(currentSource))
                : _mode;

            switch (effective)
            {
                case VibeMode.Agent:    yield return RunAgent(message, world, reply); break;
                case VibeMode.Refactor: yield return RunRefactor(world, reply, currentSource); break;
                default:                yield return RunTextMode(effective, message, world, reply); break;
            }
        }

        SetEnabled(true);
        if (chatInput != null) chatInput.ActivateInputField();
    }

    /// <summary>Ask / Plan: a plain-text answer, no edits.</summary>
    IEnumerator RunTextMode(VibeMode mode, string message, string world, TMP_Text reply)
    {
        AiResult result = null;
        yield return GeminiClient.Stream(VibeCodingService.BuildAgentRequest(mode, message, world),
            null, completed => result = completed);

        UpdateBubble(reply, result != null && result.Success && !string.IsNullOrWhiteSpace(result.Text)
            ? result.Text.Trim()
            : "(couldn't reach the AI — check your connection and try again)");
    }

    /// <summary>Agent: generate an action graph, compile + validate it, dry-run it against the
    /// maze, and place it into the editor only when it actually reaches the goal. Write-only —
    /// the player presses RUN. Up to two repair rounds correct syntax (compile/vocabulary) or
    /// logic (the program ran but missed the goal), sending only the error back each time.</summary>
    IEnumerator RunAgent(string message, string world, TMP_Text reply)
    {
        const int maxRepairs = 2;
        AiRequest request = VibeCodingService.BuildAgentRequest(VibeMode.Agent, message, world);
        string code = null;
        string lastMessage = null;

        for (int attempt = 0; attempt <= maxRepairs; attempt++)
        {
            AiResult result = null;
            yield return GeminiClient.Stream(request, null, completed => result = completed);

            if (result == null || !result.Success ||
                !ActionGraphCompiler.TryParse(result.Text, out ActionGraphResponse graph))
            {
                UpdateBubble(reply, "(couldn't reach the AI — check your connection and try again)");
                yield break;
            }
            lastMessage = graph.message;

            // Syntax + vocabulary: compile the flat graph and check it against the level's
            // unlocked names. A failure here is repaired with the compile/validation error.
            if (!ActionGraphCompiler.TryCompile(graph, out string compiled, out string compileError) ||
                VibeCodingService.Validate(compiled, _allowedBlocks, _allowedQueries, out _) != null)
            {
                if (attempt == maxRepairs) break;
                string err = compileError ?? "the program used something that isn't unlocked here";
                UpdateBubble(reply, (graph.message ?? "Let me fix that…").Trim() + "  (checking one correction…)");
                request = VibeCodingService.BuildAgentRepairRequest(message, world, err);
                continue;
            }

            // Logic: dry-run the program against a fresh copy of the world. If it doesn't reach
            // the goal, repair with the goal gap so the model fixes the route, not the grammar.
            if (TryVerify(compiled, out string goalGap))
            {
                code = compiled;
                break;
            }

            if (attempt == maxRepairs) break;
            UpdateBubble(reply, (graph.message ?? "Let me check my route…").Trim() + "  (testing and refining…)");
            request = VibeCodingService.BuildAgentRepairRequest(message, world,
                "the program ran but didn't solve it — " + goalGap);
        }

        if (code != null)
        {
            bool toBlocks = ApplyProgram(code);
            string note = toBlocks
                ? "✓ Built it on your block canvas — press RUN when you're ready."
                : _blockMode
                    ? "✓ This solution uses code this level can't show as blocks, so I put it in the code editor — switch to Code to use it."
                    : "✓ Placed in your editor. Press Code to review it, then RUN when you're ready.";
            UpdateBubble(reply, (lastMessage ?? "Here's a program for you.").Trim() + "\n" + note);
        }
        else
        {
            UpdateBubble(reply, "I couldn't make a program that fits this level's rules, so I left your code " +
                "unchanged. Try asking in Plan mode for the approach.");
        }
    }

    /// <summary>Drops a verified program onto the player's active surface: the block canvas in
    /// block mode (when the program is block-expressible), else the code editor. Returns true
    /// when it landed as blocks.</summary>
    bool ApplyProgram(string code)
    {
        if (_blockMode && blockCanvas != null)
        {
            ProgramNode program = Parser.Compile(code, out List<LangError> errors);
            if ((errors == null || errors.Count == 0) && program != null && blockCanvas.LoadProgram(program))
                return true;
        }

        if (codeEditor != null && codeEditor.input != null)
        {
            codeEditor.input.SetTextWithoutNotify(code);
            codeEditor.RefreshLineNumbers();
            codeEditor.RefreshHighlight();
        }
        return false;
    }

    /// <summary>Reads the current program (block source in block mode, editor text otherwise),
    /// dry-runs it, and returns a one-line read-out of what it does + where it falls short.</summary>
    string BuildDiagnosis(string source)
    {
        if (_grid == null || _sim == null || _def == null) return null;
        if (string.IsNullOrWhiteSpace(source)) return "the program is empty so far.";

        ProgramNode program = Parser.Compile(source, out List<LangError> errors);
        if (errors != null && errors.Count > 0)
            return "it doesn't compile yet — " + errors[0].Message;

        HeadlessProgramRunner.VerifyReport(program, _sim.CloneFresh(), _def, out RunReport report);
        string outcome = report != null && !string.IsNullOrEmpty(report.GoalGap)
            ? report.GoalGap
            : "it reaches the goal";
        DiagnosticsResult diag = CodeDiagnostics.Analyze(source, report, _allowedBlocks);
        string patterns = diag != null && !string.IsNullOrEmpty(diag.Summary) ? diag.Summary : "None.";
        return $"outcome: {outcome}; patterns: {patterns}";
    }

    /// <summary>Refactor: take the player's already-working code and offer a shorter version that
    /// uses loops. Accepts the rewrite only if it compiles, stays within the unlocked vocabulary,
    /// still WINS, and is strictly fewer statements (the "same goal + fewer steps" rule). Stashes
    /// the original so the player can say "undo". Refactor only operates on working code — an empty,
    /// broken, or losing program is redirected to the diagnostic path instead.</summary>
    IEnumerator RunRefactor(string world, TMP_Text reply, string editorText)
    {
        if (string.IsNullOrWhiteSpace(editorText))
        {
            UpdateBubble(reply, "Write a few lines first, then I'll streamline them for you.");
            yield break;
        }

        // Refactor presupposes correct code. Compile + (when a world is bound) dry-run it first.
        ProgramNode current = Parser.Compile(editorText, out List<LangError> cerr);
        if ((cerr != null && cerr.Count > 0) || current == null)
        {
            UpdateBubble(reply, "Let's get it running first — there's still a syntax error to fix. " +
                "Ask me \"why isn't this working?\" and I'll help.");
            yield break;
        }
        if (_grid != null && _sim != null && _def != null &&
            !HeadlessProgramRunner.VerifyReport(current, _sim.CloneFresh(), _def, out RunReport rep))
        {
            UpdateBubble(reply, "This doesn't solve the level yet, so I won't shorten it — let's make it " +
                "work first. Ask me \"why isn't this working?\" and I'll help. (" + (rep?.GoalGap ?? "") + ")");
            yield break;
        }

        int playerStatements = CodeAnalyticsService.Measure(editorText).Statements;

        const int maxRepairs = 2;
        AiRequest request = VibeCodingService.BuildRefactorRequest(editorText, world);
        string code = null;
        string lastMessage = null;

        for (int attempt = 0; attempt <= maxRepairs; attempt++)
        {
            AiResult result = null;
            yield return GeminiClient.Stream(request, null, completed => result = completed);

            if (result == null || !result.Success ||
                !ActionGraphCompiler.TryParse(result.Text, out ActionGraphResponse graph))
            {
                UpdateBubble(reply, "(couldn't reach the AI — check your connection and try again)");
                yield break;
            }
            lastMessage = graph.message;

            if (!ActionGraphCompiler.TryCompile(graph, out string compiled, out string compileError) ||
                VibeCodingService.Validate(compiled, _allowedBlocks, _allowedQueries, out _) != null)
            {
                if (attempt == maxRepairs) break;
                string err = compileError ?? "the rewrite used something that isn't unlocked here";
                UpdateBubble(reply, "Tidying that up…");
                request = VibeCodingService.BuildRefactorRepairRequest(editorText, world, err);
                continue;
            }

            // Must still win AND be strictly shorter than the player's version.
            bool wins = TryVerify(compiled, out string goalGap);
            int newStatements = CodeAnalyticsService.Measure(compiled).Statements;
            if (wins && (playerStatements == 0 || newStatements < playerStatements))
            {
                code = compiled;
                break;
            }

            if (attempt == maxRepairs) break;
            string reason = !wins
                ? "the shorter version stopped solving it — " + goalGap
                : "it wasn't actually shorter; replace repeated lines with a loop";
            UpdateBubble(reply, "Refining…");
            request = VibeCodingService.BuildRefactorRepairRequest(editorText, world, reason);
        }

        if (code != null)
        {
            _stashedCode = editorText;   // enable a one-tap revert (block source or editor text)
            int saved = playerStatements - CodeAnalyticsService.Measure(code).Statements;
            bool toBlocks = ApplyProgram(code);
            string where = toBlocks ? "Your blocks are rebuilt" : "Press Code to review";
            string note = saved > 0 ? $"Made it {saved} step(s) shorter with a loop. " : "Tightened it up with a loop. ";
            UpdateBubble(reply, (lastMessage ?? "").Trim() + (lastMessage != null ? "\n" : "") + note +
                where + " — say \"undo\" to put your version back, then RUN when ready.");
        }
        else
        {
            UpdateBubble(reply, "I couldn't make it shorter while keeping it correct, so I left your code as " +
                "is — it's already pretty tight!");
        }
    }

    /// <summary>Restores the active surface (blocks or editor) to the program stashed before
    /// the last refactor.</summary>
    void RevertStash(TMP_Text reply)
    {
        if (_stashedCode != null) ApplyProgram(_stashedCode);
        _stashedCode = null;
        UpdateBubble(reply, "Done — I put your original version back.");
    }

    /// <summary>Dry-runs compiled source against a fresh copy of the live world. Returns true
    /// (and leaves <paramref name="goalGap"/> null) when the program reaches the goal, or when
    /// there is no live world bound to verify against — then the syntactic checks stand alone.</summary>
    bool TryVerify(string source, out string goalGap)
    {
        goalGap = null;
        if (_grid == null || _sim == null || _def == null) return true; // nothing to dry-run

        ProgramNode program = Parser.Compile(source, out List<LangError> errors);
        if (errors != null && errors.Count > 0)
        {
            goalGap = errors[0].Message;
            return false;
        }
        return HeadlessProgramRunner.Verify(program, _sim.CloneFresh(), _def, out goalGap);
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
