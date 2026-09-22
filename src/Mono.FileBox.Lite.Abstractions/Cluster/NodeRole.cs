// 本文件包含类型 NodeRole：节点在集群中的角色。
namespace Mono.FileBox.Lite.Abstractions.Cluster;

/// <summary>Role of a node in the cluster.</summary>
public enum NodeRole
{
    /// <summary>Coordination: metadata, routing, locks.</summary>
    Coordinator,

    /// <summary>Storage: hosts physical blocks.</summary>
    Storage,

    /// <summary>Index: hosts index shards.</summary>
    Index,

    /// <summary>Backup: hosts backup-target writes.</summary>
    Backup,

    /// <summary>Single-node mode: takes on every role.</summary>
    Hybrid
}