// 本文件包含类型 BackupStatus：备份点的状态。
namespace Mono.FileBox.Lite.Abstractions.Backup;

/// <summary>Status of a backup point.</summary>
public enum BackupStatus { Pending, Running, Completed, Failed, Corrupted }