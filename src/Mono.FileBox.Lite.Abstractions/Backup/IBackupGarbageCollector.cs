// 本文件包含类型 IBackupGarbageCollector：扫描共享后备块池并回收无引用的块。
namespace Mono.FileBox.Lite.Abstractions.Backup;

/// <summary>Scans the shared backing block pool and reclaims unreferenced blocks.</summary>
public interface IBackupGarbageCollector
{
    Task<long> CollectAsync(CancellationToken ct);
}