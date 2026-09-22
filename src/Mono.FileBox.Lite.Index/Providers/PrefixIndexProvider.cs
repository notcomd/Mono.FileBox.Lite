// PrefixIndexProvider.cs — prefix (ObjectKey) index provider.
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Index;

namespace Mono.FileBox.Lite.Index.Providers;

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