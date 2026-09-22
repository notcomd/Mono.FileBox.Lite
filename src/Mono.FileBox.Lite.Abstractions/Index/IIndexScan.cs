// 本文件包含类型 IIndexScan：单个索引上可执行的扫描，返回候选内容哈希。
namespace Mono.FileBox.Lite.Abstractions.Index;

/// <summary>An executable scan over a single index. Returns candidate content hashes.</summary>
public interface IIndexScan
{
    string IndexName { get; }
    double EstimatedCost { get; }
    bool ProvidesSort(IndexSort sort);
    Task<IReadOnlyList<string>> ExecuteAsync(CancellationToken ct);
}