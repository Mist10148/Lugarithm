using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime handle for one streamed town chunk's scene objects. The logical
/// <see cref="StreamingTown"/> remains append-only; this just lets the view keep
/// a bounded active window.
/// </summary>
public class StreamedChunkView
{
    public int chunkIndex;
    public int generationId;
    public Transform root;
    public float minAlong;
    public float maxAlong;
    public bool active;
    public readonly HashSet<int> nodeIds = new HashSet<int>();

    public void SetActive(bool value)
    {
        active = value;
        if (root != null)
            root.gameObject.SetActive(value);
    }
}
