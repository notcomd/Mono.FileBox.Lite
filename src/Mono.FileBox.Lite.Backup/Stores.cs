using System.Text.Json;
using Mono.FileBox.Lite.Abstractions.Backup;

namespace Mono.FileBox.Lite.Backup;

/// <summary>Options controlling the file-system metadata stores.</summary>
public sealed class BackupStoreOptions
{
    public string RootPath { get; set; } = Path.Combine(Path.GetTempPath(), "mono-filebox-backup-meta");
}

/// <summary>Persists backup points as JSON files.</summary>
public sealed class FileSystemBackupPointStore : IBackupPointStore
{
    private readonly string _root;
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public FileSystemBackupPointStore(BackupStoreOptions options)
    {
        _root = options.RootPath;
        Directory.CreateDirectory(_root);
    }

    public string RootPath => _root;

    private string FilePath(string backupId) => Path.Combine(_root, $"{backupId}.json");

    public Task SaveAsync(BackupPoint point, CancellationToken ct)
    {
        File.WriteAllText(FilePath(point.BackupId), JsonSerializer.Serialize(point, Json));
        return Task.CompletedTask;
    }

    public Task<BackupPoint?> GetAsync(string backupId, CancellationToken ct)
    {
        var path = FilePath(backupId);
        return Task.FromResult(File.Exists(path)
            ? JsonSerializer.Deserialize<BackupPoint>(File.ReadAllText(path), Json)
            : null);
    }

    public Task DeleteAsync(string backupId, CancellationToken ct)
    {
        if (File.Exists(FilePath(backupId))) File.Delete(FilePath(backupId));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<BackupPoint>> ListAsync(BackupQuery query, CancellationToken ct)
    {
        var points = Directory.EnumerateFiles(_root, "*.json")
            .Select(f => JsonSerializer.Deserialize<BackupPoint>(File.ReadAllText(f), Json))
            .OfType<BackupPoint>();

        if (query.Kind.HasValue) points = points.Where(p => p.Kind == query.Kind.Value);
        if (query.CreatedAfter.HasValue) points = points.Where(p => p.CreatedAt >= query.CreatedAfter.Value);

        return Task.FromResult<IReadOnlyList<BackupPoint>>(
            points.OrderByDescending(p => p.CreatedAt).ToArray());
    }
}

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