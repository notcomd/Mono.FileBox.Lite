// 本文件包含类型 BackupStatus：备份点的状态。
namespace Mono.FileBox.Lite.Abstractions.Backup;

/// <summary>Status of a backup point. 中文翻译：备份点的状态（待处理/进行中/完成/失败/损坏）。</summary>
public enum BackupStatus { Pending, Running, Completed, Failed, Corrupted }