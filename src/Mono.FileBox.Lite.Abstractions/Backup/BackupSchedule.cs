// 本文件包含类型 BackupSchedule：备份调度（cron 表达式、类型、目标、保留策略）。
namespace Mono.FileBox.Lite.Abstractions.Backup;

/// <summary>A backup schedule (cron expression, kind, target, retention).</summary>
public sealed class BackupSchedule
{
    public string ScheduleId { get; init; } = string.Empty;
    public string Cron { get; init; } = string.Empty;
    public BackupKind Kind { get; init; } = BackupKind.Incremental;
    public string TargetId { get; init; } = string.Empty;
    public string? NamespaceId { get; init; }
    public TimeSpan? Retention { get; init; }
    public int? MaxKeep { get; init; }
    public bool Enabled { get; init; } = true;
}