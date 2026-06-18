using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Code Mode text editor (hard mode), styled after "The Farmer Was Replaced":
/// a dark editor with a line-number gutter, live syntax highlighting, and
/// debounced lint. The input field holds the raw source (its own glyphs are
/// invisible); a colorized overlay paints the same characters so colors line
/// up exactly with the caret. Parse problems surface inline (error line
/// reddened in the gutter) and in the status label, in plain English.
///
/// Recent upgrades: execution line highlight, per-line heatmap, auto-closing
/// pairs, and a purchasable theme palette.
/// </summary>
public class CodeEditorController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] public TMP_InputField input;
    [SerializeField] private TMP_Text lineNumbers;
    [SerializeField] private TMP_Text highlight;
    [SerializeField] private TMP_Text lintLabel;

    [Header("Exec highlight")]
    [SerializeField] private Image execLineBar;

    [Header("Lint")]
    [SerializeField] private float lintDelaySeconds = 0.5f;

    float _lintTimer;
    bool  _dirty;
    List<LangError> _errors = new List<LangError>();

    CodeTheme _theme;
    Dictionary<int, int> _heatHits;
    float _heatPulse;

    // Auto-closing pairs state.
    const string Openers  = "([{\"";
    const string Closers  = ")]}\"";
    readonly HashSet<char> _autoCloseChars = new HashSet<char> { '(', ')', '[', ']', '{', '}', '\"' };
    bool _justAutoClosed;

    public string Source => input != null ? input.text : "";

    // -------------------------------------------------------------------------

    void Start()
    {
        ApplyTheme();

        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnSettingsChanged += ApplyTheme;

        if (input != null && input.textComponent != null)
        {
            // The raw text stays in the input (for the caret/selection) but is
            // drawn transparent — the highlight overlay supplies the colors.
            Color c = input.textComponent.color;
            input.textComponent.color = new Color(c.r, c.g, c.b, 0f);

            // Lay the raw text out literally so the caret lands exactly where the
            // overlay draws each glyph (the overlay escapes '<' via <noparse>).
            input.textComponent.richText = false;

            // Auto-close pairs and skip-over already-inserted closers.
            input.onValidateInput += OnValidateInput;
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

    void OnDestroy()
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnSettingsChanged -= ApplyTheme;

        if (input != null)
            input.onValidateInput -= OnValidateInput;
    }

    void Update()
    {
        if (_dirty)
        {
            _lintTimer -= Time.deltaTime;
            if (_lintTimer <= 0f)
            {
                _dirty = false;
                Lint();
            }
        }

        if (_heatHits != null && _heatHits.Count > 0)
        {
            _heatPulse += Time.deltaTime * 4f;
            RefreshLineNumbers();
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

        UpdateExecLineBarPosition();
    }

    // -------------------------------------------------------------------------
    // Theme

    void ApplyTheme()
    {
        int themeId = SettingsManager.Instance != null
            ? SettingsManager.Instance.CodeThemeId
            : SaveSystem.Current.settings.codeThemeId;
        _theme = CodeThemeLibrary.Get(themeId);

        if (input != null)
        {
            var bg = input.GetComponent<Image>();
            if (bg != null) bg.color = _theme.backgroundColor;
        }

        if (highlight != null)
        {
            highlight.color = _theme.textColor;
            highlight.ForceMeshUpdate();
        }

        RefreshHighlight();
        RefreshLineNumbers();
    }

    // -------------------------------------------------------------------------
    // Auto-closing pairs

    char OnValidateInput(string text, int charIndex, char addedChar)
    {
        int opener = Openers.IndexOf(addedChar);
        if (opener >= 0)
        {
            // Don't auto-close inside a string or comment.
            if (IsInStringOrComment(text, charIndex)) return addedChar;

            char close = Closers[opener];
            _justAutoClosed = true;
            input.SetTextWithoutNotify(text.Insert(charIndex, addedChar.ToString() + close));
            input.stringPosition = charIndex + 1;
            RefreshLineNumbers();
            RefreshHighlight();
            return '\0';
        }

        int closer = Closers.IndexOf(addedChar);
        if (closer >= 0 && charIndex < text.Length && text[charIndex] == addedChar && _justAutoClosed)
        {
            _justAutoClosed = false;
            input.stringPosition = charIndex + 1;
            return '\0';
        }

        _justAutoClosed = false;
        return addedChar;
    }

    bool IsInStringOrComment(string text, int upTo)
    {
        bool inString = false;
        for (int i = 0; i < upTo && i < text.Length; i++)
        {
            char c = text[i];
            if (c == '#' && !inString) return true;
            if (c == '\"') inString = !inString;
        }
        return inString;
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
        Parser.Compile(Source, out _errors);
        if (_errors == null) _errors = new List<LangError>();
        RefreshLineNumbers();

        if (lintLabel == null) return;

        if (_errors.Count == 0)
        {
            lintLabel.text = $"<color=#{ColorToHex(_theme.okColor)}>✓  looks good</color>";
        }
        else
        {
            lintLabel.text = $"<color=#{ColorToHex(_theme.errorColor)}>{_errors[0]}</color>";
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
            sb.Append("<color=#").Append(LineNumberColorHex(i)).Append('>').Append(i).Append("</color>");
            sb.Append('\n');
        }

        lineNumbers.text = sb.ToString();
    }

    string LineNumberColorHex(int line)
    {
        // Error line overrides heat.
        if (_errors.Count > 0 && _errors[0].Line == line)
            return ColorToHex(_theme.errorColor);

        if (_heatHits != null && _heatHits.TryGetValue(line, out int hits) && hits > 0)
        {
            float t = Mathf.Clamp01(hits / 50f);
            Color c = Color.Lerp(_theme.heatColdColor, _theme.heatHotColor, t);
            // Pulse the hottest line.
            if (hits >= 50)
            {
                float alpha = 0.65f + 0.35f * Mathf.Sin(_heatPulse);
                c.a = alpha;
            }
            return ColorToHex(c);
        }

        return ColorToHex(_theme.textColor);
    }

    public void RefreshHighlight()
    {
        if (highlight == null || input == null) return;
        highlight.text = Colorize(input.text);
    }

    // -------------------------------------------------------------------------
    // Execution line highlight

    public void HighlightExecutingLine(int line)
    {
        if (execLineBar == null || highlight == null) return;
        if (line < 1 || line > LineCount())
        {
            ClearExecutionHighlight();
            return;
        }

        execLineBar.gameObject.SetActive(true);
        _executingLine = line;
        UpdateExecLineBarPosition();
    }

    public void ClearExecutionHighlight()
    {
        _executingLine = -1;
        if (execLineBar != null) execLineBar.gameObject.SetActive(false);
    }

    int _executingLine = -1;

    void UpdateExecLineBarPosition()
    {
        if (execLineBar == null || highlight == null || _executingLine < 1) return;

        highlight.ForceMeshUpdate();
        if (_executingLine > highlight.textInfo.lineCount) return;

        TMP_LineInfo lineInfo = highlight.textInfo.lineInfo[_executingLine - 1];

        RectTransform viewport = input != null ? input.textViewport : highlight.rectTransform;
        RectTransform barRt = (RectTransform)execLineBar.transform;

        barRt.SetParent(viewport, false);
        barRt.anchorMin = new Vector2(0f, 1f);
        barRt.anchorMax = new Vector2(1f, 1f);
        barRt.pivot     = new Vector2(0.5f, 1f);

        float y = lineInfo.ascender + highlight.rectTransform.anchoredPosition.y;
        float height = lineInfo.lineHeight;

        barRt.anchoredPosition = new Vector2(0f, y);
        barRt.sizeDelta = new Vector2(0f, height);

        Color c = _theme.execBarColor;
        c.a = 0.35f + 0.15f * Mathf.Sin(Time.time * 3f);
        execLineBar.color = c;
    }

    int LineCount()
    {
        if (input == null) return 0;
        int count = 1;
        string text = input.text;
        for (int i = 0; i < text.Length; i++)
            if (text[i] == '\n') count++;
        return count;
    }

    // -------------------------------------------------------------------------
    // Heatmap

    public void SetHeat(IReadOnlyDictionary<int, int> hits)
    {
        _heatHits = hits != null ? new Dictionary<int, int>(hits) : null;
        _heatPulse = 0f;
        RefreshLineNumbers();
    }

    public void ClearHeat()
    {
        _heatHits = null;
        RefreshLineNumbers();
    }

    // -------------------------------------------------------------------------
    // Syntax colouring — preserves every character, only inserts color tags so
    // the overlay stays glyph-aligned with the (invisible) input text.

    string Colorize(string src)
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
                sb.Append("<color=#").Append(ColorToHex(_theme.commentColor)).Append('>')
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

    string WordColor(string word)
    {
        switch (word)
        {
            case "if": case "else": case "while": case "not":
            case "for": case "in": case "def": case "return":
            case "break": case "continue": case "elif":
            case "True": case "False": case "None":
                return ColorToHex(_theme.keywordColor);
        }
        if (AgentApi.IsAction(word))  return ColorToHex(_theme.actionColor);
        if (AgentApi.IsQuery(word))   return ColorToHex(_theme.queryColor);
        if (AgentApi.IsReporter(word)) return ColorToHex(_theme.queryColor);
        return null;
    }

    static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';

    static string ColorToHex(Color c)
    {
        return $"{Mathf.RoundToInt(c.r * 255):X2}{Mathf.RoundToInt(c.g * 255):X2}{Mathf.RoundToInt(c.b * 255):X2}";
    }
}
