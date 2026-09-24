using System.Collections.Concurrent;
using Mono.FileBox.Lite.Abstractions.Index;

namespace Mono.FileBox.Lite.Index.Storage;

/// <summary>
/// In-memory entry store. Entries are held by content hash with a namespace index for
/// listing. This is the lightweight default for the Lite engine (no external database).
/// 内存条目存储。以内容哈希持有条目，并维护命名空间索引以便枚举；
/// 这是 Lite 引擎的轻量级默认实现（无需外部数据库）。
/// </summary>
/// <remarks>
/// <b>并发语义</b>：基于 <see cref="ConcurrentDictionary{TKey,TValue}"/>（按 content hash），
/// 不同对象的 <see cref="PutAsync"/>/<see cref="DeleteAsync"/> 可在多线程并行执行；同一对象的
/// 写是原子的字典更新。
/// </remarks>
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