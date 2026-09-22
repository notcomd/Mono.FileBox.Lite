// 本文件包含类型 BackupKind：备份类型。
namespace Mono.FileBox.Lite.Abstractions.Backup;

/// <summary>Type of a backup. 中文翻译：备份类型（全量/增量/差异）。</summary>
public enum BackupKind { Full, Incremental, Differential }