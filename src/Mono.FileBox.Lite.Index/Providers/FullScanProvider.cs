// FullScanProvider.cs — fallback provider returning the whole namespace as candidates.
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Index;

namespace Mono.FileBox.Lite.Index.Providers;

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