// 本文件包含类型 IObjectWriter：内容寻址对象写入器（SHA-256 key、去重）。
using Mono.FileBox.Lite.Abstractions.Storage;

namespace Mono.FileBox.Lite.Abstractions.Subsystems;

/// <summary>Content-addressed object writer (SHA-256 key, deduplication).</summary>
public interface IObjectWriter
{
    Task<string> WriteAsync(Stream content, WriteOptions options, CancellationToken ct);
    Task<bool> ExistsAsync(string contentHash, CancellationToken ct);
}