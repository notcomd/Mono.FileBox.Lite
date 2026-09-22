// NoOpBackupGarbageCollector.cs - No-op backup garbage collector.

using Mono.FileBox.Lite.Abstractions.Backup;

namespace Mono.FileBox.Lite.Backup.Scheduler;

/// <summary>
/// No-op backup garbage collector. Real block reclamation requires scanning every
/// manifest's referenced blocks before deleting; left as a no-op for the Lite engine.
/// </summary>
public sealed class NoOpBackupGarbageCollector : IBackupGarbageCollector
{
    public Task<long> CollectAsync(CancellationToken ct) => Task.FromResult(0L);
}