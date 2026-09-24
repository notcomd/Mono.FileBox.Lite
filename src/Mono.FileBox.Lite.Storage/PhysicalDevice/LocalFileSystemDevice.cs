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

    /// <summary>
    /// Streams the [<paramref name="offset"/>, <paramref name="offset"/>+<paramref name="length"/>) slice of the
    /// file at <paramref name="path"/> back as a seekable stream. Unlike <see cref="ReadBlockAsync"/>, it does not
    /// materialize the whole block: it opens the file and only exposes the requested range, so large objects are
    /// read with O(1) memory. The <paramref name="length"/> &lt; 0 means "to end of file".
    /// 以流式返回 <paramref name="path"/> 处文件的 [<paramref name="offset"/>, <paramref name="offset"/>+<paramref name="length"/>)
    /// 区间切片。与 <see cref="ReadBlockAsync"/> 不同，它不整块物化数据，而是打开文件并只暴露请求区间，
    /// 从而以 O(1) 内存读取大对象。<paramref name="length"/> &lt; 0 表示“读到文件末尾”。
    /// </summary>
    public Task<Stream> ReadBlockRangeAsync(string path, long offset, long length, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        try
        {
            var fileLen = file.Length;
            offset = Math.Max(0, Math.Min(offset, fileLen));
            if (length < 0) length = fileLen - offset;
            else length = Math.Max(0, Math.Min(length, fileLen - offset));
            return Task.FromResult<Stream>(new FileRangeStream(file, offset, length));
        }
        catch
        {
            file.Dispose();
            throw;
        }
    }

    /// <summary>
    /// A seekable, read-only window over an underlying <see cref="FileStream"/> that only exposes
    /// the bytes in [<c>offset</c>, <c>offset</c>+<c>length</c>). Reads never escape the range.
    /// 对底层 <see cref="FileStream"/> 的一个可定位、只读的窗口视图，仅暴露 [<c>offset</c>, <c>offset</c>+<c>length</c>]
    /// 区间内的字节，读取不会越出该范围。
    /// </summary>
    private sealed class FileRangeStream : Stream
    {
        private readonly FileStream _inner;
        private readonly long _start;
        private readonly long _length;
        private long _pos;

        public FileRangeStream(FileStream inner, long offset, long length)
        {
            _inner = inner;
            _start = offset;
            _length = length;
            _inner.Position = offset;
            _pos = 0;
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => _length;
        public override long Position { get => _pos; set => Seek(value, SeekOrigin.Begin); }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_pos >= _length) return 0;
            count = (int)Math.Min(count, _length - _pos);
            var read = _inner.Read(buffer, offset, count);
            _pos += read;
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            var target = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => _pos + offset,
                _ => _length + offset
            };
            target = Math.Max(0, Math.Min(target, _length));
            _inner.Position = _start + target;
            _pos = target;
            return _pos;
        }

        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing) _inner.Dispose();
            base.Dispose(disposing);
        }
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