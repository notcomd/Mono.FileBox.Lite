// 本文件包含类型 IOrderedKeyValueStore：索引提供方背后的主有序键值存储。
namespace Mono.FileBox.Lite.Abstractions.Index;

/// <summary>Primary ordered key/value storage behind the index providers. 作为索引提供方底层支持的主有序键值存储。</summary>
public interface IOrderedKeyValueStore
{
    Task PutAsync(byte[] key, byte[]? value, CancellationToken ct);
    Task<byte[]?> GetAsync(byte[] key, CancellationToken ct);
    Task DeleteAsync(byte[] key, CancellationToken ct);
    Task<bool> ExistsAsync(byte[] key, CancellationToken ct);

    /// <summary>Enumerates key/value pairs within [start, end) lexicographically.</summary>
    Task<IReadOnlyList<KeyValuePair<byte[], byte[]>>> ScanAsync(
        byte[]? start, byte[]? end, CancellationToken ct);
}