// 本文件包含类型 IObjectReader：支持范围读取的内容寻址对象读取器。
namespace Mono.FileBox.Lite.Abstractions.Subsystems;

/// <summary>Content-addressed object reader for range reads.
/// 支持范围读取的内容寻址对象读取器。</summary>
public interface IObjectReader
{
    Task<Stream> ReadAsync(
        string contentHash, long offset, long length, CancellationToken ct);
}