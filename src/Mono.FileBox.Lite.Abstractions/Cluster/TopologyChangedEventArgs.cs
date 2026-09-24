// 本文件包含类型 TopologyChangedEventArgs：描述一次拓扑变更的负载。
namespace Mono.FileBox.Lite.Abstractions.Cluster;

/// <summary>Payload describing a topology change. 描述一次集群拓扑变更的事件负载。</summary>
public sealed class TopologyChangedEventArgs : EventArgs
{
    public long NewVersion { get; init; }
    public IReadOnlyList<NodeInfo> Nodes { get; init; } = Array.Empty<NodeInfo>();
}