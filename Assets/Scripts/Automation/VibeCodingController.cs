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
        AppendHistory("Tutor: generating…");

        AiResult result = null;
        int packets = 0;
        yield return GeminiClient.Stream(
            VibeCodingService.BuildTutorRequest(message, _allowedBlocks, _allowedQueries),
            _ =>
            {
                packets++;
                UpdateLastHistory("Tutor: generating" + new string('.', 1 + packets % 3));
            },
            completed => result = completed);

        if (result == null || !result.Success || !VibeCodingService.TryParse(result.Text, out VibeCodeResponse response))
        {
            UpdateLastHistory("Tutor: (couldn't reach the AI — check your connection and try again)");
            SetEnabled(true);
            yield break;
        }

        UpdateLastHistory("Tutor: " + response.message.Trim());
        if (response.kind == "code")
        {
            string validation = VibeCodingService.Validate(response.code, _allowedBlocks, _allowedQueries, out _);
            if (validation != null)
            {
                AppendHistory("Tutor: checking one correction…");
                AiResult repairedResult = null;
                yield return GeminiClient.Stream(
                    VibeCodingService.BuildRepairRequest(message, response, validation, _allowedBlocks, _allowedQueries),
                    null, completed => repairedResult = completed);
                if (repairedResult != null && repairedResult.Success &&
                    VibeCodingService.TryParse(repairedResult.Text, out VibeCodeResponse repaired))
                {
                    response = repaired;
                    validation = VibeCodingService.Validate(response.code, _allowedBlocks, _allowedQueries, out _);
                }
            }

            if (validation == null && codeEditor != null)
            {
                codeEditor.input.SetTextWithoutNotify(response.code);
                codeEditor.RefreshLineNumbers();
                codeEditor.RefreshHighlight();
                AppendHistory("Tutor: ✓ Validated and placed in your editor. Press Code to review it, then RUN when you're ready.");
            }
            else
            {
                AppendHistory("Tutor: I kept your existing code unchanged because the generated program did not pass this level's rules.");
            }
        }

        SetEnabled(true);
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
