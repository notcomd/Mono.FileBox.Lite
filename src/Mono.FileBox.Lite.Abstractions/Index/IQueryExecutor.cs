// 本文件包含类型 IQueryExecutor：执行查询计划并返回分页的 IndexEntry 结果。
namespace Mono.FileBox.Lite.Abstractions.Index;

/// <summary>Executes a query plan and returns paged <see cref="IndexEntry"/> results. 中文翻译：执行查询计划并返回分页后的 IndexEntry 结果。</summary>
public interface IQueryExecutor
{
    Task<Page<IndexEntry>> ExecuteAsync(QueryPlan plan, CancellationToken ct);
}