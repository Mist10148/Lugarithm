using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A small taskbar of buttons that reopen/focus floating editor windows, so a
/// closed or minimized window is never lost. The scene builder registers each
/// window; clicking a dock button opens and brings that window to the front.
/// </summary>
public class WindowDock : MonoBehaviour
{
    [SerializeField] private RectTransform content;
    [SerializeField] private Button buttonTemplate;

    readonly List<EditorWindowController> _windows = new List<EditorWindowController>();

    public void Register(EditorWindowController win, string label)
    {
        if (win == null || content == null || buttonTemplate == null) return;

        _windows.Add(win);

        Button b = Instantiate(buttonTemplate, content);
        b.gameObject.SetActive(true);
        var t = b.GetComponentInChildren<TMP_Text>(true);
        if (t != null) t.text = label;
        b.onClick.AddListener(win.Open);
    }
}
