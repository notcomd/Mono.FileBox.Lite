// 本文件包含类型 IEntryStore：物化并序列化完整的 IndexEntry。
namespace Mono.FileBox.Lite.Abstractions.Index;

/// <summary>Materializes and serializes a full <see cref="IndexEntry"/>.</summary>
public interface IEntryStore
{
    Task PutAsync(string contentHash, IndexEntry entry, CancellationToken ct);
    Task<IndexEntry?> GetAsync(string contentHash, CancellationToken ct);
    Task DeleteAsync(string contentHash, CancellationToken ct);
    Task<IReadOnlyList<IndexEntry>> GetManyAsync(IEnumerable<string> hashes, CancellationToken ct);

    /// <summary>Lists all entries belonging to a namespace (no pagination).</summary>
    Task<IReadOnlyList<IndexEntry>> ListByNamespaceAsync(string namespaceId, CancellationToken ct);

    /// <summary>Lists every entry in the store.</summary>
    Task<IReadOnlyList<IndexEntry>> ListAllAsync(CancellationToken ct);

    /// <summary>Removes all entries (used by full rebuild).</summary>
    Task ClearAsync(CancellationToken ct);
}