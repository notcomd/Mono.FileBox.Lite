// 本文件包含类型 RebalanceProgress：进行中再平衡的进度。
namespace Mono.FileBox.Lite.Abstractions.Cluster;

/// <summary>Progress for an in-flight rebalance. 进行中再平衡操作的进度。</summary>
public sealed class RebalanceProgress
{
    public long TotalBlocks { get; init; }
    public long CompletedBlocks { get; init; }
    public long MovedBlocks { get; init; }
    public double Ratio => TotalBlocks == 0 ? 0 : (double)CompletedBlocks / TotalBlocks;
    public bool IsComplete => CompletedBlocks >= TotalBlocks && TotalBlocks > 0;
}