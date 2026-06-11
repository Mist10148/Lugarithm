using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Self-wiring toggle for the persistent Almanac overlay. Drop on any Button
/// in a gameplay scene; it calls AlmanacManager.Instance.Toggle() on click.
/// </summary>
[RequireComponent(typeof(Button))]
public class AlmanacToggleButton : MonoBehaviour
{
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() => AlmanacManager.Instance?.Toggle());
    }
}
