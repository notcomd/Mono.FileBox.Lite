using Mono.FileBox.Lite.Abstractions.Index;

namespace Mono.FileBox.Lite.Index.Planning;

/// <summary>
/// Expands a full <see cref="IndexQuery"/> into a list of <see cref="IndexPredicate"/>
/// that can be claimed by index providers.
/// </summary>
public static class QueryPredicateExtractor
{
    public static IReadOnlyList<IndexPredicate> Extract(IndexQuery query)
    {
        var predicates = new List<IndexPredicate>();

        if (query.KeyPrefix is not null)
            predicates.Add(new IndexPredicate { Kind = PredicateKind.KeyPrefix, Value = query.KeyPrefix });

        if (query.Tags is not null)
            foreach (var tag in query.Tags)
                predicates.Add(new IndexPredicate
                {
                    Kind = PredicateKind.Tag,
                    Field = tag.Key,
                    Value = tag.Value
                });

        if (query.AttributeRanges is not null)
            foreach (var range in query.AttributeRanges)
                predicates.Add(new IndexPredicate
                {
                    Kind = PredicateKind.Attribute,
                    Field = range.AttributeName,
                    Value = range
                });

        if (query.Tiers is not null)
            predicates.Add(new IndexPredicate { Kind = PredicateKind.Tier, Value = query.Tiers });

        if (query.States is not null)
            predicates.Add(new IndexPredicate { Kind = PredicateKind.State, Value = query.States });

        if (query.CreatedAtRange is not null)
            predicates.Add(new IndexPredicate { Kind = PredicateKind.CreatedAt, Value = query.CreatedAtRange });

        if (query.ModifiedAtRange is not null)
            predicates.Add(new IndexPredicate { Kind = PredicateKind.ModifiedAt, Value = query.ModifiedAtRange });

        if (query.SizeRange is not null)
            predicates.Add(new IndexPredicate { Kind = PredicateKind.Size, Value = query.SizeRange });

        return predicates;
    }
}

/// <summary>
/// Evaluates whether an <see cref="IndexEntry"/> satisfies every predicate of a query.
/// Used by the executor as the authoritative final filter.
/// </summary>
public static class IndexEntryMatcher
{
    public static bool Matches(IndexEntry e, IndexQuery q)
    {
        if (q.KeyPrefix is not null
            && (e.ObjectKey is null || !e.ObjectKey.StartsWith(q.KeyPrefix, StringComparison.Ordinal)))
            return false;

        if (q.Tags is not null)
            foreach (var tag in q.Tags)
                if (!e.Tags.TryGetValue(tag.Key, out var v) || !string.Equals(v, tag.Value, StringComparison.Ordinal))
                    return false;

        if (q.AttributeRanges is not null)
            foreach (var range in q.AttributeRanges)
            {
                if (!e.Attributes.TryGetValue(range.AttributeName, out var value)) return false;
                if (value is not IComparable comparable) continue;
                if (range.Min is IComparable minCmp && comparable.CompareTo(minCmp) < 0) return false;
                if (range.Max is IComparable maxCmp && comparable.CompareTo(maxCmp) > 0) return false;
            }

        if (q.Tiers is not null && !q.Tiers.Contains(e.Tier)) return false;
        if (q.States is not null && !q.States.Contains(e.State)) return false;
        if (q.CreatedAtRange is not null && !q.CreatedAtRange.Contains(e.CreatedAt)) return false;
        if (q.ModifiedAtRange is not null && !q.ModifiedAtRange.Contains(e.ModifiedAt)) return false;
        if (q.SizeRange is not null)
        {
            if (q.SizeRange.Min.HasValue && e.SizeBytes < q.SizeRange.Min.Value) return false;
            if (q.SizeRange.Max.HasValue && e.SizeBytes > q.SizeRange.Max.Value) return false;
        }

        return true;
    }
}