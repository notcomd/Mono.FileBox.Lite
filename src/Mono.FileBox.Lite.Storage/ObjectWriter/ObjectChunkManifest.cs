// <file>
// ObjectChunkManifest: data contract representing chunks an object was split into.
// </file>

namespace Mono.FileBox.Lite.Storage.ObjectWriter;

/// <summary>
/// Object-level manifest produced when an object is split into fixed-size chunks.
/// Chunks are owned by the object (no cross-object sharing): the manifest lists the
/// chunk hashes in order, and the physical chunk blocks live under the shared block
/// pool keyed by chunk hash.
/// 对象被拆分为固定大小分块时产生的对象级清单；分块归对象所有（不跨对象共享），清单按顺序列出分块哈希，物理分块块位于共享块池中并以分块哈希为键。
/// </summary>
public sealed class ObjectChunkManifest
{
    public string ContentHash { get; set; } = string.Empty;
    public long TotalSize { get; set; }
    public long ChunkSize { get; set; }
    public List<string> ChunkHashes { get; set; } = new();
}