using Mono.FileBox.Lite.Abstractions.Storage;

namespace Mono.FileBox.Lite.Storage;

/// <summary>
/// Maps a content hash to its physical layout within a pool:
/// <c>{root}/blocks/{hash[0:2]}/{hash[2:4]}/{hash}</c>.
/// </summary>
public static class ObjectPathMapper
{
    /// <summary>Relative block path for a content hash.</summary>
    public static string RelativeBlockPath(string contentHash)
    {
        if (string.IsNullOrWhiteSpace(contentHash))
            throw new ArgumentException("Content hash must not be empty.", nameof(contentHash));
        return Path.Combine("blocks", contentHash.Substring(0, 2), contentHash.Substring(2, 2), contentHash);
    }

    /// <summary>Absolute block path given a pool root and a content hash.</summary>
    public static string Resolve(string poolRoot, string contentHash)
        => Path.Combine(poolRoot, RelativeBlockPath(contentHash));
}

/// <summary>
/// Concrete <see cref="IDiskHandle"/> for local storage. Carries the pool root path
/// that the I/O pipeline needs to resolve physical block locations.
/// </summary>
public sealed class LocalDiskHandle : IDiskHandle
{
    public LocalDiskHandle(string poolId, string rootPath, bool isHealthy = true)
    {
        PoolId = poolId;
        RootPath = rootPath;
        IsHealthy = isHealthy;
    }

    public string PoolId { get; }
    public string DiskId => PoolId;
    public bool IsHealthy { get; }
    public string RootPath { get; }
}