namespace Mono.FileBox.Lite.Abstractions.Index;

/// <summary>Storage tier an object's physical block resides on.</summary>
public enum StorageTier
{
    Hot,
    Warm,
    Cold,
    Archive
}

/// <summary>A single indexed metadata entry for a stored object.</summary>
public sealed class IndexEntry
{
    public string ContentHash { get; init; } = string.Empty;
    public string NamespaceId { get; init; } = string.Empty;
    public string? ObjectKey { get; init; }
    public IReadOnlyDictionary<string, string> Tags { get; init; }
        = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, object> Attributes { get; init; }
        = new Dictionary<string, object>();
    public StorageTier Tier { get; init; }
    public long SizeBytes { get; init; }
    public string? ContentType { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset ModifiedAt { get; init; }
    public ObjectState State { get; init; }
}

/// <summary>Field-level update applied to an existing index entry.</summary>
public sealed class IndexUpdate
{
    public string? ObjectKey { get; init; }
    public StorageTier? Tier { get; init; }
    public ObjectState? State { get; init; }
    public long? SizeBytes { get; init; }
    public string? ContentType { get; init; }
    public DateTimeOffset? ModifiedAt { get; init; }
    public IReadOnlyDictionary<string, string>? Tags { get; init; }
    public IReadOnlyDictionary<string, object>? Attributes { get; init; }
    public IReadOnlyList<string>? RemoveTags { get; init; }
    public IReadOnlyList<string>? RemoveAttributes { get; init; }
}

/// <summary>Pagination cursor request.</summary>
public sealed class PageRequest
{
    public int Size { get; init; } = 100;
    public string? Cursor { get; init; }
}

/// <summary>A stable page of results with an opaque continuation cursor.</summary>
public sealed class Page<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public string? NextCursor { get; init; }
    public bool HasMore { get; init; }

    public static Page<T> Empty(string? cursor = null) => new()
    {
        Items = Array.Empty<T>(),
        NextCursor = cursor,
        HasMore = false
    };
}

/// <summary>Inclusive range of date/time values.</summary>
public sealed class TimeRange
{
    public DateTimeOffset? Start { get; init; }
    public DateTimeOffset? End { get; init; }

    public bool Contains(DateTimeOffset value)
        => (!Start.HasValue || value >= Start.Value)
           && (!End.HasValue || value <= End.Value);

    public static TimeRange All() => new();
    public static TimeRange Between(DateTimeOffset start, DateTimeOffset end)
        => new() { Start = start, End = end };
}

/// <summary>Inclusive range of 64-bit values.</summary>
public sealed class LongRange
{
    public long? Min { get; init; }
    public long? Max { get; init; }

    public static LongRange AtLeast(long min) => new() { Min = min };
    public static LongRange AtMost(long max) => new() { Max = max };
    public static LongRange Between(long min, long max) => new() { Min = min, Max = max };
}

/// <summary>Inclusive range over a numeric attribute value.</summary>
public sealed class AttributeRange
{
    public string AttributeName { get; init; } = string.Empty;
    public object? Min { get; init; }
    public object? Max { get; init; }

    public bool Contains(object? value)
    {
        if (value is not IComparable comparable) return true;
        if (Min is IComparable minCmp && comparable.CompareTo(minCmp) < 0) return false;
        if (Max is IComparable maxCmp && comparable.CompareTo(maxCmp) > 0) return false;
        return true;
    }
}

/// <summary>Sort direction.</summary>
public enum SortDirection { Ascending, Descending }

/// <summary>Sort specification for an index query.</summary>
public sealed class IndexSort
{
    public string Field { get; init; } = "CreatedAt";
    public SortDirection Direction { get; init; } = SortDirection.Ascending;
}

/// <summary>An index predicate extracted from a query (one per queryable dimension).</summary>
public sealed class IndexPredicate
{
    public PredicateKind Kind { get; init; }
    public string Field { get; init; } = string.Empty;
    public object? Value { get; init; }
    public object? Operator { get; init; }
}

/// <summary>Kinds of index predicates that a provider may claim to serve.</summary>
public enum PredicateKind
{
    KeyPrefix,
    Tag,
    Attribute,
    Tier,
    State,
    CreatedAt,
    ModifiedAt,
    Size
}

