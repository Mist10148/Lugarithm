using UnityEngine;
using UnityEngine.InputSystem;
using System;

/// <summary>
/// A trigger zone placed on the tilemap for NPCs, jeep stops, exits, and any
/// interactable object. Shows a prompt overlay when the player enters the zone
/// and fires an event when the player presses E / interact.
/// </summary>
public class InteractionTrigger : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Config
    // -------------------------------------------------------------------------

    [Header("Type")]
    [Tooltip("What kind of entity this trigger represents.")]
    [SerializeField] private EntityType entityType = EntityType.Npc;

    [Header("Display")]
    [Tooltip("Name shown in the interaction prompt (e.g. 'Talk to Maria').")]
    [SerializeField] private string promptLabel = "Interact";

    [Tooltip("The prompt text shown near the player when in range.")]
    [SerializeField] private string promptText = "Press E to interact";

    [Header("Visual")]
    [Tooltip("Indicator sprite rendered above the entity (e.g. exclamation mark).")]
    [SerializeField] private SpriteRenderer indicatorSprite;

    [Tooltip("NPC body sprite (optional — rendered at the entity position).")]
    [SerializeField] private SpriteRenderer npcSprite;

    // -------------------------------------------------------------------------
    // Events
    // -------------------------------------------------------------------------

    /// <summary>Fired when the player presses interact while in range.</summary>
    public event Action<InteractionTrigger> OnInteracted;

    /// <summary>Fired when the player enters the trigger zone.</summary>
    public event Action<InteractionTrigger> OnPlayerEntered;

    /// <summary>Fired when the player exits the trigger zone.</summary>
    public event Action<InteractionTrigger> OnPlayerExited;

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private bool _playerInRange;

    public EntityType EntityType => entityType;
    public string PromptLabel => promptLabel;
    public string PromptText => promptText;

    /// <summary>True while the player's collider overlaps this trigger.</summary>
    public bool PlayerInRange => _playerInRange;

    // -------------------------------------------------------------------------

    /// <summary>Initializes trigger config (called by the level controller at spawn time).</summary>
    public void Init(EntityType type, string label, string prompt)
    {
        entityType  = type;
        promptLabel = label;
        promptText  = prompt;
    }

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPlayerCollider(other))
        {
            _playerInRange = true;
            if (indicatorSprite != null) indicatorSprite.enabled = true;
            OnPlayerEntered?.Invoke(this);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (IsPlayerCollider(other))
        {
            _playerInRange = false;
            if (indicatorSprite != null) indicatorSprite.enabled = false;
            OnPlayerExited?.Invoke(this);
        }
    }

    void Update()
    {
        if (!_playerInRange) return;

        // Check for interact key (E key)
        var kb = Keyboard.current;
        if (kb != null && kb.eKey.wasPressedThisFrame)
        {
            OnInteracted?.Invoke(this);
        }
    }

    // -------------------------------------------------------------------------

    /// <summary>Manually set indicator visibility (for cutscenes, etc.).</summary>
    public void SetIndicatorVisible(bool visible)
    {
        if (indicatorSprite != null)
            indicatorSprite.enabled = visible && _playerInRange;
    }

    static bool IsPlayerCollider(Collider2D col)
    {
        // Check by component — the player has TopDownPlayerController
        return col.GetComponentInParent<TopDownPlayerController>() != null;
    }
}
