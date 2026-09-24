// 本文件包含类型 PoolCapacity：单一池的容量。
namespace Mono.FileBox.Lite.Abstractions.Cluster;

/// <summary>Capacity of a single pool. 单个存储池的容量信息。</summary>
public sealed class PoolCapacity
{
    public string PoolId { get; init; } = string.Empty;
    public long TotalBytes { get; init; }
    public long UsedBytes { get; init; }
    public double UsedRatio => TotalBytes == 0 ? 0 : (double)UsedBytes / TotalBytes;
    public CapacityState State { get; init; }
}