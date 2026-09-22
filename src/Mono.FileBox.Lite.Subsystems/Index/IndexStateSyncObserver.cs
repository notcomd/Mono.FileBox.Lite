// File-level documentation: Observer keeping the index entry's state and tier in sync
// as an object moves through later lifecycle transitions.
// Extracted from the original multi-type Index/DefaultIndexWriter.cs.
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Index;
using Mono.FileBox.Lite.Abstractions.Storage;
using Mono.FileBox.Lite.Abstractions.Subsystems;

namespace Mono.FileBox.Lite.Subsystems.Index;

/// <summary>
/// Observer that keeps the index entry's <see cref="IndexEntry.State"/> and
/// tier in sync as the object moves through later lifecycle transitions.
/// </summary>
public sealed class IndexStateSyncObserver : ITransitionObserver
{
    private readonly IIndexWriter _writer;

    public IndexStateSyncObserver(IIndexWriter writer) => _writer = writer;

    public Task OnBeforeAsync(ObjectTrigger t, IObjectContext ctx, CancellationToken ct)
        => Task.CompletedTask;

    public Task OnAfterAsync(ObjectTrigger t, IObjectContext ctx, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ctx.ContentHash)) return Task.CompletedTask;

        var tier = ctx.Items.TryGetValue(ObjectContextKeys.WriteOptions, out var w) && w is WriteOptions wo
            ? wo.Tier : (StorageTier?)null;

        return _writer.UpdateAsync(ctx.ContentHash, new IndexUpdate
        {
            State = ctx.CurrentState,
            Tier = tier,
            ModifiedAt = DateTimeOffset.UtcNow
        }, ct);
    }
}