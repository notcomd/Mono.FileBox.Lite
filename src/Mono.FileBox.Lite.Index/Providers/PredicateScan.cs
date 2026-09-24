// PredicateScan.cs — a single-predicate scan over the namespace's entries.
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Index;

namespace Mono.FileBox.Lite.Index.Providers;

/// <summary>
/// A scan over a single predicate that yields candidate content hashes by filtering
/// the namespace's entries. Lightweight in-memory implementation of the index scan.
/// 针对单一谓词的扫描：通过过滤命名空间内的条目来产出候选内容哈希，
/// 是索引扫描的轻量级内存实现。
/// </summary>
public sealed class PredicateScan : IIndexScan
{
    private readonly IEntryStore _store;
    private readonly string _namespaceId;
    private readonly IndexPredicate _predicate;

    public PredicateScan(IEntryStore store, string namespaceId, IndexPredicate predicate)
    {
        _store = store;
        _namespaceId = namespaceId;
        _predicate = predicate;
    }

    public string IndexName { get; } = "memory";
    public double EstimatedCost => 1.0;

    public bool ProvidesSort(IndexSort sort) => false;

    public async Task<IReadOnlyList<string>> ExecuteAsync(CancellationToken ct)
    {
        var entries = await _store.ListByNamespaceAsync(_namespaceId, ct).ConfigureAwait(false);
        var result = new List<string>();
        foreach (var entry in entries)
        {
            if (Matches(entry, _predicate)) result.Add(entry.ContentHash);
        }
        return result;
    }

    internal static bool Matches(IndexEntry e, IndexPredicate p)
    {
        switch (p.Kind)
        {
            case PredicateKind.KeyPrefix:
                return e.ObjectKey is not null
                       && e.ObjectKey.StartsWith((string)p.Value!, StringComparison.Ordinal);
            case PredicateKind.Tag:
                return e.Tags.TryGetValue(p.Field, out var v)
                       && string.Equals(v, (string)p.Value!, StringComparison.Ordinal);
            case PredicateKind.Attribute:
                if (!e.Attributes.TryGetValue(p.Field, out var value) || value is not IComparable cmp)
                    return false;
                if (p.Value is AttributeRange range)
                {
                    if (range.Min is IComparable lo && cmp.CompareTo(lo) < 0) return false;
                    if (range.Max is IComparable hi && cmp.CompareTo(hi) > 0) return false;
                }
                return true;
            case PredicateKind.Tier:
                return p.Value is IReadOnlyList<StorageTier> tiers && tiers.Contains(e.Tier);
            case PredicateKind.State:
                return p.Value is IReadOnlyList<ObjectState> states && states.Contains(e.State);
            case PredicateKind.CreatedAt:
                return p.Value is TimeRange cr && cr.Contains(e.CreatedAt);
            case PredicateKind.ModifiedAt:
                return p.Value is TimeRange mr && mr.Contains(e.ModifiedAt);
            case PredicateKind.Size:
                return p.Value is LongRange sr
                       && (!sr.Min.HasValue || e.SizeBytes >= sr.Min.Value)
                       && (!sr.Max.HasValue || e.SizeBytes <= sr.Max.Value);
            default:
                return true;
        }
    }
}