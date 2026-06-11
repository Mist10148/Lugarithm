using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Code Mode text editor (hard mode): a multiline input field with a synced
/// line-number column and debounced lint — parse problems surface in the
/// lint label and console in plain English while typing.
/// </summary>
public class CodeEditorController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_InputField input;
    [SerializeField] private TMP_Text lineNumbers;
    [SerializeField] private TMP_Text lintLabel;

    [Header("Lint")]
    [SerializeField] private float lintDelaySeconds = 0.5f;

    float _lintTimer;
    bool  _dirty;

    public string Source => input != null ? input.text : "";

    // -------------------------------------------------------------------------

    void Start()
    {
        if (input != null)
            input.onValueChanged.AddListener(_ => { _dirty = true; _lintTimer = lintDelaySeconds; });

        RefreshLineNumbers();
    }

    void Update()
    {
        if (!_dirty) return;

        RefreshLineNumbers();

        _lintTimer -= Time.deltaTime;
        if (_lintTimer <= 0f)
        {
            _dirty = false;
            Lint();
        }
    }

    // -------------------------------------------------------------------------

    /// <summary>Pre-fills the goal scaffold (only when the editor is empty).</summary>
    public void SetScaffold(string scaffold)
    {
        if (input != null && string.IsNullOrEmpty(input.text))
        {
            input.SetTextWithoutNotify(scaffold ?? "");
            RefreshLineNumbers();
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

        if (lintLabel == null) return;

        if (errors.Count == 0)
        {
            lintLabel.text  = "✓  looks good";
            lintLabel.color = new Color(0.45f, 0.85f, 0.45f);
        }
        else
        {
            lintLabel.text  = errors[0].ToString();
            lintLabel.color = new Color(0.95f, 0.45f, 0.40f);
        }
    }

    void RefreshLineNumbers()
    {
        if (lineNumbers == null || input == null) return;

        int count = 1;
        string text = input.text;
        for (int i = 0; i < text.Length; i++)
            if (text[i] == '\n') count++;

        var sb = new System.Text.StringBuilder();
        for (int i = 1; i <= count; i++)
            sb.Append(i).Append('\n');

        lineNumbers.text = sb.ToString();
    }
}
