// 本文件包含类型 IndexSort：索引查询的排序规格。
namespace Mono.FileBox.Lite.Abstractions.Index;

/// <summary>Sort specification for an index query.</summary>
public sealed class IndexSort
{
    public string Field { get; init; } = "CreatedAt";
    public SortDirection Direction { get; init; } = SortDirection.Ascending;
}