// 本文件包含类型 ChunkingOptions：对象内部固定大小分块的选项。
namespace Mono.FileBox.Lite.Abstractions.Configuration;

/// <summary>Options for object-internal fixed-size chunking.</summary>
public sealed class ChunkingOptions
{
    /// <summary>Enables chunking (per chunk-block write + object manifest).</summary>
    public bool Enabled { get; set; }

    /// <summary>Target bytes per chunk. Objects larger than this are split.</summary>
    public long ChunkSizeBytes { get; set; } = 8L * 1024 * 1024;
}