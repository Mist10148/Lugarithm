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
            Sprite sprite = Resources.Load<Sprite>("Driving/Background/" + p.sprite);
            if (sprite == null)
            {
                Debug.LogWarning($"[SceneChunk] Missing template sprite '{p.sprite}' — chunk {p.order} left bare.");
                continue;
            }

            var go = new GameObject($"Scene_{p.order}_{p.sprite}");
            go.transform.SetParent(root, false);
            go.transform.position = p.center;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = SortingBase + (p.order % SortingSpan);
        }

        SwapGround();
    }

    /// <summary>Dusk grass tile matched to the scene art, if generated.</summary>
    public static Sprite GroundSprite()
    {
        return Resources.Load<Sprite>("Driving/Background/ground_grass");
    }

    /// <summary>
    /// Swaps the Manual scene's tiled Ground sprite for the art-matched grass so
    /// the world beyond the chunks blends in. Automation's procedural ground
    /// picks the sprite up directly in AddProceduralGround. Idempotent — cheap
    /// enough per streamed chunk.
    /// </summary>
    static void SwapGround()
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
