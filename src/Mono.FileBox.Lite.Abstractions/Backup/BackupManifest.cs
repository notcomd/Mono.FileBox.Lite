// 本文件包含类型 BackupManifest：引用某备份点所属的所有块。
namespace Mono.FileBox.Lite.Abstractions.Backup;

/// <summary>References all blocks belonging to a backup point.</summary>
public sealed class BackupManifest
{
    public string BackupId { get; init; } = string.Empty;
    public IReadOnlyList<BackupEntry> Entries { get; init; } = Array.Empty<BackupEntry>();
    public string ManifestHash { get; init; } = string.Empty;
}