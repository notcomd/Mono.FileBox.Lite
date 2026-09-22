// 本文件包含类型 IBackupPointStore：持久化备份点元数据。
namespace Mono.FileBox.Lite.Abstractions.Backup;

/// <summary>Persists backup-point metadata. 中文翻译：持久化备份点元数据的存储。</summary>
public interface IBackupPointStore
{
    Task SaveAsync(BackupPoint point, CancellationToken ct);
    Task<BackupPoint?> GetAsync(string backupId, CancellationToken ct);
    Task DeleteAsync(string backupId, CancellationToken ct);
    Task<IReadOnlyList<BackupPoint>> ListAsync(BackupQuery query, CancellationToken ct);
}