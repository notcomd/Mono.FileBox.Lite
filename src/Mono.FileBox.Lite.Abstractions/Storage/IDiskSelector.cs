// 本文件包含类型 IDiskSelector：为内容哈希选择写入/读取的磁盘或池，并汇报池级健康与容量。
namespace Mono.FileBox.Lite.Abstractions.Storage;

/// <summary>
/// Chooses which disk/pool a content hash is written to or read from, and reports
/// pool-level health and capacity.
/// 中文翻译：为内容哈希选择写入或读取的目标磁盘/池，并汇报池级健康与容量信息。
/// </summary>
public interface IDiskSelector
{
    Task<IDiskHandle> SelectForWriteAsync(
        string contentHash, WriteOptions options, CancellationToken ct);
    Task<IDiskHandle> SelectForReadAsync(string contentHash, CancellationToken ct);
    Task<IReadOnlyList<DiskPoolInfo>> ListPoolsAsync(CancellationToken ct);
}