using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tiny glue: clicking the button loads a scene (through the transition
/// manager when present). Used for Back / Exit buttons everywhere.
/// </summary>
public class SceneLink : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button button;

    [Header("Target")]
    [SerializeField] private string sceneName = "LevelSelect";

    // -------------------------------------------------------------------------

    void Start()
    {
        if (button != null)
            button.onClick.AddListener(Go);
    }

    public void Go()
    {
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.TransitionTo(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }
}
