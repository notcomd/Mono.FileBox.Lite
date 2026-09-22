// 本文件包含类型 IIndexSharding：解析并迁移索引分片（分区键 = NamespaceId）。
namespace Mono.FileBox.Lite.Abstractions.Cluster;

/// <summary>Resolves and migrates index shards (partition key = NamespaceId). 中文翻译：解析并迁移索引分片（分区键 = NamespaceId）。</summary>
public interface IIndexSharding
{
    string ResolveShard(string namespaceId);
    Task<IReadOnlyList<IndexShard>> ListShardsAsync(CancellationToken ct);
    Task MigrateShardAsync(string shardId, string targetNodeId, CancellationToken ct);
}