using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Scrolling execution console: action log, warnings, plain-English errors, and
/// print() output. Rows are pooled so fast-running programs do not allocate a
/// fresh TMP object for every line forever.
/// </summary>
public class ConsoleController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;
    [SerializeField] private TMP_Text lineTemplate;

    [Header("Limits")]
    [SerializeField] private int maxLines = 90;
    [SerializeField] private float minLineHeight = 26f;

    static readonly Color InfoColor  = new Color(0.78f, 0.80f, 0.78f);
    static readonly Color WarnColor  = new Color(0.95f, 0.78f, 0.30f);
    static readonly Color ErrorColor = new Color(0.95f, 0.40f, 0.35f);
    static readonly Color PrintColor = new Color(0.92f, 0.95f, 1f);

    readonly Queue<TMP_Text> _lines = new Queue<TMP_Text>();
    readonly Stack<TMP_Text> _pool = new Stack<TMP_Text>();
    Coroutine _scrollRoutine;

    public int VisibleLineCount => _lines.Count;

    public void Info(string message)  => AddLine(message, InfoColor);
    public void Warn(string message)  => AddLine("!  " + message, WarnColor);
    public void Error(string message) => AddLine("x  " + message, ErrorColor);

    /// <summary>Program stdout: a print() line.</summary>
    public void Print(string message) => AddLine("> " + message, PrintColor);

    public void Clear()
    {
        while (_lines.Count > 0)
            ReturnLine(_lines.Dequeue());
        RequestAutoscroll();
    }

    void AddLine(string message, Color color)
    {
        string normalized = (message ?? "").Replace("\r\n", "\n").Replace('\r', '\n');
        string[] rows = normalized.Split('\n');
        if (rows.Length == 0)
        {
            AddSingleLine("", color);
            return;
        }

        for (int i = 0; i < rows.Length; i++)
            AddSingleLine(rows[i], color);
    }

    void AddSingleLine(string message, Color color)
    {
        if (lineTemplate == null || content == null) return;

        TMP_Text line = TakeLine();
        line.text  = message ?? "";
        line.color = color;
        line.gameObject.SetActive(true);
        line.transform.SetAsLastSibling();
        ApplyPreferredHeight(line);
        _lines.Enqueue(line);

        while (_lines.Count > maxLines)
            ReturnLine(_lines.Dequeue());

        LayoutRebuilder.MarkLayoutForRebuild(content);
        RequestAutoscroll();
    }

    TMP_Text TakeLine()
    {
        TMP_Text line = _pool.Count > 0 ? _pool.Pop() : Instantiate(lineTemplate, content);
        line.transform.SetParent(content, false);
        ConfigureLine(line);
        return line;
    }

    void ConfigureLine(TMP_Text line)
    {
        if (line == null) return;

        RectTransform rt = (RectTransform)line.transform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.offsetMin = new Vector2(0f, rt.offsetMin.y);
        rt.offsetMax = new Vector2(0f, rt.offsetMax.y);

        line.alignment = TextAlignmentOptions.MidlineLeft;
        line.textWrappingMode = TextWrappingModes.NoWrap;
        line.overflowMode = TextOverflowModes.Masking;
        line.richText = true;
        line.raycastTarget = false;
        line.margin = Vector4.zero;
        line.lineSpacing = 0f;
    }

    void ReturnLine(TMP_Text line)
    {
        if (line == null) return;
        line.text = "";
        line.gameObject.SetActive(false);
        _pool.Push(line);
    }

    void ApplyPreferredHeight(TMP_Text line)
    {
        ConfigureLine(line);

        LayoutElement layout = line.GetComponent<LayoutElement>();
        if (layout == null) layout = line.gameObject.AddComponent<LayoutElement>();

        float preferred = Mathf.Max(24f, minLineHeight);
        layout.minHeight = minLineHeight;
        layout.preferredHeight = preferred;
        layout.flexibleHeight = 0f;
    }

    void RequestAutoscroll()
    {
        if (!isActiveAndEnabled || scrollRect == null) return;
        if (_scrollRoutine != null) StopCoroutine(_scrollRoutine);
        _scrollRoutine = StartCoroutine(ScrollToBottomNextFrame());
    }

    IEnumerator ScrollToBottomNextFrame()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        if (content != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;
        _scrollRoutine = null;
    }
}
