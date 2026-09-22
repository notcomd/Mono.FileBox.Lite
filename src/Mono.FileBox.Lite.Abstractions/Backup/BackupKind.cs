// 本文件包含类型 BackupKind：备份类型。
namespace Mono.FileBox.Lite.Abstractions.Backup;

/// <summary>Type of a backup.</summary>
public enum BackupKind { Full, Incremental, Differential }