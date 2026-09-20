using System.Security.Cryptography;
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Subsystems;
using Mono.FileBox.Lite.Abstractions.Storage;

namespace Mono.FileBox.Lite.Storage.ObjectWriter;

/// <summary>
/// Content-addressed object writer. Computes the SHA-256 key by streaming the source,
/// reuses an existing physical block when the hash is already present (deduplication),
/// otherwise persists the block via the disk selector + I/O pipeline.
///
/// Memory behaviour is bounded: seekable sources are hashed in place and then
/// streamed to disk; non-seekable sources are spooled to a temporary file while being
/// hashed, so the whole payload is never buffered in memory.
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
        if (content.CanSeek)
        {
            // Streaming hash in place, then rewind and stream the write.
            content.Position = 0;
            var hash = Sha256.Compute(content, ct);
            if (await ExistsAsync(hash, ct).ConfigureAwait(false))
                return hash;

            var disk = await _selector.SelectForWriteAsync(hash, options, ct).ConfigureAwait(false);
            await _pipeline.WriteAsync(disk, hash, content, options, ct).ConfigureAwait(false);
            return hash;
        }

        // Non-seekable source: spool to a temporary file while hashing.
        var temp = Path.GetTempFileName();
        try
        {
            string hash;
            using (var sha = SHA256.Create())
            {
                using (var fs = File.Create(temp))
                {
                    var buffer = new byte[81920];
                    int read;
                    while ((read = await content.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false)) > 0)
                    {
                        sha.TransformBlock(buffer, 0, read, buffer, 0);
                        await fs.WriteAsync(buffer, 0, read, ct).ConfigureAwait(false);
                    }
                    sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                }
                hash = Hex(sha.Hash!);
            }

            if (await ExistsAsync(hash, ct).ConfigureAwait(false))
                return hash;

            var disk = await _selector.SelectForWriteAsync(hash, options, ct).ConfigureAwait(false);
            using var spool = File.OpenRead(temp);
            await _pipeline.WriteAsync(disk, hash, spool, options, ct).ConfigureAwait(false);
            return hash;
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
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

        ctx.Items[ObjectContextKeys.SizeBytes] = content.CanSeek ? content.Length : 0L;
    }

    private static string Hex(byte[] hash)
        => BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
}