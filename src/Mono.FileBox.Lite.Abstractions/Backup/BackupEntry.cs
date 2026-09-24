// 本文件包含类型 BackupEntry：备份清单内的单条对象条目。
using Mono.FileBox.Lite.Abstractions.Index;

namespace Mono.FileBox.Lite.Abstractions.Backup;

/// <summary>A single object entry inside a backup manifest. 备份清单内的一条对象条目，描述被备份对象的元数据信息。</summary>
public sealed class BackupEntry
{
    public string ContentHash { get; init; } = string.Empty;
    public string NamespaceId { get; init; } = string.Empty;
    public string? ObjectKey { get; init; }
    public long SizeBytes { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public IReadOnlyDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, object> Attributes { get; init; } = new Dictionary<string, object>();
    public StorageTier Tier { get; init; }
    public ObjectState State { get; init; }
    public string BlockPath { get; init; } = string.Empty;
}