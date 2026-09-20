using System.Buffers;
using System.Security.Cryptography;
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Configuration;
using Mono.FileBox.Lite.Abstractions.Subsystems;
using Mono.FileBox.Lite.Abstractions.Storage;

namespace Mono.FileBox.Lite.Storage.ObjectWriter;

/// <summary>
/// Content-addressed object writer. Computes the SHA-256 key by streaming the source,
/// reuses an existing physical block when the hash is already present (deduplication),
/// otherwise persists the block via the disk selector + I/O pipeline.
///
/// Two layouts are supported, selectable through <see cref="ChunkingOptions"/>:
/// <list type="bullet">
/// <item><b>Legacy</b> (chunking disabled, or object ≤ one chunk): the whole object is one block.</item>
/// <item><b>Chunked</b> (chunking enabled and object &gt; one chunk): the object is split into
/// fixed-size chunks, each stored as its own block (keyed by chunk hash), plus an
/// object-level manifest.</item>
/// </list>
///
/// Memory behaviour is bounded for both layouts: sources are hashed in place (seekable)
/// or spooled to a temp file (non-seekable) and never fully buffered.
/// Also acts as the <see cref="ITransitionAction"/> for the <c>Put</c> transition.
/// </summary>
public sealed class Sha256ObjectWriter : IObjectWriter, ITransitionAction
{
    private readonly IDiskSelector _selector;
    private readonly IIOPipeline _pipeline;
    private readonly IPhysicalDevice _device;
    private readonly ChunkingOptions _chunking;

    public Sha256ObjectWriter(
        IDiskSelector selector,
        IIOPipeline pipeline,
        IPhysicalDevice device,
        ChunkingOptions? chunking = null)
    {
        _selector = selector;
        _pipeline = pipeline;
        _device = device;
        _chunking = chunking ?? new ChunkingOptions();
    }

    public async Task<string> WriteAsync(
        Stream content, WriteOptions options, CancellationToken ct)
    {
        // Obtain a rewindable source plus its total size.
        var (source, totalSize, cleanup) = await OpenSourceAsync(content, ct).ConfigureAwait(false);
        try
        {
            // Pass 1: hash the whole object.
            if (source.CanSeek) source.Position = 0;
            var objectHash = Sha256.Compute(source, ct);
            if (await ExistsAsync(objectHash, ct).ConfigureAwait(false))
                return objectHash; // deduplication hit

            var disk = await _selector.SelectForWriteAsync(objectHash, options, ct).ConfigureAwait(false);

            if (ShouldChunk(totalSize))
                await WriteChunkedAsync(source, disk, objectHash, totalSize, options, ct).ConfigureAwait(false);
            else
                await WriteLegacyAsync(source, disk, objectHash, options, ct).ConfigureAwait(false);

            return objectHash;
        }
        finally
        {
            cleanup();
        }
    }

    public async Task<bool> ExistsAsync(string contentHash, CancellationToken ct)
    {
        var disk = await _selector.SelectForReadAsync(contentHash, ct).ConfigureAwait(false);
        if (disk is LocalDiskHandle local
            && await _device.ExistsAsync(ObjectPathMapper.ResolveManifest(local.RootPath, contentHash), ct).ConfigureAwait(false))
            return true; // chunked object
        return await _pipeline.ExistsAsync(disk, contentHash, ct).ConfigureAwait(false); // legacy block
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

    // -------- layout decisions --------

    private bool ShouldChunk(long totalSize)
        => _chunking.Enabled
           && _chunking.ChunkSizeBytes > 0
           && totalSize > _chunking.ChunkSizeBytes;

    private async Task<(Stream source, long totalSize, Action cleanup)> OpenSourceAsync(Stream content, CancellationToken ct)
    {
        if (content.CanSeek)
        {
            content.Position = 0;
            return (content, content.Length, () => { });
        }

        // Non-seekable: spool to a temp file so we can hash and re-read deterministically.
        var temp = Path.GetTempFileName();
        using (var shaabled = content)
        {
            using var fs = File.Create(temp);
            var buffer = ArrayPool<byte>.Shared.Rent(81920);
            try
            {
                int read;
                while ((read = await content.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false)) > 0)
                    await fs.WriteAsync(buffer, 0, read, ct).ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        var spool = File.OpenRead(temp);
        return (spool, spool.Length, () => { spool.Dispose(); if (File.Exists(temp)) File.Delete(temp); });
    }

    private async Task WriteLegacyAsync(
        Stream source, IDiskHandle disk, string objectHash, WriteOptions options, CancellationToken ct)
    {
        if (source.CanSeek) source.Position = 0;
        await _pipeline.WriteAsync(disk, objectHash, source, options, ct).ConfigureAwait(false);
    }

    private async Task WriteChunkedAsync(
        Stream source, IDiskHandle disk, string objectHash, long totalSize,
        WriteOptions options, CancellationToken ct)
    {
        if (source.CanSeek) source.Position = 0;
        var chunkSize = (int)_chunking.ChunkSizeBytes;
        var manifest = new ObjectChunkManifest
        {
            ContentHash = objectHash,
            TotalSize = totalSize,
            ChunkSize = chunkSize
        };

        var buffer = ArrayPool<byte>.Shared.Rent(chunkSize);
        try
        {
            while (source.Position < totalSize)
            {
                var got = await ReadFullyAsync(source, buffer, chunkSize, ct).ConfigureAwait(false);
                if (got == 0) break;

                using var sha = SHA256.Create();
                var chunkHash = ToHex(sha.ComputeHash(buffer, 0, got));

                using var chunk = new MemoryStream(buffer, 0, got, writable: false);
                await _pipeline.WriteAsync(disk, chunkHash, chunk, options, ct).ConfigureAwait(false);

                manifest.ChunkHashes.Add(chunkHash);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        if (disk is LocalDiskHandle local)
            await ChunkManifestCodec.WriteAsync(_device, local, manifest, ct).ConfigureAwait(false);
    }

    private static async Task<int> ReadFullyAsync(Stream source, byte[] buffer, int count, CancellationToken ct)
    {
        var read = 0;
        while (read < count)
        {
            var r = await source.ReadAsync(buffer, read, count - read, ct).ConfigureAwait(false);
            if (r == 0) break;
            read += r;
        }
        return read;
    }

    private static string ToHex(byte[] hash)
        => BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
}