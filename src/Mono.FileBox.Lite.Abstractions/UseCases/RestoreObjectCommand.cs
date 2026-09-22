// 本文件包含类型 RestoreObjectCommand：恢复对象命令。
namespace Mono.FileBox.Lite.Abstractions.UseCases;

public sealed class RestoreObjectCommand
{
    public string ContentHash { get; init; } = string.Empty;
    public string NamespaceId { get; init; } = string.Empty;
}