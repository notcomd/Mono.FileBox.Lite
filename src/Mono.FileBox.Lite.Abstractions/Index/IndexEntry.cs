// 本文件包含类型 IndexEntry：已存储对象的单条索引元数据条目。
namespace Mono.FileBox.Lite.Abstractions.Index;

/// <summary>A single indexed metadata entry for a stored object. 已存储对象的单条索引元数据条目。</summary>
public sealed record IndexEntry
{
    public string ContentHash { get; init; } = string.Empty;
    public string NamespaceId { get; init; } = string.Empty;
    public string? ObjectKey { get; init; }
    public IReadOnlyDictionary<string, string> Tags { get; init; }
        = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, object> Attributes { get; init; }
        = new Dictionary<string, object>();
    public StorageTier Tier { get; init; }
    public long SizeBytes { get; init; }
    public string? ContentType { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset ModifiedAt { get; init; }
    public ObjectState State { get; init; }
}