/// <summary>A full multi-dimensional index query.</summary>
public sealed class IndexQuery
{
    public string NamespaceId { get; init; } = string.Empty;
    public string? KeyPrefix { get; init; }
    public IReadOnlyDictionary<string, string>? Tags { get; init; }
    public IReadOnlyList<AttributeRange>? AttributeRanges { get; init; }
    public IReadOnlyList<StorageTier>? Tiers { get; init; }
    public IReadOnlyList<ObjectState>? States { get; init; }
    public TimeRange? CreatedAtRange { get; init; }
    public TimeRange? ModifiedAtRange { get; init; }
    public LongRange? SizeRange { get; init; }
    public IndexSort? Sort { get; init; }
    public PageRequest Page { get; init; } = new();
}

/// <summary>An executable scan over a single index. Returns candidate content hashes.</summary>
public interface IIndexScan
{
    string IndexName { get; }
    double EstimatedCost { get; }
    bool ProvidesSort(IndexSort sort);
    Task<IReadOnlyList<string>> ExecuteAsync(CancellationToken ct);
}

/// <summary>An executed query plan: one driving scan plus an ordered set of filters.</summary>
public sealed class QueryPlan
{
    public string NamespaceId { get; init; } = string.Empty;
    public IIndexScan Driver { get; init; } = null!;
    public IReadOnlyList<IIndexScan> Filters { get; init; } = Array.Empty<IIndexScan>();
    public IndexSort? Sort { get; init; }
    public PageRequest Page { get; init; } = new();
}

/// <summary>Produces an execution plan for a query given the available providers.</summary>
public interface IQueryPlanner
{
    QueryPlan Plan(IndexQuery query);
}

/// <summary>Executes a query plan and returns paged <see cref="IndexEntry"/> results.</summary>
public interface IQueryExecutor
{
    Task<Page<IndexEntry>> ExecuteAsync(QueryPlan plan, CancellationToken ct);
}

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

/// <summary>Verifies index consistency against the authoritative physical blocks.</summary>
public sealed class IndexConsistencyReport
{
    public IReadOnlyList<string> MissingIndexEntries { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> OrphanedIndexEntries { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> StateMismatches { get; init; } = Array.Empty<string>();
    public bool IsConsistent => MissingIndexEntries.Count == 0
                                && OrphanedIndexEntries.Count == 0
                                && StateMismatches.Count == 0;
}

/// <summary>Options for a full index rebuild cycle.</summary>
public sealed class IndexRebuildOptions
{
    public bool DropExisting { get; init; } = true;
    public bool RebuildBitmapIndexes { get; init; } = true;
    public int BatchSize { get; init; } = 500;
}

/// <summary>Primary ordered key/value storage behind the index providers.</summary>
public interface IOrderedKeyValueStore
{
    Task PutAsync(byte[] key, byte[]? value, CancellationToken ct);
    Task<byte[]?> GetAsync(byte[] key, CancellationToken ct);
    Task DeleteAsync(byte[] key, CancellationToken ct);
    Task<bool> ExistsAsync(byte[] key, CancellationToken ct);

    /// <summary>Enumerates key/value pairs within [start, end) lexicographically.</summary>
    Task<IReadOnlyList<KeyValuePair<byte[], byte[]>>> ScanAsync(
        byte[]? start, byte[]? end, CancellationToken ct);
}

/// <summary>Materializes and serializes a full <see cref="IndexEntry"/>.</summary>
public interface IEntryStore
{
    Task PutAsync(string contentHash, IndexEntry entry, CancellationToken ct);
    Task<IndexEntry?> GetAsync(string contentHash, CancellationToken ct);
    Task DeleteAsync(string contentHash, CancellationToken ct);
    Task<IReadOnlyList<IndexEntry>> GetManyAsync(IEnumerable<string> hashes, CancellationToken ct);
}

/// <summary>Encodes/decodes opaque pagination cursors.</summary>
public interface ICursorCodec
{
    string Encode(object cursor);
    object Decode(string cursor);
}