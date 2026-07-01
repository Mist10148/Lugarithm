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
/// pairs, error squigglies, gutter icons, code folding, autocomplete, and a
/// purchasable theme palette.
/// </summary>
public class CodeEditorController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] public TMP_InputField input;
    [SerializeField] private TMP_Text lineNumbers;
    [SerializeField] private TMP_Text highlight;
    [SerializeField] private TMP_Text lintLabel;
    [SerializeField] private RectTransform gutterRoot;
    // Scrolling layer holding the line numbers + gutter icons. Kept in lockstep
    // with the input's text component so numbers stay glued to their code line.
    [SerializeField] private RectTransform gutterContent;

    [Header("Exec highlight")]
    [SerializeField] private Image execLineBar;

    [Header("Squiggles")]
    [SerializeField] private RectTransform squigglesRoot;
    [SerializeField] private Sprite squiggleSprite;

    [Header("Autocomplete")]
    [SerializeField] private CodeAutocompleteController autocomplete;

    [Header("Lint")]
    [SerializeField] private float lintDelaySeconds = 0.5f;

    float _lintTimer;
    bool  _dirty;
    bool  _meshDirty = true;   // only ForceMeshUpdate the overlay when text actually changed
    float _heatRefreshTimer;   // throttles per-frame gutter rebuilds while heat pulses
    List<LangError> _errors = new List<LangError>();

    CodeTheme _theme = CodeTheme.DarkPlus;   // never null — color helpers run before ApplyTheme
    Dictionary<int, int> _heatHits;
    float _heatPulse;

    // Auto-closing pairs state.
    const string Openers  = "([{\"";
    const string Closers  = ")]}\"";
    char _pendingCloser;
    int  _pendingInsertAt = -1;

    // Squiggle pool.
    readonly List<Image> _squiggleImages = new List<Image>();

    // Gutter icon pool.
    readonly List<Image> _gutterIcons = new List<Image>();
    readonly Dictionary<int, Image> _gutterIconByLine = new Dictionary<int, Image>();

    // Folding.
    readonly HashSet<int> _foldedLines = new HashSet<int>();
    readonly List<FoldRange> _foldRanges = new List<FoldRange>();
    readonly List<Button> _foldButtons = new List<Button>();
    readonly HashSet<int> _visibleGutterIconLines = new HashSet<int>();

    struct FoldRange
    {
        public int HeaderLine;
        public int StartLine; // inclusive body start
        public int EndLine;   // inclusive body end
    }

    public string Source => input != null ? input.text : "";
    public int FoldRangeCount => _foldRanges.Count;
    public int FoldedHeaderCount => _foldedLines.Count;
    public int LogicalLineCount => LineCount();

    // -------------------------------------------------------------------------

    void Awake()
    {
        // Resolve the theme before any other component's Start() can reach the
        // colour helpers (AutomationDriveController.Start -> SetScaffold ->
        // RefreshLineNumbers -> _theme). Awake runs before all Starts, so this
        // removes the Start-order race that left _theme null and NRE'd.
        ApplyTheme();
    }

    void Start()
    {
        ApplyTheme();

        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnSettingsChanged += ApplyTheme;

        if (input != null && input.textComponent != null)
        {
            Color c = input.textComponent.color;
            input.textComponent.color = new Color(c.r, c.g, c.b, 0f);
            input.textComponent.richText = false;
            input.onValidateInput += OnValidateInput;
        }

        if (input != null)
        {
            input.onValueChanged.AddListener(_ =>
            {
                _dirty = true;
                _lintTimer = lintDelaySeconds;
                ComputeFoldRanges();
                RefreshLineNumbers();
                RefreshHighlight();
                RequestAutocomplete();
            });
        }

        ComputeFoldRanges();
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
            _heatRefreshTimer -= Time.deltaTime;
            if (_heatRefreshTimer <= 0f)
            {
                _heatRefreshTimer = 0.08f;   // ~12 Hz pulse instead of every frame
                RefreshLineNumbers();
            }
        }

        if (autocomplete != null && autocomplete.Visible)
        {
            if (Input.anyKeyDown && !IsNavigationKey())
            {
                // Let the input field update first, then refresh autocomplete.
                // Simplest is to re-request next frame via _dirty path.
            }
        }

        PreventCaretInFoldedRegion();
    }

    bool IsNavigationKey()
    {
        return Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow)
            || Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Return)
            || Input.GetKeyDown(KeyCode.Escape);
    }

    void LateUpdate()
    {
        ApplyPendingAutoClose();
        SyncHighlightToInput();
    }

    void SyncHighlightToInput()
    {
        if (highlight == null || input == null || input.textComponent == null) return;

        TMP_Text src = input.textComponent;
        ApplyCodeTextLayout(src);
        ApplyCodeTextLayout(highlight);
        ApplyCodeTextLayout(lineNumbers);

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
        highlight.textWrappingMode  = TextWrappingModes.NoWrap;
        highlight.overflowMode      = TextOverflowModes.Overflow;
        if (src.font != null && highlight.font != src.font) highlight.font = src.font;

        // Rebuild the overlay mesh at most once per change — the per-line geometry
        // below reads the cached textInfo, so we must not ForceMeshUpdate per frame.
        if (_meshDirty)
        {
            highlight.ForceMeshUpdate();
            _meshDirty = false;
        }

        // Scroll the line-number gutter in lockstep with the code. The input field
        // scrolls by shifting its text component vertically; mirror that exact
        // offset so every number tracks its glyph line (the gutter has a RectMask2D
        // so numbers that run past the top/bottom are clipped, not drawn over the
        // toolbar or lint row).
        if (gutterContent != null)
        {
            Vector2 gp = gutterContent.anchoredPosition;
            gp.y = src.rectTransform.anchoredPosition.y;
            gutterContent.anchoredPosition = gp;
        }

        // Keep gutter numbers on the same line metrics as the code so they stay
        // vertically aligned one-for-one even when font size or line spacing changes.
        if (lineNumbers != null)
        {
            lineNumbers.fontSize         = src.fontSize;
            lineNumbers.lineSpacing      = src.lineSpacing;
            lineNumbers.characterSpacing = src.characterSpacing;
            lineNumbers.textWrappingMode  = TextWrappingModes.NoWrap;
            lineNumbers.overflowMode      = TextOverflowModes.Overflow;
            if (src.font != null && lineNumbers.font != src.font)
                lineNumbers.font = src.font;
        }

        UpdateExecLineBarPosition();
        UpdateSquiggles();
        UpdateGutterIcons();
        UpdateFoldButtons();
    }

    static void ApplyCodeTextLayout(TMP_Text text)
    {
        if (text == null) return;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
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

        if (autocomplete != null)
        {
            autocomplete.actionColor = _theme.actionColor;
            autocomplete.queryColor = _theme.queryColor;
            autocomplete.keywordColor = _theme.keywordColor;
            autocomplete.varColor = _theme.variableColor;
            autocomplete.selectedColor = _theme.execBarColor;
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
            if (IsInStringOrComment(text, charIndex)) return addedChar;

            // Let TMP_InputField insert the opener normally, then append the matching
            // closer in LateUpdate. Doing it in two steps keeps TMP's caret/mesh state
            // consistent and avoids the opener flickering or disappearing.
            _pendingCloser = Closers[opener];
            _pendingInsertAt = charIndex + 1;
            return addedChar;
        }

        int closer = Closers.IndexOf(addedChar);
        if (closer >= 0 && charIndex < text.Length && text[charIndex] == addedChar)
        {
            // The closing character is already where the caret is — just step over it.
            input.stringPosition = charIndex + 1;
            _pendingCloser = '\0';
            _pendingInsertAt = -1;
            return '\0';
        }

        _pendingCloser = '\0';
        _pendingInsertAt = -1;
        return addedChar;
    }

    void ApplyPendingAutoClose()
    {
        if (_pendingCloser == '\0' || input == null) return;

        int pos = input.stringPosition;
        if (pos != _pendingInsertAt)
        {
            _pendingCloser = '\0';
            _pendingInsertAt = -1;
            return;
        }

        string text = input.text;
        if (pos < 0) pos = 0;
        if (pos > text.Length) pos = text.Length;

        input.SetTextWithoutNotify(text.Insert(pos, _pendingCloser.ToString()));
        input.stringPosition = pos;

        _meshDirty = true;
        _dirty = true;
        _lintTimer = lintDelaySeconds;
        RefreshLineNumbers();
        RefreshHighlight();
        RequestAutocomplete();

        _pendingCloser = '\0';
        _pendingInsertAt = -1;
    }

    bool IsInStringOrComment(string text, int upTo)
    {
        bool inString = false;
        char quote = '\0';
        for (int i = 0; i < upTo && i < text.Length; i++)
        {
            char c = text[i];
            if (inString)
            {
                if (c == '\\' && i + 1 < text.Length) { i++; continue; }
                if (c == quote) { inString = false; quote = '\0'; }
            }
            else if (c == '"' || c == '\'')
            {
                inString = true;
                quote = c;
            }
            else if (c == '#')
            {
                return true; // comment runs to end of line; everything after is comment
            }
        }
        return inString;
    }

    // -------------------------------------------------------------------------

    public void SetScaffold(string scaffold)
    {
        if (input != null && string.IsNullOrEmpty(input.text))
        {
            input.SetTextWithoutNotify(scaffold ?? "");
            ComputeFoldRanges();
            RefreshLineNumbers();
            RefreshHighlight();
        }
    }

    public void SetSource(string source)
    {
        if (input == null) return;
        input.SetTextWithoutNotify(source ?? "");
        if (input.textComponent != null)
            input.stringPosition = input.text.Length;
        ComputeFoldRanges();
        RefreshLineNumbers();
        RefreshHighlight();
        Lint();
    }

    public void ConfigureAutocomplete(string[] allowedBlocks, string[] allowedQueries, string[] allowedReporters)
    {
        if (autocomplete != null)
            autocomplete.SetVocabulary(allowedBlocks, allowedQueries, allowedReporters);
    }

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
        ComputeFoldRanges();

        if (lintLabel == null) return;

        if (_errors.Count == 0)
            lintLabel.text = $"<color=#{ColorToHex(_theme.okColor)}>OK  looks good</color>";
        else
            lintLabel.text = $"<color=#{ColorToHex(_theme.errorColor)}>{_errors[0]}</color>";
    }

    // -------------------------------------------------------------------------
    // Line numbers + folding display

    public void RefreshLineNumbers()
    {
        if (lineNumbers == null || input == null) return;
        ApplyCodeTextLayout(lineNumbers);
        lineNumbers.text = BuildLineNumberText(LineCount());
        lineNumbers.ForceMeshUpdate();   // refresh metrics now so gutter icons line up
    }

    public string BuildLineNumberText(int count)
    {
        var sb = new StringBuilder();
        for (int i = 1; i <= count; i++)
        {
            if (IsLineFolded(i))
            {
                sb.Append("<color=#").Append(LineNumberColorHex(i)).Append(">...</color>");
                // Skip folded body lines so the gutter shows one entry per visible line.
                int skipEnd = i;
                foreach (FoldRange fr in _foldRanges)
                {
                    if (fr.HeaderLine == i)
                    {
                        skipEnd = Mathf.Max(skipEnd, fr.EndLine);
                        break;
                    }
                }
                i = skipEnd;
            }
            else
            {
                sb.Append("<color=#").Append(LineNumberColorHex(i)).Append('>').Append(i).Append("</color>");
            }
            sb.Append('\n');
        }
        return sb.ToString();
    }

    string LineNumberColorHex(int line)
    {
        foreach (LangError err in _errors)
            if (err.Line == line) return ColorToHex(_theme.errorColor);

        if (_heatHits != null && _heatHits.TryGetValue(line, out int hits) && hits > 0)
        {
            float t = Mathf.Clamp01(hits / 50f);
            Color c = Color.Lerp(_theme.heatColdColor, _theme.heatHotColor, t);
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
        ApplyCodeTextLayout(highlight);

        string src = input.text;
        if (_foldedLines.Count > 0)
        {
            // Replace folded body lines with spaces so the overlay still aligns.
            var sb = new StringBuilder(src.Length);
            int line = 1;
            for (int i = 0; i < src.Length; i++)
            {
                if (IsLineFolded(line) && line != CurrentFoldHeader(line))
                {
                    // Replace with space to preserve width; simpler than clipping.
                    sb.Append(' ');
                }
                else
                {
                    sb.Append(src[i]);
                }

                if (src[i] == '\n') line++;
            }
            src = sb.ToString();
        }

        highlight.text = Colorize(src);
        _meshDirty = true;
    }

    int CurrentFoldHeader(int line)
    {
        foreach (FoldRange fr in _foldRanges)
            if (line >= fr.StartLine && line <= fr.EndLine)
                return fr.HeaderLine;
        return -1;
    }

    bool IsLineFolded(int line)
    {
        foreach (FoldRange fr in _foldRanges)
        {
            if (fr.HeaderLine == line) return _foldedLines.Contains(line);
            if (_foldedLines.Contains(fr.HeaderLine) && line >= fr.StartLine && line <= fr.EndLine)
                return true;
        }
        return false;
    }

    int LineCount()
    {
        if (input == null) return 0;
        int count = 1;
        string text = input.text.Replace("\r\n", "\n").Replace('\r', '\n');
        for (int i = 0; i < text.Length; i++)
            if (text[i] == '\n') count++;
        return count;
    }

    // -------------------------------------------------------------------------
    // Folding

    public void ComputeFoldRanges()
    {
        _foldRanges.Clear();
        string text = input != null ? input.text : "";
        string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var indents = new int[lines.Length];
        var nonBlank = new bool[lines.Length];

        for (int i = 0; i < lines.Length; i++)
        {
            string lineText = lines[i];
            int indent = 0;
            while (indent < lineText.Length && lineText[indent] == ' ') indent++;
            indents[i] = indent;
            nonBlank[i] = lineText.Trim().Length > 0;
        }

        for (int i = 0; i < lines.Length - 1; i++)
        {
            string trimmed = StripComment(lines[i]).TrimEnd();
            if (!trimmed.EndsWith(":")) continue;
            if (trimmed.StartsWith("#")) continue;

            int body = NextNonBlankLine(lines, nonBlank, i + 1);
            if (body < 0 || indents[body] <= indents[i]) continue;

            int end = body;
            for (int j = body + 1; j < lines.Length; j++)
            {
                if (!nonBlank[j])
                {
                    end = j;
                    continue;
                }
                if (indents[j] <= indents[i]) break;
                end = j;
            }

            _foldRanges.Add(new FoldRange
            {
                HeaderLine = i + 1,
                StartLine = body + 1,
                EndLine = end + 1,
            });
        }

        // Remove folded lines for ranges that no longer exist.
        var toRemove = new List<int>();
        foreach (int header in _foldedLines)
        {
            bool found = false;
            foreach (FoldRange fr in _foldRanges)
            {
                if (fr.HeaderLine == header) { found = true; break; }
            }
            if (!found) toRemove.Add(header);
        }
        foreach (int h in toRemove) _foldedLines.Remove(h);
    }

    static int NextNonBlankLine(string[] lines, bool[] nonBlank, int start)
    {
        for (int i = start; i < lines.Length; i++)
            if (nonBlank[i]) return i;
        return -1;
    }

    static string StripComment(string line)
    {
        bool inString = false;
        char quote = '\0';
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inString)
            {
                if (c == '\\' && i + 1 < line.Length) { i++; continue; }
                if (c == quote) { inString = false; quote = '\0'; }
            }
            else if (c == '"' || c == '\'')
            {
                inString = true;
                quote = c;
            }
            else if (c == '#')
            {
                return line.Substring(0, i);
            }
        }
        return line;
    }

    public void ToggleFold(int headerLine)
    {
        if (_foldedLines.Contains(headerLine))
            _foldedLines.Remove(headerLine);
        else
            _foldedLines.Add(headerLine);

        RefreshLineNumbers();
        RefreshHighlight();
    }

    void PreventCaretInFoldedRegion()
    {
        if (input == null) return;
        int caret = input.stringPosition;
        int line = CharIndexToLine(caret);
        if (IsLineFolded(line))
        {
            // Move caret to the header line.
            foreach (FoldRange fr in _foldRanges)
            {
                if (line >= fr.StartLine && line <= fr.EndLine && _foldedLines.Contains(fr.HeaderLine))
                {
                    int headerEnd = LineEndIndex(fr.HeaderLine);
                    if (caret > headerEnd)
                    {
                        input.stringPosition = headerEnd;
                        input.selectionAnchorPosition = headerEnd;
                        input.selectionFocusPosition = headerEnd;
                    }
                    break;
                }
            }
        }
    }

    int CharIndexToLine(int index)
    {
        string text = input.text;
        int line = 1;
        for (int i = 0; i < index && i < text.Length; i++)
            if (text[i] == '\n') line++;
        return line;
    }

    int LineEndIndex(int line)
    {
        string text = input.text;
        int current = 1;
        for (int i = 0; i < text.Length; i++)
        {
            if (current == line && text[i] == '\n') return i;
            if (text[i] == '\n') current++;
        }
        return text.Length;
    }

    void UpdateFoldButtons()
    {
        if (gutterRoot == null) return;

        EnsureFoldButtons(_foldRanges.Count);
        for (int i = 0; i < _foldRanges.Count; i++)
        {
            FoldRange fr = _foldRanges[i];
            Button btn = _foldButtons[i];
            RectTransform rt = (RectTransform)btn.transform;
            PositionGutterObject(rt, fr.HeaderLine, 0f);

            TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = _foldedLines.Contains(fr.HeaderLine) ? "▸" : "▾";

            int header = fr.HeaderLine;
            if (label != null)
                label.text = _foldedLines.Contains(header) ? ">" : "v";
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => ToggleFold(header));
            btn.gameObject.SetActive(true);
        }

        for (int i = _foldRanges.Count; i < _foldButtons.Count; i++)
            _foldButtons[i].gameObject.SetActive(false);
    }

    void EnsureFoldButtons(int count)
    {
        while (_foldButtons.Count < count)
        {
            var go = new GameObject("FoldButton", typeof(RectTransform));
            go.transform.SetParent(gutterRoot, false);
            var btn = go.AddComponent<Button>();
            var img = go.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.1f, 0.6f);
            var txt = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            txt.transform.SetParent(go.transform, false);
            txt.rectTransform.anchorMin = Vector2.zero;
            txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.offsetMin = Vector2.zero;
            txt.rectTransform.offsetMax = Vector2.zero;
            txt.alignment = TextAlignmentOptions.Center;
            txt.fontSize = 16f;
            txt.color = Color.white;
            _foldButtons.Add(btn);
        }
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

        ExpandFoldContaining(line);
        execLineBar.gameObject.SetActive(true);
        _executingLine = line;
        UpdateExecLineBarPosition();
    }

    void ExpandFoldContaining(int line)
    {
        int headerToExpand = -1;
        foreach (FoldRange fr in _foldRanges)
        {
            if (_foldedLines.Contains(fr.HeaderLine) && line >= fr.StartLine && line <= fr.EndLine)
            {
                headerToExpand = fr.HeaderLine;
                break;
            }
        }

        if (headerToExpand > 0)
        {
            _foldedLines.Remove(headerToExpand);
            RefreshLineNumbers();
            RefreshHighlight();
        }
    }

    public void ClearExecutionHighlight()
    {
        _executingLine = -1;
        _executingMarkerLine = -1;
        if (execLineBar != null) execLineBar.gameObject.SetActive(false);
    }

    int _executingLine = -1;
    int _executingMarkerLine = -1;

    void UpdateExecLineBarPosition()
    {
        if (execLineBar == null || highlight == null || _executingLine < 1) return;
        if (highlight.textInfo == null || _executingLine > highlight.textInfo.lineCount) return;

        TMP_LineInfo lineInfo = highlight.textInfo.lineInfo[_executingLine - 1];
        RectTransform barRt = (RectTransform)execLineBar.transform;

        // Parent the bar to the highlight overlay itself — that layer is already
        // kept glyph-aligned and scrolled with the code (see SyncHighlightToInput),
        // so positioning the bar from the line metrics in the highlight's own local
        // space keeps it pinned to its line through scrolling, with no separate
        // scroll term to drift out of sync.
        if (barRt.parent != highlight.rectTransform) barRt.SetParent(highlight.rectTransform, false);
        barRt.anchorMin = new Vector2(0f, 1f);
        barRt.anchorMax = new Vector2(1f, 1f);
        barRt.pivot     = new Vector2(0.5f, 1f);

        barRt.anchoredPosition = new Vector2(0f, lineInfo.ascender);
        barRt.sizeDelta = new Vector2(0f, lineInfo.lineHeight);

        Color c = _theme.execBarColor;
        c.a = 0.35f + 0.15f * Mathf.Sin(Time.time * 3f);
        execLineBar.color = c;
    }

    // -------------------------------------------------------------------------
    // Squigglies

    void UpdateSquiggles()
    {
        if (squigglesRoot == null || highlight == null) return;

        int needed = 0;
        foreach (LangError err in _errors)
            if (err.Line > 0) needed++;

        EnsureSquiggles(needed);

        if (highlight.textInfo == null) return;
        int idx = 0;
        foreach (LangError err in _errors)
        {
            if (err.Line <= 0 || err.Line > highlight.textInfo.lineCount) continue;

            TMP_LineInfo lineInfo = highlight.textInfo.lineInfo[err.Line - 1];
            Image img = _squiggleImages[idx];
            RectTransform rt = (RectTransform)img.transform;

            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            // squigglesRoot is parented under the highlight overlay (which scrolls
            // with the code), so the underline sits in highlight-local space — no
            // separate scroll term needed.
            rt.anchoredPosition = new Vector2(0f, lineInfo.ascender - lineInfo.lineHeight + 2f);
            rt.sizeDelta = new Vector2(0f, 4f);
            img.color = _theme.errorColor;
            img.gameObject.SetActive(true);
            idx++;
        }

        for (int i = idx; i < _squiggleImages.Count; i++)
            _squiggleImages[i].gameObject.SetActive(false);
    }

    void EnsureSquiggles(int count)
    {
        while (_squiggleImages.Count < count)
        {
            var go = new GameObject("Squiggle", typeof(RectTransform));
            go.transform.SetParent(squigglesRoot, false);
            var img = go.AddComponent<Image>();
            img.sprite = squiggleSprite != null ? squiggleSprite : RuntimeSquiggle();
            img.type = Image.Type.Tiled;
            img.raycastTarget = false;
            _squiggleImages.Add(img);
        }
    }

    static Sprite _runtimeSquiggle;

    // A small repeating red wavy underline, generated once so squigglies render
    // even when no squiggle sprite asset is wired. Drawn white, tinted by errorColor.
    static Sprite RuntimeSquiggle()
    {
        if (_runtimeSquiggle != null) return _runtimeSquiggle;

        const int W = 8, H = 4;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false)
        {
            wrapMode   = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear,
        };
        var clear = new Color(1f, 1f, 1f, 0f);
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
                tex.SetPixel(x, y, clear);
        for (int x = 0; x < W; x++)
        {
            float wave = Mathf.Sin((float)x / W * Mathf.PI * 2f) * 0.5f + 0.5f;
            int yy = Mathf.Clamp(Mathf.RoundToInt(wave * (H - 1)), 0, H - 1);
            tex.SetPixel(x, yy, Color.white);
            if (yy + 1 < H) tex.SetPixel(x, yy + 1, Color.white);
        }
        tex.Apply();
        _runtimeSquiggle = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), W);
        return _runtimeSquiggle;
    }

    // -------------------------------------------------------------------------
    // Gutter icons

    public void SetGutterIcon(int line, GutterIconKind kind)
    {
        if (gutterRoot == null) return;

        if (kind == GutterIconKind.None)
        {
            if (_gutterIconByLine.TryGetValue(line, out Image img))
            {
                img.gameObject.SetActive(false);
                _gutterIconByLine.Remove(line);
            }
            return;
        }

        Image icon = EnsureGutterIcon(line);
        icon.color = kind == GutterIconKind.Error ? _theme.errorColor : _theme.okColor;

        TMP_Text label = icon.GetComponentInChildren<TMP_Text>();
        if (label != null)
            label.text = kind == GutterIconKind.Error ? "⚑" : "▶";

        if (label != null)
            label.text = kind == GutterIconKind.Error ? "!" : ">";

        icon.gameObject.SetActive(true);
        PositionGutterObject((RectTransform)icon.transform, line, 18f);
    }

    Image EnsureGutterIcon(int line)
    {
        if (_gutterIconByLine.TryGetValue(line, out Image img)) return img;

        var go = new GameObject($"GutterIcon_{line}", typeof(RectTransform));
        go.transform.SetParent(gutterRoot, false);
        var imgComp = go.AddComponent<Image>();
        imgComp.color = Color.clear;
        var txt = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        txt.transform.SetParent(go.transform, false);
        txt.rectTransform.anchorMin = Vector2.zero;
        txt.rectTransform.anchorMax = Vector2.one;
        txt.rectTransform.offsetMin = Vector2.zero;
        txt.rectTransform.offsetMax = Vector2.zero;
        txt.alignment = TextAlignmentOptions.Center;
        txt.fontSize = 18f;
        _gutterIconByLine[line] = imgComp;
        return imgComp;
    }

    void UpdateGutterIcons()
    {
        _visibleGutterIconLines.Clear();

        foreach (LangError err in _errors)
        {
            if (err.Line > 0)
            {
                SetGutterIcon(err.Line, GutterIconKind.Error);
                _visibleGutterIconLines.Add(err.Line);
            }
        }

        _executingMarkerLine = _executingLine;
        if (_executingMarkerLine > 0)
        {
            SetGutterIcon(_executingMarkerLine, GutterIconKind.Executing);
            _visibleGutterIconLines.Add(_executingMarkerLine);
        }

        var stale = new List<int>();
        foreach (int line in _gutterIconByLine.Keys)
            if (!_visibleGutterIconLines.Contains(line))
                stale.Add(line);

        foreach (int line in stale)
            SetGutterIcon(line, GutterIconKind.None);
    }

    void PositionGutterObject(RectTransform rt, int line, float xOffset)
    {
        if (lineNumbers == null || lineNumbers.textInfo == null) return;
        if (line < 1 || line > lineNumbers.textInfo.lineCount) return;

        TMP_LineInfo lineInfo = lineNumbers.textInfo.lineInfo[line - 1];
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(20f, lineInfo.lineHeight);
        rt.anchoredPosition = new Vector2(xOffset, lineInfo.ascender);
    }

    public enum GutterIconKind { None, Error, Executing }

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
    // Autocomplete

    void RequestAutocomplete()
    {
        if (autocomplete == null || input == null) return;

        int caret = input.stringPosition;
        string text = input.text;
        string prefix = ExtractWordPrefix(text, caret);

        if (prefix.Length >= 1 && char.IsLetter(prefix[0]))
        {
            var vars = new List<string>();
            CollectVariableNames(text, vars);
            var funcs = new List<string>();
            CollectFunctionNames(text, funcs);
            autocomplete.Show(caret, prefix, vars, funcs);
        }
        else
        {
            autocomplete.Hide();
        }
    }

    void HideAutocomplete()
    {
        if (autocomplete != null) autocomplete.Hide();
    }

    string ExtractWordPrefix(string text, int caret)
    {
        int start = caret;
        while (start > 0 && IsWordChar(text[start - 1])) start--;
        return text.Substring(start, caret - start);
    }

    void CollectVariableNames(string text, List<string> names)
    {
        var seen = new HashSet<string>();
        var lines = text.Split('\n');
        foreach (string line in lines)
        {
            string trimmed = line.Trim();
            int eq = trimmed.IndexOf('=');
            if (eq > 0)
            {
                string lhs = trimmed.Substring(0, eq).Trim();
                if (lhs.IndexOfAny(new char[] { '[', '(', ')' }) < 0 && !seen.Contains(lhs))
                {
                    seen.Add(lhs);
                    names.Add(lhs);
                }
            }
        }
    }

    void CollectFunctionNames(string text, List<string> names)
    {
        var seen = new HashSet<string>();
        var lines = text.Split('\n');
        foreach (string line in lines)
        {
            string trimmed = line.TrimStart();
            if (!trimmed.StartsWith("def ")) continue;

            int start = 4;
            int end = start;
            while (end < trimmed.Length && IsWordChar(trimmed[end])) end++;
            if (end <= start) continue;

            string name = trimmed.Substring(start, end - start);
            if (seen.Add(name))
                names.Add(name);
        }
    }

    // -------------------------------------------------------------------------
    // Syntax colouring

    string Colorize(string src)
    {
        if (string.IsNullOrEmpty(src)) return "";

        var sb = new StringBuilder(src.Length + 64);
        int i = 0;
        bool inString = false;
        char quote = '\0';

        while (i < src.Length)
        {
            char c = src[i];

            if (inString)
            {
                // Consume the rest of the string literal; a '#' here is just text.
                int j = i;
                while (j < src.Length && src[j] != quote && src[j] != '\n')
                {
                    if (src[j] == '\\' && j + 1 < src.Length) j++;   // skip escaped char
                    j++;
                }
                if (j < src.Length && src[j] == quote) j++;            // include closing quote
                AppendRun(sb, src, i - 1, j - i + 1, ColorToHex(_theme.stringColor));
                i = j;
                inString = false;
                continue;
            }

            if (c == '"' || c == '\'')                     // string literal start
            {
                inString = true;
                quote = c;
                i++;
                continue;
            }

            if (c == '#')                                   // real comment to end of line
            {
                int j = i;
                while (j < src.Length && src[j] != '\n') j++;
                AppendRun(sb, src, i, j - i, ColorToHex(_theme.commentColor));
                i = j;
            }
            else if (char.IsDigit(c))                       // number literal
            {
                int j = i;
                while (j < src.Length && (char.IsDigit(src[j]) || src[j] == '.')) j++;
                AppendRun(sb, src, i, j - i, ColorToHex(_theme.numberColor));
                i = j;
            }
            else if (IsWordChar(c))                         // identifier / keyword
            {
                int j = i;
                while (j < src.Length && IsWordChar(src[j])) j++;
                string word = src.Substring(i, j - i);

                int k = j;                                  // a following '(' marks a call
                while (k < src.Length && (src[k] == ' ' || src[k] == '\t')) k++;
                bool isCall = k < src.Length && src[k] == '(';

                AppendRun(sb, src, i, j - i, WordColor(word, isCall));
                i = j;
            }
            else
            {
                if (c == '<') sb.Append("<noparse><</noparse>");
                else          sb.Append(c);
                i++;
            }
        }
        return sb.ToString();
    }

    // Appends src[start..start+len) wrapped in a colour tag (when hex != null),
    // escaping '<' so user text can never break the rich-text overlay.
    static void AppendRun(StringBuilder sb, string src, int start, int len, string hex)
    {
        if (hex != null) sb.Append("<color=#").Append(hex).Append('>');
        int end = start + len;
        for (int p = start; p < end && p < src.Length; p++)
        {
            char ch = src[p];
            if (ch == '<') sb.Append("<noparse><</noparse>");
            else           sb.Append(ch);
        }
        if (hex != null) sb.Append("</color>");
    }

    // Built-in functions coloured like VS Code's function-call yellow.
    static readonly HashSet<string> Builtins = new HashSet<string>
    {
        "print", "len", "append", "pop", "range", "int", "str", "float",
        "min", "max", "sum", "sorted", "randint", "abs", "round",
    };

    // VS Code "Dark+" token mapping: control keywords magenta, literal constants
    // blue, actions/builtins/calls function-yellow, queries/reporters type-teal,
    // and every other identifier the light-blue variable colour.
    string WordColor(string word, bool isCall)
    {
        switch (word)
        {
            case "if": case "elif": case "else": case "while":
            case "for": case "in": case "def": case "return":
            case "break": case "continue": case "not":
            case "and": case "or": case "repeat":
                return ColorToHex(_theme.keywordColor);
            case "True": case "False": case "None":
                return ColorToHex(_theme.constantColor);
        }
        if (AgentApi.IsAction(word))   return ColorToHex(_theme.actionColor);
        if (AgentApi.IsQuery(word))    return ColorToHex(_theme.queryColor);
        if (AgentApi.IsReporter(word)) return ColorToHex(_theme.queryColor);
        if (Builtins.Contains(word))   return ColorToHex(_theme.actionColor);
        if (isCall)                    return ColorToHex(_theme.actionColor);
        return ColorToHex(_theme.variableColor);
    }

    static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';

    static string ColorToHex(Color c)
    {
        return $"{Mathf.RoundToInt(c.r * 255):X2}{Mathf.RoundToInt(c.g * 255):X2}{Mathf.RoundToInt(c.b * 255):X2}";
    }
}
