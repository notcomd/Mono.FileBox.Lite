// 本文件包含类型 TierDefinition：存储层的定义。
using Mono.FileBox.Lite.Abstractions.Index;

namespace Mono.FileBox.Lite.Abstractions.Cluster;

/// <summary>Definition of a storage tier. 中文翻译：存储层的定义（映射到池、优先级、容量限制等）。</summary>
public sealed class TierDefinition
{
    public StorageTier Tier { get; init; }
    public string PoolId { get; init; } = string.Empty;
    public int Priority { get; init; }
    public long? CapacityBytes { get; init; }
    public long? MaxObjectSizeBytes { get; init; }
    public TimeSpan? MinAge { get; init; }
}