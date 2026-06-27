using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Lightweight IntelliSense dropdown for the code editor. Triggered by typing an
/// identifier prefix; offers AgentApi actions/queries/reporters, language
/// keywords, and in-scope variable names. Navigate with Up/Down, accept with
/// Tab/Enter, dismiss with Esc.
/// </summary>
public class CodeAutocompleteController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject root;
    [SerializeField] private RectTransform content;
    [SerializeField] private GameObject rowTemplate;
    public TMP_InputField input;
    public TMP_Text highlight;

    [Header("Theme")]
    public Color actionColor;
    public Color queryColor;
    public Color keywordColor;
    public Color varColor;
    public Color selectedColor;

    readonly List<Row> _rows = new List<Row>();
    readonly List<Suggestion> _suggestions = new List<Suggestion>();

    int _selectedIndex;
    bool _visible;
    int _replaceStart;
    int _replaceEnd;

    struct Suggestion
    {
        public string Text;
        public string Kind;
        public string Insert;
    }

    class Row
    {
        public GameObject Root;
        public TMP_Text Label;
        public Image Bg;
    }

    public bool Visible => _visible;

    void Update()
    {
        if (!_visible) return;

        if (Input.GetKeyDown(KeyCode.DownArrow)) MoveSelection(1);
        if (Input.GetKeyDown(KeyCode.UpArrow))   MoveSelection(-1);

        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Return))
        {
            Accept();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Hide();
            return;
        }
    }

    // -------------------------------------------------------------------------

    public void Show(int caret, string prefix, List<string> variables)
    {
        if (root == null || rowTemplate == null || input == null) return;

        _suggestions.Clear();

        // Agent entries.
        foreach (ApiEntry e in AgentApi.Entries)
        {
            if (e.Name.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
            {
                string insert = e.Name == "giveChange"
                    ? "giveChange(changeOwed())"
                    : e.Name + "()";
                _suggestions.Add(new Suggestion { Text = e.Name, Kind = e.Kind.ToString(), Insert = insert });
            }
        }

        // Keywords.
        string[] keywords = { "if", "else", "elif", "while", "for", "in", "def", "return", "break", "continue", "not", "and", "or", "True", "False", "None" };
        foreach (string kw in keywords)
        {
            if (kw.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                _suggestions.Add(new Suggestion { Text = kw, Kind = "keyword", Insert = kw });
        }

        // Variables.
        if (variables != null)
        {
            foreach (string v in variables)
            {
                if (v.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                    _suggestions.Add(new Suggestion { Text = v, Kind = "var", Insert = v });
            }
        }

        if (_suggestions.Count == 0)
        {
            Hide();
            return;
        }

        _replaceStart = caret - prefix.Length;
        _replaceEnd = caret;
        _selectedIndex = 0;
        _visible = true;
        root.SetActive(true);
        root.transform.SetAsLastSibling();   // float above other canvas children

        EnsureRows(_suggestions.Count);
        PopulateRows();
        PositionNearCaret();
    }

    public void Hide()
    {
        _visible = false;
        if (root != null) root.SetActive(false);
    }

    void EnsureRows(int count)
    {
        while (_rows.Count < count)
        {
            GameObject go = Instantiate(rowTemplate, content, false);
            go.SetActive(true);
            var row = new Row
            {
                Root = go,
                Label = go.GetComponentInChildren<TMP_Text>(),
                Bg = go.GetComponentInChildren<Image>(),
            };
            int idx = _rows.Count;
            Button btn = go.GetComponent<Button>();
            if (btn == null) btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => { _selectedIndex = idx; Accept(); });
            _rows.Add(row);
        }

        for (int i = 0; i < _rows.Count; i++)
            _rows[i].Root.SetActive(i < count);
    }

    void PopulateRows()
    {
        for (int i = 0; i < _suggestions.Count; i++)
        {
            Suggestion s = _suggestions[i];
            Row r = _rows[i];
            r.Label.text = $"<color=#{KindColorHex(s.Kind)}>{s.Text}</color>  <size=16><color=#888888>{s.Kind}</color></size>";
            r.Bg.color = i == _selectedIndex ? selectedColor : new Color(0.08f, 0.09f, 0.12f, 0.95f);
        }
    }

    void MoveSelection(int delta)
    {
        _selectedIndex = (_selectedIndex + delta + _suggestions.Count) % _suggestions.Count;
        PopulateRows();
    }

    void Accept()
    {
        if (!_visible || _suggestions.Count == 0 || input == null) return;

        Suggestion s = _suggestions[_selectedIndex];
        string text = input.text;
        string before = _replaceStart > 0 ? text.Substring(0, _replaceStart) : "";
        string after = _replaceEnd < text.Length ? text.Substring(_replaceEnd) : "";

        string insert = s.Insert;
        // Place caret inside parens for calls.
        int caretOffset = insert.Length;
        if (insert.EndsWith("()")) caretOffset = insert.Length - 1;

        input.SetTextWithoutNotify(before + insert + after);
        input.stringPosition = _replaceStart + caretOffset;
        input.ForceLabelUpdate();
        input.onValueChanged.Invoke(input.text);   // re-highlight/lint the accepted text

        Hide();
    }

    void PositionNearCaret()
    {
        if (highlight == null || input == null) return;

        highlight.ForceMeshUpdate();
        TMP_TextInfo info = highlight.textInfo;
        int charIndex = Mathf.Clamp(input.stringPosition, 0, Mathf.Max(0, info.characterCount - 1));

        Vector3 worldPos = Vector3.zero;
        if (info.characterCount > 0)
        {
            TMP_CharacterInfo c = info.characterInfo[charIndex];
            worldPos = highlight.transform.TransformPoint(c.topRight);
        }

        RectTransform rootRt = (RectTransform)root.transform;
        RectTransform canvasRt = (RectTransform)rootRt.GetComponentInParent<Canvas>().transform;
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, RectTransformUtility.WorldToScreenPoint(null, worldPos), null, out local);

        rootRt.anchoredPosition = local + new Vector2(0f, -24f);
    }

    string KindColorHex(string kind)
    {
        switch (kind)
        {
            case "Action": return ColorToHex(actionColor);
            case "Query":  return ColorToHex(queryColor);
            case "Reporter": return ColorToHex(queryColor);
            case "keyword": return ColorToHex(keywordColor);
            case "var": return ColorToHex(varColor);
            default: return "FFFFFF";
        }
    }

    static string ColorToHex(Color c)
    {
        return $"{Mathf.RoundToInt(c.r * 255):X2}{Mathf.RoundToInt(c.g * 255):X2}{Mathf.RoundToInt(c.b * 255):X2}";
    }
}
