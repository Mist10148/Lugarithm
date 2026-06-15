using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Persistent "Finish leg" button shown once the player has delivered their
/// committed passengers. Pressing it ends the leg (town gate → reveal → results).
/// Hidden until the leg is completable.
/// </summary>
public class LegCompletionController : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Button     finishButton;

    public bool IsVisible => root != null && root.activeSelf;

    public event Action OnFinishPressed;

    void Start()
    {
        if (root != null) root.SetActive(false);
        if (finishButton != null)
            finishButton.onClick.AddListener(() => OnFinishPressed?.Invoke());
    }

    void OnDestroy()
    {
        if (finishButton != null)
            finishButton.onClick.RemoveAllListeners();
    }

    public void Show()
    {
        if (root != null) root.SetActive(true);
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);
    }
}
