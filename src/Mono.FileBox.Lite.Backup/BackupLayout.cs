// BackupLayout.cs - Relative layout of a backup target's block pool and backup points.

using Mono.FileBox.Lite.Abstractions.Backup;

namespace Mono.FileBox.Lite.Backup.Targets;

/// <summary>The relative layout of a backup target's block pool and backup points.
/// 备份目标块池与备份点的相对目录布局。</summary>
public static class BackupLayout
{
    /// <summary>Relative block path: blocks/{hash[0:2]}/{hash[2:4]}/{hash}.</summary>
    public static string BlockPath(string hash)
        => $"blocks/{hash.Substring(0, 2)}/{hash.Substring(2, 2)}/{hash}";

    /// <summary>Relative manifest path for a backup point.</summary>
    public static string ManifestPath(string backupId)
        => $"backups/{backupId}/manifest.json";

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>Writes a manifest to the target's <c>backups/{id}/manifest.json</c>.</summary>
    public static async Task WriteManifestAsync(
        IBackupTarget target, string backupId, BackupManifest manifest, CancellationToken ct)
    {
        using var stream = new MemoryStream();
        await System.Text.Json.JsonSerializer.SerializeAsync(stream, manifest, JsonOptions, ct).ConfigureAwait(false);
        stream.Position = 0;
        await target.WriteAsync(ManifestPath(backupId), stream, ct).ConfigureAwait(false);
    }
}