using Mono.FileBox.Lite.Abstractions.Storage;
using Mono.FileBox.Lite.Storage.PhysicalDevice;

namespace Mono.FileBox.Lite.Storage.IOPipeline;

/// <summary>
/// Buffered I/O pipeline. Serializes the content to bytes, writes through the physical
/// device, and supports offset/length range reads.
/// 带缓冲的 I/O 管道：将内容序列化为字节经物理设备写入，并支持按偏移/长度进行范围读取。
/// </summary>
public sealed class BufferedIOPipeline : IIOPipeline
{
    private readonly IPhysicalDevice _device;

    public BufferedIOPipeline(IPhysicalDevice device) => _device = device;

    public Task WriteAsync(
        IDiskHandle disk, string contentHash, Stream content,
        WriteOptions options, CancellationToken ct)
    {
        if (disk is not LocalDiskHandle local)
            throw new NotSupportedException($"Unsupported disk handle '{disk.GetType().FullName}'.");

        var path = ObjectPathMapper.Resolve(local.RootPath, contentHash);

        // Streaming path: bounded-buffer copy, no full buffering of the payload.
        if (_device is LocalFileSystemDevice fs)
            return fs.WriteBlockStreamAsync(path, content, ct);

        var bytes = ReadAll(content, ct);
        return _device.WriteBlockAsync(path, bytes, ct);
    }

    public Task<Stream> ReadAsync(
        IDiskHandle disk, string contentHash,
        long offset, long length, CancellationToken ct)
    {
        if (disk is not LocalDiskHandle local)
            throw new NotSupportedException($"Unsupported disk handle '{disk.GetType().FullName}'.");

        var path = ObjectPathMapper.Resolve(local.RootPath, contentHash);

        // Streaming path: open the file and expose only the requested range. This avoids
        // materializing the whole block (ReadAllBytes + ToArray + slice) for large objects,
        // keeping download memory at O(1).
        // 流式路径：打开文件并只暴露请求区间，避免为读取大对象而整块物化（ReadAllBytes + ToArray + slice），
        // 使下载内存维持在 O(1)。
        if (_device is LocalFileSystemDevice fs)
            return fs.ReadBlockRangeAsync(path, offset, length, ct);

        return ReadAsyncBuffered(path, offset, length, ct);
    }

    private async Task<Stream> ReadAsyncBuffered(
        string path, long offset, long length, CancellationToken ct)
    {
        var block = await _device.ReadBlockAsync(path, ct).ConfigureAwait(false);
        var data = block.ToArray();

        offset = Math.Max(0, offset);
        if (length < 0) length = data.Length - offset;
        length = Math.Max(0, Math.Min(length, data.Length - offset));

        var slice = new byte[length];
        Array.Copy(data, offset, slice, 0, length);
        return new MemoryStream(slice, writable: false);
    }

    public Task DeleteAsync(IDiskHandle disk, string contentHash, CancellationToken ct)
    {
        if (disk is not LocalDiskHandle local)
            throw new NotSupportedException($"Unsupported disk handle '{disk.GetType().FullName}'.");
        return _device.DeleteBlockAsync(ObjectPathMapper.Resolve(local.RootPath, contentHash), ct);
    }

    public Task<bool> ExistsAsync(IDiskHandle disk, string contentHash, CancellationToken ct)
    {
        if (disk is not LocalDiskHandle local)
            throw new NotSupportedException($"Unsupported disk handle '{disk.GetType().FullName}'.");
        return _device.ExistsAsync(ObjectPathMapper.Resolve(local.RootPath, contentHash), ct);
    }

    private static byte[] ReadAll(Stream content, CancellationToken ct)
    {
        if (content is MemoryStream ms) return ms.ToArray();

        using var buffer = new MemoryStream();
        if (content.CanSeek) content.Position = 0;
        content.CopyTo(buffer, 81920);
        return buffer.ToArray();
    }
}