// BackupStoreOptions.cs - Options controlling the file-system metadata stores.

namespace Mono.FileBox.Lite.Backup;

/// <summary>Options controlling the file-system metadata stores.</summary>
public sealed class BackupStoreOptions
{
    public string RootPath { get; set; } = Path.Combine(Path.GetTempPath(), "mono-filebox-backup-meta");
}