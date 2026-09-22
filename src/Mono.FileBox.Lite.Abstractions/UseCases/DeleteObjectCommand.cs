// 本文件包含类型 DeleteObjectCommand：删除对象命令。
namespace Mono.FileBox.Lite.Abstractions.UseCases;

public sealed class DeleteObjectCommand
{
    public string ContentHash { get; init; } = string.Empty;
    public string NamespaceId { get; init; } = string.Empty;
}