// QueryPredicateExtractor.cs — expands a full index query into claimable predicates.
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