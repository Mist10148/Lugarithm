using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Copilot-style inline ghost-text for the code editor: after a short typing pause it requests the
/// single next line from the AI and paints it faintly after the cursor. <b>Tab</b> accepts it; any
/// caret move, Esc, or further typing dismisses it. Write-only — it never runs anything.
///
/// Rendering reuses the editor's overlay trick: a sibling <see cref="TMP_Text"/> whose base colour
/// is transparent, so the existing code (wrapped in <c>&lt;noparse&gt;</c> for alignment) stays
/// invisible while only the suggestion is tinted. v1 offers a suggestion only when the caret is at
/// the very end of the text, which keeps alignment exact without per-glyph caret math.
///
/// Cost control: a tiny 32-token request on the shared model ladder, a hard debounce, the
/// <see cref="AiResponseCache.Ghost"/> cache, and supersede-on-keystroke cancellation — so it can
/// never hammer the free Gemini keys. Self-disables in block mode. Everything is null-guarded so an
/// unwired editor/label is simply a no-op.
/// </summary>
public class GhostTextController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] CodeEditorController editor;
    [SerializeField] TMP_Text ghostLabel;   // faint overlay sibling of the editor's highlight layer

    [Header("Tuning")]
    [SerializeField] bool  enableGhost = true;
    [SerializeField] float debounceSeconds = 0.7f;

    const string FaintHex = "8FA3BD80";   // light blue-gray, ~50% alpha

    string   _goalText;
    string[] _allowedBlocks;
    string[] _allowedQueries;
    bool     _bound;

    bool   _pending;
    float  _timer;
    bool   _visible;
    string _suggestion;
    int    _suggestionCaret;
    bool   _needNewline;
    AiCancellation _inflight;

    void Awake()
    {
        if (ghostLabel != null)
        {
            ghostLabel.richText = true;
            Color c = ghostLabel.color;
            ghostLabel.color = new Color(c.r, c.g, c.b, 0f);   // prefix stays invisible
            ghostLabel.gameObject.SetActive(false);
        }
    }

    void Start()
    {
        if (editor != null && editor.input != null)
            editor.input.onValueChanged.AddListener(OnEditorChanged);
    }

    void OnDestroy()
    {
        if (editor != null && editor.input != null)
            editor.input.onValueChanged.RemoveListener(OnEditorChanged);
    }

    /// <summary>Binds the puzzle so completions stay on-goal and within the unlocked vocabulary.
    /// Call where the host wires the rest of the editor (alongside vibeCtrl.SetWorldContext).</summary>
    public void Bind(AutomationPuzzleDefinition def)
    {
        if (def == null) return;
        _goalText       = def.goalText;
        _allowedBlocks  = def.allowedBlocks;
        _allowedQueries = def.allowedQueries;
        _bound          = true;
    }

    void OnEditorChanged(string _)
    {
        Dismiss();
        _pending = true;
        _timer   = debounceSeconds;
    }

    void Update()
    {
        bool blockMode = SaveSystem.Current != null && SaveSystem.Current.settings != null &&
                         SaveSystem.Current.settings.blockMode;
        if (!enableGhost || blockMode || !_bound || editor == null || editor.input == null)
        {
            if (_visible) Dismiss();
            return;
        }

        if (_visible)
        {
            bool caretMoved = editor.input.stringPosition != _suggestionCaret;
            if (!editor.input.isFocused || caretMoved ||
                Input.GetKeyDown(KeyCode.Escape) ||
                Input.GetKeyDown(KeyCode.LeftArrow)  || Input.GetKeyDown(KeyCode.RightArrow) ||
                Input.GetKeyDown(KeyCode.UpArrow)    || Input.GetKeyDown(KeyCode.DownArrow))
            {
                Dismiss();
            }
            else if (Input.GetKeyDown(KeyCode.Tab))
            {
                Accept();
                return;
            }
            else
            {
                MirrorGeometry();   // keep the overlay aligned as the editor relayouts
            }
        }

        if (_pending)
        {
            _timer -= Time.unscaledDeltaTime;
            if (_timer <= 0f) RequestSuggestion();
        }
    }

    void RequestSuggestion()
    {
        _pending = false;
        if (editor == null || editor.input == null || !editor.input.isFocused) return;

        string text  = editor.input.text ?? "";
        int    caret = editor.input.stringPosition;
        if (caret != text.Length) return;   // v1: only complete at end-of-text

        string key = Key(text);
        if (AiResponseCache.Ghost.TryGet(key, out string cached))
        {
            ShowSuggestion(cached, text, caret);
            return;
        }

        _inflight?.Cancel();
        _inflight = new AiCancellation();
        AiCancellation mine = _inflight;

        AiRequest req = VibeCodingService.BuildGhostRequest(text, _goalText, _allowedBlocks, _allowedQueries);
        req.Cancellation = mine;

        string reqText = text;
        int    reqCaret = caret;
        StartCoroutine(GeminiClient.Stream(req, null, result =>
        {
            if (mine.IsCancellationRequested) return;          // superseded by newer typing
            if (result == null || !result.Success) return;
            string line = Clean(result.Text);
            if (string.IsNullOrEmpty(line)) return;
            AiResponseCache.Ghost.Put(key, line);

            // Only surface if the editor is still exactly where we asked.
            if (editor.input != null && editor.input.text == reqText &&
                editor.input.stringPosition == reqText.Length && editor.input.isFocused)
                ShowSuggestion(line, reqText, reqCaret);
        }));
    }

    void ShowSuggestion(string line, string text, int caret)
    {
        _suggestion      = line;
        _suggestionCaret = caret;
        _needNewline     = text.Length > 0 && text[text.Length - 1] != '\n';
        RenderGhost(text);
        _visible = true;
    }

    void RenderGhost(string text)
    {
        if (ghostLabel == null) return;
        string display = (_needNewline ? "\n" : "") + _suggestion;
        ghostLabel.text = "<noparse>" + text + "</noparse><color=#" + FaintHex + "><noparse>" +
                          display + "</noparse></color>";
        ghostLabel.gameObject.SetActive(true);
        MirrorGeometry();
    }

    void Accept()
    {
        if (!_visible || editor == null || editor.input == null) { Dismiss(); return; }

        string text    = editor.input.text ?? "";
        string updated = text + (_needNewline ? "\n" : "") + _suggestion;
        editor.input.SetTextWithoutNotify(updated);
        editor.input.stringPosition = updated.Length;
        editor.input.caretPosition  = updated.Length;
        editor.RefreshLineNumbers();
        editor.RefreshHighlight();

        Dismiss();
        _pending = true;          // offer the line after, too
        _timer   = debounceSeconds;
    }

    void Dismiss()
    {
        _visible    = false;
        _suggestion = null;
        if (ghostLabel != null)
        {
            ghostLabel.text = "";
            ghostLabel.gameObject.SetActive(false);
        }
    }

    void MirrorGeometry()
    {
        if (ghostLabel == null || editor == null || editor.input == null) return;
        TMP_Text src = editor.input.textComponent;
        if (src == null) return;

        RectTransform s = src.rectTransform;
        RectTransform g = ghostLabel.rectTransform;
        g.anchorMin        = s.anchorMin;
        g.anchorMax        = s.anchorMax;
        g.pivot            = s.pivot;
        g.sizeDelta        = s.sizeDelta;
        g.anchoredPosition = s.anchoredPosition;

        ghostLabel.margin           = src.margin;
        ghostLabel.fontSize         = src.fontSize;
        ghostLabel.alignment        = src.alignment;
        ghostLabel.lineSpacing      = src.lineSpacing;
        ghostLabel.characterSpacing = src.characterSpacing;
        ghostLabel.textWrappingMode = src.textWrappingMode;
        if (src.font != null && ghostLabel.font != src.font) ghostLabel.font = src.font;
    }

    string Key(string text)
        => _goalText + "|" +
           string.Join(",", _allowedBlocks ?? Array.Empty<string>()) + "|" + text;

    /// <summary>Reduces a model reply to one clean line: strips markdown fences, drops blank/fence
    /// lines, keeps leading indentation, trims trailing space.</summary>
    static string Clean(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;
        string t = raw.Replace("```", "");
        foreach (string lineRaw in t.Split('\n'))
        {
            string line = lineRaw.TrimEnd();
            string trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed == "python") continue;
            return line;
        }
        return null;
    }
}
