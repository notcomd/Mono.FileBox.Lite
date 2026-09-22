// 本文件包含类型 IIOPipeline：对象流读写/删除/存在的 I/O 管道原语。
namespace Mono.FileBox.Lite.Abstractions.Storage;

/// <summary>I/O pipeline primitive for object stream read/write/delete/exists. 中文翻译：对象流的读写/删除/存在性检查的 I/O 管道原语。</summary>
public interface IIOPipeline
{
    Task WriteAsync(
        IDiskHandle disk, string contentHash, Stream content,
        WriteOptions options, CancellationToken ct);
    Task<Stream> ReadAsync(
        IDiskHandle disk, string contentHash,
        long offset, long length, CancellationToken ct);
    Task DeleteAsync(IDiskHandle disk, string contentHash, CancellationToken ct);
    Task<bool> ExistsAsync(IDiskHandle disk, string contentHash, CancellationToken ct);
}