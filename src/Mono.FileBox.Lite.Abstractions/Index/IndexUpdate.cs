// 本文件包含类型 IndexUpdate：应用于既有索引条目的字段级更新。
namespace Mono.FileBox.Lite.Abstractions.Index;

/// <summary>Field-level update applied to an existing index entry. 中文翻译：应用于既有索引条目的字段级更新。</summary>
public sealed class IndexUpdate
{
    public string? ObjectKey { get; init; }
    public StorageTier? Tier { get; init; }
    public ObjectState? State { get; init; }
    public long? SizeBytes { get; init; }
    public string? ContentType { get; init; }
    public DateTimeOffset? ModifiedAt { get; init; }
    public IReadOnlyDictionary<string, string>? Tags { get; init; }
    public IReadOnlyDictionary<string, object>? Attributes { get; init; }
    public IReadOnlyList<string>? RemoveTags { get; init; }
    public IReadOnlyList<string>? RemoveAttributes { get; init; }
}