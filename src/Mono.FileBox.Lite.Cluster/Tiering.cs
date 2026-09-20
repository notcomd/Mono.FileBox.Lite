using Mono.FileBox.Lite.Abstractions.Cluster;

namespace Mono.FileBox.Lite.Cluster.Tiering;

/// <summary>In-memory tier registry and migration bookkeeping.</summary>
public sealed class InMemoryTierManager : ITierManager
{
    private readonly Dictionary<string, TierDefinition> _tiers = new(StringComparer.Ordinal);
    private readonly List<TierMigration> _migrations = new();
    private readonly object _lock = new();

    public Task RegisterTierAsync(TierDefinition tier, CancellationToken ct)
    {
        lock (_lock) _tiers[tier.PoolId] = tier;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<TierDefinition>> ListTiersAsync(CancellationToken ct)
    {
        lock (_lock) return Task.FromResult<IReadOnlyList<TierDefinition>>(
            _tiers.Values.OrderBy(t => t.Priority).ToArray());
    }

    public Task MigrateAsync(TierMigration migration, CancellationToken ct)
    {
        lock (_lock) _migrations.Add(migration);
        return Task.CompletedTask;
    }
}

/// <summary>Namespace -> shard resolution (<c>NamespaceId</c> is the partition key).</summary>
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