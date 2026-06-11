using UnityEngine;

/// <summary>
/// Persistent singleton for the in-game Almanac / Journal overlay.
/// Lives in Bootstrap.unity and survives every scene load; any scene can call
/// <see cref="Open"/>, <see cref="Close"/>, or <see cref="Toggle"/>.
/// </summary>
public class AlmanacManager : MonoBehaviour
{
    public static AlmanacManager Instance { get; private set; }

    [SerializeField] private AlmanacController controller;

    // -------------------------------------------------------------------------

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // -------------------------------------------------------------------------

    public void Open()   => controller?.Open();
    public void Close()  => controller?.Close();
    public void Toggle() => controller?.Toggle();

    public bool IsOpen => controller != null && controller.IsOpen;
}
