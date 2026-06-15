using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// VS Code Copilot-style floating chat for the Automation workspace. The player
/// describes what the jeepney should do in plain English; Gemini generates
/// valid automation-language code and injects it into the Code Editor.
/// </summary>
public class VibeCodingController : MonoBehaviour
{
    [SerializeField] TMP_InputField chatInput;
    [SerializeField] TMP_Text       historyLabel;
    [SerializeField] Button         sendButton;
    [SerializeField] CodeEditorController codeEditor;

    string[] _allowedBlocks;
    string[] _allowedQueries;
    readonly StringBuilder _history = new StringBuilder();

    public void Init(string[] allowedBlocks, string[] allowedQueries, CodeEditorController editor)
    {
        _allowedBlocks  = allowedBlocks;
        _allowedQueries = allowedQueries;
        if (editor != null) codeEditor = editor;

        if (sendButton != null)
            sendButton.onClick.AddListener(OnSend);

        if (chatInput != null)
            chatInput.onSubmit.AddListener(_ => OnSend());
    }

    void OnSend()
    {
        if (chatInput == null) return;

        string text = chatInput.text;
        if (string.IsNullOrWhiteSpace(text)) return;

        chatInput.text = "";
        AppendHistory($"You: {text.Trim()}");
        SetEnabled(false);
        StartCoroutine(GenerateCode(text.Trim()));
    }

    IEnumerator GenerateCode(string intent)
    {
        AppendHistory("Copilot: ...");

        string prompt = VibeCodingService.BuildPrompt(intent, _allowedBlocks, _allowedQueries);
        string result = null;
        yield return GeminiClient.Ask(prompt, r => result = r);

        if (result == null)
        {
            UpdateLastHistory("Copilot: (couldn't reach the AI — try again)");
            SetEnabled(true);
            yield break;
        }

        string validationError = VibeCodingService.Validate(result, out _);
        if (validationError != null)
        {
            UpdateLastHistory($"Copilot: Hmm, I got confused ({validationError}). Try rephrasing?");
            SetEnabled(true);
            yield break;
        }

        if (codeEditor != null)
        {
            codeEditor.input.SetTextWithoutNotify(result);
            codeEditor.RefreshLineNumbers();
            codeEditor.RefreshHighlight();
        }

        UpdateLastHistory("Copilot: ✓ Code applied! Run it to see if it works.");
        SetEnabled(true);
    }

    void AppendHistory(string line)
    {
        _history.AppendLine(line);
        if (historyLabel != null) historyLabel.text = _history.ToString();
    }

    void UpdateLastHistory(string line)
    {
        // Replace the last appended line (the "Copilot: ..." placeholder).
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
