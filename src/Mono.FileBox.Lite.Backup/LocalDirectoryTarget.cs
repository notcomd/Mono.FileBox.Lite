// LocalDirectoryTarget.cs - Backup target rooted at a directory on the local filesystem.

using Mono.FileBox.Lite.Abstractions.Backup;

namespace Mono.FileBox.Lite.Backup.Targets;

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