using TMPro;
using UnityEngine;

/// <summary>
/// Attach to any TMP label to make its text follow the active UI language. The
/// builders set the <see cref="key"/> (via SceneBuilderUtil.Wire) and seed the
/// English text at build time; at runtime this re-renders on enable and whenever
/// <see cref="LocalizationManager.OnLanguageChanged"/> fires, so flipping Language
/// in Settings updates every visible label live.
/// </summary>
public class LocalizedLabel : MonoBehaviour
{
    [SerializeField] private string key;

    TMP_Text _label;

    void Awake() => _label = GetComponent<TMP_Text>();

    void OnEnable()
    {
        Apply();
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged += Apply;
    }

    void OnDisable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= Apply;
    }

    /// <summary>Re-points this label at a different key and refreshes immediately.</summary>
    public void SetKey(string newKey)
    {
        key = newKey;
        Apply();
    }

    public void Apply()
    {
        if (_label == null) _label = GetComponent<TMP_Text>();
        if (_label != null) _label.text = LocalizationManager.Translate(key);
    }
}
