using Mono.FileBox.Lite.Abstractions.Index;
using Mono.FileBox.Lite.Index.Providers;

namespace Mono.FileBox.Lite.Index.Planning;

/// <summary>
/// Builds an execution plan for a query. Extracts predicates, picks the most selective
/// claimable provider as the driving scan, and uses the remaining scans as filters.
/// Falls back to a full scan when no predicate is claimable.
/// 为查询构建执行计划。提取谓词，选出最具选择性的可认领提供方作为驱动扫描，
/// 其余扫描作为过滤条件；当没有可认领的谓词时回退为全表扫描。
/// </summary>
public sealed class QueryPlanner : IQueryPlanner
{
    private readonly IReadOnlyList<IIndexProvider> _providers;
    private readonly IEntryStore _store;

    public QueryPlanner(IEnumerable<IIndexProvider> providers, IEntryStore store)
    {
        _providers = providers.ToList();
        _store = store;
    }

    public QueryPlan Plan(IndexQuery query)
    {
        var predicates = QueryPredicateExtractor.Extract(query);

        if (predicates.Count == 0 || _providers.Count == 0)
            return NoPredicatePlan(query);

        // Map each claimable predicate to its most selective provider scan.
        var scans = new List<(IIndexScan scan, double selectivity)>();
        foreach (var predicate in predicates)
        {
            var provider = SelectProvider(predicate);
            if (provider is null) continue;
            scans.Add((provider.CreateScan(query, predicate), provider.EstimateSelectivity(predicate)));
        }

        if (scans.Count == 0)
            return NoPredicatePlan(query);

        // The driver is the scan with the lowest estimated cost.
        var ordered = scans.OrderBy(s => s.selectivity).ToList();
        var driver = ordered[0].scan;
        var filters = ordered.Skip(1).Select(s => s.scan).ToList();

        return new QueryPlan
        {
            NamespaceId = query.NamespaceId,
            Driver = driver,
            Filters = filters,
            Sort = query.Sort,
            Page = query.Page,
            Source = query
        };
    }

    private IIndexProvider? SelectProvider(IndexPredicate predicate)
        => _providers
            .Where(p => p.CanHandle(predicate))
            .OrderBy(p => p.EstimateSelectivity(predicate))
            .FirstOrDefault();

    private QueryPlan NoPredicatePlan(IndexQuery query)
    {
        var full = new FullScanProvider(_store);
        return new QueryPlan
        {
            NamespaceId = query.NamespaceId,
            Driver = full.CreateScan(query, new IndexPredicate { Kind = PredicateKind.Size }),
            Sort = query.Sort,
            Page = query.Page,
            Source = query
        };
    }
}