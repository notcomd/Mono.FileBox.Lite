// 本文件包含类型 BackupOptions：备份选项。
namespace Mono.FileBox.Lite.Abstractions.Configuration;

public sealed class BackupOptions
{
    public IList<BackupTargetOptions> Targets { get; set; } = new List<BackupTargetOptions>();
    public IList<BackupScheduleOptions> Schedules { get; set; } = new List<BackupScheduleOptions>();
    public string? DefaultTargetId { get; set; }
    public int MaxConcurrency { get; set; } = 4;
    public long BandwidthLimit { get; set; }
    public int MaxRetries { get; set; } = 3;
    public RetentionPolicy Retention { get; set; } = new();
}