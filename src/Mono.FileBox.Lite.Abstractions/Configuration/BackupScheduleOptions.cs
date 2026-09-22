// 本文件包含类型 BackupScheduleOptions：备份调度选项。
using Mono.FileBox.Lite.Abstractions.Backup;

namespace Mono.FileBox.Lite.Abstractions.Configuration;

public sealed class BackupScheduleOptions
{
    public string ScheduleId { get; set; } = string.Empty;
    public string Cron { get; set; } = string.Empty;
    public BackupKind Kind { get; set; } = BackupKind.Incremental;
    public string TargetId { get; set; } = string.Empty;
    public string? NamespaceId { get; set; }
    public TimeSpan? Retention { get; set; }
    public int? MaxKeep { get; set; }
    public bool Enabled { get; set; } = true;
}