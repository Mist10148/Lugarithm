using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

/// <summary>
/// Code Mode text editor (hard mode), styled after "The Farmer Was Replaced":
/// a dark editor with a line-number gutter, live syntax highlighting, and
/// debounced lint. The input field holds the raw source (its own glyphs are
/// invisible); a colorized overlay renders the same characters so colors line
/// up exactly with the caret. Parse problems surface inline (error line
/// reddened in the gutter) and in the status label, in plain English.
/// </summary>
public class CodeEditorController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] public TMP_InputField input;
    [SerializeField] private TMP_Text lineNumbers;
    [SerializeField] private TMP_Text highlight;
    [SerializeField] private TMP_Text lintLabel;

    [Header("Lint")]
    [SerializeField] private float lintDelaySeconds = 0.5f;

    // Editor theme (VS Code "Dark+" family)
    const string KeywordHex = "C586C0";  // if / else / while / not
    const string ActionHex  = "DCDCAA";  // moveForward(), …
    const string QueryHex   = "4EC9B0";  // frontIsClear(), …
    const string CommentHex = "6A9955";  // # …
    const string ErrorHex   = "E06C75";
    const string OkHex       = "6A9955";

    float _lintTimer;
    bool  _dirty;
    int   _errorLine = -1;

    public string Source => input != null ? input.text : "";

    // -------------------------------------------------------------------------

    void Start()
    {
        if (input != null && input.textComponent != null)
        {
            // The raw text stays in the input (for the caret/selection) but is
            // drawn transparent — the highlight overlay supplies the colors.
            Color c = input.textComponent.color;
            input.textComponent.color = new Color(c.r, c.g, c.b, 0f);

            // Lay the raw text out literally so the caret lands exactly where the
            // overlay draws each glyph (the overlay escapes '<' via <noparse>).
            input.textComponent.richText = false;
        }

        if (input != null)
        {
            input.onValueChanged.AddListener(_ =>
            {
                _dirty = true;
                _lintTimer = lintDelaySeconds;
                RefreshLineNumbers();
                RefreshHighlight();
            });
        }

        RefreshLineNumbers();
        RefreshHighlight();
        SyncHighlightToInput();
    }

    void Update()
    {
        if (!_dirty) return;

        _lintTimer -= Time.deltaTime;
        if (_lintTimer <= 0f)
        {
            _dirty = false;
            Lint();
        }
    }

    // Keep the colour overlay glyph-for-glyph on top of the input's text every
    // frame. TMP_InputField shifts its text component to seat the caret and to
    // scroll; without mirroring, the colours drift onto a different line than
    // the caret (the "types above the cursor line" bug).
    void LateUpdate()
    {
        SyncHighlightToInput();
    }

    void SyncHighlightToInput()
    {
        if (highlight == null || input == null || input.textComponent == null) return;

        TMP_Text src = input.textComponent;
        RectTransform s = src.rectTransform;
        RectTransform h = highlight.rectTransform;

        h.anchorMin        = s.anchorMin;
        h.anchorMax        = s.anchorMax;
        h.pivot            = s.pivot;
        h.sizeDelta        = s.sizeDelta;
        h.anchoredPosition = s.anchoredPosition;

        highlight.margin           = src.margin;
        highlight.fontSize         = src.fontSize;
        highlight.alignment        = src.alignment;
        highlight.lineSpacing      = src.lineSpacing;
        highlight.characterSpacing = src.characterSpacing;
        highlight.textWrappingMode  = src.textWrappingMode;
        if (src.font != null && highlight.font != src.font) highlight.font = src.font;
    }

    // -------------------------------------------------------------------------

    /// <summary>Pre-fills the goal scaffold (only when the editor is empty).</summary>
    public void SetScaffold(string scaffold)
    {
        if (input != null && string.IsNullOrEmpty(input.text))
        {
            input.SetTextWithoutNotify(scaffold ?? "");
            RefreshLineNumbers();
            RefreshHighlight();
        }
    }

    /// <summary>Compiles the current source.</summary>
    public ProgramNode BuildProgram(out List<LangError> errors)
    {
        return Parser.Compile(Source, out errors);
    }

    // -------------------------------------------------------------------------

    void Lint()
    {
        Parser.Compile(Source, out List<LangError> errors);

        _errorLine = errors.Count > 0 ? errors[0].Line : -1;
        RefreshLineNumbers();

        if (lintLabel == null) return;

        if (errors.Count == 0)
        {
            lintLabel.text  = "<color=#" + OkHex + ">✓  looks good</color>";
        }
        else
        {
            lintLabel.text  = "<color=#" + ErrorHex + ">" + errors[0] + "</color>";
        }
    }

    public void RefreshLineNumbers()
    {
        if (lineNumbers == null || input == null) return;

        int count = 1;
        string text = input.text;
        for (int i = 0; i < text.Length; i++)
            if (text[i] == '\n') count++;

        var sb = new StringBuilder();
        for (int i = 1; i <= count; i++)
        {
            if (i == _errorLine) sb.Append("<color=#").Append(ErrorHex).Append('>').Append(i).Append("</color>");
            else                 sb.Append(i);
            sb.Append('\n');
        }

        lineNumbers.text = sb.ToString();
    }

    public void RefreshHighlight()
    {
        if (highlight == null || input == null) return;
        highlight.text = Colorize(input.text);
    }

    // -------------------------------------------------------------------------
    // Syntax colouring — preserves every character, only inserts color tags so
    // the overlay stays glyph-aligned with the (invisible) input text.

    static string Colorize(string src)
    {
        if (string.IsNullOrEmpty(src)) return "";

        var sb = new StringBuilder(src.Length + 64);
        int i = 0;
        while (i < src.Length)
        {
            char c = src[i];

            if (c == '#')                       // comment to end of line
            {
                int j = i;
                while (j < src.Length && src[j] != '\n') j++;
                sb.Append("<color=#").Append(CommentHex).Append('>')
                  .Append(src, i, j - i).Append("</color>");
                i = j;
            }
            else if (IsWordChar(c))             // identifier / keyword
            {
                int j = i;
                while (j < src.Length && IsWordChar(src[j])) j++;
                string word = src.Substring(i, j - i);
                string hex = WordColor(word);
                if (hex != null) sb.Append("<color=#").Append(hex).Append('>').Append(word).Append("</color>");
                else             sb.Append(word);
                i = j;
            }
            else
            {
                if (c == '<') sb.Append("<noparse><</noparse>"); // never breaks layout
                else          sb.Append(c);
                i++;
            }
        }
        return sb.ToString();
    }

    static string WordColor(string word)
    {
        switch (word)
        {
            case "if": case "else": case "while": case "not": return KeywordHex;
        }
        if (AgentApi.IsAction(word)) return ActionHex;
        if (AgentApi.IsQuery(word))  return QueryHex;
        return null;
    }

    static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';
}
