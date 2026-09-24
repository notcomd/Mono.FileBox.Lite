// 本文件包含类型 CapacityStatus：集群整体及每池的容量状态。
namespace Mono.FileBox.Lite.Abstractions.Cluster;

/// <summary>Capacity status of the cluster as a whole and per pool. 集群整体及每个存储池的容量状态。</summary>
public sealed class CapacityStatus
{
    public long TotalBytes { get; init; }
    public long UsedBytes { get; init; }
    public double UsedRatio => TotalBytes == 0 ? 0 : (double)UsedBytes / TotalBytes;
    public IReadOnlyDictionary<string, PoolCapacity> Pools { get; init; }
        = new Dictionary<string, PoolCapacity>();
    public CapacityState State { get; init; }
}