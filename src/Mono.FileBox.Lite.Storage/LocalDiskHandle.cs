// <file>
// LocalDiskHandle: concrete IDiskHandle for local storage, carrying the pool root path.
// </file>

using Mono.FileBox.Lite.Abstractions.Storage;

namespace Mono.FileBox.Lite.Storage;

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