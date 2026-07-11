using UnityEngine;

/// <summary>
/// Shared loader for the top-down "Townspeople" NPC art (the same character
/// sprites/animator controllers used by the walk-around town). Reused to give the
/// passengers waiting at drive-mode stops real townsfolk figures instead of the
/// placeholder blob. Presentation only — spawns no colliders or physics.
/// </summary>
public static class TownNpcVisuals
{
    // Character variants that ship with an NPC animator folder under
    // Resources/TutorialCharacters. 13 is the player's own character, so it is
    // deliberately left out of the waiting-crowd rotation.
    static readonly int[] WaitingVariants = { 5, 3, 15 };

    // Townspeople sheet is authored at 16 PPU (26px ~= 1.6 world-unit tall figures);
    // scaled down so a waiting person reads at a sane size beside the road.
    public const float FigureScale = 0.8f;

    // Sprites are center-pivoted front views, so lift by ~half the scaled height
    // (1.6 * 0.8 / 2) to plant the character's feet on the color-coded ground dot.
    public const float FigureLift = 0.62f;

    /// <summary>Loads the animator override controller for a Townspeople variant (may be null).</summary>
    public static RuntimeAnimatorController LoadController(int variant)
        => Resources.Load<RuntimeAnimatorController>(
               $"TutorialCharacters/Townspeople_{variant}/Townspeople_{variant}_NPC_Animator");

    /// <summary>
    /// Builds an upright, front-facing (south / toward the camera) townsperson under
    /// <paramref name="parent"/>, its character chosen deterministically from
    /// <paramref name="seed"/>. World-upright even when the parent stop zone is
    /// rotated (matching how the stop sign stays upright at corner stops). Returns
    /// the figure, or null if no character art could be loaded.
    /// </summary>
    public static GameObject BuildIdleFigure(Transform parent, int seed, int sortingOrder = 6)
    {
        int variant = WaitingVariants[Mathf.Abs(seed) % WaitingVariants.Length];
        RuntimeAnimatorController controller = LoadController(variant);
        if (controller == null) return null;

        var go = new GameObject("Figure");
        go.transform.SetParent(parent, false);
        go.transform.rotation = Quaternion.identity;   // upright regardless of a rotated stop zone
        go.transform.localScale = Vector3.one * FigureScale;
        // Lift in world space (not the rotated local axis) so feet land on the dot.
        go.transform.position = parent.position + Vector3.up * FigureLift;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = sortingOrder;

        var animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        // Idle, facing south (front toward the top-down camera) — same parameters
        // the town NPCs use in TopDownLevelController.
        animator.SetBool("Idle", true);
        animator.SetFloat("HorizontalDirection", 0f);
        animator.SetFloat("VerticallDirection", -1f);

        return go;
    }
}
