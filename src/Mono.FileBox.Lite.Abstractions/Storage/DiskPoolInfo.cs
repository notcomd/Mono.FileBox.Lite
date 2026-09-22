// 本文件包含类型 DiskPoolInfo：存储磁盘池的信息。
using Mono.FileBox.Lite.Abstractions.Index;

namespace Mono.FileBox.Lite.Abstractions.Storage;

/// <summary>Information about a storage disk pool.</summary>
public sealed class DiskPoolInfo
{
    public string PoolId { get; init; } = string.Empty;
    public string RootPath { get; init; } = string.Empty;
    public StorageTier Tier { get; init; } = StorageTier.Hot;
    public long? CapacityBytes { get; init; }
    public long? UsedBytes { get; init; }
    public int Priority { get; init; }
    public bool Enabled { get; init; } = true;
    public int HealthyDiskCount { get; init; }
}