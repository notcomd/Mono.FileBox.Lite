// 本文件包含类型 IndexQuery：完整的多维索引查询。
namespace Mono.FileBox.Lite.Abstractions.Index;

/// <summary>A full multi-dimensional index query. 中文翻译：完整的多维索引查询承载对象。</summary>
public sealed class IndexQuery
{
    public string NamespaceId { get; init; } = string.Empty;
    public string? KeyPrefix { get; init; }
    public IReadOnlyDictionary<string, string>? Tags { get; init; }
    public IReadOnlyList<AttributeRange>? AttributeRanges { get; init; }
    public IReadOnlyList<StorageTier>? Tiers { get; init; }
    public IReadOnlyList<ObjectState>? States { get; init; }
    public TimeRange? CreatedAtRange { get; init; }
    public TimeRange? ModifiedAtRange { get; init; }
    public LongRange? SizeRange { get; init; }
    public IndexSort? Sort { get; init; }
    public PageRequest Page { get; init; } = new();
}