using Mono.FileBox.Lite.Abstractions.Index;

namespace Mono.FileBox.Lite.Abstractions.Storage;

/// <summary>Options applied when writing an object's content.</summary>
public sealed class WriteOptions
{
    public StorageTier Tier { get; set; } = StorageTier.Hot;
    public string? PoolId { get; set; }
    public string? NamespaceId { get; set; }
    public long? BandwidthLimit { get; set; }
}

/// <summary>Deduplication strategy.</summary>
public enum DeduplicationMode
{
    /// <summary>Deduplicate against all existing content hash keys.</summary>
    Global,

    /// <summary>Deduplicate only within the same namespace.</summary>
    NamespaceScoped,

    /// <summary>No deduplication; always write a fresh physical block.</summary>
    Disabled
}

/// <summary>Information about a storage disk pool.</summary>
public sealed class DiskPoolInfo
{
    public string PoolId { get; init; } = string.Empty;
    public string RootPath { get; init; } = string.Empty;
    public StorageTier Tier { get; init; } = StorageTier.Hot;
    public long? CapacityBytes { get; init; }
    public long? UsedBytes { get; init; }
    public int Priority { get; init; }
    public bool Enabled { get; init; } = true;
    public int HealthyDiskCount { get; init; }
}

/// <summary>Handle to a concrete disk within a pool.</summary>
public interface IDiskHandle
{
    string PoolId { get; }
    string DiskId { get; }
    bool IsHealthy { get; }
}

/// <summary>
/// Chooses which disk/pool a content hash is written to or read from, and reports
/// pool-level health and capacity.
/// </summary>
public interface IDiskSelector
{
    Task<IDiskHandle> SelectForWriteAsync(
        string contentHash, WriteOptions options, CancellationToken ct);
    Task<IDiskHandle> SelectForReadAsync(string contentHash, CancellationToken ct);
    Task<IReadOnlyList<DiskPoolInfo>> ListPoolsAsync(CancellationToken ct);
}

/// <summary>I/O pipeline primitive for object stream read/write/delete/exists.</summary>
public interface IIOPipeline
{
    Task WriteAsync(
        IDiskHandle disk, string contentHash, Stream content,
        WriteOptions options, CancellationToken ct);
    Task<Stream> ReadAsync(
        IDiskHandle disk, string contentHash,
        long offset, long length, CancellationToken ct);
    Task DeleteAsync(IDiskHandle disk, string contentHash, CancellationToken ct);
    Task<bool> ExistsAsync(IDiskHandle disk, string contentHash, CancellationToken ct);
}

/// <summary>
/// Physical block device primitive operating on opaque paths. It has no knowledge
/// of content hashes or disk pools.
/// </summary>
public interface IPhysicalDevice
{
    Task WriteBlockAsync(string path, ReadOnlyMemory<byte> data, CancellationToken ct);
    Task<ReadOnlyMemory<byte>> ReadBlockAsync(string path, CancellationToken ct);
    Task DeleteBlockAsync(string path, CancellationToken ct);
    Task<bool> ExistsAsync(string path, CancellationToken ct);
}