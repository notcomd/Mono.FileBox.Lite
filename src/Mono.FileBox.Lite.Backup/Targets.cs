using Mono.FileBox.Lite.Abstractions.Backup;

namespace Mono.FileBox.Lite.Backup.Targets;

/// <summary>Options for a local-directory backup target.</summary>
public sealed class LocalDirectoryOptions
{
    public string RootPath { get; set; } = string.Empty;
}

/// <summary>Options for a remote object-store backup target.</summary>
public sealed class RemoteObjectOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string Bucket { get; set; } = string.Empty;
    public string? LocalMirrorPath { get; set; }
}

/// <summary>The relative layout of a backup target's block pool and backup points.</summary>
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

/// <summary>
/// Backup target rooted at a directory on the local filesystem.
/// Layout: <c>{root}/blocks/...</c> for the shared block pool and
/// <c>{root}/backups/{id}/manifest.json</c> for backup points.
/// </summary>
public sealed class LocalDirectoryTarget : IBackupTarget
{
    private readonly LocalDirectoryOptions _options;

    public LocalDirectoryTarget(LocalDirectoryOptions options)
    {
        _options = options;
        if (string.IsNullOrWhiteSpace(options.RootPath))
            throw new ArgumentException("RootPath is required.", nameof(options));
    }

    public string RootPath => _options.RootPath;

    private string Full(string relativePath) => Path.Combine(RootPath, relativePath);

    public Task<bool> ExistsAsync(string relativePath, CancellationToken ct)
        => Task.FromResult(File.Exists(Full(relativePath)) || Directory.Exists(Full(relativePath)));

    public async Task WriteAsync(string relativePath, Stream content, CancellationToken ct)
    {
        var full = Full(relativePath);
        var dir = Path.GetDirectoryName(full);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        using var outStream = File.Create(full);
        await content.CopyToAsync(outStream, 81920, ct).ConfigureAwait(false);
    }

    public Task<Stream> ReadAsync(string relativePath, CancellationToken ct)
        => Task.FromResult<Stream>(File.OpenRead(Full(relativePath)));

    public Task DeleteAsync(string relativePath, CancellationToken ct)
    {
        var full = Full(relativePath);
        if (File.Exists(full)) File.Delete(full);
        else if (Directory.Exists(full)) Directory.Delete(full, recursive: true);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> ListAsync(string prefix, CancellationToken ct)
    {
        if (!Directory.Exists(RootPath)) return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        var rootUri = new Uri(Path.Combine(Path.GetFullPath(RootPath), ".") + Path.DirectorySeparatorChar);
        var result = Directory.EnumerateFiles(RootPath, "*", SearchOption.AllDirectories)
            .Select(f => rootUri.MakeRelativeUri(new Uri(Path.GetFullPath(f))).ToString().Replace('/', '/'))
            .Where(p => string.IsNullOrEmpty(prefix) || p.StartsWith(prefix, StringComparison.Ordinal))
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToArray();
        return Task.FromResult<IReadOnlyList<string>>(result);
    }
}

/// <summary>
/// Remote object-store target. This Lite implementation stores blocks under a local
/// mirror directory so the full backup flow runs end-to-end without external services.
/// </summary>
public sealed class RemoteObjectTarget : IBackupTarget
{
    private readonly LocalDirectoryTarget _inner;

    public RemoteObjectTarget(RemoteObjectOptions options)
    {
        var bucket = string.IsNullOrWhiteSpace(options.Bucket) ? "filebox-backup" : options.Bucket;
        var mirror = options.LocalMirrorPath
                     ?? Path.Combine(Path.GetTempPath(), "mono-filebox-backup", bucket);
        _inner = new LocalDirectoryTarget(new LocalDirectoryOptions { RootPath = mirror });
    }

    public Task<bool> ExistsAsync(string relativePath, CancellationToken ct) => _inner.ExistsAsync(relativePath, ct);
    public Task WriteAsync(string relativePath, Stream content, CancellationToken ct) => _inner.WriteAsync(relativePath, content, ct);
    public Task<Stream> ReadAsync(string relativePath, CancellationToken ct) => _inner.ReadAsync(relativePath, ct);
    public Task DeleteAsync(string relativePath, CancellationToken ct) => _inner.DeleteAsync(relativePath, ct);
    public Task<IReadOnlyList<string>> ListAsync(string prefix, CancellationToken ct) => _inner.ListAsync(prefix, ct);
}

/// <summary>Default registry-based target resolver.</summary>
public sealed class DefaultBackupTargetResolver : IBackupTargetResolver
{
    private readonly Dictionary<string, IBackupTarget> _targets = new(StringComparer.Ordinal);

    public IBackupTarget Resolve(string targetId)
    {
        if (_targets.TryGetValue(targetId, out var t)) return t;
        throw new KeyNotFoundException($"No backup target registered as '{targetId}'.");
    }

    public void Register(string targetId, IBackupTarget target)
        => _targets[targetId] = target;

    public IReadOnlyList<string> TargetIds => _targets.Keys.ToArray();
}