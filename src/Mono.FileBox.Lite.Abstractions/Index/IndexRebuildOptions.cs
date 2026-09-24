// 本文件包含类型 IndexRebuildOptions：完整索引重建周期的选项。
namespace Mono.FileBox.Lite.Abstractions.Index;

/// <summary>Options for a full index rebuild cycle. 完整索引重建周期的选项。</summary>
public sealed class IndexRebuildOptions
{
    public bool DropExisting { get; init; } = true;
    public bool RebuildBitmapIndexes { get; init; } = true;
    public int BatchSize { get; init; } = 500;
}