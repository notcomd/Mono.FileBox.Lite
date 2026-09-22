// 本文件包含类型 TierMigration：描述层到层的迁移。
using Mono.FileBox.Lite.Abstractions.Index;

namespace Mono.FileBox.Lite.Abstractions.Cluster;

/// <summary>Describes a tier-to-tier migration.</summary>
public sealed class TierMigration
{
    public string ContentHash { get; init; } = string.Empty;
    public StorageTier From { get; init; }
    public StorageTier To { get; init; }
    public string TargetPoolId { get; init; } = string.Empty;
}