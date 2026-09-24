// Contains the DefaultIndexSharding type, resolving namespaces to index shards.
using Mono.FileBox.Lite.Abstractions.Cluster;

namespace Mono.FileBox.Lite.Cluster.Tiering;

/// <summary>Namespace -> shard resolution (<c>NamespaceId</c> is the partition key)
/// 命名空间到分片（shard）的解析，以 <c>NamespaceId</c> 作为分区键
/// </summary>
public sealed class DefaultIndexSharding : IIndexSharding
{
    private readonly int _shardCount;

    public DefaultIndexSharding(int shardCount = 1) => _shardCount = Math.Max(1, shardCount);

    public string ResolveShard(string namespaceId)
    {
        var hash = Math.Abs(SHAHash(namespaceId));
        return $"shard-{hash % _shardCount}";
    }

    public Task<IReadOnlyList<IndexShard>> ListShardsAsync(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IndexShard>>(
            Enumerable.Range(0, _shardCount)
                .Select(i => new IndexShard { ShardId = $"shard-{i}", NamespaceId = "*" })
                .ToArray());

    public Task MigrateShardAsync(string shardId, string targetNodeId, CancellationToken ct)
        => Task.CompletedTask;

    private static long SHAHash(string value)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(value));
        long result = 0;
        for (var i = 0; i < 16; i++) result = result * 31 + bytes[i];
        return result;
    }
}