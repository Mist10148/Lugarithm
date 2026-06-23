using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menu-only press feedback that darkens the button briefly, then restores
/// the light state before invoking the existing action. This keeps the menu
/// tactile without coupling the helper to editor-time sprite wiring.
/// </summary>
public sealed class MenuButtonPressFlash : MonoBehaviour
{
    Button _button;
    Image _face;
    TextMeshProUGUI _caption;
    Image _icon;
    Coroutine _routine;

    Color _lightFaceColor = Color.white;
    Color _darkFaceColor = new Color(0.78f, 0.78f, 0.78f, 1f);
    Color _enabledCaptionColor = Color.white;
    Color _enabledIconColor = Color.white;
    Color _disabledCaptionColor = Color.white;
    Color _disabledIconColor = new Color(0.58f, 0.58f, 0.58f, 1f);
    Color _disabledFaceColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    bool _lastInteractable;

    const float DarkHoldSeconds = 0.055f;
    const float LightHoldSeconds = 0.03f;

    void Awake()
    {
        ResolveReferences();
        CaptureBaseColors();
        SyncState(true);
    }

    void OnEnable()
    {
        ResolveReferences();
        CaptureBaseColors();
        SyncState(true);
    }

    void Update()
    {
        SyncState(false);
    }

    public void Play(Action onActivated)
    {
        ResolveReferences();

        if (_button != null && !_button.interactable)
            return;

        if (_routine != null)
            StopCoroutine(_routine);

        _routine = StartCoroutine(PressRoutine(onActivated));
    }

    void ResolveReferences()
    {
        if (_button == null)
            _button = GetComponent<Button>();

        if (_face == null && _button != null)
            _face = _button.image;

        if (_caption == null)
        {
            _caption = GetComponentInChildren<TextMeshProUGUI>(true);
            if (_caption == null && transform.parent != null)
            {
                string captionName = _button != null
                    ? _button.name.Replace("Button", "Caption")
                    : $"{gameObject.name}Caption";

                var captionTransform = transform.parent.Find(captionName);
                if (captionTransform != null)
                    _caption = captionTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        if (_icon == null)
        {
            var iconTransform = transform.Find("Icon");
            _icon = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
        }
    }

    void CaptureBaseColors()
    {
        if (_face != null)
        {
            _lightFaceColor = _face.color;
            _disabledFaceColor = new Color(_lightFaceColor.r * 0.72f,
                                           _lightFaceColor.g * 0.72f,
                                           _lightFaceColor.b * 0.72f,
                                           _lightFaceColor.a);
        }

        if (_caption != null)
        {
            _enabledCaptionColor = _caption.color;
            _disabledCaptionColor = new Color(_enabledCaptionColor.r * 0.58f,
                                              _enabledCaptionColor.g * 0.58f,
                                              _enabledCaptionColor.b * 0.58f,
                                              _enabledCaptionColor.a * 0.75f);
        }

        if (_icon != null)
            _enabledIconColor = _icon.color;

        if (_button != null)
        {
            ColorBlock colors = _button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.62f, 0.62f, 0.62f, 0.65f);
            _button.colors = colors;
            _button.transition = Selectable.Transition.ColorTint;
        }
    }

    IEnumerator PressRoutine(Action onActivated)
    {
        if (_face != null)
            _face.color = _darkFaceColor;

        yield return new WaitForSecondsRealtime(DarkHoldSeconds);

        if (_face != null)
            _face.color = _lightFaceColor;

        yield return new WaitForSecondsRealtime(LightHoldSeconds);

        onActivated?.Invoke();
        _routine = null;
    }

    void SyncState(bool force)
    {
        if (_button == null)
            return;

        bool interactable = _button.interactable;
        if (!force && interactable == _lastInteractable)
            return;

        _lastInteractable = interactable;

        if (_face != null)
            _face.color = interactable ? _lightFaceColor : _disabledFaceColor;

        if (_caption != null)
            _caption.color = interactable ? _enabledCaptionColor : _disabledCaptionColor;

        if (_icon != null)
            _icon.color = interactable ? _enabledIconColor : _disabledIconColor;
    }
}
