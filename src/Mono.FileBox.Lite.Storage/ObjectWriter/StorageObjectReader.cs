using Mono.FileBox.Lite.Abstractions.Subsystems;
using Mono.FileBox.Lite.Abstractions.Storage;

namespace Mono.FileBox.Lite.Storage.ObjectWriter;

/// <summary>Range reader that locates the owning disk and reads through the I/O pipeline.</summary>
public sealed class StorageObjectReader : IObjectReader
{
    private readonly IDiskSelector _selector;
    private readonly IIOPipeline _pipeline;

    public StorageObjectReader(IDiskSelector selector, IIOPipeline pipeline)
    {
        _selector = selector;
        _pipeline = pipeline;
    }

    public async Task<Stream> ReadAsync(
        string contentHash, long offset, long length, CancellationToken ct)
    {
        var disk = await _selector.SelectForReadAsync(contentHash, ct).ConfigureAwait(false);
        return await _pipeline.ReadAsync(disk, contentHash, offset, length, ct).ConfigureAwait(false);
    }
}