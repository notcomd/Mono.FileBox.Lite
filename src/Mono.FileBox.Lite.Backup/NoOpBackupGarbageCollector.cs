// NoOpBackupGarbageCollector.cs - No-op backup garbage collector.

using Mono.FileBox.Lite.Abstractions.Backup;

namespace Mono.FileBox.Lite.Backup.Scheduler;

/// <summary>
/// No-op backup garbage collector. Real block reclamation requires scanning every
/// manifest's referenced blocks before deleting; left as a no-op for the Lite engine.
/// 中文翻译：空操作（no-op）备份垃圾回收器。真正的块回收需要先扫描每个清单所引用的块再删除；在 Lite 引擎中保留为空操作实现。
/// </summary>
public sealed class NoOpBackupGarbageCollector : IBackupGarbageCollector
{
    public Task<long> CollectAsync(CancellationToken ct) => Task.FromResult(0L);
}