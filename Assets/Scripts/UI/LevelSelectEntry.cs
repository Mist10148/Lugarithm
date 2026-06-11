using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One row on the Level Select screen. Dumb view — <see cref="LevelSelectManager"/>
/// decides what it shows and what clicking does.
/// </summary>
public class LevelSelectEntry : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text   nameLabel;
    [SerializeField] private TMP_Text   statusLabel;
    [SerializeField] private TMP_Text   bestLabel;
    [SerializeField] private GameObject lockOverlay;
    [SerializeField] private Button     button;

    // -------------------------------------------------------------------------

    public void Setup(string levelName, string status, string best,
                      bool locked, bool interactable, Action onClick)
    {
        if (nameLabel   != null) nameLabel.text   = levelName;
        if (statusLabel != null) statusLabel.text = status;
        if (bestLabel   != null) bestLabel.text   = best;
        if (lockOverlay != null) lockOverlay.SetActive(locked);

        if (button != null)
        {
            button.interactable = interactable;
            button.onClick.RemoveAllListeners();
            if (onClick != null)
                button.onClick.AddListener(() => onClick());
        }
    }
}
