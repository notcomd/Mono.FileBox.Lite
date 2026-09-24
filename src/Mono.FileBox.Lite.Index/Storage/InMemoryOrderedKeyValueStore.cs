using System.Collections.Concurrent;
using Mono.FileBox.Lite.Abstractions.Index;

namespace Mono.FileBox.Lite.Index.Storage;

/// <summary>
/// In-memory stored ordered key/value store. Keys are compared as byte-lexicographic
/// sequences, enabling prefix range scans.
/// 内存中存储的有序键值存储。键按字节字典序比较，从而支持前缀范围扫描。
/// </summary>
public sealed class InMemoryOrderedKeyValueStore : IOrderedKeyValueStore
{
    private readonly ConcurrentDictionary<string, byte[]> _store
        = new(StringComparer.Ordinal);
    private readonly object _lock = new();

    private static string KeyString(byte[] key) => Convert.ToBase64String(key);

    public Task PutAsync(byte[] key, byte[]? value, CancellationToken ct)
    {
        _store[KeyString(key)] = value ?? Array.Empty<byte>();
        return Task.CompletedTask;
    }

    public Task<byte[]?> GetAsync(byte[] key, CancellationToken ct)
        => Task.FromResult(_store.TryGetValue(KeyString(key), out var v) ? v : null);

    public Task DeleteAsync(byte[] key, CancellationToken ct)
    {
        _store.TryRemove(KeyString(key), out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(byte[] key, CancellationToken ct)
        => Task.FromResult(_store.ContainsKey(KeyString(key)));

    public Task<IReadOnlyList<KeyValuePair<byte[], byte[]>>> ScanAsync(
        byte[]? start, byte[]? end, CancellationToken ct)
    {
        List<KeyValuePair<byte[], byte[]>> result;
        lock (_lock)
        {
            result = _store
                .Where(kv => start is null || Compare(Convert.FromBase64String(kv.Key), start) >= 0)
                .Where(kv => end is null || Compare(end, Convert.FromBase64String(kv.Key)) > 0)
                .OrderBy(kv => Convert.FromBase64String(kv.Key), new ByteArrayComparer())
                .Select(kv =>
                    new KeyValuePair<byte[], byte[]>(Convert.FromBase64String(kv.Key), kv.Value))
                .ToList();
        }
        return Task.FromResult<IReadOnlyList<KeyValuePair<byte[], byte[]>>>(result);
    }

    private static int Compare(byte[] a, byte[] b)
    {
        var n = Math.Min(a.Length, b.Length);
        for (var i = 0; i < n; i++)
        {
            var cmp = a[i].CompareTo(b[i]);
            if (cmp != 0) return cmp;
        }
        return a.Length.CompareTo(b.Length);
    }

    private sealed class ByteArrayComparer : IComparer<byte[]>
    {
        public int Compare(byte[]? x, byte[]? y)
            => x is null ? (y is null ? 0 : -1) : (y is null ? 1 : CompareBytes(x, y));

        private static int CompareBytes(byte[] a, byte[] b) => CompareArrays(a, b);
    }

    private static int CompareArrays(byte[] a, byte[] b)
    {
        var n = Math.Min(a.Length, b.Length);
        for (var i = 0; i < n; i++)
        {
            var cmp = a[i].CompareTo(b[i]);
            if (cmp != 0) return cmp;
        }
        return a.Length.CompareTo(b.Length);
    }
}