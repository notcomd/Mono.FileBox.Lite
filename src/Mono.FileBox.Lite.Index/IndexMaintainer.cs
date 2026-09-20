using Mono.FileBox.Lite.Abstractions.Index;
using Mono.FileBox.Lite.Abstractions.Subsystems;

namespace Mono.FileBox.Lite.Index;

/// <summary>
/// Maintains index consistency. The physical blocks are authoritative; the index is a
/// derived projection. Verify detects entries whose physical block is absent, and Repair
/// removes such orphaned entries.
/// </summary>
public sealed class IndexMaintainer : IIndexMaintainer
{
    private readonly IEntryStore _store;
    private readonly IObjectWriter _content;
    private readonly IIndexWriter? _writer;

    public IndexMaintainer(IEntryStore store, IObjectWriter content, IIndexWriter? writer = null)
    {
        _store = store;
        _content = content;
        _writer = writer;
    }

    public async Task RebuildAsync(IndexRebuildOptions options, CancellationToken ct)
    {
        await _store.ClearAsync(ct).ConfigureAwait(false);
    }

    public async Task<IndexConsistencyReport> VerifyAsync(CancellationToken ct)
    {
        var entries = await _store.ListAllAsync(ct).ConfigureAwait(false);
        var missing = new List<string>();   // entry references a block that is absent -> block is authoritative
        var orphaned = new List<string>();  // no index entry for a block that exists (requires a block scan; not detected here)

        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.ContentHash)) continue;
            var exists = await _content.ExistsAsync(entry.ContentHash, ct).ConfigureAwait(false);
            if (!exists) missing.Add(entry.ContentHash);
        }

        return new IndexConsistencyReport
        {
            MissingIndexEntries = missing,
            OrphanedIndexEntries = orphaned,
            StateMismatches = Array.Empty<string>()
        };
    }

    public async Task RepairAsync(IndexConsistencyReport report, CancellationToken ct)
    {
        foreach (var hash in report.MissingIndexEntries)
            await _store.DeleteAsync(hash, ct).ConfigureAwait(false);
    }
}