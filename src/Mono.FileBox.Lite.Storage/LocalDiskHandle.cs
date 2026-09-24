// <file>
// LocalDiskHandle: concrete IDiskHandle for local storage, carrying the pool root path.
// </file>

using Mono.FileBox.Lite.Abstractions.Storage;

namespace Mono.FileBox.Lite.Storage;

/// <summary>
/// Concrete <see cref="IDiskHandle"/> for local storage. Carries the pool root path
/// that the I/O pipeline needs to resolve physical block locations.
/// 本地存储的具体 <see cref="IDiskHandle"/> 实现，携带 I/O 管道用于解析物理块位置所需的池根路径。
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