// DefaultIndexReader.cs — default read path: plan then execute.
using Mono.FileBox.Lite.Abstractions.Index;
using Mono.FileBox.Lite.Abstractions.Subsystems;

namespace Mono.FileBox.Lite.Index.Planning;

/// <summary>Default read path: plan then execute.
/// 中文翻译：默认读取路径：先规划(生成执行计划)再执行。
/// </summary>
public sealed class DefaultIndexReader : IIndexReader
{
    private readonly IQueryPlanner _planner;
    private readonly IQueryExecutor _executor;

    public DefaultIndexReader(IQueryPlanner planner, IQueryExecutor executor)
    {
        _planner = planner;
        _executor = executor;
    }

    public Task<Page<IndexEntry>> QueryAsync(IndexQuery query, CancellationToken ct)
        => _executor.ExecuteAsync(_planner.Plan(query), ct);
}