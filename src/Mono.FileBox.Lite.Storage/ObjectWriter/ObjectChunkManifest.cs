// <file>
// ObjectChunkManifest: data contract representing chunks an object was split into.
// </file>

namespace Mono.FileBox.Lite.Storage.ObjectWriter;

/// <summary>
/// Object-level manifest produced when an object is split into fixed-size chunks.
/// Chunks are owned by the object (no cross-object sharing): the manifest lists the
/// chunk hashes in order, and the physical chunk blocks live under the shared block
/// pool keyed by chunk hash.
/// </summary>
public sealed class ObjectChunkManifest
{
    public string ContentHash { get; set; } = string.Empty;
    public long TotalSize { get; set; }
    public long ChunkSize { get; set; }
    public List<string> ChunkHashes { get; set; } = new();
}