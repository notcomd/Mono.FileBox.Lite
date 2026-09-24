// 本文件包含类型 IndexPredicate：从查询提取的索引谓词（每个可查询维度一个）。
namespace Mono.FileBox.Lite.Abstractions.Index;

/// <summary>An index predicate extracted from a query (one per queryable dimension). 从查询中提取的索引谓词（每个可查询维度一条）。</summary>
public sealed class IndexPredicate
{
    public PredicateKind Kind { get; init; }
    public string Field { get; init; } = string.Empty;
    public object? Value { get; init; }
    public object? Operator { get; init; }
}