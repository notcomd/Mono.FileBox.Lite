// 本文件包含类型 RebalanceOptions：控制再平衡操作的选项。
namespace Mono.FileBox.Lite.Abstractions.Cluster;

/// <summary>Options controlling a rebalance operation. 控制再平衡操作的选项（批大小、带宽/IOPS 限制、目标池等）。</summary>
public sealed class RebalanceOptions
{
    public int BatchSize { get; init; } = 100;
    public long BandwidthLimit { get; init; }
    public int? IopsLimit { get; init; }
    public IReadOnlyList<string>? PoolIds { get; init; }
    public bool Background { get; init; } = true;
}