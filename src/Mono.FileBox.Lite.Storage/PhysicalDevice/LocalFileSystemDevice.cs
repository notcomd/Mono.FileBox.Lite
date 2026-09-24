using System.Buffers;
using Mono.FileBox.Lite.Abstractions.Storage;

namespace Mono.FileBox.Lite.Storage.PhysicalDevice;

/// <summary>
/// Physical block device backed by the local filesystem. Operates purely on opaque
/// paths and has no knowledge of content hashes or disk pools.
/// 基于本地文件系统的物理块设备，仅按不透明路径进行读写，不感知内容哈希或磁盘池概念。
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

    /// <summary>
    /// Streams <paramref name="content"/> to <paramref name="path"/> with a bounded buffer,
    /// avoiding buffering the whole payload in memory. Used by the I/O pipeline for
    /// large, (possibly non-seekable) source streams.
/// 以有界缓冲区将 <paramref name="content"/> 流式写入 <paramref name="path"/>，避免整段载荷驻留内存，供 I/O 管道用于处理大体积（可能不可定位）的源流。
/// </summary>
    public Task WriteBlockStreamAsync(string path, Stream content, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        if (content.CanSeek) content.Position = 0;

        using var output = File.Create(path);
        var buffer = ArrayPool<byte>.Shared.Rent(81920);
        try
        {
            int read;
            while ((read = content.Read(buffer, 0, buffer.Length)) > 0)
            {
                ct.ThrowIfCancellationRequested();
                output.Write(buffer, 0, read);
            }
            output.Flush();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
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