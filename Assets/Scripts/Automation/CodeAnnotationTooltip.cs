using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class CodeAnnotationTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    GameObject _root;
    TMP_Text _label;
    string _text;
    bool _pinned;

    public void Configure(GameObject root, TMP_Text label, string text)
    {
        _root = root;
        _label = label;
        _text = text;
    }

    public void OnPointerEnter(PointerEventData eventData) => Show();
    public void OnPointerExit(PointerEventData eventData) { if (!_pinned) Hide(); }
    public void OnPointerClick(PointerEventData eventData)
    {
        _pinned = !_pinned;
        if (_pinned) Show(); else Hide();
    }

    void Show()
    {
        if (_label != null) _label.text = _text;
        if (_root != null) _root.SetActive(true);
    }

    void Hide() { if (_root != null) _root.SetActive(false); }
}
