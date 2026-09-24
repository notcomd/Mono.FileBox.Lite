// 本文件包含类型 IndexReplicationPolicy：索引分片的复制策略（读取可降级）。
namespace Mono.FileBox.Lite.Abstractions.Cluster;

/// <summary>Replication policy for index shards (reads may downgrade). 索引分片的复制策略（读取可降级）。</summary>
public sealed class IndexReplicationPolicy
{
    public int Factor { get; init; } = 3;
    public int WriteQuorum { get; init; } = 2;
    public int ReadQuorum { get; init; } = 1;
}