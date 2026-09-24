// 本文件包含类型 TierMigration：描述层到层的迁移。
using Mono.FileBox.Lite.Abstractions.Index;

namespace Mono.FileBox.Lite.Abstractions.Cluster;

/// <summary>Describes a tier-to-tier migration. 描述一次层级到层级的对象迁移。</summary>
public sealed class TierMigration
{
    public string ContentHash { get; init; } = string.Empty;
    public StorageTier From { get; init; }
    public StorageTier To { get; init; }
    public string TargetPoolId { get; init; } = string.Empty;
}