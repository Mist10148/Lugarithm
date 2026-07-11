using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns the whole-scene driving background: one full-image sprite per chunk,
/// placed by <see cref="SceneTemplateLibrary"/> so the painted road lies exactly
/// under the route the generators produced. Later chunks draw one order above
/// earlier ones, letting their dithered entry edge blend over the previous
/// chunk — the seams read as one continuous town.
/// Purely visual: stops, off-road checks, traffic and streaming stay on the
/// shared route graph.
/// </summary>
public static class SceneChunkVisualBuilder
{
    // Chunk sprites live between the ground (-100) and everything else. The
    // order cycles inside the band; the wrap only ever sits many chunks behind
    // the active streaming window.
    const int SortingBase = -98;
    const int SortingSpan = 90;

    // The full-scene templates are large textures; loading one synchronously
    // mid-drive is a visible hitch, so every lookup is cached (misses too — a
    // missing template logs once, not once per streamed chunk).
    static readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

    static Sprite LoadTemplate(string name)
    {
        if (_spriteCache.TryGetValue(name, out Sprite cached)) return cached;

        Sprite sprite = Resources.Load<Sprite>("Driving/Background/" + name);
        _spriteCache[name] = sprite;
        if (sprite == null)
            Debug.LogWarning($"[SceneChunk] Missing template sprite '{name}'.");
        return sprite;
    }

    /// <summary>
    /// Warm-loads every scene template (plus the grass) so the one-time texture
    /// uploads happen behind the scene transition instead of mid-drive when a
    /// chunk first streams in. Call once from the drive controllers' setup.
    /// </summary>
    public static void Preload()
    {
        if (!SceneTemplateLibrary.Active) return;
        foreach (string name in SceneTemplateLibrary.TemplateSpriteNames)
            LoadTemplate(name);
        GroundSprite();
    }

    public static void Spawn(Transform parent, List<ScenePlacement> placements)
    {
        if (parent == null || placements == null || placements.Count == 0) return;

        Transform root = parent.Find("SceneChunks");
        if (root == null)
        {
            var go = new GameObject("SceneChunks");
            go.transform.SetParent(parent, false);
            root = go.transform;
        }

        foreach (ScenePlacement p in placements)
        {
            Sprite sprite = LoadTemplate(p.sprite);
            if (sprite == null) continue;

            var go = new GameObject($"Scene_{p.order}_{p.sprite}");
            go.transform.SetParent(root, false);
            go.transform.position = p.center;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = SortingBase + (p.order % SortingSpan);
        }
    }

    /// <summary>Dusk grass tile matched to the scene art, if generated.</summary>
    public static Sprite GroundSprite()
    {
        if (_spriteCache.TryGetValue("ground_grass", out Sprite cached)) return cached;
        Sprite grass = Resources.Load<Sprite>("Driving/Background/ground_grass");
        _spriteCache["ground_grass"] = grass;
        return grass;
    }

    /// <summary>
    /// Swaps the Manual scene's tiled Ground sprite for the art-matched grass so
    /// the world beyond the chunks blends in. Automation's procedural ground
    /// picks the sprite up directly in AddProceduralGround. Call once from the
    /// drive controller's setup — the FindAnyObjectByType scene scan is not
    /// per-chunk work.
    /// </summary>
    public static void SwapGround()
    {
        Sprite grass = GroundSprite();
        if (grass == null) return;

        GroundFollow ground = Object.FindAnyObjectByType<GroundFollow>();
        if (ground == null) return;
        SpriteRenderer sr = ground.GetComponent<SpriteRenderer>();
        if (sr != null && sr.drawMode == SpriteDrawMode.Tiled && sr.sprite != grass)
            sr.sprite = grass;
    }
}
