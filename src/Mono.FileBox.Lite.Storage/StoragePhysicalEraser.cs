using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Subsystems;
using Mono.FileBox.Lite.Abstractions.Storage;

namespace Mono.FileBox.Lite.Storage;

/// <summary>
/// Physical erasure for the Purge transition: deletes the physical block and the
/// index entry, confirming both before returning.
/// </summary>
public sealed class StoragePhysicalEraser : IPhysicalEraser, ITransitionAction
{
    private readonly IDiskSelector _selector;
    private readonly IIOPipeline _pipeline;
    private readonly IIndexWriter? _indexWriter;

    public StoragePhysicalEraser(
        IDiskSelector selector,
        IIOPipeline pipeline,
        IIndexWriter? indexWriter = null)
    {
        _selector = selector;
        _pipeline = pipeline;
        _indexWriter = indexWriter;
    }

    public async Task EraseAsync(IObjectContext ctx, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ctx.ContentHash)) return;

        var disk = await _selector.SelectForReadAsync(ctx.ContentHash, ct).ConfigureAwait(false);
        await _pipeline.DeleteAsync(disk, ctx.ContentHash, ct).ConfigureAwait(false);

        if (_indexWriter is not null)
            await _indexWriter.RemoveAsync(ctx.ContentHash, ct).ConfigureAwait(false);
    }

    public Task ExecuteAsync(IObjectContext ctx, CancellationToken ct)
        => EraseAsync(ctx, ct);
}