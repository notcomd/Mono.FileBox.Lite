// 本文件包含类型 IPhysicalDevice：作用于不透明路径的物理块设备原语，不感知内容哈希或磁盘池。
namespace Mono.FileBox.Lite.Abstractions.Storage;

/// <summary>
/// Physical block device primitive operating on opaque paths. It has no knowledge
/// of content hashes or disk pools.
/// 作用于不透明路径的物理块设备原语，不感知内容哈希或磁盘池。
/// </summary>
public interface IPhysicalDevice
{
    Task WriteBlockAsync(string path, ReadOnlyMemory<byte> data, CancellationToken ct);
    Task<ReadOnlyMemory<byte>> ReadBlockAsync(string path, CancellationToken ct);
    Task DeleteBlockAsync(string path, CancellationToken ct);
    Task<bool> ExistsAsync(string path, CancellationToken ct);
}