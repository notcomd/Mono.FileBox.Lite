// 本文件包含类型 IBackupReader：列出、读取并检查备份点及其清单。
namespace Mono.FileBox.Lite.Abstractions.Backup;

/// <summary>Lists, reads and inspects backup points and their manifests. 列出、读取并检查备份点及其清单的读取器。</summary>
public interface IBackupReader
{
    Task<IReadOnlyList<BackupPoint>> ListAsync(BackupQuery query, CancellationToken ct);
    Task<BackupPoint?> GetAsync(string backupId, CancellationToken ct);
    Task<BackupManifest> ReadManifestAsync(string backupId, CancellationToken ct);
}