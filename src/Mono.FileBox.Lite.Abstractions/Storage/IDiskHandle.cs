// 本文件包含类型 IDiskHandle：池内具体磁盘的句柄。
namespace Mono.FileBox.Lite.Abstractions.Storage;

/// <summary>Handle to a concrete disk within a pool.</summary>
public interface IDiskHandle
{
    string PoolId { get; }
    string DiskId { get; }
    bool IsHealthy { get; }
}