// CronBackupScheduler.cs - Registers and lists backup schedules.

using Mono.FileBox.Lite.Abstractions.Backup;

namespace Mono.FileBox.Lite.Backup.Scheduler;

/// <summary>
/// Registers and lists backup schedules. Actual deadline evaluation is left to the host
/// (e.g. a hosted service using a cron engine); this stores schedule state only.
/// 登记并列出备份计划。实际的截止时间评估交由宿主程序处理（例如使用 cron 引擎的托管服务）；此类仅存储计划状态。
/// </summary>
public sealed class CronBackupScheduler : IBackupScheduler
{
    private readonly Dictionary<string, BackupSchedule> _schedules = new(StringComparer.Ordinal);
    private readonly object _lock = new();

    public Task ScheduleAsync(BackupSchedule schedule, CancellationToken ct)
    {
        lock (_lock) _schedules[schedule.ScheduleId] = schedule;
        return Task.CompletedTask;
    }

    public Task UnscheduleAsync(string scheduleId, CancellationToken ct)
    {
        lock (_lock) _schedules.Remove(scheduleId);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<BackupSchedule>> ListAsync(CancellationToken ct)
    {
        lock (_lock) return Task.FromResult<IReadOnlyList<BackupSchedule>>(
            _schedules.Values.OrderBy(s => s.ScheduleId, StringComparer.Ordinal).ToArray());
    }
}