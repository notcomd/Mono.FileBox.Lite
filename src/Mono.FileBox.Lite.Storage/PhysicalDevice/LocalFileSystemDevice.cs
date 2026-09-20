using Mono.FileBox.Lite.Abstractions.Storage;

namespace Mono.FileBox.Lite.Storage.PhysicalDevice;

/// <summary>
/// Physical block device backed by the local filesystem. Operates purely on opaque
/// paths and has no knowledge of content hashes or disk pools.
/// </summary>
public sealed class LocalFileSystemDevice : IPhysicalDevice
{
    public Task WriteBlockAsync(string path, ReadOnlyMemory<byte> data, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllBytes(path, data.ToArray());
        return Task.CompletedTask;
    }

    public Task<ReadOnlyMemory<byte>> ReadBlockAsync(string path, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult<ReadOnlyMemory<byte>>(File.ReadAllBytes(path));
    }

    public Task DeleteBlockAsync(string path, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string path, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(File.Exists(path));
    }
}