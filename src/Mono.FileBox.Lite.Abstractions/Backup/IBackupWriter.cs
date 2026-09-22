// 本文件包含类型 IBackupWriter：创建并管理备份点。
namespace Mono.FileBox.Lite.Abstractions.Backup;

/// <summary>Creates and manages backup points. 中文翻译：创建并管理备份点的写入器。</summary>
public interface IBackupWriter
{
    Task<BackupPoint> CreateAsync(BackupRequest request, CancellationToken ct);
    Task<BackupPoint> ContinueAsync(
        string parentBackupId, BackupRequest request, CancellationToken ct);
    Task CancelAsync(string backupId, CancellationToken ct);
}