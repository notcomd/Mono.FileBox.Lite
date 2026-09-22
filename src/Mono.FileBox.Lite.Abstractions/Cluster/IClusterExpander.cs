// 本文件包含类型 IClusterExpander：扩展集群——加入、再平衡、进度。
namespace Mono.FileBox.Lite.Abstractions.Cluster;

/// <summary>Expands the cluster: join, rebalance, progress. 中文翻译：扩展集群：节点加入、再平衡与进度查询。</summary>
public interface IClusterExpander
{
    Task JoinAsync(NodeInfo node, CancellationToken ct);
    Task RebalanceAsync(RebalanceOptions options, CancellationToken ct);
    Task<RebalanceProgress> GetProgressAsync(CancellationToken ct);
}