// 本文件包含类型 IIndexProvider：可服务特定谓词种类并给出估计选择度的索引提供方。
namespace Mono.FileBox.Lite.Abstractions.Index;

/// <summary>
/// An index provider that can serve a particular predicate kind with an estimated
/// selectivity. Providers are used by the planner to pick the driving scan.
/// </summary>
public interface IIndexProvider
{
    string Name { get; }
    int Priority { get; }
    bool CanHandle(IndexPredicate predicate);
    double EstimateSelectivity(IndexPredicate predicate);
    IIndexScan CreateScan(IndexQuery query, IndexPredicate predicate);
}