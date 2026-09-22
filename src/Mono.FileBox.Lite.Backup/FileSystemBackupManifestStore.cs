// FileSystemBackupManifestStore.cs - Persists backup manifests as JSON files.

using System.Text.Json;
using Mono.FileBox.Lite.Abstractions.Backup;

namespace Mono.FileBox.Lite.Backup;

/// <summary>Persists backup manifests as JSON files.</summary>
public sealed class FileSystemBackupManifestStore : IBackupManifestStore
{
    private readonly string _root;
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public FileSystemBackupManifestStore(BackupStoreOptions options)
    {
        _root = options.RootPath;
        Directory.CreateDirectory(_root);
    }

    private string FilePath(string backupId) => Path.Combine(_root, $"{backupId}.manifest.json");

    public Task SaveAsync(string backupId, BackupManifest manifest, CancellationToken ct)
    {
        File.WriteAllText(FilePath(backupId), JsonSerializer.Serialize(manifest, Json));
        return Task.CompletedTask;
    }

    public Task<BackupManifest?> GetAsync(string backupId, CancellationToken ct)
    {
        var path = FilePath(backupId);
        return Task.FromResult(File.Exists(path)
            ? JsonSerializer.Deserialize<BackupManifest>(File.ReadAllText(path), Json)
            : null);
    }

    public Task DeleteAsync(string backupId, CancellationToken ct)
    {
        if (File.Exists(FilePath(backupId))) File.Delete(FilePath(backupId));
        return Task.CompletedTask;
    }
}