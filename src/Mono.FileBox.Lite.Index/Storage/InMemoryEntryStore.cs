using System.Collections.Concurrent;
using Mono.FileBox.Lite.Abstractions.Index;

namespace Mono.FileBox.Lite.Index.Storage;

/// <summary>
/// In-memory entry store. Entries are held by content hash with a namespace index for
/// listing. This is the lightweight default for the Lite engine (no external database).
/// </summary>
public sealed class InMemoryEntryStore : IEntryStore
{
    private readonly ConcurrentDictionary<string, IndexEntry> _entries =
        new(StringComparer.Ordinal);

    public Task PutAsync(string contentHash, IndexEntry entry, CancellationToken ct)
    {
        _entries[contentHash] = entry;
        return Task.CompletedTask;
    }

    public Task<IndexEntry?> GetAsync(string contentHash, CancellationToken ct)
        => Task.FromResult(_entries.TryGetValue(contentHash, out var e) ? e : null);

    public Task DeleteAsync(string contentHash, CancellationToken ct)
    {
        _entries.TryRemove(contentHash, out _);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<IndexEntry>> GetManyAsync(IEnumerable<string> hashes, CancellationToken ct)
    {
        var result = new List<IndexEntry>();
        foreach (var h in hashes)
            if (_entries.TryGetValue(h, out var e)) result.Add(e);
        return Task.FromResult<IReadOnlyList<IndexEntry>>(result);
    }

    public Task<IReadOnlyList<IndexEntry>> ListByNamespaceAsync(string namespaceId, CancellationToken ct)
    {
        var result = _entries.Values
            .Where(e => string.Equals(e.NamespaceId, namespaceId, StringComparison.Ordinal))
            .OrderBy(e => e.ContentHash, StringComparer.Ordinal)
            .ToArray();
        return Task.FromResult<IReadOnlyList<IndexEntry>>(result);
    }

    public Task<IReadOnlyList<IndexEntry>> ListAllAsync(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IndexEntry>>(
            _entries.Values.OrderBy(e => e.ContentHash, StringComparer.Ordinal).ToArray());

    public Task ClearAsync(CancellationToken ct)
    {
        _entries.Clear();
        return Task.CompletedTask;
    }
}