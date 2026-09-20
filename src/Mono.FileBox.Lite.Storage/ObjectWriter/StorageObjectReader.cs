using Mono.FileBox.Lite.Abstractions.Subsystems;
using Mono.FileBox.Lite.Abstractions.Storage;

namespace Mono.FileBox.Lite.Storage.ObjectWriter;

/// <summary>
/// Range reader. For legacy (single-block) objects it reads one block slice; for
/// chunked objects it maps the requested [offset, length) range across chunk covers
/// and concatenates the per-chunk slices into the returned stream.
/// </summary>
public sealed class StorageObjectReader : IObjectReader
{
    private readonly IDiskSelector _selector;
    private readonly IIOPipeline _pipeline;
    private readonly IPhysicalDevice _device;

    public StorageObjectReader(
        IDiskSelector selector, IIOPipeline pipeline, IPhysicalDevice device)
    {
        _selector = selector;
        _pipeline = pipeline;
        _device = device;
    }

    public async Task<Stream> ReadAsync(
        string contentHash, long offset, long length, CancellationToken ct)
    {
        var disk = await _selector.SelectForReadAsync(contentHash, ct).ConfigureAwait(false);

        var manifest = disk is LocalDiskHandle local
            ? await ChunkManifestCodec.ReadAsync(_device, local, contentHash, ct).ConfigureAwait(false)
            : null;

        if (manifest is null)
            return await _pipeline.ReadAsync(disk, contentHash, offset, length, ct).ConfigureAwait(false);

        // Chunked read: compute the covering chunk range.
        var start = Math.Max(0, offset);
        var end = length < 0
            ? manifest.TotalSize
            : Math.Min(manifest.TotalSize, checked(offset + length));
        if (start >= end) return new MemoryStream(Array.Empty<byte>(), writable: false);

        var result = new MemoryStream();
        var chunkBegin = 0L;
        foreach (var chunkHash in manifest.ChunkHashes)
        {
            var chunkEnd = Math.Min(chunkBegin + manifest.ChunkSize, manifest.TotalSize);
            var overlapStart = Math.Max(start, chunkBegin);
            var overlapEnd = Math.Min(end, chunkEnd);

            if (overlapStart < overlapEnd)
            {
                var localOffset = overlapStart - chunkBegin;
                var localLength = overlapEnd - overlapStart;
                using var part = await _pipeline.ReadAsync(disk, chunkHash, localOffset, localLength, ct)
                    .ConfigureAwait(false);
                await part.CopyToAsync(result, 81920, ct).ConfigureAwait(false);
            }
            chunkBegin = chunkEnd;
        }

        result.Position = 0;
        return result;
    }
}