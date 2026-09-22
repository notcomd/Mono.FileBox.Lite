// 本文件包含类型 ClusterOptions：集群选项。
using Mono.FileBox.Lite.Abstractions.Cluster;

namespace Mono.FileBox.Lite.Abstractions.Configuration;

public sealed class ClusterOptions
{
    public NodeRole Role { get; set; } = NodeRole.Hybrid;
    public string NodeId { get; set; } = string.Empty;
    public string? AdvertiseAddress { get; set; }
    public IList<string> SeedNodes { get; set; } = new List<string>();
    public ReplicationPolicy Replication { get; set; } = new();
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan NodeTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan LockLease { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan LeaderLease { get; set; } = TimeSpan.FromSeconds(15);
    public CapacityThresholds Thresholds { get; set; } = new();
}