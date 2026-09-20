using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Subsystems;
using Mono.FileBox.Lite.Abstractions.Storage;
using Mono.FileBox.Lite.Storage.DiskSelector;

namespace Mono.FileBox.Lite.Storage.ObjectWriter;

/// <summary>
/// Content-addressed object writer. Computes the SHA-256 key of the incoming stream,
/// reuses an existing physical block when the hash is already present (deduplication),
/// otherwise persists the block via the disk selector + I/O pipeline.
/// Also acts as the <see cref="ITransitionAction"/> for the <c>Put</c> transition.
/// </summary>
public sealed class Sha256ObjectWriter : IObjectWriter, ITransitionAction
{
    private readonly IDiskSelector _selector;
    private readonly IIOPipeline _pipeline;

    public Sha256ObjectWriter(IDiskSelector selector, IIOPipeline pipeline)
    {
        _selector = selector;
        _pipeline = pipeline;
    }

    public async Task<string> WriteAsync(
        Stream content, WriteOptions options, CancellationToken ct)
    {
        var bytes = await ReadAllAsync(content, ct).ConfigureAwait(false);
        var hash = Sha256.Compute(bytes);

        if (await ExistsAsync(hash, ct).ConfigureAwait(false))
            return hash; // deduplication hit: reuse existing physical block

        var disk = await _selector.SelectForWriteAsync(hash, options, ct).ConfigureAwait(false);
        using var buffer = new MemoryStream(bytes, writable: false);
        await _pipeline.WriteAsync(disk, hash, buffer, options, ct).ConfigureAwait(false);
        return hash;
    }

    public async Task<bool> ExistsAsync(string contentHash, CancellationToken ct)
    {
        var disk = await _selector.SelectForReadAsync(contentHash, ct).ConfigureAwait(false);
        return await _pipeline.ExistsAsync(disk, contentHash, ct).ConfigureAwait(false);
    }

    public async Task ExecuteAsync(IObjectContext ctx, CancellationToken ct)
    {
        var hasContent = ctx.Items.TryGetValue(ObjectContextKeys.ContentStream, out var contentObj)
                 && contentObj is Stream stream;
        if (!hasContent)
            throw new InvalidOperationException("No content stream supplied for the Put transition.");
        var content = (Stream)contentObj!;

        var options = ctx.Items.TryGetValue(ObjectContextKeys.WriteOptions, out var optObj)
            ? optObj as WriteOptions
            : null;
        options ??= new WriteOptions();

        var hash = await WriteAsync(content, options, ct).ConfigureAwait(false);
        if (ctx is IMutableObjectContext mutable)
            mutable.ContentHash = hash;

        ctx.Items[ObjectContextKeys.SizeBytes] = (long)(content.CanSeek ? content.Length : 0);
    }

    private static async Task<byte[]> ReadAllAsync(Stream content, CancellationToken ct)
    {
        if (content is MemoryStream ms) return ms.ToArray();
        if (content.CanSeek) content.Position = 0;
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, 81920, ct).ConfigureAwait(false);
        return buffer.ToArray();
    }
}