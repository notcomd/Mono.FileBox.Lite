// 本文件包含类型 QueryPlan：已执行的查询计划——一个驱动扫描加一组有序过滤器。
namespace Mono.FileBox.Lite.Abstractions.Index;

/// <summary>An executed query plan: one driving scan plus an ordered set of filters.</summary>
public sealed class QueryPlan
{
    public string NamespaceId { get; init; } = string.Empty;
    public IIndexScan Driver { get; init; } = null!;
    public IReadOnlyList<IIndexScan> Filters { get; init; } = Array.Empty<IIndexScan>();
    public IndexSort? Sort { get; init; }
    public PageRequest Page { get; init; } = new();

    /// <summary>The original query, retained so the executor can apply an authoritative final filter.</summary>
    public IndexQuery? Source { get; init; }
}