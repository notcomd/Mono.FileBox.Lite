// 本文件包含类型 IBackupManifestStore：按备份 id 持久化备份清单。
namespace Mono.FileBox.Lite.Abstractions.Backup;

/// <summary>Persists backup manifests keyed by backup id. 中文翻译：以备份 id 为键持久化备份清单的存储。</summary>
public interface IBackupManifestStore
{
    Task SaveAsync(string backupId, BackupManifest manifest, CancellationToken ct);
    Task<BackupManifest?> GetAsync(string backupId, CancellationToken ct);
    Task DeleteAsync(string backupId, CancellationToken ct);
}