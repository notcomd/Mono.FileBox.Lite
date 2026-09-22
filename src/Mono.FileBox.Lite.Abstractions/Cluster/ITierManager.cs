// 本文件包含类型 ITierManager：管理存储层与跨层迁移。
namespace Mono.FileBox.Lite.Abstractions.Cluster;

/// <summary>Manages storage tiers and cross-tier migrations. 中文翻译：管理存储层以及跨层级迁移。</summary>
public interface ITierManager
{
    Task RegisterTierAsync(TierDefinition tier, CancellationToken ct);
    Task<IReadOnlyList<TierDefinition>> ListTiersAsync(CancellationToken ct);
    Task MigrateAsync(TierMigration migration, CancellationToken ct);
}