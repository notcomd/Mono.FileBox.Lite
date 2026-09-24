// 本文件包含类型 CapacityThresholds：驱动容量状态转换的阈值比例（0..1）。
namespace Mono.FileBox.Lite.Abstractions.Cluster;

/// <summary>Threshold ratios (0..1) driving capacity state transitions. 驱动容量状态转换的阈值比例（0..1）。</summary>
public sealed class CapacityThresholds
{
    public double Warning { get; init; } = 0.70;
    public double Critical { get; init; } = 0.85;
    public double Full { get; init; } = 0.95;
}