// 本文件包含类型 ArchiveObjectCommand：归档对象命令。
using Mono.FileBox.Lite.Abstractions.Index;

namespace Mono.FileBox.Lite.Abstractions.UseCases;

public sealed class ArchiveObjectCommand
{
    public string ContentHash { get; init; } = string.Empty;
    public string NamespaceId { get; init; } = string.Empty;
    public StorageTier Tier { get; init; } = StorageTier.Cold;
}