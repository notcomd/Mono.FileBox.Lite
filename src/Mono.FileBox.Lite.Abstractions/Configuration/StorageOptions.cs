// 本文件包含类型 StorageOptions：存储选项。
using Mono.FileBox.Lite.Abstractions.Storage;

namespace Mono.FileBox.Lite.Abstractions.Configuration;

public sealed class StorageOptions
{
    public IList<PoolOptions> Pools { get; set; } = new List<PoolOptions>();
    public WriteOptions DefaultWrite { get; set; } = new();
    public IOPipelineOptions Pipeline { get; set; } = new();
    public string HashAlgorithm { get; set; } = "SHA-256";
    public DeduplicationMode Deduplication { get; set; } = DeduplicationMode.Global;
    public bool VerifyAfterWrite { get; set; }

    /// <summary>
    /// Object-internal fixed-size chunking. When disabled (default) each object is a
    /// single physical block. When enabled, objects larger than one chunk are split into
    /// fixed-size chunks, each stored as its own block plus an object-level manifest.
    /// </summary>
    public ChunkingOptions Chunking { get; set; } = new();
}