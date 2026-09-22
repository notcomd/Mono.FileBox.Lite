// QueryExecutor.cs — executes a query plan with filtering, sorting, and pagination.
using Mono.FileBox.Lite.Abstractions.Index;
using Mono.FileBox.Lite.Abstractions.Subsystems;
using Mono.FileBox.Lite.Index.Storage;

namespace Mono.FileBox.Lite.Index.Planning;

/// <summary>
/// Executes a query plan: runs the driving scan, intersects filter scans, materializes
/// entries, applies an authoritative final filter from the source query, sorts, and
/// paginates via a stable cursor.
/// 中文翻译：执行查询计划：运行驱动扫描、与各过滤扫描取交集、物化条目、应用来自原始查询的
/// 权威性最终过滤，进行排序，并通过稳定的游标完成分页。
/// </summary>
public sealed class QueryExecutor : IQueryExecutor
{
    private readonly IEntryStore _store;
    private readonly ICursorCodec _cursor;

    public QueryExecutor(IEntryStore store, ICursorCodec cursor)
    {
        _store = store;
        _cursor = cursor;
    }

    public async Task<Page<IndexEntry>> ExecuteAsync(QueryPlan plan, CancellationToken ct)
    {
        var nsEntries = await _store.ListByNamespaceAsync(plan.NamespaceId, ct).ConfigureAwait(false);

        IReadOnlyList<string>? driverHashes = null;
        if (plan.Driver is not null)
            driverHashes = await plan.Driver.ExecuteAsync(ct).ConfigureAwait(false);
        var driverSet = driverHashes is null ? null : new HashSet<string>(driverHashes, StringComparer.Ordinal);

        // Candidate pool from the driving scan.
        var pool = driverSet is null
            ? nsEntries.ToList()
            : nsEntries.Where(e => driverSet.Contains(e.ContentHash)).ToList();

        // Intersect remaining predicate filters (by hash).
        foreach (var filter in plan.Filters)
        {
            var filterHashes = await filter.ExecuteAsync(ct).ConfigureAwait(false);
            var set = new HashSet<string>(filterHashes, StringComparer.Ordinal);
            pool = pool.Where(e => set.Contains(e.ContentHash)).ToList();
        }

        // Authoritative final filter over the full predicate set.
        if (plan.Source is not null)
            pool = pool.Where(e => IndexEntryMatcher.Matches(e, plan.Source)).ToList();

        var sorted = Sort(pool, plan.Sort ?? new IndexSort());
        var afterHash = CursorAfterHash(plan.Page.Cursor);

        var pageSize = plan.Page.Size <= 0 ? 100 : plan.Page.Size;
        var start = afterHash is null ? 0 : FirstIndexAfter(sorted, afterHash);

        var items = sorted.Skip(start).Take(pageSize).ToList();
        var hasMore = sorted.Count > start + pageSize;
        var nextCursor = hasMore && items.Count > 0
            ? _cursor.Encode(new PageCursor { AfterHash = items[items.Count - 1].ContentHash })
            : null;

        return new Page<IndexEntry>
        {
            Items = items,
            HasMore = hasMore,
            NextCursor = nextCursor
        };
    }

    private static string? CursorAfterHash(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor)) return null;
        try
        {
            return new Base64JsonCursorCodec().Decode(cursor!) is PageCursor pc ? pc.AfterHash : null;
        }
        catch
        {
            return null;
        }
    }

    private static int FirstIndexAfter(IReadOnlyList<IndexEntry> sorted, string afterHash)
    {
        for (var i = 0; i < sorted.Count; i++)
        {
            var cmp = string.Compare(sorted[i].ContentHash, afterHash, StringComparison.Ordinal);
            if (cmp > 0) return i;
        }
        return sorted.Count;
    }

    private static IReadOnlyList<IndexEntry> Sort(IEnumerable<IndexEntry> entries, IndexSort sort)
    {
        var direction = sort.Direction == SortDirection.Descending ? -1 : 1;
        var field = sort.Field;

        var sorted = entries.OrderBy(e => e, Comparer<IndexEntry>.Create((a, b) =>
        {
            var r = CompareField(a, b, field) * direction;
            return r != 0 ? r : string.Compare(a.ContentHash, b.ContentHash, StringComparison.Ordinal);
        })).ToList();

        return sorted;
    }

    private static int CompareField(IndexEntry a, IndexEntry b, string field)
    {
        switch (field)
        {
            case "SizeBytes":
                return a.SizeBytes.CompareTo(b.SizeBytes);
            case "ObjectKey":
                return string.Compare(a.ObjectKey, b.ObjectKey, StringComparison.Ordinal);
            case "ModifiedAt":
                return a.ModifiedAt.CompareTo(b.ModifiedAt);
            case "ContentHash":
                return string.Compare(a.ContentHash, b.ContentHash, StringComparison.Ordinal);
            case "CreatedAt":
            default:
                return a.CreatedAt.CompareTo(b.CreatedAt);
        }
    }
}