using System.Collections.Generic;

/// <summary>
/// A tiny in-memory LRU cache for validated AI text, keyed by a caller-built string.
/// Repeated dialogue lines and Oracle questions resolve from here instead of paying for
/// another API round-trip — instant, free, and offline-safe. In-memory only: it lives
/// for the session and clears on domain reload, so nothing is persisted to disk.
///
/// Only ever store results that already passed their feature's grounding/validation
/// checks, so a transient failure or a rejected paraphrase can never be memoised.
/// </summary>
public sealed class AiResponseCache
{
    readonly int _capacity;
    readonly Dictionary<string, string> _map;
    readonly LinkedList<string> _order;   // most-recently used at the front

    public AiResponseCache(int capacity = 64)
    {
        _capacity = capacity < 1 ? 1 : capacity;
        _map   = new Dictionary<string, string>(_capacity);
        _order = new LinkedList<string>();
    }

    public int Count => _map.Count;

    public bool TryGet(string key, out string value)
    {
        value = null;
        if (string.IsNullOrEmpty(key) || !_map.TryGetValue(key, out value)) return false;
        Touch(key);
        return true;
    }

    public void Put(string key, string value)
    {
        if (string.IsNullOrEmpty(key) || value == null) return;

        if (_map.ContainsKey(key))
        {
            _map[key] = value;
            Touch(key);
            return;
        }

        _map[key] = value;
        _order.AddFirst(key);

        while (_map.Count > _capacity)
        {
            LinkedListNode<string> oldest = _order.Last;
            _order.RemoveLast();
            _map.Remove(oldest.Value);
        }
    }

    public void Clear()
    {
        _map.Clear();
        _order.Clear();
    }

    void Touch(string key)
    {
        _order.Remove(key);       // O(n) on a small (≤capacity) list — fine at this size
        _order.AddFirst(key);
    }

    // Shared per-feature caches. Dialogue rephrases and Oracle answers each get their own
    // bounded store so one busy feature can't evict the other.
    public static readonly AiResponseCache Dialogue = new AiResponseCache(64);
    public static readonly AiResponseCache Oracle   = new AiResponseCache(64);

    // Inline ghost-text completions, keyed by goal + vocabulary + code-before-cursor. Heavily
    // hit while typing, so a generous store keeps repeated prefixes instant and token-free.
    public static readonly AiResponseCache Ghost    = new AiResponseCache(128);
}
