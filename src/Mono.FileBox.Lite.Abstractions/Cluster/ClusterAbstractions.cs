using Mono.FileBox.Lite.Abstractions.Index;

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

/// <summary>Payload describing a topology change.</summary>
public sealed class TopologyChangedEventArgs : EventArgs
{
    public long NewVersion { get; init; }
    public IReadOnlyList<NodeInfo> Nodes { get; init; } = Array.Empty<NodeInfo>();
}

/// <summary>The cluster topology view: self node, membership, versioning, change events.</summary>
public interface IClusterTopology
{
    NodeInfo Self { get; }
    Task<IReadOnlyList<NodeInfo>> ListNodesAsync(CancellationToken ct);
    Task<IReadOnlyList<NodeInfo>> ListNodesAsync(NodeRole role, CancellationToken ct);
    long TopologyVersion { get; }
    event EventHandler<TopologyChangedEventArgs>? TopologyChanged;
}

/// <summary>Replication policy used by the hash ring and consistency computations.</summary>
public sealed class ReplicationPolicy
{
    public int Factor { get; init; } = 3;
    public int WriteQuorum { get; init; } = 2;
    public int ReadQuorum { get; init; } = 2;
    public bool CrossRack { get; init; } = true;
    public bool CrossRegion { get; init; }
}

/// <summary>Replication policy for index shards (reads may downgrade).</summary>
public sealed class IndexReplicationPolicy
{
    public int Factor { get; init; } = 3;
    public int WriteQuorum { get; init; } = 2;
    public int ReadQuorum { get; init; } = 1;
}

/// <summary>A consistent hash ring mapping keys to replica nodes.</summary>
public interface IHashRing
{
    IReadOnlyList<NodeInfo> SelectNodes(string key, int replicas);
    int VirtualNodeCount { get; }
    void Rebuild(IReadOnlyList<NodeInfo> nodes);
}

/// <summary>Options controlling a rebalance operation.</summary>
public sealed class RebalanceOptions
{
    public int BatchSize { get; init; } = 100;
    public long BandwidthLimit { get; init; }
    public int? IopsLimit { get; init; }
    public IReadOnlyList<string>? PoolIds { get; init; }
    public bool Background { get; init; } = true;
}

/// <summary>Progress for an in-flight rebalance.</summary>
public sealed class RebalanceProgress
{
    public long TotalBlocks { get; init; }
    public long CompletedBlocks { get; init; }
    public long MovedBlocks { get; init; }
    public double Ratio => TotalBlocks == 0 ? 0 : (double)CompletedBlocks / TotalBlocks;
    public bool IsComplete => CompletedBlocks >= TotalBlocks && TotalBlocks > 0;
}

/// <summary>Expands the cluster: join, rebalance, progress.</summary>
public interface IClusterExpander
{
    Task JoinAsync(NodeInfo node, CancellationToken ct);
    Task RebalanceAsync(RebalanceOptions options, CancellationToken ct);
    Task<RebalanceProgress> GetProgressAsync(CancellationToken ct);
}

/// <summary>Shrinks / decommissions nodes.</summary>
public interface IClusterShrinker
{
    Task DecommissionAsync(string nodeId, CancellationToken ct);
    Task WaitForDrainAsync(string nodeId, CancellationToken ct);
    Task RemoveAsync(string nodeId, CancellationToken ct);
}

/// <summary>Definition of a storage tier.</summary>
public sealed class TierDefinition
{
    public StorageTier Tier { get; init; }
    public string PoolId { get; init; } = string.Empty;
    public int Priority { get; init; }
    public long? CapacityBytes { get; init; }
    public long? MaxObjectSizeBytes { get; init; }
    public TimeSpan? MinAge { get; init; }
}

/// <summary>Describes a tier-to-tier migration.</summary>
public sealed class TierMigration
{
    public string ContentHash { get; init; } = string.Empty;
    public StorageTier From { get; init; }
    public StorageTier To { get; init; }
    public string TargetPoolId { get; init; } = string.Empty;
}

/// <summary>Manages storage tiers and cross-tier migrations.</summary>
public interface ITierManager
{
    Task RegisterTierAsync(TierDefinition tier, CancellationToken ct);
    Task<IReadOnlyList<TierDefinition>> ListTiersAsync(CancellationToken ct);
    Task MigrateAsync(TierMigration migration, CancellationToken ct);
}

/// <summary>A single index shard.</summary>
public sealed class IndexShard
{
    public string ShardId { get; init; } = string.Empty;
    public string NamespaceId { get; init; } = string.Empty;
    public NodeInfo Owner { get; init; } = new();
}

/// <summary>Resolves and migrates index shards (partition key = NamespaceId).</summary>
public interface IIndexSharding
{
    string ResolveShard(string namespaceId);
    Task<IReadOnlyList<IndexShard>> ListShardsAsync(CancellationToken ct);
    Task MigrateShardAsync(string shardId, string targetNodeId, CancellationToken ct);
}

/// <summary>Capacity status of the cluster as a whole and per pool.</summary>
public sealed class CapacityStatus
{
    public long TotalBytes { get; init; }
    public long UsedBytes { get; init; }
    public double UsedRatio => TotalBytes == 0 ? 0 : (double)UsedBytes / TotalBytes;
    public IReadOnlyDictionary<string, PoolCapacity> Pools { get; init; }
        = new Dictionary<string, PoolCapacity>();
    public CapacityState State { get; init; }
}

/// <summary>Capacity of a single pool.</summary>
public sealed class PoolCapacity
{
    public string PoolId { get; init; } = string.Empty;
    public long TotalBytes { get; init; }
    public long UsedBytes { get; init; }
    public double UsedRatio => TotalBytes == 0 ? 0 : (double)UsedBytes / TotalBytes;
    public CapacityState State { get; init; }
}

/// <summary>Capacity watermark state.</summary>
public enum CapacityState { Normal, Warning, Critical, Full }

/// <summary>Threshold ratios (0..1) driving capacity state transitions.</summary>
public sealed class CapacityThresholds
{
    public double Warning { get; init; } = 0.70;
    public double Critical { get; init; } = 0.85;
    public double Full { get; init; } = 0.95;
}

/// <summary>Callback invoked when a capacity threshold is crossed.</summary>
public delegate void ThresholdCallback(CapacityStatus status);

/// <summary>Monitors capacity and raises threshold callbacks.</summary>
public interface ICapacityMonitor
{
    Task<CapacityStatus> GetStatusAsync(CancellationToken ct);
    void OnThreshold(ThresholdCallback callback);
}