// Contains the InMemoryTierManager type, an in-memory tier registry and migration bookkeeping.
using Mono.FileBox.Lite.Abstractions.Cluster;

namespace Mono.FileBox.Lite.Cluster.Tiering;

/// <summary>In-memory tier registry and migration bookkeeping
/// 内存中的分层（tier）注册表与迁移记账
/// </summary>
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