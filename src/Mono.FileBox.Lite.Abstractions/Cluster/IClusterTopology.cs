// 本文件包含类型 IClusterTopology：集群拓扑视图——self 节点、成员、版本、变更事件。
namespace Mono.FileBox.Lite.Abstractions.Cluster;

/// <summary>The cluster topology view: self node, membership, versioning, change events. 中文翻译：集群拓扑视图：self 节点、成员信息、版本号与变更事件。</summary>
public interface IClusterTopology
{
    NodeInfo Self { get; }
    Task<IReadOnlyList<NodeInfo>> ListNodesAsync(CancellationToken ct);
    Task<IReadOnlyList<NodeInfo>> ListNodesAsync(NodeRole role, CancellationToken ct);
    long TopologyVersion { get; }
    event EventHandler<TopologyChangedEventArgs>? TopologyChanged;
}