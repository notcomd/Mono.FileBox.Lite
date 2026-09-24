// 本文件包含类型 IndexShard：单个索引分片。
namespace Mono.FileBox.Lite.Abstractions.Cluster;

/// <summary>A single index shard. 单个索引分片。</summary>
public sealed class IndexShard
{
    public string ShardId { get; init; } = string.Empty;
    public string NamespaceId { get; init; } = string.Empty;
    public NodeInfo Owner { get; init; } = new();
}