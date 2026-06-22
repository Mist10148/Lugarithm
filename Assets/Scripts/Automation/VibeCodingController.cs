using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// In-window AI helper for the CODE editor. The title-bar "AI" button swaps the
/// editor body for this chat; the "Code" button swaps back. The player can ask
/// free-form questions (a friendly tutor answers in plain language) or ask for
/// code, in which case Gemini's reply includes a program that is dropped straight
/// into the editor. Lives on the CodeWindow itself, so it self-wires in Awake and
/// works even where <see cref="Init"/> is never called (e.g. the maze minigame).
/// </summary>
public class VibeCodingController : MonoBehaviour
{
    [Header("Chat")]
    [SerializeField] TMP_InputField      chatInput;
    [SerializeField] TMP_Text            historyLabel;
    [SerializeField] Button              sendButton;
    [SerializeField] CodeEditorController codeEditor;

    [Header("View swap (AI ⇄ Code)")]
    [SerializeField] GameObject editorBody;   // the code editor, hidden while chatting
    [SerializeField] GameObject chatBody;     // this chat panel, hidden while editing
    [SerializeField] Button     aiButton;     // title bar: show chat
    [SerializeField] Button     codeButton;   // title bar: show editor

    string[] _allowedBlocks;
    string[] _allowedQueries;
    readonly StringBuilder _history = new StringBuilder();
    bool _wired;

    void Awake()
    {
        Wire();
        ShowEditor();   // editor is the default face of the window
    }

    void Wire()
    {
        if (_wired) return;
        _wired = true;

        if (sendButton != null) sendButton.onClick.AddListener(OnSend);
        if (chatInput  != null) chatInput.onSubmit.AddListener(_ => OnSend());
        if (aiButton   != null) aiButton.onClick.AddListener(ShowChat);
        if (codeButton != null) codeButton.onClick.AddListener(ShowEditor);
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
        AppendHistory($"You: {message}");
        AppendHistory("Tutor: …");

        string prompt = VibeCodingService.BuildTutorPrompt(message, _allowedBlocks, _allowedQueries);
        string result = null;
        yield return GeminiClient.Ask(prompt, r => result = r);

        if (result == null)
        {
            UpdateLastHistory("Tutor: (couldn't reach the AI — check your connection and try again)");
            SetEnabled(true);
            yield break;
        }

        string code = ExtractCodeFence(result, out string spoken);
        UpdateLastHistory("Tutor: " + (string.IsNullOrWhiteSpace(spoken) ? "Here you go." : spoken.Trim()));

        // Only drop code in when the model actually wrote a valid program.
        if (code != null && codeEditor != null && VibeCodingService.Validate(code, out _) == null)
        {
            codeEditor.input.SetTextWithoutNotify(code);
            codeEditor.RefreshLineNumbers();
            codeEditor.RefreshHighlight();
            AppendHistory("Tutor: ✓ I put that in your editor — press \"Code\" and RUN to try it.");
        }

        SetEnabled(true);
    }

    /// <summary>Splits a reply into the spoken text and the contents of its first
    /// ```fenced``` code block (null when there is none).</summary>
    static string ExtractCodeFence(string reply, out string spoken)
    {
        spoken = reply;
        if (string.IsNullOrEmpty(reply)) return null;

        int open = reply.IndexOf("```", System.StringComparison.Ordinal);
        if (open < 0) return null;

        int afterOpen = reply.IndexOf('\n', open);
        if (afterOpen < 0) return null;
        int close = reply.IndexOf("```", afterOpen, System.StringComparison.Ordinal);
        if (close < 0) return null;

        string code = reply.Substring(afterOpen + 1, close - afterOpen - 1).TrimEnd();
        spoken = (reply.Substring(0, open) + reply.Substring(close + 3)).Trim();
        return code.Length == 0 ? null : code;
    }

    void AppendHistory(string line)
    {
        _history.AppendLine(line);
        if (historyLabel != null) historyLabel.text = _history.ToString();
    }

    void UpdateLastHistory(string line)
    {
        // Replace the last appended line (the "Tutor: …" placeholder).
        string s = _history.ToString();
        int last = s.LastIndexOf('\n', s.Length - 2);
        _history.Clear();
        _history.Append(last < 0 ? line : s.Substring(0, last + 1) + line);
        if (historyLabel != null) historyLabel.text = _history.ToString();
    }

    void SetEnabled(bool on)
    {
        if (chatInput  != null) chatInput.interactable  = on;
        if (sendButton != null) sendButton.interactable = on;
    }
}
