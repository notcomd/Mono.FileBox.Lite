// 本文件包含类型 IBackupRestorer：从备份点恢复对象并校验备份完整性。
namespace Mono.FileBox.Lite.Abstractions.Backup;

/// <summary>Restores objects from a backup point and verifies backup integrity. 中文翻译：从备份点恢复对象并校验备份完整性的恢复器。</summary>
public interface IBackupRestorer
{
    Task<RestoreReport> RestoreAsync(RestoreRequest request, CancellationToken ct);
    Task<RestoreReport> RestoreToPointAsync(
        string backupId, RestoreRequest request, CancellationToken ct);
    Task<BackupVerificationReport> VerifyAsync(string backupId, CancellationToken ct);
}