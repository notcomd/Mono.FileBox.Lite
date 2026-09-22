// 本文件包含类型 IBackupScheduler：注册/注销备份调度并列出已注册的调度。
namespace Mono.FileBox.Lite.Abstractions.Backup;

/// <summary>Registers/unregisters backup schedules and lists registered ones.</summary>
public interface IBackupScheduler
{
    Task ScheduleAsync(BackupSchedule schedule, CancellationToken ct);
    Task UnscheduleAsync(string scheduleId, CancellationToken ct);
    Task<IReadOnlyList<BackupSchedule>> ListAsync(CancellationToken ct);
}