using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Index;

namespace Mono.FileBox.Lite.Index.Providers;

/// <summary>
/// A scan over a single predicate that yields candidate content hashes by filtering
/// the namespace's entries. Lightweight in-memory implementation of the index scan.
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

/// <summary>Base helper for single-kind providers.</summary>
public abstract class SingleKindProvider : IIndexProvider
{
    private readonly PredicateKind _kind;
    private readonly int _priority;
    protected SingleKindProvider(PredicateKind kind, int priority)
    {
        _kind = kind;
        _priority = priority;
    }

    public string Name => $"idx:{_kind.ToString().ToLowerInvariant()}";
    public int Priority => _priority;

    public virtual bool CanHandle(IndexPredicate predicate) => predicate.Kind == _kind;

    public virtual double EstimateSelectivity(IndexPredicate predicate) => 1.0 / 32.0;

    public IIndexScan CreateScan(IndexQuery query, IndexPredicate predicate)
        => new PredicateScan(Store, query.NamespaceId, predicate);

    protected abstract IEntryStore Store { get; }
}

/// <summary>Prefix (ObjectKey) provider.</summary>
public sealed class PrefixIndexProvider : SingleKindProvider
{
    private readonly IEntryStore _store;
    public PrefixIndexProvider(IEntryStore store) : base(PredicateKind.KeyPrefix, 10) => _store = store;
    protected override IEntryStore Store => _store;
    public override double EstimateSelectivity(IndexPredicate p)
    {
        var len = (p.Value as string)?.Length ?? 0;
        return 1.0 / (len + 1);
    }
}

/// <summary>Tag inverted-index provider.</summary>
public sealed class TagInvertedIndexProvider : SingleKindProvider
{
    private readonly IEntryStore _store;
    public TagInvertedIndexProvider(IEntryStore store) : base(PredicateKind.Tag, 20) => _store = store;
    protected override IEntryStore Store => _store;
    public override double EstimateSelectivity(IndexPredicate p) => 1.0 / 64.0;
}

/// <summary>Attribute range provider.</summary>
public sealed class AttributeIndexProvider : SingleKindProvider
{
    private readonly IEntryStore _store;
    public AttributeIndexProvider(IEntryStore store) : base(PredicateKind.Attribute, 30) => _store = store;
    protected override IEntryStore Store => _store;
}

/// <summary>Tier bitmap provider.</summary>
public sealed class TierBitmapProvider : SingleKindProvider
{
    private readonly IEntryStore _store;
    public TierBitmapProvider(IEntryStore store) : base(PredicateKind.Tier, 40) => _store = store;
    protected override IEntryStore Store => _store;
}

/// <summary>State bitmap provider.</summary>
public sealed class StateBitmapProvider : SingleKindProvider
{
    private readonly IEntryStore _store;
    public StateBitmapProvider(IEntryStore store) : base(PredicateKind.State, 50) => _store = store;
    protected override IEntryStore Store => _store;
}

/// <summary>Time index provider (handles CreatedAt and ModifiedAt).</summary>
public sealed class TimeIndexProvider : SingleKindProvider
{
    private readonly IEntryStore _store;
    public TimeIndexProvider(IEntryStore store) : base(PredicateKind.CreatedAt, 60) => _store = store;
    protected override IEntryStore Store => _store;
    public override bool CanHandle(IndexPredicate p)
        => p.Kind == PredicateKind.CreatedAt || p.Kind == PredicateKind.ModifiedAt;
}

/// <summary>Size B-tree provider.</summary>
public sealed class SizeIndexProvider : SingleKindProvider
{
    private readonly IEntryStore _store;
    public SizeIndexProvider(IEntryStore store) : base(PredicateKind.Size, 70) => _store = store;
    protected override IEntryStore Store => _store;
}

/// <summary>
/// Fallback provider that returns the whole namespace as the candidate set. Used as
/// the driver when the query has no claimable predicate.
/// </summary>
public sealed class FullScanProvider : IIndexProvider
{
    private readonly IEntryStore _store;
    public FullScanProvider(IEntryStore store) => _store = store;

    public string Name => "idx:fullscan";
    public int Priority => 1000;
    public bool CanHandle(IndexPredicate p) => true;
    public double EstimateSelectivity(IndexPredicate p) => 1.0;

    public IIndexScan CreateScan(IndexQuery query, IndexPredicate predicate)
        => new FullScan(_store, query.NamespaceId);

    private sealed class FullScan : IIndexScan
    {
        private readonly IEntryStore _store;
        private readonly string _ns;
        public FullScan(IEntryStore store, string ns) { _store = store; _ns = ns; }
        public string IndexName => "fullscan";
        public double EstimatedCost => 100.0;
        public bool ProvidesSort(IndexSort sort) => false;
        public async Task<IReadOnlyList<string>> ExecuteAsync(CancellationToken ct)
            => (await _store.ListByNamespaceAsync(_ns, ct).ConfigureAwait(false))
                .Select(e => e.ContentHash).ToArray();
    }
}