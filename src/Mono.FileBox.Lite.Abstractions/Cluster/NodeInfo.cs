// 本文件包含类型 NodeInfo：集群中节点的描述符。
namespace Mono.FileBox.Lite.Abstractions.Cluster;

/// <summary>Descriptor for a node in the cluster.</summary>
public sealed class NodeInfo
{
    public string NodeId { get; init; } = string.Empty;
    public NodeRole Role { get; init; } = NodeRole.Hybrid;
    public string AdvertiseAddress { get; init; } = string.Empty;
    public DateTimeOffset JoinedAt { get; init; } = DateTimeOffset.UtcNow;
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
    public long TopologyVersion { get; init; }

    public override string ToString() => $"{NodeId} ({Role})";
}