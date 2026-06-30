using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Scrolling execution console (bottom of the Automation workspace): action
/// log, warnings, and plain-English errors. Lines are cloned from an inactive
/// template built into the scene (keeps fonts/styling out of code).
/// </summary>
public class ConsoleController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;
    [SerializeField] private TMP_Text lineTemplate;

    [Header("Limits")]
    [SerializeField] private int maxLines = 90;

    static readonly Color InfoColor  = new Color(0.78f, 0.80f, 0.78f);
    static readonly Color WarnColor  = new Color(0.95f, 0.78f, 0.30f);
    static readonly Color ErrorColor = new Color(0.95f, 0.40f, 0.35f);
    static readonly Color PrintColor = new Color(0.92f, 0.95f, 1f);

    readonly Queue<TMP_Text> _lines = new Queue<TMP_Text>();

    // -------------------------------------------------------------------------

    public void Info(string message)  => AddLine(message, InfoColor);
    public void Warn(string message)  => AddLine("⚠  " + message, WarnColor);
    public void Error(string message) => AddLine("✖  " + message, ErrorColor);

    /// <summary>Program stdout — a <c>print()</c> line, rendered bright like a
    /// real terminal so it stands out from the action log.</summary>
    public void Print(string message) => AddLine("» " + message, PrintColor);

    public void Clear()
    {
        while (_lines.Count > 0)
            Destroy(_lines.Dequeue().gameObject);
    }

    // -------------------------------------------------------------------------

    void AddLine(string message, Color color)
    {
        if (lineTemplate == null || content == null) return;

        TMP_Text line = Instantiate(lineTemplate, content);
        line.gameObject.SetActive(true);
        line.text  = message;
        line.color = color;
        _lines.Enqueue(line);

        while (_lines.Count > maxLines)
            Destroy(_lines.Dequeue().gameObject);

        // Stick to the bottom on the next layout pass.
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;
    }
}
