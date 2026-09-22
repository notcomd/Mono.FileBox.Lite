// 本文件包含类型 GetObjectCommand：获取对象命令。
namespace Mono.FileBox.Lite.Abstractions.UseCases;

public sealed class GetObjectCommand
{
    public string ContentHash { get; init; } = string.Empty;
    public string NamespaceId { get; init; } = string.Empty;
    public long Offset { get; init; }
    public long? Length { get; init; }
}