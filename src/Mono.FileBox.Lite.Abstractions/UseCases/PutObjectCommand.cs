// 本文件包含类型 PutObjectCommand：放置对象命令。
using Mono.FileBox.Lite.Abstractions.Storage;

namespace Mono.FileBox.Lite.Abstractions.UseCases;

public sealed class PutObjectCommand
{
    public string NamespaceId { get; init; } = string.Empty;
    public string? ObjectKey { get; init; }
    public Stream Content { get; init; } = Stream.Null;
    public string? ContentType { get; init; }
    public IReadOnlyDictionary<string, string>? Tags { get; init; }
    public IReadOnlyDictionary<string, object>? Attributes { get; init; }
    public WriteOptions? Write { get; init; }
}