// RemoteObjectTarget.cs - Remote object-store backup target backed by a local mirror directory.

using Mono.FileBox.Lite.Abstractions.Backup;

namespace Mono.FileBox.Lite.Backup.Targets;

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