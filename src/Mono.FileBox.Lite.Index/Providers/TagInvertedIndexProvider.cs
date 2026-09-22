// TagInvertedIndexProvider.cs — tag inverted-index provider.
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Index;

namespace Mono.FileBox.Lite.Index.Providers;

/// <summary>Tag inverted-index provider.</summary>
public sealed class TagInvertedIndexProvider : SingleKindProvider
{
    private readonly IEntryStore _store;
    public TagInvertedIndexProvider(IEntryStore store) : base(PredicateKind.Tag, 20) => _store = store;
    protected override IEntryStore Store => _store;
    public override double EstimateSelectivity(IndexPredicate p) => 1.0 / 64.0;
}