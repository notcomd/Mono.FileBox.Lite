// BackupStoreOptions.cs - Options controlling the file-system metadata stores.

namespace Mono.FileBox.Lite.Backup;

/// <summary>Options controlling the file-system metadata stores.
/// 控制文件系统元数据存储的选项。</summary>
public sealed class BackupStoreOptions
{
    public string RootPath { get; set; } = Path.Combine(Path.GetTempPath(), "mono-filebox-backup-meta");
}