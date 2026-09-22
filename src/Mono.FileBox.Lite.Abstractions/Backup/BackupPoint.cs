// 本文件包含类型 BackupPoint：表示一次备份操作的不可变快照。
namespace Mono.FileBox.Lite.Abstractions.Backup;

/// <summary>Immutable snapshot representing one backup operation.</summary>
public sealed class BackupPoint
{
    public string BackupId { get; set; } = string.Empty;
    public string? ParentBackupId { get; set; }
    public BackupKind Kind { get; set; }
    public BackupScope Scope { get; set; } = new();
    public string TargetId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public BackupStatus Status { get; set; }
    public long ObjectCount { get; set; }
    public long TotalBytes { get; set; }
    public string ManifestHash { get; set; } = string.Empty;
}